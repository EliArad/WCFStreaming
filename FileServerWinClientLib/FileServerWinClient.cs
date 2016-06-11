using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FileServer.Services;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading;
using DropBoxSharpBoxApi;
using System.Security.Principal;
using System.ServiceModel.Security;

namespace FileServerWinClientLib
{ 
    public class FileServerWinClient
    {
        static bool m_dropboxInitialized = false;
        DropBoxSharpBox m_dropbox = new DropBoxSharpBox();
        static bool m_connectedToFileServer = false;
        private static System.IO.FileSystemWatcher m_Watcher = null;
        static FileRepositoryServiceClient m_client = null; 
        static bool m_bIsWatching = false;
        static EventWaitHandle m_event = new AutoResetEvent(false);
        static Thread m_thread;
        static Queue<string> m_queue = new Queue<string>();
        static FileRepositoryService m_service = null;
        static ServiceHost m_wcfStreamingServerHost = null;
        string m_ipAddress;
        public delegate void CallbackMessage(int code, string ipAdress, string fileName, string FullPath, string msg, int Hash);
        CallbackMessage pCallback;

        public void SetCallback(CallbackMessage p)
        {
            pCallback = p;
        }
        public string DropBoxInitialize()
        {
            try
            {
                if (m_dropboxInitialized == false)
                    m_dropbox.Initialize();
                m_dropboxInitialized = true;
                return "ok";
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public string DropBoxUpload(string FileName)
        {
            try
            {
                m_dropbox.Upload(FileName);
                return "ok";
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public string DropBoxClose()
        {
            try
            {
                if (m_dropbox != null)
                    m_dropbox.Close();
                m_dropboxInitialized = false;
                return "ok";
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public void StopWatch()
        {
            try
            {
                if (m_Watcher != null)
                {
                    m_bIsWatching = false;
                    m_event.Set();
                    m_Watcher.EnableRaisingEvents = false;
                    m_Watcher.Dispose();
                    m_Watcher = null;
                    while (m_queue.Count > 0)
                    {
                        UploadFileToServerWithPath(m_queue.Dequeue());
                    }
                    if (m_dropboxInitialized == true)
                    {
                        m_dropbox.Close();
                        m_dropboxInitialized = false;
                    }
                }
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        void CopyThread()
        {
            while (m_bIsWatching)
            {
                try
                {
                    m_event.WaitOne();
                    while (m_queue.Count > 1)
                    {
                        if (m_dropboxInitialized == true)
                        {
                            m_dropbox.Upload(m_queue.Dequeue());
                        }
                        if (m_connectedToFileServer == true)
                        {
                            UploadFileToServerWithPath(m_queue.Dequeue());
                        }
                    }
                }
                catch (Exception err)
                {

                }
            }
        }
        public void StartWatch(string PathToWatch, string Filter, bool IncludeSubdirectories)
        {
            try
            {
                if (m_Watcher == null)
                {
                    m_bIsWatching = true;
                    m_queue.Clear();
                    m_thread = new Thread(CopyThread);
                    m_thread.Start();
                    m_Watcher = new System.IO.FileSystemWatcher();
                    m_Watcher.Filter = Filter; // "*.*";
                    if (PathToWatch[PathToWatch.Length - 1] != '\\')
                    {
                        m_Watcher.Path = PathToWatch + "\\"; // txtFile.Text + "\\";
                    }
                    else
                    {
                        m_Watcher.Path = PathToWatch;
                    }

                    m_Watcher.IncludeSubdirectories = IncludeSubdirectories;


                    m_Watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                         | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                    m_Watcher.Changed += new FileSystemEventHandler(OnChanged);
                    m_Watcher.Created += new FileSystemEventHandler(OnCreated);
                    m_Watcher.Deleted += new FileSystemEventHandler(OnDelete);
                    m_Watcher.Renamed += new RenamedEventHandler(OnRenamed);
                    m_Watcher.EnableRaisingEvents = true;
                }
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        private void OnRenamed(object sender, RenamedEventArgs e)
        {


        }
        void Host_Faulted(object sender, EventArgs e)
        {
            //Console.WriteLine("Host faulted; reinitialising host");
            if (m_wcfStreamingServerHost != null)
                m_wcfStreamingServerHost.Abort();
        }

        void Service_FileRequested(object sender, FileEventArgs e)
        {
            //Console.WriteLine(string.Format("File access\t{0}\t{1}", e.VirtualPath, DateTime.Now));
        }

        void Service_FileUploaded(object sender, FileEventArgs e)
        {
            //Console.WriteLine(string.Format("File upload\t{0}\t{1}", e.VirtualPath, DateTime.Now));
        }

        void Service_FileDeleted(object sender, FileEventArgs e)
        {
            //Console.WriteLine(string.Format("File deleted\t{0}\t{1}", e.VirtualPath, DateTime.Now));
        }
        public string SetWCFStreamingServerRepositoryFolder(string storageFolder)
        {
            if (m_service != null)
            {
                Directory.CreateDirectory(storageFolder);
                m_service.RepositoryDirectory = storageFolder;
                return "ok";
            }
            else
            {
                return "failed";
            }
        }
        public string StartWCFStreamingServer(string ipAddress , string storageFolder)
        {
            try
            {
                if (m_wcfStreamingServerHost != null)
                {
                    Console.WriteLine("Already started");
                    return "ok";
                }

                int port = 5000;
                NetTcpBinding binding = new NetTcpBinding();
                binding.TransferMode = TransferMode.Streamed;
                binding.Security.Mode = SecurityMode.Transport;
                binding.SendTimeout = new TimeSpan(0, 0, 10);
                binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.None;
                var myEndpointAddress = new EndpointAddress("net.tcp://" + ipAddress + ":" + port);
                //m_service = new FileRepositoryService("FileRepositoryService", myEndpointAddress);
                m_service = new FileRepositoryService();


                if (Directory.Exists(storageFolder) == false)
                    Directory.CreateDirectory(storageFolder);

                m_service.RepositoryDirectory = storageFolder;

                m_service.FileRequested += new FileEventHandler(Service_FileRequested);
                m_service.FileUploaded += new FileEventHandler(Service_FileUploaded);
                m_service.FileDeleted += new FileEventHandler(Service_FileDeleted);

                m_wcfStreamingServerHost = new ServiceHost(m_service);
                m_wcfStreamingServerHost.Faulted += new EventHandler(Host_Faulted);
                m_wcfStreamingServerHost.Open();
                Console.WriteLine("Service started at address: " + ipAddress);
                return "ok";
            }
            catch (Exception err)
            {
                return err.Message;
            }
       
        
        }
        public string CloseWCFStreamingServer()
        {
            if (m_wcfStreamingServerHost != null)
            {
                m_wcfStreamingServerHost.Close();
                m_wcfStreamingServerHost = null;
                return "ok";
            }
            return "ok";
        }
        public void Connect(string ipAddress , string userName = "", string Password = "")
        {
            try
            {
                if (m_client == null)
                {
                    int port = 5000;
                    NetTcpBinding binding = new NetTcpBinding();
                    
                    binding.SendTimeout = new TimeSpan(0, 0, 15); // 1 hour
                    binding.OpenTimeout = new TimeSpan(0, 0, 15); // 1 hour
                    binding.ReceiveTimeout = new TimeSpan(0, 0, 15); // 1 hour

                    // Disable credential negotiation and establishment of the
                    // security context.
                    //binding.Security.Message.ClientCredentialType = MessageCredentialType.None;

                    binding.TransferMode = TransferMode.Streamed;
                    binding.Security.Mode = SecurityMode.None;
                    binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.None;
                    binding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.None;
                    var myEndpointAddress = new EndpointAddress("net.tcp://" + ipAddress + ":" + port);
                    m_client = new FileRepositoryServiceClient("FileRepositoryService", myEndpointAddress);

                    if ((userName == string.Empty) || (Password == string.Empty))
                    {
                        Console.WriteLine("Assume computer are in the domain");
                    }
                    else
                    {
                        m_client.ClientCredentials.Windows.ClientCredential.UserName = userName; // "IND-MPFM2\\Incteam";
                        m_client.ClientCredentials.Windows.ClientCredential.Password = Password;// "Bator23";
                        m_client.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.PeerOrChainTrust;
                    }
                    m_connectedToFileServer = true;
                }
            }
            catch (Exception err)
            {
                m_connectedToFileServer = false;
                throw (new SystemException(err.Message));
            }
        }
        private void OnDelete(object sender, FileSystemEventArgs e)
        {

        }
        
        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (m_queue.Count < 250)
                    m_queue.Enqueue(e.FullPath);
                m_event.Set();
            }
            catch (Exception err)
            {

            }
        }
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            //Console.WriteLine(e.FullPath);
        }

        public void DeleteFileFromServer(string FileName)
        {

            try
            {
                string virtualPath = Path.GetFileName(FileName);
                m_client.DeleteFile(virtualPath);             
            }
            catch (Exception err)
            {
            }
        }

        public int DeleteAllFilesOnServer()
        {
            List<string> x = RefreshFileList(); 

            for (int i = 0 ; i < x.Count ; i++)
            {
                m_client.DeleteFile(x[i]);    
            }
            return x.Count;
        }

        public List<string> RefreshFileList()
        {

            StorageFileInfo[] files = null;
            files = m_client.List(null);

            List<string> listOfFiles = new List<string>();

            foreach (var file in files)
            {
                listOfFiles.Add(Path.GetFileName(file.VirtualPath));
            }
            return listOfFiles;
        }
        public static String[] SplitPath(string path)
        {
            String[] pathSeparators = new String[] { "\\" };
            return path.Split(pathSeparators, StringSplitOptions.RemoveEmptyEntries);
        }
        public void SetIpAddress(string ip)
        {
            m_ipAddress = ip;
        }
        public void UploadFileToServerWithPath(string FileName)
        {
            try
            {
                string virtualPath = Path.GetFileName(FileName);
                string[] path = SplitPath(Path.GetDirectoryName(FileName));
                string targetPath = string.Empty;
                for (int i = 2; i < path.Length; i++)
                {
                    targetPath += path[i] + "\\";
                }
                using (Stream uploadStream = new FileStream(FileName, FileMode.Open))
                {
                    m_client.PutFileWithPath(new FileUploadMessage() { VirtualPath = virtualPath, TargetPath = targetPath, DataStream = uploadStream });
                    pCallback(1, m_ipAddress, "e", "e", "e", 1);
                }
                //File.Delete(FileName);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

        public void UploadFileToServer(string FileName)
        {
            try
            {
                string virtualPath = Path.GetFileName(FileName);
                using (Stream uploadStream = new FileStream(FileName, FileMode.Open))
                {
                    m_client.PutFile(new FileUploadMessage() { VirtualPath = virtualPath, DataStream = uploadStream });
                }
                //File.Delete(FileName);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public void Close()
        {
            try
            {
                while (m_queue.Count > 0)
                {
                    UploadFileToServerWithPath(m_queue.Dequeue());
                }
                if (m_client != null)
                    m_client.Close();
                m_client = null;
                m_connectedToFileServer = false;
                StopWatch();
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
    }
}
