using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using FileServer.Services;
using System.Diagnostics;
using System.Threading;
using System.ServiceModel;
using System.Xml;
using System.Configuration;
using FileServerWinClientLib;
using System.Security.Principal;
using System.ServiceModel.Security;

namespace FileServerWinClient
{
	public partial class Form1 : Form
	{

        private System.IO.FileSystemWatcher m_Watcher;
        FileRepositoryServiceClient m_client = null;
        bool m_bIsWatching = false;
		public Form1()
		{

			InitializeComponent();
            enableGui(false); 
            LoadSettings();
		}
        void enableGui(bool enbale)
        {
            DeleteButton.Enabled = enbale;
            UploadButton.Enabled = enbale;
            DownloadButton.Enabled = enbale;
            button1.Enabled = enbale;
            button2.Enabled = enbale;
            button3.Enabled = enbale;
            button4.Enabled = enbale;

        }

		/// <summary>
		/// Handles the Load event of the Form1 control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void Form1_Load(object sender, EventArgs e)
		{
			
		}

		/// <summary>
		/// Refreshes the file list.
		/// </summary>
		private void RefreshFileList()
		{
            
			StorageFileInfo[] files = null;
			files = m_client.List(null);
			FileList.Items.Clear();

			int width = FileList.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;

			float[] widths = { .2f, .6f, .2f };

			for (int i = 0; i < widths.Length; i++)
				FileList.Columns[i].Width = (int)((float)width * widths[i]);

			foreach (var file in files)
			{
				ListViewItem item = new ListViewItem(Path.GetFileName(file.VirtualPath));

				item.SubItems.Add(file.VirtualPath);

				float fileSize = (float)file.Size / 1024.0f;
				string suffix = "Kb";

				if (fileSize > 1000.0f)
				{
					fileSize /= 1024.0f;
					suffix = "Mb";
				}
				item.SubItems.Add(string.Format("{0:0.0} {1}", fileSize, suffix));

				FileList.Items.Add(item);
			}
		}

		private void DownloadButton_Click(object sender, EventArgs e)
		{
            try
            {
                button2.Enabled = false;
                if (FileList.SelectedItems.Count == 0)
                {
                    MessageBox.Show("You must select a file to download");
                }
                else
                {
                    ListViewItem item = FileList.SelectedItems[0];

                    // Strip off 'Root' from the full path
                    string path = item.SubItems[1].Text;

                    // Ask where it should be saved
                    SaveFileDialog dlg = new SaveFileDialog()
                    {
                        RestoreDirectory = true,
                        OverwritePrompt = true,
                        Title = "Save as...",
                        FileName = Path.GetFileName(path)
                    };

                    dlg.ShowDialog(this);

                    if (!string.IsNullOrEmpty(dlg.FileName))
                    {
                        // Get the file from the server
                        using (FileStream output = new FileStream(dlg.FileName, FileMode.Create))
                        {
                            Stream downloadStream;
                            downloadStream = m_client.GetFile(path);
                            downloadStream.CopyTo(output);
                        }

                        Process.Start(dlg.FileName);
                    }
                }
                button2.Enabled = true;
            }
            catch (Exception err)
            {
                button2.Enabled = true;
                MessageBox.Show(err.Message);
            }
		}

        private void UploadFileToServer(string FileName)
        {

            string virtualPath = Path.GetFileName(FileName);

            using (Stream uploadStream = new FileStream(FileName, FileMode.Open))
            {
                 m_client.PutFile(new FileUploadMessage() { VirtualPath = virtualPath, DataStream = uploadStream });
            }
        }
        private void UploadFileToServerWithPath(string FileName)
        {

            string virtualPath = Path.GetFileName(FileName);

            string[] path = SplitPath(Path.GetDirectoryName(FileName));
            string targetPath = string.Empty;
            for (int i = 2; i < path.Length; i++)
            {
                targetPath += path[i] + "\\";
            }

            using (Stream uploadStream = new FileStream(FileName, FileMode.Open))
            {
                m_client.PutFileWithPath(new FileUploadMessage() { VirtualPath = virtualPath, TargetPath = targetPath, DataStream = uploadStream });
            }
        }

        public static String[] SplitPath(string path)
        {
            String[] pathSeparators = new String[] { "\\" };
            return path.Split(pathSeparators, StringSplitOptions.RemoveEmptyEntries);
        }
		private void UploadButton_Click(object sender, EventArgs e)
		{
            try
            {
                button2.Enabled = false;
                OpenFileDialog dlg = new OpenFileDialog()
                {
                    Title = "Select a file to upload",
                    RestoreDirectory = true,
                    CheckFileExists = true
                };

                dlg.ShowDialog();


                if (!string.IsNullOrEmpty(dlg.FileName))
                {
                    string[] path = SplitPath(Path.GetDirectoryName(dlg.FileName));
                    string targetPath = string.Empty;
                    for (int i = 2; i < path.Length; i++)
                    {
                        targetPath += path[i] + "\\";
                    }
                    string virtualPath = Path.GetFileName(dlg.FileName);

                    using (Stream uploadStream = new FileStream(dlg.FileName, FileMode.Open))
                    {
                        m_client.PutFileWithPath(new FileUploadMessage() { VirtualPath = virtualPath, TargetPath = targetPath, DataStream = uploadStream });
                    }

                    RefreshFileList();
                }
                button2.Enabled = true;
            }
            catch (Exception err)
            {
                button2.Enabled = true;
                MessageBox.Show(err.Message);
            }
		}

