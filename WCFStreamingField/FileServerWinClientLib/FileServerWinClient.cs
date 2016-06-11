using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading;
using DropBoxSharpBoxApi;
using System.Security.Principal;
using System.ServiceModel.Security;
using FileServerWinClient;
using QueueBaseFileListApi;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using RegistryClassApi;
using Microsoft.Win32;
using FastDirectoryEnumeratorLib;

namespace FileServerWinClientLib
{ 
    public class FileServerWinClient  
    {
        bool m_closeAll = false;
        const string FMT = "yyyy-MM-dd HH:mm:ss.fffffff";
        string m_currentUploadedFile;
        string m_currentUploadedDate;
        ulong m_currentUploadedG360Index;
        string m_currentUploadedFileOwner;
        string m_currentUploadedVirtualPath;
        string m_currentUploadedTargetPath;
        long m_currentSizeOfFile = 0;
        DateTime m_startSendingTime;
        FileStreamingClient m_tcpStreamingClient = null;
        bool keepStorageClientConnectionOpen = true;
        string m_FileOwnerUserName = "System";
        string m_uploadErrorStatus  = string.Empty;
        int m_uploadErrorCount = 0;
        bool m_watsonRunning = false;
        AutoResetEvent m_readyToSendNextFile = new AutoResetEvent(false);
        public bool m_verbose = true;
        Thread m_syncFileThread = null;
        Thread m_runDropBoxThread = null;
        bool m_uploadInProgress = false;
        long m_actualQueueSize = 0;
        FileRepositoryServiceClient m_client = null;
        DateTime m_startTime;
        static bool m_dropboxInitialized = false;
        static bool m_dropboxUploadMode = false;
        DropBoxSharpBox m_dropbox = new DropBoxSharpBox();
        static bool m_connectedToFileServer = false;
        static bool m_connectedToTcpFileServer = false;
        private static System.IO.FileSystemWatcher m_Watcher = null;
        private static System.IO.FileSystemWatcher m_WatcherWrong = null;
        static bool m_bIsWatching = false;
        static bool m_bIsWatchingWrong = false;
        static EventWaitHandle m_event = new AutoResetEvent(false);
        static EventWaitHandle m_retryEvent = new AutoResetEvent(false);
        static EventWaitHandle m_uploadEvent = new AutoResetEvent(false);
        
        static Thread m_thread;
        public delegate void CallbackMessage(int code, 
                                             string ipAdress, 
                                             string fileName, 
                                             string FullPath, 
                                             string msg, 
                                             int Hash, 
                                             DateTime startTime, 
                                             ulong g360Index,
                                             string FileOwner);
        CallbackMessage pCallback;
        bool m_copyThread = false;
        const int maxFilesInQueue = 40000;

        const string _writeListDir = "c:\\writeList\\";
        const string _readListDir = "c:\\readList\\";

        QueueBaseFileListWriter q = new QueueBaseFileListWriter(maxFilesInQueue, _writeListDir);
        QueueBaseFileListReader q1 = new QueueBaseFileListReader(maxFilesInQueue, _readListDir, _writeListDir);
        int m_fifoFileThreshold = 2;

        string m_serverIpAddress;
        string m_userName;
        string m_password;

        string m_clientIpAddress;
        bool m_addDateToTargetFolder = false;

        TimeSpan m_eventSendingTime = new TimeSpan(0, 3, 0);
        string m_fieldGuid;

        
        public FileServerWinClient()
        {
            clsRegistry reg = new clsRegistry();
            string time = reg.GetStringValue(
                            Registry.LocalMachine,
                            "SOFTWARE\\Goji solutions\\Field\\Watson\\",
                            "StartCapture");
            if (reg.strRegError == null)
            {
                m_startTime = DateTime.ParseExact(time, FMT, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);    
            }           
             
            m_fieldGuid = reg.GetStringValue(Registry.LocalMachine, "SOFTWARE\\Goji solutions\\Field", "Guid");
            if (reg.strRegError != null)
            {
                m_fieldGuid = "error no guid";
            } 
            
            uint x;
            x = reg.GetDWORDValue(Registry.LocalMachine, "SOFTWARE\\Goji solutions\\Field\\Streaming", "fifo_threshold");
            if (reg.strRegError == null)
            {
                if (x > 2)
                {
                    m_fifoFileThreshold = (int)x;
                }
            }

            // Get string value
            string strValue = reg.GetStringValue(
                Registry.LocalMachine,
                "SOFTWARE\\Goji solutions\\Field\\Watson",
                "WatsonStatus");
            if (reg.strRegError == null && strValue == "running")
            {
                m_watsonRunning = true;
            }
            if (reg.strRegError == null && strValue == "stopped")
            {
                m_watsonRunning = false;
            }
        }
        public void SetVerbose(bool s)
        {
            m_verbose = s;
        }
        public void SetCallback(CallbackMessage p)
        {
            pCallback = p;

            clsRegistry reg = new clsRegistry();
            uint x;
            x = reg.GetDWORDValue(Registry.LocalMachine, "SOFTWARE\\Goji solutions\\Field\\Streaming\\DropBox", "InitializeDropBoxOnStart");
            if (reg.strRegError == null)
            {
                if (x == 1)
                {
                    if (m_runDropBoxThread == null || m_runDropBoxThread.IsAlive == false)
                    {
                        m_runDropBoxThread = new Thread(RunBoxInitialize);
                        m_runDropBoxThread.Start();
                    }
                }
            }
        }
        public void DisconnectRepositoryStreamingClient()
        {
            m_connectedToFileServer = false;
            CloseCopyThread();
        }
        public string AddDateToUploadTargetFolder(bool add)
        {
            m_addDateToTargetFolder = add;
            return "ok";
        }

