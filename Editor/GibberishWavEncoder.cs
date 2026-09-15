using System;
using System.IO;
using System.Text;

namespace CozyGibberish.Editor
{
    internal static class GibberishWavEncoder
    {
        private const int HeaderSize = 44;
        private const short PcmFormat = 1;
        private const short ChannelCount = 1;
        private const short BitsPerSample = 16;
        private const string RiffId = "RIFF";
        private const string WaveId = "WAVE";
        private const string FormatId = "fmt ";
        private const string DataId = "data";

        public static byte[] Encode(GibberishAudioData audio)
        {
            if (audio == null)
            {
                throw new ArgumentNullException(nameof(audio));
            }

            var dataSize = audio.Samples.Length * sizeof(short);
            var result = new byte[HeaderSize + dataSize];
            using (var stream = new MemoryStream(result))
            using (var writer = new BinaryWriter(stream, Encoding.ASCII))
            {
                writer.Write(Encoding.ASCII.GetBytes(RiffId));
                writer.Write(36 + dataSize);
                writer.Write(Encoding.ASCII.GetBytes(WaveId));
                writer.Write(Encoding.ASCII.GetBytes(FormatId));
                writer.Write(16);
                writer.Write(PcmFormat);
                writer.Write(ChannelCount);
                writer.Write(audio.SampleRate);
                writer.Write(audio.SampleRate * ChannelCount * BitsPerSample / 8);
                writer.Write((short)(ChannelCount * BitsPerSample / 8));
                writer.Write(BitsPerSample);
                writer.Write(Encoding.ASCII.GetBytes(DataId));
                writer.Write(dataSize);

                for (var index = 0; index < audio.Samples.Length; index++)
                {
                    var sample = Math.Max(-1f, Math.Min(1f, audio.Samples[index]));
                    writer.Write((short)Math.Round(sample * short.MaxValue));
                }
            }

            return result;
        }

        public static void Write(string path, GibberishAudioData audio)
        {
            File.WriteAllBytes(path, Encode(audio));
        }
    }
}
