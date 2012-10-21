//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

using System;
using System.ServiceModel;

namespace Microsoft.WcfCompression.Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class SampleServer : ISampleServer
    {
        public string Echo(string input)
        {
            Console.WriteLine("\n\tServer Echo(string input) called:");
            Console.WriteLine("\tClient message:\t{0}\n", input);
            return input + " " + input;
        }

        public string[] BigEcho(string[] input)
        {
            Console.WriteLine("\n\tServer BigEcho(string[] input) called:", input);
            Console.WriteLine("\t{0} client messages", input.Length);
            return input;
        }
    }
}