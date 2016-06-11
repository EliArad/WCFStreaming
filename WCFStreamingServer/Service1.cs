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

namespace GojiWCFStreamingServer
{
    public static class NetworkDrives
    {
        public static bool MapDrive(string DriveLetter, string Path, string Username, string Password)
        {

            bool ReturnValue = false;

            if (System.IO.Directory.Exists(DriveLetter + ":\\"))
            {
                DisconnectDrive(DriveLetter);
            }
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;

            p.StartInfo.FileName = "net.exe";
            p.StartInfo.Arguments = " use " + DriveLetter + ": " + '"' + Path + '"' + " " + Password + " /user:" + Username + " /y";
            p.Start();
            p.WaitForExit();

            string ErrorMessage = p.StandardError.ReadToEnd();
            string OuputMessage = p.StandardOutput.ReadToEnd();
            if (ErrorMessage.Length > 0)
            {
                throw new Exception("Error:" + ErrorMessage);
            }
            else
            {
                ReturnValue = true;
            }
            return ReturnValue;
        }
        public static bool DisconnectDrive(string DriveLetter)
        {
            bool ReturnValue = false;
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;

            p.StartInfo.FileName = "net.exe";
            p.StartInfo.Arguments = " use " + DriveLetter + ": /DELETE";
            p.Start();
            p.WaitForExit();

            string ErrorMessage = p.StandardError.ReadToEnd();
            string OuputMessage = p.StandardOutput.ReadToEnd();
            if (ErrorMessage.Length > 0)
            {
                throw new Exception("Error:" + ErrorMessage);
            }
            else
            {
                ReturnValue = true;
            }
            return ReturnValue;
        }

    }
    public partial class Service1 : ServiceBase
    {
        static WCFServiceApi m_wcfc = new WCFServiceApi();

        public Service1()
        {
            InitializeComponent();
        }
        protected override void OnStop()
        {
            try
            {
                if (m_wcfc != null)
                    m_wcfc.Close();
                ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_SERVICE, "Service stopped");
            }
            catch (Exception err)
            {
                ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_SERVICE, "Service error on stop " + err.Message);
            }
        }

        protected override void OnStart(string[] args)
        {
            try
            {                                              
                try
                {
                    if (NetworkDrives.MapDrive("M", @"\\fshh002\delonfriday", "elia", "elianat4414") == false)
                    {
                        ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_SERVICE, "Cannot map M drive ");
                    }
                }
                catch (Exception er)
                {
                    ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_SERVICE, "Cannot map M drive " + er.Message);
                }

                Thread.Sleep(800);
                 
                 string addr = getIpAddress();
                 if (addr != string.Empty)
                 {

                     m_wcfc.StartService(addr, 5050);
                     m_wcfc.StartClient(addr, 5050);
                     Console.WriteLine("Service started at: " + addr + ":" + 5050);
                     ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_SERVICE, "Service started at: " + addr + ":" + 5050);
                     m_wcfc.StartWCFStreamingServerOnProxy(addr, "c:\\Goji\\Storage");
                     Console.WriteLine("Service started at: " + addr + ":" + 5000);
                     //ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_SERVICE, "Service started at: " + addr + ":" + 5000);
                 }
                 else
                 {
                     ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_SERVICE, "Service not started. error find suitable ip:" + addr);
                 }
            }
            catch (Exception err)
            {
                ServiceLogger.WriteLine(LOGGER_NAMES.GOJI_WCF_STREAMING_SERVICE,err.Message);
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
    }
}
