namespace Zebble.Device
{
    using AVFoundation;
    using Foundation;
    using System;
    using UIKit;

    internal class IOSAudioPlayer : UIView
    {
        AVPlayer AvPlayer;
        AVPlayerItem AvPlayerItem;
        NSUrl DownloadedFile;
        bool ShouldDisposeView;

        public string Path { get; }

        public IOSAudioPlayer(string path)
        {
            Path = path;

            InitializePlayer();
        }

        public override void ObserveValue(NSString keyPath, NSObject ofObject, NSDictionary change, IntPtr context)
        {
            if (ofObject is AVPlayerItem item && keyPath == "status")
            {
                if (item.Status == AVPlayerItemStatus.ReadyToPlay)
                {
                    AvPlayer?.Play();
                    return;
                }

                if (item.Status == AVPlayerItemStatus.Failed) Log.Error($"Failed to play {Path}");
                else Log.Error($"An error occured during playing {Path}");

                RetryToDownloadTrack();
            }
        }

        void InitializePlayer()
        {
            ShouldDisposeView = true;

            if (DownloadedFile != null) AvPlayerItem = new AVPlayerItem(DownloadedFile);
            else if (Path.IsUrl()) AvPlayerItem = new AVPlayerItem(new NSUrl(Path));
            else AvPlayerItem = new AVPlayerItem(AVAsset.FromUrl(NSUrl.FromString("file://" + IO.File(Path).FullName)));

            AvPlayer = new AVPlayer(AvPlayerItem) { Volume = 1.0f };

            NSNotificationCenter.DefaultCenter.AddObserver(AVPlayerItem.DidPlayToEndTimeNotification, notification => Thread.UI.Post(() => Dispose()), AvPlayerItem);
            AvPlayerItem.AddObserver(Self, "status", 0, IntPtr.Zero);
        }

        void Stop()
        {
            DownloadedFile = null;
            AvPlayer.Pause();
            AvPlayer.Seek(CoreMedia.CMTime.Zero);
        }

        void RetryToDownloadTrack()
        {
            if (!Path.IsUrl()) return;

            ShouldDisposeView = false;
            Dispose();

            var downloadTask = NSUrlSession.SharedSession.CreateDownloadTask(new NSUrl(Path), new NSUrlDownloadSessionResponse((url, response, err) =>
            {
                if (err?.Description.HasValue() == true)
                {
                    Log.Error(err.Description);
                    ShouldDisposeView = true;
                    Dispose();
                    return;
                }

                DownloadedFile = url;
                InitializePlayer();
            }));

            downloadTask.Resume();
        }

        protected override void Dispose(bool disposing)
        {
            if (ShouldDisposeView) base.Dispose(disposing);

            Stop();
            AvPlayer.Dispose();
            AvPlayerItem.Dispose();
        }
    }
}