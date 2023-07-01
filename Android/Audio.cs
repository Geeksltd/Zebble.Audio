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
    using Android.OS;
    using Android.Runtime;

    static partial class Audio
    {
        const int ENCODING_BIT_RATE = 16, AUDIO_SAMPLING_RATE = 44100;
        static MediaRecorder Recorder;
        static FileInfo Recording;

        public static async Task StartRecording(OnError errorAction = OnError.Toast)
        {
            try
            {
                if (await Permission.RecordAudio.IsRequestGranted() == false)
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

                RequestFocus(AudioFocus.Gain);
                Recorder.Start();
            }
            catch (Exception ex) { await errorAction.Apply(ex); }
        }

        public static byte[] RecordedBytes => Recording?.Exists() == true ? Recording?.ReadAllBytes() : Array.Empty<byte>();

        public static Task<byte[]> StopRecording()
        {
            try
            {
                Recorder?.Stop();
                AbandonFocus();
                return Task.FromResult(RecordedBytes);
            }
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

        static AudioFocusRequestClass FocusRequest;

        public static bool RequestFocus(AudioFocus focus)
        {
            var audioManager = AudioManager.FromContext(UIRuntime.AppContext);
            if (audioManager is null) return false;

            AbandonFocus();

            try
            {
                AudioFocusRequest requestResult;

                //if (Build.VERSION.SdkInt > BuildVersionCodes.O)
                //{
                //    var attributes = new AudioAttributes.Builder()
                //        .SetUsage(AudioUsageKind.Media)
                //        .SetContentType(AudioContentType.Speech)
                //        .Build();

                //    FocusRequest = new AudioFocusRequestClass.Builder(focus)
                //        .SetAudioAttributes(attributes)
                //        .SetAcceptsDelayedFocusGain(true)
                //        .Build();

                //    requestResult = audioManager.RequestAudioFocus(FocusRequest);
                //}
                //else
                    requestResult = audioManager.RequestAudioFocus(null, Android.Media.Stream.Music, focus);

                return requestResult == AudioFocusRequest.Granted;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static bool AbandonFocus()
        {
            var audioManager = AudioManager.FromContext(UIRuntime.AppContext);
            if (audioManager is null) return false;

            AudioFocusRequest requestResult;

            try
            {
                //if (Build.VERSION.SdkInt > BuildVersionCodes.O)
                //{
                //    if (FocusRequest is null) return true;

                //    requestResult = audioManager.AbandonAudioFocusRequest(FocusRequest);
                //    FocusRequest = null;
                //}
                //else
                    requestResult = audioManager.AbandonAudioFocus(null);

                return requestResult == AudioFocusRequest.Granted;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}