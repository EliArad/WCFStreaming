using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Description;
using ServiceLibrary;
using System.IO;
using FileServer.Services;

namespace GojiWCFStreamingClient
{
    public class WCFServiceApi
    {
        private ServiceHost host = null;
        private string urlMeta, urlService = "";
        IService1 m_proxy;
        static ServiceHost m_wcfStreamingServerHost = null;        
        static FileRepositoryService m_service = null;

        /* The client that sits on the field where watson is located */
        public string StartClient(string ipAddress,int port)
        {
            try
            {
                string endPointAddr = "";
                endPointAddr = "net.tcp://" + ipAddress + ":" + port + "/MyService";
                NetTcpBinding tcpBinding = new NetTcpBinding();
                tcpBinding.TransactionFlow = false;
                //tcpBinding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;
                //tcpBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
                tcpBinding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.None;
                tcpBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.None;
                tcpBinding.Security.Mode = SecurityMode.None;
                EndpointAddress endpointAddress = new EndpointAddress(endPointAddr);
                m_proxy = ChannelFactory<IService1>.CreateChannel(tcpBinding, endpointAddress);
                string res = m_proxy.HelloWorld("test");
                return res;
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }

        public string StopWatch()
        {
            if (m_proxy != null)
            {
                string res = m_proxy.StopWatch();
                return res;
            }
            else
            {
                return "proxy not initialize";
            }
        }
        public List<string> RefreshFileList()
        {
            if (m_proxy != null)
                return m_proxy.RefreshFileList();
            else
                return null;
        }

        public string StartWCFStreamingServerOnProxy(string ipAddress,int port , string storageFolder)
        {
            try
            {
                if (m_proxy != null)
                    return m_proxy.StartWCFStreamingServer(ipAddress, port , storageFolder);
                else
                    return "proxy not initialize";
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public string SetWCFStreamingServerRepositoryFolderOnProxy(string storageFolder)
        {
            try
            {
                if (m_proxy != null)
                    return m_proxy.SetWCFStreamingServerRepositoryFolder(storageFolder);
                else 
                return "proxy not initialize";
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public void CloseWCFStreamingServerOnProxy()
        {
            try
            {
                if (m_proxy != null)
                    m_proxy.CloseWCFStreamingServer();
                else
                    throw (new SystemException("proxy not initialize"));
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

        public int  DeleteAllFilesOnServer()
        {
            try
            {
                if (m_proxy != null)
                    return m_proxy.DeleteAllFilesOnServer();
                else
                    throw (new SystemException("proxy not initialize"));
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public string StartWatch(string PathToWatch, string Filter, bool IncludeSubdirectories)
        {
            try
            {
                if (m_proxy != null)
                {
                    string res = m_proxy.StartWatch(PathToWatch, Filter, IncludeSubdirectories);
                    return res;
                }
                else
                {
                    return "proxy not initialize";
                }
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public string ConnectToFileServer(string ipAddress, int port, string UserName , string Password)
        {
            try
            {
                if (m_proxy != null)
                {
                    string res = m_proxy.ConnectToFileServer(ipAddress, port, UserName, Password);
                    return res;
                }
                else
                {
                    return "proxy not initialize";
                }
            }
            catch (Exception err)
            {
                return "error";                
            }
        }

        public string CloseDropBox()
        {
            try
            {
                if (m_proxy != null)
                {
                    string res = m_proxy.DropBoxClose();
                    return res;
                }
                else
                {
                    return "proxy not initialize";
                }                 
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }
        public string InitializeDropBox()
        {
            try
            {
                if (m_proxy != null)
                {
                    string res = m_proxy.DropBoxInitialize();
                    return res;
                }
                else
                {
                    return "proxy not initialize";
                }
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }

        public void CloseWCFStreamingServer()
        {
            if (m_wcfStreamingServerHost != null)
                m_wcfStreamingServerHost.Close();
        }
        public void StartWCFStreamingServer(string ipAddress, int port , string storageFolder)
        {
            try
            {
                NetTcpBinding binding = new NetTcpBinding();
                binding.TransferMode = TransferMode.Streamed;
                binding.Security.Mode = SecurityMode.Transport;
                binding.SendTimeout = new TimeSpan(0, 0, 10);
                binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.None;
                var myEndpointAddress = new EndpointAddress("net.tcp://" + ipAddress + ":" + port);
                m_service = new FileRepositoryService("FileRepositoryService", myEndpointAddress);


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
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }

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

        public void StartService(string ipaddress , int port)
        {
            try
            {

                // Create the url that is needed to specify where the service should be started
                urlService = "net.tcp://" + ipaddress + ":" + port + "/MyService";

                // Instruct the ServiceHost that the type that is used is a ServiceLibrary.service1
                host = new ServiceHost(typeof(ServiceLibrary.service1));
                host.Opening += new EventHandler(host_Opening);
                host.Opened += new EventHandler(host_Opened);
                host.Closing += new EventHandler(host_Closing);
                host.Closed += new EventHandler(host_Closed);

                // The binding is where we can choose what transport layer we want to use. HTTP, TCP ect.
                NetTcpBinding tcpBinding = new NetTcpBinding();
                tcpBinding.TransactionFlow = false;
                //tcpBinding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;
                //tcpBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
                tcpBinding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.None;
                tcpBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.None;
                tcpBinding.Security.Mode = SecurityMode.None; // <- Very crucial

                // Add a endpoint
                host.AddServiceEndpoint(typeof(ServiceLibrary.IService1), tcpBinding, urlService);

                // A channel to describe the service. Used with the proxy scvutil.exe tool
                ServiceMetadataBehavior metadataBehavior;
                metadataBehavior = host.Description.Behaviors.Find<ServiceMetadataBehavior>();
                if (metadataBehavior == null)
                {
                    // This is how I create the proxy object that is generated via the svcutil.exe tool
                    metadataBehavior = new ServiceMetadataBehavior();
                    metadataBehavior.HttpGetUrl = new Uri("http://" + ipaddress + ":" + (port + 1) + "/MyService");
                    metadataBehavior.HttpGetEnabled = true;
                    metadataBehavior.ToString();
                    host.Description.Behaviors.Add(metadataBehavior);
                    urlMeta = metadataBehavior.HttpGetUrl.ToString();
                }

                host.Open();
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        public void Close()
        {

            string res = string.Empty;
            if (m_proxy != null)
                res = m_proxy.Close();
            if (host != null)
                host.Close();
            host = null;
        }
        void host_Opening(object sender, EventArgs e)
        {
         
        }
        void host_Closed(object sender, EventArgs e)
        {
             
        }

        void host_Closing(object sender, EventArgs e)
        {
             
        }

        void host_Opened(object sender, EventArgs e)
        {
        }
    }
}