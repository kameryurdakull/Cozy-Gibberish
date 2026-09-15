using System;

namespace CozyGibberish
{
    public sealed class GibberishSpeechContext
    {
        public string Text { get; }
        public string VoiceName { get; }
        public string StyleName { get; }
        public GibberishVoiceSnapshot Voice { get; }
        public GibberishUtterance Utterance { get; }
        public GibberishPlaybackPolicy PlaybackPolicy { get; }
        public int Priority { get; }
        public GibberishHybridSnapshot Hybrid { get; }
        public GibberishRequestFingerprint Fingerprint { get; }

        public GibberishSpeechContext(
            string text,
            string voiceName,
            string styleName,
            GibberishVoiceSnapshot voice,
            GibberishUtterance utterance,
            GibberishPlaybackPolicy playbackPolicy,
            int priority)
            : this(
                text,
                voiceName,
                styleName,
                voice,
                utterance,
                playbackPolicy,
                priority,
                GibberishHybridSnapshot.Empty,
                default)
        {
        }

        public GibberishSpeechContext(
            string text,
            string voiceName,
            string styleName,
            GibberishVoiceSnapshot voice,
            GibberishUtterance utterance,
            GibberishPlaybackPolicy playbackPolicy,
            int priority,
            GibberishHybridSnapshot hybrid,
            GibberishRequestFingerprint fingerprint)
        {
            Text = text ?? string.Empty;
            VoiceName = voiceName ?? string.Empty;
            StyleName = styleName ?? string.Empty;
            Voice = voice ?? throw new ArgumentNullException(nameof(voice));
            Utterance = utterance ?? throw new ArgumentNullException(nameof(utterance));
            PlaybackPolicy = playbackPolicy;
            Priority = priority;
            Hybrid = hybrid ?? GibberishHybridSnapshot.Empty;
            Fingerprint = fingerprint;
        }
    }

    public sealed class GibberishPreparedSpeech
    {
        public GibberishSpeechContext Context { get; }
        public GibberishAudioData Audio { get; }

        public GibberishPreparedSpeech(
            GibberishSpeechContext context,
            GibberishAudioData audio)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Audio = audio ?? throw new ArgumentNullException(nameof(audio));
        }
    }
}
