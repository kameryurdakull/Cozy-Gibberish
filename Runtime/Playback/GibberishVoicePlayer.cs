using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace CozyGibberish
{
    /// <summary>
    /// Main-thread playback boundary with deterministic capture, cached rendering,
    /// DSP scheduling, equal-power crossfades and bounded priority queuing.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class GibberishVoicePlayer :
        MonoBehaviour,
        IAdvancedGibberishSpeechService
    {
        private const int MaximumTextLength = 2048;
        private const string GeneratedClipPrefix = "Gibberish_";
        private const float DefaultCrossfadeDuration = 0.08f;
        private const float DefaultScheduleLeadTime = 0.04f;
        private const float DefaultEventLookahead = 0.025f;
        private const int DefaultQueueCapacity = 32;
        private const int DefaultCacheEntries = 32;
        private const long DefaultCacheMemory = 32L * 1024L * 1024L;

        private static long _nextHandleId;

        [Header("Defaults")]
        [SerializeField] private GibberishVoiceProfile _defaultVoice;
        [SerializeField] private GibberishStylePreset _defaultStyle;
        [SerializeField] private GibberishStyleStack _defaultStyleStack;
        [SerializeField] private GibberishPhonotacticProfile _defaultPhonotactics;
        [SerializeField] private GibberishProsodyProfile _defaultProsody;
        [SerializeField] private GibberishHybridVoiceBank _defaultHybridVoiceBank;

        [Header("Output")]
        [SerializeField] private GibberishOutputProfile _outputProfile;
        [SerializeField] private AudioSource _primarySource;
        [SerializeField] private AudioSource _secondarySource;
        [SerializeField, Range(0f, 1f)] private float _legacySpatialBlend;

        private readonly List<QueuedSpeech> _queue = new List<QueuedSpeech>();
        private IGibberishUtterancePlanner _planner;
        private IGibberishSynthesizer _synthesizer;
        private IGibberishEventBus _events;
        private GibberishPreparedSpeechCache _cache;
        private GibberishStreamingAudioOutput _streamingOutput;
        private ActiveSpeech _active;
        private FadingSpeech _fading;
        private long _queueSequence;
        private double _pauseDspTime;

        public bool IsSpeaking => _active != null;
        public bool IsPaused => _active != null && _active.IsPaused;
        public int QueueCount => _queue.Count;
        public GibberishPlaybackHandle CurrentHandle =>
            _active == null ? GibberishPlaybackHandle.Invalid : _active.Handle;
        public IGibberishEventBus Events => _events ?? (_events = new GibberishEventBus());
        public GibberishVoiceProfile DefaultVoice => _defaultVoice;
        public GibberishStylePreset DefaultStyle => _defaultStyle;
        public GibberishOutputProfile OutputProfile => _outputProfile;

        public void Initialize(
            IGibberishUtterancePlanner planner,
            IGibberishSynthesizer synthesizer,
            IGibberishEventBus eventBus)
        {
            _planner = planner ?? throw new ArgumentNullException(nameof(planner));
            _synthesizer = synthesizer ?? throw new ArgumentNullException(nameof(synthesizer));
            _events = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public void SetDefaults(
            GibberishVoiceProfile voice,
            GibberishStylePreset style = null)
        {
            _defaultVoice = voice;
            _defaultStyle = style;
        }

        public void SetConfiguration(
            GibberishStyleStack styleStack,
            GibberishPhonotacticProfile phonotactics,
            GibberishProsodyProfile prosody,
            GibberishHybridVoiceBank hybridVoiceBank,
            GibberishOutputProfile outputProfile)
        {
            _defaultStyleStack = styleStack;
            _defaultPhonotactics = phonotactics;
            _defaultProsody = prosody;
            _defaultHybridVoiceBank = hybridVoiceBank;
            _outputProfile = outputProfile;
            ConfigureSources();
            ConfigureCache();
        }

        public GibberishPlaybackHandle Speak(string text)
        {
            return Speak(new GibberishSpeechRequest(
                text,
                _defaultVoice,
                _defaultStyle));
        }

        public GibberishPlaybackHandle Speak(GibberishSpeechRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return GibberishPlaybackHandle.Invalid;
            }

            if (request.PlaybackPolicy == GibberishPlaybackPolicy.IgnoreIfSpeaking && IsSpeaking)
            {
                return GibberishPlaybackHandle.Invalid;
            }

            var prepared = Prewarm(request);
            return PlayPrepared(prepared);
        }

        public GibberishPlaybackHandle[] SpeakSequence(GibberishSpeechSequence sequence)
        {
            if (sequence == null)
            {
                throw new ArgumentNullException(nameof(sequence));
            }

            var handles = new GibberishPlaybackHandle[sequence.Segments.Count];
            for (var index = 0; index < sequence.Segments.Count; index++)
            {
                var request = sequence.CreateRequest(
                    index,
                    _defaultVoice,
                    _defaultStyle,
                    _defaultPhonotactics,
                    _defaultProsody,
                    _defaultHybridVoiceBank);
                handles[index] = Speak(request);
            }

            return handles;
        }

        public GibberishPreparedSpeech Prewarm(GibberishSpeechRequest request)
        {
            var context = Capture(request);
            return Render(context, CancellationToken.None);
        }

        public GibberishSpeechContext Capture(GibberishSpeechRequest request)
        {
            var voiceProfile = request.Voice != null ? request.Voice : _defaultVoice;
            if (voiceProfile == null)
            {
                throw new InvalidOperationException(
                    "A GibberishVoiceProfile must be assigned to the request or as the player's default voice.");
            }

            var style = request.Style != null ? request.Style : _defaultStyle;
            var styleStack = request.StyleStack != null
                ? request.StyleStack
                : _defaultStyleStack;
            var phonotacticProfile = request.Phonotactics != null
                ? request.Phonotactics
                : _defaultPhonotactics;
            var prosodyProfile = request.Prosody != null
                ? request.Prosody
                : _defaultProsody;
            var hybridBank = request.HybridVoiceBank != null
                ? request.HybridVoiceBank
                : _defaultHybridVoiceBank;
            var text = SanitizeText(request.Text);
            var styleInfluence = styleStack == null
                ? GibberishStyleInfluence.From(style)
                : styleStack.CreateInfluence(style);
            var prosody = prosodyProfile == null
                ? GibberishProsodySnapshot.Neutral
                : prosodyProfile.CreateSnapshot();
            var quality = ResolveQualityTier();
            var voice = voiceProfile
                .CreateSnapshot(styleInfluence, request.Expression, prosody)
                .WithQuality(quality);
            var phonotactics = phonotacticProfile == null
                ? GibberishPhonotacticSnapshot.Default
                : phonotacticProfile.CreateSnapshot();
            var planner = GetPlanner();
            var utterance = planner is IAdvancedGibberishUtterancePlanner advancedPlanner
                ? advancedPlanner.CreatePlan(text, voice, phonotactics, request.Seed)
                : planner.CreatePlan(text, voice, request.Seed);
            utterance = utterance.WithAdditionalDuration(request.TailPause);
            var hybrid = hybridBank == null
                ? GibberishHybridSnapshot.Empty
                : hybridBank.CaptureSnapshot();
            var fingerprint = GibberishRequestFingerprint.Create(
                text,
                request.Seed,
                voiceProfile,
                style,
                styleStack,
                phonotacticProfile,
                prosodyProfile,
                hybridBank,
                request.Expression,
                quality,
                request.TailPause);
            var styleName = styleStack != null
                ? styleStack.name
                : style == null
                    ? string.Empty
                    : style.name;

            return new GibberishSpeechContext(
                text,
                voiceProfile.name,
                styleName,
                voice,
                utterance,
                request.PlaybackPolicy,
                request.Priority,
                hybrid,
                fingerprint);
        }

        public GibberishPreparedSpeech Render(GibberishSpeechContext context)
        {
            return Render(context, CancellationToken.None);
        }

        public GibberishPreparedSpeech Render(
            GibberishSpeechContext context,
            CancellationToken cancellationToken)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var cache = GetCache();
            if (context.Fingerprint.Value != 0UL &&
                cache.TryGet(context.Fingerprint, out var cachedAudio))
            {
                return new GibberishPreparedSpeech(context, cachedAudio);
            }

            cancellationToken.ThrowIfCancellationRequested();
            var synthesizer = GetSynthesizer();
            var audio = synthesizer is IAdvancedGibberishSynthesizer advancedSynthesizer
                ? advancedSynthesizer.Render(
                    context.Utterance,
                    context.Voice,
                    context.Hybrid,
                    cancellationToken)
                : synthesizer.Render(context.Utterance, context.Voice);
            cancellationToken.ThrowIfCancellationRequested();

            if (context.Fingerprint.Value != 0UL)
            {
                cache.Store(context.Fingerprint, audio);
            }

            return new GibberishPreparedSpeech(context, audio);
        }

        public GibberishPlaybackHandle PlayPrepared(GibberishPreparedSpeech prepared)
        {
            if (prepared == null)
            {
                throw new ArgumentNullException(nameof(prepared));
            }

            EnsureOutputs();
            var handle = CreateHandle();
            var policy = prepared.Context.PlaybackPolicy;

            if (policy == GibberishPlaybackPolicy.IgnoreIfSpeaking && IsSpeaking)
            {
                return GibberishPlaybackHandle.Invalid;
            }

            if (policy == GibberishPlaybackPolicy.Enqueue && IsSpeaking)
            {
                return Enqueue(handle, prepared)
                    ? handle
                    : GibberishPlaybackHandle.Invalid;
            }

            if (IsSpeaking)
            {
                InterruptCurrent();
            }

            BeginPlayback(handle, prepared);
            return handle;
        }

        public bool Cancel(GibberishPlaybackHandle handle)
        {
            if (!handle.IsValid)
            {
                return false;
            }

            if (_active != null && _active.Handle == handle)
            {
                FinishCurrent(GibberishStopReason.Cancelled, true);
                return true;
            }

            for (var index = 0; index < _queue.Count; index++)
            {
                if (_queue[index].Handle != handle)
                {
                    continue;
                }

                _queue.RemoveAt(index);
                Events.Publish(new GibberishSpeechStoppedEvent(
                    handle,
                    GibberishStopReason.Cancelled,
                    AudioSettings.dspTime));
                return true;
            }

            return false;
        }

        public void StopAll()
        {
            _queue.Clear();
            if (_active != null)
            {
                FinishCurrent(GibberishStopReason.Cancelled, false);
            }

            ReleaseFading();
            _streamingOutput?.Clear();
            ApplyDucking(false);
        }

        public void Pause()
        {
            if (_active == null || _active.IsPaused)
            {
                return;
            }

            _pauseDspTime = AudioSettings.dspTime;
            _active.IsPaused = true;
            _active.Source?.Pause();
            _streamingOutput?.Pause();
        }

        public void Resume()
        {
            if (_active == null || !_active.IsPaused)
            {
                return;
            }

            var offset = AudioSettings.dspTime - _pauseDspTime;
            _active.DspStartTime += offset;
            _active.DspEndTime += offset;
            _active.IsPaused = false;
            _active.Source?.UnPause();
            _streamingOutput?.Resume();
        }

        public bool Seek(float timeSeconds)
        {
            if (_active == null)
            {
                return false;
            }

            var duration = _active.Prepared.Audio.Duration;
            var targetTime = Mathf.Clamp(timeSeconds, 0f, duration);
            var now = AudioSettings.dspTime;
            var scheduleStart = now + ResolveScheduleLeadTime();
            _active.DspStartTime = scheduleStart - targetTime;
            _active.DspEndTime = _active.DspStartTime + duration;
            _active.NextSyllableIndex = FindNextSyllableIndex(
                _active.Prepared.Context.Utterance,
                targetTime);
            _active.LastWordIndex = -1;
            _active.LastPhraseIndex = -1;

            if (ResolveBackend() == GibberishPlaybackBackend.Streaming)
            {
                var sliced = SliceAudio(_active.Prepared.Audio, targetTime);
                _streamingOutput.Interrupt(
                    sliced,
                    scheduleStart,
                    ResolveCrossfadeDuration());
                return true;
            }

            if (_active.Source == null || _active.Clip == null)
            {
                return false;
            }

            var sample = Mathf.Clamp(
                Mathf.RoundToInt(targetTime * _active.Clip.frequency),
                0,
                Mathf.Max(0, _active.Clip.samples - 1));
            _active.Source.timeSamples = sample;
            if (!_active.Source.isPlaying)
            {
                _active.Source.PlayScheduled(scheduleStart);
            }

            return true;
        }

        public void ClearCache()
        {
            _cache?.Clear();
        }

        private void Awake()
        {
            EnsureOutputs();
            ConfigureCache();
            GetPlanner();
            GetSynthesizer();
        }

        private void Update()
        {
            UpdateCrossfade();
            if (_active == null || _active.IsPaused)
            {
                return;
            }

            var dspTime = AudioSettings.dspTime;
            DispatchDueSyllables(dspTime);
            if (dspTime >= _active.DspEndTime)
            {
                FinishCurrent(GibberishStopReason.Completed, true);
            }
        }

        private void OnDisable()
        {
            _queue.Clear();
            if (_active != null)
            {
                FinishCurrent(GibberishStopReason.Disabled, false);
            }

            ReleaseFading();
            _streamingOutput?.Clear();
        }

        private void OnDestroy()
        {
            ReleaseActiveMedia();
            ReleaseFading();
        }

        private void Reset()
        {
            _primarySource = GetComponent<AudioSource>();
            ConfigureSources();
        }

        private void DispatchDueSyllables(double currentDspTime)
        {
            var utterance = _active.Prepared.Context.Utterance;
            var dispatchUntil = currentDspTime + ResolveEventLookahead();

            for (var index = _active.NextSyllableIndex; index < utterance.SyllableCount; index++)
            {
                var syllable = utterance.Syllables[index];
                var syllableDspTime = _active.DspStartTime + syllable.StartTime;
                if (syllableDspTime > dispatchUntil)
                {
                    break;
                }

                if (syllable.PhraseIndex != _active.LastPhraseIndex)
                {
                    Events.Publish(new GibberishPhraseStartedEvent(
                        _active.Handle,
                        syllable.PhraseIndex,
                        syllable.SourceStart,
                        syllableDspTime));
                    _active.LastPhraseIndex = syllable.PhraseIndex;
                }

                if (syllable.WordIndex != _active.LastWordIndex)
                {
                    Events.Publish(new GibberishWordStartedEvent(
                        _active.Handle,
                        syllable.WordIndex,
                        syllable.SourceStart,
                        syllable.SourceLength,
                        syllableDspTime));
                    _active.LastWordIndex = syllable.WordIndex;
                }

                var normalizedPosition = utterance.Duration <= 0f
                    ? 0f
                    : Mathf.Clamp01(syllable.StartTime / utterance.Duration);
                Events.Publish(new GibberishSyllableStartedEvent(
                    _active.Handle,
                    syllable,
                    normalizedPosition,
                    syllableDspTime));
                Events.Publish(new GibberishMouthCueEvent(
                    _active.Handle,
                    syllable.MouthShape,
                    syllable.Intensity,
                    syllable.Duration,
                    syllableDspTime));
                _active.NextSyllableIndex = index + 1;
            }
        }

        private void BeginPlayback(
            GibberishPlaybackHandle handle,
            GibberishPreparedSpeech prepared)
        {
            var dspStart = AudioSettings.dspTime + ResolveScheduleLeadTime();
            var dspEnd = dspStart + prepared.Audio.Duration;

            if (ResolveBackend() == GibberishPlaybackBackend.Streaming)
            {
                EnsureStreamingOutput();
                var scheduled = _streamingOutput.Schedule(
                    prepared.Audio,
                    dspStart,
                    _fading == null ? 0f : ResolveCrossfadeDuration());
                if (!scheduled)
                {
                    _streamingOutput.Interrupt(
                        prepared.Audio,
                        dspStart,
                        ResolveCrossfadeDuration());
                }

                _active = new ActiveSpeech(
                    handle,
                    prepared,
                    null,
                    null,
                    dspStart,
                    dspEnd);
            }
            else
            {
                var source = SelectAvailableSource();
                var clipName = GeneratedClipPrefix + handle.Id;
                var clip = GibberishAudioClipFactory.Create(clipName, prepared.Audio);
                source.Stop();
                source.clip = clip;
                source.volume = _fading == null ? 1f : 0f;
                source.PlayScheduled(dspStart);
                _active = new ActiveSpeech(
                    handle,
                    prepared,
                    source,
                    clip,
                    dspStart,
                    dspEnd);
            }

            if (_fading != null)
            {
                _fading.CrossfadeStartTime = dspStart;
                _fading.CrossfadeEndTime = dspStart + ResolveCrossfadeDuration();
            }

            Events.Publish(new GibberishSpeechStartedEvent(
                handle,
                prepared.Context.Text,
                prepared.Context.Utterance.Duration,
                prepared.Context.Utterance.SyllableCount,
                dspStart));
            ApplyDucking(true);
        }

        private void InterruptCurrent()
        {
            if (_active == null)
            {
                return;
            }

            var interrupted = _active;
            _active = null;
            Events.Publish(new GibberishSpeechStoppedEvent(
                interrupted.Handle,
                GibberishStopReason.Interrupted,
                AudioSettings.dspTime));

            if (ResolveBackend() == GibberishPlaybackBackend.Streaming)
            {
                _streamingOutput?.Clear();
                return;
            }

            var crossfade = ResolveCrossfadeDuration();
            if (crossfade <= 0f || interrupted.Source == null)
            {
                ReleaseMedia(interrupted.Source, interrupted.Clip);
                return;
            }

            ReleaseFading();
            _fading = new FadingSpeech(interrupted.Source, interrupted.Clip);
        }

        private void FinishCurrent(
            GibberishStopReason reason,
            bool startNext)
        {
            if (_active == null)
            {
                return;
            }

            var completed = _active;
            _active = null;
            if (ResolveBackend() != GibberishPlaybackBackend.Streaming)
            {
                ReleaseMedia(completed.Source, completed.Clip);
            }

            Events.Publish(new GibberishSpeechStoppedEvent(
                completed.Handle,
                reason,
                AudioSettings.dspTime));

            if (startNext)
            {
                TryStartNext();
                if (_active == null)
                {
                    ApplyDucking(false);
                }
            }
            else if (_queue.Count == 0)
            {
                ApplyDucking(false);
            }
        }

        private void UpdateCrossfade()
        {
            if (_fading == null)
            {
                return;
            }

            var now = AudioSettings.dspTime;
            if (now < _fading.CrossfadeStartTime)
            {
                _fading.Source.volume = 1f;
                if (_active?.Source != null)
                {
                    _active.Source.volume = 0f;
                }

                return;
            }

            var duration = Math.Max(
                0.0001d,
                _fading.CrossfadeEndTime - _fading.CrossfadeStartTime);
            var progress = Mathf.Clamp01(
                (float)((now - _fading.CrossfadeStartTime) / duration));
            _fading.Source.volume = Mathf.Cos(progress * Mathf.PI * 0.5f);
            if (_active?.Source != null)
            {
                _active.Source.volume = Mathf.Sin(progress * Mathf.PI * 0.5f);
            }

            if (progress >= 1f)
            {
                ReleaseFading();
                if (_active?.Source != null)
                {
                    _active.Source.volume = 1f;
                }
            }
        }

        private void TryStartNext()
        {
            if (_queue.Count == 0 || !isActiveAndEnabled)
            {
                return;
            }

            var next = _queue[0];
            _queue.RemoveAt(0);
            BeginPlayback(next.Handle, next.Prepared);
        }

        private bool Enqueue(
            GibberishPlaybackHandle handle,
            GibberishPreparedSpeech prepared)
        {
            if (_queue.Count >= ResolveQueueCapacity() &&
                !MakeQueueSpace(prepared.Context.Priority))
            {
                Events.Publish(new GibberishSpeechStoppedEvent(
                    handle,
                    GibberishStopReason.Cancelled,
                    AudioSettings.dspTime));
                return false;
            }

            var queued = new QueuedSpeech(handle, prepared, _queueSequence++);
            var insertIndex = _queue.Count;

            for (var index = 0; index < _queue.Count; index++)
            {
                var existing = _queue[index];
                if (queued.Priority > existing.Priority ||
                    (queued.Priority == existing.Priority && queued.Sequence < existing.Sequence))
                {
                    insertIndex = index;
                    break;
                }
            }

            _queue.Insert(insertIndex, queued);
            return true;
        }

        private bool MakeQueueSpace(int incomingPriority)
        {
            switch (ResolveOverflowPolicy())
            {
                case GibberishQueueOverflowPolicy.RejectNewest:
                    return false;
                case GibberishQueueOverflowPolicy.DropOldest:
                    var oldestIndex = FindOldestQueueIndex();
                    CancelQueuedAt(oldestIndex);
                    return true;
                default:
                    var lowestIndex = FindLowestPriorityQueueIndex();
                    if (lowestIndex < 0 || _queue[lowestIndex].Priority >= incomingPriority)
                    {
                        return false;
                    }

                    CancelQueuedAt(lowestIndex);
                    return true;
            }
        }

        private int FindOldestQueueIndex()
        {
            var result = -1;
            var sequence = long.MaxValue;
            for (var index = 0; index < _queue.Count; index++)
            {
                if (_queue[index].Sequence < sequence)
                {
                    sequence = _queue[index].Sequence;
                    result = index;
                }
            }

            return result;
        }

        private int FindLowestPriorityQueueIndex()
        {
            var result = -1;
            var priority = int.MaxValue;
            for (var index = 0; index < _queue.Count; index++)
            {
                if (_queue[index].Priority < priority)
                {
                    priority = _queue[index].Priority;
                    result = index;
                }
            }

            return result;
        }

        private void CancelQueuedAt(int index)
        {
            if (index < 0 || index >= _queue.Count)
            {
                return;
            }

            var handle = _queue[index].Handle;
            _queue.RemoveAt(index);
            Events.Publish(new GibberishSpeechStoppedEvent(
                handle,
                GibberishStopReason.Cancelled,
                AudioSettings.dspTime));
        }

        private void EnsureOutputs()
        {
            if (_primarySource == null)
            {
                _primarySource = GetComponent<AudioSource>();
            }

            if (_primarySource == null)
            {
                _primarySource = gameObject.AddComponent<AudioSource>();
            }

            if (_secondarySource == null || _secondarySource == _primarySource)
            {
                var sources = GetComponents<AudioSource>();
                for (var index = 0; index < sources.Length; index++)
                {
                    if (sources[index] != _primarySource)
                    {
                        _secondarySource = sources[index];
                        break;
                    }
                }
            }

            if (_secondarySource == null || _secondarySource == _primarySource)
            {
                _secondarySource = gameObject.AddComponent<AudioSource>();
            }

            ConfigureSources();
            if (ResolveBackend() == GibberishPlaybackBackend.Streaming)
            {
                EnsureStreamingOutput();
            }
        }

        private void EnsureStreamingOutput()
        {
            if (_streamingOutput == null)
            {
                _streamingOutput = GetComponent<GibberishStreamingAudioOutput>();
            }

            if (_streamingOutput == null)
            {
                _streamingOutput = gameObject.AddComponent<GibberishStreamingAudioOutput>();
            }

            _streamingOutput.Initialize(_primarySource);
        }

        private void ConfigureSources()
        {
            ConfigureSource(_primarySource);
            ConfigureSource(_secondarySource);
        }

        private void ConfigureSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = _outputProfile == null
                ? _legacySpatialBlend
                : _outputProfile.SpatialBlend;

            if (_outputProfile == null)
            {
                return;
            }

            source.outputAudioMixerGroup = _outputProfile.MixerGroup;
            source.reverbZoneMix = _outputProfile.ReverbZoneMix;
            source.spread = _outputProfile.Spread;
            source.dopplerLevel = _outputProfile.DopplerLevel;
            source.minDistance = _outputProfile.MinimumDistance;
            source.maxDistance = _outputProfile.MaximumDistance;
            source.rolloffMode = _outputProfile.RolloffMode;
        }

        private void ApplyDucking(bool speaking)
        {
            if (_outputProfile == null || !_outputProfile.EnableDucking)
            {
                return;
            }

            var value = speaking
                ? _outputProfile.SpeakingDuckingDb
                : _outputProfile.NormalDuckingDb;
            _outputProfile.MixerGroup.audioMixer.SetFloat(
                _outputProfile.DuckingParameter,
                value);
        }

        private AudioSource SelectAvailableSource()
        {
            if (_active?.Source == _primarySource || _fading?.Source == _primarySource)
            {
                return _secondarySource;
            }

            return _primarySource;
        }

        private IGibberishUtterancePlanner GetPlanner()
        {
            return _planner ?? (_planner = new GibberishUtterancePlanner());
        }

        private IGibberishSynthesizer GetSynthesizer()
        {
            return _synthesizer ?? (_synthesizer = new ProceduralGibberishSynthesizer());
        }

        private GibberishPreparedSpeechCache GetCache()
        {
            if (_cache == null)
            {
                ConfigureCache();
            }

            return _cache;
        }

        private void ConfigureCache()
        {
            var entries = _outputProfile == null
                ? DefaultCacheEntries
                : _outputProfile.CacheEntryCapacity;
            var memory = _outputProfile == null
                ? DefaultCacheMemory
                : _outputProfile.CacheMemoryBytes;

            if (_cache == null)
            {
                _cache = new GibberishPreparedSpeechCache(entries, memory);
            }
            else
            {
                _cache.SetLimits(entries, memory);
            }
        }

        private void ReleaseActiveMedia()
        {
            if (_active == null)
            {
                return;
            }

            ReleaseMedia(_active.Source, _active.Clip);
            _active = null;
        }

        private void ReleaseFading()
        {
            if (_fading == null)
            {
                return;
            }

            ReleaseMedia(_fading.Source, _fading.Clip);
            _fading = null;
        }

        private void ReleaseMedia(AudioSource source, AudioClip clip)
        {
            if (source != null)
            {
                source.Stop();
                source.clip = null;
                source.volume = 1f;
            }

            if (clip != null)
            {
                Destroy(clip);
            }
        }

        private GibberishPlaybackBackend ResolveBackend()
        {
            return _outputProfile == null
                ? GibberishPlaybackBackend.AudioClip
                : _outputProfile.Backend;
        }

        private GibberishQualityTier ResolveQualityTier()
        {
            return _outputProfile == null
                ? GibberishQualityTier.Balanced
                : _outputProfile.QualityTier;
        }

        private float ResolveCrossfadeDuration()
        {
            return _outputProfile == null
                ? DefaultCrossfadeDuration
                : _outputProfile.CrossfadeDuration;
        }

        private float ResolveScheduleLeadTime()
        {
            return _outputProfile == null
                ? DefaultScheduleLeadTime
                : _outputProfile.ScheduleLeadTime;
        }

        private float ResolveEventLookahead()
        {
            return _outputProfile == null
                ? DefaultEventLookahead
                : _outputProfile.EventLookahead;
        }

        private int ResolveQueueCapacity()
        {
            return _outputProfile == null
                ? DefaultQueueCapacity
                : _outputProfile.QueueCapacity;
        }

        private GibberishQueueOverflowPolicy ResolveOverflowPolicy()
        {
            return _outputProfile == null
                ? GibberishQueueOverflowPolicy.DropLowestPriority
                : _outputProfile.OverflowPolicy;
        }

        private static int FindNextSyllableIndex(
            GibberishUtterance utterance,
            float timeSeconds)
        {
            for (var index = 0; index < utterance.SyllableCount; index++)
            {
                if (utterance.Syllables[index].StartTime >= timeSeconds)
                {
                    return index;
                }
            }

            return utterance.SyllableCount;
        }

        private static GibberishAudioData SliceAudio(
            GibberishAudioData source,
            float startTime)
        {
            var startSample = Mathf.Clamp(
                Mathf.RoundToInt(startTime * source.SampleRate),
                0,
                source.Samples.Length);
            var result = new float[source.Samples.Length - startSample];
            Array.Copy(source.Samples, startSample, result, 0, result.Length);
            return new GibberishAudioData(result, source.SampleRate);
        }

        private static GibberishPlaybackHandle CreateHandle()
        {
            return new GibberishPlaybackHandle(Interlocked.Increment(ref _nextHandleId));
        }

        private static string SanitizeText(string text)
        {
            var source = text ?? string.Empty;
            return source.Length <= MaximumTextLength
                ? source
                : source.Substring(0, MaximumTextLength);
        }

        private sealed class ActiveSpeech
        {
            public GibberishPlaybackHandle Handle { get; }
            public GibberishPreparedSpeech Prepared { get; }
            public AudioSource Source { get; }
            public AudioClip Clip { get; }
            public double DspStartTime { get; set; }
            public double DspEndTime { get; set; }
            public int NextSyllableIndex { get; set; }
            public int LastWordIndex { get; set; } = -1;
            public int LastPhraseIndex { get; set; } = -1;
            public bool IsPaused { get; set; }

            public ActiveSpeech(
                GibberishPlaybackHandle handle,
                GibberishPreparedSpeech prepared,
                AudioSource source,
                AudioClip clip,
                double dspStartTime,
                double dspEndTime)
            {
                Handle = handle;
                Prepared = prepared;
                Source = source;
                Clip = clip;
                DspStartTime = dspStartTime;
                DspEndTime = dspEndTime;
            }
        }

        private sealed class FadingSpeech
        {
            public AudioSource Source { get; }
            public AudioClip Clip { get; }
            public double CrossfadeStartTime { get; set; } = double.MaxValue;
            public double CrossfadeEndTime { get; set; } = double.MaxValue;

            public FadingSpeech(AudioSource source, AudioClip clip)
            {
                Source = source;
                Clip = clip;
            }
        }

        private readonly struct QueuedSpeech
        {
            public GibberishPlaybackHandle Handle { get; }
            public GibberishPreparedSpeech Prepared { get; }
            public long Sequence { get; }
            public int Priority => Prepared.Context.Priority;

            public QueuedSpeech(
                GibberishPlaybackHandle handle,
                GibberishPreparedSpeech prepared,
                long sequence)
            {
                Handle = handle;
                Prepared = prepared;
                Sequence = sequence;
            }
        }
    }
}
