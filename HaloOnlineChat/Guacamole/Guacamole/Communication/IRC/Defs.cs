using System;

namespace Guacamole.Communication.Irc
{
    public class Defs
    {
        public static readonly Version Version = new Version(1, 0, 3);
        /// <summary>
        /// The default nick
        /// 
        /// Change this to nick you want to have as a default for every new instance
        /// of network
        /// </summary>
        public static string DefaultNick =      "Sierra-117";
        public static string DefaultQuit =      "Wake Me When You Need Me";
        public static string DefaultVersion =   "Guacamole V1";
        public static bool UsingProfiler =      false;
        public const int DefaultIRCPort =       6667;
        public const int DefaultSSLIRCPort =    6697;

        /// <summary>
        /// Convert a date to unix one
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static double ConvertDateToUnix(DateTime time)
        {
            DateTime EPOCH = new System.DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan span = (time - EPOCH);
            return span.TotalSeconds;
        }

        /// <summary>
        /// Convert a unix timestamp to human readable time
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string ConvertFromUNIXToStringSafe(string time)
        {
            try
            {
                if (time == null)
                {
                    return "Unable to read: null";
                }
                double unixtimestmp = double.Parse(time);
                return (new DateTime(1970, 1, 1, 0, 0, 0)).AddSeconds(unixtimestmp).ToString();
            }
            catch (Exception)
            {
                return "Unable to read: " + time;
            }
        }

        /// <summary>
        /// Convert a unix timestamp to human readable time
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string ConvertFromUNIXToString(string time)
        {
            if (time == null)
            {
                return null;
            }
            double unixtimestmp = double.Parse(time);
            return (new DateTime(1970, 1, 1, 0, 0, 0)).AddSeconds(unixtimestmp).ToString();
        }

        /// <summary>
        /// Return a DateTime object from unix time
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static DateTime ConvertFromUNIX(string time)
        {
            if (time == null)
            {
                throw new Exception("Provided time was NULL");
            }
            double unixtimestmp = double.Parse(time);
            return new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(unixtimestmp);
        }

        /// <summary>
        /// Return a DateTime object from unix time
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static DateTime ConvertFromUNIX(double time)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(time);
        }

        /// <summary>
        /// Priority of irc message
        /// </summary>
        public enum Priority
        {
            /// <summary>
            /// High
            /// </summary>
            High = 8,
            /// <summary>
            /// Normal
            /// </summary>
            Normal = 2,
            /// <summary>
            /// Low
            /// </summary>
            Low = 1,
            /// <summary>
            /// Lowest
            /// </summary>
            None = 0
        }
    }
}

