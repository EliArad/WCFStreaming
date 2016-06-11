using FastDirectoryEnumeratorLib;
using GojiWCFStreamingBaseApi;
using Microsoft.Win32;
using NetworkDrivesApi;
using RegistryClassApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OnFieldStorageManagmentApp
{
    public partial class Form1 : Form
    {
        string m_baseGuid;
        bool m_syncFileEnded = false;
        TimeSpan m_time = new TimeSpan(0, 0, 0);
        GojiWCFStreamingBase.ClientCallbackMessage p;
        GojiWCFStreamingBase.ServerCallbackMessage p1;
        GojiWCFStreamingBase m_client;
        bool m_running;
        bool m_timerThreadRunning = false;
        EventWaitHandle m_eventDeleteEveryTime = new AutoResetEvent(false);
        bool m_connecting = false;
        Thread m_connectThread;
        Thread m_timeThread;
        int m_totalFieldFiles = 0;
        int m_totalWCFBaseFiles = 0;
        int m_totalDropBoxBaseFiles = 0;
        Task m_taskEveryNTime = null;
        bool m_taskRunning = false;
        string m_password;
        public Form1()
        {
            m_running = true;
            InitializeComponent();
            label18.Text = m_totalFieldFiles.ToString();
            Control.CheckForIllegalCrossThreadCalls = false;
          
            groupBox3.Enabled = false;
            p = new GojiWCFStreamingBase.ClientCallbackMessage(ClientCallbackMsg);
            p1 = new GojiWCFStreamingBase.ServerCallbackMessage(ServerCallbackMsg);
            GojiWCFStreamingBase.SetServerCallback = p1;
            m_client = new GojiWCFStreamingBase(p);

            textBox1.Text = Properties.Settings.Default.FieldIpAddress;
            textBox7.Text = Properties.Settings.Default.FieldComputerUserName;
            textBox6.Text = Properties.Settings.Default.FieldComputerPassword;

            textBox4.Text = Properties.Settings.Default.StorageServerUserName;
            textBox5.Text = Properties.Settings.Default.StorageServerPassword;
            textBox2.Text = Properties.Settings.Default.FieldDirectoryToMinotor;
            textBox3.Text = Properties.Settings.Default.StorageDirectoryToUpload;
            comboBox1.SelectedIndex = Properties.Settings.Default.UploadMode;
            textBox10.Text = Properties.Settings.Default.DeleteAllUploadedFilesTimer;
            textBox12.Text = Properties.Settings.Default.uploadWrongFilesDirectory;
            textBox13.Text = Properties.Settings.Default.SyncDirectory;
            checkBox4.Checked = Properties.Settings.Default.RecoursiveSync;
            textBox14.Text = Properties.Settings.Default.OperatorUserName;
            checkBox6.Checked = Properties.Settings.Default.OperationsByUser;
            button14.Enabled = false;

            try
            {
                MapNetworkDrives();
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }


            NetCard.NetCard n = new NetCard.NetCard();
            string baseIp = n.getComputerIP();
            if (baseIp == "0.0.0.0")
            {
                clsRegistry reg = new clsRegistry();
                string bip = reg.GetStringValue(Registry.LocalMachine, "SOFTWARE\\Goji solutions\\Base", "IpAddress");
                if (reg.strRegError == null)
                {
                    baseIp = bip;
                    this.Text = "Goji Field uploaded managment " + baseIp;
                }
                else
                {
                    MessageBox.Show("You need to specify the base ip address in the registry");
                    GetManualIpAddress g = new GetManualIpAddress();
                    g.ShowDialog();
                    if (g.DialogResult == System.Windows.Forms.DialogResult.OK)
                    {
                        baseIp = g.IpAddress;
                        this.Text = "Goji Field uploaded managment " + baseIp;
                        reg.SetStringValue(Registry.LocalMachine, "SOFTWARE\\Goji solutions\\Base", "IpAddress" , baseIp);
                    }
                    return;
                }
            }
            else
            {
                this.Text = "Goji Field uploaded managment " + baseIp;
            }
        }

        void MapNetworkDrives()
        {
            if (NetworkDrives.MapDrive("M", @"\\192.168.21.7\delonfriday", @"rf-dynamics\elia", "elianat4414123") == false)
            {
                throw (new SystemException("Cannot map M drive"));
            }
            else
            {
                
            }

            if (NetworkDrives.MapDrive("R", @"\\192.168.21.7\Data", @"rf-dynamics\elia", "elianat4414123") == false)
            {
                throw (new SystemException("Cannot map R drive"));
            }
            else
            {
               
            }
        }
        void TransferWrongFiles(string source , string dest)
        {
            while (true)
            {
                try
                {
                    File.Move(source, dest);
                    return;
                }
                catch (Exception err)
                {
                    if (err.Message.Contains("Could not find") == true)
                        return;
                    Console.WriteLine(err.Message);
                    Thread.Sleep(5000);
                }
            }
        }
        void ClientCallbackMsg(string fieldGuid, string ipAddress, int code, string msg, DateTime startTime)
        {
            string[] s = msg.Split(new Char[] { ',' });
            switch (code)
            {
                case 18:
                    m_totalFieldFiles += 1;
                    label18.Text = m_totalFieldFiles.ToString();
                    string str = String.Format("From IP: {0} msg arrived: {1}", ipAddress, msg, startTime.ToString());
                    string fileName = s[2];
                    if ((fileName.ToLower().Contains(".wrong") == true) && checkBox3.Checked == true)
                    {
                        try
                        {
                            if (Directory.Exists(textBox12.Text) == false)
                                Directory.CreateDirectory(textBox12.Text);

                            string dest = textBox12.Text + "\\" + Path.GetFileName(fileName);
                            var t = new Thread(() => TransferWrongFiles(fileName, dest));
                            t.Start();
                        }
                        catch (Exception err)
                        {
                            MessageBox.Show(err.Message);
                        }
                    }
                    label15.Text = str;
                    break;
                case 633:
                    File.AppendAllText("DropBoxListFiles.txt", msg + Environment.NewLine);
                    m_totalDropBoxBaseFiles += 1;
                    label19.Text = m_totalDropBoxBaseFiles.ToString();
                    string str2 = string.Format("File upload\t{0}\t startTime: {1} Uploaded at:{2}", s[2], s[1], DateTime.Now);
                    label16.Text = str2;
                break;
                case 533:
                    MessageBox.Show("Drop box upload failed for file: " + s[3]);
                break;
                case 133:
                    m_syncFileEnded = true;
                    MessageBox.Show("Number of sync files added: " + s[1]);
                break;
                case 77:
                    groupBox3.Enabled = false;
                    tabControl1.Enabled = false;
                    InitializeClient();
                break;
                case 911:
                    MessageBox.Show(ipAddress + ": Drop box initialized: " + msg);
                break;
                case 912:
                    MessageBox.Show(ipAddress + ": Drop box is already initialized");
                break;
                case 200:
                {
                    string str1 = ipAddress + ": " + msg;
                    MessageBox.Show(str1);
                }
                break;
            }
        }
        void InitializeClient()
        {
            m_connecting = true;
            while (m_connecting == true)
            {
                try
                {
                    m_client.ConnectStreamingFieldClient(textBox1.Text, 8030, textBox7.Text, textBox6.Text);                   
                    groupBox3.Enabled = true;
                    tabControl1.Enabled = true;
                    break;
                }
                catch (Exception err)
                {
                    Thread.Sleep(1000);
                }
            }
            string m_guidForStreamingClient = "02711232ed42119ebacd00a100574918";
            m_client.Register(m_guidForStreamingClient);

            LoadSettings();

            int x = m_client.GetFifoThreshold();
            textBox11.Text = x.ToString();

            TimeSpan t = m_client.GetCopyThreadTimeEvent();
            textBox8.Text = t.ToString();
        }
        void ServerCallbackMsg(string fieldGuid, string ipAddress, int code, string msg, DateTime startTime, ulong g360Index, string fileOwnerUserName, long sizeOfFile)
        {
            string[] s = msg.Split(new Char[] { ',' });
            if (code == 400)
            {
                m_totalWCFBaseFiles += 1;
                label19.Text = m_totalWCFBaseFiles.ToString();
                string str = string.Format("File upload\t{0}\t startTime: {1} Uploaded at:{2}  size of file: {3}", s[0], s[1], DateTime.Now, fileOwnerUserName, sizeOfFile);
                label16.Text = str;
            }
            //Console.WriteLine("code: {0} ipAddress {1}  msg {2}", code, ipAddress, msg);  
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (m_client.IsConnected == false)
            {
                MessageBox.Show("You are not connected to streaming field server");
                return;
            }
            if (checkBox6.Checked == true && textBox14.Text == string.Empty)
            {
                MessageBox.Show("Please specify user name for the operations");
                return;
            }
            PasswordForm p = new PasswordForm();
            p.ShowDialog();
            if (p.DialogResult  == System.Windows.Forms.DialogResult.OK)
            {
                m_client.DeleteAllWatsonGenerateFilesOnField(p.GetPassword());
            }
        }
        void StartUploadAllFilesTask()
        {
            if (checkBox2.Checked == true)
            {
                if (m_taskEveryNTime == null)
                {
                    m_taskEveryNTime = new Task(() => { DeleteAllUploadedFilesEvery(); });
                    m_taskEveryNTime.Start();
                }
            }
        }
        void TimerThread()
        {
            m_time = new TimeSpan(0, 0, 0);
            TimeSpan t1 = new TimeSpan(0, 0, 1);
                 
            m_timerThreadRunning = true;
            while (m_timerThreadRunning)
            {
                Thread.Sleep(1000);
                m_time = m_time + t1;
                label23.Text = m_time.ToString();
            }
        }
        void DeleteAllUploadedFilesEvery()
        {
            if (checkBox6.Checked == true && textBox14.Text == string.Empty)
            {
                MessageBox.Show("Please specify user name for the operations");
                return;
            }

            m_taskRunning = true;
            m_eventDeleteEveryTime.Reset();
            string[] s = textBox10.Text.Split(new Char[] { ':' });
            TimeSpan time = new TimeSpan(0,0,0);
            if (s.Length == 3)
            {
                time = new TimeSpan(int.Parse(s[0]), int.Parse(s[1]), int.Parse(s[2]));
            } 
            else if (s.Length == 4)
            {
                time = new TimeSpan(int.Parse(s[0]), int.Parse(s[1]), int.Parse(s[2]), int.Parse(s[3]));
            }
            else
            {
                MessageBox.Show("Invalid time format:\nHH:MM:SS  or d:HH:MM:SS");
                return;
            }
            m_timeThread = new Thread(TimerThread);
            m_timeThread.Start();
            while (m_taskRunning == true)
            {
                m_eventDeleteEveryTime.WaitOne(time);
                if (m_taskRunning == false)
                {
                    m_timerThreadRunning = false;
                    m_timeThread.Join();
                    return;
                }
                try
                {
                    int num = m_client.DeleteAllUploadedFilesOnServer(m_password, textBox14.Text, checkBox6.Checked);
                    label22.Text = "Total files deleted: " + num.ToString();
                    m_time = new TimeSpan(0, 0, 0);
                }
                catch (Exception err)
                {
                    label22.Text = "Not uploaded due error";
                    m_time = new TimeSpan(0, 0, 0);
                }
            }
            m_timerThreadRunning = false;
            m_timeThread.Join();
        }
        void LoadSettings()
        {


            textBox8.Text = Properties.Settings.Default.TriggeTime;
            checkBox8.Checked = Properties.Settings.Default.AddDateToUploadDir;
            checkBox3.Checked = Properties.Settings.Default.StartWatchWrongFiles;
            checkBox2.Checked = Properties.Settings.Default.DeleteAllUploadedFilesEvery;
            textBox14.Text = Properties.Settings.Default.OperatorUserName;
            checkBox6.Checked = Properties.Settings.Default.OperationsByUser;
        }
        void SaveSettings()
        {
            Properties.Settings.Default.TriggeTime = textBox8.Text;
            Properties.Settings.Default.AddDateToUploadDir = checkBox8.Checked;
            Properties.Settings.Default.OperationsByUser = checkBox6.Checked;
            Properties.Settings.Default.OperatorUserName = textBox14.Text;
            Properties.Settings.Default.StartWatchWrongFiles = checkBox3.Checked;
            Properties.Settings.Default.DeleteAllUploadedFilesTimer = textBox10.Text;
            Properties.Settings.Default.DeleteAllUploadedFilesEvery = checkBox2.Checked;
            Properties.Settings.Default.FieldIpAddress = textBox1.Text;
            Properties.Settings.Default.FieldComputerUserName = textBox7.Text;
            Properties.Settings.Default.FieldComputerPassword = textBox6.Text;
            Properties.Settings.Default.StorageServerUserName = textBox4.Text;
            Properties.Settings.Default.StorageServerPassword = textBox5.Text;
            Properties.Settings.Default.FieldDirectoryToMinotor = textBox2.Text;
            Properties.Settings.Default.StorageDirectoryToUpload = textBox3.Text;
            Properties.Settings.Default.UploadMode = comboBox1.SelectedIndex;
            Properties.Settings.Default.uploadWrongFilesDirectory = textBox12.Text;
            Properties.Settings.Default.SyncDirectory = textBox13.Text;
            Properties.Settings.Default.RecoursiveSync = checkBox4.Checked;

            Properties.Settings.Default.Save();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_running = false;
            try
            {
                if (m_client != null)
                {
                    m_client.CloseTCPStreamingServer();
                }
                m_timerThreadRunning = false;
                m_timeThread.Join();
                m_taskRunning = false;
                m_eventDeleteEveryTime.Set();
                m_taskEveryNTime.Wait();
                m_taskEveryNTime = null;

                if (m_client.IsConnected == true)
                {
                    m_client.CloseDropBoxConnection();
                    m_client.CloseClient();
                }
            }
            catch (Exception er)
            {

            }
            SaveSettings();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == string.Empty)
            {
                MessageBox.Show("Please set ip address of the field");
                return; 
            }

            IPAddress ipAddress = null;
            bool isValidIp = System.Net.IPAddress.TryParse(textBox1.Text, out ipAddress);
            if (isValidIp == false)
            {
                MessageBox.Show("Invalid IP address format");
            }

            if (m_connectThread != null)
            {
                m_running = false;
                m_connectThread.Join();
            }

            m_connectThread = new Thread(InitializeClient);
            m_connectThread.Start();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (m_client.IsConnected == false)
            {
                MessageBox.Show("You are not connected to streaming field server");
                return;
            }
            if (textBox14.Text == string.Empty)
            {
                MessageBox.Show("Operator user name was not supplied");
                return;
            }
            m_client.SetWatsonRunning(checkBox7.Checked);
            string res = m_client.StartWatch(textBox2.Text, "*.bin", true, textBox14.Text);
            string[] s = res.Split(new Char[] { ',' });
            if (s[0] == "start watch ok on path")
            {
                button5.ForeColor = Color.Green;
                button6.ForeColor = Color.Gray;
            }
            else
            {
                button6.ForeColor = Color.Gray;
                MessageBox.Show(res);
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == -1)
            {
                MessageBox.Show("please select upload type");
                return;            
            }

            clsRegistry reg = new clsRegistry();
            m_baseGuid = reg.GetStringValue(Registry.LocalMachine, "SOFTWARE\\Goji solutions\\Base", "Guid");
            if (reg.strRegError != null)
            {
                m_baseGuid = Guid.NewGuid().ToString();
                reg.SetStringValue(Registry.LocalMachine, "SOFTWARE\\Goji solutions\\Base", "Guid", m_baseGuid);
            }
            string guid = m_client.GetClientGuid();

            NetCard.NetCard n = new NetCard.NetCard();
            string baseIp = n.getComputerIP();

            if (baseIp == "0.0.0.0")
            {
                string bip = reg.GetStringValue(Registry.LocalMachine, "SOFTWARE\\Goji solutions\\Base", "IpAddress");
                if (reg.strRegError == null)
                {
                    baseIp = bip;
                }
                else
                {
                    MessageBox.Show("You need to specify the base ip address in the registry");
                    return;
                }
            }

            if (checkBox1.Checked == true)
            {
                if (comboBox1.SelectedIndex == 0)
                {
                    try
                    { 

                        Dictionary<string, string> storageDirectoryDic = new Dictionary<string, string>();
                        storageDirectoryDic.Add(guid, textBox3.Text);
                        GojiWCFStreamingBase.StartStorageServer(storageDirectoryDic);

                       
                        if (SetCopyTimerEvent() == false)
                            return;
                        m_client.AddDateToUploadTargetFolder(checkBox8.Checked);
                        m_client.ConnectToStreamingServer(baseIp, textBox4.Text, textBox5.Text);
                        button14.Enabled = true;
                    }
                    catch (Exception err)
                    {
                        MessageBox.Show(err.Message);
                    }
                }
                else if (comboBox1.SelectedIndex == 1)
                {
                    try
                    {
                        string s = m_client.InitializeDropBox();
                        if (s != "ok")
                        {
                            MessageBox.Show(s);
                        }
                        else
                        {
                            m_client.StartDropBoxUploadMode(true);
                            button14.Enabled = true;
                        }
                    }
                    catch (Exception err)
                    {
                        MessageBox.Show(err.Message);
                    }
                }
                else if (comboBox1.SelectedIndex == 2)
                {
                    m_client.StartTCPStreamingServer(1024 * 1024 * 30 , 5005);
                    m_client.OpenTCPStreamingClient(baseIp, textBox4.Text, textBox5.Text);

                }
                checkBox1.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                if (comboBox1.SelectedIndex == 0)
                {
                    m_client.CloseCopyThread();
                    while (m_client.IsCopyThreadIsAlive() == true)
                    {
                        Application.DoEvents();
                        Thread.Sleep(1000);
                    }
                }
                else
                {
                    try
                    {
                        m_client.StartDropBoxUploadMode(false);
                    }
                    catch (Exception err)
                    {
                        MessageBox.Show(err.Message);
                    }
                }
                checkBox1.ForeColor = System.Drawing.Color.Red;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (m_client.IsConnected == false)
            {
                MessageBox.Show("You are not connected to streaming field server");
                return;
            }
            m_client.StopWatch();
            button6.ForeColor = Color.Green;
            button5.ForeColor = Color.Gray;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                if (m_client.IsConnected == false)
                {
                    MessageBox.Show("You are not connected to streaming field server");
                    return;
                }
                if (checkBox6.Checked == true && textBox14.Text == string.Empty)
                {
                    MessageBox.Show("Please specify user name for the operations");
                    return;
                }
                string stat = m_client.GetStatistics(textBox14.Text , checkBox6.Checked);
                string[] s = stat.Split(new Char[] { ',' });
                label8.Text = "waitingToUpload: " + int.Parse(s[0]);
                label9.Text = "numberUploadedAlready: " + s[1];
                label10.Text = "totalStorageSize: " + (double.Parse(s[2]) / (1024.0 * 1024.0)).ToString("0.0");
                label11.Text = "totalStorageWaitingToUploadSize: " + (double.Parse(s[3]) / (1024.0 * 1024.0)).ToString("0.0");
                label12.Text = "totalStorageUploadedSize: " + (double.Parse(s[4]) / (1024.0 * 1024.0)).ToString("0.0");
                label13.Text = "waitingToUploadDoesNotExists: " + s[5];

                label20.Text = "num uploaded Still exist on field: " + s[6];
                label21.Text = "num uploaded not exist on field: " + s[7];
                label25.Text = s[8];
                label1.Text = "Number of uploaded still exists: " + s[9];


            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (m_client.IsConnected == false)
            {
                MessageBox.Show("You are not connected to streaming field server");
                return;
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            DialogResult d = MessageBox.Show("Are you sure you want to clear the fifo manager?" , "Storage management tool" , MessageBoxButtons.YesNo);
            if (d == System.Windows.Forms.DialogResult.Yes)
            {
                m_client.CleanFifoHandler();
            }
        }

        bool SetCopyTimerEvent()
        {
            try
            {
                string t = textBox8.Text;
                string[] s = t.Split(new Char[] { ':' });
                if (s.Length != 3)
                {
                    MessageBox.Show("The format is a time format of h:m:s");
                    return false;
                }
                int hour, miniute, sec;
                hour = int.Parse(s[0]);
                miniute = int.Parse(s[1]);
                sec = int.Parse(s[2]);
                m_client.SetCopyThreadTimeEvent(new TimeSpan(hour, miniute, sec));

                TimeSpan t1 = m_client.GetCopyThreadTimeEvent();
                textBox8.Text = t1.ToString();
                return true;
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
                return false;
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            SetCopyTimerEvent();
        }

        private void button12_Click(object sender, EventArgs e)
        {
            try
            {
                if (m_client.IsConnected == false)
                {
                    MessageBox.Show("You are not connected to streaming field server");
                    return;
                }
                if (checkBox6.Checked == true && textBox14.Text == string.Empty)
                {
                    MessageBox.Show("Please specify user name for the operations");
                    return;
                }
                PasswordForm p = new PasswordForm();
                p.ShowDialog();
                if (p.DialogResult == System.Windows.Forms.DialogResult.OK)
                {

                    /*
                    dateTimePicker1.Format = DateTimePickerFormat.Custom;
                    dateTimePicker1.CustomFormat = "MM dd yyyy hh mm ss";

                    dateTimePicker2.Format = DateTimePickerFormat.Custom;
                    dateTimePicker2.CustomFormat = "MM dd yyyy hh mm ss"; 
                    */
                    string res = m_client.DeleteAllFilesFromFieldBetweenDates(dateTimePicker1.Value, dateTimePicker2.Value, false, p.GetPassword(), textBox14.Text , checkBox6.Checked);
                    string[] s = res.Split(new Char[] { ',' });
                    if (s[0] != "ok")
                    {
                        MessageBox.Show(s[0]);
                    }
                    else
                    {
                        MessageBox.Show(s[1] + " files were deleted");
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private void tabPage2_Click(object sender, EventArgs e)
        {

        }

        private void button14_Click(object sender, EventArgs e)
        {
            try
            {
                if (m_client.IsConnected == false)
                {
                    MessageBox.Show("You are not connected to streaming field server");
                    return;
                }
                string res = m_client.InitiateSingleUpload();
                MessageBox.Show(res);
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }
        private void button13_Click(object sender, EventArgs e)
        {
            if (m_client.IsConnected == false)
            {
                MessageBox.Show("You are not connected to streaming field server");
                return;
            }
        }

        private void viewDropBoxFilesListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DropBoxFileList d = new DropBoxFileList();
            d.ShowDialog();

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked == false)
            {
                m_taskRunning = false;
                m_eventDeleteEveryTime.Set();
                if (m_taskEveryNTime != null)
                    m_taskEveryNTime.Wait();
                m_taskEveryNTime = null;
            }
            else
            {

                m_taskRunning = false;
                m_eventDeleteEveryTime.Set();
                if (m_taskEveryNTime != null)
                    m_taskEveryNTime.Wait();
                m_taskEveryNTime = null;

                PasswordForm p = new PasswordForm();
                p.ShowDialog();
                if (p.DialogResult == System.Windows.Forms.DialogResult.OK)
                {
                    m_password = p.GetPassword();
                    if (m_password != "123456")
                    {
                        MessageBox.Show("Incorrect password for delete operation");
                        checkBox2.Checked = false;
                    }
                    else
                    {
                        if (m_taskEveryNTime == null)
                        {
                            m_taskEveryNTime = new Task(() => { DeleteAllUploadedFilesEvery(); });
                            m_taskEveryNTime.Start();
                        }
                    }
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (m_client.IsConnected == false)
            {
                MessageBox.Show("You are not connected to streaming field server");
                return;
            }
            if (checkBox6.Checked == true && textBox14.Text == string.Empty)
            {
                MessageBox.Show("Please specify user name for the operations");
                return;
            }
            PasswordForm p = new PasswordForm();
            p.ShowDialog();
            if (p.DialogResult == System.Windows.Forms.DialogResult.OK)
            {
                m_client.DeleteAllUploadedFilesOnServer(p.GetPassword(), textBox14.Text, checkBox6.Checked);
            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            try
            {
                int x = int.Parse(textBox11.Text);
                m_client.SetFifoThreshold(x);
                x = m_client.GetFifoThreshold();
                textBox11.Text = x.ToString();
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (checkBox3.Checked == true)
                {
                    m_client.StartWatchWrongFiles(textBox2.Text, true);
                }
                else
                {
                    m_client.StopWatchWrongFiles();
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private void button17_Click(object sender, EventArgs e)
        {
            try
            {
                
                if (textBox14.Text == string.Empty)
                {
                    MessageBox.Show("Please specify user name for the operations");
                    return;
                }
                if (checkBox5.Checked == true)
                {
                    if (Directory.Exists(textBox13.Text) == false)
                    {
                        MessageBox.Show("Wrong directory to sync is not set");
                        return;
                    }
                    int count = 0;
                    FileData[] files = FastDirectoryEnumerator.GetFiles(textBox2.Text,
                                                                       "*.wrong",
                                                                       checkBox4.Checked == true ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                    for (int i = 0; i < files.Length; i++)
                    {
                        try
                        {
                            File.Move(files[i].Path, textBox12.Text + "\\" + files[i].Name);
                            count++;
                        }
                        catch (Exception exx)
                        {
                            MessageBox.Show(exx.Message);
                        }
                    }
                    MessageBox.Show("Total wrong files moved to " + textBox12.Text + " are " + count);
                }
                int res = m_client.SyncFilesWithDataBase(textBox13.Text, checkBox4.Checked, textBox14.Text , 0);
                if (res == 0)
                {
                    MessageBox.Show("Sync files is in progress");
                }
                else
                {                                       
                    while (m_syncFileEnded == false)
                    {
                        Thread.Sleep(500);
                    }
                    m_syncFileEnded = false; 
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(textBox3.Text);
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(textBox2.Text);
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(textBox13.Text);
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox5.Checked == true)
            {
                if (Directory.Exists(textBox12.Text) == false)
                {
                    MessageBox.Show("Wrong files directory is not set");
                    checkBox5.Checked = false;
                }
            }
        }

        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            About a = new About();
            a.ShowDialog();
        }

        private void saveSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox6.Checked == true && textBox14.Text == string.Empty)
            {
                MessageBox.Show("Please specify user name for the operations");
                return;
            }
        }

        private void button18_Click(object sender, EventArgs e)
        {
            if (m_client.IsConnected == false)
            {
                MessageBox.Show("You are not connected to streaming field server");
                return;
            }
            if (checkBox6.Checked == true && textBox14.Text == string.Empty)
            {
                MessageBox.Show("Please specify user name for the operations");
                return;
            }
            PasswordForm p = new PasswordForm();
            p.ShowDialog();
            if (p.DialogResult == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    TimeSpan t;
                    TimeSpan.TryParse(textBox15.Text, out t);
                    DateTime d = DateTime.Now - t;
                    MessageBox.Show(m_client.DeleteAllWatsonGenerateFilesTimeSpanBeforeNow(d, textBox14.Text, checkBox6.Checked));
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.Message);
                }
            }
        }

        private void button19_Click(object sender, EventArgs e)
        {
            if (m_client.IsConnected == false)
            {
                MessageBox.Show("You are not connected to streaming field server");
                return;
            }
            if (checkBox6.Checked == true && textBox14.Text == string.Empty)
            {
                MessageBox.Show("Please specify user name for the operations");
                return;
            }
            PasswordForm p = new PasswordForm();
            p.ShowDialog();
            if (p.DialogResult == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    TimeSpan t;
                    TimeSpan.TryParse(textBox16.Text, out t);
                    DateTime d = DateTime.Now - t;
                    MessageBox.Show(m_client.DeleteAllUploadedFilesTimeSpanBeforeNow(d, textBox14.Text, checkBox6.Checked));
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.Message);
                }
            }
        }
         
        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            bool c = false;
            try
            {
                c = checkBox8.Checked;
                m_client.AddDateToUploadTargetFolder(checkBox8.Checked);
            }
            catch (Exception err)
            {
                checkBox8.Checked = c;
            }
        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == -1)
            {
                MessageBox.Show("please select upload type");
                return;
            }
            if (checkBox1.Checked == true)
            {
                if (comboBox1.SelectedIndex == 0)
                {
                    try
                    {

                        clsRegistry reg = new clsRegistry();
                        m_baseGuid = reg.GetStringValue(Registry.LocalMachine, "SOFTWARE\\Goji solutions\\Base", "Guid");
                        if (reg.strRegError != null)
                        {
                            m_baseGuid = Guid.NewGuid().ToString();
                            reg.SetStringValue(Registry.LocalMachine, "SOFTWARE\\Goji solutions\\Base", "Guid", m_baseGuid);
                        }

                        string guid = m_client.GetClientGuid();
                        m_client.StartTCPStreamingServer(1024 * 1024 * 10, 5005);

                        NetCard.NetCard n = new NetCard.NetCard();
                        string baseIp = n.getComputerIP();
                        if (baseIp == "0.0.0.0")
                        {
                            string bip = reg.GetStringValue(Registry.LocalMachine, "SOFTWARE\\Goji solutions\\Base", "IpAddress");
                            if (reg.strRegError == null)
                            {
                                baseIp = bip;
                            }
                            else
                            {
                                MessageBox.Show("You need to specify the base ip address in the registry");
                                return;
                            }
                        }
                        if (SetCopyTimerEvent() == false)
                            return;
                        m_client.OpenTCPStreamingClient(baseIp, textBox4.Text, textBox5.Text);
                        button14.Enabled = true;
                    }
                    catch (Exception err)
                    {
                        MessageBox.Show(err.Message);
                    }
                }
                else
                {
                     
                }
                checkBox1.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                
                checkBox1.ForeColor = System.Drawing.Color.Red;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboBox1.SelectedIndex)
            {
                case 0:
                    checkBox1.Text = "WCF Upload";
                break;
                case 1:
                    checkBox1.Text = "Dropbox Upload";
                break;
                case 2:
                    checkBox1.Text = "TCP Upload";
                break;
            }
        }

        private void linkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                DriveInfo[] d = m_client.GetDrivesInfo();
                for (int i = 0; i < d.Length; i++)
                {
                    if (d[i].Name == @"C:\")
                    {
                        label32.Text = (d[i].TotalSize / (1024 * 1024)).ToString();
                        label33.Text = (d[i].AvailableFreeSpace / (1024 * 1024)).ToString();
                        continue;
                    }
                    if (d[i].Name == @"D:\")
                    {
                        label34.Text = (d[i].TotalSize / (1024 * 1024)).ToString();
                        label27.Text = (d[i].AvailableFreeSpace / (1024 * 1024)).ToString();
                    }
                }
            }
            catch (Exception er)
            {
                MessageBox.Show(er.Message);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (m_client.IsConnected == false)
            {
                MessageBox.Show("You are not connected to streaming field server");
                return;
            }
            if (checkBox6.Checked == true && textBox14.Text == string.Empty)
            {
                MessageBox.Show("Please specify user name for the operations");
                return;
            }
            PasswordForm p = new PasswordForm();
            p.ShowDialog();
            if (p.DialogResult == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    int t;
                    int.TryParse(textBox17.Text, out t);
                    DateTime d = DateTime.Now.Subtract(new TimeSpan(t,0,0,0));
                    MessageBox.Show(m_client.DeleteAllWatsonGenerateFilesTimeSpanBeforeNow(d, textBox14.Text, checkBox6.Checked));
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.Message);
                }
            }
        }

        private void button8_Click_1(object sender, EventArgs e)
        {
            if (m_client.IsConnected == false)
            {
                MessageBox.Show("You are not connected to streaming field server");
                return;
            }
            if (checkBox6.Checked == true && textBox14.Text == string.Empty)
            {
                MessageBox.Show("Please specify user name for the operations");
                return;
            }
            PasswordForm p = new PasswordForm();
            p.ShowDialog();
            if (p.DialogResult == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    MessageBox.Show(m_client.DeleteAllWatsonGenerateFilesTimeSpanBeforeNow(dateTimePicker5.Value,
                                                                                           textBox14.Text, 
                                                                                           checkBox6.Checked));
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.Message);
                }
            }
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                m_client.SetWatsonRunning(checkBox7.Checked);
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }              
    }
}
