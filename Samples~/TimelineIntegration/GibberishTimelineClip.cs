#if COZY_GIBBERISH_TIMELINE
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CozyGibberish.Integrations.Timeline
{
    public sealed class GibberishTimelineClip : PlayableAsset, ITimelineClipAsset
    {
        [SerializeField, TextArea] private string _text;
        [SerializeField] private GibberishVoiceProfile _voice;
        [SerializeField] private GibberishStylePreset _style;
        [SerializeField] private GibberishExpression _expression;
        [SerializeField] private int _seed;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<GibberishTimelineBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.Request = new GibberishSpeechRequest(
                _text,
                _voice,
                _style,
                _expression,
                _seed);
            return playable;
        }
    }

    public sealed class GibberishTimelineBehaviour : PlayableBehaviour
    {
        public GibberishSpeechRequest Request { get; set; }

        private bool _played;

        public override void ProcessFrame(
            Playable playable,
            FrameData info,
            object playerData)
        {
            if (_played || info.effectivePlayState != PlayState.Playing)
            {
                return;
            }

            if (playerData is GibberishVoicePlayer player)
            {
                player.Speak(Request);
                _played = true;
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            _played = false;
        }
    }

    [TrackClipType(typeof(GibberishTimelineClip))]
    [TrackBindingType(typeof(GibberishVoicePlayer))]
    public sealed class GibberishTimelineTrack : TrackAsset
    {
    }
}
#endif
