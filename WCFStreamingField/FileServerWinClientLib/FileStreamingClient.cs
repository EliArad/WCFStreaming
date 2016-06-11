using GojiWCFStreamingBaseApi.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileServerWinClientLib
{
    public class FileStreamingClient
    {
        public class SocketPacket
        {
            public System.Net.Sockets.Socket thisSocket;
        }
        bool m_running = false;
        Thread m_connectionThread = null;
        Task m_task = null;
        protected EventWaitHandle m_event = new AutoResetEvent(false);
        protected IAsyncResult m_result;
        BufferQueue m_bufQueue = new BufferQueue(512 * 1024);
        protected byte[] writeBuffer = new byte[512 * 1024];
        protected byte[] readBuffer = new byte[512 * 1024];
        protected Object thisLock = new Object();
        public delegate void ClientCallbackMessage(string fieldGuid, 
                                                   string ipAddress, 
                                                   int portNumber, 
                                                   int code, 
                                                   string msg, 
                                                   DateTime startTime);
        protected string m_ipAdresss;
        protected int m_portNumber = 0;
        protected ClientCallbackMessage pClientCallback;
        protected Socket m_sock = null;
        protected AsyncCallback m_pfnCallBack;
        protected NetworkStream networkStream = null;
        protected Thread m_messageHandlerThread;
        protected BinaryWriter m_bWriter = null;
        protected string m_userName;
        protected string m_password;

        GojiWCFStreamingBaseApi.ExtensionMethods.ServerJsonApi m_wja = new GojiWCFStreamingBaseApi.ExtensionMethods.ServerJsonApi();
        public FileStreamingClient(string ipAddress, 
                                   int portNumber, 
                                   string baseGuid,
                                   string userName, 
                                   string password, 
                                   ClientCallbackMessage pcallback = null)
        {
            m_ipAdresss = ipAddress;
            m_portNumber = portNumber;
            pClientCallback = pcallback;
            m_userName = userName;
            m_password = password;

            m_running = true;

            try
            {
                Connect();
                string jData = m_wja.SendCredintials(m_userName, m_password);
                SendMessage(m_sock, jData);
                Console.WriteLine("TCP Client connected to server:{0}: {1}", m_ipAdresss , m_portNumber);
            }
            catch (Exception  err)
            {
                Console.WriteLine(err.Message);
            }          
        }
        void ConnectionThread()
        {
            while (m_running == true)
            {
                try
                {
                    Connect();
                    string jData = m_wja.SendCredintials(m_userName, m_password);
                    SendMessage(m_sock, jData);
                    break;
                }
                catch (Exception err)
                {
                   
                }
            }
        }
        string GetHasPassword(string inputString)
        {
            byte[] data = System.Text.Encoding.ASCII.GetBytes(inputString);
            data = new System.Security.Cryptography.HMACSHA512().ComputeHash(data);
            String hash = System.Text.Encoding.ASCII.GetString(data);
            return hash;
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
                m_bWriter.Write(allBuffer, 0, allBuffer.Length);
                m_bWriter.Flush();
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public void SendFile(string fileName,
                             string userName,
                             string fieldGuid,
                             DateTime startTimeToSend, 
                             ulong listNum, 
                             ulong fileNumber, 
                             ulong g360Index, 
                             string fileOwnerUserName, 
                             DateTime wtsDateTime)
        {
            try
            {
                Console.WriteLine("SendFile {0} {1} {2} ", fileName, userName, fieldGuid);
                if (m_sock == null)
                {
                    m_connectionThread = new Thread(ConnectionThread);
                    m_connectionThread.Start();
                    while (m_sock.Connected == false && m_running == true)
                    {
                        Thread.Sleep(10000);
                    }
                    if (m_sock.Connected == false)
                        return;
                }

                using (BinaryReader b = new BinaryReader(File.Open(fileName, FileMode.Open)))
                {
                    Console.WriteLine("File is opened");
                    int length = (int)b.BaseStream.Length;
                    ServerJsonApi.jFileTransfer j = new ServerJsonApi.jFileTransfer();
                    j.fileName = Path.GetFileName(fileName);
                    j.fileSize = length.ToString();
                    j.guidName = fieldGuid;
                    j.userName = userName;
                    j.DirectoryName = Path.GetDirectoryName(fileName);
                    string jData = m_wja.InitiateFileTransfer(j);
                    SendMessage(m_sock, jData);

                    Console.WriteLine("InitiateFileTransfer is sent");

                    byte[] fileBuffer = new byte[length];
                    if (b.Read(fileBuffer, 0, length) != length)
                    {
                        Console.WriteLine("err");
                    }
                    int count = length;

                    while (m_sock.Poll(5000000, SelectMode.SelectWrite) == false)
                    {
                        Console.WriteLine("This Socket is not writable!");
                    }
                    
                    while (count > 0)
                    {
                        int size = m_sock.Send(fileBuffer, 0, count, SocketFlags.None);
                        while (m_sock.Poll(5000000, SelectMode.SelectWrite) == false)
                        {
                            Console.WriteLine("This Socket is not writable 11!");
                        }
                        Console.WriteLine("size = {0}", size);
                        count -= size;
                    }
                    
                    //m_bWriter.Write(fileBuffer, 0, length);
                    //m_bWriter.Flush();
                    Console.WriteLine("Writer sent {0}", length);
                    b.Close();
                    Console.WriteLine("Writer is closed");
                }
            }
            catch (Exception err)
            {                
                Console.WriteLine(err.Message);
                Console.ReadLine();
                throw (new SystemException(err.Message));
            }
        }

        private void Connect()
        {
            if (Connect(m_ipAdresss, m_portNumber) == false)
            {
                throw (new SystemException("Connection failed"));
            }
        }
        protected void OnDataReceived(IAsyncResult asyn)
        {
            lock (thisLock)
            {
                try
                {
                    SocketPacket theSockId = (SocketPacket)asyn.AsyncState;
                    if (m_sock == null)
                        return;
                    if (theSockId.thisSocket.Connected == false)
                        return;
                    int iRx = theSockId.thisSocket.EndReceive(asyn);
                    m_bufQueue.append(writeBuffer, iRx);
                    m_event.Set();
                    WaitForData(m_sock);
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message);
                    Disconnect();
                    //SendAllCallbacks(m_fieldGuid, m_ipAdresss, m_portNumber, sCommand.ServerDisconnected, string.Empty);
                }
            }
        }
        public void Disconnect()
        {
            if (m_sock != null)
            {
                m_running = false;
                if (m_connectionThread != null && m_connectionThread.IsAlive == true)
                    m_connectionThread.Join();
                m_sock.Close();
                m_sock = null;
            }
        }

        protected void WaitForData(Socket soc)
        {
            try
            {
                if (m_pfnCallBack == null)
                {
                    m_pfnCallBack = new AsyncCallback(OnDataReceived);
                }
                SocketPacket theSocPkt = new SocketPacket();
                if (soc == null)
                {
                    theSocPkt.thisSocket = m_sock;
                    // Start listening to the data asynchronously
                    if (m_sock != null)
                        m_result = m_sock.BeginReceive(writeBuffer, 0, writeBuffer.Length, SocketFlags.None, m_pfnCallBack, theSocPkt);

                }
                else
                {
                    theSocPkt.thisSocket = soc;
                    // Start listening to the data asynchronously                    
                    m_result = soc.BeginReceive(writeBuffer, 0, writeBuffer.Length, SocketFlags.None, m_pfnCallBack, theSocPkt);
                }
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.Message);
            }
        }
        protected string MessageHandlerProcessing()
        {
            try
            {
                while (m_sock != null && m_sock.Connected)
                {
                    if (m_bufQueue.Count <= 0)
                        m_event.WaitOne();
                    if (m_sock == null)
                        return "NULL SOCKET";
                    if (m_sock.Connected == false)
                        return "THREAD_OK";

                    if (m_bufQueue.Count >= 6)
                    {
                        m_bufQueue.read(readBuffer, 6);
                        string str = Encoding.ASCII.GetString(readBuffer, 0, 6);
                        string[] s = str.Split(new Char[] { '|' });
                        if (s[0] != "@")
                        {
                            return "Error missing prehumble";
                        }
                        while (m_bufQueue.Count < int.Parse(s[1]))
                        {
                            Thread.Sleep(1);
                        }
                        m_bufQueue.read(readBuffer, int.Parse(s[1]));
                        str = Encoding.ASCII.GetString(readBuffer, 0, int.Parse(s[1]));
                        ProcessMessage(str, int.Parse(s[1]));
                    }
                    else
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                }
                return "THREAD_OK";
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }
        protected void ProcessMessage(string data, int size)
        {
            string[] jdata = data.Split(new Char[] { '|' });

            switch (jdata[0])
            {
                case "e":
                break;
            }
        }
        private bool Connect(string ipAddress, int port)
        {
            if (m_sock != null && m_sock.Connected == true)
                return true;

            lock (this)
            {
                Socket l_clientSocket;
                IAsyncResult ar;
                try
                {
                    // Create the socket instance
                    l_clientSocket = new Socket(AddressFamily.InterNetwork,
                                                SocketType.Stream,
                                                ProtocolType.Tcp);

                    l_clientSocket.NoDelay = true;
                    // Set the receive buffer size to 8k
                    l_clientSocket.SendBufferSize = 1024* 1024 * 10;
                    l_clientSocket.ReceiveBufferSize = 111000;

                    //l_clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, true);
                    //l_clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);


                    // to 1 3000 milliseconds.
                    l_clientSocket.SendTimeout = 3000;


                    // Cet the remote IP address
                    IPAddress ip = IPAddress.Parse(ipAddress);
                    // Create the end point 
                    IPEndPoint ipEnd = new IPEndPoint(ip, port);
                    // Connect to the remote host
                    ar = l_clientSocket.BeginConnect(ipEnd, null, null);
                    if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(4), false))
                    {
                        l_clientSocket.Close();
                        return false;
                    }
                    else
                    {
                        if (l_clientSocket.Connected)
                        {
                            m_sock = l_clientSocket;
                            //Wait for data asynchronously 
                            if (m_messageHandlerThread == null || m_messageHandlerThread.IsAlive == false)
                            {
                                ThreadedExecuter<string> executer = new ThreadedExecuter<string>(MessageHandlerProcessing, ThreadCompleted);
                                m_messageHandlerThread = executer.handle;
                                executer.Start();
                            }
                            m_ipAdresss = ipAddress;
                            WaitForData(m_sock);
                            networkStream = new NetworkStream(m_sock);
                            m_bWriter = new System.IO.BinaryWriter(networkStream);
                            m_portNumber = port;
                            return true;

                        }
                    }
                }
                catch (Exception e)
                {
                    throw (new SystemException(e.Message));
                }
                return false;
            }
        }
        protected void ThreadCompleted(string msg)
        {

        }
    }
}
