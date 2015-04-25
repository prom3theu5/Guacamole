using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Guacamole.Communication
{
    public class Client
    {
        private Socket clientSocket;
        private EndPoint epServer;
        private string strName;
        private string _hostIp;
        public bool _isConnected {get;set;}
        byte[] byteData = new byte[1024];
        private ArrayList _playersList;
        public ObservableCollection<string> _messagesCollection;

        /// <summary>
        /// Creates an instance of a Client or 
        /// Player ready to join an In Game Lobby
        /// </summary>
        /// <param name="playerName">a string, that contains your current
        /// player name in ElDorito and in game.</param>
        /// <param name="hostIp">a string, gets parsed as an Ip Address.
        /// Contains the IP of the current host so you can connect to 
        /// their chat server</param>
        public Client(string playerName, string hostIp)
        {
            strName = playerName;
            _hostIp = hostIp;
            _playersList = new ArrayList();
            _messagesCollection = new ObservableCollection<string>();
        }

        /// <summary>
        /// Connect to the server, and send a Login request
        /// to register your client (in order to recieve messages)
        /// </summary>
        public void Login()
        {
            try
            {
                clientSocket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Dgram, ProtocolType.Udp);

                IPAddress ipAddress = IPAddress.Parse(_hostIp);
                IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 11701);

                epServer = (EndPoint)ipEndPoint;

                Data msgToSend = new Data();
                msgToSend.cmdCommand = Command.Login;
                msgToSend.strMessage = null;
                msgToSend.strName = strName;

                byte[] byteData = msgToSend.ToByte();

                clientSocket.BeginSendTo(byteData, 0, byteData.Length,
                    SocketFlags.None, epServer, new AsyncCallback(OnSend), null);

                msgToSend = new Data();
                msgToSend.cmdCommand = Command.List;
                msgToSend.strName = strName;
                msgToSend.strMessage = null;

                byteData = msgToSend.ToByte();

                clientSocket.BeginSendTo(byteData, 0, byteData.Length, SocketFlags.None, epServer,
                    new AsyncCallback(OnSend), null);

                byteData = new byte[1024];
                clientSocket.BeginReceiveFrom(byteData,
                                           0, byteData.Length,
                                           SocketFlags.None,
                                           ref epServer,
                                           new AsyncCallback(OnReceive),
                                           null);
                _isConnected = true;
            }
            catch (Exception ex)
            {
                _messagesCollection.Add(String.Format("Client Error: {0}", ex.Message));
                Console.WriteLine("Client Error: {0}", ex.Message);
                _isConnected = false;
            } 
        }

        /// <summary>
        /// Cleanly disconnect from the server, which removes your client
        /// from the client pool.
        /// </summary>
        public void LogOut()
        {
            try
            {
                Data msgToSend = new Data();
                msgToSend.cmdCommand = Command.Logout;
                msgToSend.strName = strName;
                msgToSend.strMessage = null;

                byte[] b = msgToSend.ToByte();
                clientSocket.SendTo(b, 0, b.Length, SocketFlags.None, epServer);
                clientSocket.Close();
                _isConnected = false;
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                _messagesCollection.Add(String.Format("Client Error: {0}", ex.Message));
                Console.WriteLine("Client Error: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Sends a Message to the server, which is sent to all clients.
        /// </summary>
        /// <param name="message">a string, The message to send.</param>
        public void SendMessage(string message)
        {
            try
            {
                Data msgToSend = new Data();

                msgToSend.strName = strName;
                msgToSend.strMessage = message;
                msgToSend.cmdCommand = Command.Message;

                byte[] byteData = msgToSend.ToByte();

                clientSocket.BeginSendTo(byteData, 0, byteData.Length, SocketFlags.None, epServer, new AsyncCallback(OnSend), null);
            }
            catch (Exception ex)
            {
                _messagesCollection.Add(String.Format("Client Error: {0}", ex.Message));
                Console.WriteLine(ex.Message);
            }  
        }

        private void OnSend(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndSend(ar);
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                _messagesCollection.Add(String.Format("Client Error: {0}", ex.Message));
                Console.WriteLine(ex.Message);
            }
        }

        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndReceive(ar);

                Data msgReceived = new Data(byteData);

                switch (msgReceived.cmdCommand)
                {
                    case Command.Login:
                        _playersList.Add(msgReceived.strName);
                        break;

                    case Command.Logout:
                        _playersList.Remove(msgReceived.strName);
                        break;

                    case Command.Message:
                        break;

                    case Command.List:
                        _playersList.AddRange(msgReceived.strMessage.Split('*'));
                        _playersList.RemoveAt(_playersList.Count - 1);
                        _messagesCollection.Add(String.Format("<<<{0} has joined the game lobby>>>", strName));
                        Console.WriteLine("<<<{0} has joined the game lobby>>>", strName);
                        break;
                }

                if (msgReceived.strMessage != null && msgReceived.cmdCommand != Command.List)
                {
                    _messagesCollection.Add(String.Format(msgReceived.strMessage));
                    Console.WriteLine("{0}\r\n", msgReceived.strMessage);
                }

                byteData = new byte[1024];
                clientSocket.BeginReceiveFrom(byteData, 0, byteData.Length, SocketFlags.None, ref epServer,
                                           new AsyncCallback(OnReceive), null);
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                Console.WriteLine("Client Error: {0}", ex.Message);
            }
        }

    }
}
