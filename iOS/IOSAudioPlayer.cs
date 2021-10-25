namespace Zebble.Device
{
    using AVFoundation;
    using Foundation;
    using System;
    using UIKit;
    using Olive;

    internal class IOSAudioPlayer : UIView
    {
        AVPlayerItem PlayerItem;
        AVPlayer Player;
        NSUrl DownloadedFile;
        bool ShouldDisposeView;

        IDisposable DidPlayToEndTimeObservation;
        IDisposable StatusObservation;

        public string Path { get; }

        public IOSAudioPlayer(string path)
        {
            Path = path;

            InitializePlayer();
        }

        void InitializePlayer()
        {
            ShouldDisposeView = true;

            if (DownloadedFile != null) PlayerItem = new AVPlayerItem(DownloadedFile);
            else if (Path.IsUrl()) PlayerItem = new AVPlayerItem(new NSUrl(Path));
            else PlayerItem = new AVPlayerItem(AVAsset.FromUrl(NSUrl.FromString("file://" + IO.File(Path).FullName)));

            Player = new AVPlayer(PlayerItem) { Volume = 1.0f };

            DidPlayToEndTimeObservation = AVPlayerItem.Notifications.ObserveDidPlayToEndTime(PlayerItem, (_, _) => Thread.UI.Post(() => Dispose()));
            PlayerItem.AddObserver(nameof(AVPlayerItem.Status), 0, _ =>
            {
                if (PlayerItem.Status == AVPlayerItemStatus.ReadyToPlay)
                {
                    try { Audio.ConfigureAudio(AVAudioSessionCategory.Playback); }
                    catch { }

                    Player?.Play();
                    return;
                }

                if (PlayerItem.Status == AVPlayerItemStatus.Failed) Log.For(this).Error($"Failed to play {Path}");
                else Log.For(this).Error($"An error occured during playing {Path}");

                RetryToDownloadTrack();
            });
        }

        void Stop()
        {
            DownloadedFile = null;
            Player?.Pause();
            Player?.Seek(CoreMedia.CMTime.Zero);
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
                    Log.For(this).Error(err.Description);
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

            DidPlayToEndTimeObservation?.Dispose();
            StatusObservation?.Dispose();

            Stop();

            PlayerItem?.Dispose();
            PlayerItem = null;

            Player?.Dispose();
            Player = null;
        }
    }
}