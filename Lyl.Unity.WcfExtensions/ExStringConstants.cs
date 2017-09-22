using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyl.Unity.WcfExtensions
{
    static class ExStringConstants
    {
        internal const string Scheme = "fitsco.udp";
        internal const string UdpBindingSectionName = "system.serviceModel/bindings/udpBinding";
        internal const string OrderedSession = "orderedSession";
        internal const string ReliableSessionEnabled = "reliableSessionEnabled";
        internal const string SessionInactivityTimeout = "sessionInactivityTimeout";
        internal const string ClientBaseAddress = "clientBaseAddress";
        internal const string OneWayWcf = "oneWayWcf";
    }
}
