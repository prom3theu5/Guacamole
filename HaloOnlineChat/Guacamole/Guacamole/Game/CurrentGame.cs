using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Guacamole.Game
{
    public class CurrentGameChangedEventArgs : EventArgs
    {
        public string GameName { get; set; }
        public string GameMap { get; set; }
        public string GameType { get; set; }
    }

    public class CurrentGame
    {
        public event EventHandler<CurrentGameChangedEventArgs> GameChanged;
        public string _gameName { get; set; }
        public string _gameMap { get; set; }
        public string _gameType { get; set; }

        public ProcessSpy Game { get; set; }
        public bool Running { get; set; }

        public CurrentGame(ProcessSpy game)
        {
            Game = game;
            Running = true;
        }

        protected virtual void OnGameChanged(CurrentGameChangedEventArgs e)
        {
            EventHandler<CurrentGameChangedEventArgs> handler = GameChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private Dictionary<string, object> GetGameInfo()
        {
            return new Dictionary<string, object>()
            {
                {"name", Encoding.Unicode.GetString(Game.Read(0x01863B20, 32)).Replace("\0","")},
                {"map", Encoding.Unicode.GetString(Game.Read(0x01863ACA, 32)).Replace("\0","")},
                {"gametype", Encoding.Unicode.GetString(Game.Read(0x01863A9C, 32)).Replace("\0","")}
            };
        } 

        public void Run()
        {
            while (true)
            {
                Thread.Sleep(5000);
                var game = GetGameInfo();
                if (game["name"].ToString() == _gameName) continue;
                _gameName = game["name"].ToString();
                _gameMap = game["map"].ToString();
                _gameType = game["gametype"].ToString();
                CurrentGameChangedEventArgs args = new CurrentGameChangedEventArgs();
                args.GameName = _gameName;
                args.GameMap = _gameMap;
                args.GameType = _gameType;
                OnGameChanged(args);
            }
        }
    }
}
