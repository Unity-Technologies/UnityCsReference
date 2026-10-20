// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Collections;

namespace Unity.Entities
{
    //@TODO: Use field offset / union here... There seems to be an issue in mono preventing it...
    unsafe struct EntityQueryFilter
    {
        public struct SharedComponentData
        {
            public const int Capacity = 2;

            public int Count;
            public fixed int IndexInEntityQuery[Capacity];
            public fixed int SharedComponentIndex[Capacity];
        }

        // Saves the index of ComponentTypes in this group that have changed.
        public struct ChangedFilter
        {
            public const int Capacity = 2;

            public int Count;
            public fixed int IndexInEntityQuery[Capacity];
        }

        public uint RequiredChangeVersion;

        public SharedComponentData Shared;
        public ChangedFilter Changed;

        public bool RequiresMatchesFilter
        {
            get { return Shared.Count != 0 || Changed.Count != 0 || _UseOrderFiltering != 0; }
        }

        private uint _UseOrderFiltering;
        public bool UseOrderFiltering
        {
            get { return _UseOrderFiltering != 0; }
            internal set { _UseOrderFiltering = value ? 1u : 0u; }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertValid()
        {
            if (Shared.Count < 0 || Shared.Count > SharedComponentData.Capacity)
                throw new ArgumentOutOfRangeException($"Shared.Count {Shared.Count} is out of range [0..{SharedComponentData.Capacity}]");
            if (Changed.Count < 0 || Changed.Count > ChangedFilter.Capacity)
                throw new ArgumentOutOfRangeException($"Changed.Count {Changed.Count} is out of range [0..{ChangedFilter.Capacity}]");
        }
    }
}
