namespace Zebble.Device
{
    using Android.Media;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    static partial class Audio
    {
        const int ENCODING_BIT_RATE = 16, AUDIO_SAMPLING_RATE = 44100;
        static MediaRecorder Recorder;
        static FileInfo Recording;
        static List<MediaPlayer> Players = new List<MediaPlayer>();

        public static Task StartRecording(OnError errorAction = OnError.Toast)
        {
            try
            {
                if (Recording?.Exists() == true) Recording.SyncDelete();

                var newFile = $"Myfile{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}.wav";
                Recording = IO.CreateTempDirectory().GetFile(newFile);
                Recording.SyncDelete();

                CreateRecorder();
                Recorder.Start();
                return Task.CompletedTask;
            }
            catch (Exception ex) { return errorAction.Apply(ex); }
        }

        static Task DoPlay(string file)
        {
            MediaPlayer player = null;
            try
            {
                player = MediaPlayer.Create(Renderer.Context, Android.Net.Uri.Parse(Device.IO.AbsolutePath(file)));
                player.SetVolume(1.0f, 1.0f);
                player.Completion += Player_Completion;
                player.Start();
                Players.Add(player);
            }
            catch
            {
                Dispose(player);
            }

            return Task.CompletedTask;
        }

        static void Dispose(MediaPlayer player)
        {
            if (player == null) return;

            Players.Remove(player);
            player.Completion -= Player_Completion;
            if (player.IsPlaying) player.Stop();
            player.Reset();
            player.Dispose();
            player = null;
        }

        static Task DoPlayStream(string url)
        {
            MediaPlayer player = null;

            try
            {
                player = MediaPlayer.Create(Renderer.Context, Android.Net.Uri.Parse(url));
                player.SetAudioStreamType(Android.Media.Stream.Music);
                player.SetVolume(1.0f, 1.0f);
                player.Completion += Player_Completion;
                player.Start();
            }
            catch
            {
                Dispose(player);
            }

            return Task.CompletedTask;
        }

        static void Player_Completion(object sender, EventArgs e)
        {
            var player = (sender as MediaPlayer);
            if (player != null)
            {
                player.Completion -= Player_Completion;
                Dispose(player);
            }

            PlayingCompleted.RaiseOn(Thread.Pool);
        }

        static Task DoStopPlaying()
        {
            Players.ToArray().Do(Dispose);
            return Task.CompletedTask;
        }

        public static byte[] RecordedBytes => Recording?.ReadAllBytes() ?? new byte[0];

        public static Task<byte[]> StopRecording()
        {
            try { Recorder?.Stop(); return Task.FromResult(RecordedBytes); }
            finally { Recorder?.Release(); Recorder = null; }
        }

        static void CreateRecorder()
        {
            if (Recorder == null) Recorder = new MediaRecorder();
            else Recorder.Reset();

            Recorder.SetAudioSource(AudioSource.Mic);
            Recorder.SetOutputFormat(OutputFormat.Mpeg4);
            Recorder.SetAudioEncoder(AudioEncoder.AmrNb);
            Recorder.SetAudioEncodingBitRate(ENCODING_BIT_RATE);
            Recorder.SetAudioSamplingRate(AUDIO_SAMPLING_RATE);
            Recorder.SetOutputFile(Recording.FullName);
            Recorder.Prepare();
        }
    }
}