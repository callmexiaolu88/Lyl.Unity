using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using Lyl.Unity.WcfExtensions.ChannelManagers;

namespace Lyl.Unity.WcfExtensions.BindingElements
{
    public class ExUdpBindingElement : TransportBindingElement
    {

        #region Constructor
        
        public ExUdpBindingElement()
        {

        }

        public ExUdpBindingElement(TransportBindingElement elementToBeCloned)
            : base(elementToBeCloned)
        {

        }

        #endregion Constructor
        
        #region Public Base Class Property
        
        public override string Scheme
        {
            get { return ExStringConstants.Scheme; }
        }

        #endregion Public Base Class Property

        #region Public Base Class Method

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            return (IChannelFactory<TChannel>)(object)new UdpChannelFactory(this, context);
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (!this.CanBuildChannelListener<TChannel>(context))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Unsupported channel type: {0}.", typeof(TChannel).Name));
            }

            return (IChannelListener<TChannel>)(object)new UdpChannelListener(this, context);
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
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            return context.GetInnerProperty<T>();
        }

        #endregion Public Base Class Method

    }
}
