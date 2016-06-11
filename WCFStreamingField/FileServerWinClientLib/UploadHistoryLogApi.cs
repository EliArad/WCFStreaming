using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileServerWinClientLib
{
    //0,c:\mpfmtests\22_04_2015_22_41_47_665\0\SENSOR_0.bin,true,2015-04-22 22:41:46.9640845,2015-04-22 22:41:48.6749266,2015-04-22 22:42:27.3801661,1,admin
    public class LineInfo
    {
        public int fileIndex { get; set; }
        public string fileName { get; set; }
        public bool uploaded { get; set; }
        public DateTime logStartTime { get; set; }
        public DateTime WatsonCreationTime { get; set; }
        public DateTime uploadDateTime { get; set; }
        public ulong g360Index { get; set; }
        public string userName { get; set; }
        public LineInfo(int _fileIndex,
                         string _fileName,
                         bool _uploaded,
                         DateTime _logStartTime,
                         DateTime _WatsonCreationTime,
                         DateTime _uploadDateTime,
                         ulong _g360Index,
                         string _userName)
        {
            fileIndex = _fileIndex;
            fileName = _fileName;
            uploaded = _uploaded;
            logStartTime = _logStartTime;
            WatsonCreationTime = _WatsonCreationTime;
            uploadDateTime = _uploadDateTime;
            g360Index = _g360Index;
            userName = _userName;
        }
    }
    public class UploadHistoryLogApi : IDisposable
    {
        string m_baseDir;
        Dictionary<string, bool> m_dic = new Dictionary<string, bool>();
        List<LineInfo> m_list = new List<LineInfo>();
        public UploadHistoryLogApi(string baseDir, bool load = true)
        {
            m_baseDir = baseDir;
            if (m_baseDir[m_baseDir.Length - 1] != '\\')
                m_baseDir = m_baseDir + "\\";
            if (load == true)
                LoadLogToMemory();
        }
        public List<LineInfo> HistoryList
        {
            get
            {
                return m_list;
            }
        }
        public Dictionary<string, bool> DicFileList
        {
            get
            {
                return m_dic;
            }
        }
        public void Dispose()
        {
            m_dic.Clear();
            m_list.Clear();
        }
        void LoadLogToMemory()
        {
            m_list.Clear();
            StreamReader sr = null;
            try
            {
                const string FMT = "yyyy-MM-dd HH:mm:ss.fffffff";
                //0,c:\mpfmtests\23_03_2015_13_28_17_100\0\SENSOR_0.bin,true,2015-03-23 13:28:15.1945103,2015-03-23 13:28:18.4106534,2015-03-23 15:10:10.7713241
                string[] fileEntries = Directory.GetFiles(m_baseDir);

                foreach (string fileName in fileEntries)
                {
                    if (fileName.Contains("FieldServerFiles") == true)
                    {
                        sr = new StreamReader(fileName);
                        while (true)
                        {
                            string line = sr.ReadLine();
                            if (line == null)
                                break;
                            string[] temp = line.Split(new Char[] { ',' });

                            DateTime d1 = DateTime.ParseExact(temp[3], FMT, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                            DateTime d2 = DateTime.ParseExact(temp[4], FMT, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                            DateTime d3 = DateTime.Now;
                            try
                            {
                                if (bool.Parse(temp[2]) == true)
                                    d3 = DateTime.ParseExact(temp[5], FMT, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                            }
                            catch (Exception err)
                            {

                            }

                            LineInfo t = new LineInfo(int.Parse(temp[0]), temp[1], bool.Parse(temp[2]), d1, d2, d3, ulong.Parse(temp[6]), temp[7]);
                            m_list.Add(t);
                        }
                    }
                }
                if (sr != null)
                    sr.Close();
            }
            catch (Exception err)
            {
                if (sr != null)
                    sr.Close();
                throw (new SystemException(err.Message));
            }
        }

        public void LoadLogToMemoryDic()
        {
            m_dic.Clear();
            StreamReader sr = null;
            try
            {
                const string FMT = "yyyy-MM-dd HH:mm:ss.fffffff";
                //0,c:\mpfmtests\23_03_2015_13_28_17_100\0\SENSOR_0.bin,true,2015-03-23 13:28:15.1945103,2015-03-23 13:28:18.4106534,2015-03-23 15:10:10.7713241
                string[] fileEntries = Directory.GetFiles(m_baseDir);

                foreach (string fileName in fileEntries)
                {
                    if (fileName.Contains("FieldServerFiles") == true)
                    {
                        sr = new StreamReader(fileName);
                        while (true)
                        {
                            string line = sr.ReadLine();
                            if (line == null)
                                break;
                            string[] temp = line.Split(new Char[] { ',' });

                            try
                            {
                                m_dic.Add(temp[1], bool.Parse(temp[2]));
                            }
                            catch (Exception err)
                            {

                            }
                        }
                    }
                }
                if (sr != null)
                    sr.Close();
            }
            catch (Exception err)
            {
                if (sr != null)
                    sr.Close();
                throw (new SystemException(err.Message));
            }
        }
        public int getTotalNumberOfFilesInLog(string userName, bool byUser)
        {
            int count = 0;
            if (byUser == false)
            {
                return m_list.Count;
            }
            else
            {
                for (int i = 0; i < m_list.Count; i++)
                {
                    LineInfo t = m_list[i];
                    if (byUser == true && t.userName == userName)
                        count++;
                }
                return count;
            }
        }
        public void GetStatistics(out int waitingToUpload,
                                  out int numberUploadedAlready,
                                  out double totalStorageSize,
                                  out double totalStorageWaitingToUploadSize,
                                  out double totalStorageUploadedSize,
                                  out int waitingToUploadDoesNotExists,
                                  out int numberOfUploadedFilesThatAreStillExistOnField,
                                  out int numberOfUploadedFilesThatAreNotExistOnField,
                                  out int waitingToUploadStilExistsOnField,
                                  string userName,
                                  bool byUser)
        {
            if (m_list.Count == 0)
            {
                waitingToUpload = -1;
                numberUploadedAlready = -1;
                totalStorageSize = -1;
                totalStorageWaitingToUploadSize = -1;
                totalStorageUploadedSize = -1;
                waitingToUploadDoesNotExists = -1;
                numberOfUploadedFilesThatAreStillExistOnField = -1;
                numberOfUploadedFilesThatAreNotExistOnField = -1;
                waitingToUploadStilExistsOnField = -1;
                return;
            }
            else
            {
                waitingToUpload = 0;
                numberUploadedAlready = 0;
                totalStorageSize = 0;
                totalStorageWaitingToUploadSize = 0;
                totalStorageUploadedSize = 0;
                waitingToUploadDoesNotExists = 0;
                numberOfUploadedFilesThatAreStillExistOnField = 0;
                numberOfUploadedFilesThatAreNotExistOnField = 0;
                waitingToUploadStilExistsOnField = 0;

                for (int i = 0; i < m_list.Count; i++)
                {
                    LineInfo t = m_list[i];
                    if (byUser == true && t.userName != userName)
                        continue;
                    if (t.uploaded == false)
                    {
                        waitingToUpload++;
                        if (File.Exists(t.fileName) == true)
                        {
                            FileInfo f = new FileInfo(t.fileName);
                            long s1 = f.Length;
                            totalStorageWaitingToUploadSize += s1;
                            waitingToUploadStilExistsOnField++;
                        }
                        else
                        {
                            waitingToUploadDoesNotExists++;
                        }
                    }
                    else
                    {
                        numberUploadedAlready++;
                        if (File.Exists(t.fileName) == true)
                        {
                            FileInfo f = new FileInfo(t.fileName);
                            long s1 = f.Length;
                            totalStorageUploadedSize += s1;
                            numberOfUploadedFilesThatAreStillExistOnField++;
                        }
                        else
                        {
                            numberOfUploadedFilesThatAreNotExistOnField++;
                        }

                    }
                    if (File.Exists(t.fileName) == true)
                    {
                        FileInfo f = new FileInfo(t.fileName);
                        long s1 = f.Length;
                        totalStorageSize += s1;
                    }
                }
            }
        }
        public void DeleteAllUploadedFiles(out int numberOfDeletedFiles, out int numberOfActualDeletedFiles, string password, string userName, bool byUser)
        {
            numberOfDeletedFiles = -1;
            numberOfActualDeletedFiles = -1;
            if (password != "123456")
            {
                throw (new SystemException("Invalid password: should be with 123456"));
            }
            numberOfActualDeletedFiles = 0;
            numberOfDeletedFiles = 0;
            for (int i = 0; i < m_list.Count; i++)
            {
                LineInfo t = m_list[i];
                if (byUser == true && t.userName != userName)
                    continue;
                if (t.uploaded == true)
                {
                    if (File.Exists(t.fileName) == true)
                    {
                        numberOfActualDeletedFiles++;
                        File.Delete(t.fileName);
                    }
                    numberOfDeletedFiles++;
                }
            }
        }


        //0,c:\goji\ToSend\1.bin,true,2015-03-26 17:41:33.4953985,2015-03-26 17:41:39.7943985,2015-03-26 17:41:42.3353985
        public string DeleteAllFilesFromFieldDaysBefore(int day, string password, string userName, bool byUser)
        {
            int numberOfActualDeletedFiles = 0;
            try
            {
                if (password != "123456")
                {
                    return "Invalid password: should be with 123456,0";
                }
                DateTime date = DateTime.Today;
                date = date.AddDays(-1);
                for (int i = 0; i < m_list.Count; i++)
                {
                    LineInfo t = m_list[i];
                    if (byUser == true && t.userName != userName)
                        continue;
                    if (File.Exists(t.fileName) == true && (t.WatsonCreationTime <= date.Date))
                    {
                        numberOfActualDeletedFiles++;
                        File.Delete(t.fileName);
                    }
                }
                return "ok," + numberOfActualDeletedFiles.ToString();
            }
            catch (Exception err)
            {
                return err.Message + "," + numberOfActualDeletedFiles.ToString();
            }
        }
        public string DeleteAllFilesFromFieldBetweenDates(DateTime start,
                                                          DateTime end,
                                                          bool includeTime,
                                                          string password,
                                                          string userName,
                                                          bool byUser)
        {
            int numberOfActualDeletedFiles = 0;
            try
            {
                if (password != "123456")
                {
                    throw (new SystemException("Invalid password: should be with 123456"));
                }
                if (includeTime == false)
                {
                    for (int i = 0; i < m_list.Count; i++)
                    {
                        LineInfo t = m_list[i];
                        if (byUser == true && t.userName != userName)
                            continue;
                        if (File.Exists(t.fileName) == true && ((t.WatsonCreationTime >= start.Date) && (t.WatsonCreationTime <= end.Date)))
                        {
                            numberOfActualDeletedFiles++;
                            File.Delete(t.fileName);
                        }
                    }
                }
                else
                {


                }
                return "ok," + numberOfActualDeletedFiles.ToString();
            }
            catch (Exception err)
            {
                return err.Message + "," + numberOfActualDeletedFiles.ToString();
            }
        }
        public string DeleteAllFilesFromFieldStartingFromDate(DateTime startingFrom, string password, string userName, bool byUser)
        {
            int numberOfActualDeletedFiles = 0;
            try
            {
                if (password != "123456")
                {
                    throw (new SystemException("Invalid password: should be with 123456"));
                }
                for (int i = 0; i < m_list.Count; i++)
                {
                    LineInfo t = m_list[i];
                    if (byUser == true && t.userName != userName)
                        continue;
                    if ((File.Exists(t.fileName) == true) && (t.WatsonCreationTime >= startingFrom))
                    {
                        numberOfActualDeletedFiles++;
                        File.Delete(t.fileName);
                    }
                }
                return "ok," + numberOfActualDeletedFiles.ToString();
            }
            catch (Exception err)
            {
                return err.Message + "," + numberOfActualDeletedFiles.ToString();
            }
        }

        public string DeleteAllWatsonGenerateFilesTimeSpanBeforeNow(DateTime time, string userName, bool byUser)
        {

            int numberOfActualDeletedFiles = 0;
            try
            {               
                for (int i = 0; i < m_list.Count; i++)
                {
                    LineInfo t = m_list[i];
                    if (byUser == true && t.userName != userName)
                        continue;
                    if ((File.Exists(t.fileName) == true) && (t.WatsonCreationTime <= time))
                    {
                        numberOfActualDeletedFiles++;
                        File.Delete(t.fileName);
                    }
                }
                return "ok," + numberOfActualDeletedFiles.ToString();
            }
            catch (Exception err)
            {
                return err.Message + "," + numberOfActualDeletedFiles.ToString();
            }
        }

        public string DeleteAllUploadedFilesTimeSpanBeforeNow(DateTime time, string userName, bool byUser)
        {
            int numberOfActualDeletedFiles = 0;
            try
            {
                for (int i = 0; i < m_list.Count; i++)
                {
                    LineInfo t = m_list[i];
                    if (byUser == true && t.userName != userName)
                        continue;
                    if ((File.Exists(t.fileName) == true) && t.uploaded && (t.WatsonCreationTime >= time))
                    {
                        numberOfActualDeletedFiles++;
                        File.Delete(t.fileName);
                    }
                }
                return "ok," + numberOfActualDeletedFiles.ToString();
            }
            catch (Exception err)
            {
                return err.Message + "," + numberOfActualDeletedFiles.ToString();
            }
             
        }
    }
}
