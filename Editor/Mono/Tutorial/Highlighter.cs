// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;
using UnityEngine.UIElements;

namespace UnityEditor
{
    public enum HighlightSearchMode
    {
        None = 0,
        Auto = 1,
        Identifier = 2,
        PrefixLabel = 3,
        Content = 4,
    }

    public partial class Highlighter
    {
        private static GUIView s_View;
        private static EditorWindow s_ViewWindow;
        private static HighlightSearchMode s_SearchMode;
        private static float s_HighlightElapsedTime = 0;
        private static float s_LastTime = 0;
        private static Rect s_RepaintRegion;

        private const float kPulseSpeed = 0.45f; // Pulses per second
        private const float kPopupDuration = 0.33f; // How long time in seconds the popup takes
        private const int kExpansionMovementSize = 5; // How many pixels the rect expands out in the pulsing movement
        // Twice the IMGUI speed because UIToolkit ScrollView scroll offset has a frame delay between updates
        private static readonly float kUIToolkitScrollSpeed = scrollSpeed * 2;

        private static bool s_RecursionLock = false;

        private static VisualElement activeElement = null;
        private static ScrollView activeScrollView = null;
        private static bool activeIsImgui = false;

        private static GUIStyle s_HighlightStyle;
        private static GUIStyle highlightStyle
        {
            get
            {
                if (s_HighlightStyle == null)
                    s_HighlightStyle = new GUIStyle("ControlHighlight");
                return s_HighlightStyle;
            }
        }

        // Public API

        public static void Stop()
        {
            active = false;
            activeVisible = false;
            activeText = string.Empty;
            activeElement = null;
            activeIsImgui = false;
            activeScrollView = null;
            useUIToolkitScrolling = true;
            activeRect = new Rect();

            searchMode = HighlightSearchMode.None;
            s_LastTime = 0;
            s_HighlightElapsedTime = 0;
        }

        public static bool Highlight(string windowTitle, string text)
        {
            return Highlight(windowTitle, text, HighlightSearchMode.Auto);
        }

