// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal enum ManipulationState
    {
        None,
        Move,
        Trim
    }

    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct ManipulationData : IReadOnlyData
    {
        public readonly ManipulationState currentManipulation;

        public ManipulationData(ManipulationState currentManipulation)
        {
            this.currentManipulation = currentManipulation;
        }
    }
}
