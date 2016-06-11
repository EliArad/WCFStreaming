using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;

namespace ServiceLoggerApi
{
    public enum LOGGER_NAMES
    {
        WATSON_SERVICE,
        VNA_SERVICE,
        MPFMSERV,
        GOJIMANAGER,
        WATSON_API_MANAGER,
        WATSONSCRIPT,
        WATSONBINSCRIPT,
        GOJI_WCF_STREAMING_CLIENT,
        GOJI_WCF_STREAMING_SERVICE,
    }
    public static class ServiceLogger
    {

        static Dictionary<LOGGER_NAMES, string> m_dic = new Dictionary<LOGGER_NAMES, string>();

        public static void init()
        {
            try
            {
                if (m_dic.Count == 0)
                {
                    m_dic.Add(LOGGER_NAMES.GOJI_WCF_STREAMING_CLIENT, "c:\\gojiwcfstreamingclient.txt");                 
                    
                }
            }
            catch (Exception er)
            {

            }
        }

        public static void WriteLine(LOGGER_NAMES name, string msg)
        {
            try
            {
                init();
                FileStream fs = new FileStream(m_dic[name], FileMode.Append, FileAccess.Write); 
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine(msg);
                    sw.Close();
                }
            }
            catch (Exception err)
            {
                 
            }
        }        
        public static void Write(LOGGER_NAMES name,string msg)
        {
            try
            {
                init();
                FileStream fs = new FileStream(m_dic[name], FileMode.Append, FileAccess.Write);
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(msg);
                    sw.Close();
                }
            }
            catch (Exception err)
            {
                 
            }
        }
        
        public static void Delete(LOGGER_NAMES name)
        {
            try
            {
                if (File.Exists(m_dic[name]) == true)
                File.Delete(m_dic[name]);
            }
            catch (Exception err)
            {

            }
        }
    }
}
