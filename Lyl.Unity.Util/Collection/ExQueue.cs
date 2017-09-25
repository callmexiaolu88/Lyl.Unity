using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using Lyl.Unity.Util.AsyncResult;

namespace Lyl.Unity.Util.Collection
{
    /// <summary>
    /// InputQueue.Dequeue时的回调
    /// </summary>
    public delegate void ItemDequeuedCallback();

    /// <summary>
    /// 队列状态
    /// </summary>
    enum QueueState
    {
        Open,
        Shutdown,
        Closed,
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

        #region Private Method

        private void getWaiters(out IQueueWaiter[] waiters)
        {
            if (_WaiterList.Count > 0)
            {
                waiters = _WaiterList.ToArray();
                _WaiterList.Clear();
            }
            else
            {
                waiters = null;
            }
        }

        private void enqueueAndDispatch(ExItem<T> item, bool canDispatchOnThisThread)
        {
            bool disposeItem = false;
            bool dispatchLater = false;
            bool itemAvailable = true;
            IQueueReader<T> reader = null;
            IQueueWaiter[] waiters = null;
            lock (LockObject)
            {
                itemAvailable = (_QueueState != QueueState.Closed && _QueueState != QueueState.Shutdown);
                this.getWaiters(out waiters);
                if (_QueueState == QueueState.Open)
                {
                    if (canDispatchOnThisThread)
                    {
                        if (_QueueReader.Count == 0)
                        {
                            _ItemQueue.EnqueueAvailableItem(item);
                        }
                        else
                        {
                            reader = _QueueReader.Dequeue();
                        }
                    }
                    else
                    {
                        if (_QueueReader.Count == 0)
                        {
                            _ItemQueue.EnqueueAvailableItem(item);
                        }
                        else
                        {
                            _ItemQueue.EnqueuePendingItem(item);
                            dispatchLater = true;
                        }
                    }
                }
                else
                {
                    disposeItem = true;
                }
            }

            if (waiters != null)
            {
                if (canDispatchOnThisThread)
                {
                    completeWaiters(itemAvailable, waiters);
                }
                else
                {
                    completeWaitersLater(itemAvailable, waiters);
                }
            }

            if (reader!=null)
            {
                invokeDequeueCallback(item.Callback);
                reader.Set(item);
            }

            if (dispatchLater)
            {
                if (_OnDispatchCallback==null)
                {
                    _OnDispatchCallback = onDispatchCallback;
                }
                ThreadPool.QueueUserWorkItem(_OnDispatchCallback, this);
            }
            else if (disposeItem)
            {
                invokeDequeueCallback(item.Callback);
                item.Dispose();
            }
        }

        // This does not block, however, Dispatch() must be called later if this function returns true.
        private bool enqueueWithoutDispatch(ExItem<T> item)
        {
            lock (LockObject)
            {
                if (_QueueState == QueueState.Open)
                {
                    if (_QueueReader.Count == 0)
                    {
                        _ItemQueue.EnqueueAvailableItem(item);
                        return false;
                    }
                    else
                    {
                        _ItemQueue.EnqueuePendingItem(item);
                        return true;
                    }
                }
            }

            invokeDequeueCallback(item.Callback);
            item.Dispose();
            return false;
        }

        #endregion Private Method

        #region Internal Method

        internal bool RemoveReader(IQueueReader<T> reader)
        {
            lock (LockObject)
            {
                if (_QueueState == QueueState.Open || _QueueState == QueueState.Shutdown)
                {
                    bool removed = false;
                    for (int i = _QueueReader.Count; i > 0; i--)
                    {
                        var tmp = _QueueReader.Dequeue();
                        if (object.ReferenceEquals(tmp, reader))
                        {
                            removed = true;
                        }
                        else
                        {
                            _QueueReader.Enqueue(tmp);
                        }
                    }
                    return removed;
                }
            }
            return false;
        }

        #endregion Internal Method

        #region Public Async Method

