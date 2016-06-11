using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GojiWCFStreamingBaseApi
{
    namespace ExtensionMethods
    {

        public class ServerJsonApi
        {
            public class jCredential
            {
                public string userName;
                public string password;
            }
            
            public class jConnected
            {            
                public string random;
            }
            public class jFieldGuid
            {
                public string guid;
            }

            public class jDateTime
            {
                public string datetime;
            }

            public class jFileTransfer
            {
                public string guidName;
                public string userName;
                public string DirectoryName;
                public string fileName;
                public string fileSize;
            }
             
            public class jMsg
            {
                public string message;
            }
            public class jOPC
            {
                public string status;
            }
             
            public class jBroadcast
            {
                public int code;
                public string message;
            }
             
            public string BroadcastMessage(int code, string message)
            {
                jBroadcast data = new jBroadcast();
                data.code = code;
                data.message = message;
                string json = "broadcastmessage|" + JsonConvert.SerializeObject(data);
                return json;
            }

            public string InitiateFileTransfer(jFileTransfer j)
            {
                string json = "initiatefiletransfer|" + JsonConvert.SerializeObject(j);
                return json;
            }
             
            public string SendErrorMessage(string msg)
            {
                jMsg data = new jMsg();
                data.message = msg;
                string json = "errormsg|" + JsonConvert.SerializeObject(data);
                return json;
            }

            public string SetOPC(string status)
            {
                jOPC data = new jOPC();
                data.status = status;
                string json = "opc|" + JsonConvert.SerializeObject(data);
                return json;
            }

            public string GetClientGuid(string guid)
            {
                jFieldGuid data = new jFieldGuid();
                data.guid = guid;
                string json = "getguid|" + JsonConvert.SerializeObject(data);
                return json;
            }
            
            public string SendCredintials(string userName, string password)
            {
                jCredential start = new jCredential();
                start.userName = userName;
                start.password = password;
                string json = "sendcredintials|" + JsonConvert.SerializeObject(start);
                return json;
            }
             
            public string GetGuid()
            {
                string json = "getguid|";
                return json;
            }

            public string Connected(string random)
            {
                jConnected data = new jConnected();
                data.random = random;
                string json = "clientconnected|" + JsonConvert.SerializeObject(data);
                return json;
            }
            public T DeserializeObject<T>(string data)
            {
                T deserializedProduct = JsonConvert.DeserializeObject<T>(data);
                return deserializedProduct;
            }
        }
    }
}
