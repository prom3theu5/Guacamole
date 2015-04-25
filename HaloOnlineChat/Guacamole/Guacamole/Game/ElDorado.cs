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
        public Host _monitorHost;
        public ObservableCollection<string> AllMessages { get; set; }

        public event EventHandler ProcessLaunched;
        public event EventHandler ProcessClosed;
        public Guacamole.Communication.Irc.Protocols.ProtocolIrc _ircClient { get; set; }
        public int IrcPort { get; set; }
        public string IrcServer { get; set; }
        public Server _chatServer { get; set; }
        public Client _chatClient { get; set; }

        public ElDorado()
        {
            AllMessages = new ObservableCollection<string>();
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

            Task.Run(() =>
            {
                _monitorHost = new Host(_gameProcess);
                _monitorHost.HostNameChanged += _monitorHost_HostNameChanged;
                _monitorHost.Run();
            });
        }

        void _monitorHost_HostNameChanged(object sender, HostNameChangedEventArgs e)
        {
            Console.WriteLine("Host Name Changed: " + e.HostName);
            if (_monitorHost.LocalHost) AllMessages.Add("You are Hosting a Game");
            else AllMessages.Add("You are not the Host");
        }

        void _monitorGame_GameChanged(object sender, CurrentGameChangedEventArgs e)
        {
            Console.WriteLine("Current Game: " + e.GameName + " - " + e.GameMap + " - " + e.GameType);
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

            if (_monitorHost != null && _monitorHost.Running)
            {
                _monitorHost.Running = false;
            }
        }

        void monitorPlayer_PlayerNameChanged(object sender, PlayerNameChangedEventArgs e)
        {
            AllMessages.Add("Player Name Changed To: " + e.PlayerName);
        }

        void monitorSession_SessionChanged(object sender, SessionChangedEventArgs e)
        {
            Console.WriteLine("Session Changed:");
            Console.WriteLine("GameLoddyId: " + e.Server);
            Console.WriteLine("ClientId: " + e.Client);
            if (_monitorSession.Server != "")
            {
                if (!UseIRC)
                {
                    KillClientsAndServer();

                    if (_monitorHost.IsHostingOnlineSession())
                    {
                        CreateServerAndJoin();
                        return;
                    }

                    //JoinRemoteServer(insert ip string); Need the IP to finish this garbage
                }
                else
                {
                    if (_ircClient != null && _ircClient.IsConnected)
                    {
                        ConnectToNewIrcChannel(e.PreviousServer);
                    }
                    else
                    {
                        ConnectToIrcServerAndChannel();
                    }
                }
            }
        }
        #endregion
       
        #region If Using IRC
        public void ConnectToIrcServerAndChannel()
        {
            Task.Run(() =>
            {
                _ircClient = new Communication.Irc.Protocols.ProtocolIrc();
                _ircClient.Server = IrcServer;
                _ircClient.IRCNetwork = new Network(_ircClient.Server, _ircClient);
                _ircClient.IRCNetwork.ServerName = IrcServer;
                _ircClient.Port = 6667;
                _ircClient.IRCNetwork.On_PRIVMSG += IRCNetwork_On_PRIVMSG;
                _ircClient.IRCNetwork.On_JOIN += IRCNetwork_On_JOIN;
                _ircClient.IRCNetwork.On_PART += IRCNetwork_On_PART;
                _ircClient.IRCNetwork.Nickname = _monitorPlayer._playerName;
                _ircClient.IRCNetwork.UserName = "Halo Player " + _monitorPlayer._playerName;
                _ircClient.Open();
                _ircClient.Join("#" + _monitorSession.Server);
                AllMessages.Add("Successfully started your chat server.");
            });
        }

        void IRCNetwork_On_PART(object sender, Network.NetworkChannelDataEventArgs e)
        {
            AllMessages.Add(String.Format("<<<{0} has left the server>>>", e.SourceInfo.Nick));
        }

        void IRCNetwork_On_JOIN(object sender, Network.NetworkChannelEventArgs e)
        {
            AllMessages.Add(String.Format("<<<{0} has joined the server>>>", e.SourceInfo.Nick));
        }

        private void IRCNetwork_On_PRIVMSG(object sender, Network.NetworkPRIVMSGEventArgs e)
        {
            AllMessages.Add(String.Format("[{0}] {1}", e.SourceInfo.Nick, e.Message));
        }

        public void ConnectToNewIrcChannel(string previousServerId)
        {
            _ircClient.Part("#" + previousServerId);
            _ircClient.Join("#" + _monitorSession.Server);
            AllMessages.Add("Connected to New Server");
        }

        public void DisconnectFromIrcChannel()
        {
            if (_ircClient != null && _ircClient.IsConnected) _ircClient.Disconnect();
        }

        public void SendIrcMessage(string message)
        {
            if (!_ircClient.IsConnected) return;
            _ircClient.Message(message, "#" + _monitorSession.Server);
            AllMessages.Add(String.Format("[{0}] {1}", _monitorPlayer._playerName, message));
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
                Console.WriteLine("Game process closed.");
                IsRunning = false;

                StopMonitors();

                if (ProcessClosed != null)
                    ProcessClosed(this, EventArgs.Empty);
            }
        }

    }
}
