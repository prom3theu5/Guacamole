using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Guacamole.Game
{
    public class SessionChangedEventArgs : EventArgs
    {
        public string Server { get; set; }
        public string Client { get; set; }
        public string PreviousServer { get; set; }
    }

    public class Sessions
    {
        public event EventHandler<SessionChangedEventArgs> SessionChanged;
        public string Server { get; set; }
        public string Client { get; set; }
        public ProcessSpy Game { get; set; }
        public bool Running { get; set; }

        public Sessions(ProcessSpy game)
        {
            Game = game;
            Running = true;
            Server = "";
            Client = "";
        }

        protected virtual void OnSessionChanged(SessionChangedEventArgs e)
        {
            EventHandler<SessionChangedEventArgs> handler = SessionChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private Guid[] GetXnetParams()
        {
            return new Guid[] {
                new Guid(Game.Read(0x2247b80, 16)),
                new Guid(Game.Read(0x2247b90, 16))
            };
        }

        public bool IsHostingOnlineSession()
        {
            for (var i = 0; i < 4; i++)
                if (BitConverter.ToInt32(Game.Read(0x1a29d38 + i * 8, 4), 0) == 1)
                    return true;
            return false;
        }

        public void Run()
        {
            while (Running)
            {
                Thread.Sleep(5000);
                var session = GetXnetParams();
                if (Server.ToString().Equals(session[0].ToString().Trim().Replace("-", "")) && Client.ToString().Equals(session[1].ToString().Trim().Replace("-", ""))) continue;
                var previousSession = Server;
                Server = session[0].ToString().Trim().Replace("-", "");
                Client = session[1].ToString().Trim().Replace("-", "");
                OnSessionChanged(new SessionChangedEventArgs(){Server = Server, Client = Client, PreviousServer = previousSession});
            }
        }
    }
}
