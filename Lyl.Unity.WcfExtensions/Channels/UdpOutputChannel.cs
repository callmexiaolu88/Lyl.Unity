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
        private EndpointAddress _RemoteAddress = null;
        private Uri _Via = null;
        private UdpChannelFactory _Factory;

        private IPAddress _IPAddress = null;
        private IPEndPoint _RemoteEndPoint = null;

        public UdpOutputChannel(UdpChannelFactory factory, EndpointAddress remoteAddress, Uri via,
            BufferManager bufferManager, MessageEncoder encoder)
            : base(factory, bufferManager, encoder)
        {
            if (!string.Equals(via.Scheme, Constants.Scheme))
            {
                throw new ArgumentException(via.Scheme, "via");
            }

            this._Factory = factory;
            this._RemoteAddress = remoteAddress;
            this._Via = via;

            _IPAddress = IPAddress.Parse(via.Host);
            _RemoteEndPoint = new IPEndPoint(_IPAddress, via.Port);            
        }

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

        #endregion

        protected override void OnOpen(TimeSpan timeout)
        {
            this.connection();
        }

        private void connection()
        {
            Socket socket = null;
            int port = Via.Port;
            if (port == -1)
            {
                port = 8000; // the default port for sized.tcp
            }
            IPHostEntry hostEntry = Dns.GetHostEntry(this.Via.Host);

            if (hostEntry.AddressList.Length>0)
            {
                IPAddress address = hostEntry.AddressList.First();
                socket = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                socket.Connect(new IPEndPoint(address, port));
                base.InitializeScoket(socket);
            }
        }

        #region SendAsyncResult

        class SendAsyncResult : ExAsyncResult
        {
            ArraySegment<byte> messageBuff;
            UdpOutputChannel channel;

            public SendAsyncResult(UdpOutputChannel channel,Message message, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.channel = channel;
                try
                {
                    var result = channel.BeginSendMessage(message, out messageBuff, channel._RemoteEndPoint, onSendCallback, this);
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

            public static void End(IAsyncResult result)
            {
                ExAsyncResult.End<SendAsyncResult>(result);
            }

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
                    int bytesSent = channel.EndSendMessage(result);
                    if (bytesSent!=messageBuff.Count)
                    {
                        throw new CommunicationException(string.Format(CultureInfo.CurrentCulture,
                           "A Udp error occurred sending a message to {0}.", channel._RemoteEndPoint));
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
                if (messageBuff.Array != null)
                {
                    channel.ClearupBuffer(messageBuff.Array);
                    messageBuff = new ArraySegment<byte>();
                }
            }
        }

        #endregion SendAsyncResult

    }
}
