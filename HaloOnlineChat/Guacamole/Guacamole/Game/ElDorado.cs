using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Guacamole.Communication;
using Guacamole.Communication.Irc;
using Guacamole.Helpers;

namespace Guacamole.Game
{
    class Kernel32
    {
        [DllImport("kernel32.dll")]
        public static extern int OpenProcess(int dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(int hObject);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);
    }

    public class ProcessSpy
    {
        public uint ProcessId;
        readonly int _handle;

        public ProcessSpy(uint processId)
        {
            this.ProcessId = processId;
            _handle = Kernel32.OpenProcess(0x38, false, processId);
        }

        ~ProcessSpy()
        {
            Kernel32.CloseHandle(_handle);
        }

        public byte[] Read(int address, int size)
        {
            var buf = new byte[size];
            int bytesread = 0;
            Kernel32.ReadProcessMemory(_handle, address, buf, size, ref bytesread);
            return buf.Take(bytesread).ToArray();
        }
    }

    public class ElDorado
    {
        public bool IsRunning = false;
        public bool UseIRC { get; set; }
        private ProcessSpy _gameProcess;

        public Sessions _monitorSession;
        public Player _monitorPlayer;
        private string IrcNick;
        public FixedSizeObservable<string> AllMessages { get; set; }

        public event EventHandler ProcessLaunched;
        public event EventHandler ProcessClosed;
        public Guacamole.Communication.Irc.Protocols.ProtocolIrc _ircClient { get; set; }
        public int IrcPort { get; set; }
        public string IrcServer { get; set; }
        public Server _chatServer { get; set; }
        public Client _chatClient { get; set; }

        public ElDorado()
        {
            AllMessages = new FixedSizeObservable<string>(20);
            UseIRC = false;
        }

        void SpyOnGameProcess(uint pid)
        {
            if (!IsRunning)
            {
                AllMessages.Add("DETECTED GAME PROCESS!");
                _gameProcess = new ProcessSpy(pid);
                IsRunning = true;

                StartMonitors();

                if (ProcessLaunched != null)
                    ProcessLaunched(this, EventArgs.Empty);
            }
        }

        #region Monitor Memory Regions
        private void StartMonitors()
        {
            Task.Run(() =>
            {
                _monitorSession = new Sessions(_gameProcess);
                _monitorSession.SessionChanged += monitorSession_SessionChanged;
                _monitorSession.Run();
            });

            Task.Run(() =>
            {
                _monitorPlayer = new Player(_gameProcess);
                _monitorPlayer.PlayerNameChanged += monitorPlayer_PlayerNameChanged;
                _monitorPlayer.Run();
            });
        }

        public void StopMonitors()
        {
            if (_monitorSession != null && _monitorSession.Running)
            {
                _monitorSession.Running = false;
            }

            if (_monitorPlayer != null && _monitorPlayer.Running)
            {
                _monitorPlayer.Running = false;
            }

        }

        void monitorPlayer_PlayerNameChanged(object sender, PlayerNameChangedEventArgs e)
        {
            AllMessages.Clear();
            AllMessages.Add("Player Name Changed To: " + e.PlayerName);

            if (UseIRC)
            {
                if (_ircClient != null && _ircClient.IsConnected && !_ircClient.IRCNetwork.Nickname.Contains(_monitorPlayer._playerName))
                    DisconnectFromIrcChannel();
                ConnectToIrcServerAndChannel();
            }
        
        }

        void monitorSession_SessionChanged(object sender, SessionChangedEventArgs e)
        {
            AllMessages.Add("Session Changed:");
            AllMessages.Add("GameLoddyId: " + e.Server);
            if (_monitorSession.Server != "")
            {
                if (!UseIRC)
                {
                    KillClientsAndServer();

                    if (_monitorSession.IsHostingOnlineSession())
                    {
                        CreateServerAndJoin();
                        return;
                    }

                    //JoinRemoteServer(insert ip string); Need the IP to finish this garbage
                }
                else
                {
                    if (_ircClient != null && _ircClient.IsConnected && _ircClient.IRCNetwork.Nickname.Contains(_monitorPlayer._playerName))
                    {
                        ConnectToNewIrcChannel(e.PreviousServer);
                        return;
                    }
                    else if (_ircClient != null && _ircClient.IsConnected && !_ircClient.IRCNetwork.Nickname.Contains(_monitorPlayer._playerName))
                    {
                        _ircClient.Exit();
                        ConnectToIrcServerAndChannel();
                    }
                }
            }
        }
        #endregion
       
        #region If Using IRC
        public void ConnectToIrcServerAndChannel()
        {
            if (string.IsNullOrWhiteSpace(_monitorPlayer._playerName)) return;
            Task.Run(() =>
            {
                Random rnd = new Random();
                IrcNick = _monitorPlayer._playerName + "-" + rnd.Next(1, 999);
                _ircClient = new Communication.Irc.Protocols.ProtocolIrc();
                _ircClient.Server = IrcServer;
                _ircClient.IRCNetwork = new Network(_ircClient.Server, _ircClient);
                _ircClient.IRCNetwork.ServerName = IrcServer;
                _ircClient.Port = 6667;
                _ircClient.IRCNetwork.On_PRIVMSG += IRCNetwork_On_PRIVMSG;
                _ircClient.IRCNetwork.On_JOIN += IRCNetwork_On_JOIN;
                _ircClient.IRCNetwork.On_PART += IRCNetwork_On_PART;
                _ircClient.IRCNetwork.Nickname = IrcNick;
                _ircClient.IRCNetwork.UserName = "Halo Player " + IrcNick;
                _ircClient.Open();
                _ircClient.Join("#" + _monitorSession.Server);
                AllMessages.Clear();
                AllMessages.Add("Successfully started your chat server.");
            });
        }

