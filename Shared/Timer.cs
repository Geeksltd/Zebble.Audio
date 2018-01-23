namespace Zebble.Device
{
    using System;

    public class Timer
    {
        public Action<TimerState> TickAction;

        public Timer(TimeSpan interval)
        {
            var state = new TimerState();

            var timerDelegate = new System.Threading.TimerCallback(Tick);
            var timer = new System.Threading.Timer(timerDelegate, state, TimeSpan.FromSeconds(0), interval);

            state.Timer = timer;
        }

        void Tick(object state)
        {
            var timerState = (TimerState)state;
            TickAction.Invoke(timerState);
        }

        public class TimerState
        {
            public System.Threading.Timer Timer;
        }
    }
}
