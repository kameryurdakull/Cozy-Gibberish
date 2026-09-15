using System;
using UnityEngine;

namespace CozyGibberish
{
    /// <summary>
    /// Applies situational delivery changes without duplicating a character voice profile.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GibberishStyle",
        menuName = "Cozy Gibberish/Style Preset",
        order = 11)]
    public sealed class GibberishStylePreset : ScriptableObject, ISerializationCallbackReceiver
    {
        private const int CurrentSchemaVersion = 2;
        private const string StableIdFormat = "N";

        [SerializeField, HideInInspector] private int _schemaVersion = CurrentSchemaVersion;
        [SerializeField, HideInInspector] private string _stableId;

        [Header("Delivery")]
        [SerializeField, Range(0.5f, 2f)] private float _paceMultiplier = 1f;
        [SerializeField, Range(-12f, 12f)] private float _pitchOffsetSemitones;
        [SerializeField, Range(0f, 2f)] private float _pitchVariationMultiplier = 1f;
        [SerializeField, Range(0.25f, 2f)] private float _articulationMultiplier = 1f;

        [Header("Timbre")]
        [SerializeField, Range(-1f, 1f)] private float _warmthOffset;
        [SerializeField, Range(-1f, 1f)] private float _brightnessOffset;
        [SerializeField, Range(-1f, 1f)] private float _breathinessOffset;
        [SerializeField, Range(0.5f, 1.5f)] private float _formantShift = 1f;

        [Header("Motion")]
        [SerializeField, Range(0f, 3f)] private float _vibratoMultiplier = 1f;
        [SerializeField, Range(0f, 2f)] private float _volumeMultiplier = 1f;
        [SerializeField, Range(0f, 2f)] private float _variationMultiplier = 1f;

        public float PaceMultiplier => _paceMultiplier;
        public float PitchOffsetSemitones => _pitchOffsetSemitones;
        public float PitchVariationMultiplier => _pitchVariationMultiplier;
        public float ArticulationMultiplier => _articulationMultiplier;
        public float WarmthOffset => _warmthOffset;
        public float BrightnessOffset => _brightnessOffset;
        public float BreathinessOffset => _breathinessOffset;
        public float FormantShift => _formantShift;
        public float VibratoMultiplier => _vibratoMultiplier;
        public float VolumeMultiplier => _volumeMultiplier;
        public float VariationMultiplier => _variationMultiplier;
        public string StableId => _stableId;
        public int SchemaVersion => _schemaVersion;

        public void ApplyArchetype(GibberishStyleArchetype archetype)
        {
            switch (archetype)
            {
                case GibberishStyleArchetype.Cozy:
                    SetValues(0.88f, -1.5f, 0.72f, 0.85f, 0.25f, -0.18f, 0.12f, 0.96f, 0.7f, 0.92f, 0.7f);
                    break;
                case GibberishStyleArchetype.Energetic:
                    SetValues(1.28f, 2.5f, 1.35f, 1.3f, -0.15f, 0.25f, -0.15f, 1.05f, 1.35f, 1.05f, 1.25f);
                    break;
                case GibberishStyleArchetype.Mechanical:
                    SetValues(1.05f, -3f, 0.22f, 1.4f, -0.38f, 0.48f, -0.35f, 0.86f, 0.12f, 0.92f, 0.32f);
                    break;
                case GibberishStyleArchetype.Mysterious:
                    SetValues(0.74f, -5f, 0.6f, 0.72f, 0.18f, -0.28f, 0.28f, 0.88f, 1.4f, 0.88f, 0.9f);
                    break;
            }
        }

        private void SetValues(
            float pace,
            float pitchOffset,
            float pitchVariation,
            float articulation,
            float warmth,
            float brightness,
            float breathiness,
            float formantShift,
            float vibrato,
            float volume,
            float variation)
        {
            _paceMultiplier = pace;
            _pitchOffsetSemitones = pitchOffset;
            _pitchVariationMultiplier = pitchVariation;
            _articulationMultiplier = articulation;
            _warmthOffset = warmth;
            _brightnessOffset = brightness;
            _breathinessOffset = breathiness;
            _formantShift = formantShift;
            _vibratoMultiplier = vibrato;
            _volumeMultiplier = volume;
            _variationMultiplier = variation;
        }

        private void OnValidate()
        {
            ValidateAndMigrate();
            _paceMultiplier = Mathf.Clamp(_paceMultiplier, 0.5f, 2f);
            _formantShift = Mathf.Clamp(_formantShift, 0.5f, 1.5f);
            _volumeMultiplier = Mathf.Clamp(_volumeMultiplier, 0f, 2f);
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
    }
}
