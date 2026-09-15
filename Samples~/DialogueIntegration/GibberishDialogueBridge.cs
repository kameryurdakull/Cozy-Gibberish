using UnityEngine;

namespace CozyGibberish.Integrations.Dialogue
{
    public sealed class GibberishDialogueBridge : MonoBehaviour
    {
        [SerializeField] private GibberishVoicePlayer _player;
        [SerializeField] private GibberishVoiceProfile _voice;
        [SerializeField] private GibberishStylePreset _style;
        [SerializeField] private GibberishPhonotacticProfile _phonotactics;
        [SerializeField] private GibberishProsodyProfile _prosody;
        [SerializeField] private GibberishPlaybackPolicy _policy =
            GibberishPlaybackPolicy.Interrupt;

        public GibberishPlaybackHandle SpeakLine(string text)
        {
            return _player.Speak(new GibberishSpeechRequest(
                text,
                _voice,
                _style,
                GibberishExpression.Neutral,
                playbackPolicy: _policy,
                phonotactics: _phonotactics,
                prosody: _prosody));
        }

        public void SpeakYarnCommand(string line)
        {
            SpeakLine(line);
        }

        public void SpeakInkLine(string line)
        {
            SpeakLine(line);
        }

        public void StopDialogueVoice()
        {
            _player.StopAll();
        }
    }
}
