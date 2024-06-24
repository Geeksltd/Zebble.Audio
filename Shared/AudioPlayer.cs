namespace Zebble.Device
{
    using System;
    using System.Threading.Tasks;
    using Olive;

    public partial class AudioPlayer : IDisposable
    {
        TaskCompletionSource<bool> Ended = new TaskCompletionSource<bool>();

        public readonly AsyncEvent Completed = new AsyncEvent();

        public Task Play(string source, OnError errorAction = OnError.Toast)
        {
            return ExecuteSafe(() => DoPlay(source), errorAction, "Failed to play audio file");
        }

        public Task Stop(OnError errorAction = OnError.Toast)
        {
            return ExecuteSafe(StopPlaying, errorAction, "Failed to stop playing audio.");
        }

        Task ExecuteSafe(Func<Task> execution, OnError errorAction, string errorMessage)
        {
            var task = new TaskCompletionSource<bool>();

            AudioThread.Post(async () =>
            {
                try
                {
                    await execution().ConfigureAwait(false);
                    task.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    if (errorAction == OnError.Throw) task.TrySetException(ex);
                    else
                    {
                        await errorAction.Apply(ex, "Failed to play audio file").ConfigureAwait(false);
                        task.TrySetResult(false);
                    }
                }
            });

            return task.Task;
        }

        async Task DoPlay(string file)
        {
            await Stop(OnError.Ignore).ConfigureAwait(false);

            Ended = new TaskCompletionSource<bool>();

            if (file.IsUrl()) await PlayStream(file).ConfigureAwait(false);
            else await PlayFile(file).ConfigureAwait(false);
#if !ANDROID
            await Completed.RaiseOn(Thread.Pool).ConfigureAwait(false);
#endif
        }
    }
}
