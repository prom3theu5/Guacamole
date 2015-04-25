using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Guacamole.Game
{
    public class HostNameChangedEventArgs : EventArgs
    {
        public string HostName { get; set; }
    }

    public class Host
    {
        public event EventHandler<HostNameChangedEventArgs> HostNameChanged;
        public string _hostName { get; set; }
        public ProcessSpy Game { get; set; }
        public bool Running { get; set; }
        public bool LocalHost { get; set; }

        public Host(ProcessSpy game)
        {
            Game = game;
            Running = true;
        }

        protected virtual void OnHostNameChanged(HostNameChangedEventArgs e)
        {
            EventHandler<HostNameChangedEventArgs> handler = HostNameChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public bool IsHostingOnlineSession()
        {
            for (var i = 0; i < 4; i++)
                if (BitConverter.ToInt32(Game.Read(0x1a29d38 + i * 8, 4), 0) == 1)
                    return true;
            return false;
        }

        private string GetHostName()
        {
            return Encoding.Unicode.GetString(Game.Read(0x3ECFD0C4, 22));
        }

        public void Run()
        {
            while (true)
            {
                Thread.Sleep(5000);
                var hostName = GetHostName();
                if (hostName == _hostName) continue;
                _hostName = hostName;
                LocalHost = IsHostingOnlineSession();
                HostNameChangedEventArgs sesh = new HostNameChangedEventArgs();
                sesh.HostName = hostName;
                OnHostNameChanged(sesh);
            }
        }
    }
}
