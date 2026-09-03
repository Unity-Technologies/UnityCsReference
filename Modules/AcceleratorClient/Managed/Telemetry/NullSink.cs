// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: AssetDatabase not yet converted
#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Scripting.LifecycleManagement;
namespace Unity.AcceleratorClient.Telemetry;

// Singleton identity is load-bearing: emit helpers ReferenceEquals against Instance to skip event construction.
[NoAutoStaticsCleanup]
internal sealed class NullSink : ITelemetrySink
{
    public static readonly ITelemetrySink Instance = new NullSink();

    private NullSink()
    {
    }

    public void Emit(in TelemetryEvent evt)
    {
    }

    public Task FlushAsync(CancellationToken ct) => Task.CompletedTask;

    // defaulted ValueTask is already completed on netstandard2.1 (ValueTask.CompletedTask is net5+).
    public ValueTask DisposeAsync() => default;
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
