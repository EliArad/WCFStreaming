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
using System.IO;
using NetCard;

namespace StreamingFieldInstaller
{
    using ServiceTools;
    using Microsoft.Win32;
    using RegistryClassApi; 
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;

            NetCard.NetCard n = new NetCard.NetCard();
            string ipAddress = n.getComputerIP();
            clsRegistry reg = new clsRegistry();

            if (ipAddress == "0.0.0.0")
            {
                string bip = reg.GetStringValue(Registry.LocalMachine, "SOFTWARE\\Goji solutions\\Field", "IpAddress");
                if (reg.strRegError == null)
                {

                }
                else
                {
                    GetManualIpAddress g = new GetManualIpAddress();
                    if (g.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        ipAddress = g.IpAddress;
                        reg.SetStringValue(Registry.LocalMachine, "SOFTWARE\\Goji solutions\\Field", "IpAddress", ipAddress);
                    }
                }
            }
            label5.Visible = true;
            label5.Text = ipAddress; 
            label1.Text = "Goji MPFM Watson WCF Field installation version " + Properties.Settings.Default.WatsonInstallerVersion;
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
                                                "Streaming Field installer", System.Windows.Forms.MessageBoxButtons.OK,
                                                 System.Windows.Forms.MessageBoxIcon.None,
                                                 System.Windows.Forms.MessageBoxDefaultButton.Button1,
                                                (System.Windows.Forms.MessageBoxOptions)8192 /*MB_TASKMODAL*/);


            this.TopMost = true;
            return DialogResult.OK;
        }
          
        void DeleteOldVersion(string directory)
        {
            try
            {
                if (Directory.Exists(directory) == false)
                    return;
                string[] array1 = Directory.GetFiles(directory);
                foreach (string f in array1)
                {
                    File.Delete(f);
                }
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        static bool processIsRunning(string processName)
        {
            return System.Diagnostics.Process.GetProcesses().Any(p => p.ProcessName.Contains(processName));
        }
        public static void removeProcess(string processName)
        {
            string[] p = new string[1];
            p[0] = processName;
            for (int i = 0; i < p.Length; i++)
            {
                while (processIsRunning(p[i]))
                {
                    foreach (System.Diagnostics.Process proc in System.Diagnostics.Process.GetProcessesByName(p[i]))
                    {
                        proc.Kill();
                        Thread.Sleep(200);
                    }
                    if (processIsRunning(p[i]) == true)
                    {
                        throw (new SystemException("Unable to kill process " + p[i]));
                    }
                }
            }
        } 

        string ProcessThread_1()
        {

            try
            {

                //ServiceInstaller.Uninstall("PhidgetWCFWinService");
                //ServiceInstaller.Uninstall("WatsonWCFWinService");
                //ServiceInstaller.Uninstall("GojiWCFStreamingClient");
                //ServiceInstaller.Uninstall("GojiMPFMWatsonService");

                if (ServiceInstaller.ServiceIsInstalled("GojiWCFStreamingClient") == true)
                {
                    ServiceInstaller.StopService("GojiWCFStreamingClient");
                    for (int i = 0; i < 5; i++)
                    {
                        if (ServiceInstaller.getStatus("GojiWCFStreamingClient") != "Stopped")
                        {
                            label4.Text += ".";
                            Thread.Sleep(1000);
                            continue;
                        }
                        break;
                    }
                }
              
                label4.Visible = true;
                 
                label4.Text = "";
                if (Directory.Exists(@"C:\Program Files\Goji Solutions\GojiLTDTrunk\WCF Streaming\GojiWCFStreamingClient\bin\Release"))
                {
                    try
                    {
                        if (ServiceInstaller.ServiceIsInstalled("GojiWCFStreamingClient") == true)
                            ServiceInstaller.Uninstall("GojiWCFStreamingClient");
                    }
                    catch (Exception err)
                    {
                        throw (new SystemException(err.Message));
                    }
                    try
                    {
                        DeleteOldVersion(@"C:\Program Files\Goji Solutions\GojiLTDTrunk\WCF Streaming\GojiWCFStreamingClient\bin\Release");
                        Directory.Delete(@"C:\Program Files\Goji Solutions\GojiLTDTrunk\WCF Streaming\GojiWCFStreamingClient\bin\Release");
                    }
                    catch (Exception err)
                    {

                    }
                }

                int tryTimes = 15;
                int delayTime = 2500;

                string deleteError = string.Empty;
                label4.Visible = false;
                label4.Text = string.Empty;
                deleteError = string.Empty;
                for (int i = 0; i < tryTimes; i++)
                {
                    try
                    {
                        DeleteOldVersion(@"C:\Program Files\Goji Solutions\GojiLTDTrunk\WCF Streaming\GojiWCFStreamingField\GojiWCFStreamingClient\bin\Release");
                        deleteError = string.Empty;
                        break;
                    }
                    catch (Exception err)
                    {
                        label3.Visible = true;
                        label4.Visible = true;
                        label4.Text += ".";
                        deleteError = err.Message;
                        Refresh();
                        Thread.Sleep(delayTime);
                    }
                }
                if (deleteError != string.Empty)
                {
                    return "Error delete file: " + deleteError;
                }
                label4.Visible = false;
                label4.Text = string.Empty;
                deleteError = "";
                label3.Visible = false;
                Listener listen = new Listener(progressBar1, label2);
                listen.Subscribe();

                FebrisInstaller.Unzip(true);
                FebrisInstaller.CreateShortcuts(true);
                label2.Text = string.Empty;
                label2.Refresh();

                string msg = "Installation endded successfuly";
                ///////////////// INSTALL GojiWCFStreamingClient  Service /////////////////////
                if (ServiceInstaller.ServiceIsInstalled("GojiWCFStreamingClient") == false)
                {
                    ServiceInstaller.InstallAndStart("GojiWCFStreamingClient", "Goji WCF Streaming client", @"C:\Program Files\Goji Solutions\GojiLTDTrunk\WCF Streaming\GojiWCFStreamingField\GojiWCFStreamingClient\bin\Release\GojiWCFStreamingClient.exe");
                    Thread.Sleep(1000);
                    if (ServiceInstaller.getStatus("GojiWCFStreamingClient") != "Running")
                    {
                        msg = "Service GojiWCFStreamingClient did not started";
                    }
                }
                else
                {
                    bool started = false;
                    for (int i = 0; i < 3; i++)
                    {
                        ServiceInstaller.StartService("GojiWCFStreamingClient");
                        Thread.Sleep(1000);
                        if (ServiceInstaller.getStatus("GojiWCFStreamingClient") != "Running")
                        {

                        }
                        else
                        {
                            started = true;
                            break;
                        }
                    }
                    if (started == false)
                        msg = "Service GojiWCFStreamingClient did not started";
                }

                AuthorizeApplication("GojiWCFStreamingClient", @"C:\Program Files\Goji Solutions\GojiLTDTrunk\WCF Streaming\GojiWCFStreamingField\GojiWCFStreamingClient\bin\Release\GojiWCFStreamingClient.exe",
                    NET_FW_SCOPE_.NET_FW_SCOPE_ALL,
                    NET_FW_IP_VERSION_.NET_FW_IP_VERSION_ANY);
                /////////////////////////////////////////////////////////               

                if (false)
                {
                    label2.Text = "Sensing mail..";
                    NetCard.NetCard n = new NetCard.NetCard();
                    string ipInfo = n.getComputerIP();
                    SendEmail.Send("elia@gojisolutions.com", "Eli Arad", "Watson field installer", msg + Environment.NewLine + ipInfo);
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
