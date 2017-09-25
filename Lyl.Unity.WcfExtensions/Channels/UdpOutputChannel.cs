using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using Lyl.Unity.WcfExtensions.ChannelManagers;
using System.Net;
using System.Net.Sockets;
using Lyl.Unity.Util.AsyncResult;
using System.Globalization;

namespace Lyl.Unity.WcfExtensions.Channels
{
    sealed class UdpOutputChannel : ChannelBase, IOutputChannel
    {

        #region Private Filed

        private EndpointAddress _RemoteAddress = null;
        private Uri _Via = null;
        private IPEndPoint _RemoteEndPoint = null;
        private Socket _Socket;

        private BufferManager _BuffManager;
        private MessageEncoder _MessageEncoder;
        private UdpChannelFactory _Factory;

        #endregion Private Filed

        #region Constructor

        public UdpOutputChannel(UdpChannelFactory factory, EndpointAddress remoteAddress, Uri via,
            BufferManager bufferManager, MessageEncoder messageEncoder)
            : base(factory)
        {
            if (!string.Equals(via.Scheme, ExStringConstants.Scheme))
            {
                throw new ArgumentException(via.Scheme, "via");
            }

            this._BuffManager = bufferManager;
            this._MessageEncoder = messageEncoder;
            this._Factory = factory;
            this._RemoteAddress = remoteAddress;
            this._Via = via;

            IPAddress remoteIP = IPAddress.Parse(via.Host);
            this._RemoteEndPoint = new IPEndPoint(remoteIP, via.Port);

            this._Socket = new Socket(this._RemoteEndPoint.AddressFamily,
                SocketType.Dgram, ProtocolType.Udp);
        }
        
        #endregion 

        #region IOutputChannel Properties

        public EndpointAddress RemoteAddress
        {
            get { return _RemoteAddress; }
        }

        public Uri Via
        {
            get { return _Via; }
        }

        #endregion IOutputChannel Properties

        #region IOutputChannel Method

        public IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            return BeginSend(message, callback, state);
        }

        public IAsyncResult BeginSend(Message message, AsyncCallback callback, object state)
        {
            base.ThrowIfDisposedOrNotOpen();
            return new SendAsyncResult(this, message, callback, state);
        }

        public void EndSend(IAsyncResult result)
        {
            SendAsyncResult.End(result);
        }

        public void Send(Message message, TimeSpan timeout)
        {
            this.Send(message);
        }

        public void Send(Message message)
        {
            this.sendMessage(message);
        }

        #endregion IOutputChannel Method

        #region Public Base Class Method

        public override T GetProperty<T>()
        {
            if (typeof(T) == typeof(IOutputChannel))
            {
                return (T)(object)this;
            }

            T messageEncoderProperty = this._MessageEncoder.GetProperty<T>();
            if (messageEncoderProperty != null)
            {
                return messageEncoderProperty;
            }

            return base.GetProperty<T>();
        }

        #endregion Public Base Class Method

        #region Protect Base Class Method

        protected override void OnOpen(TimeSpan timeout) { }

        protected override void OnAbort()
        {
            if (this._Socket != null)
            {
                this._Socket.Close(0);
            }
        }

        /// <summary>
        /// 关闭Socket时的timeout不能使用太长时间，不然消息发送不出去，应该使用Close(0),当使用Close((int)timeout.TotalMilliseconds)时会阻塞调用线程
        /// </summary>
        /// <param name="timeout"></param>
        protected override void OnClose(TimeSpan timeout)
        {
            if (this._Socket != null)
            {
                this._Socket.Close();
            }
        }

        #endregion Protect Base Class Method
        
        #region Protect Base Class Async Method
        
        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new CompletedAsyncResult(callback, state);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.OnClose(timeout);
            return new CompletedAsyncResult(callback, state);
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        #endregion Protect Base Class Async Method

        #region Private Method

        private ArraySegment<byte> encodeMessage(Message message)
        {
            try
            {
                this._RemoteAddress.ApplyTo(message);
                return this._MessageEncoder.WriteMessage(message, ExDefaultValue.MaxBufferSize, this._BuffManager);
            }
            finally
            {
                message.Close();
            }
        }

        private void sendMessage(Message message)
        {
            base.ThrowIfDisposedOrNotOpen();
            ArraySegment<byte> encodeBytes = this.encodeMessage(message);
            try
            {
                var bytesSent = this._Socket.SendTo(encodeBytes.Array, encodeBytes.Offset, encodeBytes.Count,
                    SocketFlags.None, this._RemoteEndPoint);
                if (bytesSent != encodeBytes.Count)
                {
                    throw new CommunicationException(string.Format(CultureInfo.CurrentCulture,
                        "A Udp error occurred sending a message to {0}.", _RemoteEndPoint));
                }
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
            finally
            {
                this._BuffManager.ReturnBuffer(encodeBytes.Array);
            }
        }

        private void clearupBuffer(byte[] buffer)
        {
            if (buffer != null)
            {
                this._BuffManager.ReturnBuffer(buffer);
            }

        }

        #endregion Private Method

        #region SendAsyncResult Class

        class SendAsyncResult : ExAsyncResult
        {

            #region Private Filed

            private ArraySegment<byte> _MessageBuff;
            private UdpOutputChannel _Channel;

            #endregion Private Filed

            #region Constructor

            public SendAsyncResult(UdpOutputChannel channel, Message message, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this._Channel = channel;
                this._MessageBuff = channel.encodeMessage(message);
                try
                {
                    IAsyncResult result = null;
                    try
                    {
                        result = channel._Socket.BeginSendTo(_MessageBuff.Array, _MessageBuff.Offset, _MessageBuff.Count,
                            SocketFlags.None, channel._RemoteEndPoint, callback, state);
                    }
                    catch (System.Exception ex)
                    {
                        throw ex;
                    }
                
                    if (result.CompletedSynchronously)
                    {
                        completeSend(result, true);
                    }
                }
                catch (System.Exception ex)
                {
                    clearupBuffer();
                    throw ex;
                }
                
            }

            #endregion Constructor

            #region Public Static Method
            
            public static void End(IAsyncResult result)
            {
                ExAsyncResult.End<SendAsyncResult>(result);
            }

            #endregion Public Static Method

            #region Private Method

            private void onSendCallback(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }
                try
                {
                    completeSend(result, false);
                }
                catch (System.Exception ex)
                {
                    Complete(false, ex);
                }
            }

            private void completeSend(IAsyncResult result,bool completedSynchronously)
            {
                try
                {
                    int bytesSent = _Channel._Socket.EndSendTo(result);
                    if (bytesSent!=_MessageBuff.Count)
                    {
                        throw new CommunicationException(string.Format(CultureInfo.CurrentCulture,
                           "A Udp error occurred sending a message to {0}.", _Channel._RemoteEndPoint));
                    }
                }
                catch (System.Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    clearupBuffer();
                }
                Complete(completedSynchronously);
            }

            private void clearupBuffer()
            {
                if (_MessageBuff.Array != null)
                {
                    _Channel.clearupBuffer(_MessageBuff.Array);
                    _MessageBuff = new ArraySegment<byte>();
                }
            }

            #endregion Private Method

        }

        #endregion SendAsyncResult Class

    }
}