        public static bool Highlight(string windowTitle, string text, HighlightSearchMode mode)
        {
            bool success = false;
            if (s_RecursionLock || searching)
            {
                Debug.LogWarning("Highlighter recursion detected.  You are calling Highlighter.Highlight() with too much abandon.  Avoid highlighting during layout and repaint events.");
            }
            else if (Event.current != null && Event.current.type == EventType.Layout)
            {
                Debug.LogWarning("You are calling Highlighter.Highlight() inorrectly.  Avoid highlighting during layout and repaint events.");
            }
            else
            {
                s_RecursionLock = true;
                Stop();

                if (!SetWindow(windowTitle))
                {
                    Debug.LogWarning("Window " + windowTitle + " not found.");
                }
                else if (mode != HighlightSearchMode.None)
                {
                    active = true;
                    activeText = text;
                    s_SearchMode = mode;
                    s_LastTime = Time.realtimeSinceStartup;

                    success = Search();
                    if (success)
                    {
                        // Remove first to make sure it's not added more than once.
                        // Removing is ignored if not already assigned, so causes no harm.
                        EditorApplication.update -= Update;
                        EditorApplication.update += Update;

                        if (s_View.windowBackend is IEditorWindowBackend ewb)
                        {
                            ewb.overlayGUIHandler -= ControlHighlightGUI;
                            ewb.overlayGUIHandler += ControlHighlightGUI;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Item " + text + " not found in window " + windowTitle + ".");
                        Stop();
                    }

                    // Repaint all views regardless of success of search.
                    // Calling Highlight may have stopped a previous highlight in any case.
                    InternalEditorUtility.RepaintAllViews();
                }
                s_RecursionLock = false;
            }

            return success;
        }

        public static bool active { get; private set; }

        internal static bool IsSearchingForIdentifier()
        {
            return searchMode == HighlightSearchMode.Identifier || searchMode == HighlightSearchMode.Auto;
        }

        public static void HighlightIdentifier(Rect position, string identifier)
        {
            if (IsSearchingForIdentifier())
            {
                Handle(position, identifier);
            }
        }

        // Private API

        private static void Update()
        {
            Rect prevRect = activeRect;

            // If view's actualView has changed, we might still find a property with the right name in the other view,
            // but it's not the one we're looking for.
            var elementHidden = activeElement != null && (activeElement.panel == null || activeElement.resolvedStyle.display == DisplayStyle.None);
            if (activeRect.width == 0 || !ViewWindowIsActive() || elementHidden)
            {
                EditorApplication.update -= Update;
                if (s_View != null && s_View.windowBackend is IEditorWindowBackend ewb)
                {
                    ewb.overlayGUIHandler -= ControlHighlightGUI;
                }
                Stop();
                InternalEditorUtility.RepaintAllViews();
                return;
            }
            else
            {
                Search();
            }

            if (useUIToolkitScrolling)
                HandleScroll();

            // Keep elapsed time explicitly rather than measuring time since highlight began.
            // This way all views use the same elapsed time even if some realtime elapsed
            // between redraws of the different views.
            if (activeVisible)
                s_HighlightElapsedTime += Time.realtimeSinceStartup - s_LastTime;
            s_LastTime = Time.realtimeSinceStartup;

            // Calculate repaint region
            Rect r = activeRect;
            if (prevRect.width > 0)
            {
                // If the highlight rect moved, make the repaint region include both the old and new rect.
                r.xMin = Mathf.Min(r.xMin, prevRect.xMin);
                r.xMax = Mathf.Max(r.xMax, prevRect.xMax);
                r.yMin = Mathf.Min(r.yMin, prevRect.yMin);
                r.yMax = Mathf.Max(r.yMax, prevRect.yMax);
            }
            r = highlightStyle.padding.Add(r);
            r = highlightStyle.overflow.Add(r);
            r = new RectOffset(kExpansionMovementSize + 2, kExpansionMovementSize + 2, kExpansionMovementSize + 2, kExpansionMovementSize + 2).Add(r);
            if (s_HighlightElapsedTime < kPopupDuration + 0.1f)
                r = new RectOffset((int)r.width / 2, (int)r.width / 2, (int)r.height / 2, (int)r.height / 2).Add(r);
            s_RepaintRegion = r;

            // Repaint all views regardless of success of search.
            // If control disappeared we still need a repaint to hide it.
            foreach (GUIView view in Resources.FindObjectsOfTypeAll(typeof(GUIView)))
            {
                // Only handle highlight in views in the same window as the view with the highlight is in.
                if (view.window == s_View.window)
                    view.SendEvent(EditorGUIUtility.CommandEvent("HandleControlHighlight"));
            }
        }

        private static bool SetWindow(string windowTitle)
        {
            // Get window of type
            Object[] views = Resources.FindObjectsOfTypeAll(typeof(GUIView));
            GUIView view = null;
            EditorWindow window = null;
            foreach (GUIView currentView in views)
            {
                if (currentView is HostView hostView)
                {
                    if (hostView.actualView.titleContent.text == windowTitle)
                    {
                        view = currentView;
                        window = hostView.actualView;
                        break;
                    }
                }
                else if (currentView.window && currentView.GetType().Name == windowTitle)
                {
                    view = currentView;
                    break;
                }
            }

            if (s_View != null && s_View.windowBackend is IEditorWindowBackend ewb)
            {
                ewb.overlayGUIHandler -= ControlHighlightGUI;
            }

            s_View = view;
            s_ViewWindow = window;

            return (view != null);
        }

        private static bool ViewWindowIsActive()
        {
            return s_View != null && !(s_View is HostView hostView && hostView.actualView != s_ViewWindow);
        }

        private static bool Search()
        {
            searchMode = s_SearchMode;

            if (isUIToolkitWindow && !activeIsImgui)
            {
                var found = activeElement != null;
                if (!found)
                    found = SearchVisualElement(s_ViewWindow.rootVisualElement);

                if (found)
                {
                    var windowPos = s_ViewWindow.position.position;
                    var pos = activeElement.worldBound.position + windowPos;
                    activeRect = new Rect(pos.x, pos.y, activeElement.worldBound.width, activeElement.worldBound.height);
                    searchMode = HighlightSearchMode.None;
                    return true;
                }

                activeIsImgui = true;
            }

            // Try IMGUI
            s_View.RepaintImmediately();
            if (searchMode == HighlightSearchMode.None)
            {
                if (activeIsImgui && !useUIToolkitScrolling)
                    return true; // Success - control was found in window that doesn't use UIToolkit ScrollView

                if (activeElement != null)
                    return true; // Already found the IMGUIContainer

                var windowPos = s_ViewWindow.position.position;
                var r = activeRect;
                r.x -= windowPos.x;
                r.y -= windowPos.y;

                activeElement = SearchIMGUIContainer(s_ViewWindow.rootVisualElement, r.center);
                useUIToolkitScrolling = activeScrollView != null;
                return true;
            }

            s_SearchMode = HighlightSearchMode.None;
            Stop();
            return false;
        }

        private static void ControlHighlightGUI()
        {
            if (!activeVisible || searching)
                return;

            if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "HandleControlHighlight")
            {
                if (s_View.screenPosition.Overlaps(s_RepaintRegion))
                    s_View.Repaint();
                return;
            }

            if (Event.current.type != EventType.Repaint)
                return;

            Rect localRect = GUIUtility.ScreenToGUIRect(activeRect);
            localRect = highlightStyle.padding.Add(localRect);

            // Animation calculations

            // Value pulses between 0 and 1
            float pulseValue = (Mathf.Cos(s_HighlightElapsedTime * Mathf.PI * 2 * kPulseSpeed) + 1) * 0.5f;

            // Value expands from 0.01 to 1 and stays at 1
            float popupScale = Mathf.Min(1, 0.01f + s_HighlightElapsedTime / kPopupDuration);
            // Value goes from 0.01 towards 1, overshoots a bit, and ends at 1.
            popupScale = popupScale + Mathf.Sin(popupScale * Mathf.PI) * 0.5f;

            // Scale calculations
            Vector2 pulseScaleAddition = new Vector2(
                (localRect.width + kExpansionMovementSize) / localRect.width - 1.0f,
                (localRect.height + kExpansionMovementSize) / localRect.height - 1.0f
                ) * pulseValue;
            Vector2 scale = (Vector2.one + pulseScaleAddition) * popupScale;

            // Cache old values
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;

            // Set pulsing values
            GUI.color = new Color(1, 1, 1, Mathf.Clamp01(0.8f * popupScale - 0.3f * pulseValue));
            GUIUtility.ScaleAroundPivot(scale, localRect.center);

            // Draw highlight
            highlightStyle.Draw(localRect, false, false, false, false);

            // Reset to old values
            GUI.color = oldColor;
            GUI.matrix = oldMatrix;
        }

