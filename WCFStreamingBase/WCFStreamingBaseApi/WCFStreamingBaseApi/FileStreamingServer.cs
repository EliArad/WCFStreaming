using BufferQueueApi;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GojiWCFStreamingBaseApi
{
    using ExtensionMethods;
    using System.IO;
    class FileStreamingServer
    {
        public struct ClientInfo
        {
            public Socket socket;   //Socket of the client
            public string strName;  //Name by which the user logged into the chat room
            public string randomClientString;
        }
        ServerJsonApi.jFileTransfer m_fileTransferInfo;
        ServerJsonApi m_wja = new ServerJsonApi();
        int m_portNumber;
        string m_serverAddress;
        Socket serverSocket;
        BufferQueue m_bufQueue;
        ArrayList clientList;
        bool m_serverConnected = false;
        bool m_abort = false;
        string m_userName;
        string m_hasPassword;
        string m_loginName;
        string m_password;
        byte[] byteData;
        byte[] readBuffer;
        int m_receiveBufferSize;
        bool m_initiateTransfer = false;
        BinaryWriter m_binWriter = null;

        int m_fileSize;

        string m_storageDirectory = "m:\\Elia\\";

        public FileStreamingServer(int receiveBufferSize)
        {
            m_bufQueue = new BufferQueue(receiveBufferSize);
            clientList = new ArrayList();
            byteData = new byte[receiveBufferSize];
            readBuffer = new byte[receiveBufferSize];
            m_receiveBufferSize = receiveBufferSize;
        }
        public void Disconnect()
        {
            Thread.Sleep(100);
            m_abort = true;
            if (serverSocket != null)
                serverSocket.Close();
            m_serverConnected = false;
        }
        public void Close()
        {
            Disconnect();
        }
        public bool ServerConnected
        {
            get
            {
                return m_serverConnected;
            }
        } 
        public void StartServer(int portNumber, bool reuse = true)
        {
            try
            {

                NetCard.NetCard n = new NetCard.NetCard();
                string serverIp = n.getComputerIP();
                
                serverSocket = new Socket(AddressFamily.InterNetwork,
                                          SocketType.Stream,
                                          ProtocolType.Tcp);

                IPAddress ipaddress = null;
                ipaddress = IPAddress.Parse(serverIp);


                //Assign the any IP of the machine and listen on port number 1000
                IPEndPoint ipEndPoint = new IPEndPoint(ipaddress, portNumber);
                m_serverAddress = ipaddress.ToString();
                if (reuse == true)
                {
                    serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                }
                serverSocket.ReceiveBufferSize = m_receiveBufferSize;
                //Bind and listen on the given address
                serverSocket.Bind(ipEndPoint);
                serverSocket.Listen(4);
                m_portNumber = portNumber;

                //Accept the incoming clients
                serverSocket.BeginAccept(new AsyncCallback(OnAccept), null);
                m_serverConnected = true;
            }
            catch (Exception ex)
            {
                m_serverConnected = false;
                throw (new SystemException("SGSserverTCP: " + ex.Message));
            }
        }
        private readonly Random _rng = new Random();
        private const string _chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private string RandomString(int size)
        {
            char[] buffer = new char[size];

            for (int i = 0; i < size; i++)
            {
                buffer[i] = _chars[_rng.Next(_chars.Length)];
            }
            return new string(buffer);
        }
        void ReceiveProcess(Socket clientSocket)
        {

            try
            {
                if (m_bufQueue.Count <= 0)
                    return;
                if (m_serverConnected == false)
                    return;


                if (m_initiateTransfer == true)
                {
                    File.AppendAllText("c:\\tcp.txt", "1 : " + m_bufQueue.Count.ToString() + Environment.NewLine);
                    if (m_bufQueue.Count < m_fileSize)
                        return;
                    File.AppendAllText("c:\\tcp.txt", "2 : " + m_bufQueue.Count.ToString() + Environment.NewLine);
                    m_bufQueue.read(readBuffer, m_fileSize);
                    File.AppendAllText("c:\\tcp.txt", "3 : " + m_bufQueue.Count.ToString() + Environment.NewLine);
                    m_binWriter.Write(readBuffer, 0, m_fileSize);
                    File.AppendAllText("c:\\tcp.txt", "4 : " + m_bufQueue.Count.ToString() + Environment.NewLine);
                    m_fileSize = 0;
                    m_initiateTransfer = false;
                    m_binWriter.Close();
                    m_binWriter = null;
                    File.AppendAllText("c:\\tcp.txt", "5 : " + m_bufQueue.Count.ToString() + Environment.NewLine);
                    if (m_bufQueue.Count < 6)
                        return;
                    File.AppendAllText("c:\\tcp.txt", "6 : " + m_bufQueue.Count.ToString() + Environment.NewLine);                    
                }
                 

                m_bufQueue.read(readBuffer, 6);
                string str = Encoding.ASCII.GetString(readBuffer, 0, 6);
                string[] s = str.Split(new Char[] { '|' });
                if (s[0] != "@")
                {
                    return;
                }
                while (m_bufQueue.Count < int.Parse(s[1]))
                {
                    Thread.Sleep(1);
                }
                m_bufQueue.read(readBuffer, int.Parse(s[1]));
                str = Encoding.ASCII.GetString(readBuffer, 0, int.Parse(s[1]));

                string[] jdata = str.Split(new Char[] { '|' });


                if (jdata[0] != "sendcredintials" && m_userName != m_loginName && m_hasPassword != m_password)
                {
                    string jData = m_wja.SendErrorMessage("TCP Server has rejected client credentials");
                    SendMessage(clientSocket, jData);
                    return;
                }
                 
                switch (jdata[0])
                { 
                    case "sendcredintials":
                    {

                    }
                    break;
                    case "initiatefiletransfer":
                    {
                        m_fileTransferInfo = m_wja.DeserializeObject<ServerJsonApi.jFileTransfer>(jdata[1]);
                        m_initiateTransfer = true;
                        m_fileSize = int.Parse(m_fileTransferInfo.fileSize);
                        string fieldDir = m_fileTransferInfo.DirectoryName.Substring("c:\\".Length);
                        string pathOfFile = m_storageDirectory + "\\" + m_fileTransferInfo.guidName + "\\" + m_fileTransferInfo.userName + "\\" + fieldDir;
                        if (Directory.Exists(pathOfFile) == false)
                            Directory.CreateDirectory(pathOfFile);
                        pathOfFile += "\\" + m_fileTransferInfo.fileName;
                        m_binWriter = new BinaryWriter(File.Open(pathOfFile, FileMode.Create));
                    }
                    break;                                         
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }

        }
        private void OnReceive(IAsyncResult ar)
        {
            Socket clientSocket = null;
            try
            {
                int iRx;
                clientSocket = (Socket)ar.AsyncState;
                iRx = clientSocket.EndReceive(ar);
                File.AppendAllText("c:\\tcp.txt", "OnReceive: " + iRx + Environment.NewLine);
                m_bufQueue.append(byteData, iRx);
                ReceiveProcess(clientSocket);                
                clientSocket.BeginReceive(byteData,
                                          0,
                                          byteData.Length,
                                          SocketFlags.None,
                                          new AsyncCallback(OnReceive), clientSocket);
            }
            catch (Exception err)
            {
                //ServiceLogger.WriteLog(LOGGER_NAMES.MPFMSERV, err.Message);
                if (!clientSocket.Connected || clientSocket.Available == 0)
                {                     
                    int nIndex = 0;
                    foreach (ClientInfo client in clientList)
                    {
                        if (client.socket == clientSocket)
                        {
                            clientList.RemoveAt(nIndex);
                            break;
                        }
                        ++nIndex;
                    }

                    if (clientSocket != null)
                        clientSocket.Close();
                    return;
                }
                return;
            }
        }            
    
        private void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket clientSocket = serverSocket.EndAccept(ar);

                //Start listening for more clients
                serverSocket.BeginAccept(new AsyncCallback(OnAccept), null);

                //Once the client connects then start receiving the commands from her
                clientSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None,
                    new AsyncCallback(OnReceive), clientSocket);

                ClientInfo clientInfo = new ClientInfo();
                clientInfo.socket = clientSocket;
                clientList.Add(clientInfo);

                clientInfo.randomClientString = RandomString(20);
                string jData = m_wja.Connected(clientInfo.randomClientString);
                SendMessage(clientSocket, jData);
            }
            catch (Exception ex)
            {
                if (m_abort == true)
                    return;
            }
        }
        byte[] GetBytes(double[] values)
        {
            return values.SelectMany(value => BitConverter.GetBytes(value)).ToArray();
        }
        byte[] getBytes<T>(T str)
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }
        public void BroadcastMessage(int code, string message)
        {
            string jData = m_wja.BroadcastMessage(code, message);
            SendMessage(jData);
        }

        void SendMessage(Socket socket, string msg)
        {
            try
            {
                string header = "@|";
                byte[] data = System.Text.Encoding.ASCII.GetBytes(msg);
                header += data.Length.ToString("0000");
                byte[] data1 = System.Text.Encoding.ASCII.GetBytes(header);
                byte[] allBuffer = new byte[data1.Length + data.Length];
                Array.Copy(data1, 0, allBuffer, 0, data1.Length);
                Array.Copy(data, 0, allBuffer, data1.Length, data.Length);
                sendMessage(socket, allBuffer);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        void SendMessage(string msg)
        {
            try
            {
                string header = "@|";
                byte[] data = System.Text.Encoding.ASCII.GetBytes(msg);
                header += data.Length.ToString("0000");
                byte[] data1 = System.Text.Encoding.ASCII.GetBytes(header);
                byte[] allBuffer = new byte[data1.Length + data.Length];
                Array.Copy(data1, 0, allBuffer, 0, data1.Length);
                Array.Copy(data, 0, allBuffer, data1.Length, data.Length);
                sendMessage(allBuffer);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

        public void OnSend(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                client.EndSend(ar);
            }
            catch (Exception ex)
            {
                throw (new SystemException("SGSserverTCP: " + ex.Message));
            }
        }
        private void sendMessage(byte[] message)
        {
            Console.WriteLine("TCP Json broadcast to: " + clientList.Count);
            foreach (ClientInfo clientInfo in clientList)
            {
                //Send the message to all users
                clientInfo.socket.BeginSend(message, 0, message.Length, SocketFlags.None,
                    new AsyncCallback(OnSend), clientInfo.socket);
            }
        }
        private void sendMessage(Socket socket, byte[] message)
        {
            try
            {
                socket.BeginSend(message, 0, message.Length, SocketFlags.None,
                    new AsyncCallback(OnSend), socket);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
    }
}
