using System;
using System.Collections.Generic;

namespace CozyGibberish
{
    public readonly struct GibberishSyllable
    {
        public int Index { get; }
        public float StartTime { get; }
        public float Duration { get; }
        public float StartPitch { get; }
        public float EndPitch { get; }
        public GibberishVowelDefinition Vowel { get; }
        public GibberishConsonantId Onset { get; }
        public GibberishConsonantId Coda { get; }
        public GibberishMouthShape MouthShape { get; }
        public float Intensity { get; }
        public uint NoiseSeed { get; }
        public int WordIndex { get; }
        public int PhraseIndex { get; }
        public int SourceStart { get; }
        public int SourceLength { get; }

        public GibberishSyllable(
            int index,
            float startTime,
            float duration,
            float startPitch,
            float endPitch,
            GibberishVowelDefinition vowel,
            float intensity,
            uint noiseSeed)
            : this(
                index,
                startTime,
                duration,
                startPitch,
                endPitch,
                vowel,
                GibberishConsonantId.None,
                GibberishConsonantId.None,
                intensity,
                noiseSeed,
                0,
                0,
                0,
                0)
        {
        }

        public GibberishSyllable(
            int index,
            float startTime,
            float duration,
            float startPitch,
            float endPitch,
            GibberishVowelDefinition vowel,
            GibberishConsonantId onset,
            GibberishConsonantId coda,
            float intensity,
            uint noiseSeed,
            int wordIndex,
            int phraseIndex,
            int sourceStart,
            int sourceLength)
        {
            Index = index;
            StartTime = startTime;
            Duration = duration;
            StartPitch = startPitch;
            EndPitch = endPitch;
            Vowel = vowel;
            Onset = onset;
            Coda = coda;
            MouthShape = ResolveMouthShape(vowel.Id);
            Intensity = intensity;
            NoiseSeed = noiseSeed;
            WordIndex = wordIndex;
            PhraseIndex = phraseIndex;
            SourceStart = sourceStart;
            SourceLength = sourceLength;
        }

        private static GibberishMouthShape ResolveMouthShape(GibberishVowelId vowel)
        {
            switch (vowel)
            {
                case GibberishVowelId.Ee:
                    return GibberishMouthShape.Wide;
                case GibberishVowelId.Oh:
                case GibberishVowelId.Oo:
                    return GibberishMouthShape.Round;
                case GibberishVowelId.Eh:
                    return GibberishMouthShape.Narrow;
                default:
                    return GibberishMouthShape.Open;
            }
        }
    }

    public sealed class GibberishUtterance
    {
        private readonly GibberishSyllable[] _syllables;
        private readonly IReadOnlyList<GibberishSyllable> _readOnlySyllables;

        public static GibberishUtterance Empty { get; } =
            new GibberishUtterance(Array.Empty<GibberishSyllable>(), 0.02f, 0);

        public IReadOnlyList<GibberishSyllable> Syllables => _readOnlySyllables;
        public float Duration { get; }
        public int Seed { get; }
        public int SyllableCount => _syllables.Length;

        public GibberishUtterance(
            GibberishSyllable[] syllables,
            float duration,
            int seed)
        {
            _syllables = syllables ?? Array.Empty<GibberishSyllable>();
            _readOnlySyllables = Array.AsReadOnly(_syllables);
            Duration = Math.Max(0.02f, duration);
            Seed = seed;
        }

        public GibberishUtterance WithAdditionalDuration(float duration)
        {
            return duration <= 0f
                ? this
                : new GibberishUtterance(_syllables, Duration + duration, Seed);
        }
    }

    /// <summary>
    /// Converts text into a deterministic timing and phoneme plan without producing audio.
    /// </summary>
    public interface IGibberishUtterancePlanner
    {
        GibberishUtterance CreatePlan(string text, GibberishVoiceSnapshot voice, int seed);
    }

    public interface IAdvancedGibberishUtterancePlanner : IGibberishUtterancePlanner
    {
        GibberishUtterance CreatePlan(
            string text,
            GibberishVoiceSnapshot voice,
            GibberishPhonotacticSnapshot phonotactics,
            int seed);
    }
}
