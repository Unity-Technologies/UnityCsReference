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
        public extern string sortingLayerName { get; set; }
        public extern int sortingLayerID { get; set; }
        public extern int sortingOrder { get; set; }
        internal extern int sortingGroupID { get; }
        internal extern int sortingGroupOrder { get; }
        internal extern int index { get; }
    }
}
