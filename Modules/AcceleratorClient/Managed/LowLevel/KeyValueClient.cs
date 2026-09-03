// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: AssetDatabase not yet converted
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Unity.AcceleratorClient.LowLevel;

// ImplicitUsings disabled; these usings sit after the file-scoped namespace.
using System.Diagnostics;

using Unity.AcceleratorClient.Contracts;
using Unity.AcceleratorClient.Telemetry;
using Unity.AcceleratorClient.Models;
using Unity.Scripting.LifecycleManagement;

// V2 library types (from acceleratorclientcore.dll).
using AcceleratorConnection = Codice.Accelerator.Client.AcceleratorConnection;
using IKeyValueHandler = Codice.Accelerator.Interfaces.IKeyValueHandler;
using ValuesInfosWithData = Codice.Accelerator.Interfaces.ValuesInfosWithData;
using ValueInfo = Codice.Accelerator.Common.ValueInfo;
using FlexibleBufferPool = Codice.Utils.Buffers.FlexibleBufferPool;
using FlexibleBufferPoolConfig = Codice.Utils.Buffers.FlexibleBufferPoolConfig;
using BufferPoolMemoryStream = Codice.Utils.Buffers.BufferPoolMemoryStream;
using DataPacket = Codice.CM.Common.Serialization.DataPacket;
using DataPacketWriter = Codice.CM.Common.Serialization.DataPacketWriter;
using AcclibUInt128 = System.UInt128;

// One instance is shared by all worker threads; per-call scratch lives in the
// [ThreadStatic] Scratch nested class, instance fields are write-once in Connect.
[NoAutoStaticsCleanup]
internal sealed class KeyValueClient
{
    // Process-wide sink-throw counter
    private static long s_LibraryEmitFailureCount;
    private static int s_LibraryEmitFailureTraceFired;

    internal static long LibraryEmitFailureCount => Interlocked.Read(ref s_LibraryEmitFailureCount);

    internal static void ResetLibraryEmitFailureCountForTests()
    {
        Interlocked.Exchange(ref s_LibraryEmitFailureCount, 0);
        Interlocked.Exchange(ref s_LibraryEmitFailureTraceFired, 0);
    }

    // Trace.WriteLine fires once per process; the counter records the rest.
    internal static void RecordLibraryEmitFailure(Exception ex)
    {
        Interlocked.Increment(ref s_LibraryEmitFailureCount);
        if (Interlocked.Exchange(ref s_LibraryEmitFailureTraceFired, 1) == 0)
        {
            Trace.WriteLine(
                $"[AccelClient] ITelemetrySink.Emit threw {ex.GetType().FullName}: {ex.Message}; "
                + "sinks MUST NOT throw per the contract. Further occurrences are counted via "
                + "KeyValueClient.LibraryEmitFailureCount but not re-logged.");
        }
    }

    // Value-ids orphaned server-side by cancellation between upload chunks. The V2
    // server never GCs these; the leak is permanent. Cancellation-only by design.
    private static long s_CancelOrphanedValueIdCount;

    internal static long CancelOrphanedValueIdCount => Interlocked.Read(ref s_CancelOrphanedValueIdCount);

    // V2 DataPacket single-call cap; larger payloads take the multi-call chunked path.
    public const int MaxDataPacketBytes = 4 * 1024 * 1024;

    // Buffer-pool defaults adopted verbatim from the V2 standalone-app.
    internal const int k_DefaultMinBufferSize = 1024;
    internal const int k_DefaultMaxBufferSize = 8 * 1024 * 1024;
    // "0=100" is a parser-fallback shape: seeds key 0 so the halving-search always
    // terminates at "100 ms" for any size bucket (per FlexibleBufferPoolConfig).
    internal const string k_DefaultNumBuffersBeforeWait =
        "1KB=4096;2KB=4096;4KB=2048;8KB=1024;16KB=512;32KB=256;64KB=128;"
        + "128KB=96;256KB=48;512KB=24;1MB=20;2MB=20;4MB=20;8MB=20";
    internal const string k_DefaultWaitTimes = "0=100";

    // Content-hash slot the V2 server ignores today; the library always passes 0.
    private const ulong k_UnusedHash = 0UL;

    private const string k_ProbeNamespace = "probe";

    // UInt128.MaxValue is statistically never a real cache key.
    private static readonly AcclibUInt128 k_ProbeKey = new(ulong.MaxValue, ulong.MaxValue);

    private static readonly object s_PoolInitLock = new();

    private readonly IKeyValueHandlerFactory m_HandlerFactory;
    private readonly AcceleratorV2ClientOptions m_Options;
    private readonly Guid m_ProjectId;
    private readonly ITelemetrySink m_Sink;
    // Pre-formatted GUID from the AsyncKeyValueClient parent; empty for test-only clients.
    private readonly string m_SessionIdFormatted;
    private IKeyValueHandler? m_Handler;
    private string m_Endpoint = string.Empty;

    // Per-thread [ThreadStatic] scratch: each OS thread lazy-inits its own copy via
    // Ensure on first call. Not freed on Dispose — owned by the thread, not the client.
    private static class Scratch
    {
        [NoAutoStaticsCleanup]
        internal static class SingleKey
        {
            [ThreadStatic] internal static List<AcclibUInt128>? s_Acclib;
            [ThreadStatic] internal static List<int>?           s_Size;
            [ThreadStatic] internal static List<ulong>?         s_Hash;

            internal static void Ensure()
            {
                s_Acclib ??= new List<AcclibUInt128>(1);
                s_Size   ??= new List<int>(1);
                s_Hash   ??= new List<ulong>(1);
            }
        }

        [NoAutoStaticsCleanup]
        internal static class Batch
        {
            [ThreadStatic] internal static List<AcclibUInt128>?       s_KeyList;
            [ThreadStatic] internal static Dictionary<long, UInt128>? s_IdToKey;
            [ThreadStatic] internal static Dictionary<long, long>?    s_IdToSize;
            [ThreadStatic] internal static List<ValueInfo>?           s_GroupableInfos;

            internal static void Ensure()
            {
                s_KeyList        ??= new List<AcclibUInt128>();
                s_IdToKey        ??= new Dictionary<long, UInt128>();
                s_IdToSize       ??= new Dictionary<long, long>();
                s_GroupableInfos ??= new List<ValueInfo>();
            }
        }
    }

    public KeyValueClient()
        : this(new DefaultKeyValueHandlerFactory(), new AcceleratorV2ClientOptions()) { }

    internal KeyValueClient(IKeyValueHandlerFactory handlerFactory)
        : this(handlerFactory, new AcceleratorV2ClientOptions()) { }

    internal KeyValueClient(
        IKeyValueHandlerFactory handlerFactory,
        AcceleratorV2ClientOptions options)
        : this(handlerFactory, options, sessionIdFormatted: string.Empty) { }

    internal KeyValueClient(
        IKeyValueHandlerFactory handlerFactory,
        AcceleratorV2ClientOptions options,
        string sessionIdFormatted)
    {
        // ThrowIfNull is net6+; expanded for netstandard2.1.
        if (handlerFactory is null) throw new ArgumentNullException(nameof(handlerFactory));
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (sessionIdFormatted is null) throw new ArgumentNullException(nameof(sessionIdFormatted));
        // ContentKey is absent from this build; fail loud at construction.
        if (options.VerifyContentHashOnUpload || options.VerifyContentHashOnDownload)
            throw new NotSupportedException("Content-hash verification is not available in this build; VerifyContentHashOnUpload/Download must be false.");
        m_HandlerFactory = handlerFactory;
        m_Options = options;
        m_ProjectId = options.ProjectId;
        m_Sink = options.TelemetrySink ?? NullSink.Instance;
        m_SessionIdFormatted = sessionIdFormatted;
    }

    private static CacheError.ContentHashMismatch BuildContentHashMismatch(
        CacheNamespace ns, UInt128 requestedKey, UInt128 computedKey,
        int bytesLength, ContentHashVerificationDirection direction)
    {
        return new CacheError.ContentHashMismatch(
            ns, requestedKey, computedKey, bytesLength, direction);
    }

    private static void LogContentHashMismatchLine(string organization, CacheError.ContentHashMismatch error)
    {
        try
        {
            Console.Error.WriteLine(
                $"ContentHashMismatch [{error.Direction}]: ns={organization}/{error.Namespace.Name} "
                + $"requestedKey=0x{error.RequestedKey:X32} computedKey=0x{error.ComputedKey:X32} "
                + $"bytesLength={error.BytesLength}");
        }
        catch (IOException)
        {
            // Best-effort: stderr may be a broken pipe; the caller still has the mismatch in the Result.
        }
    }

    private CacheError.ContentHashMismatch? VerifyUploadHashOrFail(
        CacheNamespace ns, UInt128 key, ReadOnlySpan<byte> bytes)
    {
        if (!m_Options.VerifyContentHashOnUpload)
            return null;
        // DLL's UInt128 has no == operator; use valuetype Equals.
        UInt128 computed = ContentKeyStub.Unsupported(); // allowed: VerifyContentHashOnUpload-guarded
        if (computed.Equals(key))
            return null;
        var mismatch = BuildContentHashMismatch(
            ns, key, computed, bytes.Length, ContentHashVerificationDirection.Upload);
        LogContentHashMismatchLine(m_Options.Organization, mismatch);
        return mismatch;
    }

    private CacheError.ContentHashMismatch? VerifyDownloadHashOrFail(
        CacheNamespace ns, UInt128 key, ReadOnlySpan<byte> bytes)
    {
        if (!m_Options.VerifyContentHashOnDownload)
            return null;
        UInt128 computed = ContentKeyStub.Unsupported(); // allowed: VerifyContentHashOnDownload-guarded
        if (computed.Equals(key))
            return null;
        var mismatch = BuildContentHashMismatch(
            ns, key, computed, bytes.Length, ContentHashVerificationDirection.Download);
        LogContentHashMismatchLine(m_Options.Organization, mismatch);
        return mismatch;
    }

    // Programmer errors, Moq test-double escapes, and cancellation propagate raw;
    // everything else routes through ExceptionTranslator.
    private static bool IsProgrammerError(Exception ex) =>
        ex is NullReferenceException
        or InvalidOperationException
        or ArgumentException
        or OutOfMemoryException
        or StackOverflowException;

    private static bool IsTestDoubleEscape(Exception ex) =>
        ex.GetType().FullName?.StartsWith("Moq.", StringComparison.Ordinal) ?? false;

    private static bool IsCancellationSignal(Exception ex) =>
        ex is OperationCanceledException;

    // V2 signals server-side faults as a bare System.Exception carrying this substring;
    // strict type check so derived V2 exceptions keep their own classification.
    private static bool IsAssertTrueServerFault(Exception ex) =>
        ex.GetType() == typeof(Exception)
        && ex.Message is { } msg
        && msg.Contains(
            ExceptionTranslator.k_AssertTrueServerFaultSubstring,
            StringComparison.Ordinal);

