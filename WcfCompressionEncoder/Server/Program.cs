//#define Use_Tcp

//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

using System;
using System.ServiceModel;
using Microsoft.Samples.CompressionEncoder;

namespace Microsoft.WcfCompression.Server
{
    internal static class Program
    {
        private static void Main()
        {
#if Use_Tcp
            using (ServiceHost sampleServer = new ServiceHost(typeof(SampleServer),
                                                    new Uri("net.tcp://localhost:9009/samples/CompressionEncoder")))
#else
            using (
                var sampleServer = new ServiceHost(typeof (SampleServer),
                                                   new Uri("http://localhost:8000/samples/CompressionEncoder")))
#endif
            {
                sampleServer.Open();
                Console.WriteLine("\nPress Enter key to Exit.");
                Console.ReadLine();

                sampleServer.Close();
            }
        }
    }
}