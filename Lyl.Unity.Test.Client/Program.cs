using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Lyl.Unity.Interface;

namespace Lyl.Unity.Test.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new ChannelFactory<ITestCommunication>("SampleProfileUdpBinding_ICalculatorContract");
            var channel = factory.CreateChannel();
            var result=channel.Add(1, 1);
            Console.WriteLine(result);
            Console.Out.WriteLine("Press <ENTER> to complete test.");
            Console.In.ReadLine();
        }
    }
}
