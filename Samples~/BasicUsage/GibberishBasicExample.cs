using UnityEngine;

namespace CozyGibberish.Samples
{
    public sealed class GibberishBasicExample : MonoBehaviour
    {
        [SerializeField] private GibberishVoicePlayer _player;
        [SerializeField] private GibberishVoiceProfile _voice;
        [SerializeField] private GibberishStylePreset _cozyStyle;
        [SerializeField] private GibberishStylePreset _energeticStyle;

        [ContextMenu("Speak Cozy")]
        public void SpeakCozy()
        {
            var request = new GibberishSpeechRequest(
                "The kettle is singing. Come sit by the fire!",
                _voice,
                _cozyStyle,
                new GibberishExpression(-0.15f, 0.4f, -0.15f, -0.05f),
                seed: 42);
            _player.Speak(request);
        }

        [ContextMenu("Speak Energetic")]
        public void SpeakEnergetic()
        {
            var request = new GibberishSpeechRequest(
                "We found the secret garden!",
                _voice,
                _energeticStyle,
                new GibberishExpression(0.85f, 0f, 0.65f, 0.35f),
                seed: 73,
                priority: 10,
                playbackPolicy: GibberishPlaybackPolicy.Interrupt);
            _player.Speak(request);
        }
    }
}
