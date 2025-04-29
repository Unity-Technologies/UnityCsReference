// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using UnityEditor.ShortcutManagement;

namespace UnityEditor
{
    internal class AnnotationWindow : EditorWindow
    {
        private static SavedBool s_ShowTerrainDebugWarnings = new SavedBool("Terrain.ShowDebugWarnings", true);

        public static bool ShowTerrainDebugWarnings
        {
            get => s_ShowTerrainDebugWarnings.value;
            set => s_ShowTerrainDebugWarnings.value = value;
        }

        class Styles
        {
            public GUIStyle toggle = "Toggle";
            public GUIStyle toggleMixed = EditorStyles.toggleMixed;
            public GUIStyle listEvenBg = "ObjectPickerResultsOdd";//"ObjectPickerResultsEven";//
            public GUIStyle listOddBg = "ObjectPickerResultsEven";//"ObjectPickerResultsEven";//
            public GUIStyle background = "grey_border";
            public GUIStyle seperator = "sv_iconselector_sep";
            public GUIStyle iconDropDown = "IN dropdown";
            public GUIStyle listTextStyle;
            public GUIStyle listHeaderStyle;
            public GUIStyle columnHeaderStyle;
            public const float k_ToggleSize = 17f;

            public Styles()
            {
                listTextStyle = new GUIStyle(EditorStyles.label);
                listTextStyle.alignment = TextAnchor.MiddleLeft;
                listTextStyle.padding.left = 10;

                listHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
                listHeaderStyle.padding.left = 5;

                columnHeaderStyle = EditorStyles.miniLabel;
            }
        }

        private enum EnabledState
        {
            NotSet = -1,
            None = 0,
            All = 1,
            Mixed = 2
        }

        const float k_WindowWidth = 270;
        const float k_ScrollBarWidth = 14;
        const float k_ListElementHeight = 18;
        const float k_FrameWidth = 1f;
        float iconSize = 16;
        float gizmoRightAlign;
        float gizmoTextRightAlign;
        float iconRightAlign;
        float iconTextRightAlign;

        static AnnotationWindow s_AnnotationWindow = null;
        static long s_LastClosedTime;
        const long k_JustClosedPeriod = 400;

        static Styles m_Styles;
        List<GizmoInfo> m_RecentAnnotations;
        List<GizmoInfo> m_BuiltinAnnotations;
        List<GizmoInfo> m_ScriptAnnotations;
        Vector2 m_ScrollPosition;
        bool m_SyncWithState;
        string m_LastScriptThatHasShownTheIconSelector;

        List<MonoScript> m_MonoScriptIconsChanged;

        const int maxShowRecent = 5;
        readonly string textGizmoVisible = L10n.Tr("Show/Hide Gizmo");
        GUIContent generalContent = EditorGUIUtility.TrTextContent("General");
        GUIContent iconToggleContent = EditorGUIUtility.TrTextContent("", "Show/Hide Icon");
        GUIContent iconSelectContent = EditorGUIUtility.TrTextContent("", "Select Icon");
        GUIContent icon3dGizmoContent = EditorGUIUtility.TrTextContent("3D Icons");
        GUIContent terrainDebugWarnings = EditorGUIUtility.TrTextContent("Terrain Debug Warnings");
        GUIContent showOutlineContent = EditorGUIUtility.TrTextContent("Selection Outline");
        GUIContent showWireframeContent = EditorGUIUtility.TrTextContent("Selection Wire");
        GUIContent fadeGizmosContent = EditorGUIUtility.TrTextContent("Fade Gizmos", "Fade out and stop rendering gizmos that are small on screen");
        GUIContent lightProbeVisualizationContent = EditorGUIUtility.TrTextContent("Light Probe Visualization");
        GUIContent displayWeightsContent = EditorGUIUtility.TrTextContent("Display Weights");
        GUIContent displayOcclusionContent = EditorGUIUtility.TrTextContent("Display Occlusion");
        GUIContent highlightInvalidCellsContent = EditorGUIUtility.TrTextContent("Highlight Invalid Cells", "Highlight the invalid cells that cannot be used for probe interpolation.");
        private bool m_IsGameView;

