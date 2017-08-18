// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;


namespace UnityEditor
{
    public partial class Highlighter
    {
        private static GUIView s_View;
        private static HighlightSearchMode s_SearchMode;
        private static float s_HighlightElapsedTime = 0;
        private static float s_LastTime = 0;
        private static Rect s_RepaintRegion;

        private const float kPulseSpeed = 0.45f; // Pulses per second
        private const float kPopupDuration = 0.33f; // How long time in seconds the popup takes
        private const int kExpansionMovementSize = 5; // How many pixels the rect expands out in the pulsing movement

        private static bool s_RecursionLock = false;

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
            activeRect = new Rect();

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

        public static bool activeVisible
        {
            get { return internal_get_activeVisible(); }
            private set { internal_set_activeVisible(value); }
        }

        public static string activeText
        {
            get { return internal_get_activeText(); }
            private set { internal_set_activeText(value); }
        }

        public static Rect activeRect
        {
            get { return internal_get_activeRect(); }
            private set { internal_set_activeRect(value); }
        }

        public static void HighlightIdentifier(Rect position, string identifier)
        {
            if (searchMode == HighlightSearchMode.Identifier ||
                searchMode == HighlightSearchMode.Auto)
            {
                Handle(position, identifier);
            }
        }

        // Private API

        private static void Update()
        {
            Rect prevRect = activeRect;

            if (activeRect.width == 0 || s_View == null)
            {
                EditorApplication.update -= Update;
                Stop();
                InternalEditorUtility.RepaintAllViews();
                return;
            }
            else
            {
                Search();
            }

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
            foreach (GUIView currentView in views)
            {
                if (currentView is HostView)
                {
                    if ((currentView as HostView).actualView.titleContent.text == windowTitle)
                    {
                        view = currentView;
                        break;
                    }
                }
                else if (currentView.window && currentView.GetType().Name == windowTitle)
                {
                    view = currentView;
                    break;
                }
            }

            s_View = view;
            return (view != null);
        }

        private static bool Search()
        {
            searchMode = s_SearchMode;
            s_View.RepaintImmediately();
            if (searchMode == HighlightSearchMode.None)
                return true; // Success - control was found

            s_SearchMode = HighlightSearchMode.None;
            Stop();
            return false;
        }

        internal static void ControlHighlightGUI(GUIView self)
        {
            // Only handle highlight in views in the same window as the view with the highlight is in.
            if (s_View == null || self.window != s_View.window)
                return;

            if (!activeVisible || searching)
                return;

            if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "HandleControlHighlight")
            {
                if (self.screenPosition.Overlaps(s_RepaintRegion))
                    self.Repaint();
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
    }
}
