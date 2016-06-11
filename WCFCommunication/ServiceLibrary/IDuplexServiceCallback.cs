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
    void NotifyCallbackMessage(string ipAddress, int code, string Msg);

    [OperationContract(IsOneWay = true)]
    void NotifyDataCallback(string ipAddress, byte[] buf, int size);
       
  }
}
