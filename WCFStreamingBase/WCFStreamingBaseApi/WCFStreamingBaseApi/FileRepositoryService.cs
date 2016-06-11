using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.IO;

namespace GojiWCFStreamingBaseApi
{
	[ServiceBehavior(IncludeExceptionDetailInFaults=true,
		InstanceContextMode=InstanceContextMode.Single)]
	public class FileRepositoryService : IFileRepositoryService
	{ 
		public event FileEventHandler FileRequested;
		public event FileEventHandler FileUploaded;
		public event FileEventHandler FileDeleted;




		/// <summary>
		/// Gets or sets the repository directory.
		/// </summary>
		public string RepositoryDirectory { get; set; }

        public Dictionary<string, string> RepositoryDirectoryDic;


		/// <summary>
		/// Gets a file from the repository
		/// </summary>
		public Stream GetFile(string virtualPath)
		{
			string filePath = Path.Combine(RepositoryDirectory, virtualPath);

			if (!File.Exists(filePath))
				throw new FileNotFoundException("File was not found", Path.GetFileName(filePath));

			SendFileRequested(virtualPath, DateTime.Now, string.Empty, string.Empty,0, string.Empty , DateTime.Now, 0, string.Empty , string.Empty);

			return new FileStream(filePath, FileMode.Open, FileAccess.Read);
		}

		/// <summary>
		/// Uploads a file into the repository
		/// </summary>
		public void PutFile(FileUploadMessage msg)
		{
			string filePath = Path.Combine(RepositoryDirectoryDic[msg.fieldGuid], msg.VirtualPath);
			string dir = Path.GetDirectoryName(filePath);

			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			using (var outputStream = new FileStream(filePath, FileMode.Create))
			{
				msg.DataStream.CopyTo(outputStream);
			}

			SendFileUploaded(filePath, 
                             msg.startTime, 
                             msg.clientIpAddress, 
                             msg.fieldGuid, 
                             msg.G360Index, 
                             msg._fileOwnerUserName, 
                             msg.watsonDateTime, 
                             msg.SizeOfFile, 
                             msg.TargetPath , 
                             msg.VirtualPath);
		}

        void CreateDirectoryRecursively(string path)
        {
            string[] pathParts = path.Split('\\');

            for (int i = 0; i < pathParts.Length; i++)
            {
                if (i > 0)
                    pathParts[i] = Path.Combine(pathParts[i - 1], pathParts[i]);

                if (!Directory.Exists(pathParts[i]))
                    Directory.CreateDirectory(pathParts[i]);
            }
        }
        public void PutFileWithPath(FileUploadMessage msg)
        {
            string dir = string.Empty;
            try
            {
                string fieldPath = RepositoryDirectoryDic[msg.fieldGuid] + "\\" + msg.fieldGuid;
                if (msg.AddDateToTargetFolder == true)
                {
                    string[] fileDate = msg.VirtualPath.Split(new Char[] { '_' });
                    //"SENSOR_888_05_05_2015_09_33_50_496.bin"
                    if (fileDate.Length == 9)
                    {
                        string dateDir = fileDate[4] + "_" + fileDate[3] + "_" + fileDate[2];
                        fieldPath += "\\" + dateDir;
                    }
                    else
                    {
                        DateTime d = msg.watsonDateTime;
                        fieldPath += "\\" + d.Year + "_" + d.Month + "_" + d.Day;
                    }
                }
                if (Directory.Exists(fieldPath) == false)
                {
                    Directory.CreateDirectory(fieldPath);
                }
                string filePath = Path.Combine(fieldPath + "\\" + msg.TargetPath, msg.VirtualPath);
                dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using (var outputStream = new FileStream(filePath, FileMode.Create))
                {
                    msg.DataStream.CopyTo(outputStream);
                }

                SendFileUploaded(filePath, 
                                 msg.startTime, 
                                 msg.clientIpAddress, 
                                 msg.fieldGuid, 
                                 msg.G360Index, 
                                 msg._fileOwnerUserName, 
                                 msg.watsonDateTime, 
                                 msg.SizeOfFile, 
                                 msg.TargetPath, 
                                 msg.VirtualPath);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                File.WriteAllText("c:\\PutFileWithPath.txt", err.Message + Environment.NewLine + dir);
            }
        }

		/// <summary>
		/// Deletes a file from the repository
		/// </summary>
		public void DeleteFile(string virtualPath)
		{
			string filePath = Path.Combine(RepositoryDirectory, virtualPath);

			if (File.Exists(filePath))
			{
                SendFileDeleted(virtualPath, DateTime.Now, string.Empty, string.Empty,0 , string.Empty , DateTime.Now, 0, string.Empty, string.Empty);
				File.Delete(filePath);
			}
		}

		/// <summary>
		/// Lists files from the repository at the specified virtual path.
		/// </summary>
		/// <param name="virtualPath">The virtual path. This can be null to list files from the root of
		/// the repository.</param>
		public StorageFileInfo[] List(string virtualPath)
		{
			string basePath = RepositoryDirectory;

			if (!string.IsNullOrEmpty(virtualPath))
				basePath = Path.Combine(RepositoryDirectory, virtualPath);

			DirectoryInfo dirInfo = new DirectoryInfo(basePath);
			FileInfo[] files = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);

			return (from f in files
				   select new StorageFileInfo()
				   {
					   Size = f.Length,
					   VirtualPath = f.FullName.Substring(f.FullName.IndexOf(RepositoryDirectory) + RepositoryDirectory.Length + 1)
				   }).ToArray();
		}
 
		/// <summary>
		/// Raises the FileRequested event.
		/// </summary>
        protected void SendFileRequested(string vPath, DateTime startTime, string clientIpAddress, string fieldGuid, ulong g360Index, string fileOwnerUserName, DateTime wtsDateTime, long sizeOfFile, string TargetPath, string VirtualPath)
		{
			if (FileRequested != null)
                FileRequested(this, new FileEventArgs(vPath, startTime, clientIpAddress, fieldGuid, g360Index, fileOwnerUserName, wtsDateTime, sizeOfFile,TargetPath, VirtualPath));
		}

		/// <summary>
		/// Raises the FileUploaded event
		/// </summary>

        protected void SendFileUploaded(string vPath, 
                                        DateTime startTime, 
                                        string clientIpAddress, 
                                        string fieldGuid, 
                                        ulong g360Index, 
                                        string fileOwnerUserName, 
                                        DateTime wtsDateTime , 
                                        long sizeOfFile, 
                                        string TargetPath, 
                                        string VirtualPath)
		{
			if (FileUploaded != null)
                FileUploaded(this, new FileEventArgs(vPath, startTime, clientIpAddress, fieldGuid, g360Index, fileOwnerUserName, wtsDateTime, sizeOfFile, TargetPath, VirtualPath));
		}

		/// <summary>
		/// Raises the FileDeleted event.
		/// </summary>
        protected void SendFileDeleted(string vPath, DateTime startTime, string clientIpAddress, string fieldGuid, ulong g360Index, string fileOwnerUserName, DateTime wtsDateTime, long sizeOfFile, string TargetPath, string VirtualPath)
		{
			if (FileDeleted != null)
                FileDeleted(this, new FileEventArgs(vPath, startTime, clientIpAddress, fieldGuid, g360Index, fileOwnerUserName, wtsDateTime, sizeOfFile, TargetPath , VirtualPath));
		}

	 
	}
}
