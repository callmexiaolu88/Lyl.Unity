using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyl.Unity.Util.AsyncResult
{
    /// <summary>
    /// 带返回数据立即完成异步操作类
    /// </summary>
    /// <typeparam name="T">异步操作返回数据类型</typeparam>
    public class DataCompleteAsyncResult<T> : DataAsyncResult<T>
    {
        
        #region Constructor
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="data">异步操作返回数据</param>
        /// <param name="callback">回调函数</param>
        /// <param name="state">异步操作用户定义对象</param>
        public DataCompleteAsyncResult(T data, AsyncCallback callback, object state)
            : base(callback, state)
        {
            base.Complete(data, true);
        }

        #endregion Constructor

    }
}