        private static void HandleScroll()
        {
            if (activeScrollView == null || s_ViewWindow == null)
                return;

            // activeRect is in screen space convert back to window space
            var windowPos = s_ViewWindow.position.position;
            var r = activeRect;
            r.x -= windowPos.x;
            r.y -= windowPos.y;

            if (!activeVisible)
            {
                var paddedRect = new Rect(r.x - padding, r.y - padding,
                    r.width + padding * 2, r.height + padding * 2);

                activeVisible = !ScrollTowardsRect(activeScrollView, paddedRect, kUIToolkitScrollSpeed);
            }
            else
            {
                // If the highlight rect (without padding) was already visible but is no longer, stop the highlight.
                if (ScrollTowardsRect(activeScrollView, r, 0f))
                    activeRect = Rect.zero;
            }
        }

        private static bool ScrollTowardsRect(ScrollView sv, Rect pos, float maxDelta)
        {
            var scrollVector = CalculateScrollVector(sv, pos);

            // If we don't need scrolling, return false
            if (scrollVector.sqrMagnitude < 0.0001f)
                return false;

            // If we need scrolling but don't actually allow any, just return true to
            // indicate scrolling is needed to be able to see pos
            if (Mathf.Approximately(maxDelta, 0))
                return true;

            if (scrollVector.magnitude > maxDelta)
                scrollVector = scrollVector.normalized * maxDelta;

            sv.scrollOffset += scrollVector;
            return true;
        }

