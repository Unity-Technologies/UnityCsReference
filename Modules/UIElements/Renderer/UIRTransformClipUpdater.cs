// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    // Almost the same as the standard transform updater except that it dirty repaint when transforms change
    internal class UIRTransformClipUpdater : BaseVisualTreeUpdater
    {
        private uint m_Version = 0;
        private uint m_LastVersion = 0;

        public override string description
        {
            get { return "UIR Update Transform"; }
        }

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if ((versionChangeType & (VersionChangeType.Transform | VersionChangeType.Clip)) == 0)
                return;

            if ((versionChangeType & VersionChangeType.Transform) == VersionChangeType.Transform)
            {
                bool dirtyRepaint = true;
                if (UIRUtility.IsViewTransformWithoutNesting(ve) || UIRUtility.IsSkinnedTransformWithoutNesting(ve))
                {
                    // The element has the ViewTransform/SkinningTransform hint do not dirty repaint its children
                    dirtyRepaint = false;
                    ve.IncrementVersion(VersionChangeType.Repaint);
                }
                if (!ve.isWorldTransformDirty || !ve.isWorldClipDirty || dirtyRepaint)
                    DirtyTransformClipHierarchy(ve, dirtyRepaint); // Dirty both transform and clip when the transform changes
            }
            else if ((versionChangeType & VersionChangeType.Clip) == VersionChangeType.Clip && !ve.isWorldClipDirty)
                DirtyClipHierarchy(ve);

            ++m_Version;
        }

        private void DirtyTransformClipHierarchy(VisualElement ve, bool dirtyRepaint)
        {
            ve.isWorldTransformDirty = true;
            ve.isWorldClipDirty = true;

            if (dirtyRepaint)
                ve.IncrementVersion(VersionChangeType.Repaint);

            int count = ve.hierarchy.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = ve.hierarchy[i];
                DirtyTransformClipHierarchy(child, dirtyRepaint);
            }
        }

        private void DirtyClipHierarchy(VisualElement ve)
        {
            ve.isWorldClipDirty = true;
            ve.IncrementVersion(VersionChangeType.Repaint);

            int count = ve.hierarchy.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = ve.hierarchy[i];
                DirtyClipHierarchy(child);
            }
        }

        public override void Update()
        {
            if (m_Version == m_LastVersion)
                return;

            m_LastVersion = m_Version;

            panel.UpdateElementUnderMouse();
        }
    }
}
