// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Define focus change directions for the VisualElementFocusRing.
    /// </summary>
    public class VisualElementFocusChangeDirection : FocusChangeDirection
    {
        static readonly VisualElementFocusChangeDirection s_Left = new VisualElementFocusChangeDirection(FocusChangeDirection.lastValue + 1);

        /// <summary>
        /// The focus is moving to the left.
        /// </summary>
        public static FocusChangeDirection left => s_Left;

        static readonly VisualElementFocusChangeDirection s_Right = new VisualElementFocusChangeDirection(FocusChangeDirection.lastValue + 2);

        /// <summary>
        /// The focus is moving to the right.
        /// </summary>
        public static FocusChangeDirection right => s_Right;

        /// <summary>
        /// Last value for the direction defined by this class.
        /// </summary>
        protected new static VisualElementFocusChangeDirection lastValue { get { return s_Right; } }

        protected VisualElementFocusChangeDirection(int value) : base(value)
        {
        }
    }

    /// <summary>
    /// Define focus change to specific target for the VisualElementFocusRing.
    /// </summary>
    internal class VisualElementFocusChangeTarget : FocusChangeDirection
    {
        static readonly ObjectPool<VisualElementFocusChangeTarget> Pool = new ObjectPool<VisualElementFocusChangeTarget>(() => new VisualElementFocusChangeTarget());

        /// <summary>
        /// Gets a VisualElementFocusChangeTarget from the pool and initializes it with the given target. Use this function instead of creating new VisualElementFocusChangeTarget. Results obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        public static VisualElementFocusChangeTarget GetPooled(Focusable target)
        {
            var r = Pool.Get();
            r.target = target;
            return r;
        }

        protected override void Dispose()
        {
            target = null;
            Pool.Release(this);
        }

        internal override void ApplyTo(FocusController focusController, Focusable f)
        {
            // Unselect selected TextElement on pointer down.
            focusController.selectedTextElement = null;

            f.Focus(); // Call Focus() virtual method when an element is clicked or otherwise focused explicitly.
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public VisualElementFocusChangeTarget() : base(unspecified) {}

        /// <summary>
        /// The target to which the focus should be moved.
        /// </summary>
        public Focusable target { get; private set; }
    }

    /// <summary>
    /// Implementation of a linear focus ring. Elements are sorted according to their focusIndex.
    /// </summary>
    public class VisualElementFocusRing : IFocusRing
    {
        /// <summary>
        /// Ordering of elements in the focus ring.
        /// </summary>
        public enum DefaultFocusOrder
        {
            /// <summary>
            /// Order elements using a depth-first pre-order traversal of the element tree.
            /// </summary>
            ChildOrder,
            /// <summary>
            /// Order elements according to their position, first by X, then by Y.
            /// </summary>
            PositionXY,
            /// <summary>
            /// Order elements according to their position, first by Y, then by X.
            /// </summary>
            PositionYX
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="root">The root of the element tree for which we want to build a focus ring.</param>
        /// <param name="dfo">Default ordering of the elements in the ring.</param>
        public VisualElementFocusRing(VisualElement root, DefaultFocusOrder dfo = DefaultFocusOrder.ChildOrder)
        {
            defaultFocusOrder = dfo;
            this.root = root;
            m_FocusRing = new List<FocusRingRecord>();
        }

        readonly VisualElement root;
        private FocusController focusController => root.focusController;

        /// <summary>
        /// The focus order for elements having 0 has a focusIndex.
        /// </summary>
        public DefaultFocusOrder defaultFocusOrder { get; set; }

        class FocusRingRecord
        {
            public int m_AutoIndex;
            public Focusable m_Focusable;
            public bool m_IsSlot;
            public List<FocusRingRecord> m_ScopeNavigationOrder;
        }

        List<FocusRingRecord> m_FocusRing;

        int FocusRingAutoIndexSort(FocusRingRecord a, FocusRingRecord b)
        {
            switch (defaultFocusOrder)
            {
                case DefaultFocusOrder.ChildOrder:
                default:
                    return Comparer<int>.Default.Compare(a.m_AutoIndex, b.m_AutoIndex);

                case DefaultFocusOrder.PositionXY:
                {
                    VisualElement ave = a.m_Focusable as VisualElement;
                    VisualElement bve = b.m_Focusable as VisualElement;

                    if (ave != null && bve != null)
                    {
                        if (ave.layout.position.x < bve.layout.position.x)
                        {
                            return -1;
                        }
                        else if (ave.layout.position.x > bve.layout.position.x)
                        {
                            return 1;
                        }
                        else
                        {
                            if (ave.layout.position.y < bve.layout.position.y)
                            {
                                return -1;
                            }
                            else if (ave.layout.position.y > bve.layout.position.y)
                            {
                                return 1;
                            }
                        }
                    }

                    // a and b should be ordered using their order of appearance.
                    return Comparer<int>.Default.Compare(a.m_AutoIndex, b.m_AutoIndex);
                }
                case DefaultFocusOrder.PositionYX:
                {
                    VisualElement ave = a.m_Focusable as VisualElement;
                    VisualElement bve = b.m_Focusable as VisualElement;

                    if (ave != null && bve != null)
                    {
                        if (ave.layout.position.y < bve.layout.position.y)
                        {
                            return -1;
                        }
                        else if (ave.layout.position.y > bve.layout.position.y)
                        {
                            return 1;
                        }
                        else
                        {
                            if (ave.layout.position.x < bve.layout.position.x)
                            {
                                return -1;
                            }
                            else if (ave.layout.position.x > bve.layout.position.x)
                            {
                                return 1;
                            }
                        }
                    }

                    // a and b should be ordered using their order of appearance.
                    return Comparer<int>.Default.Compare(a.m_AutoIndex, b.m_AutoIndex);
                }
            }
        }

        int FocusRingSort(FocusRingRecord a, FocusRingRecord b)
        {
            if (a.m_Focusable.tabIndex == 0 && b.m_Focusable.tabIndex == 0)
            {
                return FocusRingAutoIndexSort(a, b);
            }
            else if (a.m_Focusable.tabIndex == 0)
            {
                // Only b has a focus index. It has priority.
                return 1;
            }
            else if (b.m_Focusable.tabIndex == 0)
            {
                // Only a has a focus index. It has priority.
                return -1;
            }
            else
            {
                // a and b should be ordered using their focus index.
                int result = Comparer<int>.Default.Compare(a.m_Focusable.tabIndex, b.m_Focusable.tabIndex);
                // but if the focus index result is being equal, we need to fallback with their automatic index
                if (result == 0)
                {
                    result = FocusRingAutoIndexSort(a, b);
                }
                return result;
            }
        }

        void DoUpdate()
        {
            m_FocusRing.Clear();
            if (root != null)
            {
                var rootScopeList = new List<FocusRingRecord>();
                int autoIndex = 0;
                BuildRingForScopeRecursive(root, ref autoIndex, rootScopeList);
                SortAndFlattenScopeLists(rootScopeList);
            }
        }

        void BuildRingForScopeRecursive(VisualElement ve, ref int scopeIndex, List<FocusRingRecord> scopeList)
        {
            var veChildCount = ve.hierarchy.childCount;
            for (int i = 0; i < veChildCount; i++)
            {
                var child = ve.hierarchy[i];

                bool isSlot = child.parent != null && child == child.parent.contentContainer;

                if (child.isCompositeRoot || isSlot)
                {
                    var childRecord = new FocusRingRecord
                    {
                        m_AutoIndex = scopeIndex++,
                        m_Focusable = child,
                        m_IsSlot = isSlot,
                        m_ScopeNavigationOrder = new List<FocusRingRecord>()
                    };
                    scopeList.Add(childRecord);

                    int autoIndex = 0;
                    BuildRingForScopeRecursive(child, ref autoIndex, childRecord.m_ScopeNavigationOrder);
                }
                else
                {
                    // areAncestorsAndSelfDisplayed is not checked in canGrabFocus to let a hidden VisualElement grab the focus when calling the Focus method.
                    if (child.canGrabFocus && child.areAncestorsAndSelfDisplayed && child.tabIndex >= 0)
                    {
                        scopeList.Add(new FocusRingRecord
                        {
                            m_AutoIndex = scopeIndex++,
                            m_Focusable = child,
                            m_IsSlot = false,
                            m_ScopeNavigationOrder = null
                        });
                    }

                    BuildRingForScopeRecursive(child, ref scopeIndex, scopeList);
                }
            }
        }

        void SortAndFlattenScopeLists(List<FocusRingRecord> rootScopeList)
        {
            if (rootScopeList != null)
            {
                rootScopeList.Sort(FocusRingSort);
                foreach (var record in rootScopeList)
                {
                    if (record.m_Focusable.canGrabFocus && record.m_Focusable.tabIndex >= 0)
                    {
                        if (!record.m_Focusable.excludeFromFocusRing)
                        {
                            m_FocusRing.Add(record);
                        }

                        SortAndFlattenScopeLists(record.m_ScopeNavigationOrder);
                    }
                    else if (record.m_IsSlot)
                    {
                        SortAndFlattenScopeLists(record.m_ScopeNavigationOrder);
                    }

                    record.m_ScopeNavigationOrder = null;
                }
            }
        }

        int GetFocusableInternalIndex(Focusable f)
        {
            if (f != null)
            {
                for (int i = 0; i < m_FocusRing.Count; i++)
                {
                    if (f == m_FocusRing[i].m_Focusable)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Get the direction of the focus change for the given event. For example, when the Tab key is pressed, focus should be given to the element to the right in the focus ring.
        /// </summary>
        public FocusChangeDirection GetFocusChangeDirection(Focusable currentFocusable, EventBase e)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            // FUTURE:
            // We could implement an extendable adapter system to convert event to a focus change direction.
            // This would enable new event sources to change the focus.

            if (e.eventTypeId == PointerDownEvent.TypeId())
            {
                if (focusController.GetFocusableParentForPointerEvent(e.elementTarget, out var target))
                    return VisualElementFocusChangeTarget.GetPooled(target);
            }

            if (currentFocusable != null && currentFocusable.isIMGUIContainer)
            {
                // Let IMGUIContainer manage the focus change.
                return FocusChangeDirection.none;
            }

            if (e.eventTypeId == NavigationMoveEvent.TypeId())
            {
                // If navigation event was sent to an element that doesn't have the focus, ignore it.
                // This is helpful in cases where a control reacts to KeyDown and moves the focus away, in which case
                // any further equivalent navigation events should not move the focus again.

                var direction = ((NavigationMoveEvent) e).direction;
                return direction == NavigationMoveEvent.Direction.Next ? VisualElementFocusChangeDirection.right :
                    direction == NavigationMoveEvent.Direction.Previous ? VisualElementFocusChangeDirection.left :
                    FocusChangeDirection.none;
            }

            return FocusChangeDirection.none;
        }

        /// <summary>
        /// Get the next element in the given direction.
        /// </summary>
        public Focusable GetNextFocusable(Focusable currentFocusable, FocusChangeDirection direction)
        {
            if (direction == FocusChangeDirection.none || direction == FocusChangeDirection.unspecified)
            {
                return currentFocusable;
            }

            if (direction is VisualElementFocusChangeTarget changeTarget)
            {
                return changeTarget.target;
            }

            DoUpdate();

            if (m_FocusRing.Count == 0)
            {
                return null;
            }

            int previousIndex = GetFocusableInternalIndex(currentFocusable);

            if (currentFocusable != null && previousIndex == -1)
            {
                // currentFocusable was not found in the ring. Use the element tree to find the next focusable.
                if (direction == VisualElementFocusChangeDirection.right)
                    return GetNextFocusableInTree(currentFocusable as VisualElement);
                if (direction == VisualElementFocusChangeDirection.left)
                    return GetPreviousFocusableInTree(currentFocusable as VisualElement);
            }

            int index = 0;
            if (direction == VisualElementFocusChangeDirection.right)
            {
                index = previousIndex + 1;

                if (index == m_FocusRing.Count)
                {
                    index = 0;
                }

                // FIXME: Element could be unrelated to delegator; should we detect this case and return null?
                // Spec is not very clear on this.
                while (m_FocusRing[index].m_Focusable.delegatesFocus)
                {
                    index++;
                    if (index == m_FocusRing.Count)
                    {
                        return null;
                    }
                }
            }
            else if (direction == VisualElementFocusChangeDirection.left)
            {
                index = previousIndex - 1;

                if (index < 0)
                {
                    index = m_FocusRing.Count - 1;
                }

                while (m_FocusRing[index].m_Focusable.delegatesFocus)
                {
                    index--;
                    if (index == -1)
                    {
                        return null;
                    }
                }
            }

            return m_FocusRing[index].m_Focusable;
        }

        internal static Focusable GetNextFocusableInTree(VisualElement currentFocusable)
        {
            if (currentFocusable == null)
            {
                return null;
            }

            VisualElement ve = currentFocusable.GetNextElementDepthFirst();
            while (!ve.canGrabFocus || ve.tabIndex < 0 || ve.excludeFromFocusRing)
            {
                ve = ve.GetNextElementDepthFirst();

                if (ve == null)
                {
                    // continue at the beginning
                    ve = currentFocusable.GetRoot();
                }

                if (ve == currentFocusable)
                {
                    // We went through the whole tree and did not find anything.
                    return currentFocusable;
                }
            }

            return ve;
        }

        internal static Focusable GetPreviousFocusableInTree(VisualElement currentFocusable)
        {
            if (currentFocusable == null)
            {
                return null;
            }

            VisualElement ve = currentFocusable.GetPreviousElementDepthFirst();
            while (!ve.canGrabFocus || ve.tabIndex < 0 || ve.excludeFromFocusRing)
            {
                ve = ve.GetPreviousElementDepthFirst();

                if (ve == null)
                {
                    // continue at the end
                    ve = currentFocusable.GetRoot();
                    while (ve.childCount > 0)
                    {
                        ve = ve.hierarchy.ElementAt(ve.childCount - 1);
                    }
                }

                if (ve == currentFocusable)
                {
                    // We went through the whole tree and did not find anything.
                    return currentFocusable;
                }
            }

            return ve;
        }
    }
}
