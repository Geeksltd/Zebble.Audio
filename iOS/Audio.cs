namespace Zebble.Device
{
    using AVFoundation;
    using Foundation;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Olive;

    partial class Audio
    {
        static AVAudioRecorder Recorder;
        static FileInfo Recording;

        public static byte[] RecordedBytes => Recording?.ReadAllBytes() ?? new byte[0];

        public static async Task StartRecording(OnError errorAction = OnError.Toast)
        {
            try
            {
                await StopRecording();

                if (Recording?.Exists() == true)
                    lock (Recording.GetSyncLock())
                        Recording.Delete();

                var newFile = $"Myfile{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}.wav";
                Recording = Device.IO.CreateTempDirectory().GetFile(newFile);
                lock (Recording.GetSyncLock())
                    Recording.Delete();

                CreateRecorder();

                Recorder.Record();
            }
            catch (Exception ex) { await errorAction.Apply(ex); }
        }

        [Obsolete]
        public static void ReleaseSession() { }

        public static void AcquireSession()
        {
            var session = AVAudioSession.SharedInstance();

            var err = session.SetCategory(AVAudioSessionCategory.PlayAndRecord, AVAudioSessionCategoryOptions.MixWithOthers);
            if (err != null) Log.For(typeof(Audio)).Error("Failed to initiate the recorder: " + err.Description);

            session.OverrideOutputAudioPort(AVAudioSessionPortOverride.Speaker, out err);
            if (err != null) Log.For(typeof(Audio)).Error("Failed to switch to speaker: " + err.Description);

            err = session.SetActive(beActive: true);
            if (err != null) Log.For(typeof(Audio)).Error("Failed to activate the recorder: " + err.Description);
        }

        static void CreateRecorder()
        {
            AcquireSession();

            Recorder = AVAudioRecorder.Create(NSUrl.FromFilename(Recording.FullName), GetSettings(), out var err);
            if (err != null) Log.For(typeof(Audio)).Error("Could not create a recorder because: " + err.Description);
        }

        public static Task<byte[]> StopRecording() { Recorder?.Stop(); return Task.FromResult(RecordedBytes); }

        static AudioSettings GetSettings() => new AudioSettings(NSDictionary.FromObjectsAndKeys(GetValues(), GetKeys()));

        static NSObject[] GetKeys()
        {
            return new NSObject[]
            {
                    AVAudioSettings.AVSampleRateKey,
                    AVAudioSettings.AVFormatIDKey,
                    AVAudioSettings.AVNumberOfChannelsKey,
                    AVAudioSettings.AVLinearPCMBitDepthKey,
                    AVAudioSettings.AVLinearPCMIsBigEndianKey,
                    AVAudioSettings.AVLinearPCMIsFloatKey };
        }

        static NSObject[] GetValues()
        {
            // set up the NSObject Array of values that will be combined with the keys to make the NSDictionary
            return new NSObject[]
            {
                    44100.0f.ToNs(), //Sample Rate
                    ((int)AudioToolbox.AudioFormatType.LinearPCM).ToNs(), //AVFormat
                    2.ToNs(), //Channels
                    16.ToNs(), //PCMBitDepth
                    NSNumber.FromBoolean (value:false), //IsBigEndianKey
                    NSNumber.FromBoolean (value: false) //IsFloatKey
            };
        }
    }
}