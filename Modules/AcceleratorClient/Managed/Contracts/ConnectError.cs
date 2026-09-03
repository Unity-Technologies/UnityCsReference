// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable
using System;
namespace Unity.AcceleratorClient.Contracts;

internal abstract record ConnectError
{
    internal sealed record Unreachable(string Endpoint, string Message) : ConnectError;

    internal sealed record ProbeFailed(string Endpoint, string Message) : ConnectError;

    internal sealed record NotAuthorized(string Endpoint, string Message) : ConnectError;
}

internal static class ConnectErrorVariantNames
{
    private const string Unreachable    = "Unreachable";
    private const string ProbeFailed    = "ProbeFailed";
    private const string NotAuthorized  = "NotAuthorized";
    private const string Unknown        = "Unknown";

    public static string For(ConnectError error) => error switch
    {
        ConnectError.Unreachable    => Unreachable,
        ConnectError.ProbeFailed    => ProbeFailed,
        ConnectError.NotAuthorized  => NotAuthorized,
        _                           => Unknown,
    };
}
