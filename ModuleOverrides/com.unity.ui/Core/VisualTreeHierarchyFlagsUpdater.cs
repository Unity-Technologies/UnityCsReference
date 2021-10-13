// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Profiling;

namespace UnityEngine.UIElements
{
    internal class VisualTreeHierarchyFlagsUpdater : BaseVisualTreeUpdater
    {
        private uint m_Version = 0;
        private uint m_LastVersion = 0;

        private static readonly string s_Description = "Update Hierarchy Flags";
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker(s_Description);
        public override ProfilerMarker profilerMarker => s_ProfilerMarker;

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if ((versionChangeType & (VersionChangeType.Transform | VersionChangeType.Size | VersionChangeType.Overflow | VersionChangeType.Hierarchy | VersionChangeType.BorderWidth | VersionChangeType.EventCallbackCategories)) == 0)
                return;

            // According to the flags, what operations must be done?
            bool mustDirtyWorldTransform = (versionChangeType & VersionChangeType.Transform) != 0;
            bool mustDirtyWorldClip = (versionChangeType & (VersionChangeType.Transform | VersionChangeType.Size | VersionChangeType.Overflow | VersionChangeType.BorderWidth)) != 0;
            bool mustDirtyEventParentCategories = (versionChangeType & (VersionChangeType.Hierarchy | VersionChangeType.EventCallbackCategories)) != 0;

            VisualElementFlags mustDirtyFlags =
                (mustDirtyWorldTransform ? VisualElementFlags.WorldTransformDirty | VisualElementFlags.WorldBoundingBoxDirty : 0) |
                (mustDirtyWorldClip ? VisualElementFlags.WorldClipDirty : 0) |
                (mustDirtyEventParentCategories ? VisualElementFlags.EventCallbackParentCategoriesDirty : 0);

            var needDirtyFlags = mustDirtyFlags & ~ve.m_Flags;
            if (needDirtyFlags != 0)
            {
                DirtyHierarchy(ve, needDirtyFlags);
            }

            DirtyBoundingBoxHierarchy(ve);

            ++m_Version;
        }

        static void DirtyHierarchy(VisualElement ve, VisualElementFlags mustDirtyFlags)
        {
            // We use VisualElementFlags to track changes across the hierarchy since all those values come from m_Flags.
            ve.m_Flags |= mustDirtyFlags;

            int count = ve.hierarchy.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = ve.hierarchy[i];

                // Are these operations already done?
                var needDirtyFlags = mustDirtyFlags & ~child.m_Flags;
                if (needDirtyFlags != 0)
                {
                    DirtyHierarchy(child, needDirtyFlags);
                }
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