        private static Vector2 CalculateScrollVector(ScrollView sv, Rect pos)
        {
            var scrollVector = Vector2.zero;
            var viewport = sv.contentViewport.worldBound;

            // If the rect we want to see is larger than the visible rect, then trim it,
            // otherwise we can get oscillation or other unwanted behavior
            float excess = pos.width - viewport.width;
            if (excess > 0)
            {
                pos.width -= excess;
                pos.x += excess * 0.5f;
            }
            excess = pos.height - viewport.height;
            if (excess > 0)
            {
                pos.height -= excess;
                pos.y += excess * 0.5f;
            }

            // Calculate needed x scrolling
            if (sv.scrollableWidth > 0)
            {
                if (pos.xMax > viewport.xMax)
                    scrollVector.x += pos.xMax - viewport.xMax;
                else if (pos.xMin < viewport.xMin)
                    scrollVector.x -= viewport.xMin - pos.xMin;
            }

            // Calculate needed y scrolling
            if (sv.scrollableHeight > 0)
            {
                if (pos.yMax > viewport.yMax)
                    scrollVector.y += pos.yMax - viewport.yMax;
                else if (pos.yMin < viewport.yMin)
                    scrollVector.y -= viewport.yMin - pos.yMin;
            }

            return scrollVector;
        }

        private static bool SearchVisualElement(VisualElement ve)
        {
            ScrollView currentScrollView = null;
            for (var i = 0; i < ve.childCount; i++)
            {
                var child = ve[i];
                if (child.resolvedStyle.display == DisplayStyle.None)
                    continue;

                switch (child)
                {
                    case Foldout foldout when !foldout.value:
                        continue; // Do not search inside collapsed foldout
                    case IPrefixLabel prefixLabel when prefixLabel.label == activeText && (searchMode == HighlightSearchMode.Auto || searchMode == HighlightSearchMode.PrefixLabel):
                        activeElement = child;
                        return true;
                    case BindableElement bindableElement when bindableElement.bindingPath == activeText && IsSearchingForIdentifier():
                        activeElement = child;
                        return true;
                    case TextElement textElement when textElement.text == activeText && (searchMode == HighlightSearchMode.Auto || searchMode == HighlightSearchMode.Content):
                        activeElement = child;
                        return true;
                    case ScrollView sv when activeScrollView == null:
                        activeScrollView = currentScrollView = sv;
                        break;
                    case ScrollView sv:
                        // Skip searching inside nested ScrollView
                        continue;
                }

                if (SearchVisualElement(child))
                    return true;

                // Element not part of this ScrollView, make sure to reset the activeScrollView or else other
                // ScrollView will be considered as nested
                if (currentScrollView != null)
                    activeScrollView = null;
            }

            return false;
        }

        private static IMGUIContainer SearchIMGUIContainer(VisualElement root, Vector2 pos)
        {
            ScrollView currentScrollView = null;
            for (var i = 0; i < root.childCount; i++)
            {
                var child = root[i];
                switch (child)
                {
                    case IMGUIContainer imguiContainer when imguiContainer.worldBound.Contains(pos):
                        return imguiContainer;
                    case ScrollView sv when activeScrollView == null:
                        activeScrollView = currentScrollView = sv;
                        break;
                    case ScrollView sv:
                        // Skip searching inside nested ScrollView
                        continue;
                }

                var c = SearchIMGUIContainer(child, pos);
                if (c != null)
                    return c;

                // Element not part of this ScrollView, make sure to reset the activeScrollView or else other
                // ScrollView will be considered as nested
                if (currentScrollView != null)
                    activeScrollView = null;
            }
            return null;
        }

        internal static bool searching => searchMode != HighlightSearchMode.None;
        internal static bool isUIToolkitWindow => s_ViewWindow != null && s_ViewWindow.isUIToolkitWindow;
    }
}
