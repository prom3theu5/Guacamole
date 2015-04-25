using System;

namespace Guacamole.Communication.Irc
{
    /// <summary>
    /// Low level object with basic IRC user info
    /// </summary>
    public class UserInfo : Target
    {
        public string Nick;
        public string Ident;
        public string Host;
        public override string TargetName
        {
            get
            {
                return this.Nick;
            }
        }

        public UserInfo()
        {
            this.Nick = null;
            this.Ident = null;
            this.Host = null;
        }

        public UserInfo(string nick, string ident, string host)
        {
            this.Nick = nick;
            this.Ident = ident;
            this.Host = host;
        }

        public UserInfo(string source)
        {
            if (source.Contains("!"))
            {
                this.Nick = source.Substring(0, source.LastIndexOf("!", StringComparison.Ordinal));
                this.Ident = source.Substring(source.LastIndexOf("!") + 1);
                if (this.Ident.Contains("@"))
                {
                    this.Host = this.Ident.Substring(this.Ident.LastIndexOf("@") + 1);
                    this.Ident = this.Ident.Substring(0, this.Ident.LastIndexOf("@"));
                }
            }
            else
            {
                this.Nick = source;
            }
        }

        public override string ToString()
        {
            return Nick + "!" + Ident + "@" + Host;
        }
    }
}

