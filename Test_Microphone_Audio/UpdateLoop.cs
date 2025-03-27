using System;
using System.Diagnostics;
using System.Windows.Threading;

namespace Test_Microphone_Audio
{
    public class UpdateLoop
    {
        private static UpdateLoop _instance;
        private DispatcherTimer _timer;
        private event Action OnUpdate;

        private UpdateLoop()
        {
            _timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(1000.0 / 60)
            };
            _timer.Tick += (s, e) => OnUpdate?.Invoke();
            _timer.Start();
        }

        public static UpdateLoop Instance => _instance ??= new UpdateLoop();

        public void Subscribe(Action updateAction)
        {
            OnUpdate += updateAction;
        }

        public void Unsubscribe(Action updateAction)
        {
            OnUpdate -= updateAction;
        }
    }
}
