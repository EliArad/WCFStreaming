using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FileServer.Services;
using System.ServiceModel;
using ServiceLoggerApi;
using System.IO;
using System.Net;
using System.Net.Sockets;
using NetCard;
using WCFStreamingServiceAndClientApi; 

namespace GojiWCFStreamingServerConsole
{

    class Program
    {
        static void Main(string[] args)
        {
            try
            { 
                WCFSeriveTest t = new WCFSeriveTest();
                t.Start();
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
            Console.ReadKey();

        }
    }

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
            p.StartInfo.Arguments = " use " + DriveLetter + ": " + '"' + Path + '"' + " " + Password + " /user:" + Username;
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

    public class WCFSeriveTest
    {
        static ServiceHost host = null;
        static FileRepositoryService service = null;
        static WCFServiceApi m_wcfc = new WCFServiceApi();

        public void Start()
        {
            try
            {

                string addr = getIpAddress();
                if (addr != string.Empty)
                {
                    m_wcfc.StartClient(addr, 8030);
                    string res;
                    if ((res = m_wcfc.StartWCFStreamingServerOnProxy(addr, "c:\\Goji\\Storage")) != "ok")
                    {
                        Console.WriteLine(res);
                    }

                    m_wcfc.StartWCFStreamingServer("c:\\Goji\\Storage");
                    if (m_wcfc.ConnectToFileServer(addr, "", "") != "ok")
                    {
                        Console.WriteLine("Error connecting to file server");
                        m_wcfc.Close();
                        return;
                    }
                    string[] list = m_wcfc.RefreshFileList();
                    for (int i = 0; i < list.Length; i++)
                    {
                        Console.WriteLine(list[i]);
                    }

                    m_wcfc.StartWatch("c:\\mpfmtests\\", "*.bin", true);
            




                    Console.WriteLine("Service started at: " + addr + ":" + 5000);
                    Console.ReadKey();
                    m_wcfc.Close();
                    
                }
                else
                {
                    throw (new SystemException("Service not started. error find suitable ip:" + addr));
                }
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

        public static IEnumerable<IPAddress> GetIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return (from ip in host.AddressList where !IPAddress.IsLoopback(ip) select ip).ToList();
        }
        static void Host_Faulted(object sender, EventArgs e)
        {
            //Console.WriteLine("Host faulted; reinitialising host");
            if (host != null)
                host.Abort();
        }

        static void Service_FileRequested(object sender, FileEventArgs e)
        {
            //Console.WriteLine(string.Format("File access\t{0}\t{1}", e.VirtualPath, DateTime.Now));
        }

        static void Service_FileUploaded(object sender, FileEventArgs e)
        {
            //Console.WriteLine(string.Format("File upload\t{0}\t{1}", e.VirtualPath, DateTime.Now));
        }

        static void Service_FileDeleted(object sender, FileEventArgs e)
        {
            //Console.WriteLine(string.Format("File deleted\t{0}\t{1}", e.VirtualPath, DateTime.Now));
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
