using System;
using UnityEngine;

namespace CozyGibberish
{
    [CreateAssetMenu(
        fileName = "GibberishPhonotactics",
        menuName = "Cozy Gibberish/Phonotactic Profile",
        order = 12)]
    public sealed class GibberishPhonotacticProfile : ScriptableObject, ISerializationCallbackReceiver
    {
        private const int CurrentSchemaVersion = 1;
        private const string StableIdFormat = "N";

        [SerializeField, HideInInspector] private int _schemaVersion = CurrentSchemaVersion;
        [SerializeField, HideInInspector] private string _stableId;

        [Header("Language Character")]
        [SerializeField] private GibberishTokenizerMode _tokenizerMode = GibberishTokenizerMode.Universal;
        [SerializeField] private GibberishSyllablePattern _pattern = GibberishSyllablePattern.Mixed;
        [SerializeField, Range(0f, 1f)] private float _vowelHarmony = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _codaProbability = 0.28f;
        [SerializeField, Range(0.5f, 1.5f)] private float _syllableDensity = 1f;
        [SerializeField] private bool _avoidImmediateRepetition = true;

        [Header("Onsets")]
        [SerializeField] private GibberishConsonantWeight[] _onsets = CreateCozyOnsets();

        [Header("Codas")]
        [SerializeField] private GibberishConsonantWeight[] _codas = CreateCozyCodas();

        public string StableId => _stableId;
        public int SchemaVersion => _schemaVersion;

        public GibberishPhonotacticSnapshot CreateSnapshot()
        {
            return new GibberishPhonotacticSnapshot(
                _tokenizerMode,
                _pattern,
                _vowelHarmony,
                _codaProbability,
                _syllableDensity,
                _avoidImmediateRepetition,
                Copy(_onsets, CreateCozyOnsets()),
                Copy(_codas, CreateCozyCodas()));
        }

        public void ApplyArchetype(GibberishVoiceArchetype archetype)
        {
            switch (archetype)
            {
                case GibberishVoiceArchetype.Whimsical:
                    _tokenizerMode = GibberishTokenizerMode.Fantasy;
                    _pattern = GibberishSyllablePattern.ConsonantVowel;
                    _vowelHarmony = 0.18f;
                    _codaProbability = 0.12f;
                    _syllableDensity = 1.18f;
                    _onsets = CreateWhimsicalOnsets();
                    _codas = CreateCozyCodas();
                    break;
                case GibberishVoiceArchetype.Mechanical:
                    _tokenizerMode = GibberishTokenizerMode.Universal;
                    _pattern = GibberishSyllablePattern.ConsonantVowelConsonant;
                    _vowelHarmony = 0f;
                    _codaProbability = 0.72f;
                    _syllableDensity = 1.25f;
                    _onsets = CreateMechanicalOnsets();
                    _codas = CreateMechanicalCodas();
                    break;
                case GibberishVoiceArchetype.Mystic:
                    _tokenizerMode = GibberishTokenizerMode.Fantasy;
                    _pattern = GibberishSyllablePattern.Mixed;
                    _vowelHarmony = 0.72f;
                    _codaProbability = 0.4f;
                    _syllableDensity = 0.82f;
                    _onsets = CreateMysticOnsets();
                    _codas = CreateMysticCodas();
                    break;
                default:
                    _tokenizerMode = GibberishTokenizerMode.Universal;
                    _pattern = GibberishSyllablePattern.Mixed;
                    _vowelHarmony = 0.35f;
                    _codaProbability = 0.28f;
                    _syllableDensity = 1f;
                    _onsets = CreateCozyOnsets();
                    _codas = CreateCozyCodas();
                    break;
            }
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (_schemaVersion < CurrentSchemaVersion)
            {
                _schemaVersion = CurrentSchemaVersion;
            }
        }

        private void OnValidate()
        {
            ValidateAndMigrate();
            _vowelHarmony = Mathf.Clamp01(_vowelHarmony);
            _codaProbability = Mathf.Clamp01(_codaProbability);
            _syllableDensity = Mathf.Clamp(_syllableDensity, 0.5f, 1.5f);

            if (_onsets == null || _onsets.Length == 0)
            {
                _onsets = CreateCozyOnsets();
            }

            if (_codas == null || _codas.Length == 0)
            {
                _codas = CreateCozyCodas();
            }
        }

