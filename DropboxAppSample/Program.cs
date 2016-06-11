using System;
using System.Linq;
using System.Text;
using System.IO;
using AppLimit.CloudComputing.SharpBox;
using AppLimit.CloudComputing.SharpBox.StorageProvider.DropBox;

namespace DropboxAppSample
{
    class Program
    {
        const string strAccessToken = @"c:\SharpDropBoxGojiElia.Token";

        static void Main(string[] args)
        {
            // Creating the cloudstorage object 
            var dropBoxStorage = new CloudStorage();

            // get the configuration for dropbox 
            var dropBoxConfig = CloudStorage.GetCloudConfigurationEasy(nSupportedCloudConfigurations.DropBox);

            // declare an access token
            ICloudStorageAccessToken accessToken = null;
            // load a valid security token from file
            using (FileStream fs = File.Open(strAccessToken, FileMode.Open, FileAccess.Read, FileShare.None))
                accessToken = dropBoxStorage.DeserializeSecurityToken(fs);

            // open the connection 
            var storageToken = dropBoxStorage.Open(dropBoxConfig, accessToken);


            // 
            // do what ever you want to 
            //

            // get a specific directory in the cloud storage, e.g. /Public 
            var root = dropBoxStorage.GetRoot();

            /*
            foreach (var fof in root) 
            { 
                // check if we have a directory 
                Boolean bIsDirectory = fof is ICloudDirectoryEntry; 
                // output the info 
                Console.WriteLine("{0}: {1}", bIsDirectory ? "DIR" : "FIL", fof.Name ); 
            }
            */
            // Folder
            //var folderEntryQq = dropBoxStorage.GetFolder("/Qq");
            //var folderEntryQqChild = dropBoxStorage.CreateFolder("QqChild", folderEntryQq);
            //var fileEntry = dropBoxStorage.UploadFile(@"C:\msdia80.dll", folderEntryQqChild);

            // Root
            var entryRoot = dropBoxStorage.GetRoot();
            var fileEntry = dropBoxStorage.UploadFile(@"C:\mpfmservicelog.txt", entryRoot);

            // close the connection 
            dropBoxStorage.Close();
        }
    }
}


