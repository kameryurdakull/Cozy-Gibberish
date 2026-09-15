using NUnit.Framework;
using UnityEngine;

namespace CozyGibberish.Tests
{
    public sealed class GibberishUtterancePlannerTests
    {
        private GibberishVoiceProfile _profile;

        [SetUp]
        public void SetUp()
        {
            _profile = ScriptableObject.CreateInstance<GibberishVoiceProfile>();
            _profile.ApplyArchetype(GibberishVoiceArchetype.Cozy);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_profile);
        }

        [Test]
        public void SameTextAndSeed_ProduceIdenticalPlan()
        {
            var voice = _profile.CreateSnapshot();
            var planner = new GibberishUtterancePlanner();

            var first = planner.CreatePlan("A gentle morning breeze.", voice, 912);
            var second = planner.CreatePlan("A gentle morning breeze.", voice, 912);

            Assert.That(second.SyllableCount, Is.EqualTo(first.SyllableCount));
            Assert.That(second.Duration, Is.EqualTo(first.Duration).Within(0.00001f));

            for (var index = 0; index < first.SyllableCount; index++)
            {
                Assert.That(second.Syllables[index].StartTime,
                    Is.EqualTo(first.Syllables[index].StartTime).Within(0.00001f));
                Assert.That(second.Syllables[index].StartPitch,
                    Is.EqualTo(first.Syllables[index].StartPitch).Within(0.00001f));
                Assert.That(second.Syllables[index].Vowel.Id,
                    Is.EqualTo(first.Syllables[index].Vowel.Id));
            }
        }

        [Test]
        public void Punctuation_AddsPhrasePause()
        {
            var voice = _profile.CreateSnapshot();
            var planner = new GibberishUtterancePlanner();

            var shortPause = planner.CreatePlan("Hello friend", voice, 12);
            var sentencePause = planner.CreatePlan("Hello. Friend.", voice, 12);

            Assert.That(sentencePause.Duration, Is.GreaterThan(shortPause.Duration));
        }

        [Test]
        public void EmptyText_ReturnsSafeEmptyUtterance()
        {
            var voice = _profile.CreateSnapshot();
            var planner = new GibberishUtterancePlanner();

            var utterance = planner.CreatePlan(string.Empty, voice, 0);

            Assert.That(utterance.SyllableCount, Is.Zero);
            Assert.That(utterance.Duration, Is.GreaterThan(0f));
        }
    }
}
