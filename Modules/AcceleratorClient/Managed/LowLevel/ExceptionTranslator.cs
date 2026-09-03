// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable
using System;
namespace Unity.AcceleratorClient.LowLevel;

using Unity.AcceleratorClient.Contracts;

internal static class ExceptionTranslator
{
    // Guards against self-referential InnerException chains; 32 is well above any realistic depth.
    private const int k_MaxInnerExceptionDepth = 32;

    // Order matters: auth check fires first, then unreachability, then network-fault.
    private static readonly string[] s_UnreachableSubstrings =
    {
        "connection refused",
        "unable to connect",
        "no such host",
        "name or service not known",
        "network is unreachable",
        "no route to host",
    };

    private static readonly string[] s_NetworkFaultSubstrings =
    {
        "timeout",
        "timed out",
        "connection reset",
        "broken pipe",
    };

    private static readonly string[] s_AuthSubstrings =
    {
        "unauthorized",
        "forbidden",
        "authentication",
        "permission denied",
    };

    internal static ConnectError TranslateConnect(Exception ex, string endpoint) =>
        TranslateConnectWithType(ex, endpoint).Error;

    internal static (ConnectError Error, string UnderlyingExceptionType) TranslateConnectWithType(
        Exception ex, string endpoint)
    {
        var deepest = Unwrap(ex);
        var message = AggregateMessages(ex);
        var typeName = deepest.GetType().FullName ?? deepest.GetType().Name;

        if (ContainsAny(message, s_AuthSubstrings))
            return (new ConnectError.NotAuthorized(endpoint, $"[{typeName}] {message}"), typeName);

        // Both arrays map to Unreachable on the connect path; they remain distinct
        // because the cache-side translator maps them to different variants.
        if (ContainsAny(message, s_UnreachableSubstrings) ||
            ContainsAny(message, s_NetworkFaultSubstrings))
            return (new ConnectError.Unreachable(endpoint, $"[{typeName}] {message}"), typeName);

        // Unclassified failures default to ProbeFailed; rare unrecognised pre-probe messages
        // also mis-tag as ProbeFailed — acceptable.
        return (new ConnectError.ProbeFailed(endpoint, $"[{typeName}] {message}"), typeName);
    }

    // Ordered after auth/network but before the catch-all ServerError.
    internal const string k_AssertTrueServerFaultSubstring =
        "AssertTrue has found a negative condition";

    // Anchored to V2's "GENERIC_EXCEPTION - The requested data exceeds the 4 MB limit (N bytes)".
    // Ordered after auth/network and AssertTrue but before the catch-all.
    internal const string k_SizeHintMismatchSubstring = "exceeds the 4 MB limit";

    internal const string k_SizeHintMismatchMarker = CacheServerErrorMarkers.SizeHintMismatch;

    internal static CacheError TranslateCache(Exception ex, CacheNamespace ns)
    {
        var deepest = Unwrap(ex);
        var message = AggregateMessages(ex);
        var typeName = deepest.GetType().FullName ?? deepest.GetType().Name;

        if (ContainsAny(message, s_AuthSubstrings))
            return new CacheError.NotAuthorized(ns, $"[{typeName}] {message}");

        if (ContainsAny(message, s_UnreachableSubstrings) ||
            ContainsAny(message, s_NetworkFaultSubstrings))
            return new CacheError.ConnectionLost(ns, $"[{typeName}] {message}");

        if (message.Contains(k_AssertTrueServerFaultSubstring, StringComparison.Ordinal))
        {
            return new CacheError.ServerError(
                ns,
                "server-side internal assertion failure (likely truncated/corrupt value state on read). " +
                "Re-uploading the value with a fresh key may help; " +
                "reading the same key will continue to fail — the V2 server does not GC orphaned value-id state " +
                "and exposes no client-callable cleanup API today, so corrupt state persists permanently " +
                "(the V2 server does not expose a client-callable cleanup API today).",
                UnderlyingExceptionType: typeName);
        }

        if (message.Contains(k_SizeHintMismatchSubstring, StringComparison.Ordinal))
        {
            return new CacheError.ServerError(
                ns,
                "V2 server rejected single-call read because the value exceeds the 4 MiB DataPacket limit. " +
                "This indicates a size-hint mismatch: the caller's expectedSize was ≤ 4 MiB, but the server " +
                "has a value larger than that under the requested key. Caller should retry with the no-hint " +
                "overload (GetAsync(ns, key, ct)) or refresh the manifest that supplied the wrong size.",
                UnderlyingExceptionType: k_SizeHintMismatchMarker);
        }

        return new CacheError.ServerError(ns, message, typeName);
    }

    private static Exception Unwrap(Exception ex)
    {
        var current = ex;
        for (var depth = 0; depth < k_MaxInnerExceptionDepth; depth++)
        {
            if (current.InnerException is null)
                return current;
            current = current.InnerException;
        }
        return current;
    }

    private static string AggregateMessages(Exception ex)
    {
        if (ex.InnerException is null)
            return ex.Message ?? string.Empty;

        var parts = new System.Text.StringBuilder(capacity: 256);
        var current = ex;
        for (var depth = 0; depth < k_MaxInnerExceptionDepth && current is not null; depth++)
        {
            if (parts.Length > 0)
                parts.Append(" -> ");
            parts.Append(current.Message ?? string.Empty);
            current = current.InnerException;
        }
        return parts.ToString();
    }

    private static bool ContainsAny(string haystack, string[] needles)
    {
        foreach (var needle in needles)
        {
            if (haystack.Contains(needle, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
