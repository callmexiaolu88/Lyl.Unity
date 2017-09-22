using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace Lyl.Unity.Test.Service
{
    class Program
    {
        static void Main(string[] args)
        {
            using (ServiceHost host = new ServiceHost(typeof(TestCommunication)))
            {
                host.Open();
                Console.Read();
            }
        }
    }
}
