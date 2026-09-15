#if COZY_GIBBERISH_UNITASK
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CozyGibberish.Integrations.UniTask
{
    public static class GibberishUniTaskExtensions
    {
        public static async UniTask<GibberishPlaybackHandle> SpeakAsync(
            this GibberishVoicePlayer player,
            GibberishSpeechRequest request,
            CancellationToken cancellationToken = default)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            await Cysharp.Threading.Tasks.UniTask.SwitchToMainThread(cancellationToken);
            var context = player.Capture(request);
            var prepared = await Cysharp.Threading.Tasks.UniTask.RunOnThreadPool(
                () => player.Render(context, cancellationToken),
                cancellationToken: cancellationToken);
            await Cysharp.Threading.Tasks.UniTask.SwitchToMainThread(cancellationToken);
            return player.PlayPrepared(prepared);
        }

        public static async UniTask<GibberishPreparedSpeech> PrewarmAsync(
            this GibberishVoicePlayer player,
            GibberishSpeechRequest request,
            CancellationToken cancellationToken = default)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            await Cysharp.Threading.Tasks.UniTask.SwitchToMainThread(cancellationToken);
            var context = player.Capture(request);
            return await Cysharp.Threading.Tasks.UniTask.RunOnThreadPool(
                () => player.Render(context, cancellationToken),
                cancellationToken: cancellationToken);
        }

        public static async UniTask<GibberishStopReason> SpeakAndWaitAsync(
            this GibberishVoicePlayer player,
            GibberishSpeechRequest request,
            CancellationToken cancellationToken = default)
        {
            var handle = await player.SpeakAsync(request, cancellationToken);
            if (!handle.IsValid)
            {
                return GibberishStopReason.Cancelled;
            }

            var completion = new UniTaskCompletionSource<GibberishStopReason>();
            using (player.Events.Subscribe<GibberishSpeechStoppedEvent>(eventData =>
                   {
                       if (eventData.Handle == handle)
                       {
                           completion.TrySetResult(eventData.Reason);
                       }
                   }))
            using (cancellationToken.Register(() => CancelOnMainThread(player, handle).Forget()))
            {
                var reason = await completion.Task;
                cancellationToken.ThrowIfCancellationRequested();
                return reason;
            }
        }

        private static async UniTaskVoid CancelOnMainThread(
            GibberishVoicePlayer player,
            GibberishPlaybackHandle handle)
        {
            await Cysharp.Threading.Tasks.UniTask.SwitchToMainThread();
            if (player != null)
            {
                player.Cancel(handle);
            }
        }
    }

    public sealed class GibberishUniTaskRenderScheduler : IDisposable
    {
        private readonly SemaphoreSlim _concurrency;

        public GibberishUniTaskRenderScheduler(int maximumConcurrency = 2)
        {
            _concurrency = new SemaphoreSlim(
                Math.Max(1, maximumConcurrency),
                Math.Max(1, maximumConcurrency));
        }

        public async UniTask<GibberishPreparedSpeech> ScheduleAsync(
            GibberishVoicePlayer player,
            GibberishSpeechRequest request,
            CancellationToken cancellationToken = default)
        {
            await _concurrency.WaitAsync(cancellationToken);
            try
            {
                return await player.PrewarmAsync(request, cancellationToken);
            }
            finally
            {
                _concurrency.Release();
            }
        }

        public void Dispose()
        {
            _concurrency.Dispose();
        }
    }
}
#endif
