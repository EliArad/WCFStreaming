using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Globalization;

namespace QueueBaseFileListApi
{
    public class QueueBaseFileListReader : QueueBaseFileListWriter
    {
        const string FMT = "yyyy-MM-dd HH:mm:ss.fffffff";
        public QueueBaseFileListReader()
        {
            throw (new SystemException("error"));
        }
        public QueueBaseFileListReader(ulong maxInFile, string dir)
        {
            throw (new SystemException("error"));
        }

        public QueueBaseFileListReader(ulong maxInFile, string readDir, string writeDir)
        {
            m_dir = readDir;
            m_writeDir = writeDir;
            m_maxListSize = maxInFile;
            Directory.CreateDirectory(m_dir);

            if (File.Exists(m_dir + "CurFileNum.txt") == false)
            {
                WriteCurFileNum(0);
            }
            if (File.Exists(m_dir + "CurListFileNum.txt") == false)
            {
                writeCurListFileNum(0);
            }
        }

        public void RemoveFileQueueLists()
        {
            try
            {
                if (Directory.Exists(m_dir))
                {
                    string[] arrayList = Directory.GetFiles(m_dir, "*.txt");
                    foreach (var f in arrayList)
                    {
                        File.Delete(f);
                    }
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }

        public string GetFile(out ulong listNum , 
                              out ulong fileNumber, 
                              out bool copyStatus, 
                              out DateTime startTime, 
                              out DateTime ?watsonDateTime,
                              out ulong g360Index,
                              out string userName)
        {
            while (Monitor.TryEnter(_locker) == false)
            {
                Thread.Sleep(1000);
            }
            watsonDateTime = null;
            copyStatus = false;
            ulong number = readCurListFileNum();
            listNum = number;
            ulong n = readCurFileNum();
            fileNumber = n;
            string fieldServerList = m_writeDir + "FieldServerFiles_" + number.ToString("000000000") + " .txt";
            if (n < m_maxListSize)
            {
                ulong n1 = n;
                if (File.Exists(fieldServerList) == false)
                {
                    startTime = DateTime.Now;
                    g360Index = 0;
                    userName = string.Empty;
                    return null;
                }
                string[] line = File.ReadAllLines(fieldServerList);

                string[] s = line[n].Split(new Char[] { ',' });
                copyStatus = (s[2].ToLower() == "false" ? false : true);
                startTime = DateTime.ParseExact(s[3], FMT, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                watsonDateTime = DateTime.ParseExact(s[4], FMT, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);

                g360Index = ulong.Parse(s[6]);
                userName = s[7];
                
                Monitor.Exit(_locker);
                return s[1];
            }
            else
            {
                n = 0;
                WriteCurFileNum(n);
                number += 1;
                writeCurListFileNum(number);
                fieldServerList = m_writeDir + "FieldServerFiles_" + number.ToString("000000000") + " .txt";
                ulong n1 = n;
                string[] line = File.ReadAllLines(fieldServerList);

                string[] s = line[n].Split(new Char[] { ',' });
                copyStatus = (s[2].ToLower() == "false" ? false : true);
                startTime = DateTime.ParseExact(s[3], FMT, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                watsonDateTime = DateTime.ParseExact(s[4], FMT, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);

                g360Index = ulong.Parse(s[6]);
                userName = s[7];
                Monitor.Exit(_locker);
                return line[n1];
            }
          
        }
        public void MarkAsSent()
        {
            while (Monitor.TryEnter(_locker) == false)
            {
                Thread.Sleep(1000);
            }
            ulong number = readCurListFileNum();
            ulong n = readCurFileNum();
            string fieldServerList = m_writeDir + "FieldServerFiles_" + number.ToString("000000000") + " .txt";
            string[] lines = File.ReadAllLines(fieldServerList);

            string[] s = lines[n].Split(new Char[] { ',' });

            string uploadTimeStr = DateTime.Now.ToString(FMT);

            string mark = s[0] + "," + s[1] + "," + "true" + "," + s[3] + "," + s[4] + "," + uploadTimeStr + "," + s[6] + "," + s[7];
            lines[n] = mark;
            File.WriteAllLines(fieldServerList , lines);
            n++;
            WriteCurFileNum(n);
            
            Monitor.Exit(_locker);
        }       
    }
}
