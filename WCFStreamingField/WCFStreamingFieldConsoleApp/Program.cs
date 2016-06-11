using QueueBaseFileListApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GojiWCFStreamingFieldConsoleApp
{
    class Program
    {

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(_EventHandler handler, bool add);


        private static ServiceHost host = null;
        private static string urlMeta, urlService = "";
        static bool m_open = false;
        private delegate bool _EventHandler(CtrlType sig);
        static _EventHandler _handler;

        static ServiceLibrary.service1 m_Instance = new ServiceLibrary.service1();

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool Handler(CtrlType sig)
        {
            switch (sig)
            {
                case CtrlType.CTRL_C_EVENT:
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                case CtrlType.CTRL_CLOSE_EVENT:
                    if (m_open == true)
                        host.Close();
                    Thread.Sleep(1000);
                    return false;
                default:
                    return false;
            }
        }

        static void Main(string[] args)
        {

            try
            {
                /*
                 // test code for the queue...
                QueueBaseFileListWriter q = new QueueBaseFileListWriter(10, "writeList\\");
                QueueBaseFileListReader q1 = new QueueBaseFileListReader(10, "readList\\", "writeList\\");
                ulong listNum;
                ulong fileNumber;
                bool copyStatus;
                for (int i = 0; i < 20; i++)
                {
                    q.AddFile("eee");
                    Console.WriteLine(q1.GetFile(out listNum, out fileNumber, out copyStatus));
                    //q1.MarkAsSent();
                }
                */
                _handler += new _EventHandler(Handler);
                SetConsoleCtrlHandler(_handler, true);

                int port = 8030;
                NetCard.NetCard n = new NetCard.NetCard();
                string ipaddress = n.getComputerIP();
                Console.WriteLine("Listening on ip address: " + ipaddress);
                // Create the url that is needed to specify where the service should be started
                urlService = "net.tcp://" + ipaddress + ":" + port + "/MyService";
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
                tcpBinding.OpenTimeout = new TimeSpan(0, 0, 10);
                tcpBinding.CloseTimeout = new TimeSpan(0, 0, 2);
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
                    metadataBehavior.HttpGetUrl = new Uri("http://" + ipaddress + ":" + (port + 1) + "/MyService");
                    metadataBehavior.HttpGetEnabled = true;
                    metadataBehavior.ToString();
                    host.Description.Behaviors.Add(metadataBehavior);
                    urlMeta = metadataBehavior.HttpGetUrl.ToString();
                }

                host.Open();
                m_open = true;
                Console.WriteLine("WCF Streaming Client on field Service is opened");
                Console.ReadLine();
                (m_Instance as ServiceLibrary.service1).CloseService();
                host.Close();
                m_open = false;
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
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
 
    }
}
