using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Lyl.Unity.Interface
{
    [ServiceContract]
    public interface ITestCommunication
    {
        [OperationContract]
        int Send(byte[] bytes);

        [OperationContract]
        byte[] Receive();

        [OperationContract]
        int Add(int p1, int p2);
    }
}
