using Android.Media;
using System;
using System.Threading.Tasks;

namespace Zebble.Device
{
    partial class AudioPlayer
    {
        MediaPlayer Player;

        static BaseThread AudioThread => Thread.Pool;

        public AudioPlayer()
        {
            Player = new MediaPlayer();
            Player.SetVolume(1.0f, 1.0f);
            Player.Completion += Player_Completion;
            Player.Error += Player_Error;
        }

        Task StopPlaying()
        {
            Player?.Stop();
            Ended.TrySetResult(false);
            return Task.CompletedTask;
        }

        public async Task<bool> PlayFile(string file)
        {
            await SetSource($"file://{IO.AbsolutePath(file)}");
            Player.Start();

            return await Ended.Task;
        }

        public async Task PlayStream(string url)
        {
            await SetSource(url);

            if (OS.IsAtLeast(Android.OS.BuildVersionCodes.O))
            {
                var attributes = new AudioAttributes.Builder().SetLegacyStreamType(Stream.Music).Build();
                Player.SetAudioAttributes(attributes);
            }

            Player.Start();
        }

        async Task SetSource(string url)
        {
            try
            {
                Player.Stop();
                Player.Reset();
                await Player.SetDataSourceAsync(Renderer.Context, Android.Net.Uri.Parse(url));
                Player.Prepare();
            }
            catch (Exception ex)
            {
                throw new Exception("Audio not accessible: " + url, ex);
            }
        }

        void Player_Completion(object sender, EventArgs e) => Ended.TrySetResult(true);

        void Player_Error(object sender, MediaPlayer.ErrorEventArgs e)
        {
            Ended.TrySetException(new Exception("Failed to play audio > " + e.What));
        }

        public void Dispose()
        {
            var player = Player;
            Player = null;
            if (player == null) return;

            Completed?.Dispose();

            player.Completion -= Player_Completion;
            player.Error -= Player_Error;

            void kill()
            {
                if (player.IsPlaying) player.Stop();
                player.Release();
            }

            if (Thread.UI.IsRunning()) kill();
            else Thread.UI.Post(kill);
        }
    }
}