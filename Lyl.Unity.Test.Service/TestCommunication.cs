using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lyl.Unity.Interface;

namespace Lyl.Unity.Test.Service
{
    class TestCommunication : ITestCommunication
    {
        #region ITestCommunication 成员

        public int Send(byte[] bytes)
        {
            return bytes.Length;
        }

        public byte[] Receive()
        {
            return Encoding.UTF8.GetPreamble();
        }

        public int Add(int p1, int p2)
        {
            return p1 + p2;
        }

        #endregion
    }
}