        public bool ValidateAndMigrate()
        {
            var changed = false;
            if (string.IsNullOrWhiteSpace(_stableId))
            {
                _stableId = Guid.NewGuid().ToString(StableIdFormat);
                changed = true;
            }

            if (_schemaVersion != CurrentSchemaVersion)
            {
                _schemaVersion = CurrentSchemaVersion;
                changed = true;
            }

            return changed;
        }

        private static GibberishConsonantWeight[] Copy(
            GibberishConsonantWeight[] source,
            GibberishConsonantWeight[] fallback)
        {
            var safeSource = source == null || source.Length == 0 ? fallback : source;
            var result = new GibberishConsonantWeight[safeSource.Length];
            Array.Copy(safeSource, result, safeSource.Length);
            return result;
        }

        private static GibberishConsonantWeight[] CreateCozyOnsets()
        {
            return new[]
            {
                new GibberishConsonantWeight(GibberishConsonantId.SoftM, 1.2f),
                new GibberishConsonantWeight(GibberishConsonantId.SoftL, 1.1f),
                new GibberishConsonantWeight(GibberishConsonantId.SoftN, 1f),
                new GibberishConsonantWeight(GibberishConsonantId.SoftW, 0.8f),
                new GibberishConsonantWeight(GibberishConsonantId.SoftB, 0.7f),
                new GibberishConsonantWeight(GibberishConsonantId.SoftY, 0.65f)
            };
        }

        private static GibberishConsonantWeight[] CreateCozyCodas()
        {
            return new[]
            {
                new GibberishConsonantWeight(GibberishConsonantId.SoftM, 1.1f),
                new GibberishConsonantWeight(GibberishConsonantId.SoftN, 1f),
                new GibberishConsonantWeight(GibberishConsonantId.SoftL, 0.8f),
                new GibberishConsonantWeight(GibberishConsonantId.HushS, 0.35f)
            };
        }

        private static GibberishConsonantWeight[] CreateWhimsicalOnsets()
        {
            return new[]
            {
                new GibberishConsonantWeight(GibberishConsonantId.SoftY, 1.25f),
                new GibberishConsonantWeight(GibberishConsonantId.SoftW, 1.1f),
                new GibberishConsonantWeight(GibberishConsonantId.BrightP, 0.85f),
                new GibberishConsonantWeight(GibberishConsonantId.BrightT, 0.8f),
                new GibberishConsonantWeight(GibberishConsonantId.SoftL, 0.7f)
            };
        }

        private static GibberishConsonantWeight[] CreateMechanicalOnsets()
        {
            return new[]
            {
                new GibberishConsonantWeight(GibberishConsonantId.MechanicalZ, 1.3f),
                new GibberishConsonantWeight(GibberishConsonantId.BrightK, 1.1f),
                new GibberishConsonantWeight(GibberishConsonantId.BrightT, 1f),
                new GibberishConsonantWeight(GibberishConsonantId.BrightP, 0.8f)
            };
        }

        private static GibberishConsonantWeight[] CreateMechanicalCodas()
        {
            return new[]
            {
                new GibberishConsonantWeight(GibberishConsonantId.BrightK, 1.2f),
                new GibberishConsonantWeight(GibberishConsonantId.BrightT, 1.1f),
                new GibberishConsonantWeight(GibberishConsonantId.MechanicalZ, 0.9f)
            };
        }

        private static GibberishConsonantWeight[] CreateMysticOnsets()
        {
            return new[]
            {
                new GibberishConsonantWeight(GibberishConsonantId.HushSh, 1.25f),
                new GibberishConsonantWeight(GibberishConsonantId.RolledR, 1f),
                new GibberishConsonantWeight(GibberishConsonantId.SoftL, 0.9f),
                new GibberishConsonantWeight(GibberishConsonantId.HushS, 0.85f),
                new GibberishConsonantWeight(GibberishConsonantId.SoftY, 0.6f)
            };
        }

        private static GibberishConsonantWeight[] CreateMysticCodas()
        {
            return new[]
            {
                new GibberishConsonantWeight(GibberishConsonantId.HushS, 1.2f),
                new GibberishConsonantWeight(GibberishConsonantId.SoftN, 0.9f),
                new GibberishConsonantWeight(GibberishConsonantId.RolledR, 0.75f),
                new GibberishConsonantWeight(GibberishConsonantId.SoftL, 0.65f)
            };
        }
    }

