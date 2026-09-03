// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable
using System;
using System.IO;
namespace Unity.AcceleratorClient.LowLevel;

using Codice.Accelerator.Client;
using Codice.Accelerator.Interfaces;

internal sealed class DefaultKeyValueHandlerFactory : IKeyValueHandlerFactory
{
    public IKeyValueHandler CreateHandler(string serverEndpoint) =>
        AcceleratorConnection.Get().GetKeyValueHandler(serverEndpoint);
}
