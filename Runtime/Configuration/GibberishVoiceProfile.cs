using System;
using System.Collections.Generic;
using UnityEngine;

namespace CozyGibberish
{
    /// <summary>
    /// Defines a character's persistent vocal identity and produces thread-safe runtime snapshots.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GibberishVoice",
        menuName = "Cozy Gibberish/Voice Profile",
        order = 10)]
    public sealed class GibberishVoiceProfile : ScriptableObject, ISerializationCallbackReceiver
    {
        private const int DefaultSampleRate = 48000;
        private const int CurrentSchemaVersion = 2;
        private const string StableIdFormat = "N";

        [SerializeField, HideInInspector] private int _schemaVersion = CurrentSchemaVersion;
        [SerializeField, HideInInspector] private string _stableId;

        [Header("Identity")]
        [SerializeField] private GibberishWaveform _waveform = GibberishWaveform.SoftTriangle;
        [SerializeField, Range(55f, 880f)] private float _basePitch = 185f;
        [SerializeField] private GibberishFloatRange _pitchVariationSemitones = new GibberishFloatRange(-2.5f, 2.5f);
        [SerializeField, Range(0f, 1f)] private float _warmth = 0.78f;
        [SerializeField, Range(0f, 1f)] private float _brightness = 0.3f;
        [SerializeField, Range(0f, 1f)] private float _breathiness = 0.08f;

        [Header("Rhythm")]
        [SerializeField, Range(1f, 8f)] private float _charactersPerSyllable = 3.2f;
        [SerializeField] private GibberishFloatRange _syllableDuration = new GibberishFloatRange(0.105f, 0.185f);
        [SerializeField, Range(0f, 0.2f)] private float _syllableGap = 0.018f;
        [SerializeField, Range(0f, 0.5f)] private float _wordPause = 0.055f;
        [SerializeField, Range(0f, 1.5f)] private float _sentencePause = 0.28f;
        [SerializeField, Range(0f, 1f)] private float _timingVariation = 0.12f;

        [Header("Articulation")]
        [SerializeField, Range(0.001f, 0.1f)] private float _attack = 0.018f;
        [SerializeField, Range(0.001f, 0.2f)] private float _release = 0.055f;
        [SerializeField, Range(0f, 1f)] private float _consonantStrength = 0.16f;
        [SerializeField, Range(0f, 12f)] private float _vibratoRate = 4.2f;
        [SerializeField, Range(0f, 2f)] private float _vibratoDepthSemitones = 0.16f;
        [SerializeField, Range(0f, 1f)] private float _phraseCadence = 0.36f;

        [Header("Output")]
        [SerializeField, Range(0f, 1f)] private float _volume = 0.72f;
        [SerializeField, Range(22050, 48000)] private int _sampleRate = DefaultSampleRate;
        [SerializeField] private GibberishOversampling _oversampling = GibberishOversampling.TwoTimes;
        [SerializeField, Min(8)] private int _maximumSyllables = 128;

        [Header("Vowel Palette")]
        [SerializeField] private GibberishVowelDefinition[] _vowels = CreateCozyVowels();

        public string StableId => _stableId;
        public int SchemaVersion => _schemaVersion;

        public GibberishVoiceSnapshot CreateSnapshot(
            GibberishStylePreset style = null,
            GibberishExpression? expression = null)
        {
            return CreateSnapshot(
                GibberishStyleInfluence.From(style),
                expression,
                GibberishProsodySnapshot.Neutral);
        }

        public GibberishVoiceSnapshot CreateSnapshot(
            GibberishStyleInfluence style,
            GibberishExpression? expression = null,
            GibberishProsodySnapshot prosody = null)
        {
            var resolvedExpression = expression ?? GibberishExpression.Neutral;
            var pace = style.PaceMultiplier;
            pace *= Mathf.Lerp(0.75f, 1.3f, (resolvedExpression.Pace + 1f) * 0.5f);

            var pitchOffset = style.PitchOffsetSemitones;
            pitchOffset += resolvedExpression.Pitch * 5f;

            var pitchVariation = style.PitchVariationMultiplier;
            var articulation = style.ArticulationMultiplier;
            var warmth = Mathf.Clamp01(_warmth + style.WarmthOffset + resolvedExpression.Warmth * 0.25f);
            var brightness = Mathf.Clamp01(_brightness + style.BrightnessOffset + resolvedExpression.Energy * 0.18f);
            var breathiness = Mathf.Clamp01(_breathiness + style.BreathinessOffset);
            var formantShift = style.FormantShift;
            var vibrato = style.VibratoMultiplier;
            var volume = style.VolumeMultiplier;
            var variation = style.VariationMultiplier;
            var energy = Mathf.Lerp(0.82f, 1.18f, (resolvedExpression.Energy + 1f) * 0.5f);
            var vowels = CopyVowels(_vowels);

            return new GibberishVoiceSnapshot(
                _waveform,
                SemitoneToFrequency(_basePitch, pitchOffset),
                new GibberishFloatRange(
                    _pitchVariationSemitones.Minimum * pitchVariation * variation,
                    _pitchVariationSemitones.Maximum * pitchVariation * variation),
                warmth,
                brightness,
                breathiness,
                Mathf.Max(1f, _charactersPerSyllable / articulation),
                new GibberishFloatRange(
                    _syllableDuration.Minimum / pace,
                    _syllableDuration.Maximum / pace),
                _syllableGap / pace,
                _wordPause / pace,
                _sentencePause / pace,
                Mathf.Clamp01(_timingVariation * variation),
                _attack / Mathf.Max(0.5f, articulation),
                _release / Mathf.Max(0.5f, articulation),
                Mathf.Clamp01(_consonantStrength * articulation),
                _vibratoRate,
                _vibratoDepthSemitones * vibrato,
                _phraseCadence,
                Mathf.Clamp01(_volume * volume * energy),
                _sampleRate,
                (int)_oversampling,
                Mathf.Max(8, _maximumSyllables),
                formantShift,
                vowels,
                prosody ?? GibberishProsodySnapshot.Neutral);
        }

        public void ApplyArchetype(GibberishVoiceArchetype archetype)
        {
            switch (archetype)
            {
                case GibberishVoiceArchetype.Cozy:
                    ApplyCozy();
                    break;
                case GibberishVoiceArchetype.Whimsical:
                    ApplyWhimsical();
                    break;
                case GibberishVoiceArchetype.Mechanical:
                    ApplyMechanical();
                    break;
                case GibberishVoiceArchetype.Mystic:
                    ApplyMystic();
                    break;
            }
        }

        private void ApplyCozy()
        {
            _waveform = GibberishWaveform.SoftTriangle;
            _basePitch = 185f;
            _pitchVariationSemitones = new GibberishFloatRange(-2.5f, 2.5f);
            _warmth = 0.78f;
            _brightness = 0.3f;
            _breathiness = 0.08f;
            _charactersPerSyllable = 3.2f;
            _syllableDuration = new GibberishFloatRange(0.105f, 0.185f);
            _timingVariation = 0.12f;
            _consonantStrength = 0.16f;
            _vibratoRate = 4.2f;
            _vibratoDepthSemitones = 0.16f;
            _phraseCadence = 0.36f;
            _volume = 0.72f;
            _oversampling = GibberishOversampling.TwoTimes;
            _vowels = CreateCozyVowels();
        }

        private void ApplyWhimsical()
        {
            ApplyCozy();
            _waveform = GibberishWaveform.Sine;
            _basePitch = 285f;
            _pitchVariationSemitones = new GibberishFloatRange(-4f, 5.5f);
            _warmth = 0.52f;
            _brightness = 0.55f;
            _charactersPerSyllable = 2.6f;
            _syllableDuration = new GibberishFloatRange(0.08f, 0.145f);
            _consonantStrength = 0.22f;
            _vibratoRate = 5.8f;
            _vibratoDepthSemitones = 0.34f;
            _phraseCadence = 0.52f;
        }

        private void ApplyMechanical()
        {
            ApplyCozy();
            _waveform = GibberishWaveform.RoundedSquare;
            _basePitch = 120f;
            _pitchVariationSemitones = new GibberishFloatRange(-0.5f, 0.5f);
            _warmth = 0.18f;
            _brightness = 0.72f;
            _breathiness = 0.02f;
            _charactersPerSyllable = 2.4f;
            _syllableDuration = new GibberishFloatRange(0.07f, 0.115f);
            _timingVariation = 0.03f;
            _consonantStrength = 0.38f;
            _vibratoRate = 7.5f;
            _vibratoDepthSemitones = 0.04f;
            _phraseCadence = 0.08f;
        }

        private void ApplyMystic()
        {
            ApplyCozy();
            _waveform = GibberishWaveform.WarmSaw;
            _basePitch = 105f;
            _pitchVariationSemitones = new GibberishFloatRange(-4.5f, 2f);
            _warmth = 0.64f;
            _brightness = 0.22f;
            _breathiness = 0.24f;
            _charactersPerSyllable = 3.8f;
            _syllableDuration = new GibberishFloatRange(0.14f, 0.25f);
            _timingVariation = 0.18f;
            _consonantStrength = 0.12f;
            _vibratoRate = 3.1f;
            _vibratoDepthSemitones = 0.42f;
            _phraseCadence = 0.58f;
        }

        private void OnValidate()
        {
            ValidateAndMigrate();
            _basePitch = Mathf.Clamp(_basePitch, 55f, 880f);
            _pitchVariationSemitones = _pitchVariationSemitones.Clamped(-24f, 24f);
            _syllableDuration = _syllableDuration.Clamped(0.03f, 1f);
            _sampleRate = Mathf.Clamp(_sampleRate, 22050, DefaultSampleRate);
            _maximumSyllables = Mathf.Max(8, _maximumSyllables);

            if (_vowels == null || _vowels.Length == 0)
            {
                _vowels = CreateCozyVowels();
            }
        }

        public bool ValidateAndMigrate()
        {
            var changed = false;
            if (string.IsNullOrWhiteSpace(_stableId))
            {
                _stableId = Guid.NewGuid().ToString(StableIdFormat);
                changed = true;
            }

            if (_schemaVersion != CurrentSchemaVersion)
            {
                _schemaVersion = CurrentSchemaVersion;
                changed = true;
            }

            return changed;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (_schemaVersion < CurrentSchemaVersion)
            {
                _schemaVersion = CurrentSchemaVersion;
            }
        }

        private static GibberishVowelDefinition[] CopyVowels(GibberishVowelDefinition[] source)
        {
            var fallback = source == null || source.Length == 0 ? CreateCozyVowels() : source;
            var result = new GibberishVowelDefinition[fallback.Length];
            Array.Copy(fallback, result, fallback.Length);
            return result;
        }

        private static float SemitoneToFrequency(float frequency, float semitones)
        {
            return frequency * Mathf.Pow(2f, semitones / 12f);
        }

        private static GibberishVowelDefinition[] CreateCozyVowels()
        {
            return new[]
            {
                new GibberishVowelDefinition(GibberishVowelId.Ah, 1.1f, 700f, 1220f, 2600f),
                new GibberishVowelDefinition(GibberishVowelId.Eh, 0.9f, 530f, 1840f, 2480f),
                new GibberishVowelDefinition(GibberishVowelId.Ee, 0.75f, 300f, 2300f, 3000f),
                new GibberishVowelDefinition(GibberishVowelId.Oh, 1f, 500f, 900f, 2450f),
                new GibberishVowelDefinition(GibberishVowelId.Oo, 0.85f, 350f, 800f, 2200f)
            };
        }
    }

    public sealed class GibberishVoiceSnapshot
    {
        public GibberishWaveform Waveform { get; }
        public float BasePitch { get; }
        public GibberishFloatRange PitchVariationSemitones { get; }
        public float Warmth { get; }
        public float Brightness { get; }
        public float Breathiness { get; }
        public float CharactersPerSyllable { get; }
        public GibberishFloatRange SyllableDuration { get; }
        public float SyllableGap { get; }
        public float WordPause { get; }
        public float SentencePause { get; }
        public float TimingVariation { get; }
        public float Attack { get; }
        public float Release { get; }
        public float ConsonantStrength { get; }
        public float VibratoRate { get; }
        public float VibratoDepthSemitones { get; }
        public float PhraseCadence { get; }
        public float Volume { get; }
        public int SampleRate { get; }
        public int Oversampling { get; }
        public int MaximumSyllables { get; }
        public float FormantShift { get; }
        public IReadOnlyList<GibberishVowelDefinition> Vowels { get; }
        public GibberishProsodySnapshot Prosody { get; }

        public GibberishVoiceSnapshot(
            GibberishWaveform waveform,
            float basePitch,
            GibberishFloatRange pitchVariationSemitones,
            float warmth,
            float brightness,
            float breathiness,
            float charactersPerSyllable,
            GibberishFloatRange syllableDuration,
            float syllableGap,
            float wordPause,
            float sentencePause,
            float timingVariation,
            float attack,
            float release,
            float consonantStrength,
            float vibratoRate,
            float vibratoDepthSemitones,
            float phraseCadence,
            float volume,
            int sampleRate,
            int oversampling,
            int maximumSyllables,
            float formantShift,
            GibberishVowelDefinition[] vowels,
            GibberishProsodySnapshot prosody = null)
        {
            Waveform = waveform;
            BasePitch = basePitch;
            PitchVariationSemitones = pitchVariationSemitones;
            Warmth = warmth;
            Brightness = brightness;
            Breathiness = breathiness;
            CharactersPerSyllable = charactersPerSyllable;
            SyllableDuration = syllableDuration;
            SyllableGap = syllableGap;
            WordPause = wordPause;
            SentencePause = sentencePause;
            TimingVariation = timingVariation;
            Attack = attack;
            Release = release;
            ConsonantStrength = consonantStrength;
            VibratoRate = vibratoRate;
            VibratoDepthSemitones = vibratoDepthSemitones;
            PhraseCadence = phraseCadence;
            Volume = volume;
            SampleRate = sampleRate;
            Oversampling = oversampling;
            MaximumSyllables = maximumSyllables;
            FormantShift = formantShift;
            var safeVowels = vowels ?? Array.Empty<GibberishVowelDefinition>();
            Vowels = Array.AsReadOnly(safeVowels);
            Prosody = prosody ?? GibberishProsodySnapshot.Neutral;
        }

        public GibberishVoiceSnapshot WithQuality(GibberishQualityTier qualityTier)
        {
            var sampleRate = SampleRate;
            var oversampling = Oversampling;
            switch (qualityTier)
            {
                case GibberishQualityTier.Mobile:
                    sampleRate = Mathf.Min(sampleRate, 32000);
                    oversampling = 1;
                    break;
                case GibberishQualityTier.Cinematic:
                    sampleRate = Mathf.Max(sampleRate, 48000);
                    oversampling = Mathf.Max(oversampling, 2);
                    break;
            }

            return Copy(sampleRate, oversampling);
        }

        public static GibberishVoiceSnapshot Blend(
            GibberishVoiceSnapshot left,
            GibberishVoiceSnapshot right,
            float weight)
        {
            if (left == null)
            {
                return right;
            }

            if (right == null)
            {
                return left;
            }

            var amount = Mathf.Clamp01(weight);
            var selectedVowels = amount < 0.5f
                ? CopyVowels(left.Vowels)
                : CopyVowels(right.Vowels);

            return new GibberishVoiceSnapshot(
                amount < 0.5f ? left.Waveform : right.Waveform,
                Mathf.Lerp(left.BasePitch, right.BasePitch, amount),
                new GibberishFloatRange(
                    Mathf.Lerp(left.PitchVariationSemitones.Minimum, right.PitchVariationSemitones.Minimum, amount),
                    Mathf.Lerp(left.PitchVariationSemitones.Maximum, right.PitchVariationSemitones.Maximum, amount)),
                Mathf.Lerp(left.Warmth, right.Warmth, amount),
                Mathf.Lerp(left.Brightness, right.Brightness, amount),
                Mathf.Lerp(left.Breathiness, right.Breathiness, amount),
                Mathf.Lerp(left.CharactersPerSyllable, right.CharactersPerSyllable, amount),
                new GibberishFloatRange(
                    Mathf.Lerp(left.SyllableDuration.Minimum, right.SyllableDuration.Minimum, amount),
                    Mathf.Lerp(left.SyllableDuration.Maximum, right.SyllableDuration.Maximum, amount)),
                Mathf.Lerp(left.SyllableGap, right.SyllableGap, amount),
                Mathf.Lerp(left.WordPause, right.WordPause, amount),
                Mathf.Lerp(left.SentencePause, right.SentencePause, amount),
                Mathf.Lerp(left.TimingVariation, right.TimingVariation, amount),
                Mathf.Lerp(left.Attack, right.Attack, amount),
                Mathf.Lerp(left.Release, right.Release, amount),
                Mathf.Lerp(left.ConsonantStrength, right.ConsonantStrength, amount),
                Mathf.Lerp(left.VibratoRate, right.VibratoRate, amount),
                Mathf.Lerp(left.VibratoDepthSemitones, right.VibratoDepthSemitones, amount),
                Mathf.Lerp(left.PhraseCadence, right.PhraseCadence, amount),
                Mathf.Lerp(left.Volume, right.Volume, amount),
                amount < 0.5f ? left.SampleRate : right.SampleRate,
                amount < 0.5f ? left.Oversampling : right.Oversampling,
                Mathf.RoundToInt(Mathf.Lerp(left.MaximumSyllables, right.MaximumSyllables, amount)),
                Mathf.Lerp(left.FormantShift, right.FormantShift, amount),
                selectedVowels,
                amount < 0.5f ? left.Prosody : right.Prosody);
        }

        private GibberishVoiceSnapshot Copy(int sampleRate, int oversampling)
        {
            return new GibberishVoiceSnapshot(
                Waveform,
                BasePitch,
                PitchVariationSemitones,
                Warmth,
                Brightness,
                Breathiness,
                CharactersPerSyllable,
                SyllableDuration,
                SyllableGap,
                WordPause,
                SentencePause,
                TimingVariation,
                Attack,
                Release,
                ConsonantStrength,
                VibratoRate,
                VibratoDepthSemitones,
                PhraseCadence,
                Volume,
                sampleRate,
                oversampling,
                MaximumSyllables,
                FormantShift,
                CopyVowels(Vowels),
                Prosody);
        }

        private static GibberishVowelDefinition[] CopyVowels(
            IReadOnlyList<GibberishVowelDefinition> source)
        {
            var result = new GibberishVowelDefinition[source.Count];
            for (var index = 0; index < source.Count; index++)
            {
                result[index] = source[index];
            }

            return result;
        }
    }
}
