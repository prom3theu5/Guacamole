using System;
using System.Collections.Generic;
using System.Threading;

namespace Guacamole.Communication.Irc
{
    /// <summary>
    /// Information about all threads that are produced by this library
    /// </summary>
    public class ThreadManager
    {
        private static List<Thread> Threads = new List<Thread>();

        /// <summary>
        /// List of all threads that were created by this library, this list can't be modified
        /// </summary>
        public static List<Thread> ThreadList
        {
            get
            {
                return new List<Thread>(Threads);
            }
        }

        public static void RemoveThread(Thread thread)
        {
            lock(Threads)
            {
                if (Threads.Contains(thread))
                {
                    Threads.Remove(thread);
                }
            }
        }

        public static void RegisterThread(Thread thread)
        {
            lock (Threads)
            {
                if (!Threads.Contains(thread))
                {
                    Threads.Add(thread);
                }
            }
        }

        public static void KillThread(Thread thread)
        {
            if (thread == null)
            {
                return;
            }
            if (Thread.CurrentThread != thread)
            {
                if (thread.ThreadState == ThreadState.WaitSleepJoin &&
                    thread.ThreadState == ThreadState.Running)
                {
                    thread.Abort();
                }
            }
            RemoveThread(thread);
        }
    }
}

