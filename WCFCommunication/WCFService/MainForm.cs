using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Description;


namespace WCFService
{
    public partial class MainForm : Form
    {
        private ServiceHost host = null;
        private string urlMeta, urlService = "";
        
        public MainForm()
        {
            InitializeComponent();           
            Append("Program started ...");
        }
       
        void host_Opening(object sender, EventArgs e)
        {
            Append("Service opening ... Stand by");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // Returns a list of ipaddress configuration
                IPHostEntry ips = Dns.GetHostEntry(Dns.GetHostName());

                // Select the first entry. I hope it's this maschines IP
                IPAddress _ipAddress = ips.AddressList[0];

                string ipaddress = "192.168.26.16";
                // Create the url that is needed to specify where the service should be started
                urlService = "net.tcp://" + ipaddress + ":8030/MyService";

                // Instruct the ServiceHost that the type that is used is a ServiceLibrary.service1
                host = new ServiceHost(typeof(ServiceLibrary.service1));
                host.Opening += new EventHandler(host_Opening);
                host.Opened += new EventHandler(host_Opened);
                host.Closing += new EventHandler(host_Closing);
                host.Closed += new EventHandler(host_Closed);


                // The binding is where we can choose what transport layer we want to use. HTTP, TCP ect.
                NetTcpBinding tcpBinding = new NetTcpBinding();
                tcpBinding.TransactionFlow = false;
                tcpBinding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;
                tcpBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
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
                    metadataBehavior.HttpGetUrl = new Uri("http://" + ipaddress + ":8001/MyService");
                    metadataBehavior.HttpGetEnabled = true;
                    metadataBehavior.ToString();
                    host.Description.Behaviors.Add(metadataBehavior);
                    urlMeta = metadataBehavior.HttpGetUrl.ToString();
                    
                }

                host.Open();
            }
            catch (Exception ex1)
            {
                Append(ex1.StackTrace);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            host.Close();
        }

        void host_Closed(object sender, EventArgs e)
        {
            Append("Service closed");
        }

        void host_Closing(object sender, EventArgs e)
        {
            Append("Service closing ... stand by");
        }

        void host_Opened(object sender, EventArgs e)
        {
            Append("Service opened.");
            Append("Service URL:\t" + urlService);
            Append("Meta URL:\t" + urlMeta + " (Not that relevant)");
            Append("Waiting for clients...");
        }

        private void Append(string str)
        {
            textBox1.AppendText("\r\n" + str);
        }
    }
}