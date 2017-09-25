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
            Console.Out.WriteLine("Testing Udp From Config.");

            ServiceHost service = new ServiceHost(typeof(TestCommunication));
            service.Open();

            Console.WriteLine("Service is started from config...");
            Console.WriteLine("Press <ENTER> to terminate the service and exit...");
            Console.ReadLine();

            service.Close();
        }
    }
}
