// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable
using System;
using System.IO;
using System.Threading;
namespace Unity.AcceleratorClient.LowLevel;

using Unity.AcceleratorClient.Telemetry;

internal sealed class AcceleratorV2ClientOptions
{
    public string Organization { get; init; } = string.Empty;

    public Guid ProjectId { get; init; } = Guid.Empty;

    public string Token { get; init; } = string.Empty;

    public string ServerEndpoint { get; init; } = "localhost:8087";

    public int WorkerThreadCount { get; init; } = 8;

    public int QueueCapacity { get; init; } = 256;

    public TimeSpan ShutdownTimeout { get; init; } = TimeSpan.FromSeconds(10);

    public bool VerifyContentHashOnUpload { get; init; } = false;

    public bool VerifyContentHashOnDownload { get; init; } = false;

    public BufferPoolConfig BufferPool { get; init; } = BufferPoolConfig.Default;
    
    public ITelemetrySink TelemetrySink { get; init; } = NullSink.Instance;

    public int ProcessSampleIntervalMs { get; init; } = 1000;

    public string? SessionId { get; init; }
}
