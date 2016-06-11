using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Net.NetworkInformation;
using CompresFiles;
using System.Net;
using System.Net.Sockets;
using System.Net.Mail;
using NATUPNPLib;
using NETCONLib;
using NetFwTypeLib;

namespace WatsonFieldInstaller
{
    using ServiceTools; 
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;

            label1.Text = "Goji WCF Streaming services setup " + Properties.Settings.Default.WatsonInstallerVersion;
            ThreadedExecuter<string> executer = new ThreadedExecuter<string>(ProcessThread_1, ThreadCompleted);
            executer.Start();
        }
        private const string CLSID_FIREWALL_MANAGER =
        "{304CE942-6E39-40D8-943A-B913C40C9CD4}";
        private static NetFwTypeLib.INetFwMgr GetFirewallManager()
        {
            Type objectType = Type.GetTypeFromCLSID(
                  new Guid(CLSID_FIREWALL_MANAGER));
            return Activator.CreateInstance(objectType)
                  as NetFwTypeLib.INetFwMgr;
        }
        // ProgID for the AuthorizedApplication object
        private const string PROGID_AUTHORIZED_APPLICATION =
            "HNetCfg.FwAuthorizedApplication";
        public bool AuthorizeApplication(string title, string applicationPath,
            NET_FW_SCOPE_ scope, NET_FW_IP_VERSION_ ipVersion)
        {
            // Create the type from prog id
            Type type = Type.GetTypeFromProgID(PROGID_AUTHORIZED_APPLICATION);
            INetFwAuthorizedApplication auth = Activator.CreateInstance(type)
                as INetFwAuthorizedApplication;
            auth.Name = title;
            auth.ProcessImageFileName = applicationPath;
            auth.Scope = scope;
            auth.IpVersion = ipVersion;
            auth.Enabled = true;



            INetFwMgr manager = GetFirewallManager();
            try
            {
                manager.LocalPolicy.CurrentProfile.AuthorizedApplications.Add(auth);
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }
        void ThreadCompleted(string s)
        {
            WW_MessageBox(s);
            Close();
        }

        void Failed()
        {
            label1.ForeColor = System.Drawing.Color.Red;
            label1.Text = "Firmware updated failed";
            Thread.Sleep(1000);
            Close();
        }
        public System.Windows.Forms.DialogResult WW_MessageBox(string Message)
        {

            this.TopMost = false;
            System.Windows.Forms.MessageBox.Show(Message,
                                                "Febris installer", System.Windows.Forms.MessageBoxButtons.OK,
                                                 System.Windows.Forms.MessageBoxIcon.None,
                                                 System.Windows.Forms.MessageBoxDefaultButton.Button1,
                                                (System.Windows.Forms.MessageBoxOptions)8192 /*MB_TASKMODAL*/);


            this.TopMost = true;
            return DialogResult.OK;
        }
               
        public IEnumerable<IPAddress> GetIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return (from ip in host.AddressList where !IPAddress.IsLoopback(ip) select ip).ToList();
        }

        void ReplaceIPAddressInConfigFile(string Path , string fileName)
        {
            string ipInfo = getComputerIP().ToString();
            var fileContents = System.IO.File.ReadAllText(Path + "\\" + fileName);
            fileContents = fileContents.Replace("net.tcp://192.168.26.16:5000", "net.tcp://" + "localhost" + ":5000");
            //fileContents = fileContents.Replace("net.tcp://localhost:5000", "net.tcp://" + ipInfo + ":5000");
            System.IO.File.WriteAllText(Path + "\\" + fileName, fileContents);
        }

        IPAddress getComputerIP()
        {

            IEnumerable<IPAddress> iip = GetIpAddress();
            IPAddress ipaddress = null;

            foreach (IPAddress i in iip)
            {
                if (i == null)
                    continue;
                Byte[] bytes = i.GetAddressBytes();
                if (bytes[2] < 20 && bytes[2] > 28)
                    continue;
                if (i.AddressFamily == AddressFamily.InterNetworkV6)
                    continue;
                if (i.AddressFamily == AddressFamily.InterNetwork)
                {
                    if (bytes[2] == 15)
                        continue;
                    ipaddress = i;
                }
            }

            if (ipaddress == null)
            {
                throw (new SystemException("Ip address of this computer is not configure\nMake sure the network cable is connected\nAnd configured"));
            }
            return ipaddress; 
        }
        void DeleteAllFiles()
        {
            try
            {
                string[] files = System.IO.Directory.GetFiles(@"C:\Program Files\Goji Solutions\GojiLTDTrunk\WCF Streaming\GojiWCFStreamingServer\bin\Release");
                for (int i = 0; i < files.Length; i++)
                {
                    System.IO.File.Delete(files[i]);
                }
            }
            catch (Exception err)
            {

            }
        }
        string ProcessThread_1()
        {

            try
            {
                string msg = string.Empty;
                //ServiceInstaller.Uninstall("GojiWCFStreamingServer");
                if (ServiceInstaller.ServiceIsInstalled("GojiWCFStreamingServer") == true)
                {
                    ServiceInstaller.StopService("GojiWCFStreamingServer");
                    label2.Text = "Stop service";
                    for (int i = 0 ; i < 18 ; i++)
                    {
                        if (ServiceInstaller.getStatus("GojiWCFStreamingServer") == "Stopped")
                            break;
                        label2.Text += ".";
                        Thread.Sleep(1000);
                    }
                    if (ServiceInstaller.getStatus("GojiWCFStreamingServer") == "Running")
                    {
                        msg = "Service GojiWCFStreamingServer not stopped";
                        MessageBox.Show(msg);
                        return "ok";
                    }
                }
                DeleteAllFiles();
                Listener listen = new Listener(progressBar1, label2);
                listen.Subscribe();

                FebrisInstaller.Unzip(true);
               // FebrisInstaller.CreateShortcuts(true);
                label2.Text = string.Empty;
                label2.Refresh();

                msg = "Installation endded successfuly";


                //ReplaceIPAddressInConfigFile(@"C:\Program Files\Goji Solutions\GojiLTDTrunk\WCF Streaming\GojiWCFStreamingServer\bin\Release",
                  //                           "GojiWCFStreamingServer.exe.config");

                ///////////////// INSTALL GojiWCFStreamingServer  Service /////////////////////
                if (ServiceInstaller.ServiceIsInstalled("GojiWCFStreamingServer") == false)
                {
                    ServiceInstaller.InstallAndStart("GojiWCFStreamingServer", "Goji WCF Streaming server", @"C:\Program Files\Goji Solutions\GojiLTDTrunk\WCF Streaming\GojiWCFStreamingServer\bin\Release\GojiWCFStreamingServer.exe");
                    label2.Text = "Install and start service";
                    for (int i = 0; i < 8; i++)
                    {
                        if (ServiceInstaller.getStatus("GojiWCFStreamingServer") == "Running")
                            break;
                        label2.Text += ".";
                        Thread.Sleep(1000);
                    }
                    if (ServiceInstaller.getStatus("GojiWCFStreamingServer") != "Running")
                    {
                        msg = "Service GojiWCFStreamingServer did not started";
                    }
                }
                else
                {
                    ServiceInstaller.StartService("GojiWCFStreamingServer");
                    label2.Text = "Start service";
                    for (int i = 0; i < 5; i++)
                    {
                        if (ServiceInstaller.getStatus("GojiWCFStreamingServer") == "Running")
                            break;
                        label2.Text += ".";
                        Thread.Sleep(1000);
                    }
                    if (ServiceInstaller.getStatus("GojiWCFStreamingServer") != "Running")
                    {
                        msg = "Service GojiWCFStreamingServer did not started";
                    }
                }

                AuthorizeApplication("GojiWCFStreamingServer", @"C:\Program Files\Goji Solutions\GojiLTDTrunk\WCF Streaming\GojiWCFStreamingServer\bin\Release\GojiWCFStreamingServer.exe",
                    NET_FW_SCOPE_.NET_FW_SCOPE_ALL,
                    NET_FW_IP_VERSION_.NET_FW_IP_VERSION_ANY);
                /////////////////////////////////////////////////////////


                if (false)
                {
                    label2.Text = "Sensing mail..";
                    string ipInfo = getComputerIP().ToString();
                    SendEmail.Send("elia@gojisolutions.com", "Eli Arad", "Goji WCF Streaming server", msg + Environment.NewLine + ipInfo);
                    label2.Text = "mail sent.";
                }
                return msg;
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }
        public class Listener
        {

            ProgressBar m_pbar;
            int m_fileCount = 0;
            bool m_firstTime = true;
            int m_compressCounter = 0;
            Label m_label;
            public Listener(ProgressBar pbar, Label label)
            {
                m_pbar = pbar;
                m_label = label;
            }
            public void Subscribe()
            {
                FebrisInstaller.Tick += new FebrisInstaller.ZipProgress(HeardIt);
            }
            private void HeardIt(int count, string fileName)
            {

                if (m_firstTime == true)
                {
                    m_fileCount = count;
                    m_pbar.Maximum = count;
                    m_pbar.Value = 0;
                    m_firstTime = false;
                }
                else
                {
                    m_compressCounter += count;
                    m_pbar.Value += count;
                    m_label.Text = fileName;
                    //Console.WriteLine("Adding files {0}, {1}", m_compressCounter, fileName);


                    // progressbar.value++;
                }
            }
        }
    }
}
