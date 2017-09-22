using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.Text;
using System.Threading.Tasks;
using Lyl.Unity.WcfExtensions.Bindings;

namespace Lyl.Unity.WcfExtensions.BindingConfigurationElements
{
    public class ExUdpBindingConfigurationElement : StandardBindingElement
    {

        public ExUdpBindingConfigurationElement(string configurationName)
            : base(configurationName)
        {
        }

        public ExUdpBindingConfigurationElement()
            : this(null)
        {
        }

        #region Public Property

        [ConfigurationProperty(ExStringConstants.OneWayWcf, DefaultValue = true)]
        public bool OneWayWcf
        {
            get { return (bool)base[ExStringConstants.OneWayWcf]; }
            set { base[ExStringConstants.OneWayWcf] = value; }
        }

        [ConfigurationProperty(ExStringConstants.OrderedSession, DefaultValue = true)]
        public bool OrderedSession
        {
            get { return (bool)base[ExStringConstants.OrderedSession]; }
            set { base[ExStringConstants.OrderedSession] = value; }
        }

        [ConfigurationProperty(ExStringConstants.ReliableSessionEnabled, DefaultValue = true)]
        public bool ReliableSessionEnabled
        {
            get { return (bool)base[ExStringConstants.ReliableSessionEnabled]; }
            set { base[ExStringConstants.ReliableSessionEnabled] = value; }
        }

        [ConfigurationProperty(ExStringConstants.SessionInactivityTimeout, DefaultValue = "00:10:00")]
        [TimeSpanValidator(MinValueString = "00:00:00")]
        public TimeSpan SessionInactivityTimeout
        {
            get { return (TimeSpan)base[ExStringConstants.SessionInactivityTimeout]; }
            set { base[ExStringConstants.SessionInactivityTimeout] = value; }
        }

        [ConfigurationProperty(ExStringConstants.ClientBaseAddress, DefaultValue = null)]
        public Uri ClientBaseAddress
        {
            get { return (Uri)base[ExStringConstants.ClientBaseAddress]; }
            set { base[ExStringConstants.ClientBaseAddress] = value; }
        }

        #endregion Public Property

        #region Protect Base Class Property

        protected override Type BindingElementType
        {
            get { return typeof(ExUdpBinding); }
        }

        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                ConfigurationPropertyCollection properties = base.Properties;
                properties.Add(new ConfigurationProperty(ExStringConstants.OneWayWcf,
                    typeof(Boolean), true, null, null, ConfigurationPropertyOptions.None));
                properties.Add(new ConfigurationProperty(ExStringConstants.OrderedSession,
                    typeof(Boolean), true, null, null, ConfigurationPropertyOptions.None));
                properties.Add(new ConfigurationProperty(ExStringConstants.ReliableSessionEnabled,
                    typeof(Boolean), true, null, null, ConfigurationPropertyOptions.None));
                properties.Add(new ConfigurationProperty(ExStringConstants.SessionInactivityTimeout,
                    typeof(TimeSpan), TimeSpan.Parse("00:10:00"), null,
                    new TimeSpanValidator(TimeSpan.Parse("00:00:00"), TimeSpan.Parse("10675199.02:48:05.4775807"), false),
                    ConfigurationPropertyOptions.None));
                properties.Add(new ConfigurationProperty(ExStringConstants.ClientBaseAddress,
                    typeof(System.Uri), null, null, null, ConfigurationPropertyOptions.None));

                return properties;
            }
        }

        #endregion Protect Base Class Property

        #region Protect Base Class Method

        protected override void OnApplyConfiguration(Binding binding)
        {
            if (binding == null)
                throw new ArgumentNullException("binding");

            if (binding.GetType() != BindingElementType)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                    "Invalid type for binding. Expected type: {0}. Type passed in: {1}.",
                    BindingElementType.AssemblyQualifiedName,
                    binding.GetType().AssemblyQualifiedName));
            }
            var udpBinding = (ExUdpBinding)binding;
            udpBinding.OneWayWcf = this.OneWayWcf;
            udpBinding.OrderedSession = this.OrderedSession;
            udpBinding.ReliableSessionEnabled = this.ReliableSessionEnabled;
            udpBinding.SessionInactivityTimeout = this.SessionInactivityTimeout;
            if (this.ClientBaseAddress != null)
                udpBinding.ClientBaseAddress = ClientBaseAddress;
        }

        #endregion Protect Base Class Method

    }
}
