using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.IO;

namespace FileServerWinClient
{
	[MessageContract]
	public class FileUploadMessage
	{
		[MessageHeader(MustUnderstand=true)]
		public string VirtualPath { get; set; }

        [MessageHeader(MustUnderstand = true)]
        public string TargetPath { get; set; }

		[MessageBodyMember(Order=1)]
		public Stream DataStream { get; set; }

        [MessageHeader(MustUnderstand = true)]
        public DateTime startTime { get; set; }

        [MessageHeader(MustUnderstand = true)]
        public string clientIpAddress { get; set; }

        [MessageHeader(MustUnderstand = true)]
        public string fieldGuid { get; set; }

        [MessageHeader(MustUnderstand = true)]
        public ulong G360Index { get; set; }


        [MessageHeader(MustUnderstand = true)]
        public string _fileOwnerUserName { get; set; }

        [MessageHeader(MustUnderstand = true)]
        public bool AddDateToTargetFolder { get; set; }


        [MessageHeader(MustUnderstand = true)]
        public DateTime watsonDateTime { get; set; }
                
        [MessageHeader(MustUnderstand = true)]
        public long SizeOfFile { get; set; }
         
	}
}
