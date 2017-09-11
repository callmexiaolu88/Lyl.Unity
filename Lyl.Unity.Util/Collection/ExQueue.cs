using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace Lyl.Unity.Util.Collection
{
    /// <summary>
    /// InputQueue.Dequeue时的回调
    /// </summary>
    delegate void ItemDequeuedCallback();

    /// <summary>
    /// 队列状态
    /// </summary>
    enum QueueState
    {
        Open,
        Shutdown,
        Close,
    }

    /// <summary>
    /// 输入输出队列
    /// </summary>
    /// <typeparam name="T">队列项类型</typeparam>
    public class ExQueue<T> : IDisposable where T : class
    {
        
        #region Private Filed
        
        ExItemQueue<T> _ItemQueue;
        Queue<IQueueReader<T>> _QueueReader;
        List<IQueueWaiter> _WaiterList;
        QueueState _QueueState;

        static WaitCallback _OnInvokeDequeueCallback;
        static WaitCallback _OnDispatchCallback;
        static WaitCallback _CompleteOutstandingReadersCallback;
        static WaitCallback _CompleteWaitersFalseCallback;
        static WaitCallback _CompleteWaitersTrueCallback;

        #endregion Private Filed
        
        #region Constructor
        
        public ExQueue()
        {
            _ItemQueue = new ExItemQueue<T>();
            _QueueReader = new Queue<IQueueReader<T>>();
            _WaiterList = new List<IQueueWaiter>();
            _QueueState = QueueState.Open;
        }

        #endregion Constructor

        #region Private Property

        private object LockObject { get { return _ItemQueue; } }

        #endregion Private Property

        #region Internal Method

        internal bool RemoveReader(IQueueReader<T> reader)
        {
            lock (LockObject)
            {
                
            }
        }

        #endregion Internal Method

        #region Public Async Method

        public IAsyncResult BeginDequeue()
        {
            return null;
        }

        #endregion Public Async Method

        #region Public Method


        #endregion Public Method

        #region Public Static Method



        #endregion Public Static Method

        #region IDisposable 成员

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion

    }

}
