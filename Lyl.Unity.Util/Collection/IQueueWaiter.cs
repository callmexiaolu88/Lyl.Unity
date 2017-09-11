using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyl.Unity.Util.Collection
{
    interface IQueueWaiter
    {
        void Set(bool itemAvailable);
    }
}
