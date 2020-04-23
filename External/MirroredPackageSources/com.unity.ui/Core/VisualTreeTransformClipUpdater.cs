using System;
using System.Diagnostics;
using Unity.Profiling;

namespace UnityEngine.UIElements
{
    internal class VisualTreeTransformClipUpdater : BaseVisualTreeUpdater
    {
        private uint m_Version = 0;
        private uint m_LastVersion = 0;

        private static readonly string s_Description = "Update Transform";
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker(s_Description);
        public override ProfilerMarker profilerMarker => s_ProfilerMarker;

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if ((versionChangeType & (VersionChangeType.Transform | VersionChangeType.Size | VersionChangeType.Overflow | VersionChangeType.Hierarchy | VersionChangeType.BorderWidth)) == 0)
                return;

            // According to the flags, what operations must be done?
            bool mustDirtyWorldTransform = (versionChangeType & VersionChangeType.Transform) != 0;
            bool mustDirtyWorldClip = (versionChangeType & (VersionChangeType.Transform | VersionChangeType.Size | VersionChangeType.Overflow | VersionChangeType.BorderWidth)) != 0;

            // Are these operations already done?
            mustDirtyWorldTransform = mustDirtyWorldTransform && !ve.isWorldTransformDirty;
            mustDirtyWorldClip = mustDirtyWorldClip && !ve.isWorldClipDirty;

            if (mustDirtyWorldTransform || mustDirtyWorldClip)
                DirtyHierarchy(ve, mustDirtyWorldTransform, mustDirtyWorldClip);

            DirtyBoundingBoxHierarchy(ve);

            ++m_Version;
        }

        static void DirtyHierarchy(VisualElement ve, bool mustDirtyWorldTransform, bool mustDirtyWorldClip)
        {
            if (mustDirtyWorldTransform)
            {
                ve.isWorldTransformDirty = true;
                ve.isWorldBoundingBoxDirty = true;
            }

            if (mustDirtyWorldClip)
                ve.isWorldClipDirty = true;

            int count = ve.hierarchy.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = ve.hierarchy[i];

                if (mustDirtyWorldTransform && !child.isWorldTransformDirty ||
                    mustDirtyWorldClip && !child.isWorldClipDirty)
                    DirtyHierarchy(child, mustDirtyWorldTransform, mustDirtyWorldClip);
            }
        }

        static void DirtyBoundingBoxHierarchy(VisualElement ve)
        {
            ve.isBoundingBoxDirty = true;
            ve.isWorldBoundingBoxDirty = true;
            var parent = ve.hierarchy.parent;
            while (parent != null && !parent.isBoundingBoxDirty)
            {
                parent.isBoundingBoxDirty = true;
                parent.isWorldBoundingBoxDirty = true;
                parent = parent.hierarchy.parent;
            }
        }

        public override void Update()
        {
            if (m_Version == m_LastVersion)
                return;

            m_LastVersion = m_Version;

            panel.UpdateElementUnderPointers();
            panel.visualTree.UpdateBoundingBox();
        }
    }
}
