// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;

namespace UnityEngine.UIElements
{
    internal class VisualTreeTransformClipUpdater : BaseVisualTreeUpdater
    {
        private uint m_Version = 0;
        private uint m_LastVersion = 0;

        public override string description
        {
            get { return "Update Transform"; }
        }

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if ((versionChangeType & (VersionChangeType.Transform | VersionChangeType.Size)) == 0)
                return;

            // According to the flags, what operations must be done?
            bool mustDirtyWorldTransform = (versionChangeType & VersionChangeType.Transform) != 0;
            bool mustDirtyWorldClip = (versionChangeType & (VersionChangeType.Transform | VersionChangeType.Size)) != 0;

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
                ve.isWorldTransformDirty = true;

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
            var parent = ve.hierarchy.parent;
            while (parent != null && !parent.isBoundingBoxDirty)
            {
                parent.isBoundingBoxDirty = true;
                parent = parent.hierarchy.parent;
            }
        }

        public override void Update()
        {
            if (m_Version == m_LastVersion)
                return;

            m_LastVersion = m_Version;

            panel.UpdateElementUnderMouse();
            panel.visualTree.UpdateBoundingBox();
        }
    }
}
