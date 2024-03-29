// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Rendering
{
    [RequireComponent(typeof(Transform))]
    [NativeType(Header = "Runtime/2D/Sorting/SortingGroup.h")]
    public sealed partial class SortingGroup : Behaviour
    {
        [StaticAccessor("SortingGroup", StaticAccessorType.DoubleColon)]
        internal extern static int invalidSortingGroupID { get; }

        [StaticAccessor("SortingGroup", StaticAccessorType.DoubleColon)]
        public extern static void UpdateAllSortingGroups();

        [StaticAccessor("SortingGroup", StaticAccessorType.DoubleColon)]
        internal extern static SortingGroup GetSortingGroupByIndex(int index);

        public extern string sortingLayerName { get; set; }
        public extern int sortingLayerID { get; set; }
        public extern int sortingOrder { get; set; }
        public extern bool sortAtRoot { get; set; }
        internal extern int sortingGroupID { get; }
        internal extern int sortingGroupOrder { get; }
        internal extern int index { get; }
        internal extern uint sortingKey { get; }
    }
}
