// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable
using System;
using System.Collections.Generic;
namespace Unity.AcceleratorClient.Telemetry;

internal static class TelemetrySchema
{
    public const int CurrentVersion = 1;
}

internal readonly record struct TelemetryEvent(
    // --- Common envelope ---
    int            SchemaVersion,
    DateTimeOffset Timestamp,
    string         SessionId,
    OpKind         Op,
    Outcome        Outcome,
    long           DurationMicros,

    // --- Per-client constants (carried on every event for filterability) ---
    string         Organization,
    string         Endpoint,
    Guid           ProjectId,

    // --- Op-specific facets (null/zero when not applicable) ---
    string?        Namespace,
    UInt128?       Key,
    int?           KeyCount,
    long?          BytesIn,
    long?          BytesOut,
    int?           Hits,
    int?           Misses,
    int?           Faults,
    bool?          SizeHintProvided,
    int?           ChunkCount,
    string?        ErrorVariant,
    string?        UnderlyingExceptionType,
    string?        DestinationPath,

    // --- ProcessSample-specific facets (null for all other ops) ---
    double?        HeapMib,
    int?           Gen0Collections,
    int?           Gen1Collections,
    int?           Gen2Collections,
    double?        AllocationRateMibPerSec,
    int?           WorkersAlive,
    int?           QueueDepth);
