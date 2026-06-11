// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.UIElements.Experimental;

namespace UnityEngine.UIElements.HierarchyV2
{
    /// <summary>
    /// Clip-based appear/disappear animation for <see cref="CollectionView"/>. Reparents the
    /// affected items into an absolutely-positioned <c>overflow:hidden</c> container and
    /// animates its height. Items below the batch shift in lockstep with the clip.
    /// </summary>
    [VisibleToOtherModules("UnityEngine.HierarchyModule")]
    internal class CollectionViewClipAnimation : ICollectionViewAnimation
    {
        const float k_HeightThreshold = 60f;
        const float k_MaxDurationMs = 70f; // Matches the legacy speed (increase for slowdown, decrease for speedup).

        VisualElement m_Owner;
        VisualElement m_ClipParent;
        Func<int, RecycledItem> m_RecycledItemForIndex;
        Action m_ScheduleRefresh;
        Action m_ClearBindWindow;
        Action m_OnAnimationCompleted;
        Func<int> m_GetVisibleViewportCount;

        VisualElement m_ClipContainer;
        ValueAnimation<StyleValues> m_Animator;

        ItemAnimationInfo? m_CurrentBatchInfo;

        // Captured via valueUpdated; resolvedStyle.height lags one frame.
        float m_CurrentClipHeight;

        // Disappear's deferred data clear. Reversal swaps it; SkipAnimation fires it on interrupt.
        Action m_CurrentOnComplete;

        // Tracked as RecycledItems so Cleanup restores positions through the verticalOffset cache.
        readonly List<RecycledItem> m_BatchItems = new();

        readonly List<(RecycledItem item, float originalY)> m_ShiftedItems = new();

        float m_ClipOriginY;
        float m_AnimatedTotalHeight;

        // Distinct from m_AnimatedTotalHeight when not all batch items are visible (disappear).
        // Below-items scale by the full height to reach the parent row.
        float m_FullBatchHeight;

        public bool isAnimating => m_Animator != null && m_Animator.isRunning;

        public void Initialize(in CollectionViewAnimationContext context)
        {
            m_RecycledItemForIndex = context.recycledItemForIndex;
            m_Owner = context.itemContainer;
            m_ClipParent = context.clipParent;
            m_ScheduleRefresh = context.scheduleRefresh;
            m_ClearBindWindow = context.clearBindWindow;
            m_OnAnimationCompleted = context.onAnimationCompleted;
            m_GetVisibleViewportCount = context.getVisibleViewportCount;
        }

        float ComputeAnimatedTotalHeight(int batchItemCount, float itemHeight)
        {
            var byBatch = batchItemCount * itemHeight;
            var viewportItemCount = m_GetVisibleViewportCount?.Invoke() ?? batchItemCount;
            var byViewport = viewportItemCount * itemHeight;
            return Mathf.Min(byBatch, byViewport);
        }

        public void OnItemsAppearing(ItemAnimationInfo info)
        {
        }

        public void OnItemsDisappearing(ItemAnimationInfo info, Action onComplete)
        {
            CollectBatchItems(info);
            if (m_BatchItems.Count == 0)
            {
                onComplete();
                return;
            }

            // Animate over the visible portion; below-items track the full batch height.
            m_AnimatedTotalHeight = ComputeAnimatedTotalHeight(m_BatchItems.Count, info.itemHeight);
            m_FullBatchHeight = info.count * info.itemHeight;
            m_ClipOriginY = m_BatchItems[0].verticalOffset;

            BuildClipContainer(m_AnimatedTotalHeight, info.itemHeight);
            CollectItemsBelowByIndex(info);

            m_CurrentBatchInfo = info;
            m_CurrentOnComplete = onComplete;
            m_CurrentClipHeight = m_AnimatedTotalHeight;

            StartAnimation(m_AnimatedTotalHeight, 0f, isAppearing: false);
        }

        public void OnItemBound(RecycledItem item, int index, ItemAnimationContext? context) { }

        public void OnRefreshCompleted() { }

