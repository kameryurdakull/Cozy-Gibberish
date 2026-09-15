using System;
using UnityEngine;

namespace CozyGibberish
{
    public enum GibberishWaveform
    {
        Sine,
        SoftTriangle,
        RoundedSquare,
        WarmSaw
    }

    public enum GibberishVoiceArchetype
    {
        Cozy,
        Whimsical,
        Mechanical,
        Mystic
    }

    public enum GibberishStyleArchetype
    {
        Cozy,
        Energetic,
        Mechanical,
        Mysterious
    }

    public enum GibberishVowelId
    {
        Ah,
        Eh,
        Ee,
        Oh,
        Oo
    }

    public enum GibberishPlaybackPolicy
    {
        Interrupt,
        Enqueue,
        IgnoreIfSpeaking
    }

    public enum GibberishStopReason
    {
        Completed,
        Interrupted,
        Cancelled,
        Disabled
    }

    public enum GibberishOversampling
    {
        Off = 1,
        TwoTimes = 2,
        FourTimes = 4
    }

    public enum GibberishConsonantId
    {
        None,
        SoftB,
        SoftM,
        SoftL,
        SoftN,
        SoftW,
        SoftY,
        BrightK,
        BrightT,
        BrightP,
        HushS,
        HushSh,
        RolledR,
        MechanicalZ
    }

    public enum GibberishMouthShape
    {
        Rest,
        Closed,
        Wide,
        Open,
        Round,
        Narrow
    }

    public enum GibberishTokenizerMode
    {
        Universal,
        TurkishLike,
        EnglishLike,
        Fantasy
    }

    public enum GibberishSyllablePattern
    {
        Vowel,
        ConsonantVowel,
        ConsonantVowelConsonant,
        Mixed
    }

    public enum GibberishQueueOverflowPolicy
    {
        RejectNewest,
        DropOldest,
        DropLowestPriority
    }

    public enum GibberishPlaybackBackend
    {
        AudioClip,
        Streaming
    }

    public enum GibberishQualityTier
    {
        Mobile,
        Balanced,
        Cinematic
    }

    [Serializable]
    public struct GibberishFloatRange
    {
        [SerializeField] private float _minimum;
        [SerializeField] private float _maximum;

        public float Minimum => Mathf.Min(_minimum, _maximum);
        public float Maximum => Mathf.Max(_minimum, _maximum);

        public GibberishFloatRange(float minimum, float maximum)
        {
            _minimum = minimum;
            _maximum = maximum;
        }

        public float Lerp(float value)
        {
            return Mathf.Lerp(Minimum, Maximum, Mathf.Clamp01(value));
        }

        public GibberishFloatRange Clamped(float minimum, float maximum)
        {
            var low = Mathf.Clamp(Minimum, minimum, maximum);
            var high = Mathf.Clamp(Maximum, minimum, maximum);
            return new GibberishFloatRange(low, high);
        }
    }

    [Serializable]
    public struct GibberishVowelDefinition
    {
        [SerializeField] private GibberishVowelId _id;
        [SerializeField, Range(0.01f, 4f)] private float _weight;
        [SerializeField, Min(100f)] private float _firstFormant;
        [SerializeField, Min(200f)] private float _secondFormant;
        [SerializeField, Min(400f)] private float _thirdFormant;

        public GibberishVowelId Id => _id;
        public float Weight => Mathf.Max(0.01f, _weight);
        public float FirstFormant => Mathf.Max(100f, _firstFormant);
        public float SecondFormant => Mathf.Max(FirstFormant + 100f, _secondFormant);
        public float ThirdFormant => Mathf.Max(SecondFormant + 200f, _thirdFormant);

        public GibberishVowelDefinition(
            GibberishVowelId id,
            float weight,
            float firstFormant,
            float secondFormant,
            float thirdFormant)
        {
            _id = id;
            _weight = weight;
            _firstFormant = firstFormant;
            _secondFormant = secondFormant;
            _thirdFormant = thirdFormant;
        }
    }

    [Serializable]
    public struct GibberishConsonantWeight
    {
        [SerializeField] private GibberishConsonantId _consonant;
        [SerializeField, Range(0.01f, 4f)] private float _weight;

        public GibberishConsonantId Consonant => _consonant;
        public float Weight => Mathf.Max(0.01f, _weight);

        public GibberishConsonantWeight(GibberishConsonantId consonant, float weight)
        {
            _consonant = consonant;
            _weight = weight;
        }
    }

    [Serializable]
    public struct GibberishExpression
    {
        [SerializeField, Range(-1f, 1f)] private float _energy;
        [SerializeField, Range(-1f, 1f)] private float _warmth;
        [SerializeField, Range(-1f, 1f)] private float _pace;
        [SerializeField, Range(-1f, 1f)] private float _pitch;

        public static GibberishExpression Neutral => new GibberishExpression(0f, 0f, 0f, 0f);

        public float Energy => Mathf.Clamp(_energy, -1f, 1f);
        public float Warmth => Mathf.Clamp(_warmth, -1f, 1f);
        public float Pace => Mathf.Clamp(_pace, -1f, 1f);
        public float Pitch => Mathf.Clamp(_pitch, -1f, 1f);

        public GibberishExpression(float energy, float warmth, float pace, float pitch)
        {
            _energy = energy;
            _warmth = warmth;
            _pace = pace;
            _pitch = pitch;
        }
    }
}
