namespace Zebble.Device
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public static partial class Audio
    {
        static List<AudioPlayer> Players = new List<AudioPlayer>();

        static AudioPlayer defaultPlayer;
        public static AudioPlayer DefaultPlayer => defaultPlayer ?? (defaultPlayer = new AudioPlayer());

        public static AudioPlayer CreatePlayer() => new AudioPlayer();
    }
}