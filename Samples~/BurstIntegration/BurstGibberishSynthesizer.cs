#if COZY_GIBBERISH_BURST
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace CozyGibberish.Integrations.Burst
{
    public sealed class BurstGibberishSynthesizer : IAdvancedGibberishSynthesizer
    {
        private readonly ProceduralGibberishSynthesizer _fallback =
            new ProceduralGibberishSynthesizer();

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
            var audio = _fallback.Render(utterance, voice, hybrid, cancellationToken);
            using (var samples = new NativeArray<float>(
                       audio.Samples,
                       Allocator.TempJob))
            {
                var job = new SoftSaturationJob
                {
                    Samples = samples,
                    Drive = 1.035f
                };
                job.Schedule(samples.Length, 256).Complete();
                samples.CopyTo(audio.Samples);
            }

            return audio;
        }

        [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
        private struct SoftSaturationJob : IJobParallelFor
        {
            public NativeArray<float> Samples;
            public float Drive;

            public void Execute(int index)
            {
                var sample = Samples[index] * Drive;
                Samples[index] = sample / (1f + Unity.Mathematics.math.abs(sample));
            }
        }
    }
}
#endif
