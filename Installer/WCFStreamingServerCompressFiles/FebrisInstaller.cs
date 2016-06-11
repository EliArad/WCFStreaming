using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ionic.Zip;
using IWshRuntimeLibrary;
using System.IO;
using System.Threading;

namespace CompresFiles
{
   public class FebrisInstaller
   {
      protected const string m_production = "Release";
      static protected ZipFile m_zip;
      static string ZipFileToCreate = "setup.zip";
      static protected bool m_32bit = false;
      public struct Shortcut
      {
         public string fileName;
         public string Path;
         public string IconPath;
         public string ShortcutName;
      }

      static public event ZipProgress Tick;
      public EventArgs e = null;
      public delegate void ZipProgress(int count, string fileName);
     
       
      static protected List<Shortcut> m_appShortcuts = new List<Shortcut>();
      public FebrisInstaller()
      {
         m_zip = new ZipFile();
      }
 
      protected virtual void Compress()
      {
      }

      protected virtual void AddShortcut()
      {
      }
      

      protected static void AddingFile(string [] filenames)
      {
         foreach (String filename in filenames)
         {
            ZipEntry e = m_zip.AddFile(filename);
            //Tick(1, filename);
         }
      }

      public virtual string[] Names
      {
          get
          {
              return null;
          }
      }
      static private void _DeleteDirectory(string path)
      {
         if (Directory.Exists(path))
         {
            //Delete all files from the Directory
            foreach (string file in Directory.GetFiles(path))
            {
               System.IO.File.Delete(file);
            }
            //Delete all child Directories
            foreach (string directory in Directory.GetDirectories(path))
            {
               _DeleteDirectory(directory);
            }
            //Delete a Directory
            Directory.Delete(path);
         }
      }
      static public void Set32Bit(bool set)
      {
         m_32bit = set;
      }
      static public void Unzip(bool delete = false)
      {

         try
         {
            string zipToUnpack = "setup.zip";
            string unpackDirectory = "c:\\program files\\Goji Solutions";
            
          
            using (ZipFile zip1 = ZipFile.Read(zipToUnpack))
            {
               Tick(zip1.Count, "Excract");
               // here, we extract every entry, but we could extract conditionally
               // based on entry name, size, date, checkbox status, etc.  



               foreach (ZipEntry e in zip1)
               {
                   if (delete)
                   {
                       if (System.IO.File.Exists(e.FileName))
                          System.IO.File.Delete(e.FileName);   
                   }
                  e.Extract(unpackDirectory, ExtractExistingFileAction.OverwriteSilently);
                  Tick(1, Path.GetFileName(e.FileName));
               }
            }
         }
         catch (Exception err)
         {
            throw (new SystemException(err.Message));
         }
      }

      static bool processIsRunning(string process)
      {
          return System.Diagnostics.Process.GetProcesses().Any(p => p.ProcessName.Contains(process));
      }
            
      static public void Zip(string targetPath = "")
      {
         int[] size = new int[1];
         FebrisInstaller[] installer = { 
                                          new GojiWCFStreamingServer(out size[0]),
                                       };

         int fileCount = 0;
         for (int i = 0; i < size.Length; i++)
         {
            fileCount += size[i];
         }


         Tick(fileCount , string.Empty);
        
         try
         {
            for (int i = 0; i < installer.Length; i++)
            {
               installer[i].Compress();
            }

            string outputDir = m_production; 
            string targetPath1;
            if (targetPath != "")
            {
               targetPath1 = targetPath + outputDir + "\\" + ZipFileToCreate;
            }
            else
            {
               targetPath = ZipFileToCreate;
               targetPath1 = targetPath;
            }
            m_zip.Save(@"C:\GojiLTDTrunk\WCF Streaming\Installer\WCFStreamingServerInstaller\bin\Release\" + ZipFileToCreate);
         }
         catch (Exception err)
         {
            throw (new SystemException(err.Message));
         }
      }

       
   }
}
