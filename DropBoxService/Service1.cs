using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using FileServer.Services;
using System.ServiceModel;
using ServiceLoggerApi;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NetCard;
using WCFStreamingServiceAndClientApi;


namespace GojiDropBoxService
{
    public partial class Service1 : ServiceBase
    {
        static WCFServiceApi m_wcfc = new WCFServiceApi();

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                string addr = getIpAddress();
                if (addr != string.Empty)
                {
                    m_wcfc.StartService(addr, 8050);
                    //m_wcfc.StartClient(addr, 8050);
                    //Console.WriteLine("Service started at: " + addr + ":" + 5050);
                    //string res = m_wcfc.InitializeDropBox();
                    //ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_SERVICE, "InitializeDropBox: " + res);
                }
                else
                {

                }
            }
            catch (Exception err)
            {
                ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_SERVICE, "OnStart: " + err.Message);
            }
        }
        public static IEnumerable<IPAddress> GetIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return (from ip in host.AddressList where !IPAddress.IsLoopback(ip) select ip).ToList();
        }

        string getIpAddress()
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
        protected override void OnStop()
        {
            //string res = m_wcfc.CloseDropBox();
            //ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_SERVICE, "CloseDropBox: " + res);
        }
    }
}
