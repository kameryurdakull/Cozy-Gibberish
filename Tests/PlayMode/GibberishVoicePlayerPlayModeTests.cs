using NUnit.Framework;
using UnityEngine;

namespace CozyGibberish.Tests
{
    public sealed class GibberishVoicePlayerPlayModeTests
    {
        private GameObject _owner;
        private GibberishVoicePlayer _player;
        private GibberishVoiceProfile _voice;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("GibberishPlayerTest");
            _owner.AddComponent<AudioSource>();
            _player = _owner.AddComponent<GibberishVoicePlayer>();
            _voice = ScriptableObject.CreateInstance<GibberishVoiceProfile>();
            _voice.ApplyArchetype(GibberishVoiceArchetype.Cozy);
            _player.SetDefaults(_voice);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_owner);
            Object.DestroyImmediate(_voice);
        }

        [Test]
        public void PauseResumeSeekAndCancel_PreserveSessionLifecycle()
        {
            var handle = _player.Speak(new GibberishSpeechRequest(
                "A lifecycle test for the cozy player.",
                _voice,
                seed: 4));

            Assert.That(handle.IsValid, Is.True);
            Assert.That(_player.IsSpeaking, Is.True);
            _player.Pause();
            Assert.That(_player.IsPaused, Is.True);
            Assert.That(_player.Seek(0.05f), Is.True);
            _player.Resume();
            Assert.That(_player.IsPaused, Is.False);
            Assert.That(_player.Cancel(handle), Is.True);
            Assert.That(_player.IsSpeaking, Is.False);
        }

        [Test]
        public void Interrupt_ReplacesHandleAndPublishesLifecycleEvent()
        {
            var interrupted = GibberishPlaybackHandle.Invalid;
            using (_player.Events.Subscribe<GibberishSpeechStoppedEvent>(eventData =>
                   {
                       if (eventData.Reason == GibberishStopReason.Interrupted)
                       {
                           interrupted = eventData.Handle;
                       }
                   }))
            {
                var first = _player.Speak(new GibberishSpeechRequest(
                    "First line",
                    _voice,
                    seed: 1));
                var second = _player.Speak(new GibberishSpeechRequest(
                    "Second line",
                    _voice,
                    seed: 2));

                Assert.That(first.IsValid, Is.True);
                Assert.That(second.IsValid, Is.True);
                Assert.That(second, Is.Not.EqualTo(first));
                Assert.That(interrupted, Is.EqualTo(first));
                Assert.That(_player.CurrentHandle, Is.EqualTo(second));
            }
        }

        [Test]
        public void Enqueue_PreservesActiveSpeechAndReturnsQueuedHandle()
        {
            var active = _player.Speak(new GibberishSpeechRequest(
                "Active",
                _voice,
                seed: 1));
            var queued = _player.Speak(new GibberishSpeechRequest(
                "Queued",
                _voice,
                seed: 2,
                playbackPolicy: GibberishPlaybackPolicy.Enqueue));

            Assert.That(active.IsValid, Is.True);
            Assert.That(queued.IsValid, Is.True);
            Assert.That(_player.CurrentHandle, Is.EqualTo(active));
            Assert.That(_player.QueueCount, Is.EqualTo(1));
        }
    }
}
