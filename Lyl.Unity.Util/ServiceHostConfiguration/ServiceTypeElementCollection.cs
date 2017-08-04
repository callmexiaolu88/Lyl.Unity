using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyl.Unity.Util.ServiceHostConfiguration
{
    sealed class ServiceTypeElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ServiceTypeElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            ServiceTypeElement ele = (ServiceTypeElement)element;
            return ele.ServiceType.MetadataToken;
        }
    }
}
