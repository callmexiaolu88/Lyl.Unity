using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;

namespace Lyl.Unity.WcfExtensions.MessageEncoders
{
    class OneWayMessageEncoder:MessageEncoder
    {

        #region Private Filed

        private OneWayMessageEncoderFactory _EncoderFactory;
        private XmlWriterSettings _WriterSettings;
        private string _ContentType;

        #endregion Private Filed

        #region Constructor

        public OneWayMessageEncoder(OneWayMessageEncoderFactory encoderFactory)
        {
            if (encoderFactory == null)
                throw new ArgumentNullException("encoderFactory");
            this._EncoderFactory = encoderFactory;
            this._WriterSettings = new XmlWriterSettings();
            this._WriterSettings.Encoding = Encoding.GetEncoding(encoderFactory.Encoding);
            this._ContentType = string.Format("{0}; charset={1}", encoderFactory.MediaType, _WriterSettings.Encoding.HeaderName);
        }

        #endregion Constructor
        
        #region Public Base Class Property
        
        public override string ContentType
        {
            get { return _ContentType; }
        }

        public override string MediaType
        {
            get { return _EncoderFactory.MediaType; }
        }

        public override MessageVersion MessageVersion
        {
            get { return _EncoderFactory.MessageVersion; }
        }

        #endregion Public Base Class Property

        #region Public Base Class Method
        
        public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
        {
            byte[] messageContents = new byte[buffer.Count];
            Array.Copy(buffer.Array, buffer.Offset, messageContents, 0, buffer.Count);
            bufferManager.ReturnBuffer(buffer.Array);
            MemoryStream stream = new MemoryStream(messageContents);
            return ReadMessage(stream, int.MaxValue);
        }

        public override Message ReadMessage(System.IO.Stream stream, int maxSizeOfHeaders, string contentType)
        {
            XmlReader reader = XmlReader.Create(stream);
            return Message.CreateMessage(reader, maxSizeOfHeaders, MessageVersion);
        }

        public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
        {
            MemoryStream stream = new MemoryStream();
            XmlWriter writer = XmlWriter.Create(stream, _WriterSettings);
            message.WriteMessage(writer);
            writer.Close();

            byte[] messageContents = stream.GetBuffer();
            int messageLength=(int)stream.Position;
            stream.Close();

            int totalLength=messageLength+messageOffset;
            byte[] takeBuffer = bufferManager.TakeBuffer(totalLength);
            Array.Copy(messageContents, 0, takeBuffer, messageOffset, messageLength);

            ArraySegment<byte> byteArray = new ArraySegment<byte>(takeBuffer, messageOffset, messageLength);
            return byteArray;
        }

        public override void WriteMessage(Message message, System.IO.Stream stream)
        {
            XmlWriter writer = XmlWriter.Create(stream, _WriterSettings);
            message.WriteMessage(writer);
            writer.Close();
        }

        #endregion Public Base Class Method

    }
}
