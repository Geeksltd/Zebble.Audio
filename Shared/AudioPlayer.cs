using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zebble.Device
{
    partial class AudioPlayer
    {
        TaskCompletionSource<bool> Completion = new TaskCompletionSource<bool>();
        string File;

        public AudioPlayer(string file) { File = file; }

        partial void Dispose();

        public void Stop()
        {
            Dispose();
            Completion.TrySetResult(false);
        }
    }
}
