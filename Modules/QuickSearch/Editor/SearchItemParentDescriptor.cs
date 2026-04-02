// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor.Search
{
    [VisibleToOtherModules]
    enum SearchItemParentType
    {
        TokenSeparatedId,
        SearchItemId
    }

    [VisibleToOtherModules]
    readonly record struct SearchItemParentDescriptor(string Id, SearchItemParentType Type)
    {
        public string Id { get; } = Id;
        public SearchItemParentType Type { get; } = Type;

        public static readonly char[] DefaultSeparatorTokens = ['/', '\\'];
    }
}
