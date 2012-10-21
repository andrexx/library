//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

using System;
using System.Configuration;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;

namespace Microsoft.Samples.CompressionEncoder
{
    public class CompressMessageEncodingElement : BindingElementExtensionElement
    {
        //Called by the WCF to discover the type of binding element this config section enables
        public override Type BindingElementType
        {
            get { return typeof (CompressMessageEncodingBindingElement); }
        }

        //The only property we need to configure for our binding element is the type of
        //inner encoder to use. Here, we support text and binary.
        [ConfigurationProperty("innerMessageEncoding", DefaultValue = "textMessageEncoding")]
        public string InnerMessageEncoding
        {
            get { return (string) base["innerMessageEncoding"]; }
            set { base["innerMessageEncoding"] = value; }
        }

        [ConfigurationProperty("compressionAlgorithm", DefaultValue = CompressionAlgorithm.GZip)]
        public CompressionAlgorithm CompressionAlgorithm
        {
            get { return (CompressionAlgorithm) base["compressionAlgorithm"]; }
            set { base["compressionAlgorithm"] = value; }
        }

        //Called by the WCF to apply the configuration settings (the property above) to the binding element
        public override void ApplyConfiguration(BindingElement bindingElement)
        {
            var binding =
                (CompressMessageEncodingBindingElement) bindingElement;
            PropertyInformationCollection propertyInfo = ElementInformation.Properties;
            if (propertyInfo["innerMessageEncoding"].ValueOrigin != PropertyValueOrigin.Default)
            {
                switch (InnerMessageEncoding)
                {
                    case "textMessageEncoding":
                        binding.InnerMessageEncodingBindingElement = new TextMessageEncodingBindingElement();
                        break;
                    case "binaryMessageEncoding":
                        binding.InnerMessageEncodingBindingElement = new BinaryMessageEncodingBindingElement();
                        break;
                }
            }
            if (propertyInfo["compressionAlgorithm"].ValueOrigin != PropertyValueOrigin.Default)
            {
                binding.CompressionAlgorithm = (CompressionAlgorithm) propertyInfo["compressionAlgorithm"].Value;
            }
        }

        //Called by the WCF to create the binding element
        protected override BindingElement CreateBindingElement()
        {
            var bindingElement = new CompressMessageEncodingBindingElement();
            ApplyConfiguration(bindingElement);
            return bindingElement;
        }
    }
}