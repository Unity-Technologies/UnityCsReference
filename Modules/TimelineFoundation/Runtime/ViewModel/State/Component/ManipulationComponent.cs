// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal class ManipulationComponent : Component<ManipulationData>
    {
        ManipulationState m_ManipulationState = ManipulationState.None;
        protected override ManipulationData GenerateReadOnlyData()
        {
            return new ManipulationData(m_ManipulationState);
        }

        public void SetManipulationState(ManipulationState manipulationState)
        {
            if (m_ManipulationState == manipulationState)
                return;

            m_ManipulationState = manipulationState;
            MarkAsDirty();
        }
    }
}
