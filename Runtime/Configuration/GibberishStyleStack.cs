using System;
using UnityEngine;

namespace CozyGibberish
{
    [Serializable]
    public struct GibberishStyleLayer
    {
        [SerializeField] private GibberishStylePreset _preset;
        [SerializeField, Range(0f, 1f)] private float _weight;

        public GibberishStylePreset Preset => _preset;
        public float Weight => Mathf.Clamp01(_weight);

        public GibberishStyleLayer(GibberishStylePreset preset, float weight)
        {
            _preset = preset;
            _weight = weight;
        }
    }

    [CreateAssetMenu(
        fileName = "GibberishStyleStack",
        menuName = "Cozy Gibberish/Style Stack",
        order = 13)]
    public sealed class GibberishStyleStack : ScriptableObject
    {
        [SerializeField] private GibberishStyleLayer[] _layers = Array.Empty<GibberishStyleLayer>();

        public void SetLayers(GibberishStylePreset[] presets)
        {
            var safePresets = presets ?? Array.Empty<GibberishStylePreset>();
            _layers = new GibberishStyleLayer[safePresets.Length];
            for (var index = 0; index < safePresets.Length; index++)
            {
                _layers[index] = new GibberishStyleLayer(
                    safePresets[index],
                    0f);
            }
        }

        public GibberishStyleInfluence CreateInfluence(GibberishStylePreset fallback = null)
        {
            var influence = GibberishStyleInfluence.Neutral;
            if (fallback != null)
            {
                influence = influence.Blend(GibberishStyleInfluence.From(fallback), 1f);
            }

            if (_layers == null)
            {
                return influence;
            }

            for (var index = 0; index < _layers.Length; index++)
            {
                var layer = _layers[index];
                if (layer.Preset == null || layer.Weight <= 0f)
                {
                    continue;
                }

                influence = influence.Blend(
                    GibberishStyleInfluence.From(layer.Preset),
                    layer.Weight);
            }

            return influence;
        }
    }

    public readonly struct GibberishStyleInfluence
    {
        public static GibberishStyleInfluence Neutral { get; } =
            new GibberishStyleInfluence(1f, 0f, 1f, 1f, 0f, 0f, 0f, 1f, 1f, 1f, 1f);

        public float PaceMultiplier { get; }
        public float PitchOffsetSemitones { get; }
        public float PitchVariationMultiplier { get; }
        public float ArticulationMultiplier { get; }
        public float WarmthOffset { get; }
        public float BrightnessOffset { get; }
        public float BreathinessOffset { get; }
        public float FormantShift { get; }
        public float VibratoMultiplier { get; }
        public float VolumeMultiplier { get; }
        public float VariationMultiplier { get; }

        public GibberishStyleInfluence(
            float paceMultiplier,
            float pitchOffsetSemitones,
            float pitchVariationMultiplier,
            float articulationMultiplier,
            float warmthOffset,
            float brightnessOffset,
            float breathinessOffset,
            float formantShift,
            float vibratoMultiplier,
            float volumeMultiplier,
            float variationMultiplier)
        {
            PaceMultiplier = paceMultiplier;
            PitchOffsetSemitones = pitchOffsetSemitones;
            PitchVariationMultiplier = pitchVariationMultiplier;
            ArticulationMultiplier = articulationMultiplier;
            WarmthOffset = warmthOffset;
            BrightnessOffset = brightnessOffset;
            BreathinessOffset = breathinessOffset;
            FormantShift = formantShift;
            VibratoMultiplier = vibratoMultiplier;
            VolumeMultiplier = volumeMultiplier;
            VariationMultiplier = variationMultiplier;
        }

        public static GibberishStyleInfluence From(GibberishStylePreset preset)
        {
            if (preset == null)
            {
                return Neutral;
            }

            return new GibberishStyleInfluence(
                preset.PaceMultiplier,
                preset.PitchOffsetSemitones,
                preset.PitchVariationMultiplier,
                preset.ArticulationMultiplier,
                preset.WarmthOffset,
                preset.BrightnessOffset,
                preset.BreathinessOffset,
                preset.FormantShift,
                preset.VibratoMultiplier,
                preset.VolumeMultiplier,
                preset.VariationMultiplier);
        }

        public GibberishStyleInfluence Blend(GibberishStyleInfluence other, float weight)
        {
            var amount = Mathf.Clamp01(weight);
            return new GibberishStyleInfluence(
                PaceMultiplier * Mathf.Lerp(1f, other.PaceMultiplier, amount),
                PitchOffsetSemitones + other.PitchOffsetSemitones * amount,
                PitchVariationMultiplier * Mathf.Lerp(1f, other.PitchVariationMultiplier, amount),
                ArticulationMultiplier * Mathf.Lerp(1f, other.ArticulationMultiplier, amount),
                WarmthOffset + other.WarmthOffset * amount,
                BrightnessOffset + other.BrightnessOffset * amount,
                BreathinessOffset + other.BreathinessOffset * amount,
                FormantShift * Mathf.Lerp(1f, other.FormantShift, amount),
                VibratoMultiplier * Mathf.Lerp(1f, other.VibratoMultiplier, amount),
                VolumeMultiplier * Mathf.Lerp(1f, other.VolumeMultiplier, amount),
                VariationMultiplier * Mathf.Lerp(1f, other.VariationMultiplier, amount));
        }
    }
}
