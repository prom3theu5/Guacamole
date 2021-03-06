using System;
using System.Collections.Generic;
using System.Text;

namespace Guacamole.Communication.Irc
{
    /// <summary>
    /// This class represents a simplest mode that can be present in a channel or network with optional parameter
    /// </summary>
    [Serializable]
    public class SimpleMode
    {
        /// <summary>
        /// Character of this mode
        /// </summary>
        private char _char;
        private string _parameter = null;
        /// <summary>
        /// Character of this mode
        /// </summary>
        public char Mode
        {
            get
            {
                return _char;
            }
        }
        /// <summary>
        /// Parameter of this mode
        /// </summary>
        public string Parameter
        {
            get
            {
                return _parameter;
            }
        }

        /// <summary>
        /// Return true in case there is a parameter in this mode
        /// </summary>
        public bool ContainsParameter
        {
            get
            {
                return _parameter != null;
            }
        }

        /// <summary>
        /// Creates a new instance of simple mode
        /// </summary>
        /// <param name="mode">Mode</param>
        /// <param name="parameter">Parameter</param>
        public SimpleMode(char mode, string parameter)
        {
            _char = mode;
            _parameter = parameter;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="Client.SimpleMode"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="Client.SimpleMode"/>.</returns>
        public override string ToString()
        {
            if (ContainsParameter)
            {
                return "+" + _char.ToString() + " " + Parameter;
            }
            return "+" + _char.ToString();
        }
    }

    /// <summary>
    /// Every mode can be represented by this, it can even contain multiple modes in 1 container
    /// Supports both channel and user modes and prefixes
    /// </summary>
    [Serializable]
    public class NetworkMode
    {
        /// <summary>
        /// Raw mode
        /// </summary>
        public List<string> _Mode = new List<string>();
        /// <summary>
        /// Optional parameters for each mode
        /// </summary>
        public string Parameters = null;
        /// <summary>
        /// Network associated with mode
        /// </summary>
        [NonSerialized]
        public Network network = null;
        /// <summary>
        /// Type
        /// </summary>
        public ModeType _ModeType = ModeType.Network;

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="Client.NetworkMode"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="Client.NetworkMode"/>.</returns>
        public override string ToString()
        {
            string _val = "";
            int curr = 0;
            lock (_Mode)
            {
                while (curr < _Mode.Count)
                {
                    _val += _Mode[curr];
                    curr++;
                }
            }
            return "+" + _val;
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="MT"></param>
        /// <param name="_Network"></param>
        public NetworkMode(ModeType MT, Network _Network)
        {
            _ModeType = MT;
            network = _Network;
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="DefaultMode"></param>
        public NetworkMode(string DefaultMode)
        {
            ChangeMode(DefaultMode);
        }

        /// <summary>
        /// This needs to be there for serialization to work
        /// </summary>
        public NetworkMode()
        {
            // place holder :)
            network = null;
        }

        /// <summary>
        /// Change mode
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool ChangeMode(string text)
        {
            char prefix = ' ';
            foreach (char _x in text)
            {
                if (network != null && _ModeType != ModeType.Network)
                {
                    switch (_ModeType)
                    {
                        case ModeType.User:
                            if (network.CModes.Contains(_x))
                            {
                                continue;
                            }
                            break;
                        case ModeType.Channel:
                            if (network.CUModes.Contains(_x) || network.PModes.Contains(_x))
                            {
                                continue;
                            }
                            break;
                    }
                }
                if (_x == ' ')
                {
                    return true;
                }
                if (_x == '-')
                {
                    prefix = _x;
                    continue;
                }
                if (_x == '+')
                {
                    prefix = _x;
                    continue;
                }
                switch (prefix)
                {
                    case '+':
                        if (!_Mode.Contains(_x.ToString()))
                        {
                            this._Mode.Add(_x.ToString());
                        }
                        continue;
                    case '-':
                        if (_Mode.Contains(_x.ToString()))
                        {
                            this._Mode.Remove(_x.ToString());
                        }
                        continue;
                } continue;
            }
            return false;
        }

        /// <summary>
        /// Mode type
        /// </summary>
        public enum ModeType
        {
            /// <summary>
            /// Channel mode
            /// </summary>
            Channel,
            /// <summary>
            /// User mode
            /// </summary>
            User,
            /// <summary>
            /// Network
            /// </summary>
            Network
        }
    }
}
