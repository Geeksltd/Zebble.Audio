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
                Player = MediaPlayer.Create(Renderer.Context, Android.Net.Uri.Parse(Device.IO.AbsolutePath(File)));
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
                Player.SetAudioStreamType(Android.Media.Stream.Music);
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
            player.Reset();
            player.Dispose();
            player = null;
        }
    }
}