    // V2 miss sentinel: null, or a non-null ValueInfo with Id == 0.
    private static bool IsValueInfoMiss(ValueInfo? info)
        => info is null || info.Id == 0;

    private static bool IsRoutableV2Failure(Exception ex)
    {
        if (IsAssertTrueServerFault(ex))
            return true;

        return !IsProgrammerError(ex)
            && !IsTestDoubleEscape(ex)
            && !IsCancellationSignal(ex);
    }

    private void EmitOpCompleted(
        OpKind op, Outcome outcome, long startTicks,
        CacheNamespace? ns,
        UInt128? key = null, int? keyCount = null,
        long? bytesIn = null, long? bytesOut = null,
        int? hits = null, int? misses = null, int? faults = null,
        bool? sizeHintProvided = null, int? chunkCount = null,
        string? errorVariant = null, string? underlyingExceptionType = null,
        string? destinationPath = null,
        string? endpointOverride = null)
    {
        if (ReferenceEquals(m_Sink, NullSink.Instance))
            return;

        var elapsedTicks = Stopwatch.GetTimestamp() - startTicks;
        var durationMicros = elapsedTicks * 1_000_000L / Stopwatch.Frequency;

        var evt = new TelemetryEvent(
            SchemaVersion: TelemetrySchema.CurrentVersion,
            Timestamp: DateTimeOffset.UtcNow,
            SessionId: m_SessionIdFormatted,
            Op: op,
            Outcome: outcome,
            DurationMicros: durationMicros,
            Organization: m_Options.Organization,
            Endpoint: endpointOverride ?? m_Endpoint,
            ProjectId: m_ProjectId,
            Namespace: ns?.Name,
            Key: key,
            KeyCount: keyCount,
            BytesIn: bytesIn,
            BytesOut: bytesOut,
            Hits: hits,
            Misses: misses,
            Faults: faults,
            SizeHintProvided: sizeHintProvided,
            ChunkCount: chunkCount,
            ErrorVariant: errorVariant,
            UnderlyingExceptionType: underlyingExceptionType,
            DestinationPath: destinationPath,
            HeapMib: null,
            Gen0Collections: null,
            Gen1Collections: null,
            Gen2Collections: null,
            AllocationRateMibPerSec: null,
            WorkersAlive: null,
            QueueDepth: null);

        try
        {
            m_Sink.Emit(in evt);
        }
        catch (Exception ex)
        {
            RecordLibraryEmitFailure(ex);
        }
    }

    private void EmitOpSuccess(
        OpKind op, long startTicks, CacheNamespace? ns,
        UInt128? key = null, int? keyCount = null,
        long? bytesIn = null, long? bytesOut = null,
        int? hits = null, int? misses = null, int? faults = null,
        bool? sizeHintProvided = null, int? chunkCount = null,
        string? destinationPath = null)
    {
        EmitOpCompleted(op, Outcome.Success, startTicks, ns,
            key: key, keyCount: keyCount,
            bytesIn: bytesIn, bytesOut: bytesOut,
            hits: hits, misses: misses, faults: faults,
            sizeHintProvided: sizeHintProvided, chunkCount: chunkCount,
            destinationPath: destinationPath);
    }

    private void EmitCacheFailure(
        OpKind op, long startTicks, CacheNamespace? ns, CacheError error,
        UInt128? key = null, int? keyCount = null,
        bool? sizeHintProvided = null, string? destinationPath = null)
    {
        if (ReferenceEquals(m_Sink, NullSink.Instance))
            return;

        var variant = CacheErrorVariantNames.For(error);
        string? underlyingExceptionType = error is CacheError.ServerError serverError
            ? serverError.UnderlyingExceptionType
            : null;
        var outcome = error switch
        {
            CacheError.NotFound => Outcome.Miss,
            CacheError.Cancelled => Outcome.Cancelled,
            _ => Outcome.Failure,
        };

        EmitOpCompleted(op, outcome, startTicks, ns,
            key: key, keyCount: keyCount,
            sizeHintProvided: sizeHintProvided,
            errorVariant: variant,
            underlyingExceptionType: underlyingExceptionType,
            destinationPath: destinationPath);
    }

    private void EmitConnectCompleted(
        Outcome outcome, long startTicks,
        string? errorVariant = null, string? underlyingExceptionType = null)
    {
        EmitOpCompleted(OpKind.Connect, outcome, startTicks, ns: null,
            errorVariant: errorVariant,
            underlyingExceptionType: underlyingExceptionType);
    }

    // Carries the attempted endpoint without mutating m_Endpoint.
    private void EmitConnectFailureWithEndpoint(
        long startTicks, ConnectError error, string typeName, string serverEndpoint)
    {
        if (ReferenceEquals(m_Sink, NullSink.Instance))
            return;

        EmitOpCompleted(
            OpKind.Connect, Outcome.Failure, startTicks, ns: null,
            errorVariant: ConnectErrorVariantNames.For(error),
            underlyingExceptionType: typeName,
            endpointOverride: serverEndpoint);
    }

    private static void EnsureSharedBufferPoolInitialised(BufferPoolConfig cfg)
    {
        if (FlexibleBufferPool.Shared.MaxBufferSize != 0)
            return;

        lock (s_PoolInitLock)
        {
            if (FlexibleBufferPool.Shared.MaxBufferSize != 0)
                return;

            // Passing string.Empty as defaultValue skips a redundant seed-parse of the
            // same config string (the API parses defaultValue then configValue).
            var pool = FlexibleBufferPool.BuildPool(
                cfg.MinBufferSize,
                cfg.MaxBufferSize,
                FlexibleBufferPoolConfig.GetNumBuffersBeforeWaitBySize(
                    cfg.NumBuffersBeforeWait,
                    string.Empty),
                FlexibleBufferPoolConfig.GetWaitTimesBySize(
                    cfg.WaitTimes,
                    string.Empty));
            FlexibleBufferPool.InitShared(pool);
        }
    }

    public bool IsConnected => m_Handler is not null;

    // GetKeyValueHandler is lazy, so the probe is what surfaces unreachable-server errors.
    public Result<ConnectError> Connect(string serverEndpoint)
    {
        EnsureSharedBufferPoolInitialised(m_Options.BufferPool);

        m_Handler = null;

        // Record startTicks for the telemetry duration measurement.
        var startTicks = Stopwatch.GetTimestamp();

        IKeyValueHandler handler;
        try
        {
            handler = m_HandlerFactory.CreateHandler(serverEndpoint);
        }
        catch (Exception ex) when (IsRoutableV2Failure(ex))
        {
            var (error, typeName) = ExceptionTranslator.TranslateConnectWithType(ex, serverEndpoint);
            EmitConnectFailureWithEndpoint(startTicks, error, typeName, serverEndpoint);
            return error;
        }

        try
        {
            List<AcclibUInt128> probeKeys = [k_ProbeKey];
            handler.GetKeyValuesWithData(m_Options.Organization, m_ProjectId, k_ProbeNamespace, probeKeys, m_Options.Token);
        }
        catch (Exception ex) when (IsRoutableV2Failure(ex))
        {
            var (error, typeName) = ExceptionTranslator.TranslateConnectWithType(ex, serverEndpoint);
            EmitConnectFailureWithEndpoint(startTicks, error, typeName, serverEndpoint);
            return error;
        }

        // Only commit m_Endpoint on the success path.
        m_Endpoint = serverEndpoint;
        m_Handler = handler;
        EmitConnectCompleted(Outcome.Success, startTicks);
        return Result<ConnectError>.Success;
    }

    public Result<PutValue, CacheError> Put(
        CacheNamespace ns, UInt128 key, ReadOnlyMemory<byte> value, CancellationToken ct = default)
    {
        var startTicks = Stopwatch.GetTimestamp();
        var result = PutImpl(ns, key, value, ct);
        if (result.IsSuccess)
            EmitOpSuccess(OpKind.Put, startTicks, ns,
                key: key,
                bytesOut: value.Length,
                chunkCount: result.Value.ChunkCount);
        else
            EmitCacheFailure(OpKind.Put, startTicks, ns, result.Error, key: key);
        return result;
    }

    private Result<PutValue, CacheError> PutImpl(
        CacheNamespace ns, UInt128 key, ReadOnlyMemory<byte> value, CancellationToken ct)
    {
        if (m_Handler is null)
            return new CacheError.NotConnected(ns);

        if (value.IsEmpty)
            return new CacheError.InvalidArgument(
                nameof(value),
                "Value must not be empty; the V2 cache does not store empty values.");

        if (value.Length > int.MaxValue)
            return new CacheError.InvalidArgument(
                nameof(value),
                $"Single value exceeds int.MaxValue ({value.Length} bytes); not supported by V2 size-list field.");

        // Verify before chunk routing so a mismatch short-circuits the value-id allocation.
        {
            var mismatch = VerifyUploadHashOrFail(ns, key, value.Span);
            if (mismatch is not null)
                return mismatch;
        }

        if (value.Length > MaxDataPacketBytes)
            return PutChunked(ns, key, value, ct);

        var packet = new DataPacket();
        try
        {
            Scratch.SingleKey.Ensure();
            Scratch.SingleKey.s_Acclib!.Clear();
            Scratch.SingleKey.s_Acclib.Add(ToAcclib(key));
            Scratch.SingleKey.s_Size!.Clear();
            Scratch.SingleKey.s_Size.Add(value.Length);
            Scratch.SingleKey.s_Hash!.Clear();
            Scratch.SingleKey.s_Hash.Add(k_UnusedHash);

            var writer = packet.GetWriter();
            WritePooledData(writer, value);
            writer.EndWrite();

            m_Handler.SetKeyValuesWithData(
                m_Options.Organization, m_ProjectId, ns.Name,
                Scratch.SingleKey.s_Acclib, Scratch.SingleKey.s_Size,
                Scratch.SingleKey.s_Hash, packet, m_Options.Token);
            return Result<PutValue, CacheError>.Ok(
                new PutValue(BytesUploaded: value.Length, ChunkCount: 1));
        }
        catch (Exception ex) when (IsRoutableV2Failure(ex))
        {
            return ExceptionTranslator.TranslateCache(ex, ns);
        }
        finally
        {
            packet.ReleaseBuffer();
        }
    }