        void IRCNetwork_On_PART(object sender, Network.NetworkChannelDataEventArgs e)
        {
            if (e.SourceInfo.Nick != IrcNick)
                AllMessages.Add(String.Format("<<[{0}] Left The Channel at {1}>>", e.SourceInfo.Nick, DateTime.Now.ToLongTimeString()));
        }

        void IRCNetwork_On_JOIN(object sender, Network.NetworkChannelEventArgs e)
        {
            if (e.SourceInfo.Nick != IrcNick)
                AllMessages.Add(String.Format("<<[{0}] Joined The Channel at {1}>>", e.SourceInfo.Nick, DateTime.Now.ToLongTimeString()));
            else
                AllMessages.Add(String.Format("<<You have joined the new Session Channel>>", e.SourceInfo.Nick));
        }

        private void IRCNetwork_On_PRIVMSG(object sender, Network.NetworkPRIVMSGEventArgs e)
        {
            AllMessages.Add(String.Format("[{0}] [{1}] {2}", DateTime.Now.ToLongTimeString(), e.SourceInfo.Nick, e.Message));
        }

        public void ConnectToNewIrcChannel(string previousServerId)
        {
            _ircClient.Part("#" + previousServerId);
            _ircClient.Join("#" + _monitorSession.Server);
            AllMessages.Clear();
            AllMessages.Add("Connected to New Server");
        }

        public void DisconnectFromIrcChannel()
        {
            if (_ircClient != null && _ircClient.IsConnected) _ircClient.Exit();
        }

        public void SendIrcMessage(string message)
        {
            if (!_ircClient.IsConnected) return;
            _ircClient.Message(message, "#" + _monitorSession.Server);
            AllMessages.Add(String.Format("[{0}] [{1}] {2}", DateTime.Now.ToLongTimeString(), _monitorPlayer._playerName, message));
        }
        #endregion

        #region If Using Built In Server
        public void CreateServerAndJoin()
        {
            Task.Run(() =>
            {
                _chatServer = new Server();
                _chatServer.StartServer();
                _chatClient = new Client("[HOST] [" + _monitorPlayer._playerName + "]", "127.0.0.1");
                _chatClient.Login();
                _chatClient._messagesCollection.CollectionChanged += _messagesCollection_CollectionChanged;
            });
        }


        void _messagesCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            foreach (string item in e.NewItems)
            {
                AllMessages.Add(item);    
            }
        }

       

        public void CreateTestClient(string ip)
        {
            Task.Run(() => {
            _chatClient = new Client("ILoveCortanasBlueNipples", ip);
            _chatClient.Login();
            _chatClient._messagesCollection.CollectionChanged += _messagesCollection_CollectionChanged;
            });
        }

        public void JoinRemoteServer(string ip)
        {
            Task.Run(() => { 
            _chatClient = new Client("[" + _monitorPlayer._playerName + "]", ip);
            _chatClient.Login();
            });
        }

        public void KillClientsAndServer()
        {
            if (_chatClient != null && _chatClient._isConnected) _chatClient.LogOut();
            if (_chatServer != null && _chatServer._isAlive) _chatServer.StopServer();
        }

        public void SendMessage(string message)
        {
            if (!_chatClient._isConnected)
            {
                Console.WriteLine("Client is not connected!");
                return;
            }
            _chatClient.SendMessage(message);
        }

        #endregion

        public void MonitorProcesses()
        {
            var processes = Process.GetProcessesByName("eldorado");

            if (processes.Length > 0)
                SpyOnGameProcess((uint)processes.OrderBy(p => p.StartTime).First().Id);

            var openQuery = new WqlEventQuery("__InstanceCreationEvent", new TimeSpan(0, 0, 1),
                "TargetInstance isa \"Win32_Process\"");
            var closeQuery = new WqlEventQuery("__InstanceDeletionEvent", new TimeSpan(0, 0, 1),
                "TargetInstance isa \"Win32_Process\"");

            var openWatcher = new ManagementEventWatcher(openQuery);
            openWatcher.EventArrived += openWatcher_EventArrived;
            openWatcher.Start();

            var closeWatcher = new ManagementEventWatcher(closeQuery);
            closeWatcher.EventArrived += closeWatcher_EventArrived;
            closeWatcher.Start();
        }

        void openWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            var instance = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;
            var name = (string)instance["Name"];
            var pid = (uint)instance["ProcessId"];

            if (name == "eldorado.exe" && !IsRunning)
                SpyOnGameProcess(pid);
        }

        void closeWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            var instance = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;
            var name = (string)instance["Name"];
            var pid = (uint)instance["ProcessId"];

            if (name == "eldorado.exe" && _gameProcess.ProcessId == pid)
            {
                AllMessages.Add("Game process closed.\nStopping Chat");
                IsRunning = false;

                StopMonitors();

                if (UseIRC && _ircClient != null && _ircClient.IsConnected)
                {
                    _ircClient.Exit();
                }

                if (!UseIRC && _chatClient != null && _chatClient._isConnected)
                {
                    _chatClient.LogOut();
                }

                AllMessages.Add("Disconnected From Chat");

                if (ProcessClosed != null)
                    ProcessClosed(this, EventArgs.Empty);
            }
        }
    }
}
