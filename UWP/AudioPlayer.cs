namespace Zebble.Device
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Windows.Media.Capture;
    using Windows.Media.MediaProperties;
    using Windows.Storage.Streams;

    partial class AudioPlayer
    {
        Windows.Media.Playback.MediaPlayer Player;

        public async Task<bool> PlayFile()
        {
            var storage = await Device.IO.File(File).ToStorageFile();
            var source = Windows.Media.Core.MediaSource.CreateFromStorageFile(storage);

            return await Play(source);
        }

        public async Task PlayStream()
        {
            var source = Windows.Media.Core.MediaSource.CreateFromUri(new Uri(File));
            await Play(source);
        }

        async Task<bool> Play(Windows.Media.Playback.IMediaPlaybackSource source)
        {
            Player = new Windows.Media.Playback.MediaPlayer
            {
                AudioCategory = Windows.Media.Playback.MediaPlayerAudioCategory.Media,
                Volume = 1
            };

            try
            {
                Player.Source = source;
                Player.MediaEnded += Player_MediaEnded;
                Player.MediaFailed += Player_MediaFailed;
                Player.Play();
            }
            catch
            {
                Dispose();
                throw;
            }

            return await Completion.Task;
        }

        void Player_MediaEnded(Windows.Media.Playback.MediaPlayer sender, object args)
        {
            Dispose();
            Completion.TrySetResult(true);
        }

        void Player_MediaFailed(Windows.Media.Playback.MediaPlayer sender, Windows.Media.Playback.MediaPlayerFailedEventArgs args)
        {
            Dispose();
            Completion.TrySetException(new Exception("Failed to play " + File + " > " + args.ErrorMessage));
        }

        partial void Dispose()
        {
            var player = Player;
            Player = null;
            if (player == null) return;

            player.MediaEnded -= Player_MediaEnded;
            player.MediaFailed -= Player_MediaFailed;
            try { player.Pause(); } catch { }

            player.Dispose();
        }
    }
}
