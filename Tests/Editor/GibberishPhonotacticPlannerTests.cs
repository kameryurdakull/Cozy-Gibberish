using NUnit.Framework;
using UnityEngine;

namespace CozyGibberish.Tests
{
    public sealed class GibberishPhonotacticPlannerTests
    {
        private GibberishVoiceProfile _voice;
        private GibberishPhonotacticProfile _phonotactics;

        [SetUp]
        public void SetUp()
        {
            _voice = ScriptableObject.CreateInstance<GibberishVoiceProfile>();
            _voice.ApplyArchetype(GibberishVoiceArchetype.Cozy);
            _phonotactics =
                ScriptableObject.CreateInstance<GibberishPhonotacticProfile>();
            _phonotactics.ApplyArchetype(GibberishVoiceArchetype.Cozy);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_voice);
            Object.DestroyImmediate(_phonotactics);
        }

        [Test]
        public void Planner_ProducesDeterministicPhonemesWithoutImmediateOnsetRepeats()
        {
            var planner = new GibberishUtterancePlanner();
            var voice = _voice.CreateSnapshot();
            var language = _phonotactics.CreateSnapshot();

            var first = planner.CreatePlan(
                "The lantern glows beside the quiet window.",
                voice,
                language,
                491);
            var second = planner.CreatePlan(
                "The lantern glows beside the quiet window.",
                voice,
                language,
                491);

            Assert.That(first.SyllableCount, Is.EqualTo(second.SyllableCount));
            for (var index = 0; index < first.SyllableCount; index++)
            {
                Assert.That(first.Syllables[index].Onset,
                    Is.EqualTo(second.Syllables[index].Onset));
                Assert.That(first.Syllables[index].Coda,
                    Is.EqualTo(second.Syllables[index].Coda));

                if (index > 0 &&
                    first.Syllables[index].Onset != GibberishConsonantId.None)
                {
                    Assert.That(
                        first.Syllables[index].Onset,
                        Is.Not.EqualTo(first.Syllables[index - 1].Onset));
                }
            }
        }

        [Test]
        public void UnicodeAndRichText_PreserveValidSourceRanges()
        {
            const string text = "<b>Günaydın</b> küçük kâşif! 🌿";
            var planner = new GibberishUtterancePlanner();
            var utterance = planner.CreatePlan(
                text,
                _voice.CreateSnapshot(),
                _phonotactics.CreateSnapshot(),
                37);

            Assert.That(utterance.SyllableCount, Is.GreaterThan(0));
            for (var index = 0; index < utterance.SyllableCount; index++)
            {
                var syllable = utterance.Syllables[index];
                Assert.That(syllable.SourceStart, Is.GreaterThanOrEqualTo(0));
                Assert.That(
                    syllable.SourceStart + syllable.SourceLength,
                    Is.LessThanOrEqualTo(text.Length));
                Assert.That(syllable.SourceStart, Is.Not.EqualTo(0));
            }
        }

        [Test]
        public void MechanicalArchetype_ProducesMoreCodasThanWhimsical()
        {
            var planner = new GibberishUtterancePlanner();
            var voice = _voice.CreateSnapshot();
            var mechanical =
                ScriptableObject.CreateInstance<GibberishPhonotacticProfile>();
            var whimsical =
                ScriptableObject.CreateInstance<GibberishPhonotacticProfile>();

            try
            {
                mechanical.ApplyArchetype(GibberishVoiceArchetype.Mechanical);
                whimsical.ApplyArchetype(GibberishVoiceArchetype.Whimsical);
                var mechanicalPlan = planner.CreatePlan(
                    "Copper circuits carry curious conversations.",
                    voice,
                    mechanical.CreateSnapshot(),
                    82);
                var whimsicalPlan = planner.CreatePlan(
                    "Copper circuits carry curious conversations.",
                    voice,
                    whimsical.CreateSnapshot(),
                    82);

                Assert.That(
                    CountCodas(mechanicalPlan),
                    Is.GreaterThan(CountCodas(whimsicalPlan)));
            }
            finally
            {
                Object.DestroyImmediate(mechanical);
                Object.DestroyImmediate(whimsical);
            }
        }

        private static int CountCodas(GibberishUtterance utterance)
        {
            var result = 0;
            for (var index = 0; index < utterance.SyllableCount; index++)
            {
                if (utterance.Syllables[index].Coda != GibberishConsonantId.None)
                {
                    result++;
                }
            }

            return result;
        }
    }
}
