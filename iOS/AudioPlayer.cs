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

        public async Task<bool> PlayFile()
        {
            Player = new AVAudioPlayer(new NSUrl(Device.IO.AbsolutePath(File)), "wav", out var err) { Volume = 1.0F };
            if (err?.Description.HasValue() == true) throw new Exception(err.Description);

            Player.FinishedPlaying += Player_FinishedPlaying;
            Player.DecoderError += Player_DecoderError;

            if (Player.PrepareToPlay())
            {
                Player.Play();
                return await Completion.Task;
            }
            else throw new Exception("Failed to play " + File);
        }

        public async Task PlayStream()
        {
            // TODO: Implement it using AVPlayer in a way to handle error and completion events.
            //StreamPlayer = new AVPlayer(NSUrl.FromString(File));
            //StreamPlayer.CurrentItem.AddObserver(...)
            //StreamPlayer.Play();

            // Workaround for now:
            var file = Device.IO.Cache.GetFile(Guid.NewGuid().ToString());
            await Network.Download(File.AsUri(), file.FullName);
            await PlayFile();
        }

        void Player_DecoderError(object sender, AVErrorEventArgs e)
        {
            Dispose();
            Completion.TrySetException(new Exception("Failed to play " + File + " > " + e.Error.Description));
        }

        void Player_FinishedPlaying(object sender, AVStatusEventArgs e)
        {
            UIThread.Dispatcher.BeginInvokeOnMainThread(() =>
            {
                Player.FinishedPlaying -= Player_FinishedPlaying;
                Dispose();
            });

            Completion.TrySetResult(true);
        }

        partial void Dispose()
        {
            var player = Player;
            Player = null;
            if (player == null) return;

            player.DecoderError -= Player_DecoderError;
            player.FinishedPlaying -= Player_FinishedPlaying;
            try { player.Stop(); } catch { }

            player.Dispose();
        }
    }
}
