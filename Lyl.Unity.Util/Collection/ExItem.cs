using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Lyl.Unity.Util.Collection
{
    struct ExItem<T> where T : class
    {
        T _Data;
        Exception _Exception;
        ItemDequeuedCallback _Callback;

        public ExItem(T data, ItemDequeuedCallback callback)
            : this(data, null, callback) { }

        public ExItem(Exception ex, ItemDequeuedCallback callback)
            : this(null, ex, callback) { }

        public ExItem(T data, Exception ex, ItemDequeuedCallback callback)
        {
            this._Data = data;
            this._Exception = ex;
            this._Callback = callback;
        }

        public T Data { get { return _Data; } }

        public Exception Exception { get { return _Exception; } }

        public ItemDequeuedCallback Callback { get { return _Callback; } }

        public void Dispose()
        {
            if (_Data != null)
            {
                if (_Data is IDisposable)
                {
                    ((IDisposable)_Data).Dispose();
                }
                else if (_Data is ICommunicationObject)
                {
                    ((ICommunicationObject)_Data).Abort();
                }
            }
        }

        public T GetData()
        {
            if (this._Exception != null)
            {
                throw _Exception;
            }
            return _Data;
        }
    }
}
