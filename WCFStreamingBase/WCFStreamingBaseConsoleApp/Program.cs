using GojiWCFStreamingBaseApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Net;
using RegistryClassApi;
using Microsoft.Win32; 


namespace ConsoleApplication1
{
    class Program
    {
        static GojiWCFStreamingBase.ClientCallbackMessage p;
        static GojiWCFStreamingBase.ServerCallbackMessage p1;
        static GojiWCFStreamingBase m_client;
        static string m_baseGuid;
        static void Main(string[] args)
        {
            p = new GojiWCFStreamingBase.ClientCallbackMessage(ClientCallbackMsg);
            p1 = new GojiWCFStreamingBase.ServerCallbackMessage(ServerCallbackMsg);
            GojiWCFStreamingBase.SetServerCallback = p1;
            m_client = new GojiWCFStreamingBase(p);
            
             
            try
            {
                Initialize();
                Console.ReadLine();
                GojiWCFStreamingBase.CloseStorageServer();
                m_client.StopWatch();

            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }
       
        static void Initialize()
        {            
            while (true)
            {
                try
                {
                    string guid = "8079d013-18f2-4aed-af4c-1918723e3f8d";
                    Console.WriteLine("Attempting to connect to 192.168.21.104:8030");
                    m_client.ConnectStreamingFieldClient("192.168.21.104", 8030 , "" , "");
                    m_client.SetCopyThreadTimeEvent(new TimeSpan(0, 0, 10));
                    Console.WriteLine("Connected to 192.168.21.104:8030");
                    break;
                }
                catch (Exception err)
                {
                    Thread.Sleep(1000);
                }
            }


            clsRegistry reg = new clsRegistry();
            m_baseGuid = reg.GetStringValue(Registry.LocalMachine, "SOFTWARE\\Goji solutions\\Base", "Guid");
            if (reg.strRegError != null)
            {
                m_baseGuid = Guid.NewGuid().ToString();
                reg.SetStringValue(Registry.LocalMachine, "SOFTWARE\\Goji solutions\\Base", "Guid", m_baseGuid);
            } 




            //m_client.DeleteAllWatsonGenerateFilesOnField("123456");
            NetCard.NetCard n = new NetCard.NetCard();
            string serverIp = n.getComputerIP();
            m_client.Register(serverIp);
            m_client.CleanFifoHandler();
            Dictionary<string, string> storageDirectoryDic = new Dictionary<string, string>();
            storageDirectoryDic.Add(m_baseGuid, "m:\\Goji\\Storage");
            GojiWCFStreamingBase.StartStorageServer(storageDirectoryDic);
            m_client.ConnectToStreamingServer(serverIp);
            m_client.StartWatch(@"C:\mpfmtests\1111", "*.bin", true, "Elia");
        }
        static void InitializeClient()
        {
            while (true)
            {
                try
                {
                    Console.WriteLine("Attempting to connect to 192.168.22.34:8030");
                    m_client.ConnectStreamingFieldClient("192.168.22.34", 8030 , "" , "");
                    m_client.SetCopyThreadTimeEvent(new TimeSpan(0, 0, 10));
                    Console.WriteLine("Connected to 192.168.22.34:8030");
                    break;
                }
                catch (Exception err)
                {
                    Thread.Sleep(1000);
                }
            }

            NetCard.NetCard n = new NetCard.NetCard();
            m_client.Register(n.getComputerIP());
            m_client.CleanFifoHandler();
            m_client.ConnectToStreamingServer("192.168.22.34");
            m_client.StartWatch(@"C:\mpfmtests\1111", "*.bin", true, "Elia");
        }

        static void ClientCallbackMsg(string fieldGuid,string ipAddress, int code, string msg, DateTime startTime)
        {
            switch (code)   
            {
                case 18:
                    Console.WriteLine("From IP: {0} msg arrived: {1}", ipAddress, msg, startTime.ToString());
                break;
                case 77:
                    InitializeClient();
                break;
            }
        }
        static void ServerCallbackMsg(string fieldGuid, string ipAddress, int code, string msg, DateTime startTime, ulong g360Index, string fileOwnerUserName, long sizeOfFile)
        {
            string[] s = msg.Split(new Char[] { ',' });
            if (code == 400)
            {
                Console.WriteLine(string.Format("File upload\t{0}\t startTime: {1} Uploaded at:{2} user: {3}", s[0], s[1], DateTime.Now, fileOwnerUserName, sizeOfFile));
            }
            //Console.WriteLine("code: {0} ipAddress {1}  msg {2}", code, ipAddress, msg);  
        }
    }
}