        string m_SearchFilter = string.Empty;

        const float exponentStart = -3.0f;
        const float exponentRange = 3.0f;

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
            const int numberOfGeneralControls = 6;

            int numberOfLightProbeVisualizationControls = 0;
            if (!UnityEngine.Rendering.SupportedRenderingFeatures.active.overridesLightProbeSystem)
            {
                if (LightProbeVisualization.lightProbeVisualizationMode == LightProbeVisualization.LightProbeVisualizationMode.None)
                    numberOfLightProbeVisualizationControls = 3;
                else
                    numberOfLightProbeVisualizationControls = 5;
            }

            int numberOfControls = numberOfGeneralControls + numberOfLightProbeVisualizationControls;
            return EditorGUI.kSingleLineHeight * numberOfControls + EditorGUI.kControlVerticalSpacing * (numberOfControls - 1) + EditorStyles.inspectorBig.padding.bottom;
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
                IconSelector.CopyIconToImporter(monoScript);

            s_LastClosedTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            s_AnnotationWindow = null;
        }

        internal static bool ShowAtPosition(Rect buttonRect, bool isGameView)
        {
            // We could not use realtimeSinceStartUp since it is set to 0 when entering/exitting playmode, we assume an increasing time when comparing time.
            long nowMilliSeconds = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            bool justClosed = nowMilliSeconds < s_LastClosedTime + k_JustClosedPeriod;
            if (!justClosed)
            {
                Event.current.Use();
                if (s_AnnotationWindow == null)
                    s_AnnotationWindow = ScriptableObject.CreateInstance<AnnotationWindow>();
                else
                {
                    // We are treating AnnotationWindow like a PopupWindow which has logic to reclose it when opened,
                    // AuxWindows derived from EditorWindow reset/reopen when repeatedly clicking the open button by design.
                    // Call Cancel() here if it is already open.
                    s_AnnotationWindow.Cancel();
                    return false;
                }

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

            float windowHeight = 2 * k_FrameWidth + GetTopSectionHeight() + DrawNormalList(false, 100, 0, 10000);
            windowHeight = Mathf.Min(windowHeight, 900);
            Vector2 windowSize = new Vector2(k_WindowWidth, windowHeight);

            ShowAsDropDown(buttonRect, windowSize);
        }

        private void IconHasChanged()
        {
            if (string.IsNullOrEmpty(m_LastScriptThatHasShownTheIconSelector))
                return;

            foreach (GizmoInfo t in m_ScriptAnnotations)
            {
                if (t.scriptClass == m_LastScriptThatHasShownTheIconSelector)
                {
                    if (t.iconEnabled == false)
                    {
                        t.iconEnabled = true;
                        SetIconState(t);
                        break;
                    }
                }
            }
            SyncToState();

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

        GizmoInfo GetAInfo(int classID, string scriptClass)
        {
            if (scriptClass != "")
                return m_ScriptAnnotations.Find(delegate(GizmoInfo o) { return o.scriptClass == scriptClass; });

            return m_BuiltinAnnotations.Find(delegate(GizmoInfo o) { return o.classID == classID; });
        }

        private void SyncToState()
        {
            // Sync annotations
            Annotation[] a = AnnotationUtility.GetAnnotations();

            m_BuiltinAnnotations = new List<GizmoInfo>();
            m_ScriptAnnotations = new List<GizmoInfo>();
            for (int i = 0; i < a.Length; ++i)
            {
                GizmoInfo anno = new GizmoInfo(a[i]);

                if(string.IsNullOrEmpty(anno.scriptClass))
                    m_BuiltinAnnotations.Add(anno);
                else
                    m_ScriptAnnotations.Add(anno);
            }

            m_BuiltinAnnotations.Sort();
            m_ScriptAnnotations.Sort();

            // Sync recently changed annotations
            m_RecentAnnotations = new List<GizmoInfo>();
            Annotation[] recent = AnnotationUtility.GetRecentlyChangedAnnotations();
            for (int i = 0; i < recent.Length && i < maxShowRecent; ++i)
            {
                // Note: ainfo can be null if script has been renamed.
                GizmoInfo ainfo = GetAInfo(recent[i].classID, recent[i].scriptClass);
                if (ainfo != null)
                    m_RecentAnnotations.Add(ainfo);
            }

            m_SyncWithState = false;
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
            GUI.Label(new Rect(k_FrameWidth, 0, position.width - 2 * k_FrameWidth, topSectionHeight), "", EditorStyles.inspectorBig);

            float topmargin = 7;
            float margin = 11;
            float curY = topmargin;

            float labelWidth = m_Styles.listHeaderStyle.CalcSize(terrainDebugWarnings).x + GUI.skin.toggle.CalcSize(GUIContent.none).x + 1;
            float rowHeight = 18;

            // General section
            Rect toggleRect = new Rect(margin, curY, labelWidth, rowHeight);
            GUI.Label(toggleRect, generalContent, EditorStyles.boldLabel);
            curY += rowHeight;

            // 3D icons toggle & slider
            toggleRect = new Rect(margin, curY, labelWidth, rowHeight);
            AnnotationUtility.use3dGizmos = GUI.Toggle(toggleRect, AnnotationUtility.use3dGizmos, icon3dGizmoContent);
            using (new EditorGUI.DisabledScope(!AnnotationUtility.use3dGizmos))
            {
                float texelWorldSize = AnnotationUtility.iconSize;
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

            // Gizmo fadeout toggle & slider
            toggleRect = new Rect(margin, curY, labelWidth, rowHeight);
            AnnotationUtility.fadeGizmos = GUI.Toggle(toggleRect, AnnotationUtility.fadeGizmos, fadeGizmosContent);
            using (new EditorGUI.DisabledScope(!AnnotationUtility.fadeGizmos))
            {
                float fadeSize = AnnotationUtility.fadeGizmoSize;
                float sliderWidth = position.width - margin - labelWidth;
                Rect sliderRect = new Rect(labelWidth + margin, curY, sliderWidth - margin, rowHeight);
                float newFadeSize = GUI.HorizontalSlider(sliderRect, fadeSize, 2.0f, 10.0f);
                if (fadeSize != newFadeSize)
                {
                    AnnotationUtility.fadeGizmoSize = newFadeSize;
                    SceneView.RepaintAll();
                }
            }
            curY += rowHeight;

            using (new EditorGUI.DisabledScope(m_IsGameView))
            {
                // Selection outline/wire
                toggleRect = new Rect(margin, curY, labelWidth, rowHeight);
                AnnotationUtility.showSelectionOutline = GUI.Toggle(toggleRect, AnnotationUtility.showSelectionOutline, showOutlineContent);
                curY += rowHeight;

                toggleRect = new Rect(margin, curY, labelWidth, rowHeight);
                AnnotationUtility.showSelectionWire = GUI.Toggle(toggleRect, AnnotationUtility.showSelectionWire, showWireframeContent);
                curY += rowHeight;

                // TODO: Change to Debug Errors & Debug Warnings
                toggleRect = new Rect(margin, curY, labelWidth, rowHeight);
                EditorGUI.BeginChangeCheck();
                s_ShowTerrainDebugWarnings.value = GUI.Toggle(toggleRect, s_ShowTerrainDebugWarnings.value, terrainDebugWarnings);
                if (EditorGUI.EndChangeCheck())
                    SceneView.RepaintAll();
            }
            curY += rowHeight;

            // Light probe section
            if (!UnityEngine.Rendering.SupportedRenderingFeatures.active.overridesLightProbeSystem)
            {
                curY += rowHeight;
                toggleRect = new Rect(margin, curY, labelWidth, rowHeight);
                GUI.Label(toggleRect, lightProbeVisualizationContent, EditorStyles.boldLabel);
                curY += rowHeight;

                toggleRect = new Rect(margin, curY, position.width - margin * 2, rowHeight);
                LightProbeVisualization.lightProbeVisualizationMode = (LightProbeVisualization.LightProbeVisualizationMode)EditorGUI.EnumPopup(toggleRect, LightProbeVisualization.lightProbeVisualizationMode);
                curY += rowHeight;

                if (LightProbeVisualization.lightProbeVisualizationMode != LightProbeVisualization.LightProbeVisualizationMode.None)
                {
                    toggleRect = new Rect(margin, curY, labelWidth, rowHeight);
                    LightProbeVisualization.showInterpolationWeights = GUI.Toggle(toggleRect, LightProbeVisualization.showInterpolationWeights, displayWeightsContent);
                    curY += rowHeight;

                    toggleRect = new Rect(margin, curY, labelWidth, rowHeight);
                    LightProbeVisualization.showOcclusions = GUI.Toggle(toggleRect, LightProbeVisualization.showOcclusions, displayOcclusionContent);
                    curY += rowHeight;

                    toggleRect = new Rect(margin, curY, labelWidth, rowHeight);
                    LightProbeVisualization.highlightInvalidCells = GUI.Toggle(toggleRect, LightProbeVisualization.highlightInvalidCells, highlightInvalidCellsContent);
                }
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
            Rect scrollViewRect = new Rect(0,
                startY + barHeight,
                position.width - 4,
                height - barHeight - k_FrameWidth);
            float totalContentHeight = DrawNormalList(false, 0, 0, 100000);
            Rect contentRect = new Rect(0, 0, 1, totalContentHeight);
            bool isScrollbarVisible = totalContentHeight > scrollViewRect.height;
            float listElementWidth = scrollViewRect.width;
            if (isScrollbarVisible)
                listElementWidth -= k_ScrollBarWidth;

            // Scrollview
            m_ScrollPosition = GUI.BeginScrollView(scrollViewRect, m_ScrollPosition, contentRect, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, EditorStyles.scrollViewAlt);
            {
                DrawNormalList(true, listElementWidth, m_ScrollPosition.y - k_ListElementHeight, m_ScrollPosition.y + totalContentHeight);
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
            bool searchDrawn = false;

            curY = DrawListSection(curY, L10n.Tr("Recently Changed"),    m_RecentAnnotations,    doDraw, listElementWidth, startY, endY, ref even, true,  ref headerDrawn, ref searchDrawn);
            curY = DrawListSection(curY, L10n.Tr("Scripts"),             m_ScriptAnnotations,    doDraw, listElementWidth, startY, endY, ref even, false, ref headerDrawn, ref searchDrawn);
            curY = DrawListSection(curY, L10n.Tr("Built-in Components"), m_BuiltinAnnotations,   doDraw, listElementWidth, startY, endY, ref even, false, ref headerDrawn, ref searchDrawn);

            return curY;
        }

        bool DoesMatchFilter(GizmoInfo el)
        {
            if (string.IsNullOrEmpty(m_SearchFilter))
                return true;
            if (el.name.IndexOf(m_SearchFilter, System.StringComparison.OrdinalIgnoreCase) < 0)
                return false;
            return true;
        }

        float DrawListSection(float y, string sectionHeader, List<GizmoInfo> listElements, bool doDraw, float listElementWidth, float startY, float endY, ref bool even, bool useSeperator, ref bool headerDrawn, ref bool searchDrawn)
        {
            float curY = y;
            const float kSearchPaddingV = 3;
            const float kSearchHeight = EditorGUI.kSingleSmallLineHeight + kSearchPaddingV;
            const float extraHeader = 15;
            const float headerHeight = 20;

            if (listElements.Count > 0)
            {
                if (doDraw)
                {
                    // Header background
                    Rect rect = new Rect(1, curY, listElementWidth - 2, extraHeader + (headerDrawn ? 0 : kSearchHeight) + headerHeight);
                    Flip(ref even);
                    GUIStyle backgroundStyle = even ? m_Styles.listEvenBg : m_Styles.listOddBg; // m_Styles.listSectionHeaderBg;//
                    GUI.Label(rect, GUIContent.Temp(""), backgroundStyle);
                }

                // Search field
                if (!searchDrawn)
                {
                    searchDrawn = true;
                    if (doDraw)
                    {
                        Rect searchRect = new Rect(11, curY + kSearchPaddingV, listElementWidth - 16, EditorGUI.kSingleSmallLineHeight);
                        m_SearchFilter = EditorGUI.ToolbarSearchField(searchRect, m_SearchFilter, false);
                    }
                    curY += kSearchHeight;
                }

                curY += extraHeader;
                if (doDraw)
                {
                    // Header text
                    DrawListHeader(sectionHeader, listElements, new Rect(3, curY, listElementWidth, headerHeight), ref headerDrawn);
                }
                curY += headerHeight;

                // List elements
                for (int i = 0; i < listElements.Count; ++i)
                {
                    if (!DoesMatchFilter(listElements[i]))
                        continue;
                    Flip(ref even);
                    if (curY > startY && curY < endY)
                    {
                        Rect rect = new Rect(1, curY, listElementWidth - 2, k_ListElementHeight);
                        if (doDraw)
                            DrawListElement(rect, even, listElements[i]);
                    }
                    curY += k_ListElementHeight;
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

        void DrawListHeader(string header, List<GizmoInfo> elements, Rect rect, ref bool headerDrawn)
        {
            GUI.Label(rect, GUIContent.Temp(header), m_Styles.listHeaderStyle);

            float toggleSize = Styles.k_ToggleSize;
            EnabledState enabledState = EnabledState.NotSet;

            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (!DoesMatchFilter(element))
                    continue;

                if (element.hasGizmo)
                {
                    if (enabledState == EnabledState.NotSet)
                    {
                        enabledState = element.gizmoEnabled ? EnabledState.All : EnabledState.None;
                    }
                    else if ((enabledState == EnabledState.All) != element.gizmoEnabled)
                    {
                        enabledState = EnabledState.Mixed;
                        break;
                    }
                }
            }

            if (enabledState == EnabledState.NotSet)
                return;

            var gizmoText = "gizmo";
            var gizmoTextSize = m_Styles.columnHeaderStyle.CalcSize(new GUIContent(gizmoText));
            gizmoTextRightAlign = gizmoTextSize.x;
            gizmoRightAlign = gizmoTextRightAlign - (gizmoTextSize.x * 0.5f - m_Styles.toggle.CalcSize(GUIContent.none).x*0.5f);
            var iconText = "icon";
            var iconTextSize = m_Styles.columnHeaderStyle.CalcSize(new GUIContent(iconText));
            iconTextRightAlign = iconTextSize.x + gizmoTextRightAlign + 10;
            iconRightAlign = iconTextRightAlign - (iconTextSize.x * 0.5f - iconSize * 0.5f);


            GUIStyle style = m_Styles.toggle;
            bool enabled = enabledState == EnabledState.All;
            bool setMixed = enabledState == EnabledState.Mixed;
            if (setMixed)
                style = m_Styles.toggleMixed;

            Rect toggleRect = new Rect(rect.width - gizmoRightAlign, rect.y + (rect.height - toggleSize) * 0.5f, toggleSize, toggleSize);

            EditorGUI.BeginChangeCheck();
            bool newEnabled = GUI.Toggle(toggleRect, enabled, GUIContent.none, style);
            if (EditorGUI.EndChangeCheck())
            {
                for (int i = 0; i < elements.Count; i++)
                {
                    var element = elements[i];
                    if (!DoesMatchFilter(element))
                        continue;

                    if (element.gizmoEnabled != newEnabled)
                    {
                        element.gizmoEnabled = newEnabled;
                        SetGizmoState(element, false);
                    }
                }
            }

            if (headerDrawn == false)
            {
                headerDrawn = true;
                GUI.color = new Color(1, 1, 1, 0.65f);

                //  Column headers
                Rect columnRect = rect;
                columnRect.y -= gizmoTextSize.y + 3;
                columnRect.x = rect.width - gizmoTextRightAlign;
                GUI.Label(columnRect, gizmoText, m_Styles.columnHeaderStyle);

                columnRect.x = rect.width - iconTextRightAlign;
                GUI.Label(columnRect, iconText, m_Styles.columnHeaderStyle);

                GUI.color = Color.white;
            }
        }

        void DrawListElement(Rect rect, bool even, GizmoInfo ainfo)
        {
            if (ainfo == null)
            {
                Debug.LogError("DrawListElement: AInfo not valid!");
                return;
            }

            string tooltip;
            float togglerSize = Styles.k_ToggleSize;
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
            GUI.Label(textRect, ainfo.name, m_Styles.listTextStyle);


            // Icon toggle
            Rect iconRect = new Rect(rect.width - iconRightAlign + 2, rect.y + (rect.height - iconSize) * 0.5f, iconSize, iconSize); // +2 because the given rect is shortened by 2px before this method call
            if (ainfo.scriptClass != "")
            {
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

                if (!ainfo.disabled)
                {
                    Rect arrowRect = iconRect;
                    arrowRect.x += 18;
                    arrowRect.y += 0;
                    arrowRect.width = 9;

                    if (GUI.Button(arrowRect, iconSelectContent, m_Styles.iconDropDown))
                    {
                        Object script = EditorGUIUtility.GetScript(ainfo.scriptClass);
                        if (script != null)
                        {
                            m_LastScriptThatHasShownTheIconSelector = ainfo.scriptClass;
                            if (IconSelector.ShowAtPosition(script, arrowRect, true))
                            {
                                IconSelector.SetMonoScriptIconChangedCallback(MonoScriptIconChanged);
                                GUIUtility.ExitGUI();
                            }
                        }
                    }
                }
            }

            if (ainfo.thumb != null)
            {
                if (!ainfo.iconEnabled)
                {
                    GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, disabledAlpha);
                    tooltip = "";
                }

                iconToggleContent.image = ainfo.thumb;
                if (GUI.Button(iconRect, iconToggleContent, GUIStyle.none))
                {
                    ainfo.iconEnabled = !ainfo.iconEnabled;
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
            if (ainfo.hasGizmo)
            {
                tooltip = textGizmoVisible;

                Rect togglerRect = new Rect(rect.width - gizmoRightAlign + 2, rect.y + (rect.height - togglerSize) * 0.5f, togglerSize, togglerSize); // +2 because the given rect is shortened by 2px before this method call
                ainfo.gizmoEnabled = GUI.Toggle(togglerRect, ainfo.gizmoEnabled, new GUIContent("", tooltip), m_Styles.toggle);
                if (GUI.changed)
                {
                    SetGizmoState(ainfo);
                }
            }

            GUI.enabled = orgGUIEnabled;
            GUI.changed = orgGUIChanged;
            GUI.color = orgColor;
        }

        void SetIconState(GizmoInfo ainfo)
        {
            AnnotationUtility.SetIconEnabled(ainfo.classID, ainfo.scriptClass, ainfo.iconEnabled ? 1 : 0);
            SceneView.RepaintAll();
        }

        void SetGizmoState(GizmoInfo ainfo, bool addToMostRecentChanged = true)
        {
            AnnotationUtility.SetGizmoEnabled(ainfo.classID, ainfo.scriptClass, ainfo.gizmoEnabled ? 1 : 0, addToMostRecentChanged);
            SceneView.RepaintAll();
        }

        [Shortcut("Scene View/Toggle Selection Outline", typeof(SceneView))]
        static void ToggleSelectionOutline()
        {
            AnnotationUtility.showSelectionOutline = !AnnotationUtility.showSelectionOutline;
        }

        [Shortcut("Scene View/Toggle Selection Wireframe", typeof(SceneView))]
        static void ToggleSelectionWireframe()
        {
            AnnotationUtility.showSelectionWire = !AnnotationUtility.showSelectionWire;
        }
    }
}