    // Multi-call upload: GetNewValueIds -> per-chunk SetValuesData -> SetKeyValues commit.
    // Cancellation between chunks orphans the value-id server-side (see s_CancelOrphanedValueIdCount).
    private Result<PutValue, CacheError> PutChunked(
        CacheNamespace ns, UInt128 key, ReadOnlyMemory<byte> value, CancellationToken ct)
    {
        // Re-checked here because PutBatch's big-entries pass calls PutChunked directly.
        if (value.IsEmpty)
            return new CacheError.InvalidArgument(
                nameof(value),
                "Value must not be empty; the V2 cache does not store empty values.");

        // Cast safe: top-level Put has already rejected value.Length > int.MaxValue.
        int totalSize = (int)value.Length;

        List<uint> valueIds;
        try
        {
            valueIds = m_Handler!.GetNewValueIds(m_Options.Organization, m_ProjectId, ns.Name, count: 1, m_Options.Token);
        }
        catch (Exception ex) when (IsRoutableV2Failure(ex))
        {
            return ExceptionTranslator.TranslateCache(ex, ns);
        }

        if (valueIds is null || valueIds.Count == 0)
            return new CacheError.ServerError(
                ns, "GetNewValueIds returned 0 ids", null);
        uint valueId = valueIds[0];

        // TakeBuffer never throws and never returns null; past the wait timeout it allocates fresh.
        byte[] buffer = FlexibleBufferPool.Shared.TakeBufferUsingConfiguredWaitTime(MaxDataPacketBytes);
        try
        {

            ulong segment = 0;
            int read = 0;
            int totalChunks = (totalSize + MaxDataPacketBytes - 1) / MaxDataPacketBytes;
            while (read < totalSize)
            {
                int chunkSize = Math.Min(MaxDataPacketBytes, totalSize - read);
                bool isLast = (read + chunkSize == totalSize);
                value.Span.Slice(read, chunkSize).CopyTo(buffer);

                var packet = new DataPacket();
                try
                {
                    var writer = packet.GetWriter();
                    writer.WriteData(valueId, 0, segment, isLast, buffer, chunkSize);
                    writer.EndWrite();
                    m_Handler.SetValuesData(m_Options.Organization, m_ProjectId, ns.Name, packet, m_Options.Token);
                }
                catch (Exception ex) when (IsRoutableV2Failure(ex))
                {
                    return ExceptionTranslator.TranslateCache(ex, ns);
                }
                finally
                {
                    packet.ReleaseBuffer();
                }

                read += chunkSize;
                segment++;

                // Cancellation honoured after ReleaseBuffer so the per-chunk packet isn't leaked.
                if (read < totalSize && ct.IsCancellationRequested)
                {
                    Interlocked.Increment(ref s_CancelOrphanedValueIdCount);
                    // Trace (not Debug) so the diagnostic survives Release builds.
                    System.Diagnostics.Trace.WriteLine(
                        $"AccelClient.PutChunked: cancellation between chunks left value-id {valueId} " +
                        $"orphaned in namespace {m_Options.Organization}/{ns.Name} " +
                        $"(segment {segment}/{totalChunks}); V2 server does not GC orphaned value-ids today.");
                    return new CacheError.Cancelled(ns);
                }
            }

            // The V2 server does not validate upload completeness at commit time, so an
            // early loop exit would commit silently-corrupt state; catch it at the debug CI gate.
            System.Diagnostics.Debug.Assert(read == totalSize,
                $"PutChunked loop-termination invariant: read ({read}) != totalSize ({totalSize}). " +
                $"The chunked-upload loop must read exactly totalSize bytes before the SetKeyValues " +
                $"commit; an early exit would commit silently-corrupt server state for value-id {valueId}.");

            // No cancellation honour point here: the chunks are fully uploaded, so committing
            // avoids orphaning a value-id; the caller observes cancellation on the next op.
            try
            {
                List<AcclibUInt128> keys = [ToAcclib(key)];
                List<ValueInfo> values = [new ValueInfo(valueId, totalSize, k_UnusedHash)];
                m_Handler.SetKeyValues(m_Options.Organization, m_ProjectId, ns.Name, keys, values, m_Options.Token);
                return Result<PutValue, CacheError>.Ok(
                    new PutValue(BytesUploaded: totalSize, ChunkCount: (int)segment));
            }
            catch (Exception ex) when (IsRoutableV2Failure(ex))
            {
                return ExceptionTranslator.TranslateCache(ex, ns);
            }
        }
        finally
        {
            FlexibleBufferPool.Shared.ReturnBuffer(buffer);
        }
    }

    // Always-chunked single-key upload streamed from disk; host-side I/O failures map to
    // ServerError (not InvalidArgument). Cancellation semantics match PutChunked.
    internal Result<PutValue, CacheError> PutFromFileStreaming(
        CacheNamespace ns, UInt128 key, string sourcePath, CancellationToken ct)
    {
        var startTicks = Stopwatch.GetTimestamp();
        var result = PutFromFileStreamingImpl(ns, key, sourcePath, ct);
        if (result.IsSuccess)
            EmitOpSuccess(OpKind.PutFromFile, startTicks, ns,
                key: key,
                bytesOut: result.Value.BytesUploaded,
                chunkCount: result.Value.ChunkCount);
        else
            EmitCacheFailure(OpKind.PutFromFile, startTicks, ns, result.Error,
                key: key);
        return result;
    }

    private Result<PutValue, CacheError> PutFromFileStreamingImpl(
        CacheNamespace ns, UInt128 key, string sourcePath, CancellationToken ct)
    {
        if (m_Handler is null)
            return new CacheError.NotConnected(ns);

        // FileInfo.Length pre-flight: reject empty files before opening the FileStream.
        long fileLength;
        try
        {
            var info = new FileInfo(sourcePath);
            if (!info.Exists)
                return new CacheError.ServerError(
                    ns,
                    $"Could not read file '{sourcePath}': file does not exist.",
                    UnderlyingExceptionType: typeof(FileNotFoundException).FullName);
            fileLength = info.Length;
        }
        // Argument-shape exceptions (null/invalid path) map to path-aware ServerError, not
        // InvalidArgument, matching the host-side I/O mapping.
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or ArgumentNullException
            or NotSupportedException)
        {
            return new CacheError.ServerError(
                ns,
                $"Could not read file '{sourcePath}': {ex.Message}",
                UnderlyingExceptionType: ex.GetType().FullName);
        }

        if (fileLength == 0)
            return new CacheError.InvalidArgument(
                nameof(sourcePath),
                $"Empty file content at '{sourcePath}': the V2 cache does not "
                    + "store empty values.");

        // Reject >= int.MaxValue: at exactly int.MaxValue the totalChunks numerator
        // (totalSize + MaxDataPacketBytes - 1) overflows int.
        if (fileLength >= int.MaxValue)
            return new CacheError.InvalidArgument(
                nameof(sourcePath),
                $"File '{sourcePath}' is {fileLength} bytes which is >= int.MaxValue; not supported by V2 size-list field.");

        int totalSize = (int)fileLength;

        // bufferSize: 1 because the chunk loop reads into a pool-rented buffer, so any
        // FileStream-internal buffer is pure waste (a 4 MiB one would allocate per call).
        FileStream sourceStream;
        try
        {
            sourceStream = new FileStream(
                sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 1, FileOptions.SequentialScan);
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or ArgumentNullException
            or NotSupportedException)
        {
            return new CacheError.ServerError(
                ns,
                $"Could not read file '{sourcePath}': {ex.Message}",
                UnderlyingExceptionType: ex.GetType().FullName);
        }

