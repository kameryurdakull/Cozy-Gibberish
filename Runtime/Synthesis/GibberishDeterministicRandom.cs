using System;

namespace CozyGibberish
{
    internal struct GibberishDeterministicRandom
    {
        private const uint FallbackState = 0xA341316Cu;
        private uint _state;

        public GibberishDeterministicRandom(uint seed)
        {
            _state = seed == 0u ? FallbackState : seed;
        }

        public uint NextUInt()
        {
            var value = _state;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            _state = value;
            return value;
        }

        public float Next01()
        {
            return (NextUInt() & 0x00FFFFFFu) / 16777215f;
        }

        public float NextSigned()
        {
            return Next01() * 2f - 1f;
        }

        public static int CombineSeed(string text, int seed)
        {
            const uint offsetBasis = 2166136261u;
            const uint prime = 16777619u;

            var hash = offsetBasis ^ (uint)seed;
            var source = text ?? string.Empty;
            for (var index = 0; index < source.Length; index++)
            {
                hash ^= source[index];
                hash *= prime;
            }

            return unchecked((int)(hash == 0u ? FallbackState : hash));
        }
    }
}
