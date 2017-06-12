// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;

namespace UnityEditor
{
    internal class LayerVisibilityWindow : EditorWindow
    {
        private class Styles
        {
            public readonly GUIStyle background = "grey_border";
            public readonly GUIStyle menuItem = "MenuItem";
            public readonly GUIStyle listEvenBg = "ObjectPickerResultsOdd";
            public readonly GUIStyle listOddBg = "ObjectPickerResultsEven";
            public readonly GUIStyle separator = "sv_iconselector_sep";
            public readonly GUIStyle lockButton = "IN LockButton";
            public readonly GUIStyle listTextStyle;
            public readonly GUIStyle listHeaderStyle;
            public readonly Texture2D visibleOn  = EditorGUIUtility.LoadIcon("animationvisibilitytoggleon");
            public readonly Texture2D visibleOff = EditorGUIUtility.LoadIcon("animationvisibilitytoggleoff");
            public Styles()
            {
                listTextStyle = new GUIStyle(EditorStyles.label);
                listTextStyle.alignment = TextAnchor.MiddleLeft;
                listTextStyle.padding.left = 10;

                listHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
                listHeaderStyle.padding.left = 5;
            }
        }

        const float kScrollBarWidth = 14;
        const float kFrameWidth = 1f;
        const float kToggleSize = 17;
        const float kSeparatorHeight = 6;
        const string kLayerVisible = "Show/Hide Layer";
        const string kLayerLocked = "Lock Layer for Picking";

        private static LayerVisibilityWindow s_LayerVisibilityWindow;
        private static long s_LastClosedTime;
        private static Styles s_Styles;

        private List<string> s_LayerNames;
        private List<int> s_LayerMasks;
        private int m_AllLayersMask;

        private float m_ContentHeight;
        private Vector2 m_ScrollPosition;

        private void CalcValidLayers()
        {
            s_LayerNames = new List<string>();
            s_LayerMasks = new List<int>();
            m_AllLayersMask = 0;

            for (var i = 0; i < 32; i++)
            {
                var s = InternalEditorUtility.GetLayerName(i);
                if (s == string.Empty)
                    continue;
                s_LayerNames.Add(string.Format("{0}: {1}", i, s));
                s_LayerMasks.Add(i);
                m_AllLayersMask |= (1 << i);
            }
        }

        internal void OnEnable()
        {
            hideFlags = HideFlags.DontSave;
            wantsMouseMove = true;
        }

        internal void OnDisable()
        {
            s_LastClosedTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            s_LayerVisibilityWindow = null;
        }

        internal static bool ShowAtPosition(Rect buttonRect)
        {
            // We could not use realtimeSinceStartUp since it is set to 0 when entering/exitting playmode, we assume an increasing time when comparing time.
            long nowMilliSeconds = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            bool justClosed = nowMilliSeconds < s_LastClosedTime + 50;
            if (!justClosed)
            {
                Event.current.Use();
                if (s_LayerVisibilityWindow == null)
                    s_LayerVisibilityWindow = CreateInstance<LayerVisibilityWindow>();
                s_LayerVisibilityWindow.Init(buttonRect);
                return true;
            }
            return false;
        }

        private void Init(Rect buttonRect)
        {
            // Has to be done before calling Show / ShowWithMode
            buttonRect = GUIUtility.GUIToScreenRect(buttonRect);

            CalcValidLayers();

            var rowCount = (s_LayerNames.Count + 2 + 1 + 1);

            var windowHeight = rowCount * EditorGUI.kSingleLineHeight + kSeparatorHeight;

            int sortingLayerCount = InternalEditorUtility.GetSortingLayerCount();
            if (sortingLayerCount > 1)
            {
                windowHeight += kSeparatorHeight + EditorGUI.kSingleLineHeight;
                windowHeight += sortingLayerCount * EditorGUI.kSingleLineHeight;
            }
            m_ContentHeight = windowHeight;
            windowHeight += 2 * kFrameWidth;
            windowHeight = Mathf.Min(windowHeight, 600);

            var windowSize = new Vector2(180, windowHeight);
            ShowAsDropDown(buttonRect, windowSize);
        }

