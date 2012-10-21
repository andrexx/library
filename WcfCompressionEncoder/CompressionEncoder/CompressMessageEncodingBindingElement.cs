//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Xml;

namespace Microsoft.Samples.CompressionEncoder
{
    

    //This is the binding element that, when plugged into a custom binding, will enable the GZip encoder
    public sealed class CompressMessageEncodingBindingElement
        : MessageEncodingBindingElement //BindingElement
          , IPolicyExportExtension
    {
        //We will use an inner binding element to store information required for the inner encoder
        private MessageEncodingBindingElement _innerBindingElement;

        private CompressionAlgorithm _compressionAlgorithm;

        //By default, use the default text encoder as the inner encoder
        public CompressMessageEncodingBindingElement()
            : this(new TextMessageEncodingBindingElement(), CompressionAlgorithm.GZip)
        {
        }

        public CompressMessageEncodingBindingElement(MessageEncodingBindingElement messageEncoderBindingElement,
                                                          CompressionAlgorithm compressionAlgorithm)
        {
            _innerBindingElement = messageEncoderBindingElement;
            _compressionAlgorithm = compressionAlgorithm;
        }

        public MessageEncodingBindingElement InnerMessageEncodingBindingElement
        {
            get { return _innerBindingElement; }
            set { _innerBindingElement = value; }
        }

        public CompressionAlgorithm CompressionAlgorithm
        {
            get { return _compressionAlgorithm; }
            set { _compressionAlgorithm = value; }
        }

        //Main entry point into the encoder binding element. Called by WCF to get the factory that will create the
        //message encoder
        public override MessageEncoderFactory CreateMessageEncoderFactory()
        {
            return new CompressMessageEncoderFactory(_innerBindingElement.CreateMessageEncoderFactory(),
                                                          _compressionAlgorithm);
        }

        public override MessageVersion MessageVersion
        {
            get { return _innerBindingElement.MessageVersion; }
            set { _innerBindingElement.MessageVersion = value; }
        }

        public override BindingElement Clone()
        {
            return new CompressMessageEncodingBindingElement(_innerBindingElement, _compressionAlgorithm);
        }

        public override T GetProperty<T>(BindingContext context)
        {
            if (typeof (T) == typeof (XmlDictionaryReaderQuotas))
            {
                return _innerBindingElement.GetProperty<T>(context);
            }
            return base.GetProperty<T>(context);
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

        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            context.BindingParameters.Add(this);
            return context.CanBuildInnerChannelListener<TChannel>();
        }

        void IPolicyExportExtension.ExportPolicy(MetadataExporter exporter, PolicyConversionContext policyContext)
        {
            if (policyContext == null)
            {
                throw new ArgumentNullException("policyContext");
            }
            var document = new XmlDocument();
            policyContext.GetBindingAssertions().Add(document.CreateElement(
                CompressMessageEncodingPolicyConstants.GZipEncodingPrefix,
                CompressMessageEncodingPolicyConstants.GZipEncodingName,
                CompressMessageEncodingPolicyConstants.GZipEncodingNamespace));
        }
    }

    //This class is necessary to be able to plug in the GZip encoder binding element through
    //a configuration file
}