        public IAsyncResult BeginDequeue(TimeSpan timeout, AsyncCallback callback, object state)
        {
            ExItem<T> item = default(ExItem<T>);
            lock (LockObject)
            {
                if (_QueueState == QueueState.Open)
                {
                    if (_ItemQueue.HasAvailableItem)
                    {
                        item = _ItemQueue.DequeueAvailableItem();
                    }
                    else
                    {
                        AsyncQueueReader<T> reader = new AsyncQueueReader<T>(this, timeout, callback, state);
                        _QueueReader.Enqueue(reader);
                        return reader;
                    }
                }
                else if (_QueueState == QueueState.Shutdown)
                {
                    if (_ItemQueue.HasAvailableItem)
                    {
                        item = _ItemQueue.DequeueAvailableItem();
                    }
                    else if (_ItemQueue.HasAnyItem)
                    {
                        AsyncQueueReader<T> reader = new AsyncQueueReader<T>(this, timeout, callback, state);
                        _QueueReader.Enqueue(reader);
                        return reader;
                    }
                }
            }

            invokeDequeueCallback(item.Callback);
            return new DataCompleteAsyncResult<T>(item.GetData(), callback, state);
        }

        public IAsyncResult BeginWaitForItem(TimeSpan timeout, AsyncCallback callback, object state)
        {
            lock (LockObject)
            {
                if (_QueueState==QueueState.Open)
                {
                    if (!_ItemQueue.HasAvailableItem)
                    {
                        AsyncQueueWaiter waiter = new AsyncQueueWaiter(timeout, callback, state);
                        _WaiterList.Add(waiter);
                        return waiter;
                    }
                }
                else if (_QueueState == QueueState.Shutdown)
                {
                    if (!_ItemQueue.HasAvailableItem && _ItemQueue.HasAnyItem)
                    {
                        AsyncQueueWaiter waiter = new AsyncQueueWaiter(timeout, callback, state);
                        _WaiterList.Add(waiter);
                        return waiter;
                    }
                }
            }
            return new DataCompleteAsyncResult<bool>(true, callback, state);
        }

        public T EndDequeue(IAsyncResult result)
        {
            T value;
            if (!EndDequeue(result, out value))
            {
                throw new TimeoutException("Asynchronous Dequeue operation timed out.");
            }
            return value;
        }

        public bool EndDequeue(IAsyncResult result,out T value)
        {
            DataCompleteAsyncResult<T> dataResult = result as DataCompleteAsyncResult<T>;
            if (dataResult!=null)
            {
                value = DataCompleteAsyncResult<T>.End(result);
                return true;
            }
            return AsyncQueueReader<T>.End(result, out value);
        }

        public bool EndWaitForItem(IAsyncResult result)
        {
            DataCompleteAsyncResult<bool> dataResult = result as DataCompleteAsyncResult<bool>;
            if (dataResult!=null)
            {
               return DataCompleteAsyncResult<bool>.End(result);
            }
            return AsyncQueueWaiter.End(result);
        }

        #endregion Public Async Method

        #region Public Method

        public T Dequeue(TimeSpan timeout)
        {
            T value;
            if (!Dequeue(timeout,out value))
            {
                throw new TimeoutException(string.Format("Dequeue timed out in {0}.", timeout));
            }
            return value;
        }

        public bool Dequeue(TimeSpan timeout,out T value)
        {
            WaitQueueReader<T> reader = null;
            ExItem<T> item = new ExItem<T>();
            lock (LockObject)
            {
                if (_QueueState==QueueState.Open)
                {
                    if (_ItemQueue.HasAvailableItem)
                    {
                        item = _ItemQueue.DequeueAvailableItem();
                    }
                    else
                    {
                        reader = new WaitQueueReader<T>(this);
                        _QueueReader.Enqueue(reader);
                    }
                }
                else if (_QueueState == QueueState.Shutdown)
                {
                    if (_ItemQueue.HasAvailableItem)
                    {
                        item = _ItemQueue.DequeueAvailableItem();
                    }
                    else if (_ItemQueue.HasAnyItem)
                    {
                        reader = new WaitQueueReader<T>(this);
                        _QueueReader.Enqueue(reader);
                    }
                    else
                    {
                        value = default(T);
                        return true;
                    }
                }
                else
                {
                    value = default(T);
                    return true;
                }
            }

            if (reader!=null)
            {
              return  reader.Wait(timeout, out value);
            }
            else
            {
                invokeDequeueCallback(item.Callback);
                value = item.GetData();
                return true;
            }
        }

