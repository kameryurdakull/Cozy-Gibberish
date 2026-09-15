using System;
using System.Threading;
using UnityEngine;

namespace CozyGibberish
{
    public sealed class ProceduralGibberishSynthesizer : IAdvancedGibberishSynthesizer
    {
        private const float TwoPi = Mathf.PI * 2f;
        private const float MinimumClipDuration = 0.02f;
        private const float MasterHeadroom = 0.92f;
        private const int CancellationStride = 1024;

        public GibberishAudioData Render(
            GibberishUtterance utterance,
            GibberishVoiceSnapshot voice)
        {
            return Render(
                utterance,
                voice,
                GibberishHybridSnapshot.Empty,
                CancellationToken.None);
        }

        public GibberishAudioData Render(
            GibberishUtterance utterance,
            GibberishVoiceSnapshot voice,
            GibberishHybridSnapshot hybrid,
            CancellationToken cancellationToken)
        {
            if (utterance == null)
            {
                throw new ArgumentNullException(nameof(utterance));
            }

            if (voice == null)
            {
                throw new ArgumentNullException(nameof(voice));
            }

            var duration = Mathf.Max(MinimumClipDuration, utterance.Duration);
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * voice.SampleRate));
            var samples = new float[sampleCount];
            var resolvedHybrid = hybrid ?? GibberishHybridSnapshot.Empty;

            for (var index = 0; index < utterance.SyllableCount; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var syllable = utterance.Syllables[index];
                RenderSyllable(samples, syllable, voice, cancellationToken);
                BlendHybridGrain(samples, syllable, voice.SampleRate, resolvedHybrid);
            }

