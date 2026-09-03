// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable
using System;
namespace Unity.AcceleratorClient.Contracts;

internal enum ContentHashVerificationDirection
{
    Upload,
    Download,
}

internal abstract record CacheError
{
    internal sealed record NotFound(CacheNamespace Namespace, UInt128 Key) : CacheError;

    internal sealed record ServerError(
        CacheNamespace Namespace,
        string Message,
        string? UnderlyingExceptionType) : CacheError;

    // Retryable after reconnecting.
    internal sealed record ConnectionLost(CacheNamespace Namespace, string Message) : CacheError;

    // Reserved; not currently produced by any code path.
    internal sealed record Timeout(CacheNamespace Namespace, TimeSpan Elapsed, string Message) : CacheError;

    internal sealed record Cancelled(CacheNamespace Namespace) : CacheError;

    internal sealed record InvalidArgument(string ParameterName, string Message) : CacheError;

    internal sealed record NotConnected(CacheNamespace Namespace) : CacheError;

    internal sealed record NotAuthorized(CacheNamespace Namespace, string Message) : CacheError;

    internal sealed record ContentHashMismatch(
        CacheNamespace Namespace,
        UInt128 RequestedKey,
        UInt128 ComputedKey,
        int BytesLength,
        ContentHashVerificationDirection Direction) : CacheError;
}

internal static class CacheErrorVariantNames
{
    private const string NotFound              = "NotFound";
    private const string ServerError           = "ServerError";
    private const string ConnectionLost        = "ConnectionLost";
    private const string Timeout               = "Timeout";
    private const string Cancelled             = "Cancelled";
    private const string InvalidArgument       = "InvalidArgument";
    private const string NotConnected          = "NotConnected";
    private const string NotAuthorized         = "NotAuthorized";
    private const string ContentHashMismatch   = "ContentHashMismatch";
    private const string Unknown               = "Unknown";

    public static string For(CacheError error) => error switch
    {
        CacheError.NotFound              => NotFound,
        CacheError.ServerError           => ServerError,
        CacheError.ConnectionLost        => ConnectionLost,
        CacheError.Timeout               => Timeout,
        CacheError.Cancelled             => Cancelled,
        CacheError.InvalidArgument       => InvalidArgument,
        CacheError.NotConnected          => NotConnected,
        CacheError.NotAuthorized         => NotAuthorized,
        CacheError.ContentHashMismatch   => ContentHashMismatch,
        _                                => Unknown,
    };
}

internal static class CacheServerErrorMarkers
{
    // Fires when a size-hinted Get undershoots the real value size; the client does NOT auto-retry.
    public const string SizeHintMismatch = "SizeHintMismatch";
}
