using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Lyl.Unity.WcfExtensions.MessageEncoders;

namespace Lyl.Unity.WcfExtensions.BindingElements
{
    class OneWayEncoderBindingElement : MessageEncodingBindingElement
    {

        #region Private Filed

        private MessageVersion _MessageVersion;
        private string _MediaType;
        private string _Encoding;
        private XmlDictionaryReaderQuotas _ReaderQuotas;

        #endregion Private Filed

        #region Constructor

        private OneWayEncoderBindingElement(OneWayEncoderBindingElement bindingElement)
            : this(bindingElement._MessageVersion, bindingElement._MediaType, bindingElement._Encoding)
        {
            this._ReaderQuotas = new XmlDictionaryReaderQuotas();
            bindingElement._ReaderQuotas.CopyTo(_ReaderQuotas);
        }

        public OneWayEncoderBindingElement(MessageVersion messageVersion, string mediaType, string encoding)
        {
            if (encoding == null)
                throw new ArgumentNullException("encoding");

            if (mediaType == null)
                throw new ArgumentNullException("mediaType");

            if (messageVersion == null)
                throw new ArgumentNullException("messageVersion");

            this._MessageVersion = messageVersion;
            this._MediaType = mediaType;
            this._Encoding = encoding;
            this._ReaderQuotas = new XmlDictionaryReaderQuotas();
        }

        public OneWayEncoderBindingElement(string mediaType, string encoding)
            : this(MessageVersion.Soap11WSAddressing10, mediaType, encoding) { }

        public OneWayEncoderBindingElement(string encoding)
            : this("text/xml", encoding) { }

        public OneWayEncoderBindingElement()
            : this("UTF-8") { }

        #endregion Constructor

        #region Public Property

        public override MessageVersion MessageVersion
        {
            get
            {
                return _MessageVersion;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                _MessageVersion = value;
            }
        }

        public string MediaType
        {
            get
            {
                return _MediaType;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                _MediaType = value;
            }
        }

        public string Encoding
        {
            get
            {
                return _Encoding;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                _Encoding = value;
            }
        }

        public XmlDictionaryReaderQuotas ReaderQuotas
        {
            get
            {
                return _ReaderQuotas;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                _ReaderQuotas = value;
            }
        }

        #endregion Public Property

        #region Public Base Class Method

        public override MessageEncoderFactory CreateMessageEncoderFactory()
        {
            return new OneWayMessageEncoderFactory(_MessageVersion, _MediaType, _Encoding);
        }

        public override BindingElement Clone()
        {
            return new OneWayEncoderBindingElement(this);
        }

        public override T GetProperty<T>(BindingContext context)
        {
            if (typeof(T) == typeof(XmlDictionaryReaderQuotas))
            {
                return (T)(object)this._ReaderQuotas;
            }
            else
            {
                return base.GetProperty<T>(context);
            }
        }

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            context.BindingParameters.Add(this);
            return context.BuildInnerChannelFactory<TChannel>();
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            context.BindingParameters.Add(this);
            return context.BuildInnerChannelListener<TChannel>();
        }

        public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            context.BindingParameters.Add(this);
            return context.CanBuildInnerChannelFactory<TChannel>();
        }

        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            context.BindingParameters.Add(this);
            return context.CanBuildInnerChannelListener<TChannel>();
        }

        #endregion Public Base Class Method

    }
}
