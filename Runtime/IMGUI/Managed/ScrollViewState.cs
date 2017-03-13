// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting;

namespace UnityEngine
{
    internal class ScrollViewState
    {
        public Rect position;
        public Rect visibleRect;
        public Rect viewRect;
        public Vector2 scrollPosition;
        public bool apply;

        [RequiredByNativeCode] // Created by reflection from GUI.BeginScrollView
        public ScrollViewState() {}

        public void ScrollTo(Rect pos)
        {
            ScrollTowards(pos, Mathf.Infinity);
        }

        public bool ScrollTowards(Rect pos, float maxDelta)
        {
            Vector2 scrollVector = ScrollNeeded(pos);

            // If we don't need scrolling, return false
            if (scrollVector.sqrMagnitude < 0.0001f)
                return false;

            // If we need scrolling but don't actually allow any, just return true to
            // indicate scrolling is needed to be able to see pos
            if (maxDelta == 0)
                return true;

            // Clamp scrolling to max allowed delta
            if (scrollVector.magnitude > maxDelta)
                scrollVector = scrollVector.normalized * maxDelta;

            // Apply scrolling
            scrollPosition += scrollVector;
            apply = true;

            return true;
        }

        private Vector2 ScrollNeeded(Rect pos)
        {
            Rect r = visibleRect;
            r.x += scrollPosition.x;
            r.y += scrollPosition.y;

            // If the rect we want to see is larger than the visible rect, then trim it,
            // otherwise we can get oscillation or other unwanted behavior
            float excess = pos.width - visibleRect.width;
            if (excess > 0)
            {
                pos.width -= excess;
                pos.x += excess * 0.5f;
            }
            excess = pos.height - visibleRect.height;
            if (excess > 0)
            {
                pos.height -= excess;
                pos.y += excess * 0.5f;
            }

            Vector2 scrollVector = Vector2.zero;

            // Calculate needed x scrolling
            if (pos.xMax > r.xMax)
                scrollVector.x += pos.xMax - r.xMax;
            else if (pos.xMin < r.xMin)
                scrollVector.x -= r.xMin - pos.xMin;

            // Calculate needed y scrolling
            if (pos.yMax > r.yMax)
                scrollVector.y += pos.yMax - r.yMax;
            else if (pos.yMin < r.yMin)
                scrollVector.y -= r.yMin - pos.yMin;

            // Clamp scrolling to bounds so we don't request to scroll past the edge
            Rect actualViewRect = viewRect;
            actualViewRect.width = Mathf.Max(actualViewRect.width, visibleRect.width);
            actualViewRect.height = Mathf.Max(actualViewRect.height, visibleRect.height);
            scrollVector.x = Mathf.Clamp(scrollVector.x, actualViewRect.xMin - scrollPosition.x, actualViewRect.xMax - visibleRect.width - scrollPosition.x);
            scrollVector.y = Mathf.Clamp(scrollVector.y, actualViewRect.yMin - scrollPosition.y, actualViewRect.yMax - visibleRect.height - scrollPosition.y);

            return scrollVector;
        }
    }
}
