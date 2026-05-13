// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.GraphToolkit.CSO;
using System;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal class ToolbarGraphLoadedObserver : StateObserver
    {
        ToolStateComponent m_ToolStateComponent;
        readonly Action m_OnGraphLoaded;

        public ToolbarGraphLoadedObserver(ToolStateComponent toolStateComponent, Action onGraphLoaded)
            : base(new IStateComponent[] { toolStateComponent },
                   Array.Empty<IStateComponent>())
        {
            m_ToolStateComponent = toolStateComponent;
            m_OnGraphLoaded = onGraphLoaded;
        }

        public override void Observe()
        {
            using var obs = this.ObserveState(m_ToolStateComponent);
            if (obs.UpdateType != UpdateType.None)
                m_OnGraphLoaded?.Invoke();
        }
    }
}
