//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

using System;
using System.ServiceModel;

namespace Microsoft.WcfCompression.Server
{
    [ServiceContract]
    public interface ISampleServer
    {
        [OperationContract]
        string Echo(string input);

        [OperationContract]
        string[] BigEcho(string[] input);
    }
}