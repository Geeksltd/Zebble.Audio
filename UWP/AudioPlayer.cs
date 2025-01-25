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

        public AudioPlayer()
        {
            Player = new Windows.Media.Playback.MediaPlayer
            {
                Volume = 1,
                AudioCategory = Windows.Media.Playback.MediaPlayerAudioCategory.Media,
            };
            Player.MediaEnded += Player_MediaEnded;
            Player.MediaFailed += Player_MediaFailed;
        }

        static BaseThread AudioThread => Thread.UI;

        public async Task<bool> PlayFile(string file)
        {
            var storage = await Device.IO.File(file).ToStorageFile();
            var source = Windows.Media.Core.MediaSource.CreateFromStorageFile(storage);

            return await Play(source);
        }

        public async Task PlayStream(string url)
        {
            var source = Windows.Media.Core.MediaSource.CreateFromUri(new Uri(url));
            await Play(source);
        }

        async Task<bool> Play(Windows.Media.Playback.IMediaPlaybackSource source)
        {
            Player.Source = source;
            Player.Play();

            return await Ended.Task;
        }

        void Player_MediaEnded(Windows.Media.Playback.MediaPlayer sender, object args)
        {
            Ended.TrySetResult(true);
        }

        void Player_MediaFailed(Windows.Media.Playback.MediaPlayer sender, Windows.Media.Playback.MediaPlayerFailedEventArgs args)
        {
            Ended.TrySetException(new Exception("Failed to play audio > " + args.ErrorMessage));
        }

        public void Dispose()
        {
            var player = Player;
            Player = null;
            if (player == null) return;

            player.MediaEnded -= Player_MediaEnded;
            player.MediaFailed -= Player_MediaFailed;
            try { player.Pause(); } catch { }

            player.Dispose();
			
			GC.SuppressFinalize(this);
        }

        Task StopPlaying()
        {
            if (Player?.PlaybackSession?.CanPause == true)
                Player?.Pause();

            return Task.CompletedTask;
        }
    }
}
