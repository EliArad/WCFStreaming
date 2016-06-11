using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace QueueBaseFileListApi
{
    public class QueueBaseFileListWriter
    {
        protected object _locker = new object();
        protected string m_dir = string.Empty;
        protected string m_writeDir;

        protected ulong m_maxListSize;
        public QueueBaseFileListWriter()
        {

        }
        public QueueBaseFileListWriter(ulong maxInFile, string dir)
        {
            
            m_dir = dir;
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

            if (File.Exists(m_dir + "G360FileNum.txt") == false)
            {
                writeG360FileNum(1);
            }
        }

        public QueueBaseFileListWriter(ulong maxInFile, string readDir, string writeDir)
        {
            throw (new SystemException("error"));
        }
   
        public virtual ulong readCurListFileNum()
        {
            using (StreamReader sr = new StreamReader(m_dir + "CurListFileNum.txt"))
            {
                return ulong.Parse(sr.ReadLine());
            }
        }
        protected void IncG360Index(out ulong curG_Num , out ulong afterG_num)
        {
            try
            {
                ulong num = readG360FileNum();
                curG_Num = num;
                num += 1;
                WriteG360FileNum(num);
                afterG_num = num;
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
        protected virtual void writeG360FileNum(ulong num)
        {
            using (StreamWriter sw = new StreamWriter(m_dir + "G360FileNum.txt"))
            {
                sw.WriteLine(num);
            }
        }

        public virtual ulong readG360FileNum()
        {
            using (StreamReader sr = new StreamReader(m_dir + "G360FileNum.txt"))
            {
                return ulong.Parse(sr.ReadLine());
            }
        }
        protected virtual void WriteG360FileNum(ulong num)
        {
            using (StreamWriter sw = new StreamWriter(m_dir + "G360FileNum.txt"))
            {
                sw.WriteLine(num);
            }
        }

        protected virtual void writeCurListFileNum(ulong num)
        {
            using (StreamWriter sw = new StreamWriter(m_dir + "CurListFileNum.txt"))
            {
                sw.WriteLine(num);
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

        public virtual ulong readCurFileNum()
        {
            using (StreamReader sr = new StreamReader(m_dir + "CurFileNum.txt"))
            {
                return ulong.Parse(sr.ReadLine());
            }
        }
        protected virtual void WriteCurFileNum(ulong num)
        {
            using (StreamWriter sw = new StreamWriter(m_dir + "CurFileNum.txt"))
            {
                sw.WriteLine(num);
            }
        }

        public void AddFile(string fileName, DateTime startTime, DateTime watsonCreationTime, ulong g360Index, string UserName)
        {
            while (Monitor.TryEnter(_locker) == false)
            {
                Thread.Sleep(1000);
            }

            const string FMT = "yyyy-MM-dd HH:mm:ss.fffffff";
            string logStartTime = startTime.ToString(FMT);
            string strWatsonCreationTime = watsonCreationTime.ToString(FMT);



            ulong number = readCurListFileNum();
            string fieldServerList = m_dir + "FieldServerFiles_" + number.ToString("000000000") + " .txt";
            ulong n = readCurFileNum();
            if (n < m_maxListSize)
            {
                using (StreamWriter w = File.AppendText(fieldServerList))
                {
                    w.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7}", n, fileName, false, logStartTime, strWatsonCreationTime, "add_file_event", g360Index, UserName);
                }
                n++;
                WriteCurFileNum(n);
            }
            else
            {
                n = 0;
                WriteCurFileNum(n);

                number = readCurListFileNum();
                number++;
                writeCurListFileNum(number);

                fieldServerList = m_dir + "FieldServerFiles_" + number.ToString("000000000") + " .txt";

                using (StreamWriter w = File.AppendText(fieldServerList))
                {
                    w.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7}", n, fileName, false, logStartTime, strWatsonCreationTime, "add_file_event", g360Index, UserName);
                }
                n++;
                WriteCurFileNum(n);
            }
            Monitor.Exit(_locker);
        }

        public void AddFile(string fileName, DateTime startTime, DateTime watsonCreationTime, out ulong g360Index, string UserName)
        { 
            while (Monitor.TryEnter(_locker) == false)
            {
                Thread.Sleep(1000);
            }

            const string FMT = "yyyy-MM-dd HH:mm:ss.fffffff";
            string logStartTime = startTime.ToString(FMT);
            string strWatsonCreationTime = watsonCreationTime.ToString(FMT);
            


            ulong number = readCurListFileNum();
            string fieldServerList = m_dir + "FieldServerFiles_" + number.ToString("000000000") + " .txt";
            ulong n = readCurFileNum();
            if (n < m_maxListSize)
            {
                using (StreamWriter w = File.AppendText(fieldServerList))
                {
                    ulong a,b;
                    IncG360Index(out a, out b);
                    g360Index = a;
                    w.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7}",n, fileName, false, logStartTime, strWatsonCreationTime, "add_file_event", a , UserName);
                }
                n++;
                WriteCurFileNum(n);
            }
            else
            {
                n = 0;
                WriteCurFileNum(n);

                number = readCurListFileNum();
                number++;
                writeCurListFileNum(number);

                fieldServerList = m_dir + "FieldServerFiles_" + number.ToString("000000000") + " .txt";

                using (StreamWriter w = File.AppendText(fieldServerList))
                {
                    ulong a, b;
                    IncG360Index(out a, out b);
                    g360Index = a;
                    w.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7}",n, fileName, false, logStartTime, strWatsonCreationTime, "add_file_event", a, UserName);
                }
                n++;
                WriteCurFileNum(n);
            }
            Monitor.Exit(_locker);
        }
    }
}
