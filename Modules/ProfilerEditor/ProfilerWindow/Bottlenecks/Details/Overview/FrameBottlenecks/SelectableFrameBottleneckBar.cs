// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    /// <summary>
    /// Wrapper element for frame bottleneck bars that implements selection behavior.
    /// </summary>
    class SelectableFrameBottleneckBar : VisualElement, ISelectedDetailsViewElement
    {
        VisualElement m_Bar;

        public SelectableFrameBottleneckBar(VisualElement bar)
        {
            m_Bar = bar;
            Add(bar);
            
            // Make the wrapper container transparent to layout
            style.flexGrow = 1;
            style.flexShrink = 0;
        }

        public void SetSelected(bool value)
        {
            m_Bar.SetCheckedPseudoState(value);
        }
    }
}
