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

        public static byte[] RecordedBytes => Recording?.ReadAllBytes() ?? new byte[0];

        public static Task<byte[]> StopRecording()
        {
            try { Recorder?.Stop(); return Task.FromResult(RecordedBytes); }
            finally { Recorder?.Release(); Recorder = null; }
        }

        static BaseThread AudioThread => Thread.Pool;

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