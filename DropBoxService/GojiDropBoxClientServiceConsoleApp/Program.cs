using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FileServerWinClientLib;
using System.Net;
using System.Net.Sockets;
using ServiceLoggerApi;
using WCFStreamingServiceAndClientApi;



namespace GojiWCFStreamingClientConsoleApp
{
    class Program
    {
        static WCFServiceApi m_wcfc = new WCFServiceApi();

        public static IEnumerable<IPAddress> GetIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return (from ip in host.AddressList where !IPAddress.IsLoopback(ip) select ip).ToList();
        }

        static string getIpAddress()
        {
            IPAddress ipaddress = null;

            NetCard.NetCard.ShowNetworkInterfaceMessage();
            try
            {
                foreach (KeyValuePair<string, string> entry in NetCard.NetCard.m_netDic)
                {
                    ipaddress = IPAddress.Parse(entry.Value);
                    break;
                }
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
            return ipaddress.ToString();
           
        }
        static void Main(string[] args)
        {
            string addr = string.Empty;
            
            /// Comment this code if you want to raise the server here instead using it as a service
            ///
           
            try
            {
                addr = getIpAddress();
                           
                if (addr != string.Empty)
                {
                    Console.WriteLine("1) for both client and server");
                    Console.WriteLine("2) for server only");
                    Console.WriteLine("3) for client only");
                    int userSelect = int.Parse(Console.ReadLine());
                    // The same service that located on the service of the client 
                    // This service is to send the client window service command( service that located on the field)
                    if (userSelect == 1 || userSelect == 2)
                    {
                        m_wcfc.StartService(addr, 8050);
                        ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_CLIENT, "Service started to list to " + addr);
                        Console.WriteLine("Server started at " + addr + ":" + 8050);
                    }

                    if (userSelect == 1 || userSelect == 3)
                    {
                        string res;
                        if ((res = m_wcfc.StartClient(addr, 8050)) != "Hello world from test")
                        {
                            throw (new SystemException("Failed to connect to wcf client server at field side"));
                        }
                        Console.WriteLine("Client started at " + addr + ":" + 8050);
                        res = m_wcfc.InitializeDropBox();
                        if (res != "ok")
                        {
                            throw (new SystemException(res));
                        }
                        Console.WriteLine("Dropbox initialized ok");

                        string filter = "*.*";
                        string storeDir = "c:\\mpfmtests";
                        m_wcfc.StartWatch(storeDir, filter, true);
                        Console.WriteLine("Start watch all files at: " + storeDir);
                    }
                }
            }
            catch (Exception err)
            {
                ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_CLIENT, "Service error on start: " + addr);
                return; 
            }          
            /*
            m_wcfc.StartClient(addr, 8030);
            if (m_wcfc.ConnectToFileServer(addr , 5000) != "ok")
            {
                Console.WriteLine("Error connecting to file server");
                m_wcfc.Close();
                return; 
            }
            List<string> list = m_wcfc.RefreshFileList();
            for (int i = 0; i < list.Count; i++)
            {
                Console.WriteLine(list[i]);
            }

            m_wcfc.StartWatch("c:\\mpfmtests\\", "*.bin", true);
            */
            /*
            m_wcfc.DeleteAllFilesOnServer();
            list = m_wcfc.RefreshFileList();
            for (int i = 0; i < list.Count; i++)
            {
                Console.WriteLine(list[i]);
            }
            */
            Console.WriteLine("Write any key to stop client");
            Console.ReadLine();
            m_wcfc.StopWatch();
            m_wcfc.CloseDropBox();
            m_wcfc.Close();

            try
            {
                //m_wcfc.Close();
                ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_CLIENT, "Service stopped");
            }
            catch (Exception err)
            {
                ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_CLIENT, "Service stopped error" + err.Message);
            }

        }
    }
}
