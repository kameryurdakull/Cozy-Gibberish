using System;
using System.Threading;

namespace CozyGibberish
{
    /// <summary>
    /// Renders an immutable utterance plan into engine-independent mono PCM data.
    /// Implementations must be safe to call from a worker thread.
    /// </summary>
    public interface IGibberishSynthesizer
    {
        GibberishAudioData Render(GibberishUtterance utterance, GibberishVoiceSnapshot voice);
    }

    public interface IAdvancedGibberishSynthesizer : IGibberishSynthesizer
    {
        GibberishAudioData Render(
            GibberishUtterance utterance,
            GibberishVoiceSnapshot voice,
            GibberishHybridSnapshot hybrid,
            CancellationToken cancellationToken);
    }

    public sealed class GibberishAudioData
    {
        public float[] Samples { get; }
        public int SampleRate { get; }
        public int Channels => 1;
        public float Duration => Samples.Length / (float)SampleRate;

        public GibberishAudioData(float[] samples, int sampleRate)
        {
            Samples = samples ?? Array.Empty<float>();
            SampleRate = Math.Max(8000, sampleRate);
        }
    }
}
