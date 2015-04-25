using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Guacamole.Communication
{
    enum Command
    {
        Login,
        Logout,
        Message,
        List,
        Null
    }

    public class Server
    {
        struct ClientInfo
        {
            public EndPoint endpoint;  
            public string strName;
        }

        private ArrayList clientList;
        private Socket serverSocket;
        byte[] byteData = new byte[1024];
        public bool _isAlive { get; set; }

        /// <summary>
        /// Creates a new instance of a Server.
        /// </summary>
        public Server()
        {
            clientList = new ArrayList();
        }

        /// <summary>
        /// Starts the created server instance, and gets ready 
        /// for connecting players.
        /// </summary>
        public void StartServer()
        {
            try
            {
                serverSocket = new Socket(AddressFamily.InterNetwork,
                   SocketType.Dgram, ProtocolType.Udp);

                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 11701);
                serverSocket.Bind(ipEndPoint);
                IPEndPoint ipeSender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint epSender = (EndPoint)ipeSender;

                serverSocket.BeginReceiveFrom(byteData, 0, byteData.Length,
                    SocketFlags.None, ref epSender, new AsyncCallback(OnReceive), epSender);
                _isAlive = true;
            }
            catch (Exception ee)
            {
                Console.WriteLine(ee.Message);
                _isAlive = false;
            }

        }

        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                IPEndPoint ipeSender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint epSender = (EndPoint)ipeSender;
                serverSocket.EndReceiveFrom(ar, ref epSender);
                Data msgReceived = new Data(byteData);
                Data msgToSend = new Data();

                byte[] message;

                msgToSend.cmdCommand = msgReceived.cmdCommand;
                msgToSend.strName = msgReceived.strName;

                switch (msgReceived.cmdCommand)
                {
                    case Command.Login:

                        ClientInfo clientInfo = new ClientInfo();
                        clientInfo.endpoint = epSender;
                        clientInfo.strName = msgReceived.strName;
                            clientList.Add(clientInfo);
                            msgToSend.strMessage = "<<<" + msgReceived.strName + " has joined the game lobby>>>";
                           break;

                    case Command.Logout:
                        int nIndex = 0;
                        foreach (ClientInfo client in clientList)
                        {
                            if (client.endpoint.Equals(epSender))
                            {
                                clientList.RemoveAt(nIndex);
                                break;
                            }
                            ++nIndex;
                        }

                        msgToSend.strMessage = "<<<" + msgReceived.strName + " has left the game lobby>>>";
                        break;

                    case Command.Message:

                        msgToSend.strMessage = String.Format("{0}: {1}", msgReceived.strName, msgReceived.strMessage);
                        break;

                    case Command.List:

                        msgToSend.cmdCommand = Command.List;
                        msgToSend.strName = null;
                        msgToSend.strMessage = null;

                        foreach (ClientInfo client in clientList)
                        {
                            msgToSend.strMessage += client.strName + "*";
                        }

                        message = msgToSend.ToByte();

                        serverSocket.BeginSendTo(message, 0, message.Length, SocketFlags.None, epSender,
                                new AsyncCallback(OnSend), epSender);
                        break;
                }

                if (msgToSend.cmdCommand != Command.List)
                {
                    message = msgToSend.ToByte();

                    foreach (ClientInfo clientInfo in clientList)
                    {
                        if (clientInfo.endpoint != epSender ||
                            msgToSend.cmdCommand != Command.Login)
                        {
                            serverSocket.BeginSendTo(message, 0, message.Length, SocketFlags.None, clientInfo.endpoint,
                                new AsyncCallback(OnSend), clientInfo.endpoint);
                        }
                    }
                }

                ipeSender = new IPEndPoint(IPAddress.Any, 0);
                epSender = (EndPoint)ipeSender;
                serverSocket.BeginReceiveFrom(byteData, 0, byteData.Length, SocketFlags.None, ref epSender,
                    new AsyncCallback(OnReceive), epSender);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An Error Occured: {0}", ex.Message);
            }
        }

        private void OnSend(IAsyncResult ar)
        {
            try
            {
                serverSocket.EndSend(ar);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Send Error: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Cleanly stop the server.
        /// </summary>
        internal void StopServer()
        {
            serverSocket.Close();
            _isAlive = false;
        }
    }

    /// <summary>
    /// Data transfered to and from server by clients.
    /// </summary>
    class Data
    {
        public Data()
        {
            this.cmdCommand = Command.Null;
            this.strMessage = null;
            this.strName = null;
        }

        public Data(byte[] data)
        {
            this.cmdCommand = (Command)BitConverter.ToInt32(data, 0);

            int nameLen = BitConverter.ToInt32(data, 4);

            int msgLen = BitConverter.ToInt32(data, 8);

            if (nameLen > 0)
                this.strName = Encoding.UTF8.GetString(data, 12, nameLen);
            else
                this.strName = null;

            if (msgLen > 0)
                this.strMessage = Encoding.UTF8.GetString(data, 12 + nameLen, msgLen);
            else
                this.strMessage = null;
        }

        public byte[] ToByte()
        {
            List<byte> result = new List<byte>();

            result.AddRange(BitConverter.GetBytes((int)cmdCommand));

            if (strName != null)
                result.AddRange(BitConverter.GetBytes(strName.Length));
            else
                result.AddRange(BitConverter.GetBytes(0));

            if (strMessage != null)
                result.AddRange(BitConverter.GetBytes(strMessage.Length));
            else
                result.AddRange(BitConverter.GetBytes(0));

            if (strName != null)
                result.AddRange(Encoding.UTF8.GetBytes(strName));

            if (strMessage != null)
                result.AddRange(Encoding.UTF8.GetBytes(strMessage));

            return result.ToArray();
        }

        public string strName;
        public string strMessage;
        public Command cmdCommand;
    }
}
