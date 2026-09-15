using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using UnityEngine;

namespace CozyGibberish.Tests
{
    public sealed class GibberishAdvancedRuntimeTests
    {
        private GibberishVoiceProfile _voice;

        [SetUp]
        public void SetUp()
        {
            _voice = ScriptableObject.CreateInstance<GibberishVoiceProfile>();
            _voice.ApplyArchetype(GibberishVoiceArchetype.Cozy);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_voice);
        }

        [Test]
        public void Cache_EvictsLeastRecentlyUsedEntryWithinBudget()
        {
            var cache = new GibberishPreparedSpeechCache(2, 1024L);
            var first = new GibberishRequestFingerprint(1UL);
            var second = new GibberishRequestFingerprint(2UL);
            var third = new GibberishRequestFingerprint(3UL);
            var audio = new GibberishAudioData(new float[64], 48000);

            cache.Store(first, audio);
            cache.Store(second, audio);
            Assert.That(cache.TryGet(first, out _), Is.True);
            cache.Store(third, audio);

            Assert.That(cache.TryGet(first, out _), Is.True);
            Assert.That(cache.TryGet(second, out _), Is.False);
            Assert.That(cache.TryGet(third, out _), Is.True);
        }

        [Test]
        public void CancelledSynthesis_StopsBeforeRendering()
        {
            var voice = _voice.CreateSnapshot();
            var utterance = new GibberishUtterancePlanner().CreatePlan(
                "Cancellation should be immediate.",
                voice,
                9);
            var synthesizer = new ProceduralGibberishSynthesizer();
            var cancellation = new CancellationToken(true);

            Assert.Throws<OperationCanceledException>(() =>
                synthesizer.Render(
                    utterance,
                    voice,
                    GibberishHybridSnapshot.Empty,
                    cancellation));
        }

        [Test]
        public void NetworkCodec_RoundTripsDeterministicPayload()
        {
            var request = new GibberishSpeechRequest(
                "Network cozy voice",
                _voice,
                expression: new GibberishExpression(0.2f, 0.4f, -0.1f, 0.3f),
                seed: 775,
                priority: 4,
                playbackPolicy: GibberishPlaybackPolicy.Enqueue,
                tailPause: 0.2f);
            var network = GibberishNetworkRequest.From(request);
            var payload = GibberishNetworkCodec.Serialize(network);
            var restored = GibberishNetworkCodec.Deserialize(payload);

            Assert.That(restored.Text, Is.EqualTo(request.Text));
            Assert.That(restored.Seed, Is.EqualTo(request.Seed));
        }

        [Test]
        public void CinematicQuality_PreservesHighSampleRateAndOversampling()
        {
            var cinematic = _voice
                .CreateSnapshot()
                .WithQuality(GibberishQualityTier.Cinematic);
            var mobile = _voice
                .CreateSnapshot()
                .WithQuality(GibberishQualityTier.Mobile);

            Assert.That(cinematic.SampleRate, Is.GreaterThanOrEqualTo(48000));
            Assert.That(cinematic.Oversampling, Is.GreaterThanOrEqualTo(2));
            Assert.That(mobile.SampleRate, Is.LessThanOrEqualTo(32000));
            Assert.That(mobile.Oversampling, Is.EqualTo(1));
        }

        [Test]
        public void BalancedPreview_RendersWithinProductionBudget()
        {
            var voice = _voice.CreateSnapshot();
            var utterance = new GibberishUtterancePlanner().CreatePlan(
                "A short production timing benchmark.",
                voice,
                52);
            var synthesizer = new ProceduralGibberishSynthesizer();
            var timer = Stopwatch.StartNew();

            var audio = synthesizer.Render(utterance, voice);
            timer.Stop();

            Assert.That(audio.Samples.Length, Is.GreaterThan(0));
            Assert.That(timer.ElapsedMilliseconds, Is.LessThan(1500));
        }

        [Test]
        public void Spectrum_ContainsStableLowMidAndHighEnergy()
        {
            var voice = _voice.CreateSnapshot();
            var utterance = new GibberishUtterancePlanner().CreatePlan(
                "Spectral regression lantern.",
                voice,
                218);
            var audio = new ProceduralGibberishSynthesizer().Render(utterance, voice);
            var first = MeasureBands(audio.Samples, audio.SampleRate);
            var secondAudio =
                new ProceduralGibberishSynthesizer().Render(utterance, voice);
            var second = MeasureBands(secondAudio.Samples, secondAudio.SampleRate);

            Assert.That(first.Low, Is.GreaterThan(0.000001d));
            Assert.That(first.Mid, Is.GreaterThan(0.000001d));
            Assert.That(first.High, Is.GreaterThan(0.00000001d));
            Assert.That(second.Low, Is.EqualTo(first.Low).Within(0.000000001d));
            Assert.That(second.Mid, Is.EqualTo(first.Mid).Within(0.000000001d));
            Assert.That(second.High, Is.EqualTo(first.High).Within(0.000000001d));
        }

        private static SpectrumBands MeasureBands(float[] samples, int sampleRate)
        {
            var low = 0d;
            var mid = 0d;
            var high = 0d;
            var previous = 0f;

            for (var index = 0; index < samples.Length; index++)
            {
                var sample = samples[index];
                low += sample * sample;
                var difference = sample - previous;
                mid += difference * difference;
                if ((index & 1) == 0)
                {
                    high += difference * difference * (sampleRate / 48000d);
                }

                previous = sample;
            }

            return new SpectrumBands(low, mid, high);
        }

        private readonly struct SpectrumBands
        {
            public double Low { get; }
            public double Mid { get; }
            public double High { get; }

            public SpectrumBands(double low, double mid, double high)
            {
                Low = low;
                Mid = mid;
                High = high;
            }
        }
    }
}
