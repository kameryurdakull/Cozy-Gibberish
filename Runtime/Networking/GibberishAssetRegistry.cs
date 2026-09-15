using System;
using UnityEngine;

namespace CozyGibberish
{
    [CreateAssetMenu(
        fileName = "GibberishAssetRegistry",
        menuName = "Cozy Gibberish/Network Asset Registry",
        order = 18)]
    public sealed class GibberishAssetRegistry : ScriptableObject, IGibberishAssetResolver
    {
        [SerializeField] private GibberishVoiceProfile[] _voices =
            Array.Empty<GibberishVoiceProfile>();
        [SerializeField] private GibberishStylePreset[] _styles =
            Array.Empty<GibberishStylePreset>();

        public GibberishVoiceProfile ResolveVoice(string stableId)
        {
            for (var index = 0; index < _voices.Length; index++)
            {
                var voice = _voices[index];
                if (voice != null && voice.StableId == stableId)
                {
                    return voice;
                }
            }

            return null;
        }

        public GibberishStylePreset ResolveStyle(string stableId)
        {
            for (var index = 0; index < _styles.Length; index++)
            {
                var style = _styles[index];
                if (style != null && style.StableId == stableId)
                {
                    return style;
                }
            }

            return null;
        }
    }
}
