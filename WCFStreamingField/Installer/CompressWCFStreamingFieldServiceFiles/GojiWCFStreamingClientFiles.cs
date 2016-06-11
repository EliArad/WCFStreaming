using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ionic.Zip;


namespace CompresFiles
{
   public class GojiWCFStreamingClient : FebrisInstaller
   {
       const string path = @"C:\GojiLTDTrunk\WCF Streaming\GojiWCFStreamingField\GojiWCFStreamingClient\bin\" + m_production;
      // note: this does not recurse directories! 
      String[] filenames = { 
                               
                    path + "\\AppLimit.CloudComputing.SharpBox.dll",
                    path + "\\FileServerWinClientLib.dll",
                    path + "\\GojiWCFStreamingClient.exe",
                    path + "\\GojiWCFStreamingClient.exe.config",
                    path + "\\MPFMCommonDefenitions.dll",
                    path + "\\Newtonsoft.Json.Net40.dll",
                    path + "\\ServiceLibrary.dll",
                    path + "\\WCFFileRepositoryFieldClient.dll",

                           };

      public GojiWCFStreamingClient(out int size)
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
