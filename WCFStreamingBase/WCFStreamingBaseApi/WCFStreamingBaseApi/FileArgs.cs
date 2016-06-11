using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GojiWCFStreamingBaseApi
{
	public delegate void FileEventHandler(object sender, FileEventArgs e);

	public class FileEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the virtual path.
		/// </summary>
        /// 

        public string CompleteTargetPath
        {
            get { return _CompleteTargetPath; }
        }
        string _CompleteTargetPath = null;

		public string VirtualPath
		{
			get { return _VirtualPath; }
		}
		string _VirtualPath = null;

        public DateTime startTime
        {
            get { return _startTime; }
        }
        DateTime _startTime = DateTime.Now;

        public string ClientIpAddress
        {
            get { return _ClientIpAddress; }
        }
        string _ClientIpAddress = null;

        public string fieldGuid
        {
            get { return _fieldGuid; }
        }
        string _fieldGuid = null;
         
        public ulong g360Index
        {
            get { return _g360Index; }
        }
        ulong _g360Index = 0;


        public string fileOwnerUserName
        {
            get { return _fileOwnerUserName; }
        }
        string _fileOwnerUserName = null;



        public DateTime WatsonDateTime
        {
            get { return _watsonDateTime; }
        }
        DateTime _watsonDateTime = DateTime.Now;

        public long sizeOfFile
        {
            get { return _sizeOfFile; }
        }
        long _sizeOfFile = 0;


        public string TargetPath
        {
            get { return _TargetPath; }
        }
        string _TargetPath = null;
         

		/// <summary>
		/// Initializes a new instance of the <see cref="FileEventArgs"/> class.
		/// </summary>
		/// <param name="vPath">The v path.</param>
        public FileEventArgs(string vPath, 
                             DateTime startTime, 
                             string clientIpAddress, 
                             string fieldGuid, 
                             ulong g360Index, 
                             string fileOwnerUserName, 
                             DateTime watsonDateTime, 
                             long sizeOfFile,
                             string TargetPath,
                             string VirtualPath)
		{
            this._CompleteTargetPath = vPath;
            this._startTime = startTime;
            this._ClientIpAddress = clientIpAddress;
            this._fieldGuid = fieldGuid;
            this._g360Index = g360Index;
            this._fileOwnerUserName = fileOwnerUserName;
            this._watsonDateTime = watsonDateTime;
            this._sizeOfFile = sizeOfFile;
            this._TargetPath = TargetPath;
            this._VirtualPath = VirtualPath;
        }
	}
}
