using System;
using System.Collections.Generic;
using UnityEngine;

namespace CozyGibberish
{
    [Serializable]
    public struct GibberishSpeechSegment
    {
        [SerializeField, TextArea] private string _text;
        [SerializeField] private GibberishStylePreset _style;
        [SerializeField] private GibberishExpression _expression;
        [SerializeField] private int _seed;
        [SerializeField] private int _priority;
        [SerializeField, Min(0f)] private float _pauseAfter;

        public string Text => _text ?? string.Empty;
        public GibberishStylePreset Style => _style;
        public GibberishExpression Expression => _expression;
        public int Seed => _seed;
        public int Priority => _priority;
        public float PauseAfter => Mathf.Max(0f, _pauseAfter);
    }

    [CreateAssetMenu(
        fileName = "GibberishSpeechSequence",
        menuName = "Cozy Gibberish/Speech Sequence",
        order = 17)]
    public sealed class GibberishSpeechSequence : ScriptableObject
    {
        [SerializeField] private GibberishSpeechSegment[] _segments =
            Array.Empty<GibberishSpeechSegment>();

        public IReadOnlyList<GibberishSpeechSegment> Segments => _segments;

        public GibberishSpeechRequest CreateRequest(
            int index,
            GibberishVoiceProfile defaultVoice,
            GibberishStylePreset defaultStyle,
            GibberishPhonotacticProfile phonotactics = null,
            GibberishProsodyProfile prosody = null,
            GibberishHybridVoiceBank hybrid = null)
        {
            if (_segments == null || index < 0 || index >= _segments.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var segment = _segments[index];
            return new GibberishSpeechRequest(
                segment.Text,
                defaultVoice,
                segment.Style != null ? segment.Style : defaultStyle,
                segment.Expression,
                segment.Seed,
                segment.Priority,
                index == 0
                    ? GibberishPlaybackPolicy.Interrupt
                    : GibberishPlaybackPolicy.Enqueue,
                phonotactics,
                null,
                prosody,
                hybrid,
                segment.PauseAfter);
        }
    }
}
