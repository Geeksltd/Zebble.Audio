namespace Zebble.Device
{
    using AVFoundation;
    using Foundation;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Olive;

    partial class AudioPlayer
    {
        static AVAudioPlayer Player;

        static BaseThread AudioThread => Thread.UI;

        public Task<bool> PlayFile(string file)
        {
            var url = new NSUrl(Device.IO.AbsolutePath(file));
            return PlayFile(url);
        }

        public async Task<bool> PlayFile(NSUrl url = null)
        {
            try
            {
                Player = new AVAudioPlayer(url, "wav", out var err) { Volume = 1.0F };
                if (err?.Description.HasValue() == true) return false;
            }
            catch (Exception)
            {
                return false;
            }

            Player.FinishedPlaying += async (object sender, AVStatusEventArgs args) => await Player_FinishedPlaying(sender, args);
            Player.DecoderError += Player_DecoderError;

            if (Player.PrepareToPlay())
            {
                Audio.ConfigureAudio(AVAudioSessionCategory.Playback);
                var result = Player.Play();
                return await Ended.Task;
            }
            else throw new Exception("Failed to play " + url);
        }

        public Task PlayStream(string url)
        {
            // TODO: Implement it using AVPlayer in a way to handle error and completion events.
            //StreamPlayer = new AVPlayer(NSUrl.FromString(File));
            //StreamPlayer.CurrentItem.AddObserver(...)
            //StreamPlayer.Play();
            //--->
            //Implemented but it has some problems yet.
            //StreamPlayer = new IOSAudioPlayer(File);

            var result = new TaskCompletionSource<bool>();

            // Workaround for now:
            var downloadTask = NSUrlSession.SharedSession.CreateDownloadTask(new NSUrl(url), new NSUrlDownloadSessionResponse((u, response, err) =>
            {
                if (err?.Description.HasValue() == true)
                {
                    Log.For(this).Error(null, "Failed to play audio\n" + err.Description);
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

        async Task Player_FinishedPlaying(object sender, AVStatusEventArgs e)
        {
            Ended.TrySetResult(true);
            await Completed.Raise();
        }

        public Task StopPlaying()
        {
            Player?.Stop();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Thread.UI.Post(() =>
            {
                var player = Player;
                Player = null;
                if (player == null) return;

                player.DecoderError -= Player_DecoderError;
                player.FinishedPlaying -= async (object sender, AVStatusEventArgs args) => await Player_FinishedPlaying(sender, args);

                try { player.Stop(); } catch { }
                player.Dispose();
            });
        }
    }
}
