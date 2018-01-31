namespace Zebble.Device
{
    using System;
    using System.Threading.Tasks;

    public static partial class Audio
    {
        public static readonly AsyncEvent PlayingCompleted = new AsyncEvent();

        public static async Task Play(string source, OnError errorAction = OnError.Toast)
        {
            try
            {
                await StopPlaying(OnError.Ignore);
                if (source.IsUrl())
                    await Thread.UI.Run(() => PlayStream(source));
                else
                    await Thread.UI.Run(() => DoPlay(source));
            }
            catch (Exception ex) { await errorAction.Apply(ex, "Failed to play audio file"); }
        }

        static async Task PlayStream(string url, OnError errorAction = OnError.Toast)
        {
            try
            {
                await StopPlaying(OnError.Ignore);
                await Thread.UI.Run(() => DoPlayStream(url));
            }
            catch (Exception ex) { await errorAction.Apply(ex, "Failed to play audio file"); }
        }

        public static async Task StopPlaying(OnError errorAction = OnError.Toast)
        {
            try { await Thread.UI.Run(DoStopPlaying); }
            catch (Exception ex) { await errorAction.Apply(ex, "Failed to stop playing audio."); }
        }
    }
}
