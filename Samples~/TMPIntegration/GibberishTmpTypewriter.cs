#if COZY_GIBBERISH_TMP
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CozyGibberish.Integrations.TMP
{
    public sealed class GibberishTmpTypewriter : MonoBehaviour
    {
        [SerializeField] private GibberishVoicePlayer _player;
        [SerializeField] private TMP_Text _text;

        private readonly List<PendingWord> _pending = new List<PendingWord>();
        private IDisposable _startedSubscription;
        private IDisposable _wordSubscription;
        private IDisposable _stoppedSubscription;
        private GibberishPlaybackHandle _activeHandle;

        private void OnEnable()
        {
            _startedSubscription =
                _player.Events.Subscribe<GibberishSpeechStartedEvent>(OnSpeechStarted);
            _wordSubscription =
                _player.Events.Subscribe<GibberishWordStartedEvent>(OnWordStarted);
            _stoppedSubscription =
                _player.Events.Subscribe<GibberishSpeechStoppedEvent>(OnSpeechStopped);
        }

        private void Update()
        {
            var now = AudioSettings.dspTime;
            for (var index = _pending.Count - 1; index >= 0; index--)
            {
                var pending = _pending[index];
                if (pending.DspTime > now)
                {
                    continue;
                }

                _text.maxVisibleCharacters = Mathf.Max(
                    _text.maxVisibleCharacters,
                    ResolveVisibleCharacterCount(pending.SourceEnd));
                _pending.RemoveAt(index);
            }
        }

        private void OnDisable()
        {
            _startedSubscription?.Dispose();
            _wordSubscription?.Dispose();
            _stoppedSubscription?.Dispose();
            _pending.Clear();
        }

        private void OnSpeechStarted(GibberishSpeechStartedEvent eventData)
        {
            _activeHandle = eventData.Handle;
            _pending.Clear();
            _text.text = eventData.Text;
            _text.maxVisibleCharacters = 0;
            _text.ForceMeshUpdate();
        }

        private void OnWordStarted(GibberishWordStartedEvent eventData)
        {
            if (eventData.Handle != _activeHandle)
            {
                return;
            }

            _pending.Add(new PendingWord(
                eventData.SourceStart + eventData.SourceLength,
                eventData.DspTime));
        }

        private void OnSpeechStopped(GibberishSpeechStoppedEvent eventData)
        {
            if (eventData.Handle != _activeHandle)
            {
                return;
            }

            _pending.Clear();
            if (eventData.Reason == GibberishStopReason.Completed)
            {
                _text.maxVisibleCharacters = int.MaxValue;
            }
        }

        private int ResolveVisibleCharacterCount(int sourceEnd)
        {
            var count = 0;
            var characters = _text.textInfo.characterInfo;
            for (var index = 0; index < characters.Length; index++)
            {
                if (characters[index].index >= sourceEnd)
                {
                    break;
                }

                count++;
            }

            return count;
        }

        private readonly struct PendingWord
        {
            public int SourceEnd { get; }
            public double DspTime { get; }

            public PendingWord(int sourceEnd, double dspTime)
            {
                SourceEnd = sourceEnd;
                DspTime = dspTime;
            }
        }
    }
}
#endif
