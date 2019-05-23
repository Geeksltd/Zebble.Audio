namespace Zebble.Device
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public static partial class Audio
    {
        static List<AudioPlayer> Players = new List<AudioPlayer>();

        public static readonly AsyncEvent PlayingCompleted = new AsyncEvent();

        public static Task Play(string source, OnError errorAction = OnError.Toast)
        {
            return ExecuteSafe(() => DoPlay(source), errorAction, "Failed to play audio file");
        }

        public static Task StopPlaying(OnError errorAction = OnError.Toast)
        {
            return ExecuteSafe(DoStopPlaying, errorAction, "Failed to stop playing audio.");
        }

        static Task ExecuteSafe(Func<Task> execution, OnError errorAction, string errorMessage)
        {
            var task = new TaskCompletionSource<bool>();

            AudioThread.Post(async () =>
            {
                try
                {
                    await execution();
                    task.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    await errorAction.Apply(ex, "Failed to play audio file");
                    if (errorAction == OnError.Throw) task.TrySetException(ex);
                    else task.TrySetResult(false);
                }
            });

            return task.Task;
        }

        static async Task DoPlay(string file)
        {
            await StopPlaying(OnError.Ignore);

            var player = new AudioPlayer(file);
            Players.Add(player);

            try
            {
                if (file.IsUrl()) await player.PlayStream();
                else await player.PlayFile();
                await PlayingCompleted.RaiseOn(Thread.Pool);
            }
            finally { Players.Remove(player); }
        }

        static Task DoStopPlaying()
        {
            Players.ToArray().Do(x => x.Stop());
            return Task.CompletedTask;
        }
    }
}
