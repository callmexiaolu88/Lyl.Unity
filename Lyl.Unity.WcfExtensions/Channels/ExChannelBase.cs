using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace Lyl.Unity.WcfExtensions.Channels
{
    /// <summary>
    /// 扩展信道基实现
    /// </summary>
    abstract class ExChannelBase : ChannelBase
    {

        #region protected属性

        /// <summary>
        /// 内部信道
        /// </summary>
        protected ChannelBase InnerChannel { get; private set; }

        /// <summary>
        /// 信道栈底部
        /// </summary>
        protected bool ChannelStackEnd { get; private set; }

        #endregion protected属性

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="channelManager">信道管理器</param>
        /// <param name="innerChannel">内部信道</param>
        public ExChannelBase(ChannelManagerBase channelManager, ChannelBase innerChannel)
            : base(channelManager)
        {
            this.InnerChannel = innerChannel;
            this.ChannelStackEnd = this.InnerChannel != null ? false : true;
        }

        #endregion 构造函数

    }
}
