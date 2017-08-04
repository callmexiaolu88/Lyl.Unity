using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyl.Unity.Util.ServiceHostConfiguration
{
    /// <summary>
    /// 批量服务类型节点
    /// </summary>
    sealed class BatchingHostingSetting : ConfigurationSection
    {
        [ConfigurationProperty(ServiceHostConfigConst.Empty, IsDefaultCollection = true)]
        public ServiceTypeElementCollection ServiceTypes
        {
            get { return (ServiceTypeElementCollection)this[ServiceHostConfigConst.Empty]; }
        }

        public static BatchingHostingSetting GetSection(string sectionName = null)
        {
            var name = string.IsNullOrEmpty(sectionName) ? ServiceHostConfigConst.BatchSectionName : sectionName;
            return ConfigurationManager.GetSection(name) as BatchingHostingSetting;
        }
    }
}