		private void DeleteButton_Click(object sender, EventArgs e)
		{
            try
            {
                button2.Enabled = false;
                if (FileList.SelectedItems.Count == 0)
                {
                    MessageBox.Show("You must select a file to delete");
                }
                else
                {
                    string virtualPath = FileList.SelectedItems[0].SubItems[1].Text;

                    m_client.DeleteFile(virtualPath);

                    RefreshFileList();
                }
                button2.Enabled = true;
            }
            catch (Exception err)
            {
                button2.Enabled = true;
                MessageBox.Show(err.Message);
            }
		}

        void StopWatch()
        {
            m_bIsWatching = false;
            m_Watcher.EnableRaisingEvents = false;
            m_Watcher.Dispose();
        }
        private void button2_Click(object sender, EventArgs e)
        {

            try
            {
                DeleteButton.Enabled = false;
                UploadButton.Enabled = false;
                DownloadButton.Enabled = false;
                if (comboBox1.Text == string.Empty)
                {
                    MessageBox.Show("Please select filter");
                    return;
                }
                StartWatch(textBox1.Text, comboBox1.Text, checkBox1.Checked);
                button2.Enabled = false;
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        void StartWatch(string PathToWatch, string Filter, bool IncludeSubdirectories)
        {
            m_bIsWatching = true;

            m_Watcher = new System.IO.FileSystemWatcher();
            m_Watcher.Filter = Filter; // "*.*";
            if (PathToWatch[PathToWatch.Length - 1] != '\\')
                m_Watcher.Path = PathToWatch + "\\"; // txtFile.Text + "\\";
            else
                m_Watcher.Path = PathToWatch;

            m_Watcher.IncludeSubdirectories = IncludeSubdirectories;


            m_Watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                 | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            m_Watcher.Changed += new FileSystemEventHandler(OnChanged);
            m_Watcher.Created += new FileSystemEventHandler(OnCreated);
            m_Watcher.Deleted += new FileSystemEventHandler(OnDelete);
            m_Watcher.Renamed += new RenamedEventHandler(OnRenamed);
            m_Watcher.EnableRaisingEvents = true;
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {

            UploadFileToServerWithPath(e.FullPath);
        }
        private void OnDelete(object sender, FileSystemEventArgs e)
        {


        }
        private void OnChanged(object sender, FileSystemEventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                DeleteButton.Enabled = true;
                UploadButton.Enabled = true;
                DownloadButton.Enabled = true;
                StopWatch();
                button2.Enabled = true;
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult result = this.folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBox1.Text = this.folderBrowserDialog1.SelectedPath;
            }
        }

        void LoadSettings()
        {
            textBox1.Text = Properties.Settings.Default.MonitorDirectory;
            checkBox1.Checked = Properties.Settings.Default.IncludeSubDir;
            comboBox1.SelectedIndex = Properties.Settings.Default.FilterIndex;
            textBox2.Text = Properties.Settings.Default.ServerAddress;
        }
        void SaveSettings()
        {
            Properties.Settings.Default.ServerAddress = textBox2.Text;
            Properties.Settings.Default.IncludeSubDir = checkBox1.Checked;
            Properties.Settings.Default.MonitorDirectory = textBox1.Text;
            Properties.Settings.Default.FilterIndex = comboBox1.SelectedIndex;
            Properties.Settings.Default.Save();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
            if (m_client != null)
                m_client.Close();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                RefreshFileList();
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }
        
        void UpdateAppConfig(string newAddress , string appConfigFileName)
        {
            var doc = new XmlDocument();
            doc.Load(appConfigFileName);
            XmlNodeList endpoints = doc.GetElementsByTagName("endpoint");
            foreach (XmlNode item in endpoints)
            {
                var adressAttribute = item.Attributes["address"];
                if (!ReferenceEquals(null, adressAttribute))
                {
                    adressAttribute.Value = string.Format("net.tcp://{0}:5000", newAddress);
                }
            }
            doc.Save(appConfigFileName);
        }
        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                if (textBox2.Text == string.Empty)
                {
                    MessageBox.Show("Please specify the server address");
                    return;
                }

                NetTcpBinding binding = new NetTcpBinding();
                binding.TransferMode = TransferMode.Streamed;
                binding.Security.Mode = SecurityMode.None;
                binding.SendTimeout = new TimeSpan(0, 0, 10);
                binding.OpenTimeout = new TimeSpan(0, 0, 10);
                binding.ReceiveTimeout = new TimeSpan(0, 0, 10);
                binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.None;
                var myEndpointAddress = new EndpointAddress("net.tcp://" + textBox2.Text + ":5000");
                m_client = new FileRepositoryServiceClient("FileRepositoryService", myEndpointAddress);
                //m_client.ClientCredentials.Windows.AllowedImpersonationLevel = TokenImpersonationLevel.Impersonation;

                if (textBox3.Text != string.Empty && textBox4.Text != string.Empty)
                {
                    m_client.ClientCredentials.Windows.ClientCredential.UserName = textBox3.Text; // "IND-MPFM2\\Incteam";
                    m_client.ClientCredentials.Windows.ClientCredential.Password = textBox4.Text; // "Bator23";
                    m_client.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.PeerOrChainTrust;
                }


                RefreshFileList();
                enableGui(true);                 
            }
            catch (Exception err)
            {
                enableGui(false);
                MessageBox.Show(err.Message);
            }
        }
	}
}
