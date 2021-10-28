namespace Zebble.Device
{
    public static partial class Audio
    {
        static AudioPlayer defaultPlayer;
        public static AudioPlayer DefaultPlayer => defaultPlayer ??= new AudioPlayer();
    }
}