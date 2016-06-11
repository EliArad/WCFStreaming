using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ionic.Zip;


namespace CompresFiles
{
   public class DropBoxService : FebrisInstaller
   {
       const string path = @"C:\GojiLTDTrunk\WCF Streaming\GojiDropBoxService\bin\" + m_production;
      // note: this does not recurse directories! 
      String[] filenames = { 

                    path + " \\AppLimit.CloudComputing.SharpBox.dll",
                    path + " \\FileServer.Services.dll",
                    path + " \\FileServerWinClientLib.dll",
                    path + " \\GojiDropBoxService.exe",
                    path + " \\Newtonsoft.Json.Net40.dll",
                    path + " \\MPFMCommonDefenitions.dll",
                    path + " \\ServiceLibrary.dll",
     };

      public DropBoxService(out int size)
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