        using (sourceStream)
        {
            return PutFromStreamChunked(
                ns, key, sourceStream, totalSize, sourcePath, ct);
        }
    }

    // Chunked-upload loop against an arbitrary Stream of known length; extracted from
    // PutFromFileStreaming so tests can inject a custom (e.g. truncating) Stream.
    internal Result<PutValue, CacheError> PutFromStreamChunked(
        CacheNamespace ns, UInt128 key, Stream sourceStream, int totalSize,
        string sourcePathForDiagnostics, CancellationToken ct)
    {
        if (m_Handler is null)
            return new CacheError.NotConnected(ns);

        // Cancellation before GetNewValueIds closes the pre-first-chunk orphan window.
        if (ct.IsCancellationRequested)
            return new CacheError.Cancelled(ns);

        List<uint> valueIds;
        try
        {
            valueIds = m_Handler!.GetNewValueIds(m_Options.Organization, m_ProjectId, ns.Name, count: 1, m_Options.Token);
        }
        catch (Exception ex) when (IsRoutableV2Failure(ex))
        {
            return ExceptionTranslator.TranslateCache(ex, ns);
        }

        if (valueIds is null || valueIds.Count == 0)
            return new CacheError.ServerError(
                ns, "GetNewValueIds returned 0 ids", UnderlyingExceptionType: null);
        uint valueId = valueIds[0];

        byte[] buffer = FlexibleBufferPool.Shared.TakeBufferUsingConfiguredWaitTime(MaxDataPacketBytes);
        try
        {
            ulong segment = 0;
            int read = 0;
            int totalChunks = (totalSize + MaxDataPacketBytes - 1) / MaxDataPacketBytes;
            while (read < totalSize)
            {
                int chunkSize = Math.Min(MaxDataPacketBytes, totalSize - read);
                bool isLast = (read + chunkSize == totalSize);

                // ReadExactly throws EndOfStreamException (an IOException) on mid-upload
                // truncation, mapped to ServerError below.
                try
                {
                    // Stream.ReadExactly is net7+; use ReadExactlyCompat polyfill.
                    ReadExactlyCompat(sourceStream, buffer, chunkSize);
                }
                catch (Exception ex) when (ex is IOException)
                {
                    return new CacheError.ServerError(
                        ns,
                        $"Could not read file '{sourcePathForDiagnostics}': {ex.Message}",
                        UnderlyingExceptionType: ex.GetType().FullName);
                }

                var packet = new DataPacket();
                try
                {
                    var writer = packet.GetWriter();
                    writer.WriteData(valueId, 0, segment, isLast, buffer, chunkSize);
                    writer.EndWrite();
                    m_Handler.SetValuesData(m_Options.Organization, m_ProjectId, ns.Name, packet, m_Options.Token);
                }
                catch (Exception ex) when (IsRoutableV2Failure(ex))
                {
                    return ExceptionTranslator.TranslateCache(ex, ns);
                }
                finally
                {
                    packet.ReleaseBuffer();
                }

                read += chunkSize;
                segment++;

                if (read < totalSize && ct.IsCancellationRequested)
                {
                    Interlocked.Increment(ref s_CancelOrphanedValueIdCount);
                    // Trace (not Debug) so the diagnostic survives Release builds.
                    System.Diagnostics.Trace.WriteLine(
                        $"AccelClient.PutFromFileStreaming: cancellation between chunks left value-id {valueId} " +
                        $"orphaned in namespace {m_Options.Organization}/{ns.Name} " +
                        $"(segment {segment}/{totalChunks}); V2 server does not GC orphaned value-ids today.");
                    return new CacheError.Cancelled(ns);
                }
            }

            System.Diagnostics.Debug.Assert(read == totalSize,
                $"PutFromFileStreaming loop-termination invariant: read ({read}) != totalSize ({totalSize}). " +
                $"The chunked-upload loop must read exactly totalSize bytes before the SetKeyValues " +
                $"commit; an early exit would commit silently-corrupt server state for value-id {valueId}.");

            try
            {
                List<AcclibUInt128> keys = [ToAcclib(key)];
                List<ValueInfo> values = [new ValueInfo(valueId, totalSize, k_UnusedHash)];
                m_Handler.SetKeyValues(m_Options.Organization, m_ProjectId, ns.Name, keys, values, m_Options.Token);
                return Result<PutValue, CacheError>.Ok(
                    new PutValue(BytesUploaded: totalSize, ChunkCount: (int)segment));
            }
            catch (Exception ex) when (IsRoutableV2Failure(ex))
            {
                return ExceptionTranslator.TranslateCache(ex, ns);
            }
        }
        finally
        {
            FlexibleBufferPool.Shared.ReturnBuffer(buffer);
        }
    }

    // Multi-key fetch. Hits are present in the returned dict; absent keys are misses.
    // Callers MUST dispose the returned BatchGetRaw (its hits dict is pooled).
    public Result<BatchGetRaw, CacheError>
        GetBatch(CacheNamespace ns, IReadOnlyList<UInt128> keys, CancellationToken ct = default)
    {
        var startTicks = Stopwatch.GetTimestamp();
        var result = GetBatchImpl(ns, keys, ct);
        if (result.IsSuccess)
        {
            long bytesIn = 0;
            foreach (var kvp in result.Value.Hits)
                bytesIn += kvp.Value.Length;
            var hitCount = result.Value.Hits.Count;
            var faultCount = result.Value.Faults.Count;
            EmitOpSuccess(OpKind.GetBatch, startTicks, ns,
                keyCount: keys.Count,
                bytesIn: bytesIn,
                hits: hitCount,
                misses: keys.Count - hitCount - faultCount,
                faults: faultCount);
        }
        else
            EmitCacheFailure(OpKind.GetBatch, startTicks, ns, result.Error,
                keyCount: keys.Count);
        return result;
    }

    private Result<BatchGetRaw, CacheError>
        GetBatchImpl(CacheNamespace ns, IReadOnlyList<UInt128> keys, CancellationToken ct)
    {
        if (m_Handler is null)
            return new CacheError.NotConnected(ns);

        if (keys.Count == 0)
        {
            // Rent even for empty batches so the disposal contract is uniform.
            return Result<BatchGetRaw, CacheError>.Ok(new BatchGetRaw(
                DictionaryPool.Rent(0),
                new Dictionary<UInt128, CacheError>()));
        }

        // keys.Count == 1: single-call GetKeyValuesWithData shortcut. This read is
        // hard-rejected by V2 for values > 4 MiB; large-value callers must use GetToFile.
        if (keys.Count == 1)
        {
            var singleKey = keys[0];
            var perKeyResult = FetchSingleKeyValueWithDataOrNull(ns, singleKey, ct);
            if (!perKeyResult.IsSuccess)
                return perKeyResult.Error;
            var hits = DictionaryPool.Rent(1);
            var faulted = new Dictionary<UInt128, CacheError>();
            // null = miss (absent from hits); the single-call path has no per-key faults.
            if (perKeyResult.Value is { } bytes)
                hits[singleKey] = bytes;
            ApplyDownloadVerification(ns, hits, faulted);
            return Result<BatchGetRaw, CacheError>.Ok(new BatchGetRaw(hits, faulted));
        }

        // GetBatchChunked owns the multi-key download-side verification; no re-verify here.
        return GetBatchChunked(ns, keys, ct);
    }

    // No-op when verification is off. Moves each hash-mismatched entry from hits to faulted;
    // removals are buffered so hits is not mutated during enumeration.
    private void ApplyDownloadVerification(
        CacheNamespace ns,
        Dictionary<UInt128, ReadOnlyMemory<byte>> hits,
        Dictionary<UInt128, CacheError> faulted)
    {
        if (!m_Options.VerifyContentHashOnDownload || hits.Count == 0)
            return;

        // Faults entries are normalized to ServerError(UnderlyingExceptionType:
        // "ContentHashMismatch") to hold the BatchGetValue.Faults ServerError-only invariant;
        // the structured mismatch fields are preserved in the Message body.
        List<UInt128>? mismatchKeys = null;
        foreach (var kvp in hits)
        {
            UInt128 computed = ContentKeyStub.Unsupported(); // allowed: VerifyContentHashOnDownload-guarded (caller checked)
            if (!computed.Equals(kvp.Key))
            {
                mismatchKeys ??= new List<UInt128>();
                mismatchKeys.Add(kvp.Key);
                var mismatch = BuildContentHashMismatch(
                    ns, kvp.Key, computed, kvp.Value.Length, ContentHashVerificationDirection.Download);
                LogContentHashMismatchLine(m_Options.Organization, mismatch);
                faulted[kvp.Key] = new CacheError.ServerError(
                    ns,
                    Message: $"Content hash mismatch on Download: requested=0x{kvp.Key:X32}, "
                        + $"computed=0x{computed:X32}, bytes={kvp.Value.Length}",
                    UnderlyingExceptionType: "ContentHashMismatch");
            }
        }
        if (mismatchKeys is null)
            return;
        foreach (var key in mismatchKeys)
            hits.Remove(key);
    }

    #region Single-key Get entry points

    // No size hint: discovers the size then fetches via GetBatchChunked with N=1.
    public Result<CacheEntry, CacheError> GetSingle(
        CacheNamespace ns, UInt128 key, CancellationToken ct = default)
    {
        var startTicks = Stopwatch.GetTimestamp();
        var result = GetSingleImpl(ns, key, ct);
        if (result.IsSuccess)
            EmitOpSuccess(OpKind.Get, startTicks, ns,
                key: key,
                bytesIn: result.Value.Size,
                sizeHintProvided: false);
        else
            EmitCacheFailure(OpKind.Get, startTicks, ns, result.Error,
                key: key, sizeHintProvided: false);
        return result;
    }

    private Result<CacheEntry, CacheError> GetSingleImpl(
        CacheNamespace ns, UInt128 key, CancellationToken ct)
    {
        if (m_Handler is null)
            return new CacheError.NotConnected(ns);

        // Reuse GetBatchChunked with N=1 so discovery, routing, and fault partitioning apply.
        var batchResult = GetBatchChunked(ns, [key], ct);
        if (!batchResult.IsSuccess)
            return batchResult.Error;

        using var raw = batchResult.Value;
        if (raw.Hits.TryGetValue(key, out var bytes))
            return Result<CacheEntry, CacheError>.Ok(new CacheEntry(key, bytes, bytes.Length));
        if (raw.Faults.TryGetValue(key, out var fault))
            return fault;
        return new CacheError.NotFound(ns, key);
    }

    // Size-hint fetch: hints <= MaxDataPacketBytes take the one-round-trip fast path; larger
    // hints fall through to the no-hint discovery path. A too-small hint surfaces as ServerError
    // (SizeHintMismatch marker) rather than auto-retrying. Both branches emit sizeHintProvided: true.
    public Result<CacheEntry, CacheError> GetSingle(
        CacheNamespace ns, UInt128 key, long expectedSize, CancellationToken ct = default)
    {
        var startTicks = Stopwatch.GetTimestamp();
        Result<CacheEntry, CacheError> result;
        if (expectedSize > MaxDataPacketBytes)
        {
            result = GetSingleImpl(ns, key, ct);
        }
        else
        {
            result = GetSingleSmallHintImpl(ns, key, expectedSize, ct);
        }
        if (result.IsSuccess)
            EmitOpSuccess(OpKind.Get, startTicks, ns,
                key: key,
                bytesIn: result.Value.Size,
                sizeHintProvided: true);
        else
            EmitCacheFailure(OpKind.Get, startTicks, ns, result.Error,
                key: key, sizeHintProvided: true);
        return result;
    }

    private Result<CacheEntry, CacheError> GetSingleSmallHintImpl(
        CacheNamespace ns, UInt128 key, long expectedSize, CancellationToken ct)
    {
        if (m_Handler is null)
            return new CacheError.NotConnected(ns);

        // Reject negative hints: expectedSize < 0 would else satisfy the <= check and
        // silently take the small-hint fast path.
        if (expectedSize < 0)
            return new CacheError.InvalidArgument(
                nameof(expectedSize), "expectedSize must be non-negative.");

        var perKey = FetchSingleKeyValueWithDataOrNull(ns, key, ct);
        if (!perKey.IsSuccess)
            return perKey.Error;
        if (perKey.Value is not { } bytes)
            return new CacheError.NotFound(ns, key);

        // Own recompute site: this path does not route through GetBatchChunked.
        {
            var mismatch = VerifyDownloadHashOrFail(ns, key, bytes.Span);
            if (mismatch is not null)
                return mismatch;
        }

        return Result<CacheEntry, CacheError>.Ok(new CacheEntry(key, bytes, bytes.Length));
    }

    #endregion

    // Single-key V2 single-call read primitive: one round trip. Ok(null) on a miss, Ok(bytes)
    // on a hit, Fail on a translated failure. Hard-rejected by V2 for values > 4 MiB, so
    // size-unaware callers must route via GetBatchChunked.
    private Result<ReadOnlyMemory<byte>?, CacheError>
        FetchSingleKeyValueWithDataOrNull(CacheNamespace ns, UInt128 key, CancellationToken ct = default)
    {
        // Entry-only guard; the single V2 call itself is uninterruptible.
        if (ct.IsCancellationRequested)
            return new CacheError.Cancelled(ns);

        ValuesInfosWithData result;
        try
        {
            Scratch.SingleKey.Ensure();
            Scratch.SingleKey.s_Acclib!.Clear();
            Scratch.SingleKey.s_Acclib.Add(ToAcclib(key));
            result = m_Handler!.GetKeyValuesWithData(
                m_Options.Organization, m_ProjectId, ns.Name, Scratch.SingleKey.s_Acclib, m_Options.Token);
        }
        catch (Exception ex) when (IsRoutableV2Failure(ex))
        {
            return ExceptionTranslator.TranslateCache(ex, ns);
        }

        // Miss-shape: server returned no Values or no packet at all.
        if (result?.Values is null || result.Packet is null)
        {
            // The try/finally below is not entered here, so release the pooled packet
            // buffer explicitly to avoid leaking a non-null Packet paired with null Values.
            result?.Packet?.ReleaseBuffer();
            return Result<ReadOnlyMemory<byte>?, CacheError>.Ok(null);
        }

        try
        {
            // Short response (fewer Values than the 1 requested): ServerError, not a miss.
            if (result.Values.Count < 1)
            {
                return new CacheError.ServerError(ns,
                    $"Server returned {result.Values.Count} Values entries for 1 requested key.",
                    null);
            }

            if (IsValueInfoMiss(result.Values[0]))
                return Result<ReadOnlyMemory<byte>?, CacheError>.Ok(null);

            long expectedId = result.Values[0].Id;
            ReadOnlyMemory<byte>? hitBytes = null;
            var reader = result.Packet.GetReader();
            int packetEntriesRead = 0;
            try
            {
                while (!reader.Eof())
                {
                    var fileData = reader.ReadFileData();
                    packetEntriesRead++;
                    if (fileData.Data is null)
                        continue;

                    // Route by objId, not position: assert the server returned the value-id it claimed.
                    if (fileData.objId != expectedId)
                    {
                        return new CacheError.ServerError(ns,
                            $"Server returned packet entry with unknown objId {fileData.objId}.",
                            null);
                    }

                    // Defensive copy: decouple from the pooled packet buffer's lifetime.
                    var copy = new byte[fileData.Data.Length];
                    Buffer.BlockCopy(fileData.Data, 0, copy, 0, fileData.Data.Length);
                    hitBytes = new ReadOnlyMemory<byte>(copy);
                }
            }
            finally
            {
                reader.CloseRead();
            }

            // Server claimed a hit but the packet was genuinely empty (zero entries): ServerError.
            if (hitBytes is null && packetEntriesRead < 1)
            {
                return new CacheError.ServerError(ns,
                    $"Server returned {packetEntriesRead} packet entries for 1 expected hit.",
                    null);
            }

            return Result<ReadOnlyMemory<byte>?, CacheError>.Ok(hitBytes);
        }
        catch (Exception ex) when (IsRoutableV2Failure(ex))
        {
            return ExceptionTranslator.TranslateCache(ex, ns);
        }
        finally
        {
            result.Packet.ReleaseBuffer();
        }
    }

    // Chunked/split read for keys.Count >= 2: pre-discovers sizes via GetKeyValues, partitions
    // into budget groups (individually-big entries get their own segmented download), reassembles.
    private Result<BatchGetRaw, CacheError>
        GetBatchChunked(CacheNamespace ns, IReadOnlyList<UInt128> keys, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return new CacheError.Cancelled(ns);

        // Step 1: size discovery via GetKeyValues (no WithData); a throw here fails the whole batch.
        Scratch.Batch.Ensure();

        List<ValueInfo> infos;
        try
        {
            Scratch.Batch.s_KeyList!.Clear();
            foreach (var k in keys)
                Scratch.Batch.s_KeyList.Add(ToAcclib(k));
            infos = m_Handler!.GetKeyValues(m_Options.Organization, m_ProjectId, ns.Name, Scratch.Batch.s_KeyList, m_Options.Token);
        }
        catch (Exception ex) when (IsRoutableV2Failure(ex))
        {
            return ExceptionTranslator.TranslateCache(ex, ns);
        }

        var hits = DictionaryPool.Rent(keys.Count);
        var faulted = new Dictionary<UInt128, CacheError>();
        // On the success path BatchGetRaw owns 'hits'; the finally returns it to the pool
        // otherwise. Set true on the same statement as the Result.Ok so there is no leak window.
        bool ownershipTransferred = false;
        try
        {
        if (infos is null || infos.Count == 0)
        {
            var raw = new BatchGetRaw(hits, faulted);
            ownershipTransferred = true;
            return Result<BatchGetRaw, CacheError>.Ok(raw);
        }
        // Non-empty but shorter than requested = server contract violation; can't partition.
        if (infos.Count < keys.Count)
        {
            return new CacheError.ServerError(ns,
                $"Server returned {infos.Count} ValueInfo entries for {keys.Count} requested keys.",
                null);
        }

        // Step 2: classify the discovery response into (groupableInfos, idToKey, idToSize).
        var classification = ClassifyKeysBySize(infos, keys);
        var idToKey = classification.IdToKey;
        var idToSize = classification.IdToSize;
        var groupableInfos = classification.GroupableInfos;

        // All-miss + since-cancelled: the bucketing loop never runs, so honour cancellation here.
        if (groupableInfos.Count == 0 && ct.IsCancellationRequested)
            return new CacheError.Cancelled(ns);

        // Step 3: bucket groupableInfos; individually-big entries download per-segment immediately.
        long currentBudget = 0;
        var currentGroup = new List<uint>();
        foreach (var info in groupableInfos)
        {
            if (ct.IsCancellationRequested)
                return new CacheError.Cancelled(ns);

            uint id = (uint)info.Id;
            long size = info.Size;

            if (size > MaxDataPacketBytes)
            {
                long expectedSize = idToSize[info.Id];
                var bigResult = DownloadBigValueIntoHits(ns, id, expectedSize, idToKey, hits, faulted, ct);
                if (!bigResult.IsSuccess)
                    return bigResult.Error;
                continue;
            }

            if (currentGroup.Count > 0 && currentBudget + size > MaxDataPacketBytes)
            {
                var flushResult = DownloadSmallGroupIntoHits(ns, currentGroup, idToKey, hits, faulted);
                if (!flushResult.IsSuccess)
                    return flushResult.Error;
                currentGroup.Clear();
                currentBudget = 0;
            }

            currentGroup.Add(id);
            currentBudget += size;
        }
        if (currentGroup.Count > 0)
        {
            var flushResult = DownloadSmallGroupIntoHits(ns, currentGroup, idToKey, hits, faulted);
            if (!flushResult.IsSuccess)
                return flushResult.Error;
        }

        ApplyDownloadVerification(ns, hits, faulted);

        var result = new BatchGetRaw(hits, faulted);
        ownershipTransferred = true;
        return Result<BatchGetRaw, CacheError>.Ok(result);
        }
        finally
        {
            if (!ownershipTransferred)
                DictionaryPool.Return(hits);
        }
    }

    // WARNING: the three fields alias the calling thread's Scratch.Batch [ThreadStatic]
    // instances, mutated on the next ClassifyKeysBySize call. Do not store across an await
    // or re-enter ClassifyKeysBySize on the same thread while this result is in use.
    private readonly record struct ChunkedReadClassification(
        IReadOnlyList<ValueInfo> GroupableInfos,
        IReadOnlyDictionary<long, UInt128> IdToKey,
        IReadOnlyDictionary<long, long> IdToSize);

    // Builds the (GroupableInfos, idToKey, idToSize) partition from the discovery response,
    // filtering out misses. Precondition: Scratch.Batch.Ensure called on this thread.
    private static ChunkedReadClassification ClassifyKeysBySize(
        List<ValueInfo> infos,
        IReadOnlyList<UInt128> keys)
    {
        Scratch.Batch.s_IdToKey!.Clear();
        Scratch.Batch.s_IdToSize!.Clear();
        Scratch.Batch.s_GroupableInfos!.Clear();
        for (int i = 0; i < keys.Count; i++)
        {
            var info = infos[i];
            if (IsValueInfoMiss(info))
                continue;
            Scratch.Batch.s_IdToKey[info.Id] = keys[i];
            Scratch.Batch.s_IdToSize[info.Id] = info.Size;
            Scratch.Batch.s_GroupableInfos.Add(info);
        }
        return new ChunkedReadClassification(Scratch.Batch.s_GroupableInfos, Scratch.Batch.s_IdToKey, Scratch.Batch.s_IdToSize);
    }

    // Single GetValuesData call for a budget group of small values, reassembled via objId routing.
    // GetValuesData throw / null packet → per-group fault, loop continues; unknown objId or short
    // response → whole-batch ServerError (protocol corruption), early return.
    private Result<CacheError> DownloadSmallGroupIntoHits(
        CacheNamespace ns,
        List<uint> valueIds,
        IReadOnlyDictionary<long, UInt128> idToKey,
        Dictionary<UInt128, ReadOnlyMemory<byte>> hits,
        Dictionary<UInt128, CacheError> faulted)
    {
        DataPacket packet;
        try
        {
            // TODO extend [ThreadStatic] scratch to this per-flush List<ulong> allocation.
            // Segment 0 for each id (small values fit in one segment).
            var segments = new List<ulong>(valueIds.Count);
            for (int i = 0; i < valueIds.Count; i++)
                segments.Add(0UL);
            packet = m_Handler!.GetValuesData(
                m_Options.Organization, m_ProjectId, ns.Name, valueIds, segments, m_Options.Token);
        }
        catch (Exception ex) when (IsRoutableV2Failure(ex))
        {
            // Per-group fault: every key in this group fails together; other groups unaffected.
            string rawType = ex.GetType().FullName ?? ex.GetType().Name;
            string perGroupDiagnostic =
                $"Per-group GetValuesData call for {valueIds.Count} value-id(s) failed: "
                + $"[{rawType}] {ex.Message}";
            MarkGroupFaulted(valueIds, idToKey, faulted, ns, perGroupDiagnostic, sourceException: ex);
            return Result<CacheError>.Success;
        }

        if (packet is null)
        {
            // Server returned no packet for the group: per-group fault for each key.
            string perGroupDiagnostic =
                $"Per-group GetValuesData call for {valueIds.Count} value-id(s) returned a null packet "
                + "(server claims hits via the discovery call but cannot deliver bytes).";
            MarkGroupFaulted(valueIds, idToKey, faulted, ns, perGroupDiagnostic,
                sourceException: null);
            return Result<CacheError>.Success;
        }

        try
        {
            var reader = packet.GetReader();
            int packetEntriesRead = 0;
            // Track returned value-ids so a short-response failure can name the omitted ones.
            var returnedIds = new HashSet<long>(valueIds.Count);
            try
            {
                while (!reader.Eof())
                {
                    var fileData = reader.ReadFileData();
                    packetEntriesRead++;
                    returnedIds.Add(fileData.objId);
                    // Null-Data entry for a known objId is observed as a miss (neither hits nor faulted).
                    if (fileData.Data is null)
                        continue;
                    if (!idToKey.TryGetValue(fileData.objId, out var key))
                    {
                        // Unknown objId: packet-level corruption taints every entry, so whole-batch.
                        return new CacheError.ServerError(ns,
                            $"Server returned packet entry with unknown objId {fileData.objId}.",
                            null);
                    }
                    var copy = new byte[fileData.Data.Length];
                    Buffer.BlockCopy(fileData.Data, 0, copy, 0, fileData.Data.Length);
                    hits[key] = new ReadOnlyMemory<byte>(copy);
                }
            }
            finally
            {
                reader.CloseRead();
            }
            // Packet entries < expected: short response violates the V2 protocol contract, whole-batch.
            if (packetEntriesRead < valueIds.Count)
            {
                // Cap the missing-id enumeration so a pathological all-missing case stays log-friendly.
                const int k_MaxMissingIdsToReport = 8;
                var missingDetail = BuildMissingValueIdDiagnostic(
                    valueIds, returnedIds, idToKey, k_MaxMissingIdsToReport);
                return new CacheError.ServerError(ns,
                    $"Server returned {packetEntriesRead} packet entries for {valueIds.Count} expected hits."
                    + $" {missingDetail}",
                    null);
            }
            return Result<CacheError>.Success;
        }
        catch (Exception ex) when (IsRoutableV2Failure(ex))
        {
            // A reader throw on a received packet is protocol corruption: whole-batch.
            return ExceptionTranslator.TranslateCache(ex, ns);
        }
        finally
        {
            packet.ReleaseBuffer();
        }
    }

    // Builds the missing-value-id detail string for a short-response diagnostic, capped at maxToReport.
    private static string BuildMissingValueIdDiagnostic(
        List<uint> requestedIds,
        HashSet<long> returnedIds,
        IReadOnlyDictionary<long, UInt128> idToKey,
        int maxToReport)
    {
        var missing = new List<uint>(requestedIds.Count);
        for (int i = 0; i < requestedIds.Count; i++)
        {
            if (!returnedIds.Contains(requestedIds[i]))
                missing.Add(requestedIds[i]);
        }
        if (missing.Count == 0)
        {
            // Fallback: counts mismatch but nothing missing means the server duplicated entries.
            return "All requested value-ids were returned at least once — server likely duplicated entries.";
        }
        var sb = new System.Text.StringBuilder(128);
        sb.Append("Missing value-ids: ");
        int reported = Math.Min(missing.Count, maxToReport);
        for (int i = 0; i < reported; i++)
        {
            if (i > 0) sb.Append(' ');
            var id = missing[i];
            sb.Append('[').Append(id).Append(" (key=");
            if (idToKey.TryGetValue(id, out var key))
                // DLL UInt128 has no ToString(format); hex the two halves.
                sb.Append("0x").Append(((ulong)(key >> 64)).ToString("X16")).Append(((ulong)key).ToString("X16"));
            else
                sb.Append("<unmapped>");
            sb.Append(")]");
        }
        if (missing.Count > reported)
        {
            sb.Append(" ... (").Append(missing.Count - reported).Append(" more elided)");
        }
        return sb.ToString();
    }

    // Writes one ServerError into faulted for every value-id in a failed group. Its
    // UnderlyingExceptionType carries the classified variant the failure would have surfaced as
    // on the whole-batch rail (or "NullPacket" when sourceException is null).
    private static void MarkGroupFaulted(
        List<uint> valueIds,
        IReadOnlyDictionary<long, UInt128> idToKey,
        Dictionary<UInt128, CacheError> faulted,
        CacheNamespace ns,
        string perGroupDiagnostic,
        Exception? sourceException)
    {
        // Hoisted out of the loop: every value-id in the group gets the same fault.
        string classifiedUnderlyingType = ClassifyAsUnderlyingType(sourceException, ns);
        var perGroupError = new CacheError.ServerError(
            ns, perGroupDiagnostic, UnderlyingExceptionType: classifiedUnderlyingType);

        foreach (var vid in valueIds)
        {
            if (!idToKey.TryGetValue(vid, out var key))
            {
                // Internal invariant violation: ClassifyKeysBySize should have mapped every value-id.
                // Assert in debug; Trace + skip in release (preserves prior behaviour) for post-mortem.
                System.Diagnostics.Debug.Assert(false,
                    $"MarkGroupFaulted invariant: idToKey missing for valueId {vid}; "
                    + "see — every valueId in this loop should have been "
                    + "inserted into idToKey by GetBatchChunked.ClassifyKeysBySize.");
                System.Diagnostics.Trace.WriteLine(
                    $"[AccelClient] MarkGroupFaulted: invariant violation — valueId {vid} not in idToKey; "
                    + "key dropped from BatchGetValue shape. See .");
                continue;
            }
            faulted[key] = perGroupError;
        }
    }

    // Translates sourceException to a string discriminator for ServerError.UnderlyingExceptionType,
    // so retry-policy callers can switch on it (e.g. "ConnectionLost"). "NullPacket" when null.
    private static string ClassifyAsUnderlyingType(Exception? sourceException, CacheNamespace ns)
    {
        if (sourceException is null)
        {
            return "NullPacket";
        }

        var translated = ExceptionTranslator.TranslateCache(sourceException, ns);
        return translated switch
        {
            CacheError.ConnectionLost => nameof(CacheError.ConnectionLost),
            CacheError.NotAuthorized => nameof(CacheError.NotAuthorized),
            CacheError.NotFound => nameof(CacheError.NotFound),
            CacheError.Cancelled => nameof(CacheError.Cancelled),
            // ServerError sub-variants keep the translator's discriminator; fall back to the CLR type.
            CacheError.ServerError serverError =>
                serverError.UnderlyingExceptionType
                    ?? sourceException.GetType().FullName
                    ?? "Unknown",
            _ => sourceException.GetType().FullName ?? "Unknown",
        };
    }

    // Per-segment GetValuesData loop for an individually-large value, accumulated into memory.
    // Post-loop truncation guard compares accumulated bytes + sawLast against expectedSize.
    // Truncation / V2 throw → per-key fault, loop continues; cancellation → whole-batch.
    // WARNING: OOM and BufferPoolMemoryStream "Stream was too long" IOException are not routed
    // into faulted/ExceptionTranslator (no leak — Dispose still runs — just an error-translation gap).
    private Result<CacheError> DownloadBigValueIntoHits(
        CacheNamespace ns,
        uint valueId,
        long expectedSize,
        IReadOnlyDictionary<long, UInt128> idToKey,
        Dictionary<UInt128, ReadOnlyMemory<byte>> hits,
        Dictionary<UInt128, CacheError> faulted,
        CancellationToken ct)
    {
        if (!idToKey.TryGetValue(valueId, out var key))
        {
            // Only fires on a programmer-error mis-wire; whole-batch fail keeps it loud.
            return new CacheError.ServerError(ns,
                $"Internal: idToKey lookup missing for value-id {valueId}", null);
        }

        // Pre-sized BufferPoolMemoryStream: one rental covers the whole payload, no LOH grow-churn.
        // GetContentCopy (below) sizes the hits entry to exactly mLength, not the pool bucket size.
        DownloadHasherStub? hasher = null;
        // Clamp before narrowing to int: a value > int.MaxValue would wrap negative and defeat
        // Math.Max(1,...), and > pool.MaxBufferSize would make TakeBuffer throw.
        int poolMax = FlexibleBufferPool.Shared.MaxBufferSize;
        int initialCapacity = Math.Max(1, (int)Math.Min(expectedSize, poolMax > 0 ? poolMax : int.MaxValue));
        using var ms = new BufferPoolMemoryStream(FlexibleBufferPool.Shared, initialCapacity);
        ulong segment = 0;
        bool sawLastSegment = false;
        while (true)
        {
            DataPacket? packet;
            try
            {
                packet = m_Handler!.GetValuesData(
                    m_Options.Organization, m_ProjectId, ns.Name,
                    [valueId],
                    [segment],
                    m_Options.Token);
            }
            catch (Exception ex) when (IsRoutableV2Failure(ex))
            {
                // Per-key fault: raw type in the message for triage, classified variant in the discriminator.
                string rawType = ex.GetType().FullName ?? ex.GetType().Name;
                string classifiedUnderlyingType = ClassifyAsUnderlyingType(ex, ns);
                faulted[key] = new CacheError.ServerError(ns,
                    $"Per-segment GetValuesData call for value-id {valueId} failed mid-stream: "
                    + $"[{rawType}] {ex.Message}",
                    UnderlyingExceptionType: classifiedUnderlyingType);
                return Result<CacheError>.Success;
            }

            if (packet is null)
                break;

            bool sawLast;
            try
            {
                var reader = packet.GetReader();
                try
                {
                    var fileData = reader.ReadFileData();
                    if (fileData.Data is not null && fileData.Data.Length > 0)
                    {
                        ms.Write(fileData.Data, 0, fileData.Data.Length);
                        // allowed: VerifyContentHashOnDownload-guarded
                        hasher?.Append(fileData.Data.AsSpan(0, fileData.Data.Length));
                    }
                    sawLast = fileData.bLastSegment;
                }
                finally
                {
                    reader.CloseRead();
                }
            }
            finally
            {
                packet.ReleaseBuffer();
            }

            if (sawLast)
            {
                sawLastSegment = true;
                break;
            }
            segment++;

            if (ct.IsCancellationRequested)
                return new CacheError.Cancelled(ns);
        }

        // Read-side truncation guard: mismatched length or no bLastSegment is a
        // server-side contract violation; per-key fault, outer loop continues.
        if (ms.Length != expectedSize || !sawLastSegment)
        {
            faulted[key] = new CacheError.ServerError(ns,
                $"Truncated download for value-id {valueId}: expected {expectedSize} bytes "
                + $"in {m_Options.Organization}/{ns.Name}, got {ms.Length} (sawLast={sawLastSegment}). "
                + "Server returned an early null packet or a premature bLastSegment=true; "
                + "this indicates corrupt server-side state for this value — "
                + "no GC of orphaned uploads, no client-callable cleanup API yet. The "
                + "value is treated as a server-error; the caller may retry but the "
                + "underlying data is unrecoverable without a re-upload.",
                null);
            return Result<CacheError>.Success;
        }

        var bytes = ms.GetContentCopy();
        if (hasher is not null)
        {
            UInt128 computed = ContentKeyStub.Unsupported();
            if (!computed.Equals(key))
            {
                var mismatch = BuildContentHashMismatch(
                    ns, key, computed, bytes.Length,
                    ContentHashVerificationDirection.Download);
                LogContentHashMismatchLine(m_Options.Organization, mismatch);
                faulted[key] = new CacheError.ServerError(
                    ns,
                    Message: $"Content hash mismatch on Download: requested=0x{key:X32}, "
                        + $"computed=0x{computed:X32}, bytes={bytes.Length}",
                    UnderlyingExceptionType: "ContentHashMismatch");
                return Result<CacheError>.Success;
            }
        }
        hits[key] = new ReadOnlyMemory<byte>(bytes);
        return Result<CacheError>.Success;
    }

    // Streams a single value to disk via the segmented GetValuesData loop (never fully in memory).
    // FileMode.Create truncates any existing file; on cancellation/failure the partial file is
    // best-effort deleted and the destination path is untrusted. Returned GetValue.Bytes is default.
    public Result<GetValue, CacheError> GetToFile(
        CacheNamespace ns, UInt128 key, string destinationPath, CancellationToken ct = default)
    {
        var startTicks = Stopwatch.GetTimestamp();
        var result = GetToFileImpl(ns, key, destinationPath, ct);
        if (result.IsSuccess)
            EmitOpSuccess(OpKind.GetToFile, startTicks, ns,
                key: key,
                bytesIn: result.Value.Size,
                sizeHintProvided: false,
                destinationPath: destinationPath);
        else
            EmitCacheFailure(OpKind.GetToFile, startTicks, ns, result.Error,
                key: key, sizeHintProvided: false, destinationPath: destinationPath);
        return result;
    }

    private Result<GetValue, CacheError> GetToFileImpl(
        CacheNamespace ns, UInt128 key, string destinationPath, CancellationToken ct)
    {
        if (m_Handler is null)
            return new CacheError.NotConnected(ns);
        if (string.IsNullOrEmpty(destinationPath))
            return new CacheError.InvalidArgument(
                nameof(destinationPath), "destination path required");

        // Step 1: discover size + value-id via GetKeyValues (no WithData).
        List<ValueInfo> infos;
        try
        {
            List<AcclibUInt128> keys = [ToAcclib(key)];
            infos = m_Handler.GetKeyValues(m_Options.Organization, m_ProjectId, ns.Name, keys, m_Options.Token);
        }
        catch (Exception ex) when (IsRoutableV2Failure(ex))
        {
            return ExceptionTranslator.TranslateCache(ex, ns);
        }

        if (infos is null || infos.Count == 0 || IsValueInfoMiss(infos[0]))
            return new CacheError.NotFound(ns, key);
        var info = infos[0];
        long totalSize = info.Size;
        uint valueId = (uint)info.Id;

        // Step 2: open destination. FileShare.None excludes readers of the half-written file;
        // the 81920-byte buffer suits the per-segment Writes (the single-shot path uses bufferSize:1).
        var openResult = TryOpenDestinationForWrite(
            ns, destinationPath, bufferSize: 81920, FileOptions.None);
        if (!openResult.IsSuccess)
            return openResult.Error;
        FileStream? fs = openResult.Value;
        try
        {
            // Step 3: segmented GetValuesData loop. When verification is on, the hasher tees
            // each segment during the write so Step 5 needs no disk re-read (no TOCTOU window).
            DownloadHasherStub? hasher = null;
            ulong segment = 0;
            long bytesWritten = 0;
            bool sawLastSegment = false;
            while (true)
            {
                DataPacket? packet;
                try
                {
                    packet = m_Handler.GetValuesData(
                        m_Options.Organization, m_ProjectId, ns.Name,
                        [valueId],
                        [segment],
                        m_Options.Token);
                }
                catch (Exception ex) when (IsRoutableV2Failure(ex))
                {
                    // Close + best-effort delete the partial file before returning.
                    fs.Dispose(); fs = null;
                    BestEffortDeletePartialFile(destinationPath);
                    return ExceptionTranslator.TranslateCache(ex, ns);
                }

                if (packet is null)
                {
                    // No packet: treat as end-of-stream.
                    break;
                }

                bool sawLast;
                try
                {
                    var reader = packet.GetReader();
                    try
                    {
                        var fileData = reader.ReadFileData();
                        if (fileData.Data is not null && fileData.Data.Length > 0)
                        {
                            try
                            {
                                fs.Write(fileData.Data, 0, fileData.Data.Length);
                                bytesWritten += fileData.Data.Length;
                                // allowed: VerifyContentHashOnDownload-guarded
                                hasher?.Append(fileData.Data.AsSpan(0, fileData.Data.Length));
                            }
                            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
                            {
                                // Host-side write failure (disk full, etc.): close + delete partial.
                                fs.Dispose(); fs = null;
                                BestEffortDeletePartialFile(destinationPath);
                                return new CacheError.ServerError(
                                    ns,
                                    $"Could not write destination file '{destinationPath}': {ex.Message}",
                                    UnderlyingExceptionType: ex.GetType().FullName);
                            }
                        }
                        sawLast = fileData.bLastSegment;
                    }
                    finally
                    {
                        reader.CloseRead();
                    }
                }
                finally
                {
                    packet.ReleaseBuffer();
                }

                if (sawLast)
                {
                    sawLastSegment = true;
                    break;
                }
                segment++;

                // Cancellation after ReleaseBuffer so the per-segment packet isn't leaked.
                if (ct.IsCancellationRequested)
                {
                    // Close the handle before delete so it isn't blocked; partial file is untrusted.
                    fs.Dispose(); fs = null;
                    BestEffortDeletePartialFile(destinationPath);
                    return new CacheError.Cancelled(ns);
                }
            }

            // Read-side truncation guard: the V2 server does not validate download completeness,
            // so an early null packet or premature bLastSegment yields a truncated file. Unlike the
            // upload-side Debug.Assert, this is a real runtime condition, so it returns ServerError.
            if (bytesWritten != totalSize || !sawLastSegment)
            {
                fs.Dispose(); fs = null;
                BestEffortDeletePartialFile(destinationPath);
                return new CacheError.ServerError(
                    ns,
                    $"Truncated download to '{destinationPath}': expected {totalSize} bytes in "
                    + $"{m_Options.Organization}/{ns.Name}, got {bytesWritten} (sawLast={sawLastSegment}). "
                    + "Server returned an early null packet or a premature bLastSegment=true; "
                    + "this indicates corrupt server-side state for this value — "
                    + "no GC of orphaned uploads, no client-callable cleanup API yet. The "
                    + "partial destination file has been best-effort deleted.",
                    null);
            }

            // Step 4: flush before returning so immediate readers see the complete bytes.
            try
            {
                fs.Flush();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                fs.Dispose(); fs = null;
                BestEffortDeletePartialFile(destinationPath);
                return new CacheError.ServerError(
                    ns,
                    $"Could not flush destination file '{destinationPath}': {ex.Message}",
                    UnderlyingExceptionType: ex.GetType().FullName);
            }

            // Step 5: verify the accumulated hash (no disk re-read); delete on mismatch.
            if (hasher is not null)
            {
                UInt128 computed = ContentKeyStub.Unsupported();
                if (!computed.Equals(key))
                {
                    fs.Dispose(); fs = null;
                    BestEffortDeletePartialFile(destinationPath);
                    var mismatch = BuildContentHashMismatch(
                        ns, key, computed, (int)Math.Min(totalSize, int.MaxValue),
                        ContentHashVerificationDirection.Download);
                    LogContentHashMismatchLine(m_Options.Organization, mismatch);
                    return mismatch;
                }
            }

            // Bytes is default: the payload went to the file, only Size is meaningful.
            return Result<GetValue, CacheError>.Ok(new GetValue(default, totalSize));
        }
        finally
        {
            fs?.Dispose();
        }
    }

    // Size-hint variant: hints <= MaxDataPacketBytes take the single-call fast path, larger hints
    // fall through to the no-hint streamed path. Both branches emit sizeHintProvided: true.
    public Result<GetValue, CacheError> GetToFile(
        CacheNamespace ns, UInt128 key, string destinationPath, long expectedSize,
        CancellationToken ct = default)
    {
        var startTicks = Stopwatch.GetTimestamp();
        Result<GetValue, CacheError> result;
        if (expectedSize > MaxDataPacketBytes)
        {
            result = GetToFileImpl(ns, key, destinationPath, ct);
        }
        else
        {
            result = GetToFileSmallHintImpl(ns, key, destinationPath, expectedSize, ct);
        }
        if (result.IsSuccess)
            EmitOpSuccess(OpKind.GetToFile, startTicks, ns,
                key: key,
                bytesIn: result.Value.Size,
                sizeHintProvided: true,
                destinationPath: destinationPath);
        else
            EmitCacheFailure(OpKind.GetToFile, startTicks, ns, result.Error,
                key: key, sizeHintProvided: true, destinationPath: destinationPath);
        return result;
    }

    private Result<GetValue, CacheError> GetToFileSmallHintImpl(
        CacheNamespace ns, UInt128 key, string destinationPath, long expectedSize,
        CancellationToken ct)
    {
        if (m_Handler is null)
            return new CacheError.NotConnected(ns);
        if (string.IsNullOrEmpty(destinationPath))
            return new CacheError.InvalidArgument(
                nameof(destinationPath), "destination path required");

        // Reject negative hints (symmetric with the in-memory size-hint GetSingle).
        if (expectedSize < 0)
            return new CacheError.InvalidArgument(
                nameof(expectedSize), "expectedSize must be non-negative.");

        return WriteSingleHitToFileFast(ns, key, destinationPath, ct);
    }

    // Small-hint fast path: one V2 round-trip via FetchSingleKeyValueWithDataOrNull, then a single
    // Write to disk. Same destination-open + host-side-I/O -> ServerError semantics as the no-hint path.
    private Result<GetValue, CacheError> WriteSingleHitToFileFast(
        CacheNamespace ns, UInt128 key, string destinationPath, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
            return new CacheError.Cancelled(ns);

        var perKey = FetchSingleKeyValueWithDataOrNull(ns, key, ct);
        if (!perKey.IsSuccess)
            return perKey.Error;
        if (perKey.Value is not { } bytes)
            return new CacheError.NotFound(ns, key);

        // Verify before the write so a mismatched value never touches the destination path.
        {
            var mismatch = VerifyDownloadHashOrFail(ns, key, bytes.Span);
            if (mismatch is not null)
                return mismatch;
        }

        // bufferSize: 1 + SequentialScan: the default 81920-byte buffer is wasted for a one-shot Write.
        var openResult = TryOpenDestinationForWrite(
            ns, destinationPath, bufferSize: 1, FileOptions.SequentialScan);
        if (!openResult.IsSuccess)
            return openResult.Error;
        using var fs = openResult.Value;
        return BestEffortWriteAll(fs, bytes, destinationPath, ns, bytes.Length);
    }

    // FileShare.None gives exclusive write access (a concurrent reader of a half-written file would see
    // corrupt bytes). bufferSize is parameterised: 81920 for the streamed path, 1 for the single-shot.
    private static Result<FileStream, CacheError> TryOpenDestinationForWrite(
        CacheNamespace ns, string destinationPath, int bufferSize, FileOptions fileOptions)
    {
        try
        {
            var fs = new FileStream(
                destinationPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize,
                fileOptions);
            return fs;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return new CacheError.ServerError(
                ns,
                $"Could not open destination file '{destinationPath}': {ex.Message}",
                UnderlyingExceptionType: ex.GetType().FullName);
        }
    }

    // Single-shot Write + Flush; on host-side I/O failure suppresses the dispose throw,
    // best-effort deletes the partial file, and returns ServerError.
    private static Result<GetValue, CacheError> BestEffortWriteAll(
        FileStream fs, ReadOnlyMemory<byte> payload, string destinationPath,
        CacheNamespace ns, long size)
    {
        try
        {
            fs.Write(payload.Span);
            fs.Flush();
            return new GetValue(default, size);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            try { fs.Dispose(); } catch { /* best-effort; suppress to reach cleanup */ }
            BestEffortDeletePartialFile(destinationPath);
            return new CacheError.ServerError(
                ns,
                $"Could not write destination file '{destinationPath}': {ex.Message}",
                UnderlyingExceptionType: ex.GetType().FullName);
        }
    }

    // ArgumentException is included so a malformed path the open accepted but Delete rejects
    // does not escape and crash the worker.
    internal static void BestEffortDeletePartialFile(string destinationPath)
    {
        try
        {
            File.Delete(destinationPath);
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or ArgumentException)
        {
            System.Diagnostics.Debug.WriteLine(
                $"AccelClient.GetToFile: partial-file delete failed at '{destinationPath}': {ex.Message}");
        }
    }

    // Multi-key delete; empty keys returns success without a wire call. V2 deletes are idempotent.
    // TODO extend [ThreadStatic] scratch to the per-call List<AcclibUInt128> allocation.
    public Result<CacheError> Delete(CacheNamespace ns, IReadOnlyList<UInt128> keys)
    {
        var startTicks = Stopwatch.GetTimestamp();
        var result = DeleteImpl(ns, keys);
        if (result.IsSuccess)
            EmitOpSuccess(OpKind.Delete, startTicks, ns, keyCount: keys.Count);
        else
            EmitCacheFailure(OpKind.Delete, startTicks, ns, result.Error,
                keyCount: keys.Count);
        return result;
    }

    private Result<CacheError> DeleteImpl(CacheNamespace ns, IReadOnlyList<UInt128> keys)
    {
        if (m_Handler is null)
            return new CacheError.NotConnected(ns);

        if (keys.Count == 0)
            return Result<CacheError>.Success;

        try
        {
            var keyList = new List<AcclibUInt128>(keys.Count);
            foreach (var k in keys)
                keyList.Add(ToAcclib(k));
            m_Handler.DeleteKeys(m_Options.Organization, m_ProjectId, ns.Name, keyList, m_Options.Token);
            return Result<CacheError>.Success;
        }
        catch (Exception ex) when (IsRoutableV2Failure(ex))
        {
            return ExceptionTranslator.TranslateCache(ex, ns);
        }
    }

    // Multi-key upload: big entries (> MaxDataPacketBytes) upload first via PutChunked, then small
    // entries are packed into 4 MiB flushes. NOT transactional: mid-batch cancellation leaves
    // already-committed entries visible without surfacing partial counts.
    public Result<BatchPutValue, CacheError> PutBatch(
        CacheNamespace ns,
        IReadOnlyList<KeyValuePair<UInt128, ReadOnlyMemory<byte>>> entries,
        CancellationToken ct = default)
    {
        var startTicks = Stopwatch.GetTimestamp();
        var result = PutBatchImpl(ns, entries, ct);
        if (result.IsSuccess)
            EmitOpSuccess(OpKind.PutBatch, startTicks, ns,
                keyCount: entries.Count,
                bytesOut: result.Value.BytesUploaded);
        else
            EmitCacheFailure(OpKind.PutBatch, startTicks, ns, result.Error,
                keyCount: entries.Count);
        return result;
    }

    private Result<BatchPutValue, CacheError> PutBatchImpl(
        CacheNamespace ns,
        IReadOnlyList<KeyValuePair<UInt128, ReadOnlyMemory<byte>>> entries,
        CancellationToken ct)
    {
        if (m_Handler is null)
            return new CacheError.NotConnected(ns);

        // Empty-value rejection before verification, so an empty + key-mismatch entry surfaces
        // as the more specific InvalidArgument rather than ContentHashMismatch.
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Value.IsEmpty)
                return new CacheError.InvalidArgument(
                    nameof(entries),
                    $"entries[{i}].Value must not be empty; the V2 cache does not store empty values.");
        }

        if (entries.Count == 0)
            return Result<BatchPutValue, CacheError>.Ok(new BatchPutValue(0, 0, 0));

        // Per-entry verification, fail-fast on first mismatch.
        if (m_Options.VerifyContentHashOnUpload)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var mismatch = VerifyUploadHashOrFail(ns, entry.Key, entry.Value.Span);
                if (mismatch is not null)
                    return mismatch;
            }
        }

        // Partition into big (per-entry chunked) and small (packed), preserving order within each.
        var bigEntries = new List<KeyValuePair<UInt128, ReadOnlyMemory<byte>>>();
        var smallEntries = new List<KeyValuePair<UInt128, ReadOnlyMemory<byte>>>();
        foreach (var kvp in entries)
        {
            if (kvp.Value.Length > MaxDataPacketBytes)
                bigEntries.Add(kvp);
            else
                smallEntries.Add(kvp);
        }

        int succeededCount = 0;
        long bytesUploaded = 0;

        // Big-entries pass: one PutChunked per entry; a failed entry is not counted as succeeded.
        foreach (var entry in bigEntries)
        {
            if (ct.IsCancellationRequested)
                return new CacheError.Cancelled(ns);

            var bigResult = PutChunked(ns, entry.Key, entry.Value, ct);
            if (!bigResult.IsSuccess)
                return bigResult.Error;
            succeededCount++;
            bytesUploaded += entry.Value.Length;
        }

        // Cancellation at the big/small seam, before any small-entries flush.
        if (ct.IsCancellationRequested)
            return new CacheError.Cancelled(ns);

        // Small-entries packing pass: flush currentBatch when the next entry would exceed the budget.
        var currentBatch = new List<KeyValuePair<UInt128, ReadOnlyMemory<byte>>>();
        long currentBatchSize = 0;
        foreach (var entry in smallEntries)
        {
            if (ct.IsCancellationRequested)
                return new CacheError.Cancelled(ns);

            if (currentBatch.Count > 0 && currentBatchSize + entry.Value.Length > MaxDataPacketBytes)
            {
                var flushResult = FlushSmallBatch(ns, currentBatch);
                if (!flushResult.IsSuccess)
                    return flushResult.Error;
                succeededCount += currentBatch.Count;
                bytesUploaded += currentBatchSize;
                currentBatch.Clear();
                currentBatchSize = 0;

                // Between-flushes cancellation honour point.
                if (ct.IsCancellationRequested)
                    return new CacheError.Cancelled(ns);
            }
            currentBatch.Add(entry);
            currentBatchSize += entry.Value.Length;
        }
        if (currentBatch.Count > 0)
        {
            var flushResult = FlushSmallBatch(ns, currentBatch);
            if (!flushResult.IsSuccess)
                return flushResult.Error;
            succeededCount += currentBatch.Count;
            bytesUploaded += currentBatchSize;
        }

        return Result<BatchPutValue, CacheError>.Ok(
            new BatchPutValue(
                Succeeded: succeededCount,
                Failed: 0,
                BytesUploaded: bytesUploaded));
    }

    // Multi-key streaming-from-file upload: each entry is streamed via PutFromFileStreaming (no
    // whole-file LOH allocation). Cancellation honoured between entries; first per-entry failure
    // short-circuits, previously-committed entries remain visible and counted in Succeeded.
    internal Result<BatchPutValue, CacheError> PutBatchFromFiles(
        CacheNamespace ns,
        IReadOnlyList<KeyValuePair<UInt128, string>> entries,
        CancellationToken ct)
    {
        var startTicks = Stopwatch.GetTimestamp();
        var result = PutBatchFromFilesImpl(ns, entries, ct);
        if (result.IsSuccess)
            EmitOpSuccess(OpKind.PutFromFiles, startTicks, ns,
                keyCount: entries.Count,
                bytesOut: result.Value.BytesUploaded);
        else
            EmitCacheFailure(OpKind.PutFromFiles, startTicks, ns, result.Error,
                keyCount: entries.Count);
        return result;
    }

    private Result<BatchPutValue, CacheError> PutBatchFromFilesImpl(
        CacheNamespace ns,
        IReadOnlyList<KeyValuePair<UInt128, string>> entries,
        CancellationToken ct)
    {
        if (m_Handler is null)
            return new CacheError.NotConnected(ns);

        if (entries.Count == 0)
            return Result<BatchPutValue, CacheError>.Ok(new BatchPutValue(0, 0, 0));

        int succeededCount = 0;
        long bytesUploaded = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            // Cancellation here does not leak: no value-id allocated for entries[i] yet.
            if (ct.IsCancellationRequested)
                return new CacheError.Cancelled(ns);

            var entry = entries[i];

            // Non-emitting Impl variant so the batch emits exactly one PutFromFiles event.
            var entryResult = PutFromFileStreamingImpl(ns, entry.Key, entry.Value, ct);
            if (!entryResult.IsSuccess)
            {
                // Surface PutFromFileStreaming's path-aware per-entry diagnostic verbatim.
                return entryResult.Error;
            }
            succeededCount++;
            bytesUploaded += entryResult.Value.BytesUploaded;
        }

        return Result<BatchPutValue, CacheError>.Ok(
            new BatchPutValue(
                Succeeded: succeededCount,
                Failed: 0,
                BytesUploaded: bytesUploaded));
    }

    // Single-call SetKeyValuesWithData flush of a small-entries batch that fits in one DataPacket.
    // TODO extend [ThreadStatic] scratch to the 3 per-flush List<T> allocations.
    private Result<CacheError> FlushSmallBatch(
        CacheNamespace ns,
        IReadOnlyList<KeyValuePair<UInt128, ReadOnlyMemory<byte>>> batch)
    {
        var packet = new DataPacket();
        try
        {
            var keys   = new List<AcclibUInt128>(batch.Count);
            var sizes  = new List<int>(batch.Count);
            var hashes = new List<ulong>(batch.Count);

            var writer = packet.GetWriter();
            foreach (var kvp in batch)
            {
                keys.Add(ToAcclib(kvp.Key));
                sizes.Add(kvp.Value.Length);
                hashes.Add(k_UnusedHash);

                WritePooledData(writer, kvp.Value);
            }
            writer.EndWrite();

            m_Handler!.SetKeyValuesWithData(
                m_Options.Organization, m_ProjectId, ns.Name, keys, sizes, hashes, packet, m_Options.Token);
            return Result<CacheError>.Success;
        }
        catch (Exception ex) when (IsRoutableV2Failure(ex))
        {
            return ExceptionTranslator.TranslateCache(ex, ns);
        }
        finally
        {
            packet.ReleaseBuffer();
        }
    }

    // BCL System.UInt128 -> the vendored DLL's polyfilled System.UInt128; identical (lower, upper) layout.
    private static AcclibUInt128 ToAcclib(UInt128 key)
    {
        // DLL UInt128 has no & operator; op_Explicit(->ulong) yields the low 64 bits.
        ulong lower = (ulong)key;
        ulong upper = (ulong)(key >> 64);
        return new AcclibUInt128(upper, lower);
    }

    // Stream.ReadExactly polyfill (net7+): fills exactly 'count' bytes
    // or throws EndOfStreamException.
    private static void ReadExactlyCompat(Stream stream, byte[] buffer, int count)
    {
        int total = 0;
        while (total < count)
        {
            int read = stream.Read(buffer, total, count - total);
            if (read == 0)
                throw new EndOfStreamException();
            total += read;
        }
    }

    // Shared TakeBuffer -> CopyTo -> WriteData -> ReturnBuffer recipe. The explicit dataLen keeps
    // the pool's power-of-two-rounded buffer tail from being written.
    private static void WritePooledData(DataPacketWriter writer, ReadOnlyMemory<byte> value)
    {
        byte[] buffer = FlexibleBufferPool.Shared.TakeBufferUsingConfiguredWaitTime(value.Length);
        try
        {
            value.Span.CopyTo(buffer);
            writer.WriteData(0, 0, 0, true, buffer, value.Length);
        }
        finally
        {
            FlexibleBufferPool.Shared.ReturnBuffer(buffer);
        }
    }

}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
