using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;
using FileServerWinClientLib;
using ServiceLoggerApi;
using DropBoxSharpBoxApi;
using System.Net; 


namespace ServiceLibrary
{
    // You have created a class library to define and implement your WCF service.
    // You will need to add a reference to this library from another project and add 
    // the code to that project to host the service as described below.  Another way
    // to create and host a WCF service is by using the Add New Item, WCF Service 
    // template within an existing project such as a Console Application or a Windows 
    // Application.


    [ServiceContract(SessionMode = SessionMode.Required,
                      CallbackContract = typeof(IDuplexServiceCallback))]
    public interface IService1
    {
        [OperationContract]
        string MyOperation1(string myValue);
        [OperationContract]
        string MyOperation2(DataContract1 dataContractValue);
        [OperationContract]
        string HelloWorld(string str);

        [OperationContract]
        string StartWatch(string PathToWatch, string Filter, bool IncludeSubdirectories);

        [OperationContract]
        string StopWatch();
        [OperationContract]
        string ConnectToFileServer(string ipAddress, string UserName , string Password);

        [OperationContract]
        string UploadFileToServer(string FileName);

        [OperationContract]
        string Close();

        [OperationContract]
        List<string> RefreshFileList();

             
        [OperationContract]
        string StartWCFStreamingServer(string ipAddress, string storageFolder);


        [OperationContract]
        int DeleteAllFilesOnServer();
        
        [OperationContract]
        string CloseWCFStreamingServer();

        [OperationContract]
        string SetWCFStreamingServerRepositoryFolder(string storageFolder);

        
        [OperationContract]
        string DropBoxInitialize();

        [OperationContract]
        string DropBoxClose();

        [OperationContract]
        string DropBoxUpload(string fileName);
    }
      
    public class service1 : IService1
    {
        public static IDuplexServiceCallback Callback;
        FileServerWinClient m_fileServerClient = new FileServerWinClient();
        public List<IDuplexServiceCallback> listCallback = new List<IDuplexServiceCallback>();
        string m_ipAddress;

        public service1()
        {
            NetCard.NetCard n = new NetCard.NetCard();
            m_ipAddress = n.getComputerIP();
            FileServerWinClient.CallbackMessage pCallback = new FileServerWinClient.CallbackMessage(CallbackMessage);
            m_fileServerClient.SetCallback(pCallback);
            m_fileServerClient.SetIpAddress(m_ipAddress);

        }
        void CallbackMessage(int code , string ipAdress , string fileName , string FullPath , string msg, int Hash)
        {
            Console.WriteLine(msg);
        }
         
        public string MyOperation1(string myValue)
        {
            return "Hello: " + myValue;
        }
        public string MyOperation2(DataContract1 dataContractValue)
        {
            return "Hello: " + dataContractValue.FirstName;
        }
        public string HelloWorld(string str)
        {
            return "Hello world from " + str;
        }

        void BroadcastMessage(int code, string msg)
        {

            for (int i = 0; i < listCallback.Count; i++)
            {
                try
                {
                    //Console.WriteLine("{0} , {1}", code, msg);
                    try
                    {
                        listCallback[i].NotifyCallbackMessage(m_ipAddress, code, msg);
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine(err.Message);
                        listCallback.RemoveAt(i);
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message);
                }
            }
        }
        public string Close()
        {
            try
            {
                m_fileServerClient.Close();
                return "ok";
            }
            catch (Exception err)
            {
                ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_CLIENT, err.Message);
                return err.Message;
            }
        }
        public int DeleteAllFilesOnServer()
        {
            try
            {
                return m_fileServerClient.DeleteAllFilesOnServer();
            }
            catch (Exception err)
            {
                ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_CLIENT, err.Message);
                return 0;
            }
        }
        public List<string> RefreshFileList()
        {
            try
            {
                return m_fileServerClient.RefreshFileList();
            }
            catch (Exception err)
            {
                ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_CLIENT, err.Message);
                return new List<string>();
            }
        }
        public string UploadFileToServer(string FileName)
        {
            try
            {
                m_fileServerClient.UploadFileToServer(FileName);
                return "ok";
            }
            catch (Exception err)
            {
                ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_CLIENT, err.Message);
                return err.Message;
            }
        }
        public string SetWCFStreamingServerRepositoryFolder(string storageFolder)
        {
            return m_fileServerClient.SetWCFStreamingServerRepositoryFolder(storageFolder);
        }

        public string CloseWCFStreamingServer()
        {
            return m_fileServerClient.CloseWCFStreamingServer();
        }

        public string StartWCFStreamingServer(string ipAddress, string storageFolder)
        {
            try
            {
                return m_fileServerClient.StartWCFStreamingServer(ipAddress, storageFolder);
            }
            catch (Exception err)
            {
                return err.Message;
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
                m_fileServerClient.Connect(ipAddress, UserName, Password);
                return "ok";
            }
            catch (Exception err)
            {
                ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_CLIENT, err.Message);
                return err.Message;
            }
        }
        public string StopWatch()
        {
            try
            {
                m_fileServerClient.StopWatch();
                ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_CLIENT, "Stopped watched");
                return "ok";
            }
            catch (Exception err)
            {
                ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_CLIENT, err.Message);
                return err.Message;
            }
        }
        public string StartWatch(string PathToWatch, string Filter, bool IncludeSubdirectories)
        {
            try
            {
                Callback = OperationContext.Current.GetCallbackChannel<IDuplexServiceCallback>();
                listCallback.Add(Callback);

                m_fileServerClient.StartWatch(PathToWatch, Filter, IncludeSubdirectories);
                ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_CLIENT, "Start watched " + PathToWatch);
                return "ok";
            }
            catch (Exception err)
            {
                ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_CLIENT, err.Message);
                return err.Message;
            }
        }
    }

    [DataContract]
    public class DataContract1
    {
        string firstName;
        string lastName;

        [DataMember]
        public string FirstName
        {
            get { return firstName; }
            set { firstName = value; }
        }
        [DataMember]
        public string LastName
        {
            get { return lastName; }
            set { lastName = value; }
        }
    }

}
