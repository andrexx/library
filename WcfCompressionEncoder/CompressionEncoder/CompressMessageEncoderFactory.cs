//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.ServiceModel.Channels;

namespace Microsoft.Samples.CompressionEncoder
{
    /// <summary>
    ///   This class is used to create the custom encoder (MyCompressionMessageEncoder)
    /// </summary>
    internal class CompressMessageEncoderFactory : MessageEncoderFactory
    {
        private readonly MessageEncoderFactory _innerFactory;
        private readonly MessageEncoder _encoder;
        private readonly CompressionAlgorithm _compressionAlgorithm;

        //The GZip encoder wraps an inner encoder
        //We require a factory to be passed in that will create this inner encoder
        public CompressMessageEncoderFactory(MessageEncoderFactory messageEncoderFactory,
                                                  CompressionAlgorithm compressionAlgorithm)
        {
            if (messageEncoderFactory == null)
                throw new ArgumentNullException("messageEncoderFactory",
                                                "A valid message encoder factory must be passed to the CompressionEncoder");

            _encoder = new MyCompressionMessageEncoder(messageEncoderFactory.Encoder, compressionAlgorithm);
            _compressionAlgorithm = compressionAlgorithm;
            _innerFactory = messageEncoderFactory;
        }

        //The service framework uses this property to obtain an encoder from this encoder factory
        public override MessageEncoder Encoder
        {
            get { return _encoder; }
        }

        public override MessageVersion MessageVersion
        {
            get { return _encoder.MessageVersion; }
        }

        public override MessageEncoder CreateSessionEncoder()
        {
            return new MyCompressionMessageEncoder(_innerFactory.CreateSessionEncoder(), _compressionAlgorithm);
        }

        //This is the actual GZip encoder
        private class MyCompressionMessageEncoder : MessageEncoder
        {
            private const string GZipContentType = "application/x-gzip";
            private const string DeflateContentType = "application/x-deflate";

            //This implementation wraps an inner encoder that actually converts a WCF Message
            //into textual XML, binary XML or some other format. This implementation then compresses the results.
            //The opposite happens when reading messages.
            //This member stores this inner encoder.
            private readonly MessageEncoder _innerEncoder;

            private readonly CompressionAlgorithm _compressionAlgorithm;

            //We require an inner encoder to be supplied (see comment above)
            internal MyCompressionMessageEncoder(MessageEncoder messageEncoder,
                                                 CompressionAlgorithm compressionAlgorithm)
            {
                if (messageEncoder == null)
                    throw new ArgumentNullException("messageEncoder",
                                                    "A valid message encoder must be passed to the CompressionEncoder");
                _innerEncoder = messageEncoder;
                _compressionAlgorithm = compressionAlgorithm;
            }

            public override string ContentType
            {
                get
                {
                    return _compressionAlgorithm == CompressionAlgorithm.GZip
                               ? GZipContentType
                               : DeflateContentType;
                }
            }

            public override string MediaType
            {
                get { return ContentType; }
            }

            //SOAP version to use - we delegate to the inner encoder for this
            public override MessageVersion MessageVersion
            {
                get { return _innerEncoder.MessageVersion; }
            }

            //Helper method to compress an array of bytes
            private static ArraySegment<byte> CompressBuffer(ArraySegment<byte> buffer, BufferManager bufferManager,
                                                             int messageOffset,
                                                             CompressionAlgorithm compressionAlgorithm)
            {
                var memoryStream = new MemoryStream();

                using (Stream compressedStream = compressionAlgorithm == CompressionAlgorithm.GZip
                                                     ? new GZipStream(memoryStream, CompressionMode.Compress, true)
                                                     : (Stream)
                                                       new DeflateStream(memoryStream, CompressionMode.Compress, true)
                    )
                {
                    compressedStream.Write(buffer.Array, buffer.Offset, buffer.Count);
                }

                byte[] compressedBytes = memoryStream.ToArray();
                int totalLength = messageOffset + compressedBytes.Length;
                byte[] bufferedBytes = bufferManager.TakeBuffer(totalLength);

                Array.Copy(compressedBytes, 0, bufferedBytes, messageOffset, compressedBytes.Length);

                bufferManager.ReturnBuffer(buffer.Array);
                var byteArray = new ArraySegment<byte>(bufferedBytes, messageOffset,
                                                       compressedBytes.Length);

                return byteArray;
            }

