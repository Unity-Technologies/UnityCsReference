// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    internal static class KeyboardNavigation
    {
        /// <summary>
        /// Keeps a newly focused control visible while the user navigates with Tab.
        /// UI Toolkit's <see cref="ScrollView"/> does not scroll to a focused descendant on its own,
        /// so tabbing to a control below the fold otherwise looks like it does nothing.
        /// </summary>
        /// <remarks>
        /// When focus lands inside a nested <see cref="ScrollView"/> (for example the message log's list or
        /// the asset tables), the outer view scrolls to reveal that nested view as a whole and lets it scroll
        /// its own rows.
        /// </remarks>
        public static void ScrollFocusedIntoView(ScrollView scrollView)
        {
            if (scrollView == null)
                return;

            scrollView.RegisterCallback<FocusInEvent>(evt =>
            {
                if (evt.target is not VisualElement focused)
                    return;

                // Reveal the focused control unless it lives inside a nested ScrollView, in which case reveal
                // that nested view instead. Walk up to this view, keeping the outermost nested ScrollView found along the way.
                var target = focused;
                for (var ancestor = focused.hierarchy.parent;
                     ancestor != null && ancestor != scrollView;
                     ancestor = ancestor.hierarchy.parent)
                {
                    if (ancestor is ScrollView nested)
                        target = nested;
                }

                // Skip focus that landed outside the scrollable content (scrollbars, the ScrollView itself) —
                // ScrollTo only accepts content-container descendants.
                if (scrollView.contentContainer.Contains(target))
                    scrollView.ScrollTo(target);
            });
        }
    }
}