            ConditionOutput(samples, cancellationToken);
            return new GibberishAudioData(samples, voice.SampleRate);
        }

        private static void RenderSyllable(
            float[] destination,
            GibberishSyllable syllable,
            GibberishVoiceSnapshot voice,
            CancellationToken cancellationToken)
        {
            var sampleRate = voice.SampleRate;
            var oversampling = Mathf.Clamp(voice.Oversampling, 1, 4);
            var internalRate = sampleRate * oversampling;
            var startSample = Mathf.Max(0, Mathf.FloorToInt(syllable.StartTime * sampleRate));
            var length = Mathf.Max(1, Mathf.CeilToInt(syllable.Duration * sampleRate));
            var available = Mathf.Min(length, destination.Length - startSample);
            var vowel = syllable.Vowel;
            var formantShift = voice.FormantShift;
            var quality = Mathf.Lerp(3.2f, 8.5f, voice.Warmth);
            var firstFormant = new GibberishBiquadBandPass(vowel.FirstFormant * formantShift, quality, internalRate);
            var secondFormant = new GibberishBiquadBandPass(vowel.SecondFormant * formantShift, quality * 0.82f, internalRate);
            var thirdFormant = new GibberishBiquadBandPass(vowel.ThirdFormant * formantShift, quality * 0.68f, internalRate);
            var random = new GibberishDeterministicRandom(syllable.NoiseSeed);
            var phase = random.Next01() * TwoPi;
            var duration = syllable.Duration;

            for (var outputIndex = 0; outputIndex < available; outputIndex++)
            {
                if ((outputIndex & (CancellationStride - 1)) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                var accumulated = 0f;

                for (var subSample = 0; subSample < oversampling; subSample++)
                {
                    var internalIndex = outputIndex * oversampling + subSample;
                    var time = internalIndex / (float)internalRate;
                    var progress = Mathf.Clamp01(time / duration);
                    var smoothProgress = progress * progress * (3f - 2f * progress);
                    var frequency = Mathf.Lerp(syllable.StartPitch, syllable.EndPitch, smoothProgress);
                    var vibrato = Mathf.Sin(TwoPi * voice.VibratoRate * time);
                    frequency *= Mathf.Pow(2f, vibrato * voice.VibratoDepthSemitones / 12f);
                    var phaseIncrement = TwoPi * frequency / internalRate;
                    phase += phaseIncrement;

                    if (phase >= TwoPi)
                    {
                        phase -= TwoPi;
                    }

                    var voiced = CreateWaveform(
                        phase,
                        phaseIncrement,
                        voice.Waveform,
                        voice.Warmth);
                    var noise = random.NextSigned();
                    var breath = noise * voice.Breathiness * 0.22f;
                    var excitation = voiced * (1f - voice.Breathiness * 0.35f) + breath;
                    var f1 = firstFormant.Process(excitation);
                    var f2 = secondFormant.Process(excitation);
                    var f3 = thirdFormant.Process(excitation);
                    var formants = f1 * 2.7f + f2 * 2.15f + f3 * 1.35f;
                    var dry = voiced * Mathf.Lerp(0.12f, 0.28f, voice.Brightness);
                    var consonantEnvelope = Mathf.Exp(-time * Mathf.Lerp(42f, 85f, voice.Brightness));
                    var onset = CreateConsonant(
                        syllable.Onset,
                        time,
                        duration,
                        noise,
                        phase,
                        voice.ConsonantStrength,
                        true);
                    var coda = CreateConsonant(
                        syllable.Coda,
                        time,
                        duration,
                        noise,
                        phase,
                        voice.ConsonantStrength,
                        false);
                    var consonant = noise * consonantEnvelope * voice.ConsonantStrength * 0.25f +
                                    onset +
                                    coda;
                    var envelope = CreateEnvelope(time, duration, voice.Attack, voice.Release);
                    var sample = (formants + dry + consonant) * envelope * syllable.Intensity * voice.Volume;
                    accumulated += SoftClip(sample);
                }

                destination[startSample + outputIndex] += accumulated / oversampling;
            }
        }

        private static float CreateWaveform(
            float phase,
            float phaseIncrement,
            GibberishWaveform waveform,
            float warmth)
        {
            var sine = Mathf.Sin(phase);
            switch (waveform)
            {
                case GibberishWaveform.Sine:
                    return sine;
                case GibberishWaveform.RoundedSquare:
                    return (float)Math.Tanh(Mathf.Lerp(1.4f, 3.2f, 1f - warmth) * sine);
                case GibberishWaveform.WarmSaw:
                    var normalizedPhase = phase / TwoPi;
                    var normalizedIncrement = phaseIncrement / TwoPi;
                    var saw = 2f * normalizedPhase - 1f;
                    saw -= 2f * PolyBlep(normalizedPhase, normalizedIncrement);
                    return Mathf.Lerp(saw, sine, 0.35f + warmth * 0.35f);
                default:
                    var triangle = 2f / Mathf.PI * Mathf.Asin(sine);
                    return Mathf.Lerp(triangle, sine, warmth * 0.48f);
            }
        }

        private static float CreateConsonant(
            GibberishConsonantId consonant,
            float time,
            float duration,
            float noise,
            float phase,
            float strength,
            bool onset)
        {
            if (consonant == GibberishConsonantId.None)
            {
                return 0f;
            }

            var edgeTime = onset ? time : duration - time;
            var envelopeRate = ResolveConsonantEnvelopeRate(consonant);
            var envelope = Mathf.Exp(-Mathf.Max(0f, edgeTime) * envelopeRate);
            var source = ResolveConsonantSource(consonant, noise, phase);
            return source * envelope * strength;
        }

        private static float ResolveConsonantEnvelopeRate(GibberishConsonantId consonant)
        {
            switch (consonant)
            {
                case GibberishConsonantId.HushS:
                case GibberishConsonantId.HushSh:
                    return 26f;
                case GibberishConsonantId.SoftM:
                case GibberishConsonantId.SoftN:
                case GibberishConsonantId.SoftL:
                    return 38f;
                default:
                    return 72f;
            }
        }

        private static float ResolveConsonantSource(
            GibberishConsonantId consonant,
            float noise,
            float phase)
        {
            switch (consonant)
            {
                case GibberishConsonantId.SoftM:
                case GibberishConsonantId.SoftN:
                    return Mathf.Sin(phase * 0.5f) * 0.55f + noise * 0.08f;
                case GibberishConsonantId.SoftL:
                case GibberishConsonantId.SoftW:
                case GibberishConsonantId.SoftY:
                    return Mathf.Sin(phase) * 0.42f + noise * 0.12f;
                case GibberishConsonantId.HushS:
                    return noise * 0.75f;
                case GibberishConsonantId.HushSh:
                    return noise * 0.55f + Mathf.Sin(phase * 1.5f) * 0.08f;
                case GibberishConsonantId.RolledR:
                    return Mathf.Sin(phase) * Mathf.Sign(Mathf.Sin(phase * 3f)) * 0.45f;
                case GibberishConsonantId.MechanicalZ:
                    return Mathf.Sign(Mathf.Sin(phase * 2f)) * 0.35f + noise * 0.18f;
                case GibberishConsonantId.BrightK:
                case GibberishConsonantId.BrightT:
                case GibberishConsonantId.BrightP:
                    return noise;
                default:
                    return Mathf.Sin(phase) * 0.35f;
            }
        }

        private static void BlendHybridGrain(
            float[] destination,
            GibberishSyllable syllable,
            int destinationRate,
            GibberishHybridSnapshot hybrid)
        {
            if (!hybrid.TryGet(syllable.Onset, out var grain) ||
                grain.Samples.Length == 0 ||
                grain.SampleRate <= 0)
            {
                return;
            }

            var startSample = Mathf.Max(0, Mathf.FloorToInt(syllable.StartTime * destinationRate));
            var rateRatio = grain.SampleRate / (float)destinationRate;
            var outputLength = Mathf.Min(
                Mathf.CeilToInt(grain.Samples.Length / rateRatio),
                destination.Length - startSample);

            for (var outputIndex = 0; outputIndex < outputLength; outputIndex++)
            {
                var sourcePosition = outputIndex * rateRatio;
                var left = Mathf.Clamp(Mathf.FloorToInt(sourcePosition), 0, grain.Samples.Length - 1);
                var right = Mathf.Min(left + 1, grain.Samples.Length - 1);
                var sample = Mathf.Lerp(
                    grain.Samples[left],
                    grain.Samples[right],
                    sourcePosition - left);
                var normalized = outputLength <= 1 ? 1f : outputIndex / (float)(outputLength - 1);
                var envelope = 1f - normalized * normalized;
                var mix = grain.Mix * envelope;
                var destinationIndex = startSample + outputIndex;
                destination[destinationIndex] = Mathf.Lerp(
                    destination[destinationIndex],
                    sample,
                    mix);
            }
        }

        private static float PolyBlep(float phase, float phaseIncrement)
        {
            if (phase < phaseIncrement)
            {
                var normalized = phase / phaseIncrement;
                return normalized + normalized - normalized * normalized - 1f;
            }

            if (phase > 1f - phaseIncrement)
            {
                var normalized = (phase - 1f) / phaseIncrement;
                return normalized * normalized + normalized + normalized + 1f;
            }

            return 0f;
        }

        private static float CreateEnvelope(
            float time,
            float duration,
            float attack,
            float release)
        {
            var attackEnvelope = Mathf.Clamp01(time / Mathf.Max(0.001f, attack));
            var releaseEnvelope = Mathf.Clamp01((duration - time) / Mathf.Max(0.001f, release));
            var envelope = Mathf.Min(attackEnvelope, releaseEnvelope);
            return envelope * envelope * (3f - 2f * envelope);
        }

        private static float SoftClip(float sample)
        {
            return sample / (1f + Mathf.Abs(sample));
        }

        private static void ConditionOutput(
            float[] samples,
            CancellationToken cancellationToken)
        {
            var previousInput = 0f;
            var previousOutput = 0f;
            var peak = 0f;

            for (var index = 0; index < samples.Length; index++)
            {
                if ((index & (CancellationStride - 1)) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                var input = samples[index];
                var output = input - previousInput + 0.995f * previousOutput;
                previousInput = input;
                previousOutput = output;
                samples[index] = output;
                peak = Mathf.Max(peak, Mathf.Abs(output));
            }

            if (peak <= MasterHeadroom)
            {
                return;
            }

            var gain = MasterHeadroom / peak;
            for (var index = 0; index < samples.Length; index++)
            {
                samples[index] *= gain;
            }
        }
    }
}
