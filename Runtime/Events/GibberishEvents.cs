using UnityEngine;

namespace CozyGibberish
{
    public readonly struct GibberishSpeechStartedEvent
    {
        public GibberishPlaybackHandle Handle { get; }
        public string Text { get; }
        public float Duration { get; }
        public int SyllableCount { get; }
        public double DspStartTime { get; }

        public GibberishSpeechStartedEvent(
            GibberishPlaybackHandle handle,
            string text,
            float duration,
            int syllableCount)
            : this(handle, text, duration, syllableCount, AudioSettings.dspTime)
        {
        }

        public GibberishSpeechStartedEvent(
            GibberishPlaybackHandle handle,
            string text,
            float duration,
            int syllableCount,
            double dspStartTime)
        {
            Handle = handle;
            Text = text;
            Duration = duration;
            SyllableCount = syllableCount;
            DspStartTime = dspStartTime;
        }
    }

    public readonly struct GibberishSyllableStartedEvent
    {
        public GibberishPlaybackHandle Handle { get; }
        public GibberishSyllable Syllable { get; }
        public float NormalizedPosition { get; }
        public double DspTime { get; }

        public GibberishSyllableStartedEvent(
            GibberishPlaybackHandle handle,
            GibberishSyllable syllable,
            float normalizedPosition)
            : this(handle, syllable, normalizedPosition, AudioSettings.dspTime)
        {
        }

        public GibberishSyllableStartedEvent(
            GibberishPlaybackHandle handle,
            GibberishSyllable syllable,
            float normalizedPosition,
            double dspTime)
        {
            Handle = handle;
            Syllable = syllable;
            NormalizedPosition = normalizedPosition;
            DspTime = dspTime;
        }
    }

    public readonly struct GibberishSpeechStoppedEvent
    {
        public GibberishPlaybackHandle Handle { get; }
        public GibberishStopReason Reason { get; }
        public double DspTime { get; }

        public GibberishSpeechStoppedEvent(
            GibberishPlaybackHandle handle,
            GibberishStopReason reason)
            : this(handle, reason, AudioSettings.dspTime)
        {
        }

        public GibberishSpeechStoppedEvent(
            GibberishPlaybackHandle handle,
            GibberishStopReason reason,
            double dspTime)
        {
            Handle = handle;
            Reason = reason;
            DspTime = dspTime;
        }
    }

    public readonly struct GibberishWordStartedEvent
    {
        public GibberishPlaybackHandle Handle { get; }
        public int WordIndex { get; }
        public int SourceStart { get; }
        public int SourceLength { get; }
        public double DspTime { get; }

        public GibberishWordStartedEvent(
            GibberishPlaybackHandle handle,
            int wordIndex,
            int sourceStart,
            int sourceLength,
            double dspTime)
        {
            Handle = handle;
            WordIndex = wordIndex;
            SourceStart = sourceStart;
            SourceLength = sourceLength;
            DspTime = dspTime;
        }
    }

    public readonly struct GibberishPhraseStartedEvent
    {
        public GibberishPlaybackHandle Handle { get; }
        public int PhraseIndex { get; }
        public int SourceStart { get; }
        public double DspTime { get; }

        public GibberishPhraseStartedEvent(
            GibberishPlaybackHandle handle,
            int phraseIndex,
            int sourceStart,
            double dspTime)
        {
            Handle = handle;
            PhraseIndex = phraseIndex;
            SourceStart = sourceStart;
            DspTime = dspTime;
        }
    }

    public readonly struct GibberishMouthCueEvent
    {
        public GibberishPlaybackHandle Handle { get; }
        public GibberishMouthShape Shape { get; }
        public float Intensity { get; }
        public float Duration { get; }
        public double DspTime { get; }

        public GibberishMouthCueEvent(
            GibberishPlaybackHandle handle,
            GibberishMouthShape shape,
            float intensity,
            float duration,
            double dspTime)
        {
            Handle = handle;
            Shape = shape;
            Intensity = intensity;
            Duration = duration;
            DspTime = dspTime;
        }
    }
}
