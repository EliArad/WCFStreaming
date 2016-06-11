using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FileServerWinClientLib;
using System.Net;
using System.Net.Sockets;
using ServiceLoggerApi;
using NetCard;
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
                     
            try
            {
                addr = getIpAddress();
                
                if (addr != string.Empty)
                {
                    // The same service that located on the service of the client 
                    // This service is to send the client window service command( service that located on the field)
                    m_wcfc.StartService(addr, 8030);
                    ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_CLIENT, "Service started to list to " + addr);
                }
            }
            catch (Exception err)
            {
                ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_CLIENT, "Service error on start: " + addr);
                return; 
            }          
            
            Console.WriteLine("Write any key to stop client");
            Console.ReadLine();
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
