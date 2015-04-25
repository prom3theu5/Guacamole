using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Guacamole.Game
{
    public class PlayerNameChangedEventArgs : EventArgs
    {
        public string PlayerName { get; set; }
    }

    public class Player
    {
        public event EventHandler<PlayerNameChangedEventArgs> PlayerNameChanged;
        public string _playerName { get; set; }
        public ProcessSpy Game { get; set; }
        public bool Running { get; set; }

        public Player(ProcessSpy game)
        {
            Game = game;
            Running = true;
        }

        protected virtual void OnPlayerNameChanged(PlayerNameChangedEventArgs e)
        {
            EventHandler<PlayerNameChangedEventArgs> handler = PlayerNameChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private string GetPlayerName()
        {
            return Encoding.Unicode.GetString(Game.Read(0x5250048, 22));
        }

        public void Run()
        {
            while (Running)
            {
                Thread.Sleep(5000);
                var playerName = GetPlayerName().Replace("\0","");
                if (playerName == _playerName) continue;
                _playerName = playerName;
                PlayerNameChangedEventArgs sesh = new PlayerNameChangedEventArgs();
                sesh.PlayerName = playerName;
                OnPlayerNameChanged(sesh);
            }
        }
    }
}
