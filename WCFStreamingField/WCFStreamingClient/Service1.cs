using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GojiWCFStreamingClient
{
    public partial class Service1 : ServiceBase
    {
        Thread m_thread; 
        private static ServiceHost host = null;
        private static string urlMeta, urlService = "";
        static ServiceLibrary.service1 m_Instance = new ServiceLibrary.service1();
        bool m_running = true;
        string m_ipAddress;
        public Service1()
        {
            InitializeComponent();
        }

        void WaitForIp()
        {
            while (m_running)
            {
                NetCard.NetCard n = new NetCard.NetCard();
                m_ipAddress = n.getComputerIP();
                if (m_ipAddress != "0.0.0.0")
                {
                    OpenService();
                    break;
                }
                Thread.Sleep(3000);
            }
        }

        protected override void OnStart(string[] args)
        {
            m_thread = new Thread(WaitForIp);
            m_thread.Start();            
        }   
        void OpenService()
        {
            try
            {
                int port = 8030;

                //File.AppendAllText("c:\\GojiWCFStreamingClient.txt", "Streaming initializing on address:" + ipaddress + Environment.NewLine);
                // Create the url that is needed to specify where the service should be started
                urlService = "net.tcp://" + m_ipAddress + ":" + port + "/MyService";
                //host = new ServiceHost(typeof(ServiceLibrary.service1));
                host = new ServiceHost(m_Instance, new Uri(urlService));
                host.Opening += new EventHandler(host_Opening);
                host.Opened += new EventHandler(host_Opened);
                host.Closing += new EventHandler(host_Closing);
                host.Closed += new EventHandler(host_Closed);

                // The binding is where we can choose what
                // transport layer we want to use. HTTP, TCP ect.
                NetTcpBinding tcpBinding = new NetTcpBinding();
                tcpBinding.TransactionFlow = false;
                tcpBinding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;

                int x = tcpBinding.MaxConnections;
                tcpBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;


                //tcpBinding.ReceiveTimeout = new TimeSpan(0, 0, 5);
                tcpBinding.SendTimeout = new TimeSpan(0, 0, 5);
                //tcpBinding.OpenTimeout = new TimeSpan(0, 0, 2);
                //tcpBinding.CloseTimeout = new TimeSpan(0, 0, 2);
                tcpBinding.Security.Mode = SecurityMode.Transport;
                // <- Very crucial

                // Add a endpoint
                host.AddServiceEndpoint(typeof(ServiceLibrary.IService), tcpBinding, urlService);

                // A channel to describe the service.
                // Used with the proxy scvutil.exe tool
                ServiceMetadataBehavior metadataBehavior;
                metadataBehavior =
                  host.Description.Behaviors.Find<ServiceMetadataBehavior>();
                if (metadataBehavior == null)
                {
                    // This is how I create the proxy object
                    // that is generated via the svcutil.exe tool
                    metadataBehavior = new ServiceMetadataBehavior();
                    metadataBehavior.HttpGetUrl = new Uri("http://" + m_ipAddress + ":" + (port + 1) + "/MyService");
                    metadataBehavior.HttpGetEnabled = true;
                    metadataBehavior.ToString();
                    host.Description.Behaviors.Add(metadataBehavior);
                    urlMeta = metadataBehavior.HttpGetUrl.ToString();
                }

                host.Open();
                //File.AppendAllText("c:\\GojiWCFStreamingClient.txt", "Streaming client open!" + Environment.NewLine);
            }
            catch (Exception err)
            {
                //File.AppendAllText("c:\\GojiWCFStreamingClient.txt", err.Message + Environment.NewLine);
            }
        }
        static void host_Opening(object sender, EventArgs e)
        {

        }
        static void host_Closed(object sender, EventArgs e)
        {

        }

        static void host_Closing(object sender, EventArgs e)
        {

        }

        static void host_Opened(object sender, EventArgs e)
        {
        }
        protected override void OnStop()
        {
            if (host != null)
            {
                m_running = false;
                if (m_thread != null)
                    m_thread.Join();
                (m_Instance as ServiceLibrary.service1).CloseService();
                host.Close();
            }
        }
    }
}
