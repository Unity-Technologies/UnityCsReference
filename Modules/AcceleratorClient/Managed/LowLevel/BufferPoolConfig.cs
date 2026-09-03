// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable
using System;
namespace Unity.AcceleratorClient.LowLevel;

// FlexibleBufferPool.Shared is process-global: config applies once, on the first
// KeyValueClient.Connect call across all client instances in the process.
internal sealed record BufferPoolConfig(
    int MinBufferSize,
    int MaxBufferSize,
    string NumBuffersBeforeWait,
    string WaitTimes)
{
    public static BufferPoolConfig Default => new(
        MinBufferSize: KeyValueClient.k_DefaultMinBufferSize,
        MaxBufferSize: KeyValueClient.k_DefaultMaxBufferSize,
        NumBuffersBeforeWait: KeyValueClient.k_DefaultNumBuffersBeforeWait,
        WaitTimes: KeyValueClient.k_DefaultWaitTimes);
}
