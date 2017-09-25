using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using Lyl.Unity.WcfExtensions.BindingElements;
using System.Configuration;
using System.ServiceModel.Configuration;
using Lyl.Unity.WcfExtensions.BindingConfigurationElements;
using System.ServiceModel;

namespace Lyl.Unity.WcfExtensions.Bindings
{
    public sealed class ExUdpBinding : Binding
    {
        
        #region Private Filed
        
        private bool _OneWayWcf;
        private bool _ReliableSessionEnabled;

        private CompositeDuplexBindingElement _CompositeDuplexBindingElement;
        private ReliableSessionBindingElement _ReliableSessionBindingElement;
        private MessageEncodingBindingElement _EncodingBindingElement;
        private MessageEncodingBindingElement _OneWayEncodingBindingElement;
        private TransportBindingElement _TransportBindingElement;

        #endregion Private Filed
        
        #region Public Property
        
        public bool OneWayWcf
        {
            get { return _OneWayWcf; }
            set { _OneWayWcf = value; }
        }

        public bool ReliableSessionEnabled
        {
            get { return _ReliableSessionEnabled; }
            set { _ReliableSessionEnabled = value; }
        }

        public bool OrderedSession
        {
            get { return _ReliableSessionBindingElement.Ordered; }
            set { _ReliableSessionBindingElement.Ordered = value; }
        }

        public TimeSpan SessionInactivityTimeout
        {
            get { return _ReliableSessionBindingElement.InactivityTimeout; }
            set { _ReliableSessionBindingElement.InactivityTimeout = value; }
        }

        public Uri ClientBaseAddress
        {
            get { return _CompositeDuplexBindingElement.ClientBaseAddress; }
            set { _CompositeDuplexBindingElement.ClientBaseAddress = value; }
        }

        #endregion Public Property

        #region Contructor

        public ExUdpBinding()
        {
            initialize();
        }

        public ExUdpBinding(string configurationName)
            : this()
        {
            applyConfiguration(configurationName);
        }

        public ExUdpBinding(bool reliableSessionEnadled, bool oneWayWcf)
            : this()
        {
            _ReliableSessionEnabled = reliableSessionEnadled;
            _OneWayWcf = oneWayWcf;
        }

        public ExUdpBinding(bool reliableSessionEnadled)
            : this(reliableSessionEnadled, false) { }

        #endregion Contructor

        #region Private Method

        private void initialize()
        {
            _ReliableSessionBindingElement = new ReliableSessionBindingElement();
            _CompositeDuplexBindingElement = new CompositeDuplexBindingElement();            
            _EncodingBindingElement = new TextMessageEncodingBindingElement();
            _OneWayEncodingBindingElement = new OneWayEncoderBindingElement();
            _TransportBindingElement = new ExUdpBindingElement();
        }

        private void applyConfiguration(string configurationName)
        {
            var section = ConfigurationManager.GetSection(ExStringConstants.UdpBindingSectionName) as StandardBindingCollectionElement<ExUdpBinding, ExUdpBindingConfigurationElement>;
            if (section != null)
            {
                var element = section.Bindings[configurationName];
                if (element == null)
                {
                    throw new ConfigurationErrorsException(string.Format(System.Globalization.CultureInfo.CurrentCulture,
                        "There is no binding named {0} at {1}.", configurationName, section.BindingName));
                }
                else
                {
                    element.ApplyConfiguration(this);
                }
            }
            else
            {
                throw new ConfigurationErrorsException(string.Format(System.Globalization.CultureInfo.CurrentCulture,
                        "There is no binding named {0} at {1}.", configurationName, ExStringConstants.UdpBindingSectionName));
            }
        }

        #endregion Private Method

        #region Public Base Class Method

        public override BindingElementCollection CreateBindingElements()
        {
            BindingElementCollection bindingElements = new BindingElementCollection();

            if (_ReliableSessionEnabled)
            {
                bindingElements.Add(_ReliableSessionBindingElement);
                bindingElements.Add(_CompositeDuplexBindingElement);
            }
            if (_OneWayWcf)
            {
                bindingElements.Add(_OneWayEncodingBindingElement);
            }
            else
            {
                bindingElements.Add(_EncodingBindingElement);
            }
            bindingElements.Add(_TransportBindingElement);
            return bindingElements.Clone();
        }

        public override string Scheme
        {
            get { return ExStringConstants.Scheme; }
        }

        #endregion Public Base Class Method

    }
}
