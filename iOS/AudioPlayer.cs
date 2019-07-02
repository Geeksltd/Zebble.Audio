namespace Zebble.Device
{
    using AVFoundation;
    using Foundation;
    using System;
    using System.IO;
    using System.Threading.Tasks;

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
            Player = new AVAudioPlayer(url, "wav", out var err) { Volume = 1.0F };
            if (err?.Description.HasValue() == true) throw new Exception(err.Description);

            Player.FinishedPlaying += Player_FinishedPlaying;
            Player.DecoderError += Player_DecoderError;

            if (Player.PrepareToPlay())
            {
                Player.Play();
                return await Ended.Task;
            }
            else throw new Exception("Failed to play " + url);
        }

        public async Task PlayStream(string url)
        {
            // TODO: Implement it using AVPlayer in a way to handle error and completion events.
            //StreamPlayer = new AVPlayer(NSUrl.FromString(File));
            //StreamPlayer.CurrentItem.AddObserver(...)
            //StreamPlayer.Play();
            //--->
            //Implemented but it has some problems yet.
            //StreamPlayer = new IOSAudioPlayer(File);

            // Workaround for now:
            var downloadTask = NSUrlSession.SharedSession.CreateDownloadTask(new NSUrl(url), new NSUrlDownloadSessionResponse((u, response, err) =>
            {
                if (err?.Description.HasValue() == true)
                {
                    Log.Error("Failed to play audio\n" + err.Description);
                    return;
                }

                PlayFile(u).RunInParallel();
            }));

            downloadTask.Resume();
        }

        void Player_DecoderError(object sender, AVErrorEventArgs e)
        {
            Ended.TrySetException(new Exception("Failed to play audio > " + e.Error.Description));
        }

        void Player_FinishedPlaying(object sender, AVStatusEventArgs e)
        {
            Ended.TrySetResult(true);
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
                player.FinishedPlaying -= Player_FinishedPlaying;

                try { player.Stop(); } catch { }
                player.Dispose();
            });
        }
    }
}
