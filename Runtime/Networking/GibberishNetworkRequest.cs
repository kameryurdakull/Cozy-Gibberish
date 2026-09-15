using System;
using UnityEngine;

namespace CozyGibberish
{
    public interface IGibberishAssetResolver
    {
        GibberishVoiceProfile ResolveVoice(string stableId);
        GibberishStylePreset ResolveStyle(string stableId);
    }

    [Serializable]
    public struct GibberishNetworkRequest
    {
        [SerializeField] private string _text;
        [SerializeField] private string _voiceId;
        [SerializeField] private string _styleId;
        [SerializeField] private float _energy;
        [SerializeField] private float _warmth;
        [SerializeField] private float _pace;
        [SerializeField] private float _pitch;
        [SerializeField] private int _seed;
        [SerializeField] private int _priority;
        [SerializeField] private GibberishPlaybackPolicy _playbackPolicy;
        [SerializeField] private float _tailPause;

        public string Text => _text ?? string.Empty;
        public string VoiceId => _voiceId ?? string.Empty;
        public string StyleId => _styleId ?? string.Empty;
        public int Seed => _seed;

        public static GibberishNetworkRequest From(GibberishSpeechRequest request)
        {
            return new GibberishNetworkRequest
            {
                _text = request.Text,
                _voiceId = request.Voice == null ? string.Empty : request.Voice.StableId,
                _styleId = request.Style == null ? string.Empty : request.Style.StableId,
                _energy = request.Expression.Energy,
                _warmth = request.Expression.Warmth,
                _pace = request.Expression.Pace,
                _pitch = request.Expression.Pitch,
                _seed = request.Seed,
                _priority = request.Priority,
                _playbackPolicy = request.PlaybackPolicy,
                _tailPause = request.TailPause
            };
        }

        public GibberishSpeechRequest Resolve(IGibberishAssetResolver resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            var voice = resolver.ResolveVoice(VoiceId);
            var style = resolver.ResolveStyle(StyleId);
            return new GibberishSpeechRequest(
                Text,
                voice,
                style,
                new GibberishExpression(_energy, _warmth, _pace, _pitch),
                _seed,
                _priority,
                _playbackPolicy,
                tailPause: _tailPause);
        }
    }

    public static class GibberishNetworkCodec
    {
        public static string Serialize(GibberishNetworkRequest request)
        {
            return JsonUtility.ToJson(request);
        }

        public static GibberishNetworkRequest Deserialize(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                throw new ArgumentException("Network payload cannot be empty.", nameof(payload));
            }

            return JsonUtility.FromJson<GibberishNetworkRequest>(payload);
        }
    }
}
