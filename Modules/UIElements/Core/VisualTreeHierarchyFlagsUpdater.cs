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

        // The name of this updater is kind of confusing, but overall what is does in its Update()
        // can summarized as updating the actual bounds of elements and everything that depends on that
        private static readonly string s_Description = "UIElements.UpdateElementBounds";
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker(s_Description);
        public override ProfilerMarker profilerMarker => s_ProfilerMarker;

        // According to the flags, what operations must be done?
        private const VersionChangeType WorldTransformChanged = VersionChangeType.Transform;
        private const VersionChangeType WorldClipChanged = VersionChangeType.Transform | VersionChangeType.Size | VersionChangeType.Overflow | VersionChangeType.BorderWidth;
        private const VersionChangeType EventParentCategoriesChanged = VersionChangeType.Hierarchy | VersionChangeType.EventCallbackCategories;
        private const VersionChangeType BoundingBoxChanged = VersionChangeType.Transform | VersionChangeType.Size | VersionChangeType.Overflow | VersionChangeType.Hierarchy;

        protected const VersionChangeType ChildrenChanged = WorldTransformChanged | WorldClipChanged | EventParentCategoriesChanged;
        protected const VersionChangeType ParentChanged = BoundingBoxChanged;
        protected const VersionChangeType VersionChanged = WorldTransformChanged | WorldClipChanged | VersionChangeType.Hierarchy | VersionChangeType.Picking;

        protected const VersionChangeType AnythingChanged = ChildrenChanged | ParentChanged | VersionChanged;

        protected const VisualElementFlags BoundingBoxDirtyFlags =
            VisualElementFlags.BoundingBoxDirty | VisualElementFlags.WorldBoundingBoxDirty  | VisualElementFlags.BoundingBoxDirtiedSinceLastLayoutPass;

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if ((versionChangeType & (VersionChangeType.Transform | VersionChangeType.Size | VersionChangeType.Overflow | VersionChangeType.Hierarchy | VersionChangeType.BorderWidth | VersionChangeType.EventCallbackCategories | VersionChangeType.Picking)) == 0)
                return;

            // According to the flags, what operations must be done?
            bool mustDirtyWorldTransform = (versionChangeType & VersionChangeType.Transform) != 0;
            bool mustDirtyWorldClip = (versionChangeType & (VersionChangeType.Transform | VersionChangeType.Size | VersionChangeType.Overflow | VersionChangeType.BorderWidth)) != 0;
            bool mustDirtyEventParentCategories = (versionChangeType & (VersionChangeType.Hierarchy | VersionChangeType.EventCallbackCategories)) != 0;

            VisualElementFlags mustDirtyFlags =
                (mustDirtyWorldTransform ? VisualElementFlags.WorldTransformDirty | VisualElementFlags.WorldBoundingBoxDirty : 0) |
                (mustDirtyWorldClip ? VisualElementFlags.WorldClipDirty : 0) |
                (mustDirtyEventParentCategories ? VisualElementFlags.EventInterestParentCategoriesDirty : 0);

            var needDirtyFlags = mustDirtyFlags & ~ve.m_Flags;
            if (needDirtyFlags != 0)
            {
                DirtyHierarchy(ve, needDirtyFlags);
            }

            DirtyBoundingBoxHierarchy(ve);

            ++m_Version;
        }

        protected static VisualElementFlags GetChildrenMustDirtyFlags(VisualElement ve, VersionChangeType versionChangeType)
        {
            VisualElementFlags mustDirty = 0;

            if ((versionChangeType & WorldTransformChanged) != 0)
                mustDirty |= VisualElementFlags.WorldTransformDirty | VisualElementFlags.WorldBoundingBoxDirty;
            if ((versionChangeType & WorldClipChanged) != 0)
                mustDirty |= VisualElementFlags.WorldClipDirty;
            if ((versionChangeType & EventParentCategoriesChanged) != 0)
                mustDirty |= VisualElementFlags.EventInterestParentCategoriesDirty;

            return mustDirty;
        }

        protected static void DirtyHierarchy(VisualElement ve, VisualElementFlags mustDirtyFlags)
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

        private static void DirtyBoundingBoxHierarchy(VisualElement ve)
        {
            // Even if all the local bounding box flags are dirty already, we need to check the first parent too.
            // This is because other factors can impact the parent boundingBox besides our own boundingBox changing
            // (for instance, if our ShouldClip() method or resolvedStyle.display return a different value).
            ve.m_Flags |= BoundingBoxDirtyFlags;
            DirtyParentHierarchy(ve.hierarchy.parent, BoundingBoxDirtyFlags);
        }

        protected static void DirtyParentHierarchy(VisualElement ve, VisualElementFlags flags)
        {
            while (ve != null && (ve.m_Flags & flags) != flags)
            {
                ve.m_Flags |= flags;
                ve = ve.hierarchy.parent;
            }
        }

        public override void Update()
        {
            if (m_Version == m_LastVersion)
                return;

            m_LastVersion = m_Version;

            panel.visualTree.UpdateBoundingBox();

            bool hasChanged = panel.UpdateElementUnderPointers();

            // Runtime panels are re-applying styles during the rendering playerloop callback
            // Editor panels must re-evaluate styles to properly render hover states on the first frame a change occured
            if (hasChanged && panel.contextType == ContextType.Editor)
            {
                panel.ApplyStyles();
            }
        }
    }

    /// <summary>
    /// We try to reduce the impact on the non-WorldSpace code path as much as possible by having no virtual calls
    /// in the VisualTreeHierarchyFlagsUpdater base class.
    /// Instead, we copy the logic in this derived class and we modify the flags and the operations to add the
    /// Picking3D Bounds logic only where it's needed.
    /// </summary>
    internal class VisualTreeWorldSpaceHierarchyFlagsUpdater : VisualTreeHierarchyFlagsUpdater
    {
        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            base.OnVersionChanged(ve, versionChangeType);

            if ((versionChangeType & AnythingChanged) == 0)
                return;

            if ((versionChangeType & ChildrenChanged) != 0)
            {
                DirtyHierarchy(ve, GetChildrenMustDirtyFlags(ve, versionChangeType));
            }

            if ((versionChangeType & ParentChanged) != 0)
            {
                DirtyParentHierarchy(ve, GetParentMustDirtyFlags(ve));
            }
        }

        private new const VisualElementFlags BoundingBoxDirtyFlags =
            VisualTreeHierarchyFlagsUpdater.BoundingBoxDirtyFlags | VisualElementFlags.LocalBounds3DDirty;

        VisualElementFlags GetParentMustDirtyFlags(VisualElement ve)
        {
            var mustDirty = BoundingBoxDirtyFlags;

            if (ve.has3DTransform)
                mustDirty |= VisualElementFlags.Needs3DBounds;

            return mustDirty;
        }

        public override void Update()
        {
            // Nothing to update. Don't call UpdateElementUnderPointers for world-space panels.
        }
    }
}
