// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Search
{
    [Flags]
    public enum IndexingOptions : byte
    {
        None         = 0,
        Types        = 1 << 0,      // Index type information about objects
        Properties   = 1 << 1,      // Index serialized properties of objects
        Extended     = 1 << 2,      // Index all sub-assets and objects as new documents
        Dependencies = 1 << 3,      // Index object dependencies (i.e. ref:<name>)

        Keep         = 1 << 6,      // Indicate that the index should not get deleted after resolution.

        Temporary    = 1 << 7,      // Indicate that the index should be created under Temp/...

        All = Types | Properties | Extended | Dependencies
    }

    static class IndexingOptionsExtensions
    {
        public static bool HasAny(this IndexingOptions flags, IndexingOptions f) => (flags & f) != 0;
        public static bool HasAll(this IndexingOptions flags, IndexingOptions all) => (flags & all) == all;
        public static bool HasNone(this IndexingOptions flags, IndexingOptions f) => (flags & f) == 0;
    }
}
