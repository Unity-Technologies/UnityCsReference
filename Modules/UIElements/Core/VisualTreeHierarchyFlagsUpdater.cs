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
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker(ProfilerCategory.UIToolkit, s_Description);
        public override ProfilerMarker profilerMarker => s_ProfilerMarker;

        // According to the flags, what operations must be done?
        private const VersionChangeType WorldTransformChanged = VersionChangeType.Transform;
        private const VersionChangeType WorldClipChanged = VersionChangeType.Transform | VersionChangeType.Size | VersionChangeType.Overflow | VersionChangeType.BorderWidth;
        private const VersionChangeType EventParentCategoriesChanged = VersionChangeType.Hierarchy | VersionChangeType.EventCallbackCategories;

        protected const VersionChangeType BoundingBoxChanged = VersionChangeType.Transform | VersionChangeType.Size | VersionChangeType.Overflow | VersionChangeType.Hierarchy;
        protected const VersionChangeType ChildrenChanged = WorldTransformChanged | WorldClipChanged | EventParentCategoriesChanged;
        protected const VersionChangeType VersionChanged = WorldTransformChanged | WorldClipChanged | VersionChangeType.Hierarchy | VersionChangeType.Picking;

        protected const VersionChangeType AnythingChanged = ChildrenChanged | BoundingBoxChanged | VersionChanged;

        protected const VisualElementTransformFlags BoundingBoxDirtyFlags =
            VisualElementTransformFlags.BoundingBoxDirty | VisualElementTransformFlags.WorldBoundingBoxDirty | VisualElementTransformFlags.BoundingBoxDirtiedSinceLastLayoutPass;

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if ((versionChangeType & AnythingChanged) == 0)
                return;

            if ((versionChangeType & ChildrenChanged) != 0)
            {
                DirtyChildrenHierarchy(ve, GetChildrenMustDirtyFlags(ve, versionChangeType));
            }

            if ((versionChangeType & BoundingBoxChanged) != 0)
            {
                DirtyBoundingBoxHierarchy(ve);
            }

            if ((versionChangeType & VersionChanged) != 0)
            {
                ++m_Version;
            }
        }

        protected static (VisualElementFlags, VisualElementTransformFlags) GetChildrenMustDirtyFlags(VisualElement ve, VersionChangeType versionChangeType)
        {
            VisualElementFlags mustDirty = 0;
            VisualElementTransformFlags mustDirtyTransform = 0;

            if ((versionChangeType & WorldTransformChanged) != 0)
                mustDirtyTransform |= VisualElementTransformFlags.WorldTransformDirty | VisualElementTransformFlags.WorldBoundingBoxDirty;
            if ((versionChangeType & WorldClipChanged) != 0)
                mustDirty |= VisualElementFlags.WorldClipDirty;
            if ((versionChangeType & EventParentCategoriesChanged) != 0)
                mustDirty |= VisualElementFlags.EventInterestParentCategoriesDirty;

            return (mustDirty, mustDirtyTransform);
        }

        protected static void DirtyChildrenHierarchy(VisualElement ve, (VisualElementFlags flags, VisualElementTransformFlags transformFlags) mustDirty)
        {
            // Are these operations already done?
            var needDirtyFlags = mustDirty.flags & ~ve.flags;
            var needDirtyTransformFlags = mustDirty.transformFlags & ~ve.transformFlags;
            if (needDirtyFlags == 0 && needDirtyTransformFlags == 0)
                return;

            // We use VisualElementFlags to track changes across the hierarchy since all those values come from flags.
            ve.flags |= needDirtyFlags;
            ve.transformFlags |= needDirtyTransformFlags;

            int count = ve.hierarchy.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = ve.hierarchy[i];
                DirtyChildrenHierarchy(child, (needDirtyFlags, needDirtyTransformFlags));
            }
        }

        private static void DirtyBoundingBoxHierarchy(VisualElement ve)
        {
            // Even if all the local bounding box flags are dirty already, we need to check the first parent too.
            // This is because other factors can impact the parent boundingBox besides our own boundingBox changing
            // (for instance, if our ShouldClip() method or resolvedStyle.display return a different value).
            ve.transformFlags |= BoundingBoxDirtyFlags;
            DirtyParentHierarchy(ve.hierarchy.parent, BoundingBoxDirtyFlags);
        }

        private static void DirtyParentHierarchy(VisualElement ve, VisualElementTransformFlags flags)
        {
            while (ve != null && (ve.transformFlags & flags) != flags)
            {
                ve.transformFlags |= flags;
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
            if ((versionChangeType & AnythingChanged) == 0)
                return;

            if ((versionChangeType & ChildrenChanged) != 0)
            {
                DirtyChildrenHierarchy(ve, GetChildrenMustDirtyFlags(ve, versionChangeType));
            }

            if ((versionChangeType & BoundingBoxChanged) != 0)
            {
                DirtyBoundingBoxHierarchy(ve);
            }
        }

        private new const VisualElementTransformFlags BoundingBoxDirtyFlags =
            VisualTreeHierarchyFlagsUpdater.BoundingBoxDirtyFlags |
            VisualElementTransformFlags.BoundingBoxWithoutNestedDirty |
            VisualElementTransformFlags.LocalBounds3DDirty |
            VisualElementTransformFlags.LocalBoundsWithoutNested3DDirty;

        private static VisualElementTransformFlags GetParentMustDirtyFlags(VisualElement ve)
        {
            var mustDirty = BoundingBoxDirtyFlags;

            if (ve.has3DTransform)
                mustDirty |= VisualElementTransformFlags.Needs3DBounds;

            return mustDirty;
        }

        private static void DirtyBoundingBoxHierarchy(VisualElement ve)
        {
            var flags = GetParentMustDirtyFlags(ve);
            ve.transformFlags |= flags;

            if (ve is IPanelComponentRootElement)
                // We crossed a panel component boundary, don't dirty the "without nested" flags anymore
                flags &= ~VisualElementTransformFlags.LocalBoundsWithoutNested3DDirty;

            DirtyParentHierarchy(ve.hierarchy.parent, flags);
        }

        private static void DirtyParentHierarchy(VisualElement ve, VisualElementTransformFlags flags)
        {
            while (ve != null && (ve.transformFlags & flags) != flags)
            {
                ve.transformFlags |= flags;

                if (ve is IPanelComponentRootElement)
                    // We crossed a panel component boundary, don't dirty the "without nested" flags anymore
                    flags &= ~VisualElementTransformFlags.LocalBoundsWithoutNested3DDirty;

                ve = ve.hierarchy.parent;
            }
        }

        public override void Update()
        {
            // Nothing to update. Don't call UpdateElementUnderPointers for world-space panels.
        }
    }
}
