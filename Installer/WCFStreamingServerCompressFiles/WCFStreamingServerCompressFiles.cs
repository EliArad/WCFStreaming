using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ionic.Zip;


namespace CompresFiles
{
   public class GojiWCFStreamingServer : FebrisInstaller
   {
       const string path = @"C:\GojiLTDTrunk\WCF Streaming\GojiWCFStreamingServer\bin\" + m_production;
      // note: this does not recurse directories! 
      String[] filenames = { 

                path + " \\GojiWCFStreamingServer.exe",
                path + " \\FileServerWinClientLib.dll",
                path + " \\ServiceLibrary.dll",
                path + " \\GojiWCFStreamingServer.exe.config",
                path + " \\AppLimit.CloudComputing.SharpBox.dll",
                path + " \\Newtonsoft.Json.Net40.dll",
                path + " \\MPFMCommonDefenitions.dll",
                path + " \\FileServer.Services.dll",
                           };

      public GojiWCFStreamingServer(out int size)
      {
         size = filenames.Length;
      }
      public override string[] Names
      {
          get
          {
              return filenames;
          }
      }
      protected override void AddShortcut()
      {
      }
      protected override void Compress()
      {          
         try
         {
            AddingFile(filenames);
         }
         catch (Exception err)
         {
            throw (new SystemException(err.Message));
         }
      }          
   } 
}
