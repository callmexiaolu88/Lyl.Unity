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
    sealed class UdpOutputChannel : UdpBaseChannel, IOutputChannel
    {

        #region Private Filed

        private EndpointAddress _RemoteAddress = null;
        private Uri _Via = null;
        private UdpChannelFactory _Factory;

        private IPAddress _IPAddress = null;
        private IPEndPoint _RemoteEndPoint = null;

        #endregion Private Filed

        #region Constructor

        public UdpOutputChannel(UdpChannelFactory factory, EndpointAddress remoteAddress, Uri via,
            BufferManager bufferManager, MessageEncoder encoder)
            : base(factory, bufferManager, encoder)
        {
            if (!string.Equals(via.Scheme, ExStringConstants.Scheme))
            {
                throw new ArgumentException(via.Scheme, "via");
            }

            this._Factory = factory;
            this._RemoteAddress = remoteAddress;
            this._Via = via;

            _IPAddress = IPAddress.Parse(via.Host);
            _RemoteEndPoint = new IPEndPoint(_IPAddress, via.Port);            
        }
        
        #endregion 

        #region IOutputChannel 成员

        public EndpointAddress RemoteAddress
        {
            get { return _RemoteAddress; }
        }

        public Uri Via
        {
            get { return _Via; }
        }

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
            this.SendMessage(message, this._RemoteEndPoint);
        }

        #endregion IOutputChannel 成员

        #region Protect Base Class Method

        protected override void OnOpen(TimeSpan timeout)
        {
            this.connection();
        }

        #endregion Protect Base Class Method

        #region Private Method

        private void connection()
        {
            Socket socket = null;
            socket = new Socket(_RemoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            base.InitializeScoket(socket);
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
                try
                {
                    var result = channel.BeginSendMessage(message, out _MessageBuff, channel._RemoteEndPoint, onSendCallback, this);
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
                    int bytesSent = _Channel.EndSendMessage(result);
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
                    _Channel.ClearupBuffer(_MessageBuff.Array);
                    _MessageBuff = new ArraySegment<byte>();
                }
            }

            #endregion Private Method

        }

        #endregion SendAsyncResult Class

    }
}
