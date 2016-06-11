using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLibrary
{

  [ServiceContract]
  public interface IDuplexServiceCallback {

    [OperationContract(IsOneWay = true)]
    void NotifyCallbackMessage(string fieldGuid ,string ipAddress, int code, string Msg, DateTime startTime, string userName);

    [OperationContract(IsOneWay = true)]
    void NotifyDataCallback(string fieldGuid, string ipAddress, byte[] buf, int size, string userName);
       
  }
}
