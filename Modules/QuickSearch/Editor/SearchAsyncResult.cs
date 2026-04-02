// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Search
{
    enum SearchAsyncResolutionState
    {
        Unresolved = 0,
        Resolving,
        Failed,
        Resolved
    }

    readonly record struct SearchAsyncResult<T>(T Value, SearchAsyncResolutionState State)
    {
        public T Value { get; } = Value;
        public SearchAsyncResolutionState State { get; } = State;

        public static SearchAsyncResult<T> Unresolved { get; } = new(default, SearchAsyncResolutionState.Unresolved);
    }
}
