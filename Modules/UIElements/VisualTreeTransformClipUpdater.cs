// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.UIElements
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
            if ((versionChangeType & (VersionChangeType.Transform | VersionChangeType.Clip)) == 0)
                return;

            if ((versionChangeType & VersionChangeType.Transform) == VersionChangeType.Transform && (!ve.isWorldTransformDirty || !ve.isWorldClipDirty))
                DirtyTransformClipHierarchy(ve); // Dirty both transform and clip when the transform changes
            else if ((versionChangeType & VersionChangeType.Clip) == VersionChangeType.Clip && !ve.isWorldClipDirty)
                DirtyClipHierarchy(ve);

            ++m_Version;
        }

        private void DirtyTransformClipHierarchy(VisualElement ve)
        {
            ve.isWorldTransformDirty = true;
            ve.isWorldClipDirty = true;

            int count = ve.shadow.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = ve.shadow[i];
                if (child.isWorldTransformDirty && child.isWorldClipDirty)
                    continue;

                DirtyTransformClipHierarchy(child);
            }
        }

        private void DirtyClipHierarchy(VisualElement ve)
        {
            ve.isWorldClipDirty = true;

            int count = ve.shadow.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = ve.shadow[i];
                if (child.isWorldClipDirty)
                    continue;

                DirtyClipHierarchy(child);
            }
        }

        public override void Update()
        {
            if (m_Version == m_LastVersion)
                return;

            m_LastVersion = m_Version;

            panel.dispatcher.UpdateElementUnderMouse(panel);
        }
    }
}
