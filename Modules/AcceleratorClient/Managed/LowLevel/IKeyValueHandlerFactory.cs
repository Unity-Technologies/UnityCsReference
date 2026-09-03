// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable
using System;
using System.IO;
namespace Unity.AcceleratorClient.LowLevel;

using Codice.Accelerator.Interfaces;

// Test seam for the V2 DLL's instance-based handler construction (AcceleratorConnection.Get.GetKeyValueHandler).
// A hand-rolled stub is used in tests instead of Moq: Moq can't proxy an internal interface without
// InternalsVisibleTo("DynamicProxyGenAssembly2")
internal interface IKeyValueHandlerFactory
{
    IKeyValueHandler CreateHandler(string serverEndpoint);
}
