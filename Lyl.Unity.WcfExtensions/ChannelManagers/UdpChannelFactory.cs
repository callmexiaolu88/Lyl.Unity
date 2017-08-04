using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using Lyl.Unity.Util;
using Lyl.Unity.WcfExtensions.Channels;

namespace Lyl.Unity.WcfExtensions.ChannelManagers
{
    class UdpChannelFactory : ChannelFactoryBase<IOutputChannel>
    {

        private BufferManager _BufferManager = null;
        private MessageEncoderFactory _MessageEncoderFactory = null;

        public UdpChannelFactory(TransportBindingElement bindingElement, BindingContext context)
        {
            _BufferManager = BufferManager.CreateBufferManager(bindingElement.MaxBufferPoolSize, int.MaxValue);
            var me = context.BindingParameters.Find<MessageEncodingBindingElement>();
            if (me!=null)
            {
                _MessageEncoderFactory = me.CreateMessageEncoderFactory();
            }
        }

        public override T GetProperty<T>()
        {
            T result = _MessageEncoderFactory.Encoder.GetProperty<T>();
            if (result == null)
            {
                if (typeof(T) == typeof(MessageVersion))
                {
                    result = (T)(object)_MessageEncoderFactory.Encoder.MessageVersion;
                }
                else
                {
                    result = base.GetProperty<T>();
                }
            }
            return result;
        }

        protected override IOutputChannel OnCreateChannel(EndpointAddress address, Uri via)
        {
            return new UdpOutputChannel(this, address, via, _MessageEncoderFactory.Encoder);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new CompletedAsyncResult(callback, state);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        protected override void OnOpen(TimeSpan timeout) { }
    }
}
