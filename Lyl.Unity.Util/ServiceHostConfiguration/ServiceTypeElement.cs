using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lyl.Unity.Util.ServiceHostConfiguration;

namespace Lyl.Unity.Util.ServiceHostConfiguration
{
    /// <summary>
    /// 服务类型元素
    /// </summary>
    sealed class ServiceTypeElement : ConfigurationElement
    {
        [ConfigurationProperty(ServiceHostConfigConst.Type, IsRequired = true)]
        [TypeConverter(typeof(TypeNameConverter))]
        public Type ServiceType
        {
            get { return (Type)this[ServiceHostConfigConst.Type]; }
            set { this[ServiceHostConfigConst.Type] = value; }
        }
    }
}