        public void EnqueueAndDispatch(T value)
        {
            EnqueueAndDispatch(value, null);
        }

        public void EnqueueAndDispatch(T value, ItemDequeuedCallback dequeuedCallback)
        {
            EnqueueAndDispatch(value, dequeuedCallback, true);
        }

        public void EnqueueAndDispatch(T value, ItemDequeuedCallback dequeuedCallback, bool canDispatchOnThisThread)
        {
            Debug.Assert(value != null, "item parameter should not be null");
            enqueueAndDispatch(new ExItem<T>(value, dequeuedCallback), canDispatchOnThisThread);
        }

        public void EnqueueAndDispatch(Exception exception, ItemDequeuedCallback dequeuedCallback, bool canDispatchOnThisThread)
        {
            Debug.Assert(exception != null, "exception parameter should not be null");
            enqueueAndDispatch(new ExItem<T>(exception, dequeuedCallback), canDispatchOnThisThread);
        }

        public bool EnqueueWithoutDispatch(T value)
        {
            return EnqueueWithoutDispatch(value, null);
        }

        public bool EnqueueWithoutDispatch(T value, ItemDequeuedCallback dequeuedCallback)
        {
            Debug.Assert(value != null, "EnqueueWithoutDispatch: item parameter should not be null");
            return enqueueWithoutDispatch(new ExItem<T>(value, dequeuedCallback));
        }

        public bool EnqueueWithoutDispatch(Exception exception, ItemDequeuedCallback dequeuedCallback)
        {
            Debug.Assert(exception != null, "EnqueueWithoutDispatch: exception parameter should not be null");
            return enqueueWithoutDispatch(new ExItem<T>(exception, dequeuedCallback));
        }

