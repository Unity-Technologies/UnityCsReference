// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements.HierarchyV2
{
    /// <summary>
    /// Describes a batch of items entering or leaving the list. Pure data — no visual properties.
    /// </summary>
    [VisibleToOtherModules("UnityEngine.HierarchyModule")]
    internal struct ItemAnimationInfo
    {
        /// <summary>The index of the first item in the affected batch.</summary>
        public int firstIndex;

        /// <summary>The number of items in the batch. Use 1 for a single item.</summary>
        public int count;

        /// <summary>The height of each item in pixels.</summary>
        public float itemHeight;

        /// <summary>True if items are entering the list; false if they are leaving.</summary>
        public bool isAppearing;

        /// <summary>Total pixel height of the batch.</summary>
        public float totalHeight => count * itemHeight;
    }

    /// <summary>
    /// Per-item context passed to <see cref="ICollectionViewAnimation.OnItemBound"/> during an active animation.
    /// </summary>
    [VisibleToOtherModules("UnityEngine.HierarchyModule")]
    internal struct ItemAnimationContext
    {
        /// <summary>The batch this item belongs to.</summary>
        public ItemAnimationInfo batchInfo;

        /// <summary>The 0-based position of this item within the batch.</summary>
        public int indexInBatch;
    }

    /// <summary>Per-tick reflow state a strategy reports via <see cref="CollectionViewAnimationContext.onAnimationProgress"/>.</summary>
    [VisibleToOtherModules("UnityEngine.HierarchyModule")]
    internal struct AnimationReflowState
    {
        /// <summary>The batch being animated.</summary>
        public ItemAnimationInfo batch;

        /// <summary>Settled pixel height of the batch (<c>count * itemHeight</c>).</summary>
        public float fullBatchHeight;

        /// <summary>Current animated height of the clip, in <c>[0, animatedTotalHeight]</c>.</summary>
        public float clipHeight;

        /// <summary>Peak animated height. May be viewport-capped below <see cref="fullBatchHeight"/>.</summary>
        public float animatedTotalHeight;
    }

    /// <summary>
    /// Bag of resources provided to an <see cref="ICollectionViewAnimation"/> on assignment. Lets
    /// strategies grow new dependencies without breaking the interface signature.
    /// </summary>
    [VisibleToOtherModules("UnityEngine.HierarchyModule")]
    internal struct CollectionViewAnimationContext
    {
        /// <summary>Returns the <see cref="RecycledItem"/> for an index, or null if not bound.
        /// Write positions through <see cref="RecycledItem.verticalOffset"/> so the cached offset stays in sync.</summary>
        public Func<int, RecycledItem> recycledItemForIndex;

        /// <summary>The container holding all visible item elements.</summary>
        public VisualElement itemContainer;

        /// <summary>Container for animation clip elements; must sit outside the item container's
        /// overflow-hidden parent to avoid double-clipping at viewport boundaries.</summary>
        public VisualElement clipParent;

        /// <summary>Schedules a deferred refresh; call after cleanup.</summary>
        public Action scheduleRefresh;

        /// <summary>Clears the extra-bind window opened for the batch. Strategies MUST call this
        /// on completion / skip so the virtualizer trims back to the viewport.</summary>
        public Action clearBindWindow;

        /// <summary>Invoked on every terminal state (completion, skip, reversal end).</summary>
        public Action onAnimationCompleted;

        /// <summary>Returns the current viewport item count. Strategies cap animation extents to
        /// this so the visible portion of the clip animates across the full duration on large batches.</summary>
        public Func<int> getVisibleViewportCount;

        /// <summary>Reports per-tick animation progress so the virtualizer can reflow below-items and the clip live.</summary>
        public Action<AnimationReflowState> onAnimationProgress;

        /// <summary>Registers (non-null) or clears (null) the active clip container and its region.</summary>
        public Action<VisualElement, ItemAnimationInfo> setActiveClipContainer;
    }

    /// <summary>
    /// Strategy interface for animating items in a <see cref="CollectionView"/>. Implementations
    /// choose the granularity (group, per-item, or mix) and own all visual decisions.
    /// </summary>
    [VisibleToOtherModules("UnityEngine.HierarchyModule")]
    internal interface ICollectionViewAnimation
    {
        /// <summary>Whether any animation is currently in progress.</summary>
        bool isAnimating { get; }

        /// <summary>
        /// Called once when assigned to a <see cref="CollectionView"/>. Provides resources without
        /// coupling the strategy to <see cref="CollectionView"/> internals.
        /// </summary>
        void Initialize(in CollectionViewAnimationContext context);

        /// <summary>Notifies that a batch is about to appear. Group impls prepare clip containers; per-item impls record the range.</summary>
        void OnItemsAppearing(ItemAnimationInfo info);

        /// <summary>
        /// Notifies that a batch is about to leave. The implementation MUST call <paramref name="onComplete"/>
        /// when the animation finishes (or immediately if not animating). The caller defers data removal until then.
        /// </summary>
        void OnItemsDisappearing(ItemAnimationInfo info, Action onComplete);

        /// <summary>
        /// Called for each item during binding. <paramref name="context"/> is non-null when the
        /// item is part of an active animation batch, and null for normal binds.
        /// </summary>
        void OnItemBound(RecycledItem item, int index, ItemAnimationContext? context);

        /// <summary>Called after <see cref="CollectionView.RefreshItems"/> completes.</summary>
        void OnRefreshCompleted();

        /// <summary>
        /// Called after items have been bound and are visible. Use the recycledItemForIndex
        /// delegate from <see cref="CollectionViewAnimationContext"/> to query bound items.
        /// </summary>
        void OnItemsAppeared(ItemAnimationInfo info);

        /// <summary>
        /// Reverses the in-flight animation when <paramref name="info"/> matches the running batch
        /// (same firstIndex+count, opposite isAppearing). Returns true on success.
        /// </summary>
        bool TryReverseAnimation(ItemAnimationInfo info, Action onComplete);

        /// <summary>Immediately finishes all in-progress animations.</summary>
        void SkipAnimation();
    }
}
