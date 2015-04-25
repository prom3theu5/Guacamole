using System;
using System.Collections.Generic;

namespace Guacamole.Communication.Irc
{
    /// <summary>
    /// This is a global interface for channel modes with parameters
    /// </summary>
    [Serializable]
    public class ChannelParameterMode
    {
        /// <summary>
        /// Target of a mode
        /// </summary>
        public string Target = null;
        /// <summary>
        /// Time when it was set
        /// </summary>
        public string Time = null;
        /// <summary>
        /// User who set the ban / invite
        /// </summary>
        public string User = null;
    }

    /// <summary>
    /// Invite
    /// </summary>
    [Serializable]
    public class ChannelInvite : ChannelParameterMode
    {
        /// <summary>
        /// Creates a new instance of invite
        /// </summary>
        public ChannelInvite()
        {
            // This empty constructor is here so that we can serialize this
        }

        /// <summary>
        /// Creates a new instance of invite
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="target">Target</param>
        /// <param name="time">Time</param>
        public ChannelInvite(string user, string target, string time)
        {
            User = user;
            Target = target;
            Time = time;
        }
    }

    /// <summary>
    /// Exception
    /// </summary>
    [Serializable]
    public class ChannelBanException : ChannelParameterMode
    {
        /// <summary>
        /// Creates a new instance of channel ban exception (xml constructor only)
        /// </summary>
        public ChannelBanException()
        {
            // This empty constructor is here so that we can serialize this
        }
    }

    /// <summary>
    /// Simplest ban
    /// </summary>
    [Serializable]
    public class ChannelBan : ChannelParameterMode
    {
        /// <summary>
        /// Creates a new instance of simple ban (xml constructor only)
        /// </summary>
        public ChannelBan()
        {
            // This empty constructor is here so that we can serialize this
        }

        /// <summary>
        /// Creates a new instance of simple ban
        /// </summary>
        /// <param name="user">Person who set a ban</param>
        /// <param name="target">Who is target</param>
        /// <param name="time">Unix date when it was set</param>
        public ChannelBan(string user, string target, string time)
        {
            Target = target;
            User = user;
            Time = time;
        }
    }
    
    /// <summary>
    /// Channel object
    /// </summary>
    [Serializable]
    public class Channel : Target
    {
        /// <summary>
        /// Name of a channel including the special prefix, if it's unknown this variable is null
        /// </summary>
        public string Name
        {
            set
            {
                this.lname = null;
                this.name = value;
            }
            get
            {
                return this.name;
            }
        }
        private string name = null;
        public override string TargetName
        {
            get
            {
                return this.Name;
            }
        }
        /// <summary>
        /// Lower case of this channel that is used frequently, we cache it here so that we
        /// don't need to use expensive string functions to make it so often
        /// </summary>
        [Obsolete ("Use LowerName instead of this")]
        public string lName = null;
        private string lname = null;
        public string LowerName
        {
            get
            {
                if (this.Name == null)
                    return null;
                if (this.lname == null)
                    lname = this.Name.ToLower();
                return lname;
            }
        }
        /// <summary>
        /// Network the channel belongs to
        /// </summary>
        [NonSerialized]
        public Network _Network = null;
        /// <summary>
        /// List of all users in current channel
        /// </summary>
        [NonSerialized]
        protected Dictionary<string, User> UserList = new Dictionary<string, User>();
        /// <summary>
        /// Topic, if it's unknown this variable is null
        /// </summary>
        public string Topic = null;
        /// <summary>
        /// User who set a topic
        /// </summary>
        public string TopicUser = "<Unknown user>";
        /// <summary>
        /// Date when a topic was set
        /// </summary>
        public int TopicDate = 0;
        public string Password = null;
        /// <summary>
        /// Invites
        /// </summary>
        public List<ChannelInvite> Invites = null;
        /// <summary>
        /// List of bans set
        /// </summary>
        public List<ChannelBan> Bans = null;
        /// <summary>
        /// Exception list 
        /// </summary>
        public List<ChannelBanException> Exceptions = null;
        public DateTime CreationTime;
        public string Website = null;
        /// <summary>
        /// If channel output is temporarily hidden
        /// </summary>
        public bool TemporarilyHidden = false;
        /// <summary>
        /// If true the channel is processing the /who data
        /// </summary>
        public bool IsParsingWhoData = false;
        /// <summary>
        /// If true the channel is processing invites
        /// </summary>
        public bool IsParsingInviteData = false;
        /// <summary>
        /// If true the channel is processing ban data
        /// </summary>
        public bool IsParsingBanData = false;
        /// <summary>
        /// If true the channel is processing exception data
        /// </summary>
        public bool IsParsingExceptionData = false;
        /// <summary>
        /// If true the channel is processing whois data
        /// </summary>
        public bool IsParsingWhoisData = false;
        /// <summary>
        /// Channel mode
        /// </summary>
        [NonSerialized]
        public NetworkMode ChannelMode = null;
        /// <summary>
        /// If true the window is considered usable, in case it's false, the window is flagged as parted channel
        /// </summary>
        public bool ChannelWork = false;
        /// <summary>
        /// Whether part from this channel was requested, this is used to detect if part was forced or not
        /// </summary>
        public bool PartRequested = false;
        [Obsolete ("no longer used")]
        public bool dispose = false;
        /// <summary>
        /// If this is false the channel is not being used / you aren't in it or you can't access it
        /// </summary>
        public virtual bool IsAlive
        {
            get
            {
                if (!ChannelWork)
                {
                    return false;
                }
                if (_Network != null)
                {
                    return _Network.IsConnected;
                }
                return false;
            }
        }
        
