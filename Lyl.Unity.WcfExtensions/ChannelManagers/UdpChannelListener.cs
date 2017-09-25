using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Lyl.Unity.Util.AsyncResult;
using Lyl.Unity.Util.Collection;
using Lyl.Unity.WcfExtensions.Channels;

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
        private object _CurrentChannelLockObject;
        private Uri _Uri;
        private AsyncCallback _OnSocketReceive;

        #endregion Private Filed

        #region Constructor

        public UdpChannelListener(TransportBindingElement bindingElement, BindingContext context)
        {
            _BufferManager = BufferManager.CreateBufferManager(bindingElement.MaxBufferPoolSize, ExDefaultValue.MaxBufferSize);
            var me = context.BindingParameters.Find<MessageEncodingBindingElement>();
            if (me != null)
            {
                _MessageEncoderFactory = me.CreateMessageEncoderFactory();
            }

            _ChannelQueue = new ExQueue<IInputChannel>();
            _CurrentChannelLockObject = new object();
            _Sockets = new List<Socket>(2);
            initializeUri(context);
        }

        #endregion Constructor

        #region Public Property
        
        public MessageEncoderFactory MessageEncoderFactory { get { return _MessageEncoderFactory; } }

        #endregion Public Property

        #region Public Base Class Property

        public override Uri Uri
        {
            get { return _Uri; }
        }

        #endregion Public Base Class Property

        #region Protected Async Base Class Method

        protected override IAsyncResult OnBeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            ExtentionHelper.ValidateTimeout(timeout);
            if (!this.IsDisposed)
            {
                this.ensureChannelAvailable();
            }
            return _ChannelQueue.BeginDequeue(timeout, callback, state);
        }

        protected override IInputChannel OnEndAcceptChannel(IAsyncResult result)
        {
            IInputChannel channel;
            if (_ChannelQueue.EndDequeue(result,out channel))
            {
                return channel;
            }
            else
            {
                throw new TimeoutException();
            }
        }

        protected override IAsyncResult OnBeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            ExtentionHelper.ValidateTimeout(timeout);
            return _ChannelQueue.BeginWaitForItem(timeout, callback, state);
        }

        protected override bool OnEndWaitForChannel(IAsyncResult result)
        {
            return _ChannelQueue.EndWaitForItem(result);
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

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.OnOpen(timeout);
            return new CompletedAsyncResult(callback, state);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        #endregion Protected Async Base Class Method

        #region Protected Base Class Method

        protected override IInputChannel OnAcceptChannel(TimeSpan timeout)
        {
            ExtentionHelper.ValidateTimeout(timeout);
            if (!this.IsDisposed)
            {
                this.ensureChannelAvailable();
            }

            IInputChannel channel;
            if (_ChannelQueue.Dequeue(timeout, out channel))
            {
                return channel;
            }
            else
            {
                throw new TimeoutException(
                 string.Format("Accept on listener at address {0} timed out after {1}.",
                 this.Uri.AbsoluteUri, timeout));
            }
        }

        protected override bool OnWaitForChannel(TimeSpan timeout)
        {
            ExtentionHelper.ValidateTimeout(timeout);
            return _ChannelQueue.WaitForItem(timeout);
        }

        protected override void OnAbort()
        {
            lock (ThisLock)
            {
                closeListenSockets(TimeSpan.Zero);
                _ChannelQueue.Close();
            }
        }

        protected override void OnClose(TimeSpan timeout)
        {
            lock (ThisLock)
            {
                closeListenSockets(TimeSpan.Zero);
                _ChannelQueue.Close();
            }
        }

        protected override void OnClosed()
        {
            if (_BufferManager!=null)
            {
                _BufferManager.Clear();
            }
            base.OnClosed();
        }

        protected override void OnOpening()
        {
            base.OnOpening();
            _OnSocketReceive = new AsyncCallback(onSocketReceive);
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            if (_Uri==null)
            {
                throw new InvalidOperationException("Uri must be set before ChannelListener is opened.");
            }
            if (_Sockets.Count==0)
            {
                if (_Uri.HostNameType==UriHostNameType.IPv4||
                    _Uri.HostNameType==UriHostNameType.IPv6)
                {
                    var socket = createListenSocket(IPAddress.Parse(_Uri.Host), _Uri.Port);
                    _Sockets.Add(socket);
                }
                else
                {
                    var socket = createListenSocket(IPAddress.Any, _Uri.Port);
                    _Sockets.Add(socket);
                }
            }
        }

        protected override void OnOpened()
        {
            base.OnOpened();
            Socket[] socketsSnapshot = _Sockets.ToArray();
            WaitCallback startReceiveCallback = startSocketReceive;
            for (int i = 0; i < socketsSnapshot.Length; i++)
            {
                ThreadPool.QueueUserWorkItem(startReceiveCallback, socketsSnapshot[i]);
            }
        }

        #endregion Protected Base Class Method

        #region Public Base Class Method

        public override T GetProperty<T>()
        {
            T messageEncoderProperty = this.MessageEncoderFactory.Encoder.GetProperty<T>();
            if (messageEncoderProperty != null)
            {
                return messageEncoderProperty;
            }

            if (typeof(T) == typeof(MessageVersion))
            {
                return (T)(object)this.MessageEncoderFactory.Encoder.MessageVersion;
            }

            return base.GetProperty<T>();
        }

        #endregion Public Base Class Method

        #region Private  Method

        private void initializeUri(BindingContext context)
        {
            Uri baseAddressUri = context.ListenUriBaseAddress;
            if (baseAddressUri==null)
            {
                if (context.ListenUriMode == ListenUriMode.Unique)
                {
                    UriBuilder uriBuilder = new UriBuilder(ExStringConstants.Scheme, Dns.GetHostName());
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

            if (baseAddress.Scheme != ExStringConstants.Scheme)
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

            lock (ThisLock)
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
            lock (ThisLock)
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

        private bool createOrRetrieveChannel(out UdpInputChannel channel)
        {
            bool createChannel = false;
            channel = _CurrentChannel;
            if (channel==null)
            {
                lock (_CurrentChannelLockObject)
                {
                    channel = _CurrentChannel;
                    if (channel == null)
                    {
                        channel = new UdpInputChannel(this);
                        channel.Closed += onChannelClosed;
                        _CurrentChannel = channel;
                        createChannel = true;
                    }
                }
            }
            return createChannel;
        }

        private void ensureChannelAvailable()
        {
            UdpInputChannel channel;
            var createChannel=createOrRetrieveChannel(out channel);
            if (createChannel)
            {
                _ChannelQueue.EnqueueAndDispatch(channel);
            }
        }

        private void onChannelClosed(object sender, EventArgs e)
        {
            UdpInputChannel channel = (UdpInputChannel)sender;
            lock (_CurrentChannelLockObject)
            {
                if (_CurrentChannel==channel)
                {
                    _CurrentChannel = null;
                }
            }
        }

        private EndPoint createDummyEndPoint(Socket socket)
        {
            if (socket.AddressFamily==AddressFamily.InterNetwork)
            {
                return new IPEndPoint(IPAddress.Any, 0);
            }
            else
            {
                return new IPEndPoint(IPAddress.IPv6Any, 0);
            }
        }

        private void startSocketReceive(object state)
        {
            Socket listenSocket = (Socket)state;
            IAsyncResult result = null;
            try
            {
                lock (ThisLock)
                {
                    if (base.State == CommunicationState.Opened)
                    {
                        EndPoint dummy = createDummyEndPoint(listenSocket);
                        byte[] buffer = _BufferManager.TakeBuffer(ExDefaultValue.MaxBufferSize);
                        result = listenSocket.BeginReceiveFrom(buffer, 0, buffer.Length,
                            SocketFlags.None, ref dummy, _OnSocketReceive, new ScoketReceiveState(listenSocket, buffer));
                    }

                    if (result!=null&&result.CompletedSynchronously)
                    {
                        continueSocketReceivie(result, listenSocket);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine("Error in receiving from the socket.");
                Debug.WriteLine(ex.ToString());
            }
        }

        private void onSocketReceive(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }
            var socket=((ScoketReceiveState)result.AsyncState).Socket;
            continueSocketReceivie(result, socket);
        }

        private void continueSocketReceivie(IAsyncResult receiveResult, Socket listenSocket)
        {
            bool continueReceiving = true;
            while (continueReceiving)
            {
                Message receivedMessage = null;
                if (receiveResult != null)
                {
                    receivedMessage = endSocketReceive(listenSocket, receiveResult);
                    receiveResult = null;
                }

                lock (ThisLock)
                {
                    if (base.State==CommunicationState.Opened)
                    {
                        EndPoint dummy = createDummyEndPoint(listenSocket);
                        byte[] buffer = _BufferManager.TakeBuffer(ExDefaultValue.MaxBufferSize);
                        receiveResult = listenSocket.BeginReceiveFrom(buffer, 0, buffer.Length,
                            SocketFlags.None, ref dummy, _OnSocketReceive, new ScoketReceiveState(listenSocket, buffer));
                    }
                }

                if (receiveResult == null || !receiveResult.CompletedSynchronously)
                {
                    continueReceiving = false;
                    dispatchMessage(receivedMessage);
                }
                else if (receivedMessage != null)
                {
                    ThreadPool.QueueUserWorkItem(dispatchMessageCallback, receivedMessage);
                }
            }
        }

        private void dispatchMessage(Message receiveMessage)
        {
            if (receiveMessage==null)
            {
                return;
            }
            try
            {
                UdpInputChannel newChannel;
                bool createChannel = createOrRetrieveChannel(out newChannel);

                newChannel.Dispatch(receiveMessage);

                if (createChannel)
                {
                    _ChannelQueue.EnqueueAndDispatch(newChannel);
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine("Error dispatching Message.");
                Debug.WriteLine(ex.ToString());
            }
        }

        private void dispatchMessageCallback(object state)
        {
            Message receiveMessage = (Message)state;
            dispatchMessage(receiveMessage);
        }

        private Message endSocketReceive(Socket listenSocket, IAsyncResult receiveResult)
        {
            if (base.State!=CommunicationState.Opened)
            {
                return null;
            }
            byte[] buffer = ((ScoketReceiveState)receiveResult.AsyncState).Buffer;
            Debug.Assert(buffer != null);
            Message message = null;
            try
            {
                int count = 0;
                lock (ThisLock)
                {
                    if (base.State == CommunicationState.Opened)
                    {
                        EndPoint dummy = createDummyEndPoint(listenSocket);
                        count = listenSocket.EndReceiveFrom(receiveResult, ref dummy);
                        Console.WriteLine(dummy.ToString());
                    }
                }

                if (count > 0)
                {
                    try
                    {
                        message = _MessageEncoderFactory.Encoder.ReadMessage(new ArraySegment<byte>(buffer, 0, count), _BufferManager);
                    }
                    catch (System.Exception ex)
                    {
                        throw new ProtocolException(
                            "There is a problem with the XML that was received from the network. See inner exception for more details.",
                            ex);
                    }               
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine("Error in completing the async receive via EndReceiveFrom method.");
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                if (message==null)
                {
                    _BufferManager.ReturnBuffer(buffer);
                    buffer = null;
                }
            }
            return message;
        }

        #endregion Private  Method

        #region ScoketReceiveState Class

        class ScoketReceiveState
        {
            
            #region Private Filed
            
            private Socket _Socket;
            private byte[] _Buffer;

            #endregion Private Filed

            #region Constructor

            public ScoketReceiveState(Socket socket, byte[] buffer)
            {
                this._Socket = socket;
                this._Buffer = buffer;
            }

            #endregion Constructor

            #region Public Property
            
            public Socket Socket { get { return _Socket; } }

            public byte[] Buffer { get { return _Buffer; } }

            #endregion Public Property

        }

        #endregion ScoketReceiveState Class

    }
}
