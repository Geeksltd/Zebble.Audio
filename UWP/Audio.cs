namespace Zebble.Device
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Windows.Media.Capture;
    using Windows.Media.MediaProperties;
    using Windows.Storage.Streams;

    partial class Audio
    {
        static MediaCapture Capture;
        static InMemoryRandomAccessStream Buffer;
        static Windows.Media.Playback.MediaPlayer Player;
        static TimeSpan Start, End, CurrentPosition;
        static Timer WinTimer;

        static Task DoPlay(string file)
        {
            return Thread.UI.Run(async () =>
            {
                Player = new Windows.Media.Playback.MediaPlayer
                {
                    AudioCategory = Windows.Media.Playback.MediaPlayerAudioCategory.Media,
                    Volume = 1
                };

                var storage = await Device.IO.File(file).ToStorageFile();
                Player.Source = Windows.Media.Core.MediaSource.CreateFromStorageFile(storage);
                Player.MediaEnded += Player_MediaEnded;
                Player.Play();
            });
        }

        static void Player_MediaEnded(Windows.Media.Playback.MediaPlayer sender, object args)
        {
            sender.MediaEnded -= Player_MediaEnded;
            PlayingCompleted.RaiseOn(Thread.Pool);
        }

        static Task DoPlay(string file, TimeSpan start, TimeSpan end)
        {
            return Thread.UI.Run(async () =>
            {
                Player = new Windows.Media.Playback.MediaPlayer { AudioCategory = Windows.Media.Playback.MediaPlayerAudioCategory.Media, Volume = 1 };
                var stream = (await Device.IO.File(file).ReadAllBytesAsync()).ToRandomAccessStream();

                Player.Source = Windows.Media.Core.MediaSource.CreateFromStream(stream, string.Empty);
                End = end;
                Start = CurrentPosition = start;
                Player.PlaybackSession.Position = Start;
                Player.Play();
                WinTimer = new Timer(TimeSpan.FromSeconds(1));
                WinTimer.TickAction += StartPlayProgressUpdater;
            });
        }

        static Task DoPlayStream(string url)
        {
            return Thread.UI.Run(async () =>
            {
                Player = new Windows.Media.Playback.MediaPlayer { AudioCategory = Windows.Media.Playback.MediaPlayerAudioCategory.Media, Volume = 1 };
                Player.Source = Windows.Media.Core.MediaSource.CreateFromUri(new Uri(url));
                Player.Play();
            });
        }

        static void StartPlayProgressUpdater(Timer.TimerState state)
        {
            Thread.UI.Run(() =>
            {
                CurrentPosition = Player.PlaybackSession.Position;
                if ((int)CurrentPosition.TotalMilliseconds >= (int)End.TotalMilliseconds)
                {
                    Player.Pause();
                    Player = null;
                    CurrentPosition = 0.Milliseconds();
                    state.Timer.Dispose();
                }
            });
        }

        static Task DoStopPlaying() => Thread.UI.Run(() => { Player?.Pause(); Player = null; WinTimer = null; });

        public static async Task StartRecording(OnError errorAction = OnError.Toast)
        {
            try { await Thread.Pool.Run(() => DoStartRecording()); }
            catch (Exception ex) { await errorAction.Apply(ex); }
        }

        public static byte[] RecordedBytes => Buffer?.AsStreamForRead().ReadAllBytes() ?? new byte[0];

        static async Task DoStartRecording()
        {
            Buffer?.Dispose();
            Capture?.Dispose();

            Buffer = new InMemoryRandomAccessStream();

            Capture = new MediaCapture();
            await Capture.InitializeAsync(new MediaCaptureInitializationSettings
            {
                StreamingCaptureMode = StreamingCaptureMode.Audio
            });

            Capture.RecordLimitationExceeded += _ => { throw new Exception("Record Limitation Exceeded "); };
            Capture.Failed += (s, e) =>
            {
                // TODO: The recorded file is perhaps corrupt. It cannot be played.
                throw new Exception($"Code: {e.Code}. {e.Message}");
            };

            await Capture.StartRecordToStreamAsync(MediaEncodingProfile.CreateMp3(AudioEncodingQuality.High), Buffer);
        }

        public static async Task<byte[]> StopRecording()
        {
            await Capture.StopRecordWithResultAsync();
            return RecordedBytes;
        }
    }
}