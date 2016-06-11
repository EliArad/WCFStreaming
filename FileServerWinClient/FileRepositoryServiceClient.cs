using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FileServer.Services;
using System.ServiceModel;
using System.IO;


namespace FileServerWinClientLib
{
	public class FileRepositoryServiceClient : ClientBase<IFileRepositoryService>, IFileRepositoryService, IDisposable
	{
		public FileRepositoryServiceClient()
			: base("FileRepositoryService")
		{
		}

        public FileRepositoryServiceClient(string endpointConfigurationName) :
            base(endpointConfigurationName)
        {
        }

        public FileRepositoryServiceClient(string endpointConfigurationName, string remoteAddress) :
            base(endpointConfigurationName, remoteAddress)
        {
        }

        public FileRepositoryServiceClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) :
            base(endpointConfigurationName, remoteAddress)
        {
        }

        public FileRepositoryServiceClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) :
            base(binding, remoteAddress)
        {
        }

		public System.IO.Stream GetFile(string virtualPath)
		{
            try
            {
                return base.Channel.GetFile(virtualPath);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
		}

		public void PutFile(FileUploadMessage msg)
		{
            try
            {
                base.Channel.PutFile(msg);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
		}

        public void PutFileWithPath(FileUploadMessage msg)
        {
            try
            {
                base.Channel.PutFileWithPath(msg);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }

		public void DeleteFile(string virtualPath)
		{
            try
            {
                base.Channel.DeleteFile(virtualPath);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
		}

		public StorageFileInfo[] List()
		{
            try
            {
                return List(null);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
		}

		public StorageFileInfo[] List(string virtualPath)
		{
            try
            {
                return base.Channel.List(virtualPath);
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
		}
        
		void IDisposable.Dispose()
		{
            try
            {
                if (this.State == CommunicationState.Opened)
                    this.Close();
            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
		}

	}
}
