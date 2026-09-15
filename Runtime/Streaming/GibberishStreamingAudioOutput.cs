using System;
using System.Threading;
using UnityEngine;

namespace CozyGibberish
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class GibberishStreamingAudioOutput : MonoBehaviour
    {
        private const string StreamingClipName = "CozyGibberish_Streaming";
        private const int MinimumBufferSeconds = 2;

        [SerializeField, Range(2, 30)] private int _bufferSeconds = 8;

        private AudioSource _source;
        private AudioClip _streamingClip;
        private SingleProducerSingleConsumerBuffer _buffer;
        private int _sampleRate;
        private volatile bool _paused;

        public int BufferedSamples => _buffer == null ? 0 : _buffer.Available;
        public float BufferedDuration => _sampleRate <= 0 ? 0f : BufferedSamples / (float)_sampleRate;

        public void Initialize(AudioSource source = null)
        {
            if (_streamingClip != null)
            {
                _source = source != null ? source : _source;
                if (_source != null)
                {
                    _source.loop = true;
                    if (!_source.isPlaying)
                    {
                        _source.Play();
                    }
                }

                return;
            }

            _source = source != null ? source : GetComponent<AudioSource>();
            _sampleRate = AudioSettings.outputSampleRate;
            var capacity = Mathf.Max(
                _sampleRate * MinimumBufferSeconds,
                _sampleRate * _bufferSeconds);
            _buffer = new SingleProducerSingleConsumerBuffer(capacity);
            _streamingClip = AudioClip.Create(
                StreamingClipName,
                _sampleRate,
                1,
                _sampleRate,
                true,
                ReadAudio);
            _source.clip = _streamingClip;
            _source.loop = true;
            _source.playOnAwake = false;
            _source.Play();
        }

        public bool Schedule(
            GibberishAudioData audio,
            double dspStartTime,
            float crossfadeDuration)
        {
            if (audio == null)
            {
                throw new ArgumentNullException(nameof(audio));
            }

            Initialize();
            var samples = Resample(audio.Samples, audio.SampleRate, _sampleRate);
            var leadSamples = Mathf.Max(
                0,
                Mathf.RoundToInt((float)(dspStartTime - AudioSettings.dspTime) * _sampleRate));
            var required = leadSamples + samples.Length;
            if (_buffer.Free < required)
            {
                return false;
            }

            _buffer.WriteSilence(leadSamples);
            ApplyFadeIn(samples, crossfadeDuration, _sampleRate, _buffer.LastReadSample);
            return _buffer.Write(samples);
        }

        public void Interrupt(
            GibberishAudioData audio,
            double dspStartTime,
            float crossfadeDuration)
        {
            Initialize();
            _buffer.Clear();
            Schedule(audio, dspStartTime, crossfadeDuration);
        }

        public void Pause()
        {
            _paused = true;
        }

        public void Resume()
        {
            _paused = false;
        }

        public void Clear()
        {
            _buffer?.Clear();
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            if (_streamingClip != null)
            {
                Destroy(_streamingClip);
            }
        }

        private void ReadAudio(float[] data)
        {
            if (_paused || _buffer == null)
            {
                Array.Clear(data, 0, data.Length);
                return;
            }

            _buffer.Read(data);
        }

        private static float[] Resample(float[] source, int sourceRate, int destinationRate)
        {
            if (source == null || source.Length == 0)
            {
                return Array.Empty<float>();
            }

            if (sourceRate == destinationRate)
            {
                var copy = new float[source.Length];
                Array.Copy(source, copy, source.Length);
                return copy;
            }

            var ratio = sourceRate / (float)destinationRate;
            var outputLength = Mathf.Max(1, Mathf.CeilToInt(source.Length / ratio));
            var result = new float[outputLength];
            for (var index = 0; index < result.Length; index++)
            {
                var sourcePosition = index * ratio;
                var left = Mathf.Clamp(Mathf.FloorToInt(sourcePosition), 0, source.Length - 1);
                var right = Mathf.Min(left + 1, source.Length - 1);
                result[index] = Mathf.Lerp(
                    source[left],
                    source[right],
                    sourcePosition - left);
            }

            return result;
        }

        private static void ApplyFadeIn(
            float[] samples,
            float duration,
            int sampleRate,
            float previousSample)
        {
            var fadeSamples = Mathf.Min(
                samples.Length,
                Mathf.RoundToInt(Mathf.Max(0f, duration) * sampleRate));
            for (var index = 0; index < fadeSamples; index++)
            {
                var progress = fadeSamples <= 1 ? 1f : index / (float)(fadeSamples - 1);
                var equalPower = Mathf.Sin(progress * Mathf.PI * 0.5f);
                samples[index] = Mathf.Lerp(previousSample, samples[index], equalPower);
            }
        }

        private sealed class SingleProducerSingleConsumerBuffer
        {
            private readonly float[] _samples;
            private long _readPosition;
            private long _writePosition;
            private float _lastReadSample;

            public int Available
            {
                get
                {
                    var write = Volatile.Read(ref _writePosition);
                    var read = Volatile.Read(ref _readPosition);
                    return (int)Math.Min(_samples.Length, Math.Max(0L, write - read));
                }
            }

            public int Free => _samples.Length - Available;
            public float LastReadSample => _lastReadSample;

            public SingleProducerSingleConsumerBuffer(int capacity)
            {
                _samples = new float[Math.Max(1, capacity)];
            }

            public bool Write(float[] source)
            {
                if (source == null || source.Length > Free)
                {
                    return false;
                }

                var write = Volatile.Read(ref _writePosition);
                for (var index = 0; index < source.Length; index++)
                {
                    _samples[(int)((write + index) % _samples.Length)] = source[index];
                }

                Volatile.Write(ref _writePosition, write + source.Length);
                return true;
            }

            public bool WriteSilence(int count)
            {
                if (count < 0 || count > Free)
                {
                    return false;
                }

                var write = Volatile.Read(ref _writePosition);
                for (var index = 0; index < count; index++)
                {
                    _samples[(int)((write + index) % _samples.Length)] = 0f;
                }

                Volatile.Write(ref _writePosition, write + count);
                return true;
            }

            public void Read(float[] destination)
            {
                var read = Volatile.Read(ref _readPosition);
                var write = Volatile.Read(ref _writePosition);
                var available = (int)Math.Min(destination.Length, Math.Max(0L, write - read));

                for (var index = 0; index < available; index++)
                {
                    var sample = _samples[(int)((read + index) % _samples.Length)];
                    destination[index] = sample;
                    _lastReadSample = sample;
                }

                if (available < destination.Length)
                {
                    Array.Clear(destination, available, destination.Length - available);
                }

                Volatile.Write(ref _readPosition, read + available);
            }

            public void Clear()
            {
                var write = Volatile.Read(ref _writePosition);
                Volatile.Write(ref _readPosition, write);
            }
        }
    }
}
