namespace Zebble.Device
{
    using AVFoundation;
    using Foundation;
    using System;
    using System.Threading.Tasks;
    using Olive;

    partial class AudioPlayer
    {
        static AVAudioPlayer Player;

        static BaseThread AudioThread => Thread.UI;

        public Task<bool> PlayFile(string file)
        {
            var url = IO.AbsolutePath(file).ToNsUrl();
            return PlayFile(url);
        }

        public async Task<bool> PlayFile(NSUrl url = null)
        {
            try
            {
                Dispose();

                Player = new AVAudioPlayer(url, "wav", out var err) { Volume = 1.0F };
                if (err?.Description.HasValue() == true) return false;
            }
            catch (Exception)
            {
                return false;
            }

            Player.FinishedPlaying += Player_FinishedPlaying;
            Player.DecoderError += Player_DecoderError;

            if (Player.PrepareToPlay())
            {
                Audio.AcquireSession();
                var result = Player.Play();
                return await Ended.Task;
            }
            else throw new Exception("Failed to play " + url);
        }

        public Task PlayStream(string url)
        {
            var result = new TaskCompletionSource<bool>();

            var downloadTask = NSUrlSession.SharedSession.CreateDownloadTask(url.ToNsUrl(), new NSUrlDownloadSessionResponse((u, response, err) =>
            {
                if (err?.Description.HasValue() == true)
                {
                    Log.For(this).Error("Failed to play audio\n" + err.Description);
                    return;
                }

                try
                {
                    PlayFile(u).RunInParallel();
                    result.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    result.TrySetException(ex);
                }
            }));

            downloadTask.Resume();

            return result.Task;
        }

        void Player_DecoderError(object sender, AVErrorEventArgs e)
        {
            Ended.TrySetException(new Exception("Failed to play audio > " + e.Error.Description));
        }

        void Player_FinishedPlaying(object sender, AVStatusEventArgs e)
        {
            Ended.TrySetResult(true);
            Audio.ReleaseSession();
            Completed.Raise().RunInParallel();
        }

        public Task StopPlaying()
        {
            Player?.Stop();
            Audio.ReleaseSession();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (Player != null)
            {
                Player.DecoderError -= Player_DecoderError;
                Player.FinishedPlaying -= Player_FinishedPlaying;
            }

            Player?.Dispose();
            Player = null;
        }
    }
}
