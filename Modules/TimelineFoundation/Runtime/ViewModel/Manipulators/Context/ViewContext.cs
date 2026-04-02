// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.IntegerTime;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct ViewContext
    {
        public readonly DiscreteTime time;
        public readonly IReadOnlyList<Item> visibleItems;

        public ViewContext(DiscreteTime time, IReadOnlyList<Item> visibleItems)
        {
            this.time = time;
            this.visibleItems = visibleItems;
        }
    }
}
