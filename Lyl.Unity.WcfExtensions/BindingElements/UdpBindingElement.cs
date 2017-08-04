using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using Lyl.Unity.WcfExtensions.ChannelManagers;

namespace Lyl.Unity.WcfExtensions.BindingElements
{
    public class UdpBindingElement : TransportBindingElement
    {

        public UdpBindingElement()
        {

        }

        public UdpBindingElement(TransportBindingElement elementToBeCloned)
            : base(elementToBeCloned)
        {

        }

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            if (CanBuildChannelFactory<TChannel>(context))
            {
                return new UdpChannelFactory<TChannel>(context);
            }
            return null;
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            if (CanBuildChannelListener<TChannel>(context))
            {
                return new UdpChannelListener<TChannel>(context);
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
            return new UdpBindingElement(this);
        }

        public override T GetProperty<T>(BindingContext context)
        {
            return context.GetInnerProperty<T>();
        }

        public override string Scheme
        {
            get { return "soap.udp"; }
        }
    }
}
