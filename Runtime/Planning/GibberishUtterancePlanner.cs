using System;
using System.Collections.Generic;
using UnityEngine;

namespace CozyGibberish
{
    public sealed class GibberishUtterancePlanner : IAdvancedGibberishUtterancePlanner
    {
        private const int MaximumWordSyllables = 12;

        public GibberishUtterance CreatePlan(
            string text,
            GibberishVoiceSnapshot voice,
            int seed)
        {
            return CreatePlan(
                text,
                voice,
                GibberishPhonotacticSnapshot.Default,
                seed);
        }

        public GibberishUtterance CreatePlan(
            string text,
            GibberishVoiceSnapshot voice,
            GibberishPhonotacticSnapshot phonotactics,
            int seed)
        {
            if (voice == null)
            {
                throw new ArgumentNullException(nameof(voice));
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return GibberishUtterance.Empty;
            }

            var language = phonotactics ?? GibberishPhonotacticSnapshot.Default;
            var resolvedSeed = GibberishDeterministicRandom.CombineSeed(text, seed);
            var random = new GibberishDeterministicRandom((uint)resolvedSeed);
            var tokens = GibberishTextTokenizer.Parse(text, language.TokenizerMode);
            var syllables = new List<GibberishSyllable>(
                Mathf.Min(text.Length, voice.MaximumSyllables));
            var cursor = 0f;
            var previousVowel = GibberishVowelId.Ah;
            var previousOnset = GibberishConsonantId.None;
            var previousCoda = GibberishConsonantId.None;

            for (var tokenIndex = 0; tokenIndex < tokens.Count; tokenIndex++)
            {
                if (syllables.Count >= voice.MaximumSyllables)
                {
                    break;
                }

                var token = tokens[tokenIndex];
                var normalizedTokenPosition = tokens.Count <= 1
                    ? 0f
                    : tokenIndex / (float)(tokens.Count - 1);
                var density = voice.CharactersPerSyllable /
                              Mathf.Max(0.5f, language.SyllableDensity);
                var rawCount = Mathf.CeilToInt(token.CharacterCount / density);
                var wordSyllables = Mathf.Clamp(rawCount, 1, MaximumWordSyllables);

                for (var wordSyllableIndex = 0;
                     wordSyllableIndex < wordSyllables;
                     wordSyllableIndex++)
                {
                    if (syllables.Count >= voice.MaximumSyllables)
                    {
                        break;
                    }

                    var wordProgress = wordSyllables <= 1
                        ? 0f
                        : wordSyllableIndex / (float)(wordSyllables - 1);
                    var phrasePosition = Mathf.Clamp01(
                        normalizedTokenPosition + wordProgress / Mathf.Max(1, tokens.Count));
                    var prosody = voice.Prosody;
                    var durationRandom = random.Next01();
                    var baseDuration = voice.SyllableDuration.Lerp(durationRandom);
                    var timingScale = 1f + random.NextSigned() * voice.TimingVariation;
                    var paceScale = prosody.SamplePace(phrasePosition);
                    var duration = Mathf.Max(0.03f, baseDuration * timingScale / paceScale);
                    var pitchRandom = voice.PitchVariationSemitones.Lerp(random.Next01());
                    var prosodyPitch = prosody.SamplePitch(phrasePosition);
                    var startPitch = SemitoneToFrequency(
                        voice.BasePitch,
                        pitchRandom + prosodyPitch);
                    var endSemitones = CreateCadence(
                        token.Pause,
                        wordSyllableIndex,
                        wordSyllables,
                        voice,
                        ref random);
                    var endPitch = SemitoneToFrequency(startPitch, endSemitones);
                    var vowel = SelectVowel(
                        voice.Vowels,
                        previousVowel,
                        language.VowelHarmony,
                        ref random);
                    var onset = language.SelectOnset(previousOnset, ref random);
                    var coda = language.SelectCoda(previousCoda, ref random);
                    var energy = prosody.SampleEnergy(phrasePosition);
                    var intensity = Mathf.Clamp01(
                        (0.82f + random.NextSigned() * 0.12f + voice.Brightness * 0.08f) *
                        energy);
                    var sourceStart = token.SourceStart +
                                      Mathf.FloorToInt(
                                          token.SourceLength * wordSyllableIndex /
                                          (float)wordSyllables);
                    var sourceEnd = token.SourceStart +
                                    Mathf.CeilToInt(
                                        token.SourceLength * (wordSyllableIndex + 1) /
                                        (float)wordSyllables);

                    syllables.Add(new GibberishSyllable(
                        syllables.Count,
                        cursor,
                        duration,
                        startPitch,
                        endPitch,
                        vowel,
                        onset,
                        coda,
                        intensity,
                        random.NextUInt(),
                        token.WordIndex,
                        token.PhraseIndex,
                        sourceStart,
                        Mathf.Max(0, sourceEnd - sourceStart)));

                    previousVowel = vowel.Id;
                    previousOnset = onset;
                    previousCoda = coda;
                    cursor += duration + voice.SyllableGap;
                }

                cursor += ResolvePause(token.Pause, voice);
            }

            var plan = syllables.ToArray();
            var durationWithTail = cursor + voice.Release + 0.02f;
            return new GibberishUtterance(plan, durationWithTail, resolvedSeed);
        }

