using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel.Channels;
using Lyl.Unity.Util;
using Lyl.Unity.Util.AsyncResult;

namespace Lyl.Unity.WcfExtensions.Channels
{
    abstract class UdpBaseChannel : ExChannelBase
    {
        
        #region Private Filed
        
        private const int maxBufferSize = 64 * 1024;
        private Socket _Socket;
        private BufferManager _BuffManager;
        private MessageEncoder _MessageEncoder;

        #endregion Private Filed

        #region Constructor

        public UdpBaseChannel(ChannelManagerBase channelManager, BufferManager bufferManager, MessageEncoder messageEncoder)
            : base(channelManager, null)
        {
            _BuffManager = bufferManager;
            _MessageEncoder = messageEncoder;
        }

        #endregion Constructor

        #region ExChannelBase

        protected void InitializeScoket(Socket socket)
        {
            if (this._Socket != null)
            {
                this._Socket = socket;
            }
        }

        protected override void OnAbort()
        {
            if (this._Socket != null)
            {
                this._Socket.Close();
            }
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.OnClose(timeout);
            return new CompletedAsyncResult(callback, state);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.OnOpen(timeout);
            return new CompletedAsyncResult(callback, state);
        }

        protected override void OnClose(TimeSpan timeout)
        {
            if (this._Socket != null)
            {
                this._Socket.Close((int)timeout.TotalMilliseconds);
            }
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        protected override void OnOpen(TimeSpan timeout)
        {

        }

        #endregion ExChannelBase

        #region Protected Method
        
        protected void SendMessage(Message message,TimeSpan timeout)
        {
            base.ThrowIfDisposedOrNotOpen();
            ArraySegment<byte> encodeBytes = default(ArraySegment<byte>);
            try
            {
                encodeBytes = this.encodeMessage(message);
                this.writeData(encodeBytes);
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (encodeBytes.Array!=null)
                {
                    this._BuffManager.ReturnBuffer(encodeBytes.Array);
                }
            }
        }

        protected Message ReceiveMessage(TimeSpan timeout)
        {
            base.ThrowIfDisposedOrNotOpen();
            try
            {
                var data = this.readData(timeout);
                var message = this.decodeMessage(data);
                return message;
            }
            catch (Exception ex)
            {
                
                throw ex;
            }
        }

        #endregion Protected Method

        #region Private Method

        private ArraySegment<byte> encodeMessage(Message message)
        {
            try
            {
                return this._MessageEncoder.WriteMessage(message, maxBufferSize, this._BuffManager);
            }
            finally
            {
                message.Close();
            }
        }

        private Message decodeMessage(ArraySegment<byte> data)
        {
            try
            {
                if (data.Array == null)
                {
                    return null;
                }
                else
                {
                    return this._MessageEncoder.ReadMessage(data, this._BuffManager);
                }
            }
            finally
            {
                if (data.Array != null)
                {
                    this._BuffManager.ReturnBuffer(data.Array);
                }
            }
        }

        private int writeData(ArraySegment<byte> bytes)
        {
            try
            {
                byte[] buffer = new byte[bytes.Count];
                Array.Copy(bytes.Array, bytes.Offset, buffer, 0, bytes.Count);
                return this._Socket.Send(buffer);
            }
            finally
            {
                if (bytes.Array != null)
                    this._BuffManager.ReturnBuffer(bytes.Array);
            }
        }

        private ArraySegment<byte> readData(TimeSpan timeout)
        {
            try
            {
                byte[] buffer=this._BuffManager.TakeBuffer(maxBufferSize);
                int length=this._Socket.Receive(buffer);
                var result = new ArraySegment<byte>(buffer, 0, length);
                this._BuffManager.ReturnBuffer(buffer);
                return result;
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        #endregion Private Method

    }
}
