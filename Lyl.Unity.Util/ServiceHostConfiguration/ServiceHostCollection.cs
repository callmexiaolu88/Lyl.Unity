using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Lyl.Unity.Util.ServiceHostConfiguration
{
    public class ServiceHostCollection : Collection<ServiceHost>, IDisposable
    {
        public ServiceHostCollection()
        {
            var settings = BatchingHostingSetting.GetSection();
            foreach (ServiceTypeElement stElement in settings.ServiceTypes)
            {
                this.Add(stElement.ServiceType);
            }
        }

        public ServiceHostCollection(params Type[] serviceTypes)
            : this()
        {
            Add(serviceTypes);
        }

        public void Add(params Type[] serviceTypes)
        {
            if (serviceTypes != null)
            {
                Array.ForEach(serviceTypes, st => this.Add(new ServiceHost(st)));
            }
        }

        public void Open()
        {
            foreach (var host in this)
            {
                host.Open();
            }
        }

        public void Close()
        {
            foreach (var host in this)
            {
                host.Close();
            }
        }

        #region IDisposable 成员

        public void Dispose()
        {
            foreach (IDisposable host in this)
            {
                host.Dispose();
            }
        }

        #endregion
    }
}
