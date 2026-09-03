// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Profiling;

namespace UnityEngine.UIElements
{
    // Runs each element's [OnComponentChanged] handlers once per frame. The generated SetXxx setters flag
    // an element with VersionChangeType.Component (through VisualElement.MarkComponentDirty); this updater
    // collects the flagged elements and flushes them in the Component phase — after data-binding writes and
    // before the style/layout/repaint passes a handler may trigger, so the invalidation lands the same frame.
    internal class VisualTreeComponentUpdater : BaseVisualTreeUpdater
    {
        static readonly string s_Description = "UIElements.UpdateComponents";
        static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker(ProfilerCategory.UIToolkit, s_Description);
        public override ProfilerMarker profilerMarker => s_ProfilerMarker;

        HashSet<VisualElement> m_DirtyElements = new HashSet<VisualElement>();
        HashSet<VisualElement> m_Processing = new HashSet<VisualElement>();

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if ((versionChangeType & VersionChangeType.Component) == VersionChangeType.Component)
                m_DirtyElements.Add(ve);
        }

        public override void Update()
        {
            if (m_DirtyElements.Count == 0)
                return;

            // Swap before iterating
            (m_Processing, m_DirtyElements) = (m_DirtyElements, m_Processing);

            foreach (var ve in m_Processing)
            {
                if (ve.elementPanel == panel)
                    ve.FlushComponentChanges();
            }

            m_Processing.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            m_DirtyElements.Clear();
            m_Processing.Clear();
        }
    }
}
