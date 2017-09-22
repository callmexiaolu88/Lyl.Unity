using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace Lyl.Unity.WcfExtensions.MessageEncoders
{
    class OneWayMessageEncoderFactory : MessageEncoderFactory
    {
        #region Private Filed

        private MessageVersion _MessageVersion;
        private string _MediaType;
        private string _Encoding;
        private MessageEncoder _Encoder;

        #endregion Private Filed

        #region Constructor

        public OneWayMessageEncoderFactory(MessageVersion messageVersion, string mediaType, string encoding)
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
            this._Encoder = new OneWayMessageEncoder(this);
        }

        #endregion Constructor

        #region Public Base Class Method

        public override MessageEncoder Encoder
        {
            get { return _Encoder; }
        }

        public override MessageVersion MessageVersion
        {
            get { return _MessageVersion; }
        }

        #endregion Public Base Class Method

        #region Internal Property

        internal string MediaType
        {
            get
            {
                return _MediaType;
            }
        }

        internal string Encoding
        {
            get
            {
                return _Encoding;
            }
        }

        #endregion Internal Property
    }
}