        public void OnItemsAppeared(ItemAnimationInfo info)
        {
            CollectBatchItems(info);
            if (m_BatchItems.Count == 0)
                return;

            // Parent row has a settled offset; force-bound children may not.
            var parentItem = m_RecycledItemForIndex?.Invoke(info.firstIndex - 1);
            m_ClipOriginY = parentItem != null
                ? parentItem.verticalOffset + info.itemHeight
                : m_BatchItems[0].verticalOffset;

            m_FullBatchHeight = info.count * info.itemHeight;
            m_AnimatedTotalHeight = ComputeAnimatedTotalHeight(m_BatchItems.Count, info.itemHeight);

            BuildClipContainer(0f, info.itemHeight);
            CollectItemsBelowByIndex(info);

            foreach (var (item, originalY) in m_ShiftedItems)
                item.verticalOffset = originalY - m_FullBatchHeight;

            m_CurrentBatchInfo = info;
            m_CurrentOnComplete = null; // Appear sets data eagerly.
            m_CurrentClipHeight = 0f;

            StartAnimation(0f, m_AnimatedTotalHeight, isAppearing: true);
        }

        public bool TryReverseAnimation(ItemAnimationInfo info, Action onComplete)
        {
            if (m_Animator == null || !m_Animator.isRunning)
                return false;

            if (m_CurrentBatchInfo is not { } current)
                return false;

            if (current.firstIndex != info.firstIndex
                || current.count != info.count
                || current.isAppearing == info.isAppearing)
                return false;

            // Replace the in-flight animator without firing its OnCompleted.
            m_Animator.onAnimationCompleted = null;
            m_Animator.Stop();
            RecycleAnimator();

            var fromHeight = m_CurrentClipHeight;
            var targetHeight = info.isAppearing ? m_AnimatedTotalHeight : 0f;

            // Proportional remaining duration so reversal velocity matches forward velocity.
            var distance = m_AnimatedTotalHeight > 0 ? Mathf.Abs(targetHeight - fromHeight) : 0f;
            var fullDurationMs = GetDurationMs(m_AnimatedTotalHeight);
            var remainingMs = m_AnimatedTotalHeight > 0
                ? Mathf.Max(1f, distance / m_AnimatedTotalHeight * fullDurationMs)
                : 1f;

            m_CurrentBatchInfo = info;
            m_CurrentOnComplete = onComplete;

            StartAnimator(fromHeight, targetHeight, (int)remainingMs, info.isAppearing);

            return true;
        }

        public void SkipAnimation()
        {
            // Cleanup must run before invoking it so RefreshItems triggered by pending doesn't find items still inside the clip.
            var pending = GetCurrentOnComplete();
            var hadAnimation = m_Animator != null;

            if (m_Animator != null)
            {
                // Stop() fires OnCompleted synchronously — clear it first before firing pending below.
                m_Animator.onAnimationCompleted = null;
                m_Animator.Stop();
                RecycleAnimator();
            }

            Cleanup();
            pending?.Invoke();

            // NotifyItems* call SkipAnimation defensively. Firing unconditionally would clear HierarchyView's animating-node
            // tracker before the new animator starts, breaking same-node reversal/same-direction guards on the next click.
            if (hadAnimation || pending != null)
                m_OnAnimationCompleted?.Invoke();
        }

        void RecycleAnimator()
        {
            m_Animator?.Recycle();
            m_Animator = null;
        }

        Action GetCurrentOnComplete()
        {
            var pending = m_CurrentOnComplete;
            m_CurrentOnComplete = null;
            return pending;
        }

        void CollectBatchItems(ItemAnimationInfo info)
        {
            m_BatchItems.Clear();
            for (var i = 0; i < info.count; i++)
            {
                var item = m_RecycledItemForIndex?.Invoke(info.firstIndex + i);
                if (item != null)
                    m_BatchItems.Add(item);
            }
        }

        void BuildClipContainer(float startHeight, float itemHeight)
        {
            m_ClipContainer = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    top = 0,
                    left = 0,
                    right = 0,
                    overflow = Overflow.Hidden,
                    height = startHeight,
                    translate = new Translate(0, m_ClipOriginY, 0),
                }
            };

