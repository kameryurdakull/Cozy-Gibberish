using NUnit.Framework;
using UnityEngine;

namespace CozyGibberish.Tests
{
    public sealed class ProceduralGibberishSynthesizerTests
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
        public void Render_ProducesFiniteHeadroomSafeSamples()
        {
            var voice = _profile.CreateSnapshot();
            var planner = new GibberishUtterancePlanner();
            var utterance = planner.CreatePlan("Tea is ready!", voice, 23);
            var synthesizer = new ProceduralGibberishSynthesizer();

            var audio = synthesizer.Render(utterance, voice);
            var peak = 0f;

            Assert.That(audio.Samples.Length, Is.GreaterThan(0));
            Assert.That(audio.SampleRate, Is.EqualTo(voice.SampleRate));

            for (var index = 0; index < audio.Samples.Length; index++)
            {
                var sample = audio.Samples[index];
                Assert.That(float.IsNaN(sample), Is.False);
                Assert.That(float.IsInfinity(sample), Is.False);
                peak = Mathf.Max(peak, Mathf.Abs(sample));
            }

            Assert.That(peak, Is.LessThanOrEqualTo(0.921f));
            Assert.That(peak, Is.GreaterThan(0.001f));
        }

        [Test]
        public void Render_IsDeterministic()
        {
            var voice = _profile.CreateSnapshot();
            var planner = new GibberishUtterancePlanner();
            var utterance = planner.CreatePlan("Same voice, same result.", voice, 71);
            var synthesizer = new ProceduralGibberishSynthesizer();

            var first = synthesizer.Render(utterance, voice);
            var second = synthesizer.Render(utterance, voice);

            CollectionAssert.AreEqual(first.Samples, second.Samples);
        }
    }
}
