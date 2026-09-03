// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace Unity.Localization;

/// <summary>
/// Returns an already-completed Awaitable for the synchronous resolution paths (string formatting, cache hits).
/// </summary>
static class AwaitableUtility
{
    public static Awaitable<T> FromResult<T>(T value)
    {
        var source = new AwaitableCompletionSource<T>();
        source.SetResult(value);
        return source.Awaitable;
    }

    public static Awaitable Completed()
    {
        var source = new AwaitableCompletionSource();
        source.SetResult();
        return source.Awaitable;
    }
}