        public virtual int UserCount
        {
            get
            {
                return this.UserList.Count;
            }
        }

        /// <summary>
        /// Constructor (simple)
        /// </summary>
        public Channel()
        {
            ChannelWork = true;
            ChannelMode = new NetworkMode(NetworkMode.ModeType.Channel, _Network);
            Topic = "";
            TopicUser = "";
        }

        /// <summary>
        /// Constructor (normal)
        /// </summary>
        public Channel(Network network)
        {
            _Network = network;
            ChannelWork = true;
            ChannelMode = new NetworkMode(NetworkMode.ModeType.Channel, _Network);
            Topic = "";
            TopicUser = "";
        }

        /// <summary>
        /// Renew bans
        /// </summary>
        public void ReloadBans()
        {
            IsParsingBanData = true;
            if (Bans == null)
            {
                Bans = new List<ChannelBan>();
            }
            else
            {
                lock (Bans)
                {
                    Bans.Clear();
                }
            }
            _Network.Transfer("MODE " + Name + " +b");
        }

        public void ReloadInvites()
        {
            IsParsingExceptionData = true;
            if (Invites == null)
            {
                Invites = new List<ChannelInvite>();
            }
            else
            {
                lock (Invites)
                {
                    Invites.Clear();
                }
            }
            _Network.Transfer("MODE " + Name + " +I");
        }

        public void ReloadExceptions()
        {
            IsParsingExceptionData = true;
            if (Exceptions == null)
            {
                Exceptions = new List<ChannelBanException>();
            }
            else
            {
                lock (Exceptions)
                {
                    Exceptions.Clear();
                }
            }
            _Network.Transfer("MODE " + Name + " +e");
        }

        /// <summary>
        /// Return true if channel contains the given user name
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool ContainsUser(string user)
        {
            if (UserFromName(user) != null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Part this channel
        /// </summary>
        public virtual void Part()
        {
            if (IsAlive && _Network != null)
            {
                _Network.Part(this);
                PartRequested = true;
            }
        }

        public virtual void ClearUsers()
        {
            this.UserList.Clear();
        }
        
        /// <summary>
        /// Return true if a channel is matching ban (exact, not a mask)
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public virtual bool ContainsBan(string host)
        {
            if (Bans == null)
            {
                return false;
            }
            lock (Bans)
            {
                foreach (ChannelBan name in Bans)
                {
                    if (name.Target == host)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public virtual Dictionary<string, User> RetrieveUL()
        {
            lock (this.UserList)
            {
                return new Dictionary<string, User>(this.UserList);
            }
        }

        /// <summary>
        /// This function returns a special user mode for a user that should be in user list (for example % for halfop or @ for operator)
        /// </summary>
        /// <param name="nick"></param>
        /// <returns></returns>
        protected static string uchr(User nick)
        {
            return nick.ChannelPrefix;
        }
        
        public virtual void InsertUser(User user)
        {
            lock (this.UserList)
            {
                // don't use LowNick directly it can change between these 2 instructions
                string ln = user.LowerNick;
                user.Channel = this;
                if (!this.UserList.ContainsKey(ln))
                {
                    this.UserList.Add(ln, user);
                }
            }
        }
        
        /// <summary>
        /// Insert ban to a ban list, this will not set a ban to channel, this will only set it into memory of pidgeon
        /// </summary>
        /// <param name="ban">Host</param>
        /// <param name="user">User who set</param>
        /// <param name="time">Time when it was set</param>
        public virtual void InsertBan(string ban, string user, string time = "0")
        {
            if (Bans == null)
            {
                Bans = new List<ChannelBan>();
            }
            ChannelBan br = new ChannelBan(user, ban, time);
            lock (Bans)
            {
                Bans.Add(br);
            }
        }

        /// <summary>
        /// Removes a ban where target is matching "ban" this needs to be perfect match (a != A and x* != xX) you can't use mask
        /// </summary>
        /// <param name="ban"></param>
        /// <returns></returns>
        public virtual bool RemoveBan(string ban)
        {
            if (this.Bans == null)
            {
                return false;
            }
            ChannelBan br = null;
            lock (Bans)
            {
                foreach (ChannelBan xx in Bans)
                {
                    if (xx.Target == ban)
                    {
                        br = xx;
                        break;
                    }
                }
                if (br != null)
                {
                    Bans.Remove(br);
                    return true;
                }
            }
            return false;
        }

        public virtual void RemoveUser(User user)
        {
            lock (this.UserList)
            {
                string name = user.Nick.ToLower ();
                if (this.UserList.ContainsKey (name))
                {
                    this.UserList.Remove (name);
                }
            }
        }
        
        public virtual void RemoveUser(string nick)
        {
            nick = nick.ToLower();
            lock (this.UserList)
            {
                if (this.UserList.ContainsKey(nick))
                {
                    this.UserList.Remove(nick);
                }
            }
        }

        public virtual User GetSelf()
        {
            if (this._Network != null)
            {
                return this.UserFromName(this._Network.Nickname);
            }
            return null;
        }
        
        /// <summary>
        /// Return user object if specified user exist
        /// </summary>
        /// <param name="name">User name</param>
        /// <returns></returns>
        public virtual User UserFromName(string name)
        {
            if (name == null)
            {
                throw new Exception("User name can't be null");
            }
            User user = null;
            this.UserList.TryGetValue (name.ToLower (System.Globalization.CultureInfo.CurrentUICulture), out user);
            return user;
        }
    }
}