            //Helper method to decompress an array of bytes
            private static ArraySegment<byte> DecompressBuffer(ArraySegment<byte> buffer, BufferManager bufferManager,
                                                               CompressionAlgorithm compressionAlgorithm)
            {
                var memoryStream = new MemoryStream(buffer.Array, buffer.Offset, buffer.Count);
                var decompressedStream = new MemoryStream();
                //int totalRead = 0;
                const int blockSize = 1024;
                byte[] tempBuffer = bufferManager.TakeBuffer(blockSize);
                using (Stream compressedStream = compressionAlgorithm == CompressionAlgorithm.GZip
                                                     ? new GZipStream(memoryStream, CompressionMode.Decompress)
                                                     : (Stream)
                                                       new DeflateStream(memoryStream, CompressionMode.Decompress))
                {
                    while (true)
                    {
                        int bytesRead = compressedStream.Read(tempBuffer, 0, blockSize);
                        if (bytesRead == 0)
                            break;
                        decompressedStream.Write(tempBuffer, 0, bytesRead);
                        //totalRead += bytesRead;
                    }
                }
                bufferManager.ReturnBuffer(tempBuffer);

                byte[] decompressedBytes = decompressedStream.ToArray();
                byte[] bufferManagerBuffer = bufferManager.TakeBuffer(decompressedBytes.Length + buffer.Offset);
                Array.Copy(buffer.Array, 0, bufferManagerBuffer, 0, buffer.Offset);
                Array.Copy(decompressedBytes, 0, bufferManagerBuffer, buffer.Offset, decompressedBytes.Length);

                var byteArray = new ArraySegment<byte>(bufferManagerBuffer, buffer.Offset,
                                                       decompressedBytes.Length);
                bufferManager.ReturnBuffer(buffer.Array);

                return byteArray;
            }

            //One of the two main entry points into the encoder. Called by WCF to decode a buffered byte array into a Message.
            public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager,
                                                string contentType)
            {
                //Decompress the buffer
                ArraySegment<byte> decompressedBuffer = DecompressBuffer(buffer, bufferManager, _compressionAlgorithm);
                //Use the inner encoder to decode the decompressed buffer
                Message returnMessage = _innerEncoder.ReadMessage(decompressedBuffer, bufferManager);
                returnMessage.Properties.Encoder = this;
                return returnMessage;
            }

            //One of the two main entry points into the encoder. Called by WCF to encode a Message into a buffered byte array.
            public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize,
                                                            BufferManager bufferManager, int messageOffset)
            {
                //Use the inner encoder to encode a Message into a buffered byte array
                ArraySegment<byte> buffer = _innerEncoder.WriteMessage(message, maxMessageSize, bufferManager, 0);
                //Compress the resulting byte array
                Debug.WriteLine("Original size: {0}", buffer.Count);
                buffer = CompressBuffer(buffer, bufferManager, messageOffset, _compressionAlgorithm);
                Debug.WriteLine("Compressed size: {0}", buffer.Count);
                return buffer;
            }

            public override Message ReadMessage(Stream stream, int maxSizeOfHeaders, string contentType)
            {
                //Pass false for the "leaveOpen" parameter to the GZipStream constructor.
                //This will ensure that the inner stream gets closed when the message gets closed, which
                //will ensure that resources are available for reuse/release.
                Stream compressedStream = _compressionAlgorithm == CompressionAlgorithm.GZip
                                              ? new GZipStream(stream, CompressionMode.Decompress, false)
                                              : (Stream) new DeflateStream(stream, CompressionMode.Decompress, false);
                return _innerEncoder.ReadMessage(compressedStream, maxSizeOfHeaders);
            }

            public override void WriteMessage(Message message, Stream stream)
            {
                using (Stream compressedStream = _compressionAlgorithm == CompressionAlgorithm.GZip
                                                     ? new GZipStream(stream, CompressionMode.Decompress)
                                                     : (Stream) new DeflateStream(stream, CompressionMode.Decompress))
                {
                    _innerEncoder.WriteMessage(message, compressedStream);
                }

                // innerEncoder.WriteMessage(message, gzStream) depends on that it can flush data by flushing 
                // the stream passed in, but the implementation of GZipStream.Flush will not flush underlying
                // stream, so we need to flush here.
                stream.Flush();
            }
        }
    }
}