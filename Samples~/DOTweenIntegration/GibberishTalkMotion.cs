#if COZY_GIBBERISH_DOTWEEN
using System;
using DG.Tweening;
using UnityEngine;

namespace CozyGibberish.Integrations.DOTween
{
    public sealed class GibberishTalkMotion : MonoBehaviour
    {
        [SerializeField] private GibberishVoicePlayer _player;
        [SerializeField] private Transform _animatedTarget;
        [SerializeField, Range(0f, 0.4f)] private float _scaleAmount = 0.08f;
        [SerializeField, Min(0.01f)] private float _attackDuration = 0.055f;
        [SerializeField, Min(0.01f)] private float _releaseDuration = 0.11f;
        [SerializeField] private Ease _attackEase = Ease.OutBack;
        [SerializeField] private Ease _releaseEase = Ease.OutSine;

        private IDisposable _syllableSubscription;
        private IDisposable _stopSubscription;
        private Tween _activeTween;
        private Vector3 _restScale;

        private void OnEnable()
        {
            if (_player == null)
            {
                enabled = false;
                return;
            }

            if (_animatedTarget == null)
            {
                _animatedTarget = transform;
            }

            _restScale = _animatedTarget.localScale;
            _syllableSubscription = _player.Events.Subscribe<GibberishMouthCueEvent>(OnMouthCue);
            _stopSubscription = _player.Events.Subscribe<GibberishSpeechStoppedEvent>(OnSpeechStopped);
        }

        private void OnDisable()
        {
            _syllableSubscription?.Dispose();
            _stopSubscription?.Dispose();
            _syllableSubscription = null;
            _stopSubscription = null;
            _activeTween?.Kill();
            _activeTween = null;

            if (_animatedTarget != null)
            {
                _animatedTarget.localScale = _restScale;
            }
        }

        private void OnMouthCue(GibberishMouthCueEvent eventData)
        {
            var intensity = Mathf.Lerp(0.55f, 1f, eventData.Intensity);
            var targetScale = _restScale * (1f + _scaleAmount * intensity);
            var delay = Mathf.Max(
                0f,
                (float)(eventData.DspTime - AudioSettings.dspTime));
            _activeTween?.Kill();
            _activeTween = DOTween.Sequence()
                .SetDelay(delay)
                .Append(_animatedTarget.DOScale(targetScale, _attackDuration).SetEase(_attackEase))
                .Append(_animatedTarget.DOScale(_restScale, _releaseDuration).SetEase(_releaseEase))
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void OnSpeechStopped(GibberishSpeechStoppedEvent eventData)
        {
            _activeTween?.Kill();
            _activeTween = _animatedTarget
                .DOScale(_restScale, _releaseDuration)
                .SetEase(_releaseEase)
                .SetUpdate(true)
                .SetLink(gameObject);
        }
    }
}
#endif
