using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace Lyl.Unity.WcfExtensions.BindingElements
{
    class OneWayEncoderBindingElement : MessageEncodingBindingElement
    {

        private MessageVersion _MessageVersion;

        public OneWayEncoderBindingElement(MessageVersion messageVersion)
        {
            this._MessageVersion = messageVersion;
        }

        public OneWayEncoderBindingElement(MessageEncodingBindingElement elementToBeCloned)
            : base(elementToBeCloned) { }

        public override MessageEncoderFactory CreateMessageEncoderFactory()
        {
            throw new NotImplementedException();
        }

        public override MessageVersion MessageVersion
        {
            get
            {
                return _MessageVersion;
            }
            set
            {
                _MessageVersion = value;
            }
        }

        public override BindingElement Clone()
        {
            return new OneWayEncoderBindingElement(this);
        }
    }
}