    public sealed class GibberishPhonotacticSnapshot
    {
        private readonly GibberishConsonantWeight[] _onsets;
        private readonly GibberishConsonantWeight[] _codas;

        public static GibberishPhonotacticSnapshot Default { get; } =
            new GibberishPhonotacticProfileFallback().Create();

        public GibberishTokenizerMode TokenizerMode { get; }
        public GibberishSyllablePattern Pattern { get; }
        public float VowelHarmony { get; }
        public float CodaProbability { get; }
        public float SyllableDensity { get; }
        public bool AvoidImmediateRepetition { get; }

        public GibberishPhonotacticSnapshot(
            GibberishTokenizerMode tokenizerMode,
            GibberishSyllablePattern pattern,
            float vowelHarmony,
            float codaProbability,
            float syllableDensity,
            bool avoidImmediateRepetition,
            GibberishConsonantWeight[] onsets,
            GibberishConsonantWeight[] codas)
        {
            TokenizerMode = tokenizerMode;
            Pattern = pattern;
            VowelHarmony = Mathf.Clamp01(vowelHarmony);
            CodaProbability = Mathf.Clamp01(codaProbability);
            SyllableDensity = Mathf.Clamp(syllableDensity, 0.5f, 1.5f);
            AvoidImmediateRepetition = avoidImmediateRepetition;
            _onsets = onsets ?? Array.Empty<GibberishConsonantWeight>();
            _codas = codas ?? Array.Empty<GibberishConsonantWeight>();
        }

        internal GibberishConsonantId SelectOnset(
            GibberishConsonantId previous,
            ref GibberishDeterministicRandom random)
        {
            if (Pattern == GibberishSyllablePattern.Vowel)
            {
                return GibberishConsonantId.None;
            }

            if (Pattern == GibberishSyllablePattern.Mixed && random.Next01() < 0.2f)
            {
                return GibberishConsonantId.None;
            }

            return Select(_onsets, previous, ref random);
        }

        internal GibberishConsonantId SelectCoda(
            GibberishConsonantId previous,
            ref GibberishDeterministicRandom random)
        {
            var requiresCoda = Pattern == GibberishSyllablePattern.ConsonantVowelConsonant;
            if (!requiresCoda && random.Next01() > CodaProbability)
            {
                return GibberishConsonantId.None;
            }

            return Select(_codas, previous, ref random);
        }

        private GibberishConsonantId Select(
            GibberishConsonantWeight[] source,
            GibberishConsonantId previous,
            ref GibberishDeterministicRandom random)
        {
            if (source.Length == 0)
            {
                return GibberishConsonantId.None;
            }

            var totalWeight = 0f;
            for (var index = 0; index < source.Length; index++)
            {
                if (AvoidImmediateRepetition && source[index].Consonant == previous)
                {
                    continue;
                }

                totalWeight += source[index].Weight;
            }

            if (totalWeight <= 0f)
            {
                return source[0].Consonant;
            }

            var selection = random.Next01() * totalWeight;
            for (var index = 0; index < source.Length; index++)
            {
                var candidate = source[index];
                if (AvoidImmediateRepetition && candidate.Consonant == previous)
                {
                    continue;
                }

                selection -= candidate.Weight;
                if (selection <= 0f)
                {
                    return candidate.Consonant;
                }
            }

            return source[source.Length - 1].Consonant;
        }

        private sealed class GibberishPhonotacticProfileFallback
        {
            public GibberishPhonotacticSnapshot Create()
            {
                var onsets = new[]
                {
                    new GibberishConsonantWeight(GibberishConsonantId.SoftM, 1f),
                    new GibberishConsonantWeight(GibberishConsonantId.SoftL, 1f),
                    new GibberishConsonantWeight(GibberishConsonantId.SoftN, 1f)
                };
                var codas = new[]
                {
                    new GibberishConsonantWeight(GibberishConsonantId.SoftM, 1f),
                    new GibberishConsonantWeight(GibberishConsonantId.SoftN, 1f)
                };
                return new GibberishPhonotacticSnapshot(
                    GibberishTokenizerMode.Universal,
                    GibberishSyllablePattern.Mixed,
                    0.35f,
                    0.28f,
                    1f,
                    true,
                    onsets,
                    codas);
            }
        }
    }
}
