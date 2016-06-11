using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using AppLimit.CloudComputing.SharpBox;
using AppLimit.CloudComputing.SharpBox.StorageProvider.DropBox;


namespace DropBoxSharpBoxApi
{
    public class DropBoxSharpBox
    {
        string strAccessToken = @"c:\SharpDropBoxGojiElia.Token";
        ICloudDirectoryEntry m_entryRoot;
        // Creating the cloudstorage object 
        CloudStorage m_dropBoxStorage = new CloudStorage();
        bool m_initialize = false;
        public DropBoxSharpBox(string TokenAccessFile = "")
        {
            if (File.Exists(TokenAccessFile))
            {
                strAccessToken = TokenAccessFile;
            }
        }
        public string Initialize()
        {
            if (m_initialize == true)
            {
                return "ok";
            }
            try
            {
                // get the configuration for dropbox 
                var dropBoxConfig = CloudStorage.GetCloudConfigurationEasy(nSupportedCloudConfigurations.DropBox);

                // declare an access token
                ICloudStorageAccessToken accessToken = null;
                // load a valid security token from file
                using (FileStream fs = File.Open(strAccessToken, FileMode.Open, FileAccess.Read, FileShare.None))
                    accessToken = m_dropBoxStorage.DeserializeSecurityToken(fs);

                // open the connection 
                var storageToken = m_dropBoxStorage.Open(dropBoxConfig, accessToken);
                m_entryRoot = m_dropBoxStorage.GetRoot();
                m_initialize = true;
                return "ok";
            }
            catch (Exception err)
            {
                m_initialize = false;
                return err.Message;
            }
        }
        public bool Initialized
        {
            get
            {
                return m_initialize;
            }
        }
        public string Upload(string FileName)
        {
            if (m_initialize == false)
                return "not initialized";
            try
            {
                var fileEntry = m_dropBoxStorage.UploadFile(FileName, m_entryRoot);
                return "ok";
            }
            catch (Exception err)
            {
                return err.Message;
            }

        }
        public void Close()
        {
            try
            {
                // close the connection 
                m_dropBoxStorage.Close();
                m_initialize = false;
            }
            catch (Exception err)
            {
                m_initialize = false;
            }
        }
    }
}
