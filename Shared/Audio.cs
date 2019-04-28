namespace Zebble.Device
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public static partial class Audio
    {
        static List<AudioPlayer> Players = new List<AudioPlayer>();

        public static readonly AsyncEvent PlayingCompleted = new AsyncEvent();

        public static async Task Play(string source, OnError errorAction = OnError.Toast)
        {
            try { await AudioThread.Run(() => DoPlay(source)); }
            catch (Exception ex) { await errorAction.Apply(ex, "Failed to play audio file"); }
        }

        public static async Task StopPlaying(OnError errorAction = OnError.Toast)
        {
            try { await AudioThread.Run(DoStopPlaying); }
            catch (Exception ex) { await errorAction.Apply(ex, "Failed to stop playing audio."); }
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
