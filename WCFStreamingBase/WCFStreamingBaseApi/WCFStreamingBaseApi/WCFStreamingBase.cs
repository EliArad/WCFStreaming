using GojiWCFStreamingBaseApi.ServiceReference1;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GojiWCFStreamingBaseApi
{
    public class GojiWCFStreamingBase : IServiceCallback
    {
        const string FMT = "yyyy-MM-dd HH:mm:ss.fffffff";
        string m_clientIpAdresss;
        IService m_client = null;
        DuplexChannelFactory<IService> pipeFactory;
        static ServiceHost host = null;
        static FileRepositoryService service = null;
        bool m_IsConnected = false;
        public delegate void ServerCallbackMessage(string fieldGuid, string ipAddress, int code, string msg, DateTime startTime, ulong g360Index, string fileOwnerUserName, long sizeOfFile, string TargetPath, string VirtualPath);
        public delegate void ClientCallbackMessage(string fieldGuid,string ipAddress, int code, string msg, DateTime startTime, string userName);
        string m_fieldGuid;
        static ServerCallbackMessage pServerCallback;
        Dictionary<string, ClientCallbackMessage> pClientCallback = new Dictionary<string, ClientCallbackMessage>();
        AutoResetEvent m_pingEvent = new AutoResetEvent(false);
        FileStreamingServer m_tcpStreamingServer;
        bool m_running = false;
        Thread m_pingThread = null;
        public GojiWCFStreamingBase(ClientCallbackMessage p )
        {
            m_running = true;
            pClientCallback.Add("Main", p);

           
        }
        public void AddClientCallback(string controller, ClientCallbackMessage p)
        {
            if (pClientCallback.ContainsKey(controller) == false)
            {
                pClientCallback.Add(controller, p);
            }
            else
            {
                pClientCallback[controller] = p;
            }
        }
        public static ServerCallbackMessage SetServerCallback
        { 
            set
            {
                pServerCallback = value;
            }
        }
        public void StartTCPStreamingServer(int receiveBufferSize , int portNumber)
        {
            if (m_tcpStreamingServer == null)
                m_tcpStreamingServer = new FileStreamingServer(receiveBufferSize);

            m_tcpStreamingServer.StartServer(portNumber);
        }
        public void CloseTCPStreamingServer()
        {
             if (m_tcpStreamingServer != null)
             {
                 m_tcpStreamingServer.Close();
             }
        }
        public void ConnectStreamingFieldClient(string ipAddress, int portNumber, string userName , string password)
        {
            try
            {
                NetTcpBinding tcpBinding = new NetTcpBinding();
                tcpBinding.OpenTimeout = TimeSpan.FromSeconds(10);
                tcpBinding.ReceiveTimeout = TimeSpan.FromSeconds(120);
                tcpBinding.SendTimeout = TimeSpan.FromSeconds(120);
                tcpBinding.CloseTimeout = TimeSpan.FromSeconds(10);
                tcpBinding.ReliableSession.InactivityTimeout = new TimeSpan(1, 0, 0, 0);
                tcpBinding.TransactionFlow = false;
                tcpBinding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;
                tcpBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
                tcpBinding.Security.Mode = SecurityMode.Transport;
                m_clientIpAdresss = ipAddress;


                pipeFactory =
                 new DuplexChannelFactory<IService>(
                     new InstanceContext(this),
                    tcpBinding,
                     new EndpointAddress("net.tcp://" + ipAddress + ":" + portNumber + "/MyService"));

 
                if (userName != string.Empty)
                {
                    pipeFactory.Credentials.Windows.ClientCredential.UserName = userName;
                    pipeFactory.Credentials.Windows.ClientCredential.Password = password;
                    pipeFactory.Credentials.ServiceCertificate.Authentication.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.PeerOrChainTrust;
                }


                m_client = pipeFactory.CreateChannel();


                string res = m_client.HelloWorld("Eli");
                if (res != "Hello world from Eli")
                {
                    throw (new SystemException("Extepected hello from Eli from field server"));
                }

                m_fieldGuid = m_client.getGuild();

                if (m_pingThread != null)
                {
                    m_pingEvent.Set();
                    m_running = false;
                    m_pingThread.Join();
                    m_pingThread = null;
                }

                if (m_pingThread == null || m_pingThread.IsAlive == false)
                {
                    m_pingThread = new Thread(PingThread);
                    m_pingThread.Start();
                }

                ((ICommunicationObject)m_client).Closed += new EventHandler(delegate
                {
                    ServiceClose(ipAddress);
                });

                ((ICommunicationObject)m_client).Faulted += new EventHandler(delegate
                {

                    ServiceFault(ipAddress);
                });

                m_IsConnected = true;
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public bool IsConnected
        {
            get
            {
                return m_IsConnected;
            }
        }

        public string OpenTCPStreamingClient(string ipAddress, string userName, string password)
        {
            try
            {
                return m_client.OpenTCPStreamingClient( ipAddress,userName,password);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

        public string GetCurrentUploadedFileInfo(out string currentUploadedFile,
                                                 out ulong currentUploadedG360Index,
                                                 out string currentUploadedFileOwner,
                                                 out DateTime currentUploadedDate,
                                                 out string m_currentUploadedVirtualPath,
                                                 out string currentUploadedTargetPath)
        {
            try
            {
                string s =  m_client.GetCurrentUploadedFileInfo();

                string[] tdata = s.Split(new Char[] { ',' });
                currentUploadedFile = tdata[0];
                currentUploadedDate = DateTime.ParseExact(tdata[1], FMT, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                currentUploadedG360Index = ulong.Parse(tdata[2]);
                currentUploadedFileOwner = tdata[3];
                m_currentUploadedVirtualPath = tdata[4];
                currentUploadedTargetPath = tdata[5];
                return s;
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

        public DriveInfo[] GetDrivesInfo()
        {
            try
            {
                return m_client.GetDrivesInfo();
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

        public string MarkLastFileAsUploaded(string TargetPath,
                                             string VirtualPath, 
                                             ulong currentUploadedG360Index, 
                                             string currentUploadedFileOwner,
                                             long currentSizeOfFile)
        {
            try
            {
                return m_client.MarkLastFileAsUploaded(TargetPath, VirtualPath, currentUploadedG360Index, currentUploadedFileOwner, currentSizeOfFile);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

        public void DisconnectRepositoryStreamingClient()
        {
            try
            {
                m_client.DisconnectRepositoryStreamingClient();
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

        public bool GetVerbose()
        {
            try
            {
                return m_client.GetVerbose();
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

        public void SetVerbose(bool set)
        {
            try
            {
                m_client.SetVerbose(set);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        private void ServiceClose(string ipAddress)
        {
            m_IsConnected = false;

            foreach (KeyValuePair<string, ClientCallbackMessage> entry in pClientCallback)
            {
                entry.Value(m_fieldGuid, m_clientIpAdresss, 88, ipAddress, DateTime.Now, "admin");
            }
            
        }
        private void ServiceFault(string ipAddress)
        {
            m_IsConnected = false;
            Console.WriteLine("Service faulted!");
            ((ICommunicationObject)m_client).Faulted -= new EventHandler(delegate
            {

            });
            Thread t = new Thread(() =>
            {
                foreach (KeyValuePair<string, ClientCallbackMessage> entry in pClientCallback)
                {
                    entry.Value(m_fieldGuid, m_clientIpAdresss, 77, ipAddress, DateTime.Now,"admin");
                }
            });
            t.Start();
        }
        public void NotifyDataCallback(string fieldGuid, string ipAddress, byte[] buf, int size, string userName)
        {
            
        }
        public void NotifyCallbackMessage(string fieldGuid,string ipAddress, int code, string msg, DateTime startTime, string userName)
        {
            foreach (KeyValuePair<string, ClientCallbackMessage> entry in pClientCallback)
            {
                entry.Value(fieldGuid, ipAddress, code, msg, startTime, userName);
            }
        }
        public string Ping()
        {
            try
            {
                return m_client.Ping();
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        private void PingThread()
        {
            while (m_running)
            {
                try
                {
                    m_pingEvent.WaitOne(10000);
                    Ping();
                }
                catch (Exception err)
                {
                    m_pingEvent.WaitOne(10000);
                }
            }
        }
        public void StopWatch()
        {
            try
            {
                m_client.StopWatch();
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public void Echo()
        {
            try
            {
                m_client.Echo();
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public string GetStatistics(string userName , bool byUser)
        {
            try
            {
                return m_client.GetStatistics(userName , byUser);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

        public bool IsCopyThreadIsAlive()
        {

            try
            {
                return m_client.IsCopyThreadIsAlive();
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

        public void ConnectToStreamingServer(string ipAddress, string userName = "", string password = "")
        {
            try
            {
                m_client.ConnectToStreamingServer(ipAddress, userName , password);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

        public string StopWatchWrongFiles()
        {
            try
            {
                return m_client.StopWatchWrongFiles();
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public int SyncFilesWithDataBase(string directoryToSync, bool recoursive, string userName, int SyncBy)
        {
            try
            {
                return m_client.SyncFilesWithDataBase(directoryToSync, recoursive, userName , SyncBy);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public string StartWatchWrongFiles(string PathToWatch, 
                                           bool IncludeSubdirectories)                                           
        {
            try
            {
                return m_client.StartWatchWrongFiles(PathToWatch, IncludeSubdirectories);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

        public string StartWatch(string PathToWatch, string Filter, bool IncludeSubdirectories, string fileOwnerUserName)
        {
            try
            {
                if (fileOwnerUserName == string.Empty)
                {
                    return "failed, user name not supplied";
                }
                return m_client.StartWatch(PathToWatch, Filter, IncludeSubdirectories, fileOwnerUserName);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public void Register(string ServerIpAddress)
        {
            try
            {
                m_client.Register(ServerIpAddress);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public void CloseCopyThread()
        {
            try
            {
                m_client.CloseCopyThread();
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public void CloseClient()
        {
            if (pipeFactory != null)
            {
                pipeFactory.Close();
                m_IsConnected = false;
                m_running = false;
                m_pingEvent.Set();
                m_pingThread.Join();
            }
        }

        public void SetFifoThreshold(int depth)
        {
            if (depth < 2)
            {
                throw (new SystemException("Minimum depth is 2"));
            }
            try
            {
                m_client.SetFifoThreshold(depth);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

        public int GetFifoThreshold()
        {
             
            try
            {
                return m_client.GetFifoThreshold();
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

        public int DeleteAllUploadedFilesOnServer(string password, string userName, bool byUser)
        {
            try
            {
                return m_client.DeleteAllUploadedFilesOnServer(password, userName , byUser);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public int DeleteAllWatsonGenerateFilesOnField(string password)
        {
            return 0;    
        }

        public string DeleteAllFilesFromFieldDaysBefore(int days, string password, string userName, bool byUser)
        {
            try
            {
                return m_client.DeleteAllFilesFromFieldDaysBefore(days, password, userName , byUser);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

        public string InitiateSingleUpload()
        {
            try
            {
                return m_client.InitiateSingleUpload();
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

        public string SetWatsonRunning(bool set)
        {
            try
            {
                return m_client.SetWatsonRunning(set);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

        public string DeleteAllWatsonGenerateFilesTimeSpanBeforeNow(DateTime time, string userName, bool byUser)
        {

            try
            {
                return m_client.DeleteAllWatsonGenerateFilesTimeSpanBeforeNow(time,userName, byUser);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
         
        public string GetClientGuid()
        {
            try
            {
                return m_client.GetClientGuid();
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

        public string AddDateToUploadTargetFolder(bool add)
        {
            try
            {
                return m_client.AddDateToUploadTargetFolder(add);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

        public string DeleteAllUploadedFilesTimeSpanBeforeNow(DateTime time, string userName, bool byUser)
        {
            try
            {
                return m_client.DeleteAllUploadedFilesTimeSpanBeforeNow(time, userName, byUser);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

        public string DeleteAllFilesFromFieldBetweenDates(DateTime start, DateTime end, bool includeTime, string password, string userName , bool byUser)
        {
            try
            {
                return m_client.DeleteAllFilesFromFieldBetweenDates(start, end, includeTime, password, userName , byUser);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public int DeleteAllFilesFromFieldStartingFromDate(DateTime startingFrom, string password, string userName, bool byUser)
        {
            try
            {
                return int.Parse(m_client.DeleteAllFilesFromFieldStartingFromDate(startingFrom, password, userName , byUser));
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

        static void Host_Faulted(object sender, EventArgs e)
        {
            Console.WriteLine("Host faulted; reinitialising host");
            host.Abort();
        }

        static void Service_FileRequested(object sender, FileEventArgs e)
        {
            Console.WriteLine(string.Format("File access\t{0}\t{1}", e.VirtualPath, e.startTime));
        }

        static void Service_FileUploaded(object sender, FileEventArgs e)
        {
            string msg = e.CompleteTargetPath + "," + e.startTime;
            pServerCallback(e.fieldGuid , 
                            e.ClientIpAddress, 
                            400, 
                            msg, 
                            e.startTime , 
                            e.g360Index , 
                            e.fileOwnerUserName, 
                            e.sizeOfFile,
                            e.TargetPath,
                            e.VirtualPath);
        }

        static void Service_FileDeleted(object sender, FileEventArgs e)
        {
            Console.WriteLine(string.Format("File deleted\t{0}\t{1}", e.VirtualPath, e.startTime));
        }

        public void CloseDropBoxConnection()
        {
            try
            {
                m_client.CloseDropBoxConnection();
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public static void CloseStorageServer()
        {
            if (host != null)
            {
                host.Close();
                host = null;
            }
        }
        public void SetCopyThreadTimeEvent(TimeSpan t)
        {
            try
            {
                m_client.SetCopyThreadTimeEvent(t);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public TimeSpan GetCopyThreadTimeEvent()
        {
            try
            {
                return m_client.GetCopyThreadTimeEvent();
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public void CleanFifoHandler()
        {
            try
            {
                m_client.CleanFifoHandler();
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public static string RepositoryDirectory
        {
            get
            {
                return service.RepositoryDirectory;
            }
        }
        public static bool IsStorageServerOpen()
        {
            return host != null ? true : false;
        }
        public void StartDropBoxUploadMode(bool start)
        {
            try
            {
                m_client.StartDropBoxUploadMode(start);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public void RunBoxInitialize()
        {
            try
            {
                m_client.RunBoxInitialize();
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public string InitializeDropBox()
        {
            try
            {
                return m_client.DropBoxInitialize();
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        /*
        public static void StartStorageServer(string storageDirectory)
        {
            if (host != null)
                host.Close();
            host = null;
            service = new FileRepositoryService();
            service.RepositoryDirectory = storageDirectory;

            service.FileRequested += new FileEventHandler(Service_FileRequested);
            service.FileUploaded += new FileEventHandler(Service_FileUploaded);
            service.FileDeleted += new FileEventHandler(Service_FileDeleted);

            host = new ServiceHost(service);
            host.Faulted += new EventHandler(Host_Faulted);

            try
            {
                host.Open();
            }
            catch (Exception err)
            {
                host = null;
                throw (new SystemException(err.Message));
            }
        }
        */
        public static void UpdateStorageServerDirectories(Dictionary<string, string> storageDirectoryDic)
        {
            if (service != null)
                service.RepositoryDirectoryDic = storageDirectoryDic;
        }
        public static void UpdateStorageServerDirectory(string fieldGuid, string storageDirectory)
        {
            if (service != null)
            {
                if (service.RepositoryDirectoryDic.ContainsKey(fieldGuid) == true)
                    service.RepositoryDirectoryDic[fieldGuid] = storageDirectory;
                else
                {
                    throw (new SystemException("guid not exist in repostory dic"));
                }
            }
        }
        public static void StartStorageServer(Dictionary<string, string> storageDirectoryDic)
        {
             
            Task task = new Task(() => {

                try
                {
                    if (host != null)
                        host.Close();
                    host = null;
                    service = new FileRepositoryService();
                    service.RepositoryDirectoryDic = storageDirectoryDic;

                    service.FileRequested += new FileEventHandler(Service_FileRequested);
                    service.FileUploaded += new FileEventHandler(Service_FileUploaded);
                    service.FileDeleted += new FileEventHandler(Service_FileDeleted);

                    host = new ServiceHost(service);
                    host.Faulted += new EventHandler(Host_Faulted);
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message);
                }

                try
                {
                    host.Open();
                }
                catch (Exception err)
                {
                    host = null;
                    pServerCallback(string.Empty, string.Empty, 490, err.Message, DateTime.Now, 0, string.Empty, 0, string.Empty, string.Empty);
                }                        
            });
            task.Start();
        }
    }
}