        private static GibberishVowelDefinition SelectVowel(
            IReadOnlyList<GibberishVowelDefinition> vowels,
            GibberishVowelId previous,
            float harmony,
            ref GibberishDeterministicRandom random)
        {
            if (vowels == null || vowels.Count == 0)
            {
                return new GibberishVowelDefinition(
                    GibberishVowelId.Ah,
                    1f,
                    700f,
                    1220f,
                    2600f);
            }

            var useHarmony = random.Next01() < harmony;
            var previousGroup = ResolveVowelGroup(previous);
            var totalWeight = 0f;

            for (var index = 0; index < vowels.Count; index++)
            {
                var candidate = vowels[index];
                if (useHarmony && ResolveVowelGroup(candidate.Id) != previousGroup)
                {
                    continue;
                }

                var repetitionPenalty = candidate.Id == previous ? 0.18f : 1f;
                totalWeight += candidate.Weight * repetitionPenalty;
            }

            if (totalWeight <= 0f)
            {
                return vowels[0];
            }

            var selection = random.Next01() * totalWeight;
            for (var index = 0; index < vowels.Count; index++)
            {
                var candidate = vowels[index];
                if (useHarmony && ResolveVowelGroup(candidate.Id) != previousGroup)
                {
                    continue;
                }

                var repetitionPenalty = candidate.Id == previous ? 0.18f : 1f;
                selection -= candidate.Weight * repetitionPenalty;
                if (selection <= 0f)
                {
                    return candidate;
                }
            }

            return vowels[vowels.Count - 1];
        }

        private static int ResolveVowelGroup(GibberishVowelId vowel)
        {
            return vowel == GibberishVowelId.Eh || vowel == GibberishVowelId.Ee ? 0 : 1;
        }

        private static float CreateCadence(
            GibberishPause pause,
            int wordIndex,
            int wordSyllables,
            GibberishVoiceSnapshot voice,
            ref GibberishDeterministicRandom random)
        {
            var movement = random.NextSigned() * 0.65f;
            if (wordIndex < wordSyllables - 1)
            {
                return movement;
            }

            switch (pause)
            {
                case GibberishPause.Question:
                    return 2.2f * voice.PhraseCadence + movement;
                case GibberishPause.Exclamation:
                    return 1.1f * voice.PhraseCadence + movement;
                case GibberishPause.Sentence:
                    return -2.8f * voice.PhraseCadence + movement;
                case GibberishPause.Comma:
                    return -0.8f * voice.PhraseCadence + movement;
                default:
                    return -0.35f * voice.PhraseCadence + movement;
            }
        }

        private static float ResolvePause(
            GibberishPause pause,
            GibberishVoiceSnapshot voice)
        {
            switch (pause)
            {
                case GibberishPause.Sentence:
                case GibberishPause.Question:
                case GibberishPause.Exclamation:
                    return voice.SentencePause;
                case GibberishPause.Comma:
                    return Mathf.Lerp(voice.WordPause, voice.SentencePause, 0.42f);
                default:
                    return voice.WordPause;
            }
        }

        private static float SemitoneToFrequency(float frequency, float semitones)
        {
            return frequency * Mathf.Pow(2f, semitones / 12f);
        }
    }
}
