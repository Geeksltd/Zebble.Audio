using Android.Media;
using System;
using System.Threading.Tasks;

namespace Zebble.Device
{
    partial class AudioPlayer
    {
        MediaPlayer Player;

        public async Task<bool> PlayFile()
        {
            try
            {
                var audioFile = $"file://{IO.AbsolutePath(File)}";
                Player = MediaPlayer.Create(Renderer.Context, Android.Net.Uri.Parse(audioFile));
                if (Player == null) throw new Exception("Failed to play " + File);
                Player.SetVolume(1.0f, 1.0f);
                Player.Completion += Player_Completion;
                Player.Error += Player_Error;
                Player.Start();
            }
            catch
            {
                Dispose();
                throw;
            }

            return await Completion.Task;
        }

        public Task PlayStream()
        {
            try
            {
                Player = MediaPlayer.Create(Renderer.Context, Android.Net.Uri.Parse(File));
                if (OS.IsAtLeast(Android.OS.BuildVersionCodes.O))
                {
                    var attributes = new AudioAttributes.Builder().SetLegacyStreamType(Stream.Music).Build();
                    Player.SetAudioAttributes(attributes);
                }
                else Player.SetAudioStreamType(Stream.Music);
                Player.SetVolume(1.0f, 1.0f);
                Player.Completion += Player_Completion;
                Player.Error += Player_Error;
                Player.Start();
            }
            catch
            {
                Dispose();
                throw;
            }

            return Completion.Task;
        }

        void Player_Completion(object sender, EventArgs e)
        {
            Dispose();
            Completion.TrySetResult(true);
        }

        void Player_Error(object sender, MediaPlayer.ErrorEventArgs e)
        {
            Dispose();
            Completion.TrySetException(new Exception("Failed to play " + File + " > " + e.What));
        }

        partial void Dispose()
        {
            var player = Player;
            Player = null;
            if (player == null) return;

            player.Completion -= Player_Completion;
            player.Error -= Player_Error;
            if (player.IsPlaying) player.Stop();

            Thread.UI.Post(() =>
            {
                player.Reset();
                player.Dispose();
                player = null;
            });
        }
    }
}