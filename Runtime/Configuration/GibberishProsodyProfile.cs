using System;
using UnityEngine;

namespace CozyGibberish
{
    [CreateAssetMenu(
        fileName = "GibberishProsody",
        menuName = "Cozy Gibberish/Prosody Profile",
        order = 14)]
    public sealed class GibberishProsodyProfile : ScriptableObject
    {
        private const int SampleCount = 64;

        [SerializeField] private AnimationCurve _pitchSemitones =
            AnimationCurve.EaseInOut(0f, 0f, 1f, -1.25f);
        [SerializeField] private AnimationCurve _energy =
            AnimationCurve.EaseInOut(0f, 1f, 1f, 0.88f);
        [SerializeField] private AnimationCurve _pace =
            AnimationCurve.Linear(0f, 1f, 1f, 0.94f);

        public GibberishProsodySnapshot CreateSnapshot()
        {
            var pitch = Sample(_pitchSemitones);
            var energy = Sample(_energy);
            var pace = Sample(_pace);
            return new GibberishProsodySnapshot(pitch, energy, pace);
        }

        private static float[] Sample(AnimationCurve curve)
        {
            var result = new float[SampleCount];
            var safeCurve = curve ?? AnimationCurve.Linear(0f, 0f, 1f, 0f);
            for (var index = 0; index < result.Length; index++)
            {
                var position = index / (float)(result.Length - 1);
                result[index] = safeCurve.Evaluate(position);
            }

            return result;
        }
    }

    public sealed class GibberishProsodySnapshot
    {
        private static readonly float[] NeutralPitch = { 0f, 0f };
        private static readonly float[] NeutralScale = { 1f, 1f };

        private readonly float[] _pitch;
        private readonly float[] _energy;
        private readonly float[] _pace;

        public static GibberishProsodySnapshot Neutral { get; } =
            new GibberishProsodySnapshot(NeutralPitch, NeutralScale, NeutralScale);

        public GibberishProsodySnapshot(float[] pitch, float[] energy, float[] pace)
        {
            _pitch = CopyOrFallback(pitch, NeutralPitch);
            _energy = CopyOrFallback(energy, NeutralScale);
            _pace = CopyOrFallback(pace, NeutralScale);
        }

        public float SamplePitch(float normalizedPosition)
        {
            return Sample(_pitch, normalizedPosition);
        }

        public float SampleEnergy(float normalizedPosition)
        {
            return Mathf.Max(0.05f, Sample(_energy, normalizedPosition));
        }

        public float SamplePace(float normalizedPosition)
        {
            return Mathf.Max(0.1f, Sample(_pace, normalizedPosition));
        }

        private static float Sample(float[] source, float position)
        {
            var scaled = Mathf.Clamp01(position) * (source.Length - 1);
            var left = Mathf.FloorToInt(scaled);
            var right = Mathf.Min(left + 1, source.Length - 1);
            return Mathf.Lerp(source[left], source[right], scaled - left);
        }

        private static float[] CopyOrFallback(float[] source, float[] fallback)
        {
            var safeSource = source == null || source.Length < 2 ? fallback : source;
            var result = new float[safeSource.Length];
            Array.Copy(safeSource, result, safeSource.Length);
            return result;
        }
    }
}
