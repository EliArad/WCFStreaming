using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using FileServerWinClientLib;
using System.Net;
using System.Net.Sockets;
using ServiceLoggerApi;
using NetCard;
using WCFStreamingServiceAndClientApi;

namespace GojiWCFStreamingClient
{
    public partial class Service1 : ServiceBase
    {
        WCFServiceApi m_wcfc = new WCFServiceApi();
        public Service1()
        {
            InitializeComponent();
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

        protected override void OnStart(string[] args)
        {
            string addr = string.Empty;
            try
            {
                addr = getIpAddress();
                if (addr != string.Empty)
                {
                    m_wcfc.StartService(addr , 8030);
                    //ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_CLIENT, "Service started to list to " + addr);
                }
            }
            catch (Exception err)
            {
                //ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_CLIENT, "Service error on start: " + addr);
            }
        }

        protected override void OnStop()
        {
            try
            {
                m_wcfc.Close();
                ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_CLIENT, "Service stopped");
            }
            catch (Exception err)
            {
                ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_CLIENT, "Service stopped error" + err.Message);
            }
        }
    }
}
