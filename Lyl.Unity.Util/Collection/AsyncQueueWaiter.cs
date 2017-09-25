using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lyl.Unity.Util.AsyncResult;

namespace Lyl.Unity.Util.Collection
{
    class AsyncQueueWaiter : ExAsyncResult, IQueueWaiter
    {
        #region Private Static Filed

        static TimerCallback _TimerCallback = new TimerCallback(TimerCallback);

        #endregion Private Static Filed

        #region Private Filed

        bool _ItemAvailable;
        object _LockObject = new object();
        Timer _Timer;

        #endregion Private Filed

        #region Constructor
        
        public AsyncQueueWaiter(TimeSpan timeout, AsyncCallback callback, object state)
            : base(callback, state)
        {
            if (timeout != TimeSpan.MaxValue)
            {
                this._Timer = new Timer(_TimerCallback, this, timeout, TimeSpan.FromMilliseconds(-1));
            }
        }

        #endregion Constructor

        #region Private Static Method

        private static void TimerCallback(object state)
        {
            AsyncQueueWaiter thisPtr = (AsyncQueueWaiter)state;
            thisPtr.Complete(false);
        }

        #endregion Private Static Method

        #region Public Static Method

        public static bool End(IAsyncResult asyncResult)
        {
            AsyncQueueWaiter waiterResult = ExAsyncResult.End<AsyncQueueWaiter>(asyncResult);
            return waiterResult._ItemAvailable;
        }

        #endregion Public Static Method

        #region IQueueWaiter 成员

        public void Set(bool itemAvailable)
        {
            bool timely = false;
            lock (_LockObject)
            {
                if (_Timer == null || _Timer.Change(-1, -1))
                    timely = true;
                this._ItemAvailable = itemAvailable;
            }
            if (timely)
            {
                Complete(false);
            }
        }

        #endregion
    }
}