            for (var i = 0; i < m_BatchItems.Count; i++)
            {
                var item = m_BatchItems[i];
                item.verticalOffset = i * itemHeight;
                m_ClipContainer.Add(item.element);
            }

            m_ClipParent.Add(m_ClipContainer);
        }

        void StartAnimation(float fromHeight, float toHeight, bool isAppearing)
        {
            StartAnimator(fromHeight, toHeight, (int)GetDurationMs(m_AnimatedTotalHeight), isAppearing);
        }

        void StartAnimator(float fromHeight, float toHeight, int durationMs, bool isAppearing)
        {
            m_Animator = m_ClipContainer.experimental.animation
                .Start(new StyleValues { height = fromHeight }, new StyleValues { height = toHeight }, durationMs);
            m_Animator.KeepAlive();
            m_Animator.valueUpdated += OnAnimatorValueUpdated;
            m_Animator.OnCompleted(isAppearing ? OnAppearCompleted : OnDisappearCompleted);
        }

        void OnAnimatorValueUpdated(VisualElement element, StyleValues sv)
        {
            // We use the interpolated StyleValues; resolvedStyle.height lags a frame and would put below-items behind
            // the clip's actual rendered edge. The VisualElement signature is left in case we want to react to it in the future.
            m_CurrentClipHeight = sv.height;
            UpdateBelowItemOffsets(sv.height);
        }

        void OnAppearCompleted() => OnAnimationDone(scheduleRefresh: true);
        void OnDisappearCompleted() => OnAnimationDone(scheduleRefresh: false);

        void OnAnimationDone(bool scheduleRefresh)
        {
            var pending = GetCurrentOnComplete();
            RecycleAnimator();
            Cleanup();
            pending?.Invoke();
            if (scheduleRefresh)
                m_ScheduleRefresh?.Invoke();
            m_OnAnimationCompleted?.Invoke();
        }

        // Computes originalY arithmetically — resolvedStyle is stale for just-bound items.
        void CollectItemsBelowByIndex(ItemAnimationInfo info)
        {
            m_ShiftedItems.Clear();

            var firstBelow = info.firstIndex + info.count;

            for (var i = 0; ; i++)
            {
                var index = firstBelow + i;
                var item = m_RecycledItemForIndex?.Invoke(index);
                if (item == null)
                    break;

                var originalY = m_ClipOriginY + m_FullBatchHeight + i * info.itemHeight;
                m_ShiftedItems.Add((item, originalY));
            }
        }

        // Track clip's bottom edge so below-items stay flush during the animation.
        void UpdateBelowItemOffsets(float currentClipHeight)
        {
            if (m_ClipContainer == null)
                return;

            if (float.IsNaN(currentClipHeight))
                currentClipHeight = 0;

            var shift = currentClipHeight - m_FullBatchHeight;
            foreach (var (item, originalY) in m_ShiftedItems)
                item.verticalOffset = originalY + shift;
        }

        void Cleanup()
        {
            m_CurrentBatchInfo = null;
            m_CurrentClipHeight = 0f;

            m_ClearBindWindow?.Invoke();

            // Write through verticalOffset so the cache stays in sync — the post-cleanup
            // BindVisibleItems cascade reads from it.
            foreach (var (item, originalY) in m_ShiftedItems)
                item.verticalOffset = originalY;
            m_ShiftedItems.Clear();

            if (m_ClipContainer == null)
            {
                m_BatchItems.Clear();
                return;
            }

            for (var i = 0; i < m_BatchItems.Count; i++)
            {
                var item = m_BatchItems[i];
                var localY = item.verticalOffset; // already in clip-local coords
                m_Owner.Add(item.element);
                item.verticalOffset = localY + m_ClipOriginY;
            }
            m_BatchItems.Clear();

            m_ClipContainer.RemoveFromHierarchy();
            m_ClipContainer = null;
        }

        static float GetDurationMs(float height)
        {
            return height > k_HeightThreshold
                ? k_MaxDurationMs
                : height * k_MaxDurationMs / k_HeightThreshold;
        }
    }
}
