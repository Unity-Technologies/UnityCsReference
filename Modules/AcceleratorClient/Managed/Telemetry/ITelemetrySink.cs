// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
namespace Unity.AcceleratorClient.Telemetry;

// Emit MUST be thread-safe, non-blocking (no I/O on the calling thread), and MUST NOT throw.
internal interface ITelemetrySink : IAsyncDisposable
{
    void Emit(in TelemetryEvent evt);

    Task FlushAsync(CancellationToken ct);
}
