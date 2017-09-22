using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using Lyl.Unity.WcfExtensions.ChannelManagers;

namespace Lyl.Unity.WcfExtensions.BindingElements
{
    public class ExUdpBindingElement : TransportBindingElement
    {

        public ExUdpBindingElement()
        {

        }

        public ExUdpBindingElement(TransportBindingElement elementToBeCloned)
            : base(elementToBeCloned)
        {

        }

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            if (CanBuildChannelFactory<TChannel>(context))
            {
                return (IChannelFactory<TChannel>)new UdpChannelFactory(this, context);
            }
            return null;
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            if (CanBuildChannelListener<TChannel>(context))
            {
                return (IChannelListener<TChannel>)new UdpChannelListener(this, context);
            }
            return null;
        }

        public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
        {
            return typeof(TChannel) == typeof(IOutputChannel);
        }

        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            return typeof(TChannel) == typeof(IInputChannel);
        }

        public override BindingElement Clone()
        {
            return new ExUdpBindingElement(this);
        }

        public override T GetProperty<T>(BindingContext context)
        {
            if (typeof(T) == typeof(MessageVersion))
            {
                return (T)(object)MessageVersion.None;
            }

            return base.GetProperty<T>(context);
        }

        public override string Scheme
        {
            get { return ExStringConstants.Scheme; }
        }
    }
}
