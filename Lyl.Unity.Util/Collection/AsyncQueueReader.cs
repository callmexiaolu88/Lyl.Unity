using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lyl.Unity.Util.AsyncResult;
using System.Threading;

namespace Lyl.Unity.Util.Collection
{
    class AsyncQueueReader<T> : ExAsyncResult, IQueueReader<T> where T:class
    {
        
        #region Private Static Filed
        
        static TimerCallback _TimerCallback = new TimerCallback(TimerCallback);

        #endregion Private Static Filed

        #region Private Filed

        bool _Expired;
        ExQueue<T> _InputQueue;
        T _Item;
        Timer _Timer;
        
        #endregion Private Filed

        #region Constuctor
        
        public AsyncQueueReader(ExQueue<T> inputQueue, TimeSpan timeout, AsyncCallback callback, object state)
            : base(callback, state)
        {
            _InputQueue = inputQueue;
            if (timeout!=TimeSpan.MaxValue)
            {
                _Timer = new Timer(AsyncQueueReader<T>._TimerCallback, this, timeout, TimeSpan.FromMilliseconds(-1));
            }
        }

        #endregion Constuctor

        #region Private Static Method

        private static void TimerCallback(object state)
        {
            AsyncQueueReader<T> thisPtr = (AsyncQueueReader<T>)state;
            if (thisPtr._InputQueue.RemoveReader(thisPtr))
            {
                thisPtr._Expired = true;
                thisPtr.Complete(false);
            }
        }

        #endregion Private Static Method

        #region Public Static Method
        
        public static bool End(IAsyncResult asyncResult,out T value)
        {
            AsyncQueueReader<T> readerResult = ExAsyncResult.End<AsyncQueueReader<T>>(asyncResult);
            if (readerResult._Expired)
            {
                value = default(T);
                return false;
            }
            else
            {
                value = readerResult._Item;
                return true;
            }
        }

        #endregion Public Static Method

        #region IQueueReader<T> 成员

        public void Set(ExItem<T> item)
        {
            this._Item = item.Data;
            if (_Timer!=null)
            {
                _Timer.Change(-1, -1);
            }
            Complete(false, item.Exception);
        }

        #endregion
    }
}
