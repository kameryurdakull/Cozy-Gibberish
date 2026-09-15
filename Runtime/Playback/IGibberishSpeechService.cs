namespace CozyGibberish
{
    /// <summary>
    /// Gameplay-facing facade for speech playback, cancellation and lifecycle events.
    /// </summary>
    public interface IGibberishSpeechService
    {
        bool IsSpeaking { get; }
        IGibberishEventBus Events { get; }

        GibberishPlaybackHandle Speak(GibberishSpeechRequest request);
        bool Cancel(GibberishPlaybackHandle handle);
        void StopAll();
    }

    public interface IAdvancedGibberishSpeechService : IGibberishSpeechService
    {
        bool IsPaused { get; }
        GibberishPreparedSpeech Prewarm(GibberishSpeechRequest request);
        void Pause();
        void Resume();
        bool Seek(float timeSeconds);
        void ClearCache();
    }
}
