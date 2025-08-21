// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor.Search
{
    [NativeHeader("Modules/QuickSearch/SearchIndexOperator.h")]
    internal enum SearchIndexOperator
    {
        Contains,
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,

        DoNotCompareScore,

        None
    }
}
