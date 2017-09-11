using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Lyl.Unity.Util.Collection
{
    class WaitQueueWaiter:IQueueWaiter
    {
        
        #region Private Filed
        
        bool _ItemAvailable;
        ManualResetEvent _WaitHandler;
        object _LockObject = new object();

        #endregion Private Filed
        
        #region Constructor
        
        public WaitQueueWaiter()
        {
            _WaitHandler = new ManualResetEvent(false);
        }

        #endregion Constructor

        #region IQueueWaiter 成员

        public void Set(bool itemAvailable)
        {
            lock (_LockObject)
            {
                _ItemAvailable = itemAvailable;
                _WaitHandler.Set();
            }
        }

        #endregion IQueueWaiter 成员

        #region Public Method
        
        public bool Wait(TimeSpan timeout)
        {
            if (timeout == TimeSpan.MaxValue)
            {
                _WaitHandler.WaitOne();
            }
            else if(!_WaitHandler.WaitOne(timeout,false))
            {
                return false;
            }
            return _ItemAvailable;
        }

        #endregion Public Method

    }
}
