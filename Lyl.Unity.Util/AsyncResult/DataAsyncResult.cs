using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyl.Unity.Util.AsyncResult
{
    /// <summary>
    /// 带数据异步结果类
    /// </summary>
    /// <typeparam name="T">异步操作返回数据</typeparam>
    public abstract class DataAsyncResult<T> : ExAsyncResult
    {
        
        #region Private Filed
        
        /// <summary>
        /// 异步操作返回数据
        /// </summary>
        private T _Data;

        #endregion Private Filed

        #region Constructor

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="callback">回调函数</param>
        /// <param name="state">异步操作用户定义对象</param>
        protected DataAsyncResult(AsyncCallback callback, object state)
            : base(callback, state) { }

        #endregion Constructor
        
        #region Public Property
        
        /// <summary>
        /// 异步操作数据结果
        /// </summary>
        public T Data { get { return _Data; } }

        #endregion Public Property
        
        #region Protected Method
        
        /// <summary>
        /// 异步操作完成时捕获到异常时调用函数
        /// </summary>
        /// <param name="data">异步操作返回数据</param>
        /// <param name="completedSynchronously">异步操作是否同步完成</param>
        protected void Complete(T data,bool completedSynchronously)
        {
            this._Data = data;
            base.Complete(completedSynchronously);
        }

        #endregion Protected Method
        
        #region Static Public Method
        
        /// <summary>
        /// 异步操作完成后执行方法
        /// </summary>
        /// <param name="result">异步操作结果</param>
        /// <returns>返回异步操作数据</returns>
        public static T End(IAsyncResult result)
        {
            DataAsyncResult<T> typedResult = ExAsyncResult.End<DataAsyncResult<T>>(result);
            return typedResult.Data;
        }

        #endregion Static Public Method

    }
}
