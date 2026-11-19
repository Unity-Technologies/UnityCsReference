// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [Flags]
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal enum ItemTypeFlags
    {
        None = 0,
        Clip = 1 << 0,
        Gap = 1 << 1,
        Transition = 1 << 2,
        Marker = 1 << 3,
        All = Clip | Gap | Transition | Marker,
        Interval = Clip | Gap | Transition
    }
}