        public string OpenTCPStreamingClient(string ipAddress, string userName , string password)
        {
            try
            {

                NetCard.NetCard n = new NetCard.NetCard();
                m_clientIpAddress = n.getComputerIP();

                m_serverIpAddress = ipAddress;
                m_userName = userName;
                m_password = password;
                m_connectedToTcpFileServer = true;

                m_tcpStreamingClient = new 
                FileStreamingClient(ipAddress,
                                    5005,
                                    m_fieldGuid,
                                    userName,
                                    password,
                                    null);

                if (m_verbose)
                {
                    string msg = "OpenRepositoryStreamingClient: " + m_userName + " " +  m_password;
                    pCallback(150, m_clientIpAddress, string.Empty, string.Empty, msg, 1, DateTime.Now, 0, m_userName);
                }

                if (m_thread == null || m_thread.IsAlive == false)
                {
                    m_thread = new Thread(CopyThread);
                    m_thread.Start();
                }
                return "ok";
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }
         
        public void OpenRepositoryStreamingClient(string ipAddress, string userName , string password)
        {
            try
            {

                NetCard.NetCard n = new NetCard.NetCard();
                m_clientIpAddress = n.getComputerIP();

                m_serverIpAddress = ipAddress;
                m_userName = userName;
                m_password = password;
                m_connectedToFileServer = true;

                if (m_verbose)
                {
                    string msg = "OpenRepositoryStreamingClient: " + m_userName + " " +  m_password;
                    pCallback(150, m_clientIpAddress, string.Empty, string.Empty, msg, 1, DateTime.Now , 0, m_userName);
                }

                if (m_thread == null || m_thread.IsAlive == false)
                {
                    m_thread = new Thread(CopyThread);
                    m_thread.Start();
                }
                 
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        FileRepositoryServiceClient ConnectFileRepositoryServer()
        {
            FileRepositoryServiceClient client;            
            var myEndpointAddress = new EndpointAddress("http://" + m_serverIpAddress + ":5000");
            client = new FileRepositoryServiceClient("FileRepositoryService", myEndpointAddress);
             
            if (m_userName != string.Empty && m_password != string.Empty)
            {
                client.ClientCredentials.Windows.ClientCredential.UserName = m_userName;
                client.ClientCredentials.Windows.ClientCredential.Password = m_password;
                client.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.PeerOrChainTrust;
            }
            return client;
        }

        public void StartDropBoxUploadMode(bool start)
        {
            m_dropboxUploadMode = start;
        }
        public void RunBoxInitialize()
        {
            try
            {
                NetCard.NetCard n = new NetCard.NetCard();
                m_clientIpAddress = n.getComputerIP();

                if (m_dropboxInitialized == false)
                {
                    Console.WriteLine("initializing dropbox..");
                    string res = m_dropbox.Initialize();
                    Console.WriteLine("done.. {0}", res);
                    pCallback(911, m_clientIpAddress, string.Empty, string.Empty, res, 1, DateTime.Now, 0, m_userName);
                    if (res != "ok")
                    {
                        return;
                    }
                }
                else
                {
                    pCallback(912, m_clientIpAddress, string.Empty, string.Empty, "Drop box already initialized", 1, DateTime.Now, 0, m_userName);
                }
                m_dropboxInitialized = true;
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
    
        public string DropBoxInitialize()
        {
            try
            {

                if (m_runDropBoxThread == null || m_runDropBoxThread.IsAlive == false)
                {
                    m_runDropBoxThread = new Thread(RunBoxInitialize);
                    m_runDropBoxThread.Start();
                }
                if (m_thread == null || m_thread.IsAlive == false)
                {
                    m_thread = new Thread(CopyThread);
                    m_thread.Start();
                }
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
                NetCard.NetCard n = new NetCard.NetCard();
                m_clientIpAddress = n.getComputerIP();

                m_startSendingTime = DateTime.Now;
                string res = m_dropbox.Upload(FileName);
                DateTime endSendingTime = DateTime.Now;
                TimeSpan TotalSendingTime = endSendingTime - m_startSendingTime;
                if (res == "ok")
                    pCallback(633, m_clientIpAddress, FileName, FileName, TotalSendingTime.ToString(), 1, DateTime.Now, 0,m_userName);
                else
                    pCallback(533, m_clientIpAddress, FileName, FileName, "drop box failed: " + res, 1, DateTime.Now, 0, m_userName);
                return res;
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

        string _GetCurrentUploadedFileInfo()
        {
            string s = m_currentUploadedFile + "," +
                       m_currentUploadedDate + "," +
                       m_currentUploadedG360Index + "," +
                       m_currentUploadedFileOwner + "," +
                       m_currentUploadedVirtualPath + "," +
                       m_currentUploadedTargetPath;

            return s;
        }

        public string GetCurrentUploadedFileInfo()
        {
            string s = m_currentUploadedFile + "," +
                       m_currentUploadedDate + "," +
                       m_currentUploadedG360Index + "," +
                       m_currentUploadedFileOwner + "," +
                       m_currentUploadedVirtualPath + "," +
                       m_currentUploadedTargetPath;

            return s;
        }
        private void UploadFileToServerWithPath(string FileName, DateTime startTimeToSend, ulong listNum, ulong fileNumber, ulong g360Index, string fileOwnerUserName, DateTime wtsDateTime)
        {

            string virtualPath = Path.GetFileName(FileName);

            string[] path = SplitPath(Path.GetDirectoryName(FileName));
            string targetPath = string.Empty;
            for (int i = 2; i < path.Length; i++)
            {
                targetPath += path[i] + "\\";
            }

            if (File.Exists(FileName) == false)
            {
                q1.MarkAsSent();
                m_event.Set();
                m_retryEvent.Set();
                return;
            }
         
            using (Stream uploadStream = new FileStream(FileName, FileMode.Open))
            {
                try
                {
                    if (m_client != null && keepStorageClientConnectionOpen == false)
                    {
                        m_client.Close();
                        m_client = null;
                    }
                    Console.WriteLine("Connect to repository..");

                    m_currentUploadedFile = string.Empty;
                    string strstartDate = DateTime.Now.AddYears(-10).ToString(FMT);
                    m_currentUploadedDate = strstartDate;
                    m_currentUploadedG360Index = 0;
                    m_currentUploadedFileOwner = string.Empty;
                    m_currentUploadedVirtualPath = string.Empty;
                    m_currentUploadedTargetPath = string.Empty;


                    if (m_verbose)
                    {
                        string msg = "Trying to connect to respository server..";
                        pCallback(150, m_clientIpAddress, string.Empty, string.Empty, msg, 1, DateTime.Now, 0, fileOwnerUserName);
                    }

                    if (m_client == null)
                        m_client = ConnectFileRepositoryServer();
                    Console.WriteLine("Connection done, uploading file: " + FileName);
                    m_uploadInProgress = true;
                    DateTime startSendingTime = DateTime.Now;

                    
                    m_currentUploadedFile = FileName;
                    strstartDate = DateTime.Now.ToString(FMT);
                    m_currentUploadedDate = strstartDate;
                    m_currentUploadedG360Index = g360Index;
                    m_currentUploadedFileOwner = fileOwnerUserName;
                    m_currentUploadedVirtualPath = virtualPath;
                    m_currentUploadedTargetPath = targetPath;


                    if (m_verbose)
                    {
                        string msg = "Connnected to repository , uploading file " + FileName + " (" + g360Index + ")";
                        Console.WriteLine(msg);
                        pCallback(155,
                                  m_clientIpAddress,
                                  string.Empty,
                                  string.Empty,
                                  _GetCurrentUploadedFileInfo(),
                                  1,
                                  DateTime.Now,
                                  0,
                                  fileOwnerUserName);
                    }
                                         
                    // Create new FileInfo object and get the Length.
                    FileInfo f = new FileInfo(m_currentUploadedFile);
                    m_currentSizeOfFile = f.Length;

                    m_client.PutFileWithPath(new FileUploadMessage() { 
                        VirtualPath = virtualPath, 
                        TargetPath = targetPath, 
                        DataStream = uploadStream, 
                        startTime = startTimeToSend,
                        clientIpAddress = m_clientIpAddress, 
                        fieldGuid = m_fieldGuid, 
                        G360Index = g360Index, 
                        _fileOwnerUserName = fileOwnerUserName,
                        AddDateToTargetFolder = m_addDateToTargetFolder,
                        watsonDateTime = wtsDateTime,
                        SizeOfFile = f.Length
                    });

                    m_readyToSendNextFile.WaitOne(10000);

                }
                catch (Exception err)
                {
                    m_uploadInProgress = false;
                    Console.WriteLine(err.Message);
                    pCallback(200, m_clientIpAddress, FileName, virtualPath, err.Message, 1, DateTime.Now, 0, fileOwnerUserName);
                    m_uploadErrorStatus = err.Message;
                    m_uploadErrorCount++;
                    if (m_client != null)
                    {
                        m_client.Close();
                        m_client = null;
                    }
                }
            }
        }

        public string MarkLastFileAsUploaded(string TargetPath,
                                             string VirtualPath,
                                             ulong currentUploadedG360Index, 
                                             string currentUploadedFileOwner,
                                             long currentSizeOfFile)
        {


            if ((TargetPath != m_currentUploadedTargetPath) ||
                (VirtualPath != m_currentUploadedVirtualPath) ||
                (currentUploadedG360Index != m_currentUploadedG360Index) ||
                (currentSizeOfFile != m_currentSizeOfFile) || 
                (currentUploadedFileOwner != m_currentUploadedFileOwner))
            {
                return "Error, this file was not uploaded";
            }

            DateTime endSendingTime = DateTime.Now;
            TimeSpan TotalSendingTime = endSendingTime - m_startSendingTime;
            Console.WriteLine("TotalSendingTime: {0}", TotalSendingTime.ToString());
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.WriteLine("File uploaded: " + m_currentUploadedFile);
            }
            else
                Debug.WriteLine("File uploaded: " + m_currentUploadedFile);
            m_uploadInProgress = false;

            q1.MarkAsSent();

            if (m_verbose)
            {
                string msg = "File uploaded and marked as send " + m_currentUploadedFile;
                pCallback(150, m_clientIpAddress, string.Empty, string.Empty, msg, 1, DateTime.Now, 0, currentUploadedFileOwner);
            }

            m_currentUploadedFile = string.Empty;
            string strstartDate = DateTime.Now.AddYears(-10).ToString(FMT);
            m_currentUploadedDate = strstartDate;
            m_currentUploadedG360Index = 0;
            m_currentUploadedFileOwner = string.Empty;
            m_currentUploadedVirtualPath = string.Empty;
            m_currentUploadedTargetPath = string.Empty;

            m_readyToSendNextFile.Set();

            return "ok";
        }

        public void CloseCopyThread()
        {
            m_copyThread = false;
            m_event.Set();
            m_retryEvent.Set();
            m_uploadEvent.Set();
            m_readyToSendNextFile.Set();
            if (m_thread != null && m_thread.IsAlive == true)
                m_thread.Join();
        }
        public void StopWatch()
        {
            try
            {
                if (m_Watcher != null)
                {
                    m_bIsWatching = false;
                    m_Watcher.EnableRaisingEvents = false;
                    m_Watcher.Dispose();
                    m_Watcher = null;
                }
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
                m_closeAll = true;
                DisconnectRepositoryStreamingClient();

                if (m_syncFileThread != null)
                    m_syncFileThread.Join();

                if (m_runDropBoxThread != null)
                    m_runDropBoxThread.Join();

                if (m_client != null)
                {
                    m_client.Close();
                }
            }
            catch (Exception err)
            {

            }            
        }
        public string StopWatchWrongFiles()
        {
            try
            {
                if (m_WatcherWrong != null)
                {
                    m_bIsWatchingWrong = false;
                    m_WatcherWrong.EnableRaisingEvents = false;
                    m_WatcherWrong.Dispose();
                    m_WatcherWrong = null;
                }
                return "ok";
            }
            catch (Exception err)
            {
               return err.Message;
            }
        }
        public void CloseDropBoxConnection()
        {
            if (m_dropboxInitialized == true)
            {
                m_dropbox.Close();
                m_dropboxInitialized = false;
            }
        }
        public void SetCopyThreadTimeEvent(TimeSpan t)
        {
            m_eventSendingTime = t;
            Console.WriteLine("Changing the copy thread time event to : {0}:{1}:{2}", t.Hours, t.Minutes, t.Seconds);
            m_event.Set();
            string s =  string.Format("Changing the copy thread time event to-{0}:{1}:{2}", t.Hours, t.Minutes, t.Seconds);
            pCallback(201, m_clientIpAddress, s, string.Empty, "0", 0, DateTime.Now, 0,m_FileOwnerUserName);
        }

        public int SyncFilesWithDataBase(string DirectoryToSync, bool recoursive, string userName , int SyncBy)
        {
            if (m_syncFileThread == null || m_syncFileThread.IsAlive == false)
            {
                m_syncFileThread = new Thread(() => SyncFileThread(DirectoryToSync, recoursive, userName, SyncBy));
                m_syncFileThread.Start();
                return 1;
            }
            else
            {
                NetCard.NetCard n = new NetCard.NetCard();
                m_clientIpAddress = n.getComputerIP();
                pCallback(133, m_clientIpAddress, "0", string.Empty, "0", 0, DateTime.Now,0,m_FileOwnerUserName);
                return 0;
            }
        }

        void SyncFileThread(string DirectoryToSync, bool recoursive, string userName, int SyncBy)
        {
            try
            {
                int count = 0;
                FileData[] files = FastDirectoryEnumerator.GetFiles(DirectoryToSync,
                                                                   "*.Bin",
                                                                   recoursive == true ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

                ulong g360Index;
                using (UploadHistoryLogApi m = new UploadHistoryLogApi(_writeListDir, false))
                {
                    m.LoadLogToMemoryDic();
                    string fileToAdd = string.Empty;
                    Dictionary<string, bool> list = m.DicFileList;
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (list.ContainsKey(files[i].Path) == false)
                        {
                            switch (SyncBy)
                            { 
                                case 0:
                                    q.AddFile(files[i].Path, m_startTime, files[i].CreationTime, out g360Index, userName);
                                break;
                                case 1:
                                    q.AddFile(files[i].Path, m_startTime, files[i].LastAccesTime, out g360Index, userName);
                                break;
                                case 2:
                                    q.AddFile(files[i].Path, m_startTime, files[i].LastWriteTime, out g360Index, userName);
                                break;
                            }
                            count++;
                        }
                    }
                }
                pCallback(133, m_clientIpAddress, count.ToString(), string.Empty, "ok", 0, DateTime.Now, 0, m_FileOwnerUserName);
            }
            catch (Exception err)
            {
                pCallback(133, m_clientIpAddress, "0", string.Empty, err.Message, 0, DateTime.Now,0, m_FileOwnerUserName);
            }
        }

        public TimeSpan GetCopyThreadTimeEvent()
        {
            return m_eventSendingTime;
        }

        long getQueueSize()
        {
            ulong x = q.readCurListFileNum();
            ulong _x = q.readCurFileNum();
            ulong writeIndex = x * maxFilesInQueue + _x;

            ulong x1 = q1.readCurListFileNum();
            ulong _x1 = q1.readCurFileNum();
            ulong readIndex = x1 * maxFilesInQueue + _x1;
            //Console.WriteLine("Number of files to send: {0}", writeIndex - readIndex);
            m_actualQueueSize = (long)(writeIndex - readIndex);
            return m_actualQueueSize;
        }
        public bool IsCopyThreadIsAlive()
        {
            if (m_thread == null)
                return false;
            return m_thread.IsAlive;
        }
        public string SetWatsonRunning(bool set)
        {
            try
            {
                m_watsonRunning = set;
                return "ok";
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }
        void CopyThread()
        {
            m_copyThread = true;
            DateTime startTime;
            DateTime ?watsonDateTime;
            ulong g360Index = 0;

            if (m_verbose)
            {
                string msg = "CopyThread started";
                pCallback(150, m_clientIpAddress, string.Empty, string.Empty, msg, 1, DateTime.Now,0,m_userName);
            }

            while (m_copyThread)
            {
                try
                {

                    ulong x = q.readCurListFileNum();
                    ulong _x = q.readCurFileNum();
                    ulong writeIndex = x * maxFilesInQueue + _x;

                    ulong x1 = q1.readCurListFileNum();
                    ulong _x1 = q1.readCurFileNum();
                    ulong readIndex = x1 * maxFilesInQueue + _x1;
                    //Console.WriteLine("Number of files to send: {0}", writeIndex - readIndex);
                    m_actualQueueSize = (long)(writeIndex - readIndex);

                    if (m_watsonRunning == true && m_actualQueueSize < m_fifoFileThreshold)
                    {
                        m_event.WaitOne();
                        if (m_copyThread == false)
                            return;
                    }
                    else if (m_watsonRunning == false && m_actualQueueSize == 0)
                    {
                        m_event.WaitOne();
                        if (m_copyThread == false)
                            return;
                    }

                    if (m_uploadErrorCount > 10)
                    {
                        Console.WriteLine("After 10 reties to upload , we will wait 10 minutes time");
                        m_retryEvent.WaitOne(new TimeSpan(0, 10 , 0));
                        if (m_copyThread == false)
                            return;
                        m_uploadErrorCount = 0;
                    }

                    if (m_watsonRunning == true)
                    {
                        m_uploadEvent.WaitOne(m_eventSendingTime);
                        if (m_copyThread == false)
                            return;
                    }
                    
                    if (m_copyThread == false)
                        return;
                    ulong listNum,  fileNumber;
                    bool copyStatus;                   
                    if (m_watsonRunning == true)
                    {
                        if (m_actualQueueSize < m_fifoFileThreshold)
                            continue;
                    }
                    else
                    {
                        if (m_actualQueueSize == 0)
                            continue;
                    }
                    string userName;
                    string fileName = q1.GetFile(out listNum, out fileNumber, out copyStatus, out startTime, out watsonDateTime, out g360Index, out userName);
                    if (fileName == null)
                        continue;
                    if (m_dropboxInitialized == true && m_dropboxUploadMode == true)
                    {
                        if (File.Exists(fileName) == false)
                        {
                            q1.MarkAsSent();
                            m_event.Set();
                            m_retryEvent.Set();
                            continue;
                        }
                        DateTime startSendingTime = DateTime.Now;
                        Console.WriteLine("uploading {0} to drop box", fileName);
                        string res;
                        if ((res = m_dropbox.Upload(fileName)) != "ok")
                        {
                            pCallback(533, m_clientIpAddress, fileName, fileName, "drop box failed: " + res, 1, DateTime.Now, 0, userName);
                            continue;
                        }
                        q1.MarkAsSent();
                        DateTime endSendingTime = DateTime.Now;
                        TimeSpan TotalSendingTime = endSendingTime - startSendingTime;
                        Console.WriteLine("TotalSendingTime: {0}", TotalSendingTime.ToString());
                        pCallback(633, m_clientIpAddress, fileName, fileName, TotalSendingTime.ToString(), 1, DateTime.Now, 0, userName); 
                    }
                    if (m_connectedToFileServer == true)
                    {
                        UploadFileToServerWithPath(fileName, startTime, listNum, fileNumber, g360Index, userName, watsonDateTime.Value);
                    }
                    if (m_connectedToTcpFileServer == true && m_tcpStreamingClient != null)
                    {
                        if (File.Exists(fileName) == false)
                        {
                            q1.MarkAsSent();
                            m_event.Set();
                            m_retryEvent.Set();
                            continue;
                        }

                        m_uploadInProgress = true;
                        DateTime startSendingTime = DateTime.Now;

                        if (m_verbose)
                        {
                            string msg = "Connnected to repository , uploading file " + fileName;
                            pCallback(150, m_clientIpAddress, string.Empty, string.Empty, msg, 1, DateTime.Now, 0, userName);
                        }

                        m_tcpStreamingClient.SendFile(fileName, userName, m_fieldGuid, startTime, listNum, fileNumber, g360Index, userName, watsonDateTime.Value);


                        DateTime endSendingTime = DateTime.Now;
                        TimeSpan TotalSendingTime = endSendingTime - startSendingTime;
                        Console.WriteLine("TotalSendingTime: {0}", TotalSendingTime.ToString());
                        if (System.Diagnostics.Debugger.IsAttached)
                        {
                            Console.WriteLine("File uploaded: " + fileName);
                        }
                        else
                            Debug.WriteLine("File uploaded: " + fileName);
                        m_uploadInProgress = false;

                        //q1.MarkAsSent();

                        if (m_verbose)
                        {
                            string msg = "File uploaded and marked as send " + fileName;
                            pCallback(150, m_clientIpAddress, string.Empty, string.Empty, msg, 1, DateTime.Now, 0, userName);
                        }
                    }
                }
                catch (Exception err)
                {                   
                    Console.WriteLine(err.Message);
                }
            }
        }
         
        public string StartWatch(string PathToWatch, string Filter, bool IncludeSubdirectories, string userName)
        {
            try
            {
                m_FileOwnerUserName = userName;
                 
                if (m_Watcher != null)
                {
                    StopWatch();
                }
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
                m_bIsWatching = true;
                Console.WriteLine("Watch started!");
                return "start watch ok on path, " + PathToWatch + " ,filter: " + Filter;
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public string StartWatchWrongFiles(string PathToWatch, bool IncludeSubdirectories)
        {
            try
            {
                if (m_WatcherWrong != null)
                {
                    StopWatchWrongFiles();
                }
                m_WatcherWrong = new System.IO.FileSystemWatcher();
                m_WatcherWrong.Filter = "*.wrong";
                if (PathToWatch[PathToWatch.Length - 1] != '\\')
                {
                    m_WatcherWrong.Path = PathToWatch + "\\"; // txtFile.Text + "\\";
                }
                else
                {
                    m_WatcherWrong.Path = PathToWatch;
                }

                m_WatcherWrong.IncludeSubdirectories = IncludeSubdirectories;
                m_WatcherWrong.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                        | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                m_WatcherWrong.Changed += new FileSystemEventHandler(OnChanged);
                m_WatcherWrong.Created += new FileSystemEventHandler(OnCreated);
                m_WatcherWrong.Deleted += new FileSystemEventHandler(OnDelete);
                m_WatcherWrong.Renamed += new RenamedEventHandler(OnRenamed);
                m_WatcherWrong.EnableRaisingEvents = true;
                m_bIsWatchingWrong = true;
                return "start watch ok on path, " + PathToWatch + " ,filter: " + "*.wrong";
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        private void OnRenamed(object sender, RenamedEventArgs e)
        {


        }
        public void CleanFifoHandler()
        {
            q.RemoveFileQueueLists();
            q1.RemoveFileQueueLists();
            q = new QueueBaseFileListWriter(maxFilesInQueue, _writeListDir);
            q1 = new QueueBaseFileListReader(maxFilesInQueue, _readListDir, _writeListDir);
        }
        private void OnDelete(object sender, FileSystemEventArgs e)
        {

        }
        
        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                const string FMT = "yyyy-MM-dd HH:mm:ss.fffffff";
                clsRegistry reg = new clsRegistry();
                string time = reg.GetStringValue(
                                Registry.LocalMachine,
                                "SOFTWARE\\Goji solutions\\Field\\Watson\\",
                                "StartCapture");
                if (reg.strRegError != null)
                {
                    throw (new SystemException("Cannot get watson start time"));
                }
                m_startTime = DateTime.ParseExact(time, FMT, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);


                // first date time is the created time , second is the upload time
                ulong g360Index;
                //Console.WriteLine("e.FullPath:" + e.FullPath);
                //Console.WriteLine("m_startTime:" + m_startTime);
                //Console.WriteLine("m_FileOwnerUserName:" + m_FileOwnerUserName);
                q.AddFile(e.FullPath, m_startTime, DateTime.Now, out g360Index, m_FileOwnerUserName);
                //Console.WriteLine("Add file Ok");
                pCallback(18, m_clientIpAddress, Path.GetFileName(e.FullPath), e.FullPath, "Watson file", 1, m_startTime, g360Index, m_FileOwnerUserName);
                //Console.WriteLine("Add file to queue: " + e.FullPath);
                m_event.Set();
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            //Console.WriteLine(e.FullPath);
        }

        public string DeleteAllFilesFromFieldDaysBefore(int days, string password, string userName, bool byUser)
        {
            try
            {
                using (UploadHistoryLogApi m = new UploadHistoryLogApi(_writeListDir))
                {
                    try
                    {
                        return m.DeleteAllFilesFromFieldDaysBefore(days, password,userName,byUser);
                    }
                    catch (Exception err)
                    {
                        return err.Message;
                    }
                }
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }


        public string DeleteAllWatsonGenerateFilesTimeSpanBeforeNow(DateTime time, string userName, bool byUser)
        {

            using (UploadHistoryLogApi m = new UploadHistoryLogApi(_writeListDir))
            {
                try
                {
                    return m.DeleteAllWatsonGenerateFilesTimeSpanBeforeNow(time, userName, byUser);                    
                }
                catch (Exception err)
                {
                    return err.Message;
                }
            }
        }
        public string DeleteAllUploadedFilesTimeSpanBeforeNow(DateTime time, string userName, bool byUser)
        {

            using (UploadHistoryLogApi m = new UploadHistoryLogApi(_writeListDir))
            {
                try
                {
                    return m.DeleteAllUploadedFilesTimeSpanBeforeNow(time, userName, byUser);
                }
                catch (Exception err)
                {
                    return err.Message;
                }
            }
        }

        public string DeleteAllFilesFromFieldBetweenDates(DateTime start, DateTime end, bool includeTime, string password, string userName, bool byUser)
        {
            try
            {
                using (UploadHistoryLogApi m = new UploadHistoryLogApi(_writeListDir))
                {
                    try
                    {
                        return m.DeleteAllFilesFromFieldBetweenDates(start, end, includeTime, password, userName , byUser);
                    }
                    catch (Exception err)
                    {
                        return err.Message;
                    }
                }
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }
        public string DeleteAllFilesFromFieldStartingFromDate(DateTime startingFrom, string password, string userName, bool byUser)
        {
            try
            {
                using (UploadHistoryLogApi m = new UploadHistoryLogApi(_writeListDir))
                {
                    try
                    {
                        return m.DeleteAllFilesFromFieldStartingFromDate(startingFrom, password, userName, byUser);
                    }
                    catch (Exception err)
                    {
                        return err.Message;
                    }
                }
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }

        public int DeleteAllUploadedFilesOnServer(string password, string userName, bool byUser)
        {
            using (UploadHistoryLogApi m = new UploadHistoryLogApi(_writeListDir))
            {
                try
                {
                    int numberOfDeletedFiles;
                    int numberOfActualDeletedFiles;
                    m.DeleteAllUploadedFiles(out numberOfDeletedFiles, out numberOfActualDeletedFiles, password, userName, byUser);
                    return numberOfActualDeletedFiles;
                }
                catch (Exception err)
                {
                    return -1;
                }
            }
        }
        public int GetFifoThreshold()
        {
            return m_fifoFileThreshold;
        }
        
        public string SetFifoThreshold(int depth)
        {
            if (depth >= 2)
            {   
                m_fifoFileThreshold = depth;
                clsRegistry reg = new clsRegistry();
                uint x;
                reg.SetDWORDValue(Registry.LocalMachine, "SOFTWARE\\Goji solutions\\Field\\Streaming", "fifo_threshold" , depth);
                if (reg.strRegError == null)
                {
                    return "ok";
                }
                else
                {
                    return reg.strRegError;
                }                
            }
            else
            {
                return "Size less than 2";
            }
        }
        public string InitiateSingleUpload()
        {
            m_event.Set();
            m_retryEvent.Set();
            m_uploadEvent.Set();
            if (m_copyThread == false)
                return "copy thread is not enable";
            else {
                long size = getQueueSize();
                return "queue size is " + size;
            }
        }
        public string GetStatistics(string userName, bool byUser)
        {

            using (UploadHistoryLogApi m = new UploadHistoryLogApi(_writeListDir))
            {
                int waitingToUpload;
                int numberUploadedAlready;
                double totalStorageSize;
                double totalStorageWaitingToUploadSize;
                double totalStorageUploadedSize;
                int waitingToUploadDoesNotExists;
                int numberOfUploadedFilesThatAreStillExistOnField;
                int numberOfUploadedFilesThatAreNotExistOnField;
                int waitingToUploadStilExistsOnField;
              

                try
                {
                    m.GetStatistics(out waitingToUpload,
                                    out numberUploadedAlready,
                                    out totalStorageSize,
                                    out totalStorageWaitingToUploadSize,
                                    out totalStorageUploadedSize,
                                    out waitingToUploadDoesNotExists,
                                    out numberOfUploadedFilesThatAreStillExistOnField,
                                    out numberOfUploadedFilesThatAreNotExistOnField,
                                    out waitingToUploadStilExistsOnField,
                                    userName , 
                                    byUser);


                    return waitingToUpload.ToString() + "," + numberUploadedAlready + "," + totalStorageSize + "," + totalStorageWaitingToUploadSize + "," + totalStorageUploadedSize + "," + waitingToUploadDoesNotExists + ", " + numberOfUploadedFilesThatAreStillExistOnField + "," + numberOfUploadedFilesThatAreNotExistOnField + ", " + m_uploadInProgress + "," + waitingToUploadStilExistsOnField;
                }
                catch (Exception err)
                {
                    return err.Message;
                }
            }
        }
        public static String[] SplitPath(string path)
        {
            String[] pathSeparators = new String[] { "\\" };
            return path.Split(pathSeparators, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
