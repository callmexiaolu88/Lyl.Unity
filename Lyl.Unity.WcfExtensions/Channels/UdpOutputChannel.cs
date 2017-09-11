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

namespace Lyl.Unity.WcfExtensions.Channels
{
    sealed class UdpOutputChannel : UdpBaseChannel, IOutputChannel
    {
        private EndpointAddress _RemoteAddress = null;
        private Uri _Via = null;
        private UdpChannelFactory _Factory;

        private IPAddress _IPAddress = null;

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
            throw new NotImplementedException();
        }

        public IAsyncResult BeginSend(Message message, AsyncCallback callback, object state)
        {
            return BeginSend(message, base.DefaultSendTimeout, callback, state);
        }

        public void EndSend(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        public void Send(Message message, TimeSpan timeout)
        {
            base.SendMessage(message, timeout);
        }

        public void Send(Message message)
        {
            this.Send(message, base.DefaultSendTimeout);
        }

        #endregion

        protected override void OnOpen(TimeSpan timeout)
        {
            this.connection();
            base.OnOpen(timeout);
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
                socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(new IPEndPoint(address, port));
                base.InitializeScoket(socket);
            }
        }
    }
}