        public bool WaitForItem(TimeSpan timeout)
        {
            WaitQueueWaiter waiter = null;
            bool itemAvailable = false;
            lock (LockObject)
            {
                if (_QueueState==QueueState.Open)
                {
                    if (_ItemQueue.HasAvailableItem)
                    {
                        itemAvailable = true;
                    }
                    else
                    {
                        waiter = new WaitQueueWaiter();
                        _WaiterList.Add(waiter);
                    }
                }
                else if (_QueueState==QueueState.Shutdown)
                {
                    if (_ItemQueue.HasAvailableItem)
                    {
                        itemAvailable = true;
                    }
                    else if(_ItemQueue.HasAnyItem)
                    {
                        waiter = new WaitQueueWaiter();
                        _WaiterList.Add(waiter);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            if (waiter!=null)
            {
                return waiter.Wait(timeout);
            }
            else
            {
                return itemAvailable;
            }
        }

        public void Dispatch()
        {
            IQueueReader<T> reader = null;
            ExItem<T> item = new ExItem<T>();
            IQueueReader<T>[] outstandingReaders = null;
            IQueueWaiter[] waiters = null;
            bool itemAvailable = true;
            lock (LockObject)
            {
                itemAvailable = (_QueueState != QueueState.Closed && _QueueState != QueueState.Shutdown);
                this.getWaiters(out waiters);
                if (_QueueState != QueueState.Closed)
                {
                    _ItemQueue.MakePendingItemAvailable();
                    if (_QueueReader.Count>0)
                    {
                        item = _ItemQueue.DequeueAvailableItem();
                        reader = _QueueReader.Dequeue();
                        if (_QueueState == QueueState.Shutdown &&
                            _QueueReader.Count > 0 &&
                            _ItemQueue.ItemCount == 0)
                        {
                            outstandingReaders = new IQueueReader<T>[_QueueReader.Count];
                            _QueueReader.CopyTo(outstandingReaders, 0);
                            _QueueReader.Clear();
                            itemAvailable = false;
                        }
                    }
                }
            }

            if (outstandingReaders!=null)
            {
                if (_CompleteOutstandingReadersCallback==null)
                {
                    _CompleteOutstandingReadersCallback = completeOutstandingReadersCallback;
                }
                ThreadPool.QueueUserWorkItem(_CompleteOutstandingReadersCallback, outstandingReaders);
            }

            if (waiters!=null)
            {
                completeWaitersLater(itemAvailable, waiters);
            }

            if (reader!=null)
            {
                invokeDequeueCallback(item.Callback);
                reader.Set(item);
            }
        }

        public void Close()
        {
            this.Dispose();
        }

        public void Shutdown()
        {
            IQueueReader<T>[] outstandingReaders = null;
            lock (LockObject)
            {
                if (_QueueState == QueueState.Shutdown)
                    return;

                if (_QueueState == QueueState.Closed)
                    return;

                this._QueueState = QueueState.Shutdown;
                if (_QueueReader.Count>0&&_ItemQueue.ItemCount==0)
                {
                    outstandingReaders = new IQueueReader<T>[_QueueReader.Count];
                    _QueueReader.CopyTo(outstandingReaders, 0);
                    _QueueReader.Clear();
                }
            }

            if (outstandingReaders!=null)
            {
                for (int i = 0; i < outstandingReaders.Length; i++)
                {
                    outstandingReaders[i].Set(new ExItem<T>((Exception)null, null));
                }
            }
        }

        #endregion Public Method

        #region Private Static Method

        private static void invokeDequeueCallback(ItemDequeuedCallback dequeueCallback)
        {
            if (dequeueCallback != null)
            {
                dequeueCallback.Invoke();
            }
        }

        private static void invokeDequeueCallbackLater(ItemDequeuedCallback dequeueCallback)
        {
            if (dequeueCallback != null)
            {
                if (_OnInvokeDequeueCallback==null)
                {
                    _OnInvokeDequeueCallback = onInvokeDequeueCallback;
                }
                ThreadPool.QueueUserWorkItem(_OnInvokeDequeueCallback, dequeueCallback);
            }
        }

        private static void onInvokeDequeueCallback(object state)
        {
            ItemDequeuedCallback dequeueCallback = (ItemDequeuedCallback)state;
            invokeDequeueCallback(dequeueCallback);
        }

        private static void onDispatchCallback(object state)
        {
            ((ExQueue<T>)state).Dispatch();
        }

        private static void completeWaiters(bool itemAvailable,IQueueWaiter[] waiters)
        {
            if (waiters!=null)
            {
                for (int i = 0; i < waiters.Length;i++ )
                {
                    waiters[i].Set(itemAvailable);
                }
            }
        }

        private static void completeWaitersLater(bool itemAvailable, IQueueWaiter[] waiters)
        {
            if (itemAvailable)
            {
                if (_CompleteWaitersTrueCallback==null)
                {
                    _CompleteWaitersTrueCallback = onCompleteWaitersTrueCallback;
                }
                ThreadPool.QueueUserWorkItem(_CompleteWaitersTrueCallback, waiters);
            }
            else
            {
                if (_CompleteWaitersFalseCallback == null)
                {
                    _CompleteWaitersFalseCallback = onCompleteWaitersFalseCallback;
                }
                ThreadPool.QueueUserWorkItem(_CompleteWaitersFalseCallback, waiters);
            }
        }

        private static void onCompleteWaitersTrueCallback(object state)
        {
            IQueueWaiter[] waiters = (IQueueWaiter[])state;
            completeWaiters(true, waiters);
        }

        private static void onCompleteWaitersFalseCallback(object state)
        {
            IQueueWaiter[] waiters = (IQueueWaiter[])state;
            completeWaiters(false, waiters);
        }

        private static void completeOutstandingReadersCallback(object state)
        {
            IQueueReader<T>[] outstandingReaders = (IQueueReader<T>[])state;
            for (int i = 0; i < outstandingReaders.Length; i++)
            {
                outstandingReaders[i].Set(default(ExItem<T>));
            }
        }

        #endregion Private Static Method

        #region Disposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                bool dispose = false;
                lock (LockObject)
                {
                    if (_QueueState != QueueState.Closed)
                    {
                        _QueueState = QueueState.Closed;
                        dispose = true;
                    }
                    if (dispose)
                    {
                        while (_QueueReader.Count > 0)
                        {
                            var reader = _QueueReader.Dequeue();
                            reader.Set(default(ExItem<T>));
                        }

                        while (_ItemQueue.HasAnyItem)
                        {
                            var item = _ItemQueue.DequeueAnyItem();
                            item.Dispose();
                            invokeDequeueCallback(item.Callback);
                        }
                    }
                }
            }
        }

        #endregion Disposable

    }

}
