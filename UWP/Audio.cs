namespace Zebble.Device
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Windows.Media.Capture;
    using Windows.Media.MediaProperties;
    using Windows.Storage.Streams;
    using Olive;

    partial class Audio
    {
        static MediaCapture Capture;
        static InMemoryRandomAccessStream Buffer;
        
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