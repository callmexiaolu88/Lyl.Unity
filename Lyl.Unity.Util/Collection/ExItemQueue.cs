using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Lyl.Unity.Util.Collection
{
    /// <summary>
    /// ExItem队列
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class ExItemQueue<T> where T : class
    {

        #region Private Filed

        ExItem<T>[] _Items;
        int _Head;
        int _PendingCount;
        int _TotalCount;

        #endregion Private Filed
        
        #region Constructor
        
        public ExItemQueue()
        {
            _Items = new ExItem<T>[1];
        }

        #endregion Constructor

        #region Private Method

        private void enquequeItemCore(ExItem<T> item)
        {
            if (_TotalCount == _Items.Length)
            {
                ExItem<T>[] newItems = new ExItem<T>[_Items.Length * 2];
                for (int i = 0; i < _TotalCount; i++)
                {
                    newItems[i] = _Items[(_Head + i) % _Items.Length];
                }
                _Head = 0;
                _Items = newItems;
            }
            int tail = (_Head + _TotalCount) % _Items.Length;
            _Items[tail] = item;
            _TotalCount++;
        }
        
        private ExItem<T> dequeueItemCore()
        {
            if (_TotalCount==0)
            {
                Debug.Assert(false, "ExItemQueue.TotalCount==0");
                throw new Exception("ExItemQueue.TotalCount==0");
            }
            var item = _Items[_Head];
            _Items[_Head] = new ExItem<T>();
            _TotalCount--;
            _Head = (_Head + 1) % _Items.Length;
            return item;
        }

        #endregion Private Method
                
        #region Public Method
        
        public void EnqueuePendingItem(ExItem<T> item)
        {
            enquequeItemCore(item);
            _PendingCount++;
        }

        public void EnqueueAvailableItem(ExItem<T> item)
        {
            enquequeItemCore(item);
        }

        public void MakePendingItemAvailable()
        {
            if (_PendingCount==0)
            {
                Debug.Assert(false, "ExItemQueue.PendingCount==0");
                throw new Exception("ExItemQueue.PendingCount==0");
            }
            _PendingCount--;
        }

        public ExItem<T> DequeueAvailableItem()
        {
            if (_TotalCount==_PendingCount)
            {
                Debug.Assert(false, "ExItemQueue.TotalCount==ExItemQueue.PendingCount==" + _TotalCount);
                throw new Exception("ExItemQueue.TotalCount==ExItemQueue.PendingCount==" + _TotalCount);
            }
            return dequeueItemCore();
        }

        public ExItem<T> DequeueAnyItem()
        {
            if (_PendingCount==_TotalCount)
            {
                _PendingCount--;
            }
            return dequeueItemCore();
        }

        #endregion Public Method
        
        #region Public Property

        public bool HasAvailableItem
        {
            get { return _TotalCount > _PendingCount; }
        }

        public bool HasAnyItem
        {
            get { return _TotalCount > 0; }
        }

        public int ItemCount
        {
            get { return _TotalCount; }
        }

        #endregion Public Property

    }
}