        internal void OnGUI()
        {
            // We do not use the layout event
            if (Event.current.type == EventType.Layout)
                return;

            if (s_Styles == null)
                s_Styles = new Styles();

            var scrollViewRect = new Rect(kFrameWidth, kFrameWidth, position.width - 2 * kFrameWidth, position.height - 2 * kFrameWidth);
            var contentRect = new Rect(0, 0, 1, m_ContentHeight);
            bool isScrollbarVisible = m_ContentHeight > scrollViewRect.height;
            float listElementWidth = scrollViewRect.width;
            if (isScrollbarVisible)
                listElementWidth -= kScrollBarWidth;

            m_ScrollPosition = GUI.BeginScrollView(scrollViewRect, m_ScrollPosition, contentRect);
            Draw(listElementWidth);
            GUI.EndScrollView();

            // Background with 1 pixel border
            GUI.Label(new Rect(0, 0, position.width, position.height), GUIContent.none, s_Styles.background);

            // Use mouse move so we get hover state correctly in the menu item rows
            if (Event.current.type == EventType.MouseMove)
                Event.current.Use();

            // Escape closes the window
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Close();
                GUIUtility.ExitGUI();
            }
        }

        private void DrawListBackground(Rect rect, bool even)
        {
            GUIStyle backgroundStyle = even ? s_Styles.listEvenBg : s_Styles.listOddBg;
            GUI.Label(rect, GUIContent.none, backgroundStyle);
        }

        private void DrawHeader(ref Rect rect, string text, ref bool even)
        {
            DrawListBackground(rect, even);
            GUI.Label(rect, GUIContent.Temp(text), s_Styles.listHeaderStyle);
            rect.y += EditorGUI.kSingleLineHeight;
            even = !even;
        }

        private void DrawSeparator(ref Rect rect, bool even)
        {
            DrawListBackground(new Rect(rect.x + 1, rect.y, rect.width - 2, kSeparatorHeight), even);
            GUI.Label(new Rect(rect.x + 5, rect.y + 3, rect.width - 10, 3), GUIContent.none, s_Styles.separator);
            rect.y += kSeparatorHeight;
        }

        private void Draw(float listElementWidth)
        {
            var drawPos = new Rect(0, 0, listElementWidth, EditorGUI.kSingleLineHeight);

            bool even = false;

            DrawHeader(ref drawPos, "Layers", ref even);

            // Everything & nothing entries
            DoSpecialLayer(drawPos, true, ref even);
            drawPos.y += EditorGUI.kSingleLineHeight;
            DoSpecialLayer(drawPos, false, ref even);
            drawPos.y += EditorGUI.kSingleLineHeight;

            // Layers
            for (var i = 0; i < s_LayerNames.Count; ++i)
            {
                DoOneLayer(drawPos, i, ref even);
                drawPos.y += EditorGUI.kSingleLineHeight;
            }

            // Sorting layers, if anything else than the single default one is present
            int sortingLayerCount = InternalEditorUtility.GetSortingLayerCount();
            if (sortingLayerCount > 1)
            {
                DrawSeparator(ref drawPos, even);
                DrawHeader(ref drawPos, "Sorting Layers", ref even);
                for (var i = 0; i < sortingLayerCount; ++i)
                {
                    DoOneSortingLayer(drawPos, i, ref even);
                    drawPos.y += EditorGUI.kSingleLineHeight;
                }
            }

            // Edit Layers entry
            DrawSeparator(ref drawPos, even);
            DrawListBackground(drawPos, even);
            if (GUI.Button(drawPos, EditorGUIUtility.TempContent("Edit Layers..."), s_Styles.menuItem))
            {
                Close();
                Selection.activeObject = EditorApplication.tagManager;
                GUIUtility.ExitGUI();
            }
        }

