// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor
{
    internal class AnnotationWindow : EditorWindow
    {
        class Styles
        {
            public GUIStyle toggle = "OL Toggle";
            public GUIStyle listEvenBg = "ObjectPickerResultsOdd";//"ObjectPickerResultsEven";//
            public GUIStyle listOddBg = "ObjectPickerResultsEven";//"ObjectPickerResultsEven";//
            public GUIStyle background = "grey_border";
            public GUIStyle seperator = "sv_iconselector_sep";
            public GUIStyle iconDropDown = "IN dropdown";
            public GUIStyle listTextStyle;
            public GUIStyle listHeaderStyle;
            public GUIStyle texelWorldSizeStyle;
            public GUIStyle columnHeaderStyle;
            public Styles()
            {
                listTextStyle = new GUIStyle(EditorStyles.label);
                listTextStyle.alignment = TextAnchor.MiddleLeft;
                listTextStyle.padding.left = 10;

                listHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
                listHeaderStyle.padding.left = 5;

                texelWorldSizeStyle = new GUIStyle(EditorStyles.label);
                texelWorldSizeStyle.alignment = TextAnchor.UpperRight;
                texelWorldSizeStyle.font = EditorStyles.miniLabel.font;
                texelWorldSizeStyle.fontSize = EditorStyles.miniLabel.fontSize;
                texelWorldSizeStyle.padding.right = 0;

                columnHeaderStyle = new GUIStyle(EditorStyles.miniLabel);
            }
        }
        const float kWindowWidth = 270;
        const float scrollBarWidth = 14;
        const float listElementHeight = 18;
        const float gizmoRightAlign = 23;
        const float iconRightAlign = 64;
        const float frameWidth = 1f;

        static bool s_Debug = false;
        static AnnotationWindow s_AnnotationWindow = null;
        static long s_LastClosedTime;
        static Styles m_Styles;
        List<AInfo> m_RecentAnnotations;
        List<AInfo> m_BuiltinAnnotations;
        List<AInfo> m_ScriptAnnotations;
        Vector2 m_ScrollPosition;
        bool m_SyncWithState;
        string m_LastScriptThatHasShownTheIconSelector;
        List<MonoScript> m_MonoScriptIconsChanged;

        const int maxShowRecent = 5;
        const string textGizmoVisible = "Show/Hide Gizmo";
        GUIContent iconToggleContent = new GUIContent("", "Show/Hide Icon");
        GUIContent iconSelectContent = new GUIContent("", "Select Icon");

        GUIContent icon3dGizmoContent = new GUIContent("3D Icons");
        GUIContent showGridContent = new GUIContent("Show Grid");
        GUIContent showOutlineContent = new GUIContent("Selection Outline");
        GUIContent showWireframeContent = new GUIContent("Selection Wire");
        private bool m_IsGameView;

        const float exponentStart = -3.0f;
        const float exponentRange = 3.0f;
        const string kAlwaysFullSizeText = "Always Full Size";
        const string kHideAllIconsText = "Hide All Icons";

        static float ConvertTexelWorldSizeTo01(float texelWorldSize)
        {
            if (texelWorldSize == -1.0f)
                return 1.0f;
            if (texelWorldSize == 0.0f)
                return 0.0f;
            return (Mathf.Log10(texelWorldSize) - exponentStart) / exponentRange;
        }

        static float Convert01ToTexelWorldSize(float value01)
        {
            if (value01 <= 0.0f)
                return 0.0f; // always hidden
            return Mathf.Pow(10.0f, exponentStart + exponentRange * value01); // texel size is between 10e-2 (0.01) and 10e2 (100) worldunits. (texel in the icon texture)
        }

        static string ConvertTexelWorldSizeToString(float texelWorldSize)
        {
            if (texelWorldSize == -1.0f)
            {
                return kAlwaysFullSizeText;
            }
            if (texelWorldSize == 0.0f)
            {
                return kHideAllIconsText;
            }

            float displaySize = texelWorldSize * 32.0f; // The 32 is default icon size, so we show worldsize for 32 pixel icons.
            int numDecimals = MathUtils.GetNumberOfDecimalsForMinimumDifference(displaySize * 0.1f);
            return displaySize.ToString("N" + numDecimals);
        }

        public void MonoScriptIconChanged(MonoScript monoScript)
        {
            if (monoScript == null)
                return;

            bool add = true;
            foreach (MonoScript m in m_MonoScriptIconsChanged)
                if (m.GetInstanceID() == monoScript.GetInstanceID())
                    add = false;

            if (add)
                m_MonoScriptIconsChanged.Add(monoScript);
        }

        static public void IconChanged()
        {
            if (s_AnnotationWindow != null)
                s_AnnotationWindow.IconHasChanged();
        }

        float GetTopSectionHeight()
        {
            const int numberOfControls = 5;
            return EditorGUI.kSingleLineHeight * numberOfControls + EditorGUI.kControlVerticalSpacing * numberOfControls;
        }

        void OnEnable()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Close;
            hideFlags = HideFlags.DontSave;
        }

        void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= Close;
            // When window closes we copy all changes to monoimporters (reimport monoScripts)
            foreach (MonoScript monoScript in m_MonoScriptIconsChanged)
                MonoImporter.CopyMonoScriptIconToImporters(monoScript);

            s_LastClosedTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            s_AnnotationWindow = null;
        }

        internal static bool ShowAtPosition(Rect buttonRect, bool isGameView)
        {
            // We could not use realtimeSinceStartUp since it is set to 0 when entering/exitting playmode, we assume an increasing time when comparing time.
            long nowMilliSeconds = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            bool justClosed = nowMilliSeconds < s_LastClosedTime + 50;
            if (!justClosed)
            {
                Event.current.Use();
                if (s_AnnotationWindow == null)
                    s_AnnotationWindow = ScriptableObject.CreateInstance<AnnotationWindow>();
                s_AnnotationWindow.Init(buttonRect, isGameView);
                return true;
            }
            return false;
        }

        void Init(Rect buttonRect, bool isGameView)
        {
            // Has to be done before calling Show / ShowWithMode
            buttonRect = GUIUtility.GUIToScreenRect(buttonRect);

            m_MonoScriptIconsChanged = new List<MonoScript>();

            m_SyncWithState = true;

            m_IsGameView = isGameView;

            SyncToState();

            float windowHeight = 2 * frameWidth + GetTopSectionHeight() + DrawNormalList(false, 100, 0, 10000);
            windowHeight = Mathf.Min(windowHeight, 900);
            Vector2 windowSize = new Vector2(kWindowWidth, windowHeight);

            ShowAsDropDown(buttonRect, windowSize);
        }

        private void IconHasChanged()
        {
            if (string.IsNullOrEmpty(m_LastScriptThatHasShownTheIconSelector))
                return;

            foreach (AInfo t in m_ScriptAnnotations)
            {
                if (t.m_ScriptClass == m_LastScriptThatHasShownTheIconSelector)
                {
                    if (t.m_IconEnabled == false)
                    {
                        t.m_IconEnabled = true;
                        SetIconState(t);
                        break;
                    }
                }
            }

            Repaint();
        }

        void Cancel()
        {
            // Undo changes we have done.
            // PerformTemporaryUndoStack must be called before Close() below
            // to ensure that we properly undo changes before closing window.
            //Undo.PerformTemporaryUndoStack();

            Close();
            GUI.changed = true;
            GUIUtility.ExitGUI();
        }

        AInfo GetAInfo(int classID, string scriptClass)
        {
            if (scriptClass != "")
                return m_ScriptAnnotations.Find(delegate(AInfo o) { return o.m_ScriptClass == scriptClass; });

            return m_BuiltinAnnotations.Find(delegate(AInfo o) { return o.m_ClassID == classID; });
        }

        private void SyncToState()
        {
            // Sync annotations
            Annotation[] a = AnnotationUtility.GetAnnotations();

            string debuginfo = "";
            if (s_Debug)
                debuginfo += "AnnotationWindow: SyncToState\n";

            m_BuiltinAnnotations = new List<AInfo>();
            m_ScriptAnnotations = new List<AInfo>();
            for (int i = 0; i < a.Length; ++i)
            {
                if (s_Debug)
                    debuginfo += "   same as below: icon " + a[i].iconEnabled + " gizmo " + a[i].gizmoEnabled + "\n";

                bool ge = (a[i].gizmoEnabled == 1) ? true : false;
                bool ie = (a[i].iconEnabled == 1) ? true : false;
                AInfo anno = new AInfo(ge, ie, a[i].flags, a[i].classID, a[i].scriptClass);

                if (anno.m_ScriptClass == "")
                {
                    m_BuiltinAnnotations.Add(anno);
                    if (s_Debug)
                        debuginfo += "   " + UnityType.FindTypeByPersistentTypeID(anno.m_ClassID).name + ": icon " + anno.m_IconEnabled + " gizmo " + anno.m_GizmoEnabled + "\n";
                }
                else
                {
                    m_ScriptAnnotations.Add(anno);
                    if (s_Debug)
                        debuginfo += "   " + a[i].scriptClass + ": icon " + anno.m_IconEnabled + " gizmo " + anno.m_GizmoEnabled + "\n";
                }
            }

            m_BuiltinAnnotations.Sort();
            m_ScriptAnnotations.Sort();

            // Sync recently changed annotations
            m_RecentAnnotations = new List<AInfo>();
            Annotation[] recent = AnnotationUtility.GetRecentlyChangedAnnotations();
            for (int i = 0; i < recent.Length && i < maxShowRecent; ++i)
            {
                // Note: ainfo can be null if script has been renamed.
                AInfo ainfo = GetAInfo(recent[i].classID, recent[i].scriptClass);
                if (ainfo != null)
                    m_RecentAnnotations.Add(ainfo);
            }

            m_SyncWithState = false;

            if (s_Debug)
                Debug.Log(debuginfo);
        }

        internal void OnGUI()
        {
            if (m_Styles == null)
                m_Styles = new Styles();

            if (m_SyncWithState)
                SyncToState();

            // Content
            float topSectionHeight = GetTopSectionHeight();
            DrawTopSection(topSectionHeight);
            DrawAnnotationList(topSectionHeight, position.height - topSectionHeight);

            // Background with 1 pixel border
            if (Event.current.type == EventType.Repaint)
                m_Styles.background.Draw(new Rect(0, 0, position.width, position.height), GUIContent.none, false, false, false, false);

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                Cancel();
        }

        // ------------------------------------
        // TOP SECTION
        // ------------------------------------
        void DrawTopSection(float topSectionHeight)
        {
            // Bg
            GUI.Label(new Rect(frameWidth, 0, position.width - 2 * frameWidth, topSectionHeight), "", EditorStyles.inspectorBig);

            float topmargin = 7;
            float margin = 11;
            float curY = topmargin;
            float labelWidth = 120;
            //Extra spacing looks good here
            float rowHeight = EditorGUI.kSingleLineHeight + 2 * EditorGUI.kControlVerticalSpacing;

            // Toggle 3D gizmos
            Rect toggleRect = new Rect(margin, curY, labelWidth, rowHeight);
            AnnotationUtility.use3dGizmos = GUI.Toggle(toggleRect, AnnotationUtility.use3dGizmos, icon3dGizmoContent);

            // Texel world size
            float texelWorldSize = AnnotationUtility.iconSize;
            if (s_Debug)
            {
                Rect texelSizeRect = new Rect(0, curY + 10, position.width - margin, rowHeight);
                GUI.Label(texelSizeRect, ConvertTexelWorldSizeToString(texelWorldSize), m_Styles.texelWorldSizeStyle);
            }

            using (new EditorGUI.DisabledScope(!AnnotationUtility.use3dGizmos))
            {
                // Slider
                float sliderWidth = position.width - margin - labelWidth;
                float iconSize01 = ConvertTexelWorldSizeTo01(texelWorldSize);
                Rect sliderRect = new Rect(labelWidth + margin, curY, sliderWidth - margin, rowHeight);
                iconSize01 = GUI.HorizontalSlider(sliderRect, iconSize01, 0.0f, 1.0f);
                if (GUI.changed)
                {
                    AnnotationUtility.iconSize = Convert01ToTexelWorldSize(iconSize01);
                    SceneView.RepaintAll();
                }
            }

            curY += rowHeight;

            // Show Grid
            using (new EditorGUI.DisabledScope(m_IsGameView))
            {
                toggleRect = new Rect(margin, curY, labelWidth, rowHeight);
                AnnotationUtility.showGrid = GUI.Toggle(toggleRect, AnnotationUtility.showGrid, showGridContent);

                toggleRect.y += rowHeight;
                AnnotationUtility.showSelectionOutline = GUI.Toggle(toggleRect, AnnotationUtility.showSelectionOutline, showOutlineContent);

                toggleRect.y += rowHeight;
                AnnotationUtility.showSelectionWire = GUI.Toggle(toggleRect, AnnotationUtility.showSelectionWire, showWireframeContent);
            }
        }

        // ------------------------------------
        // ANNOTATION SECTION
        // ------------------------------------

        // Returns height used
        void DrawAnnotationList(float startY, float height)
        {
            // Calc sizes
            const float barHeight = 1;
            Rect scrollViewRect = new Rect(frameWidth,
                    startY + barHeight,
                    position.width - 2 * frameWidth,
                    height - barHeight - frameWidth);
            float totalContentHeight = DrawNormalList(false, 0, 0, 100000);
            Rect contentRect = new Rect(0, 0, 1, totalContentHeight);
            bool isScrollbarVisible = totalContentHeight > scrollViewRect.height;
            float listElementWidth = scrollViewRect.width;
            if (isScrollbarVisible)
                listElementWidth -= scrollBarWidth;

            // Scrollview
            m_ScrollPosition = GUI.BeginScrollView(scrollViewRect, m_ScrollPosition, contentRect);
            {
                DrawNormalList(true, listElementWidth, m_ScrollPosition.y - listElementHeight, m_ScrollPosition.y + totalContentHeight);
            }
            GUI.EndScrollView();
        }

        void Flip(ref bool even)
        {
            even = !even;
        }

        float DrawNormalList(bool doDraw, float listElementWidth, float startY, float endY)
        {
            bool even = true;
            float curY = 0;
            bool headerDrawn = false;

            curY = DrawListSection(curY, "Recently Changed",    m_RecentAnnotations,    doDraw, listElementWidth, startY, endY, ref even, true, ref headerDrawn);
            curY = DrawListSection(curY, "Scripts",             m_ScriptAnnotations,    doDraw, listElementWidth, startY, endY, ref even, false, ref headerDrawn);
            curY = DrawListSection(curY, "Built-in Components", m_BuiltinAnnotations,   doDraw, listElementWidth, startY, endY, ref even, false, ref headerDrawn);

            return curY;
        }

        float DrawListSection(float y, string sectionHeader, List<AInfo> listElements, bool doDraw, float listElementWidth, float startY, float endY, ref bool even, bool useSeperator, ref bool headerDrawn)
        {
            float curY = y;
            const float extraHeader = 10;
            const float headerHeight = 20;

            if (listElements.Count > 0)
            {
                if (doDraw)
                {
                    // Header background
                    Rect rect = new Rect(1, curY, listElementWidth - 2, extraHeader + headerHeight);
                    Flip(ref even);
                    GUIStyle backgroundStyle = even ? m_Styles.listEvenBg : m_Styles.listOddBg; // m_Styles.listSectionHeaderBg;//
                    GUI.Label(rect, GUIContent.Temp(""), backgroundStyle);
                }
                curY += extraHeader;
                if (doDraw)
                {
                    // Header text
                    DrawListHeader(sectionHeader, new Rect(3, curY, listElementWidth, headerHeight), ref headerDrawn);
                }
                curY += headerHeight;

                // List elements
                for (int i = 0; i < listElements.Count; ++i)
                {
                    Flip(ref even);
                    if (curY > startY && curY < endY)
                    {
                        Rect rect = new Rect(1, curY, listElementWidth - 2, listElementHeight);
                        if (doDraw)
                            DrawListElement(rect, even, listElements[i]);
                    }
                    curY += listElementHeight;
                }

                if (useSeperator)
                {
                    float height = 6;
                    if (doDraw)
                    {
                        GUIStyle backgroundStyle = even ? m_Styles.listEvenBg : m_Styles.listOddBg;
                        GUI.Label(new Rect(1, curY, listElementWidth - 2, height), GUIContent.Temp(""), backgroundStyle);
                        GUI.Label(new Rect(10, curY + 3, listElementWidth - 15, 3), GUIContent.Temp(""), m_Styles.seperator);
                    }
                    curY += height;
                }
            }

            return curY;
        }

        void DrawListHeader(string header, Rect rect, ref bool headerDrawn)
        {
            GUI.Label(rect, GUIContent.Temp(header), m_Styles.listHeaderStyle);


            if (headerDrawn == false)
            {
                headerDrawn = true;
                GUI.color = new Color(1, 1, 1, 0.65f);

                //  Column headers
                Rect columnRect = rect;
                columnRect.y += -10;
                columnRect.x = rect.width - 32;
                GUI.Label(columnRect, "gizmo", m_Styles.columnHeaderStyle);

                columnRect.x = rect.width - iconRightAlign;
                GUI.Label(columnRect, "icon", m_Styles.columnHeaderStyle);

                GUI.color = Color.white;
            }
        }

        void DrawListElement(Rect rect, bool even, AInfo ainfo)
        {
            if (ainfo == null)
            {
                Debug.LogError("DrawListElement: AInfo not valid!");
                return;
            }

            string tooltip;
            float togglerSize = 17;
            float disabledAlpha = 0.3f;

            // We maintain our own gui.changed
            bool orgGUIChanged = GUI.changed;
            bool orgGUIEnabled = GUI.enabled;
            Color orgColor = GUI.color;
            GUI.changed = false;
            GUI.enabled = true;

            // Bg
            GUIStyle backgroundStyle = even ? m_Styles.listEvenBg : m_Styles.listOddBg;
            GUI.Label(rect, GUIContent.Temp(""), backgroundStyle);


            // Text
            Rect textRect = rect;
            //textRect.x += 22;
            textRect.width = rect.width - iconRightAlign - 22; // ensure text doesnt flow behind toggles
            GUI.Label(textRect, ainfo.m_DisplayText, m_Styles.listTextStyle);


            // Icon toggle
            float iconSize = 16;
            Rect iconRect = new Rect(rect.width - iconRightAlign, rect.y + (rect.height - iconSize) * 0.5f, iconSize, iconSize);
            Texture thumb = null;
            if (ainfo.m_ScriptClass != "")
            {
                // Icon for scripts
                thumb = EditorGUIUtility.GetIconForObject(EditorGUIUtility.GetScript(ainfo.m_ScriptClass));

                Rect div = iconRect;
                div.x += 18;
                div.y += 1;
                div.width = 1;
                div.height = 12;

                if (!EditorGUIUtility.isProSkin)
                    GUI.color = new Color(0, 0, 0, 0.33f);
                else
                    GUI.color = new Color(1, 1, 1, 0.13f);

                GUI.DrawTexture(div, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill);
                GUI.color = Color.white;

                Rect arrowRect = iconRect;
                arrowRect.x += 18;
                arrowRect.y += 0;
                arrowRect.width = 9;

                if (GUI.Button(arrowRect, iconSelectContent, m_Styles.iconDropDown))
                {
                    Object script = EditorGUIUtility.GetScript(ainfo.m_ScriptClass);
                    if (script != null)
                    {
                        m_LastScriptThatHasShownTheIconSelector = ainfo.m_ScriptClass;
                        if (IconSelector.ShowAtPosition(script, arrowRect, true))
                        {
                            IconSelector.SetMonoScriptIconChangedCallback(MonoScriptIconChanged);
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }
            else
            {
                // Icon for builtin components
                if (ainfo.HasIcon())
                    thumb = AssetPreview.GetMiniTypeThumbnailFromClassID(ainfo.m_ClassID);
            }

            if (thumb != null)
            {
                if (!ainfo.m_IconEnabled)
                {
                    GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, disabledAlpha);
                    tooltip = "";
                }

                iconToggleContent.image = thumb;
                if (GUI.Button(iconRect, iconToggleContent, GUIStyle.none))
                {
                    ainfo.m_IconEnabled = !ainfo.m_IconEnabled;
                    SetIconState(ainfo);
                }

                GUI.color = orgColor;
            }

            if (GUI.changed)
            {
                SetIconState(ainfo);
                GUI.changed = false;
            }

            GUI.enabled = true;
            GUI.color = orgColor;

            // Gizmo toggle
            if (ainfo.HasGizmo())
            {
                tooltip = textGizmoVisible;

                Rect togglerRect = new Rect(rect.width - gizmoRightAlign, rect.y + (rect.height - togglerSize) * 0.5f, togglerSize, togglerSize);
                ainfo.m_GizmoEnabled = GUI.Toggle(togglerRect, ainfo.m_GizmoEnabled, new GUIContent("", tooltip), m_Styles.toggle);
                if (GUI.changed)
                {
                    SetGizmoState(ainfo);
                }
            }

            GUI.enabled = orgGUIEnabled;
            GUI.changed = orgGUIChanged;
            GUI.color = orgColor;
        }

        void SetIconState(AInfo ainfo)
        {
            AnnotationUtility.SetIconEnabled(ainfo.m_ClassID, ainfo.m_ScriptClass, ainfo.m_IconEnabled ? 1 : 0);
            SceneView.RepaintAll();
        }

        void SetGizmoState(AInfo ainfo)
        {
            AnnotationUtility.SetGizmoEnabled(ainfo.m_ClassID, ainfo.m_ScriptClass, ainfo.m_GizmoEnabled ? 1 : 0);
            SceneView.RepaintAll();
        }
    }

    internal class AInfo : System.IComparable, System.IEquatable<AInfo>
    {
        // Similar values as in Annotation (in AnnotationManager.h)
        public enum Flags { kHasIcon = 1, kHasGizmo = 2 };

        public AInfo(bool gizmoEnabled, bool iconEnabled, int flags, int classID, string scriptClass)
        {
            m_GizmoEnabled = gizmoEnabled;
            m_IconEnabled = iconEnabled;
            m_ClassID = classID;
            m_ScriptClass = scriptClass;
            m_Flags = flags;
            if (m_ScriptClass == "")
                m_DisplayText = UnityType.FindTypeByPersistentTypeID(m_ClassID).name;
            else
                m_DisplayText = m_ScriptClass;
        }

        public bool m_IconEnabled;
        public bool m_GizmoEnabled;
        public int m_ClassID;
        public string m_ScriptClass;
        public string m_DisplayText;
        public int m_Flags;

        bool IsBitSet(byte b, int pos)
        {
            return (b & (1 << pos)) != 0;
        }

        public bool HasGizmo()
        {
            return (m_Flags & (int)Flags.kHasGizmo) > 0;
        }

        public bool HasIcon()
        {
            return (m_Flags & (int)Flags.kHasIcon) > 0;
        }

        public int CompareTo(object obj)
        {
            AInfo other = obj as AInfo;
            if (other != null)
                return this.m_DisplayText.CompareTo(other.m_DisplayText);
            else
                throw new System.ArgumentException("Object is not an AInfo");
        }

        public bool Equals(AInfo other)
        {
            return (m_ClassID == other.m_ClassID && m_ScriptClass == other.m_ScriptClass);
        }
    }
}
