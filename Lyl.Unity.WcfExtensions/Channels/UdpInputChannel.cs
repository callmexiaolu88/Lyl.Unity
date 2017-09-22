using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using Lyl.Unity.WcfExtensions.Channels;
using Lyl.Unity.Util.Collection;
using Lyl.Unity.WcfExtensions.ChannelManagers;
using Lyl.Unity.Util.AsyncResult;

namespace Lyl.Unity.WcfExtensions.Channels
{
    sealed class UdpInputChannel : ExChannelBase, IInputChannel
    {

        #region Private Filed

        private ExQueue<Message> _MessageQueue;
        private MessageEncoder _Encoder;

        #endregion Private Filed

        #region Constructor

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="channelManager">信道管理器</param>
        /// <param name="innerChannel">内部信道</param>
        public UdpInputChannel(UdpChannelListener channelManager, ChannelBase innerChannel)
            : base(channelManager, innerChannel)
        {
            _MessageQueue = new ExQueue<Message>();
            _Encoder = channelManager.MessageEncoderFactory.Encoder;
        }

        #endregion Constructor

        #region Public Base Class Property
        
        public EndpointAddress LocalAddress
        {
            get { return null; }
        }

        #endregion Public Base Class Property

        #region Protect Base Class Async Method

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            OnClose(timeout);
            return new CompletedAsyncResult(callback, state);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            OnOpen(timeout);
            return new CompletedAsyncResult(callback, state);
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        public IAsyncResult BeginReceive(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return BeginTryReceive(timeout, callback, state);
        }

        public IAsyncResult BeginReceive(AsyncCallback callback, object state)
        {
            return BeginReceive(this.DefaultReceiveTimeout, callback, state);
        }

        public Message EndReceive(IAsyncResult result)
        {
            return _MessageQueue.EndDequeue(result);
        }

        public IAsyncResult BeginTryReceive(TimeSpan timeout, AsyncCallback callback, object state)
        {
            ExtentionHelper.ValidateTimeout(timeout);
            return _MessageQueue.BeginDequeue(timeout, callback, state);
        }

        public bool EndTryReceive(IAsyncResult result, out Message message)
        {
            return _MessageQueue.EndDequeue(result, out message);
        }

        public IAsyncResult BeginWaitForMessage(TimeSpan timeout, AsyncCallback callback, object state)
        {
            ExtentionHelper.ValidateTimeout(timeout);
            return _MessageQueue.BeginWaitForItem(timeout, callback, state);
        }

        public bool EndWaitForMessage(IAsyncResult result)
        {
            return _MessageQueue.EndWaitForItem(result);
        }

        #endregion Protect Base Class Async Method

        #region Protect Base Class Method
        
        protected override void OnAbort()
        {
            _MessageQueue.Close();
        }

        protected override void OnClose(TimeSpan timeout)
        {
            _MessageQueue.Close();
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            return;
        }

        public Message Receive(TimeSpan timeout)
        {
            Message message;
            if (TryReceive(timeout,out message))
            {
                return message;
            }
            else
            {
                throw new TimeoutException(
                    string.Format("Receive timed out after {0}. The time allotted to this operation may have been a portion of a longer timeout.",
                    timeout));
            }
        }

        public Message Receive()
        {
            return Receive(this.DefaultReceiveTimeout);
        }

        public bool TryReceive(TimeSpan timeout, out Message message)
        {
            ExtentionHelper.ValidateTimeout(timeout);
            return _MessageQueue.Dequeue(timeout, out message);
        }

        public bool WaitForMessage(TimeSpan timeout)
        {
            ExtentionHelper.ValidateTimeout(timeout);
            return _MessageQueue.WaitForItem(timeout);
        }

        #endregion Protect Base Class Method

        #region Public Method
        
        public void Dispatch(Message receiveMessage)
        {
            _MessageQueue.EnqueueAndDispatch(receiveMessage);
        }

        public override T GetProperty<T>()
        {
            if (typeof(T)==typeof(IInputChannel))
            {
                return (T)(object)this;
            }
            T messageEncoderProperty = this._Encoder.GetProperty<T>();
            if (messageEncoderProperty!=null)
            {
                return messageEncoderProperty;
            }
            return base.GetProperty<T>();
        }

        #endregion Public Method

    }
}
