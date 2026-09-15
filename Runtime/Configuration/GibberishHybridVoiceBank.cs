using System;
using System.Collections.Generic;
using UnityEngine;

namespace CozyGibberish
{
    [Serializable]
    public struct GibberishHybridGrain
    {
        [SerializeField] private GibberishConsonantId _consonant;
        [SerializeField] private AudioClip _clip;
        [SerializeField, Range(0f, 1f)] private float _mix;

        public GibberishConsonantId Consonant => _consonant;
        public AudioClip Clip => _clip;
        public float Mix => Mathf.Clamp01(_mix);
    }

    [CreateAssetMenu(
        fileName = "GibberishHybridBank",
        menuName = "Cozy Gibberish/Hybrid Voice Bank",
        order = 16)]
    public sealed class GibberishHybridVoiceBank : ScriptableObject
    {
        [SerializeField] private GibberishHybridGrain[] _grains = Array.Empty<GibberishHybridGrain>();

        public GibberishHybridSnapshot CaptureSnapshot()
        {
            var snapshots = new List<GibberishGrainSnapshot>();
            if (_grains == null)
            {
                return new GibberishHybridSnapshot(snapshots.ToArray());
            }

            for (var index = 0; index < _grains.Length; index++)
            {
                var grain = _grains[index];
                if (grain.Clip == null || grain.Mix <= 0f)
                {
                    continue;
                }

                var interleaved = new float[grain.Clip.samples * grain.Clip.channels];
                if (!grain.Clip.GetData(interleaved, 0))
                {
                    continue;
                }

                var mono = MixToMono(interleaved, grain.Clip.channels, grain.Clip.samples);
                snapshots.Add(new GibberishGrainSnapshot(
                    grain.Consonant,
                    grain.Clip.frequency,
                    grain.Mix,
                    mono));
            }

            return new GibberishHybridSnapshot(snapshots.ToArray());
        }

        private static float[] MixToMono(float[] interleaved, int channels, int sampleCount)
        {
            var safeChannels = Mathf.Max(1, channels);
            var mono = new float[sampleCount];
            for (var sample = 0; sample < sampleCount; sample++)
            {
                var sum = 0f;
                for (var channel = 0; channel < safeChannels; channel++)
                {
                    sum += interleaved[sample * safeChannels + channel];
                }

                mono[sample] = sum / safeChannels;
            }

            return mono;
        }
    }

    public readonly struct GibberishGrainSnapshot
    {
        public GibberishConsonantId Consonant { get; }
        public int SampleRate { get; }
        public float Mix { get; }
        public float[] Samples { get; }

        public GibberishGrainSnapshot(
            GibberishConsonantId consonant,
            int sampleRate,
            float mix,
            float[] samples)
        {
            Consonant = consonant;
            SampleRate = sampleRate;
            Mix = Mathf.Clamp01(mix);
            Samples = samples ?? Array.Empty<float>();
        }
    }

    public sealed class GibberishHybridSnapshot
    {
        private readonly GibberishGrainSnapshot[] _grains;

        public static GibberishHybridSnapshot Empty { get; } =
            new GibberishHybridSnapshot(Array.Empty<GibberishGrainSnapshot>());

        public GibberishHybridSnapshot(GibberishGrainSnapshot[] grains)
        {
            _grains = grains ?? Array.Empty<GibberishGrainSnapshot>();
        }

        public bool TryGet(GibberishConsonantId consonant, out GibberishGrainSnapshot grain)
        {
            for (var index = 0; index < _grains.Length; index++)
            {
                if (_grains[index].Consonant == consonant)
                {
                    grain = _grains[index];
                    return true;
                }
            }

            grain = default;
            return false;
        }
    }
}
