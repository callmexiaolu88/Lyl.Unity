using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel.Channels;

namespace Lyl.Unity.WcfExtensions.ChannelManagers
{
    class UdpChannelListener : ChannelListenerBase<IInputChannel>
    {
        private IChannelListener<IInputChannel> _InnerChannelListener;

        public UdpChannelListener(TransportBindingElement bindingElement, BindingContext context)
        {
            //           _BufferManager = BufferManager.CreateBufferManager(bindingElement.MaxBufferPoolSize, int.MaxValue);
            //var me = context.BindingParameters.Find<MessageEncodingBindingElement>();
            //if (me!=null)
            //{
            //    _MessageEncoderFactory = me.CreateMessageEncoderFactory();
            //}
        }

        protected override IInputChannel OnAcceptChannel(TimeSpan timeout)
        {
            var innerChannel = _InnerChannelListener.AcceptChannel(timeout);
            return null;
        }

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

        protected override bool OnWaitForChannel(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public override Uri Uri
        {
            get { throw new NotImplementedException(); }
        }

        protected override void OnAbort()
        {
            throw new NotImplementedException();
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        protected override void OnClose(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
    }
}
