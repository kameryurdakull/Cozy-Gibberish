using System;

namespace CozyGibberish
{
    /// <summary>
    /// Minimal typed event contract used to decouple speech playback from presentation and gameplay.
    /// </summary>
    public interface IGibberishEventBus
    {
        IDisposable Subscribe<TEvent>(Action<TEvent> handler);
        void Publish<TEvent>(TEvent eventData);
    }
}
