// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.CommandStateObserver;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A basic observer for toolbar elements.
    /// </summary>
    class ToolbarUpdateObserver : StateObserver
    {
        IToolbarElement m_ToolbarElement;

        ToolStateComponent m_ToolState;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolbarUpdateObserver"/> class.
        /// </summary>
        /// <param name="element">The toolbar element updated by this observer.</param>
        /// <param name="toolState">The state to observe.</param>
        public ToolbarUpdateObserver(IToolbarElement element, ToolStateComponent toolState)
            : base(toolState)
        {
            m_ToolbarElement = element;
            m_ToolState = toolState;
        }

        public override void Observe()
        {
            using (var observation = this.ObserveState(m_ToolState))
            {
                if (observation.UpdateType != UpdateType.None)
                {
                    m_ToolbarElement.Update();
                }
            }
        }
    }
}
