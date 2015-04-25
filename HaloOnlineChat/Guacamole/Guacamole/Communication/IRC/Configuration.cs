using System;

namespace Guacamole.Communication.Irc
{
    public class Configuration
    {
        public bool AggressiveMode = true;
        public bool AggressiveExceptions = false;
        public bool AggressiveBans = true;
        public bool AggressiveInvites = false;
        public bool AggressiveUsers = true;
        public int TrafficInterval = 1000;
        /// <summary>
        /// You can change this in case you want all mode data to be forwarded as raw IRC text after parsing,
        /// this is needed when you are using this for bouncers
        /// </summary>
        public bool ForwardModes = false;
        public string Nick = Defs.DefaultNick;
        public string Nick2 = null;
        public string GetNick2()
        {
            if (Nick2 == null)
            {
                Random rn = new Random(DateTime.Now.Millisecond);
                return Nick + rn.Next(1, 200).ToString();
            }
            return Nick2;
        }
    }
}

