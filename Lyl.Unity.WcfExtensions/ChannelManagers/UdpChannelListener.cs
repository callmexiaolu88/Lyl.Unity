using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel.Channels;
using System.Net.Sockets;
using Lyl.Unity.Util.Collection;
using Lyl.Unity.WcfExtensions.Channels;
using System.ServiceModel.Description;
using System.Net;
using System.Globalization;

namespace Lyl.Unity.WcfExtensions.ChannelManagers
{
    class UdpChannelListener : ChannelListenerBase<IInputChannel>
    {
        
        #region Private Filed
        
        private BufferManager _BufferManager = null;
        private MessageEncoderFactory _MessageEncoderFactory = null;
        private List<Socket> _Sockets;
        private ExQueue<IInputChannel> _ChannelQueue;
        private UdpInputChannel _CurrentChannel;
        private object _LockObject;
        private Uri _Uri;

        #endregion Private Filed

        #region Constructor

        public UdpChannelListener(TransportBindingElement bindingElement, BindingContext context)
        {
            _LockObject = new object();
            _BufferManager = BufferManager.CreateBufferManager(bindingElement.MaxBufferPoolSize, int.MaxValue);
            var me = context.BindingParameters.Find<MessageEncodingBindingElement>();
            if (me != null)
            {
                _MessageEncoderFactory = me.CreateMessageEncoderFactory();
            }

            _ChannelQueue = new ExQueue<IInputChannel>();
            _Sockets = new List<Socket>();
            initializeUri(context);
        }

        #endregion Constructor

        #region Public Base Class Property

        public override Uri Uri
        {
            get { return _Uri; }
        }

        #endregion Public Base Class Property

        #region Protected Async Base Class Method

        protected override IAsyncResult OnBeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        protected override IInputChannel OnEndAcceptChannel(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        protected override IAsyncResult OnBeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        protected override bool OnEndWaitForChannel(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        #endregion Protected Async Base Class Method

        #region Protected Base Class Method

        protected override IInputChannel OnAcceptChannel(TimeSpan timeout)
        {
            var innerChannel = _InnerChannelListener.AcceptChannel(timeout);
            return null;
        }

        protected override bool OnWaitForChannel(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        protected override void OnAbort()
        {
            throw new NotImplementedException();
        }

        protected override void OnClose(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        #endregion Protected Base Class Method

        
        #region Private  Method
        
        private void initializeUri(BindingContext context)
        {
            Uri baseAddressUri = context.ListenUriBaseAddress;
            if (baseAddressUri==null)
            {
                if (context.ListenUriMode == ListenUriMode.Unique)
                {
                    UriBuilder uriBuilder = new UriBuilder(Constants.Scheme, Dns.GetHostName());
                    uriBuilder.Path = Guid.NewGuid().ToString();
                    baseAddressUri = uriBuilder.Uri;
                }
                else
                {
                    throw new InvalidOperationException("Null is only a supported value for ListenUriBaseAddress when using ListenUriMode.Unique.");
                }
            }

            if (context.ListenUriMode == ListenUriMode.Unique)
            {
                this.initializeUniqueUri(baseAddressUri);
            }
            else
            {
                initializeUri(baseAddressUri, context.ListenUriRelativeAddress);
            }
        }

        private void initializeUri(Uri baseAddress, string relativeAddress)
        {
            if (baseAddress == null)
                throw new ArgumentNullException("baseAddress");

            if (relativeAddress == null)
                throw new ArgumentNullException("relativeAddress");

            if (!baseAddress.IsAbsoluteUri)
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                    "Base address must be an absolute URI."), "baseAddress");

            if (baseAddress.Scheme != Constants.Scheme)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Invalid URI scheme: {0}.", baseAddress.Scheme), "baseAddress");
            }

            var fullUri = baseAddress;
            if (relativeAddress!=null)
            {
                if (!baseAddress.AbsolutePath.EndsWith("/"))
                {
                    UriBuilder uriBuilder = new UriBuilder(baseAddress);
                    uriBuilder.Path = uriBuilder.Path + "/";
                    baseAddress = uriBuilder.Uri;
                }
                fullUri = new Uri(baseAddress, relativeAddress);
            }

            lock (_LockObject)
            {
                ThrowIfDisposedOrImmutable();
                this._Uri = fullUri;
                closeListenSockets(TimeSpan.Zero);
            }
        }

        private void initializeUniqueUri(Uri baseAddressUri)
        {
            if (baseAddressUri==null)
                throw new ArgumentNullException("baseAddressUri");
            int port;
            lock (_LockObject)
            {
                closeListenSockets(TimeSpan.Zero);
                IPAddress ipAddress = null;
                if (!IPAddress.TryParse(baseAddressUri.Host,out ipAddress))
                {
                    ipAddress = IPAddress.Any;
                }

                Socket socket = createListenSocket(IPAddress.Any, 0);
                port = ((IPEndPoint)socket.LocalEndPoint).Port;
                _Sockets.Add(socket);
            }

            UriBuilder uriBuilder = new UriBuilder(baseAddressUri);
            uriBuilder.Port = port;
            initializeUri(baseAddressUri, string.Empty);
        }

        private Socket createListenSocket(IPAddress ipAddress, int port)
        {
            IPEndPoint bindIPEndPoint = new IPEndPoint(ipAddress, port);
            Socket socket = new Socket(bindIPEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(bindIPEndPoint);
            return socket;
        }

        private void closeListenSockets(TimeSpan timeout)
        {
            for (int i = 0; i < _Sockets.Count; i++)
            {
                _Sockets[i].Close((int)timeout.TotalMilliseconds);
            }
            _Sockets.Clear();
        }

        #endregion Private  Method

    }
}
