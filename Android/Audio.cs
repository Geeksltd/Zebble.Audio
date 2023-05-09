using Android;
using Android.App;

[assembly: UsesPermission(Manifest.Permission.RecordAudio)]
[assembly: UsesPermission(Manifest.Permission.ModifyAudioSettings)]

namespace Zebble.Device
{
    using Android.Media;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Olive;

    static partial class Audio
    {
        const int ENCODING_BIT_RATE = 16, AUDIO_SAMPLING_RATE = 44100;
        static MediaRecorder Recorder;
        static FileInfo Recording;

        public static async Task StartRecording(OnError errorAction = OnError.Toast)
        {
            try
            {
                if (await Permission.RecordAudio.IsRequestGranted())
                {
                    return;
                }

                if (Recording?.Exists() == true)
                    lock (Recording.GetSyncLock())
                        Recording.Delete();

                var newFile = $"Myfile{DateTime.UtcNow:yyyyMMddHHmmss}.wav";
                Recording = IO.CreateTempDirectory().GetFile(newFile);
                lock (Recording.GetSyncLock())
                    Recording.Delete();

                CreateRecorder();
                Recorder.Start();
            }
            catch (Exception ex) { await errorAction.Apply(ex); }
        }

        public static byte[] RecordedBytes => Recording?.Exists() == true ? Recording?.ReadAllBytes() : Array.Empty<byte>();

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