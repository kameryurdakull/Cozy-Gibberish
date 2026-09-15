using System;

namespace CozyGibberish
{
    public readonly struct GibberishRequestFingerprint :
        IEquatable<GibberishRequestFingerprint>
    {
        private const ulong OffsetBasis = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        public ulong Value { get; }

        public GibberishRequestFingerprint(ulong value)
        {
            Value = value;
        }

        public static GibberishRequestFingerprint Create(
            string text,
            int seed,
            GibberishVoiceProfile voice,
            GibberishStylePreset style,
            GibberishStyleStack styleStack,
            GibberishPhonotacticProfile phonotactics,
            GibberishProsodyProfile prosody,
            GibberishHybridVoiceBank hybrid,
            GibberishExpression expression,
            GibberishQualityTier qualityTier,
            float tailPause)
        {
            var hash = OffsetBasis;
            hash = Add(hash, text);
            hash = Add(hash, seed);
            hash = Add(hash, voice == null ? 0 : voice.GetInstanceID());
            hash = Add(hash, style == null ? 0 : style.GetInstanceID());
            hash = Add(hash, styleStack == null ? 0 : styleStack.GetInstanceID());
            hash = Add(hash, phonotactics == null ? 0 : phonotactics.GetInstanceID());
            hash = Add(hash, prosody == null ? 0 : prosody.GetInstanceID());
            hash = Add(hash, hybrid == null ? 0 : hybrid.GetInstanceID());
            hash = Add(hash, BitConverter.SingleToInt32Bits(expression.Energy));
            hash = Add(hash, BitConverter.SingleToInt32Bits(expression.Warmth));
            hash = Add(hash, BitConverter.SingleToInt32Bits(expression.Pace));
            hash = Add(hash, BitConverter.SingleToInt32Bits(expression.Pitch));
            hash = Add(hash, (int)qualityTier);
            hash = Add(hash, BitConverter.SingleToInt32Bits(tailPause));
            return new GibberishRequestFingerprint(hash);
        }

        public bool Equals(GibberishRequestFingerprint other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is GibberishRequestFingerprint other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(
            GibberishRequestFingerprint left,
            GibberishRequestFingerprint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            GibberishRequestFingerprint left,
            GibberishRequestFingerprint right)
        {
            return !left.Equals(right);
        }

        private static ulong Add(ulong hash, int value)
        {
            var result = hash;
            unchecked
            {
                result ^= (uint)value;
                result *= Prime;
                result ^= (uint)(value >> 16);
                result *= Prime;
            }

            return result;
        }

        private static ulong Add(ulong hash, string value)
        {
            var result = hash;
            var source = value ?? string.Empty;
            unchecked
            {
                for (var index = 0; index < source.Length; index++)
                {
                    result ^= source[index];
                    result *= Prime;
                }
            }

            return result;
        }
    }
}
