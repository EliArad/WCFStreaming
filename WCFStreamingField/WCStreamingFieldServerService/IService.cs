using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLibrary
{
    [ServiceContract(SessionMode = SessionMode.Required,
                      CallbackContract = typeof(IDuplexServiceCallback))]
    public interface IService
    {
        [OperationContract]
        string HelloWorld(string str);

        [OperationContract]
        bool IsCopyThreadIsAlive();

        [OperationContract]
        void CloseCopyThread();
                
        [OperationContract]
        void Echo();

        [OperationContract]
        string Ping();

        [OperationContract]
        void SetVerbose(bool s);

        [OperationContract]
        List<DriveInfo> GetDrivesInfo();

        [OperationContract]
        bool GetVerbose();


        [OperationContract]
        string MarkLastFileAsUploaded(string TargetPath,
                                      string VirtualPath,
                                      ulong currentUploadedG360Index,
                                      string currentUploadedFileOwner,
                                      long currentSizeOfFile);



        [OperationContract]
        string GetCurrentUploadedFileInfo();

        [OperationContract]
        string GetClientGuid();

        [OperationContract]
        string OpenTCPStreamingClient(string ipAddress, string userName, string password);

        [OperationContract]
        void ConnectToStreamingServer(string ipAddress, string userName, string password);

        [OperationContract]
        string StartWatch(string PathToWatch, string Filter, bool IncludeSubdirectories, string fileOwnerUserName);

        [OperationContract]
        string SetWatsonRunning(bool set);       

        [OperationContract]
        string StartWatchWrongFiles(string PathToWatch, bool IncludeSubdirectories);

        [OperationContract]
        string getGuild();

        [OperationContract]
        int SyncFilesWithDataBase(string DirectoryToSync, bool recoursive, string userName, int SyncBy);

        [OperationContract]
        string StopWatchWrongFiles();

        [OperationContract]
        void CloseDropBoxConnection();

        [OperationContract]
        string InitiateSingleUpload(); 

        [OperationContract]
        void SetCopyThreadTimeEvent(TimeSpan t);

        [OperationContract]
        void CleanFifoHandler();

        [OperationContract]
        string StopWatch();

        [OperationContract]
        void DisconnectRepositoryStreamingClient();

        [OperationContract]
        void Register(string serverIpAddress);

        [OperationContract]
        string ConnectToFileServer(string ipAddress, string UserName, string Password);

        [OperationContract]
        string UploadFileToServer(string FileName);

        [OperationContract]
        string Close();

        [OperationContract]
        string [] RefreshFileList();

        [OperationContract]
        string GetStatistics(string userName, bool byUser);


        [OperationContract]
        string StartWCFStreamingServer(string ipAddress, string storageFolder);

        [OperationContract]
        int GetFifoThreshold();

        [OperationContract]
        TimeSpan GetCopyThreadTimeEvent();

        [OperationContract]
        string DeleteAllWatsonGenerateFilesTimeSpanBeforeNow(DateTime time, string userName, bool byUser);
        [OperationContract]
        string DeleteAllUploadedFilesTimeSpanBeforeNow(DateTime time, string userName, bool byUser);

        [OperationContract]
        string AddDateToUploadTargetFolder(bool add);


        [OperationContract]
        int DeleteAllUploadedFilesOnServer(string password, string userName, bool byUser);

        [OperationContract]
        string SetWCFStreamingServerRepositoryFolder(string storageFolder);

        [OperationContract]
        string SetFifoThreshold(int depth);


        [OperationContract]
        string DropBoxInitialize();

      
        [OperationContract]
        string DropBoxClose();

        [OperationContract]
        string DropBoxUpload(string fileName);

        [OperationContract]
        string DeleteAllFilesFromFieldDaysBefore(int days, string password, string userName, bool byUser);

        [OperationContract]
        string DeleteAllFilesFromFieldBetweenDates(DateTime start, DateTime end, bool includeTime, string password, string userName, bool byUser);
            
        [OperationContract]
        string DeleteAllFilesFromFieldStartingFromDate(DateTime startingFrom, string password, string userName, bool byUser);

        [OperationContract]
        void StartDropBoxUploadMode(bool start);

        [OperationContract]
        void RunBoxInitialize();

    }   
}
