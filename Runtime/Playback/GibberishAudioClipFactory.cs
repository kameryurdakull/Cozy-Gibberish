using System;
using UnityEngine;

namespace CozyGibberish
{
    public static class GibberishAudioClipFactory
    {
        public static AudioClip Create(string clipName, GibberishAudioData audio)
        {
            if (audio == null)
            {
                throw new ArgumentNullException(nameof(audio));
            }

            var safeName = string.IsNullOrWhiteSpace(clipName) ? nameof(GibberishAudioData) : clipName;
            var clip = AudioClip.Create(
                safeName,
                Mathf.Max(1, audio.Samples.Length),
                audio.Channels,
                audio.SampleRate,
                false);

            if (audio.Samples.Length > 0 && !clip.SetData(audio.Samples, 0))
            {
                UnityEngine.Object.Destroy(clip);
                throw new InvalidOperationException("Unity could not upload the generated gibberish samples.");
            }

            return clip;
        }
    }
}
