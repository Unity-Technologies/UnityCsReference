namespace UnityEngine.UIElements
{
    internal class NavigateFocusRing : IFocusRing
    {
        public class ChangeDirection : FocusChangeDirection
        {
            public ChangeDirection(int i) : base(i) {}
        }

        public static readonly ChangeDirection Left = new ChangeDirection(1);
        public static readonly ChangeDirection Right = new ChangeDirection(2);
        public static readonly ChangeDirection Up = new ChangeDirection(3);
        public static readonly ChangeDirection Down = new ChangeDirection(4);
        public static readonly ChangeDirection Next = new ChangeDirection(5);
        public static readonly ChangeDirection Previous = new ChangeDirection(6);

        private readonly VisualElement m_Root;
        private readonly VisualElementFocusRing m_Ring;

        public NavigateFocusRing(VisualElement root)
        {
            m_Root = root;
            m_Ring = new VisualElementFocusRing(root);
        }

        public FocusChangeDirection GetFocusChangeDirection(Focusable currentFocusable, EventBase e)
        {
            if (e.eventTypeId == PointerDownEvent.TypeId())
            {
                if (e.target is Focusable focusable)
                    return VisualElementFocusChangeTarget.GetPooled(focusable);
            }

            if (e.eventTypeId == NavigationMoveEvent.TypeId())
            {
                switch (((NavigationMoveEvent)e).direction)
                {
                    case NavigationMoveEvent.Direction.Left: return Left;
                    case NavigationMoveEvent.Direction.Up: return Up;
                    case NavigationMoveEvent.Direction.Right: return Right;
                    case NavigationMoveEvent.Direction.Down: return Down;
                }
            }
            //TODO: make NavigationTabEvent public and use it here
            else if (e.eventTypeId == KeyDownEvent.TypeId())
            {
                var kde = (KeyDownEvent)e;

                // Important: using KeyDownEvent.character for focus prevents a TextField bug.
                // IMGUI sends KeyDownEvent with keyCode != None, then it sends another one with character != '\0'.
                // If we use keyCode instead of character, TextField will receive focus on the first KeyDownEvent,
                // then text will become selected and, in the case of multiline, the KeyDownEvent with character = '\t'
                // will immediately overwrite the text with a single Tab string.
                if (kde.character == (char)25 || kde.character == '\t')
                    return kde.shiftKey ? Previous : Next;
            }

            return FocusChangeDirection.none;
        }

        public virtual Focusable GetNextFocusable(Focusable currentFocusable, FocusChangeDirection direction)
        {
            if (direction is VisualElementFocusChangeTarget changeTarget)
            {
                return changeTarget.target;
            }

            if (direction == Next || direction == Previous)
            {
                return m_Ring.GetNextFocusable(currentFocusable, direction == Next
                    ? VisualElementFocusChangeDirection.right
                    : VisualElementFocusChangeDirection.left);
            }

            if (direction == Up || direction == Down || direction == Right || direction == Left)
            {
                return GetNextFocusable2D(currentFocusable, (ChangeDirection)direction);
            }

            return currentFocusable;
        }

        Focusable GetNextFocusable2D(Focusable currentFocusable, ChangeDirection direction)
        {
            if (!(currentFocusable is VisualElement ve))
                ve = m_Root;

            ve = GetRootFocusable(ve);

            Rect panelBounds = m_Root.worldBoundingBox;
            Rect panelRect = new Rect(panelBounds.position - Vector2.one, panelBounds.size + Vector2.one * 2);
            Rect rect = ve.worldBound;
            Rect validRect = new Rect(rect.position - Vector2.one, rect.size + Vector2.one * 2);
            if (direction == Up) validRect.yMin = panelRect.yMin;
            else if (direction == Down) validRect.yMax = panelRect.yMax;
            else if (direction == Left) validRect.xMin = panelRect.xMin;
            else if (direction == Right) validRect.xMax = panelRect.xMax;

            var best = new FocusableHierarchyTraversal
            {
                currentFocusable = ve,
                direction = direction,
                validRect = validRect,
                firstPass = true
            }.GetBestOverall(m_Root);

            if (best != null)
                return GetLeafFocusable(best);

            validRect = new Rect(rect.position - Vector2.one, rect.size + Vector2.one * 2);
            if (direction == Down) validRect.yMin = panelRect.yMin;
            else if (direction == Up) validRect.yMax = panelRect.yMax;
            else if (direction == Right) validRect.xMin = panelRect.xMin;
            else if (direction == Left) validRect.xMax = panelRect.xMax;

            best = new FocusableHierarchyTraversal
            {
                currentFocusable = ve,
                direction = direction,
                validRect = validRect,
                firstPass = false
            }.GetBestOverall(m_Root);

            if (best != null)
                return GetLeafFocusable(best);

            return currentFocusable;
        }

        static bool IsActive(VisualElement v)
        {
            return v.resolvedStyle.display != DisplayStyle.None && v.enabledInHierarchy;
        }

        static bool IsFocusable(Focusable focusable)
        {
            return focusable.canGrabFocus && focusable.tabIndex >= 0;
        }

        static bool IsLeaf(Focusable focusable)
        {
            return !focusable.excludeFromFocusRing && !focusable.delegatesFocus;
        }

        static bool IsFocusRoot(VisualElement focusable)
        {
            if (focusable.isCompositeRoot) return true;
            var parent = focusable.hierarchy.parent;
            return parent == null || !IsFocusable(parent);
        }

        static VisualElement GetLeafFocusable(VisualElement v)
        {
            return GetLeafFocusableRecursive(v) ?? v;
        }

        static VisualElement GetLeafFocusableRecursive(VisualElement v)
        {
            if (IsLeaf(v))
                return v;
            int n = v.childCount;
            for (int i = 0; i < n; i++)
            {
                var child = v[i];
                if (!IsFocusable(child))
                    continue;
                var leaf = GetLeafFocusableRecursive(child);
                if (leaf != null)
                    return leaf;
            }
            return null;
        }

        static VisualElement GetRootFocusable(VisualElement v)
        {
            while (true)
            {
                if (IsFocusRoot(v))
                    return v;
                v = v.hierarchy.parent;
            }
        }

        struct FocusableHierarchyTraversal
        {
            public VisualElement currentFocusable;
            public Rect validRect;
            public bool firstPass;
            public ChangeDirection direction;

            bool ValidateHierarchyTraversal(VisualElement v)
            {
                return IsActive(v) && v.worldBoundingBox.Overlaps(validRect);
            }

            bool ValidateElement(VisualElement v)
            {
                return IsFocusable(v) && v.worldBound.Overlaps(validRect);
            }

            int Order(VisualElement a, VisualElement b)
            {
                Rect ra = a.worldBound, rb = b.worldBound;
                int result = StrictOrder(ra, rb);
                return result != 0 ? result : TieBreaker(ra, rb);
            }

            int StrictOrder(VisualElement a, VisualElement b)
            {
                return StrictOrder(a.worldBound, b.worldBound);
            }

            int StrictOrder(Rect ra, Rect rb)
            {
                float diff = 0f;
                if (direction == Up) diff = rb.yMax - ra.yMax;
                else if (direction == Down) diff = ra.yMin - rb.yMin;
                else if (direction == Left) diff = rb.xMax - ra.xMax;
                else if (direction == Right) diff = ra.xMin - rb.xMin;
                if (!Mathf.Approximately(diff, 0f))
                    return diff > 0 ? 1 : -1;
                return 0;
            }

            int TieBreaker(Rect ra, Rect rb)
            {
                // Elements are aligned in the search axis. Find who's top-left corner is closer to current element.
                // TODO: use other corners if grow direction is not left-to-right / top-to-bottom
                Rect rc = currentFocusable.worldBound;
                float diff = (ra.min - rc.min).sqrMagnitude - (rb.min - rc.min).sqrMagnitude;
                if (!Mathf.Approximately(diff, 0f))
                    return diff > 0 ? 1 : -1;

                // Elements probably only differ through numerical rounding errors, so we don't force an order.
                return 0;
            }

            public VisualElement GetBestOverall(VisualElement candidate, VisualElement bestSoFar = null)
            {
                if (!ValidateHierarchyTraversal(candidate))
                    return bestSoFar;

                if (ValidateElement(candidate))
                {
                    if ((!firstPass || StrictOrder(candidate, currentFocusable) > 0) &&
                        (bestSoFar == null || Order(bestSoFar, candidate) > 0))
                        bestSoFar = candidate;

                    if (IsFocusRoot(candidate))
                        return bestSoFar;
                }

                int n = candidate.childCount;
                for (int i = 0; i < n; i++)
                {
                    var child = candidate[i];
                    bestSoFar = GetBestOverall(child, bestSoFar);
                }

                return bestSoFar;
            }
        }
    }
}
