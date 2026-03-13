using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Textfyre.UI.Helpers
{
    public class MouseClickManager
    {
        public event MouseButtonEventHandler Click;
        public event MouseButtonEventHandler DoubleClick;

        private bool Clicked { get; set; }

        public Control Control { get; set; }

        public int Timeout { get; set; }

        private DispatcherTimer _timer;
        private object _pendingSender;
        private MouseButtonEventArgs _pendingArgs;

        public MouseClickManager(Control control, int timeout)
        {
            this.Clicked = false;
            this.Control = control;
            this.Timeout = timeout;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(timeout);
            _timer.Tick += Timer_Tick;
        }

        public void HandleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.Clicked)
            {
                // Second click within timeout — double-click
                this.Clicked = false;
                _timer.Stop();
                OnDoubleClick(sender, e);
            }
            else
            {
                // First click — wait for possible second click
                this.Clicked = true;
                _pendingSender = sender;
                _pendingArgs = e;
                _timer.Start();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();

            if (this.Clicked)
            {
                this.Clicked = false;
                OnClick(_pendingSender, _pendingArgs);
            }
        }

        private void OnClick(object sender, MouseButtonEventArgs e)
        {
            MouseButtonEventHandler handler = Click;

            if (handler != null)
                handler(sender, e);
        }

        private void OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MouseButtonEventHandler handler = DoubleClick;

            if (handler != null)
                handler(sender, e);
        }
    }
}
