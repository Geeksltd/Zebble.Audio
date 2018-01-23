namespace Zebble.Device
{
    using AVFoundation;
    using Foundation;
    using System;
    using System.IO;
    using System.Threading.Tasks;

    partial class Audio
    {
        static AVAudioPlayer Player;
        static AVAudioRecorder Recorder;
        static FileInfo Recording;
        static double End, CurrentPosition;

        static Task DoPlay(string file)
        {
            Player = new AVAudioPlayer(new NSUrl(Device.IO.AbsolutePath(file)), "wav", out var err) { Volume = 1.0F };
            if (err?.Description.HasValue() == true) throw new Exception(err.Description);

            Player.FinishedPlaying += Player_FinishedPlaying;

            if (Player.PrepareToPlay())
                Player.Play();

            return Task.CompletedTask;
        }

        static void Player_FinishedPlaying(object sender, AVStatusEventArgs e)
        {
            (sender as AVAudioPlayer).Perform(x => x.FinishedPlaying -= Player_FinishedPlaying);
            PlayingCompleted.RaiseOn(Thread.Pool);
        }

        static Task DoPlay(string file, TimeSpan start, TimeSpan end)
        {
            Player = new AVAudioPlayer(new NSUrl(Device.IO.AbsolutePath(file)), "wav", out var err) { Volume = 1 };
            if (err?.Description.HasValue() == true) throw new Exception(err.Description);
            CurrentPosition = start.TotalSeconds;
            End = end.TotalSeconds;
            Player.CurrentTime = CurrentPosition;
            Player.Play();

            var timer = new Timer(TimeSpan.FromSeconds(1));
            timer.TickAction += StartPlayProgressUpdater;

            return Task.CompletedTask;
        }

        static void StartPlayProgressUpdater(Timer.TimerState state)
        {
            CurrentPosition = Player.CurrentTime;

            if (CurrentPosition < End) return;

            Player.Stop();
            Player.Dispose();
            CurrentPosition = 0;
            state.Timer.Dispose();
        }

        static Task DoStopPlaying()
        {
            if (Player?.Playing == true)
            {
                Player.Stop();
                Player.Dispose();
            }

            return Task.CompletedTask;
        }

        public static byte[] RecordedBytes => Recording?.ReadAllBytes() ?? new byte[0];

        public static async Task StartRecording(OnError errorAction = OnError.Toast)
        {
            try
            {
                await StopRecording();

                if (Recording?.Exists() == true) Recording.SyncDelete();

                var newFile = $"Myfile{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}.wav";
                Recording = Device.IO.CreateTempDirectory().GetFile(newFile);
                Recording.SyncDelete();

                CreateRecorder();

                Recorder.Record();
            }
            catch (Exception ex) { await errorAction.Apply(ex); }
        }

        static void CreateRecorder()
        {
            var session = AVAudioSession.SharedInstance();

            var err = session.SetCategory(AVAudioSessionCategory.PlayAndRecord);
            if (err != null) throw new Exception("Failed to initiate the recorder: " + err.Description);

            err = session.SetActive(beActive: true);
            if (err != null) throw new Exception("Failed to activate the recorder: " + err.Description);

            Recorder = AVAudioRecorder.Create(NSUrl.FromFilename(Recording.FullName), GetSettings(), out err);
            if (err != null) throw new Exception("Could not create a recorder because: " + err.Description);
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