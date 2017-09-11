using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyl.Unity.Util.AsyncResult
{
    /// <summary>
    /// 异步操作立即完成类
    /// </summary>
    public class CompletedAsyncResult : ExAsyncResult
    {
        
        #region Constructor

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="callback">回调函数</param>
        /// <param name="state">异步操作用户定义对象</param>
        public CompletedAsyncResult(AsyncCallback callback, object state)
            : base(callback, state)
        {
            Complete(true);
        }

        #endregion Constructor

        #region Static Public Method

        /// <summary>
        /// 异步操作完成后执行方法
        /// </summary>
        /// <param name="result">异步操作结果</param>
        public static void End(IAsyncResult result)
        {
            ExAsyncResult.End<CompletedAsyncResult>(result);
        }

        #endregion Static Public Method

    }
}
