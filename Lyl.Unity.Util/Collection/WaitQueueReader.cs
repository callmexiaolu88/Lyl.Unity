using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Lyl.Unity.Util.Collection
{
    class WaitQueueReader<T>:IQueueReader<T> where T:class
    {
        #region Private Filed

        Exception _Exception;
        ExQueue<T> _InputQueue;
        T _Item;
        ManualResetEvent _WaitHandler;
        object _LockObject = new object();

        #endregion Private Filed

        #region Constructor

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="inputQueue">输入队列</param>
        public WaitQueueReader(ExQueue<T> inputQueue)
        {
            _InputQueue = inputQueue;
            _WaitHandler = new ManualResetEvent(false);
        }

        #endregion Constructor

        #region IQueueReader 成员

        public void Set(ExItem<T> item)
        {
            lock (_LockObject)
            {
                Debug.Assert(this._Item == null, "InputQueue.WaitQueueReader.Set: (this.item == null)");
                Debug.Assert(this._Exception == null, "InputQueue.WaitQueueReader.Set: (this.exception == null)");

                this._Exception = item.Exception;
                this._Item = item.Data;
                _WaitHandler.Set();
            }
        }

        #endregion

        #region Public Method

        public bool Wait(TimeSpan timeout, out T value)
        {
            bool safeClose = false;
            try
            {
                if (timeout == TimeSpan.MaxValue)
                {
                    _WaitHandler.WaitOne();
                }
                else if (!_WaitHandler.WaitOne(timeout, false))
                {
                    if (this._InputQueue.RemoveReader(this))
                    {
                        value = default(T);
                        safeClose = true;
                        return false;
                    }
                    else
                    {
                        _WaitHandler.WaitOne();
                    }
                }
                safeClose = true;
            }
            finally
            {
                if (safeClose)
                {
                    _WaitHandler.Close();
                }
            }
            value = this._Item;
            return true;
        }

        #endregion Public Method
    }
}