        void DoSpecialLayer(Rect rect, bool all, ref bool even)
        {
            int visibleMask = Tools.visibleLayers;
            int expectedMask = all ? m_AllLayersMask : 0;
            bool visible = (visibleMask & m_AllLayersMask) == expectedMask;
            bool visibleChanged, lockedChanged;
            DoLayerEntry(rect, all ? "Everything" : "Nothing", even, true, false, visible, false, out visibleChanged, out lockedChanged);
            if (visibleChanged)
            {
                Tools.visibleLayers = all ? ~0 : 0;
                RepaintAllSceneViews();
            }
            even = !even;
        }

        void DoOneLayer(Rect rect, int index, ref bool even)
        {
            int visibleMask = Tools.visibleLayers;
            int lockedMask = Tools.lockedLayers;
            int layerMask = 1 << (s_LayerMasks[index]);
            bool visible = (visibleMask & layerMask) != 0;
            bool locked = (lockedMask & layerMask) != 0;
            bool visibleChanged, lockedChanged;
            DoLayerEntry(rect, s_LayerNames[index], even, true, true, visible, locked, out visibleChanged, out lockedChanged);
            if (visibleChanged)
            {
                Tools.visibleLayers ^= layerMask;
                RepaintAllSceneViews();
            }
            if (lockedChanged)
            {
                Tools.lockedLayers ^= layerMask;
            }
            even = !even;
        }

        void DoOneSortingLayer(Rect rect, int index, ref bool even)
        {
            bool locked = InternalEditorUtility.GetSortingLayerLocked(index);
            bool visibleChanged, lockedChanged;
            DoLayerEntry(rect, InternalEditorUtility.GetSortingLayerName(index), even, false, true, true, locked, out visibleChanged, out lockedChanged);
            if (lockedChanged)
            {
                InternalEditorUtility.SetSortingLayerLocked(index, !locked);
            }
            even = !even;
        }

        private void DoLayerEntry(Rect rect, string layerName, bool even, bool showVisible, bool showLock, bool visible, bool locked, out bool visibleChanged, out bool lockedChanged)
        {
            DrawListBackground(rect, even);

            EditorGUI.BeginChangeCheck();
            // text (works as visibility toggle as well)
            Rect textRect = rect;
            textRect.width -= kToggleSize * 2;
            visible = GUI.Toggle(textRect, visible, EditorGUIUtility.TempContent(layerName), s_Styles.listTextStyle);

            // visible checkbox
            var toggleRect = new Rect(rect.width - kToggleSize * 2, rect.y + (rect.height - kToggleSize) * 0.5f, kToggleSize, kToggleSize);
            visibleChanged = false;
            if (showVisible)
            {
                var oldColor = GUI.color;
                var newColor = oldColor;
                newColor.a = visible ? 0.6f : 0.4f;
                GUI.color = newColor;
                var iconRect = toggleRect;
                iconRect.y += 3;
                var gc = new GUIContent(string.Empty, visible ? s_Styles.visibleOn : s_Styles.visibleOff, kLayerVisible);
                GUI.Toggle(iconRect, visible, gc, GUIStyle.none);
                GUI.color = oldColor;
                visibleChanged = EditorGUI.EndChangeCheck();
            }

            // locked checkbox
            lockedChanged = false;
            if (showLock)
            {
                toggleRect.x += kToggleSize;
                EditorGUI.BeginChangeCheck();
                var oldColor = GUI.backgroundColor;
                var newColor = oldColor;
                if (!locked)
                    newColor.a *= 0.4f;
                GUI.backgroundColor = newColor;
                GUI.Toggle(toggleRect, locked, new GUIContent(string.Empty, kLayerLocked), s_Styles.lockButton);
                GUI.backgroundColor = oldColor;
                lockedChanged = EditorGUI.EndChangeCheck();
            }
        }

        static void RepaintAllSceneViews()
        {
            foreach (SceneView sv in Resources.FindObjectsOfTypeAll(typeof(SceneView)))
                sv.Repaint();
        }
    }
}
