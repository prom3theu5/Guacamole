using System;
using System.Diagnostics;

namespace Guacamole.Communication.Irc
{
    /// <summary>
    /// Profiler
    /// </summary>
    public class Profiler
    {
        /// <summary>
        /// Time
        /// </summary>
        private Stopwatch time = new Stopwatch();
        /// <summary>
        /// Function that is being checked
        /// </summary>
        public string Function = null;

        /// <summary>
        /// Creates a new instance with name of function
        /// </summary>
        /// <param name="function"></param>
        public Profiler(string function)
        {
            Function = function;
            time.Start();
        }

        /// <summary>
        /// Called when profiler is supposed to be stopped
        /// </summary>
        public string Done()
        {
            time.Stop();
            return ("PROFILER: " + Function + ": " + time.ElapsedMilliseconds.ToString());
        }
    }
}

