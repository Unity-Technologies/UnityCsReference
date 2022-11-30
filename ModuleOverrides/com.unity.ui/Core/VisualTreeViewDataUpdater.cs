// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Profiling;

namespace UnityEngine.UIElements
{
    internal class VisualTreeViewDataUpdater : BaseVisualTreeUpdater
    {
        private HashSet<VisualElement> m_UpdateList = new HashSet<VisualElement>();
        private HashSet<VisualElement> m_ParentList = new HashSet<VisualElement>();

        private const int kMaxValidatePersistentDataCount = 5;
        private uint m_Version = 0;
        private uint m_LastVersion = 0;

        private static readonly string s_Description = "Update ViewData";
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker(s_Description);
        public override ProfilerMarker profilerMarker => s_ProfilerMarker;

        // Only Editor panels actually do something with the ViewData of VisualElements
        // Skip the execution of the updater completely for Runtime UI (including when developing in the Editor)
        public bool enabled => panel.contextType == ContextType.Editor;

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if (!enabled)
                return;

            if ((versionChangeType & VersionChangeType.ViewData) != VersionChangeType.ViewData)
                return;

            ++m_Version;

            m_UpdateList.Add(ve);
            PropagateToParents(ve);
        }

        public override void Update()
        {
            if (!enabled)
                return;

            if (m_Version == m_LastVersion)
                return;

            int validatePersistentDataCount = 0;
            while (m_LastVersion != m_Version)
            {
                m_LastVersion = m_Version;

                ValidateViewDataOnSubTree(visualTree, true);
                validatePersistentDataCount++;

                if (validatePersistentDataCount > kMaxValidatePersistentDataCount)
                {
                    Debug.LogError("UIElements: Too many children recursively added that rely on persistent view data: " + visualTree);
                    break;
                }
            }

            m_UpdateList.Clear();
            m_ParentList.Clear();
        }

        private void ValidateViewDataOnSubTree(VisualElement ve, bool enablePersistence)
        {
            // Persistence of view data is almost always enabled as long as an element has
            // a valid viewDataKey. The only exception is when an element is in its parent's
            // shadow tree, that is, not a physical child of its logical parent's contentContainer.
            // In this exception case, persistence is disabled on the element even if the element
            // does have a viewDataKey, if its logical parent does not have a viewDataKey.
            enablePersistence = ve.IsViewDataPersitenceSupportedOnChildren(enablePersistence);

            if (m_UpdateList.Contains(ve))
            {
                m_UpdateList.Remove(ve);
                ve.OnViewDataReady(enablePersistence);
            }

            if (m_ParentList.Contains(ve))
            {
                m_ParentList.Remove(ve);
                var childCount = ve.hierarchy.childCount;
                for (int i = 0; i < childCount; ++i)
                {
                    ValidateViewDataOnSubTree(ve.hierarchy[i], enablePersistence);
                }
            }
        }

        private void PropagateToParents(VisualElement ve)
        {
            var parent = ve.hierarchy.parent;
            while (parent != null)
            {
                if (!m_ParentList.Add(parent))
                {
                    break;
                }

                parent = parent.hierarchy.parent;
            }
        }
    }
}
