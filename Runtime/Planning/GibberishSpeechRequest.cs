using System;

namespace CozyGibberish
{
    /// <summary>
    /// Immutable per-utterance input. Voice and style fall back to the player's defaults when omitted.
    /// </summary>
    public readonly struct GibberishSpeechRequest
    {
        public string Text { get; }
        public GibberishVoiceProfile Voice { get; }
        public GibberishStylePreset Style { get; }
        public GibberishExpression Expression { get; }
        public int Seed { get; }
        public int Priority { get; }
        public GibberishPlaybackPolicy PlaybackPolicy { get; }
        public GibberishPhonotacticProfile Phonotactics { get; }
        public GibberishStyleStack StyleStack { get; }
        public GibberishProsodyProfile Prosody { get; }
        public GibberishHybridVoiceBank HybridVoiceBank { get; }
        public float TailPause { get; }

        public GibberishSpeechRequest(
            string text,
            GibberishVoiceProfile voice = null,
            GibberishStylePreset style = null,
            GibberishExpression? expression = null,
            int seed = 0,
            int priority = 0,
            GibberishPlaybackPolicy playbackPolicy = GibberishPlaybackPolicy.Interrupt,
            GibberishPhonotacticProfile phonotactics = null,
            GibberishStyleStack styleStack = null,
            GibberishProsodyProfile prosody = null,
            GibberishHybridVoiceBank hybridVoiceBank = null,
            float tailPause = 0f)
        {
            Text = text ?? string.Empty;
            Voice = voice;
            Style = style;
            Expression = expression ?? GibberishExpression.Neutral;
            Seed = seed;
            Priority = priority;
            PlaybackPolicy = playbackPolicy;
            Phonotactics = phonotactics;
            StyleStack = styleStack;
            Prosody = prosody;
            HybridVoiceBank = hybridVoiceBank;
            TailPause = Math.Max(0f, tailPause);
        }
    }

    public readonly struct GibberishPlaybackHandle : IEquatable<GibberishPlaybackHandle>
    {
        public static GibberishPlaybackHandle Invalid => default;

        public long Id { get; }
        public bool IsValid => Id > 0;

        internal GibberishPlaybackHandle(long id)
        {
            Id = id;
        }

        public bool Equals(GibberishPlaybackHandle other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is GibberishPlaybackHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(GibberishPlaybackHandle left, GibberishPlaybackHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GibberishPlaybackHandle left, GibberishPlaybackHandle right)
        {
            return !left.Equals(right);
        }
    }
}
