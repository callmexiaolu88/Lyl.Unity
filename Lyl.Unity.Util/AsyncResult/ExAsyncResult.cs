using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Lyl.Unity.Util.AsyncResult
{
    /// <summary>
    /// 自定义异步操作结果类
    /// </summary>
    public abstract class ExAsyncResult : IAsyncResult
    {
        
        #region Private Filed
        
        /// <summary>
        /// 异步操作完成时回调函数
        /// </summary>
        private AsyncCallback _Callback;

        /// <summary>
        /// 异步操作线程等待对象
        /// </summary>
        private ManualResetEvent _ManualResetEvent;

        /// <summary>
        /// 异步操作异常
        /// </summary>
        private Exception _Exception;

        /// <summary>
        /// 异步操作用户定义对象
        /// </summary>
        private object _State;

        /// <summary>
        /// 异步操作是否同步完成
        /// </summary>
        private bool _CompletedSynchronously;

        /// <summary>
        /// 异步操作是否调用End
        /// </summary>
        private bool _EndCalled;

        /// <summary>
        /// 异步操作是否完成
        /// </summary>
        private bool _IsCompleted;

        /// <summary>
        /// 同步锁
        /// </summary>
        private object _Lock;

        #endregion Private Filed

        #region Constructor
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="callback">回调函数</param>
        /// <param name="state">异步操作用户定义对象</param>
        protected ExAsyncResult(AsyncCallback callback, object state)
        {
            this._Callback = callback;
            this._State = state;
            this._Lock = new object();
        }

        #endregion Constructor

        #region IAsyncResult 成员

        /// <summary>
        /// 异步操作用户定义对象
        /// </summary>
        public object AsyncState
        {
            get { return _State; }
        }

        /// <summary>
        /// 异步操作线程等待对象
        /// </summary>
        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if (_ManualResetEvent != null)
                {
                    return _ManualResetEvent;
                }

                lock (_Lock)
                {
                    if (_ManualResetEvent == null)
                    {
                        _ManualResetEvent = new ManualResetEvent(_IsCompleted);
                    }
                }

                return _ManualResetEvent;
            }
        }

        /// <summary>
        /// 异步操作是否同步完成
        /// </summary>
        public bool CompletedSynchronously
        {
            get { return _CompletedSynchronously; }
        }

        /// <summary>
        /// 异步操作是否完成
        /// </summary>
        public bool IsCompleted
        {
            get { return _IsCompleted; }
        }

        #endregion
        
        #region Protected Method
        
        /// <summary>
        /// 异步操作完成调用函数
        /// </summary>
        /// <param name="completedSynchronously">异步操作是否同步完成</param>
        protected void Complete(bool completedSynchronously)
        {
            if (_IsCompleted)
            {
                // It is a bug to call Complete twice.
                throw new InvalidOperationException("Cannot call Complete twice");
            }

            this._CompletedSynchronously = completedSynchronously;

            if (completedSynchronously)
            {
                // If we completedSynchronously, then there is no chance that the _ManualResetEvent was created so
                // we do not need to worry about a race condition.
                Debug.Assert(this._ManualResetEvent == null, "No ManualResetEvent should be created for a synchronous AsyncResult.");
                this._IsCompleted = true;
            }
            else
            {
                lock (_Lock)
                {
                    this._IsCompleted = true;
                    if (this._ManualResetEvent != null)
                    {
                        this._ManualResetEvent.Set();
                    }
                }
            }

            // If the callback throws, there is a bug in the callback implementation
            if (_Callback != null)
            {
                _Callback(this);
            }
        }

        /// <summary>
        /// 异步操作完成时捕获到异常时调用函数
        /// </summary>
        /// <param name="completedSynchronously">异步操作是否同步完成</param>
        /// <param name="exception">异步操作期间捕获的异常</param>
        protected void Complete(bool completedSynchronously, Exception exception)
        {
            this._Exception = exception;
            Complete(completedSynchronously);
        }

        #endregion Protected Method
        
        #region Static Protected Method
        
        /// <summary>
        /// 异步操作完成后调用该方法
        /// </summary>
        /// <typeparam name="TAsyncResult">异步结果类型</typeparam>
        /// <param name="result">验证异步操作结果</param>
        /// <returns>返回验证后异步操作结果</returns>
        protected static TAsyncResult End<TAsyncResult>(IAsyncResult result)
            where TAsyncResult : ExAsyncResult
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            TAsyncResult asyncResult = result as TAsyncResult;

            if (asyncResult == null)
            {
                throw new ArgumentException("Invalid async result.", "result");
            }

            if (asyncResult._EndCalled)
            {
                throw new InvalidOperationException("Async object already ended.");
            }

            asyncResult._EndCalled = true;

            if (!asyncResult._IsCompleted)
            {
                asyncResult.AsyncWaitHandle.WaitOne();
            }

            if (asyncResult._ManualResetEvent != null)
            {
                asyncResult._ManualResetEvent.Close();
            }

            if (asyncResult._Exception != null)
            {
                throw asyncResult._Exception;
            }

            return asyncResult;
        }

        #endregion Static Protected Method

    }
}
