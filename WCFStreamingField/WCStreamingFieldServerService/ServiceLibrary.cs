using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;
using FileServerWinClientLib;
using DropBoxSharpBoxApi;
using System.Net;
using RegistryClassApi;
using Microsoft.Win32;
using System.IO;
using System.Threading;
using System.Globalization;
using QueueBaseFileListApi; 


namespace ServiceLibrary
{

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class service1 : IService 
    {        
        public static IDuplexServiceCallback Callback;
        FileServerWinClient m_fileServerClient = new FileServerWinClient();
        string m_FileOwnerUserName = "System";
        public Dictionary<string, IDuplexServiceCallback> listCallback = new Dictionary<string ,IDuplexServiceCallback>();
        string m_ipAddress;
        string m_fieldGuid;
        const string FMT = "yyyy-MM-dd HH:mm:ss.fffffff";
        const int maxFilesInQueue = 40000;
        const string _writeListDir = "c:\\Goji\\FieldCreatedNotBroadcast\\writeList\\";
        const string _readListDir = "c:\\Goji\\FieldCreatedNotBroadcast\\readList\\";

        QueueBaseFileListWriter q = new QueueBaseFileListWriter(maxFilesInQueue, _writeListDir);
        QueueBaseFileListReader q1 = new QueueBaseFileListReader(maxFilesInQueue, _readListDir, _writeListDir);

        public void CloseService()
        {
            m_fileServerClient.StopWatch();
            m_fileServerClient.StopWatchWrongFiles();
            m_fileServerClient.Close();
            m_fileServerClient.CloseCopyThread();
            m_fileServerClient.CloseDropBoxConnection();            
        }

        void Initialize()
        {

            try
            {
                NetCard.NetCard n = new NetCard.NetCard();
                m_ipAddress = n.getComputerIP();
                while (m_ipAddress == "0.0.0.0")
                {
                    Thread.Sleep(10000);
                    m_ipAddress = n.getComputerIP();
                }
                File.WriteAllText("c:\\WCFStreamingClient.txt", "Initialized done: ip " + m_ipAddress);

                clsRegistry reg = new clsRegistry();
                m_fieldGuid = reg.GetStringValue(Registry.LocalMachine, "SOFTWARE\\Goji solutions\\Field", "Guid");
                if (reg.strRegError != null)
                {
                    m_fieldGuid = "error no guid";
                }

                FileServerWinClient.CallbackMessage pCallback = new FileServerWinClient.CallbackMessage(CallbackMessage);
                m_fileServerClient.SetCallback(pCallback);


                string strValue = reg.GetStringValue(
                      Registry.LocalMachine,
                      "SOFTWARE\\Goji solutions\\Field\\Watson",
                      "FileOwnerUserName");

                if (reg.strRegError == null)
                {
                    m_FileOwnerUserName = strValue;
                }
                else
                {
                    m_FileOwnerUserName = "System";
                }


                strValue = reg.GetStringValue(
                        Registry.LocalMachine,
                        "SOFTWARE\\Goji solutions\\Field\\Watson",
                        "WatsonStatus");
                if (reg.strRegError == null && strValue == "running")
                {
                    string userName;
                    userName = reg.GetStringValue(Registry.LocalMachine,
                                                   "SOFTWARE\\Goji solutions\\Field\\Watson",
                                                   "OperateUserName");

                    if (reg.strRegError != null)
                    {
                        pCallback(600, m_ipAddress, string.Empty, string.Empty, "Operator Owner does not exists", 0, DateTime.Now, 0, string.Empty);
                        return;
                    }

                    strValue = reg.GetStringValue(
                    Registry.LocalMachine,
                    "SOFTWARE\\Goji solutions\\Field\\Watson",
                    "StartWatchMonitorDirectory");
                    if (reg.strRegError == null)
                    {
                        if (Directory.Exists(strValue) == true)
                            m_fileServerClient.StartWatch(strValue, "*.bin", true, userName);
                    }
                }
            }
            catch (Exception err)
            {
                File.AppendAllText("c:\\GojiWCFStreamingClient.txt", err.Message);
            }
        }

        public string Ping()
        {
            return "Pong";
        }

        public List <DriveInfo> GetDrivesInfo()
        {
            List<DriveInfo> d = new List<DriveInfo>();
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    //Console.WriteLine("{0} {1} bytes", drive.Name, drive.TotalSize);
                    d.Add(drive);
                }
            }
            return d;
        }
         
        public service1()
        {
            var t = new Thread(Initialize);
            t.Start();

             
        }
        void CallbackMessage(int code, string ipAdress, string fileName, string FullPath, string msg, int Hash, DateTime startTime, ulong g360Index, string userName)
        {
            //Console.WriteLine("code: {0} ipadress {1} fileName: {2} , fullPath{3}  msg: {4}", code, ipAdress, fileName, FullPath, msg, startTime.ToString());
            string line = ipAdress + "," + fileName + "," + FullPath + "," + msg + "," + startTime.ToString() + "," + g360Index;
            int r = BroadcastMessage(code, line, startTime, userName);
            if (r == 0 && code == 18)
            {
                q.AddFile(FullPath, startTime, DateTime.Now, g360Index,userName);
            }
        }
        public void Register(string baseFieldGuid)
        {
            Callback = OperationContext.Current.GetCallbackChannel<IDuplexServiceCallback>();
            if (listCallback.ContainsKey(baseFieldGuid) == false)
                listCallback.Add(baseFieldGuid,Callback);
            else
                listCallback[baseFieldGuid] = Callback;
        }

        public string GetCurrentUploadedFileInfo()
        {
            return m_fileServerClient.GetCurrentUploadedFileInfo();
        }

        public int SyncFilesWithDataBase(string DirectoryToSync, bool recoursive, string userName , int SyncBy)
        {
            return m_fileServerClient.SyncFilesWithDataBase(DirectoryToSync, recoursive, userName,  SyncBy);
        }

        public void SetVerbose(bool set)
        {
            Console.WriteLine("Set verbose to: " + set.ToString());
            m_fileServerClient.m_verbose = set;
        }
        public bool GetVerbose()
        {
            return m_fileServerClient.m_verbose;
        }

        public string getGuild()
        {
            return m_fieldGuid;
        }
        public void SetCopyThreadTimeEvent(TimeSpan t)
        {
            m_fileServerClient.SetCopyThreadTimeEvent(t);
        }
        public void CleanFifoHandler()
        {
            m_fileServerClient.CleanFifoHandler();
        }

        public void CloseCopyThread()
        {
            m_fileServerClient.CloseCopyThread();
        }

        public void StartDropBoxUploadMode(bool start)
        {
            m_fileServerClient.StartDropBoxUploadMode(start);
        }
        public bool IsCopyThreadIsAlive()
        {
            return m_fileServerClient.IsCopyThreadIsAlive();
        }
        public void RunBoxInitialize()
        {
            m_fileServerClient.RunBoxInitialize();
        }

        public string MarkLastFileAsUploaded(string TargetPath,
                                             string VirtualPath,
                                             ulong currentUploadedG360Index,
                                             string currentUploadedFileOwner,
                                             long currentSizeOfFile)
        {
            return m_fileServerClient.MarkLastFileAsUploaded(TargetPath,
                                                             VirtualPath,
                                                             currentUploadedG360Index,
                                                             currentUploadedFileOwner,
                                                             currentSizeOfFile);    
        }

        public string HelloWorld(string str)
        {
            return "Hello world from " + str;
        }
        public void Echo()
        {
            BroadcastMessage(9, "This is echo test", DateTime.Now, "admin");
        }
       
        int BroadcastMessage(int code, string msg , DateTime startTime, string userName)
        {
            again:
            int count = listCallback.Count;
            Console.WriteLine("broadcast to: " + count);
            if (count == 0)
                return 0;
            foreach (KeyValuePair<string, IDuplexServiceCallback> entry in listCallback)    
            {
                try
                {
                    try
                    {
                        DateTime _date =  DateTime.SpecifyKind(startTime, DateTimeKind.Unspecified);
                        entry.Value.NotifyCallbackMessage(m_fieldGuid, m_ipAddress, code, msg, _date, userName);
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine("Error in BroadcastMessage: " + err.Message);
                        listCallback.Remove(entry.Key);
                        goto again;
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message);
                }
            }
            return count;
        }
        public string Close()
        {
            try
            {
                //m_fileServerClient.Close();
                return "ok";
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }

        public string SetWatsonRunning(bool set)
        {
            try
            {
                return m_fileServerClient.SetWatsonRunning(set);
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }


        public string DeleteAllWatsonGenerateFilesTimeSpanBeforeNow(DateTime time, string userName, bool byUser)
        {

            try
            {
                return m_fileServerClient.DeleteAllWatsonGenerateFilesTimeSpanBeforeNow(time, userName , byUser);
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }

        public string OpenTCPStreamingClient(string ipAddress, string userName, string password)
        {
            try
            {
                return m_fileServerClient.OpenTCPStreamingClient(ipAddress, userName, password);
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }

        public string AddDateToUploadTargetFolder(bool add)
        {
            try
            {
                return m_fileServerClient.AddDateToUploadTargetFolder(add);
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }

        public string DeleteAllUploadedFilesTimeSpanBeforeNow(DateTime time, string userName, bool byUser)
        {
            try
            {
                return m_fileServerClient.DeleteAllUploadedFilesTimeSpanBeforeNow(time, userName, byUser);
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }
        public TimeSpan GetCopyThreadTimeEvent()
        {
            try
            {
                return m_fileServerClient.GetCopyThreadTimeEvent();
            }
            catch (Exception err)
            {
                return new TimeSpan(0,0,0);
            }
        }
        public int GetFifoThreshold()
        {
            try
            {
                return m_fileServerClient.GetFifoThreshold();
            }
            catch (Exception err)
            {
                return -1;
            }
        } 

        public string SetFifoThreshold(int depth)
        {
            try
            {
                return m_fileServerClient.SetFifoThreshold(depth);
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }
        public int DeleteAllUploadedFilesOnServer(string password,string userName, bool byUser)
        {
            try
            {
                return m_fileServerClient.DeleteAllUploadedFilesOnServer(password, userName , byUser);
            }
            catch (Exception err)
            {
                return 0;
            }
        }
        public string [] RefreshFileList()
        {
            try
            {
                //return m_fileServerClient.RefreshFileList();
                return null;
            }
            catch (Exception err)
            {
                return null;
            }
        }
        public string UploadFileToServer(string FileName)
        {
            try
            {
                //m_fileServerClient.UploadFileToServer(FileName);
                return "ok";
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }
        public string SetWCFStreamingServerRepositoryFolder(string storageFolder)
        {
            //return m_fileServerClient.SetWCFStreamingServerRepositoryFolder(storageFolder);
            return "failed";
        }

        public string StartWCFStreamingServer(string ipAddress, string storageFolder)
        {
            try
            {
                //return m_fileServerClient.StartWCFStreamingServer(ipAddress, storageFolder);
                return "failed";
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }

        public void CloseDropBoxConnection()
        {
            try
            {
                m_fileServerClient.CloseDropBoxConnection();
            }
            catch (Exception err)
            {
                
            }
        }
        public string DropBoxInitialize()
        {
            try
            {
                return m_fileServerClient.DropBoxInitialize();
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }

        public string DropBoxClose()
        {
            try
            {
                return m_fileServerClient.DropBoxClose();
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }

        public string DropBoxUpload(string fileName)
        {
            try
            {
                return m_fileServerClient.DropBoxUpload(fileName);
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }

        public string ConnectToFileServer(string ipAddress, string UserName, string Password)
        {
            try
            {
                //m_fileServerClient.Connect(ipAddress, UserName, Password);
                return "ok";
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }
        public string StopWatch()
        {
            try
            {
                m_fileServerClient.StopWatch();
                return "ok";
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }
        public string InitiateSingleUpload()
        {
            return m_fileServerClient.InitiateSingleUpload();
        }
        public string GetStatistics(string userName, bool byUser)
        {
            return m_fileServerClient.GetStatistics(userName ,byUser);
        }
        public void DisconnectRepositoryStreamingClient()
        {
            m_fileServerClient.DisconnectRepositoryStreamingClient();
        }
        public void ConnectToStreamingServer(string ipAddress, string userName, string password)
        {
            m_fileServerClient.OpenRepositoryStreamingClient(ipAddress, userName , password);
        }

        public string GetClientGuid()
        {
            clsRegistry reg = new clsRegistry();
            m_fieldGuid = reg.GetStringValue(Registry.LocalMachine, "SOFTWARE\\Goji solutions\\Field", "Guid");
            if (reg.strRegError != null)
            {
                m_fieldGuid = "error no guid";
            }
            return m_fieldGuid;
        }

        public string DeleteAllFilesFromFieldDaysBefore(int days, string password, string userName, bool byUser)
        {
            return m_fileServerClient.DeleteAllFilesFromFieldDaysBefore(days, password,userName, byUser);
        }

        public string DeleteAllFilesFromFieldBetweenDates(DateTime start, DateTime end, bool includeTime, string password, string userName, bool byUser)
        {
            return m_fileServerClient.DeleteAllFilesFromFieldBetweenDates(start, end, includeTime,password,userName, byUser);
        }

        public string DeleteAllFilesFromFieldStartingFromDate(DateTime startingFrom, string password, string userName, bool byUser)
        {
            return m_fileServerClient.DeleteAllFilesFromFieldStartingFromDate(startingFrom, password, userName, byUser);
        }

        public string StopWatchWrongFiles()
        {
            try
            {
                return  m_fileServerClient.StopWatchWrongFiles();
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }

        public string StartWatchWrongFiles(string PathToWatch, bool IncludeSubdirectoriese)
        {
            try
            {
                return m_fileServerClient.StartWatchWrongFiles(PathToWatch, IncludeSubdirectoriese);
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }
        public string StartWatch(string PathToWatch, string Filter, bool IncludeSubdirectories, string fileOwnerUserName)
        {
            try
            {
                return m_fileServerClient.StartWatch(PathToWatch, Filter, IncludeSubdirectories, fileOwnerUserName);
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }
    }
}
