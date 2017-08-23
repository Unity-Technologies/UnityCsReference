// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.StyleSheets;

namespace UnityEditor.Experimental.UIElements.Debugger
{
    class UIElementsDebugger : EditorWindow
    {
        private int m_CurFoldout;

        private string m_DumpId = "dump";
        private bool m_UxmlDumpExpanded;
        private bool m_UxmlDumpStyleFields;
        private bool m_NewLineOnAttributes;
        private bool m_AutoNameElements;

        private ViewPanel? m_CurPanel;
        private string m_DetailFilter = string.Empty;
        private Vector2 m_DetailScroll = Vector2.zero;
        private bool m_Overlay = true;

        private PickingData m_PickingData;
        private bool m_PickingElementInPanel;

        private VisualElement m_SelectedElement;

        private bool m_ShowDefaults;
        private SplitterState m_SplitterState;

        private Texture2D m_TempTexture;
        private TreeViewState m_VisualTreeTreeViewState;
        private VisualTreeTreeView m_VisualTreeTreeView;
        private static readonly FieldInfo[] k_FieldInfos = typeof(VisualElementStylesData).GetFields();

        [MenuItem("Window/UI Debuggers/UIElements Debugger", false, 2013, true)]
        public static void Open()
        {
            GetWindow<UIElementsDebugger>().Show();
        }

        private bool InterceptEvents(Event ev)
        {
            if (!m_CurPanel.HasValue)
                return false;
            if (!Event.current.isMouse)
                return false;
            var e = m_CurPanel.Value.Panel.Pick(ev.mousePosition);
            if (e != null)
                ((PanelDebug)m_CurPanel.Value.Panel.panelDebug).highlightedElement = e.controlid;

            // stop intercepting events
            if (ev.clickCount > 0 && ev.button == 0)
            {
                m_CurPanel.Value.Panel.panelDebug.interceptEvents = null;
                m_PickingElementInPanel = false;
                m_VisualTreeTreeView.ExpandAll();
                var node = m_VisualTreeTreeView.GetRows()
                    .OfType<VisualTreeItem>()
                    .FirstOrDefault(vti => e != null && vti.elt.controlid == e.controlid);
                if (node != null)
                    m_VisualTreeTreeView.SetSelection(new List<int> { node.id }, TreeViewSelectionOptions.RevealAndFrame);
            }
            return true;
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));
            bool refresh = false;
            if (m_PickingData == null)
                m_PickingData = new PickingData();

            EditorGUI.BeginChangeCheck();
            m_PickingData.DoSelectDropDown();
            if (EditorGUI.EndChangeCheck())
            {
                refresh = true;
            }
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                m_PickingData.Refresh();
            }

            bool includeShadowHierarchy = GUILayout.Toggle(m_VisualTreeTreeView.includeShadowHierarchy, Styles.includeShadowHierarchyContent, EditorStyles.toolbarButton);
            if (includeShadowHierarchy != m_VisualTreeTreeView.includeShadowHierarchy)
            {
                m_VisualTreeTreeView.includeShadowHierarchy = includeShadowHierarchy;
                refresh = true;
            }

            if (m_CurPanel.HasValue)
            {
                var newPickingElementInPanel = GUILayout.Toggle(m_PickingElementInPanel, Styles.pickElementInPanelContent, EditorStyles.toolbarButton);
                if (newPickingElementInPanel != m_PickingElementInPanel)
                {
                    m_PickingElementInPanel = newPickingElementInPanel;
                    if (m_PickingElementInPanel)
                        m_CurPanel.Value.Panel.panelDebug.interceptEvents = InterceptEvents;
                }
            }

            m_Overlay = GUILayout.Toggle(m_Overlay, Styles.overlayContent, EditorStyles.toolbarButton);

            EditorGUILayout.EndHorizontal();

            if (refresh)
            {
                EndPicking(m_PickingData.Selected);
            }

            if (m_CurPanel.HasValue)
            {
                if (m_CurPanel.Value.Panel.panelDebug.enabled != m_Overlay)
                {
                    m_CurPanel.Value.Panel.panelDebug.enabled = m_Overlay;
                    m_CurPanel.Value.Panel.visualTree.Dirty(ChangeType.Repaint);
                }

                SplitterGUILayout.BeginHorizontalSplit(m_SplitterState, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                SplitterGUILayout.EndHorizontalSplit();

                float column1Width = m_SplitterState.realSizes.Length > 0 ? m_SplitterState.realSizes[0] : 150;
                var column2Width = position.width - column1Width;
                var column1Rect = new Rect(0, EditorGUI.kWindowToolbarHeight, column1Width, position.height - EditorGUI.kWindowToolbarHeight);
                var column2Rect = new Rect(column1Width, EditorGUI.kWindowToolbarHeight, column2Width, column1Rect.height);

                m_VisualTreeTreeView.OnGUI(column1Rect);
                DrawSelection(column2Rect);

                // Draw separator
                EditorGUI.DrawRect(
                    new Rect(column1Width + column1Rect.xMin, column1Rect.y, 1, column1Rect.height),
                    Styles.separatorColor);
            }
        }

        private void EndPicking(ViewPanel? viewPanel)
        {
            var it = UIElementsUtility.GetPanelsIterator();
            while (it.MoveNext())
                it.Current.Value.panelDebug = null;
            m_CurPanel = viewPanel;
            if (m_CurPanel.HasValue)
            {
                if (m_CurPanel.Value.Panel.panelDebug == null)
                    m_CurPanel.Value.Panel.panelDebug = new PanelDebug();
                m_CurPanel.Value.Panel.panelDebug.enabled = true;

                m_CurPanel.Value.Panel.visualTree.Dirty(ChangeType.Repaint);
                m_VisualTreeTreeView.panel = (m_CurPanel.Value.Panel);
                m_VisualTreeTreeView.Reload();
            }
        }

        private void DrawSelection(Rect rect)
        {
            var evt = Event.current;
            if (evt.type == EventType.Layout)
                CacheData();

            if (m_SelectedElement == null)
                return;

            GUILayout.BeginArea(rect);

            EditorGUILayout.LabelField(m_SelectedElement.GetType().Name, Styles.KInspectorTitle);

            m_DetailScroll = EditorGUILayout.BeginScrollView(m_DetailScroll);

            var sizeRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(Styles.SizeRectHeight));
            sizeRect.y += EditorGUIUtility.singleLineHeight;
            DrawSize(sizeRect, m_SelectedElement);

            DrawUxmlDump(m_SelectedElement);

            DrawMatchingRules();

            DrawProperties();

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawUxmlDump(VisualElement selectedElement)
        {
            m_UxmlDumpExpanded = EditorGUILayout.Foldout(m_UxmlDumpExpanded, Styles.uxmlContent);
            if (m_UxmlDumpExpanded)
            {
                EditorGUI.BeginChangeCheck();
                m_DumpId = EditorGUILayout.TextField("Template id", m_DumpId);
                m_UxmlDumpStyleFields = EditorGUILayout.Toggle("Include style fields", m_UxmlDumpStyleFields);
                m_NewLineOnAttributes = EditorGUILayout.Toggle("Line breaks on attributes", m_NewLineOnAttributes);
                m_AutoNameElements = EditorGUILayout.Toggle("Auto name elements", m_AutoNameElements);
                if (EditorGUI.EndChangeCheck())
                {
                    m_SelectedElementUxml = null;
                }
                if (m_SelectedElementUxml == null)
                {
                    UxmlExporter.ExportOptions options = UxmlExporter.ExportOptions.None;

                    if (m_UxmlDumpStyleFields)
                        options = UxmlExporter.ExportOptions.StyleFields;

                    if (m_NewLineOnAttributes)
                        options |= UxmlExporter.ExportOptions.NewLineOnAttributes;
                    if (m_AutoNameElements)
                        options |= UxmlExporter.ExportOptions.AutoNameElements;

                    m_SelectedElementUxml = UxmlExporter.Dump(selectedElement, m_DumpId ?? "template", options);
                }
                EditorGUILayout.TextArea(m_SelectedElementUxml);
            }
        }

        private void CacheData()
        {
            if (!m_VisualTreeTreeView.HasSelection())
            {
                m_SelectedElement = null;
                m_UxmlDumpExpanded = false;

                if (!m_PickingElementInPanel && m_CurPanel.HasValue && m_CurPanel.Value.Panel != null && m_CurPanel.Value.Panel.panelDebug != null)
                    ((PanelDebug)m_CurPanel.Value.Panel.panelDebug).highlightedElement = 0;
                return;
            }

            int selectedId = m_VisualTreeTreeView.GetSelection().First();
            VisualTreeItem selectedItem = m_VisualTreeTreeView.GetNodeFor(selectedId);

            if (selectedItem == null)
                return;

            VisualElement element = selectedItem.elt;

            if (element == null)
                return;

            if (!m_CurPanel.HasValue)
                return;
            if (m_SelectedElement != element)
            {
                m_SelectedElement = element;
                m_SelectedElementUxml = null;
            }

            // element picking uses the highlight
            if (!m_PickingElementInPanel)
                ((PanelDebug)m_CurPanel.Value.Panel.panelDebug).highlightedElement = element.controlid;
            GetElementMatchers();
        }

        private static MatchedRulesExtractor s_MatchedRulesExtractor = new MatchedRulesExtractor();
        private string m_SelectedElementUxml;

        private void GetElementMatchers()
        {
            if (m_SelectedElement == null || m_SelectedElement.elementPanel == null)
                return;
            s_MatchedRulesExtractor.selectedElementRules.Clear();
            s_MatchedRulesExtractor.selectedElementStylesheets.Clear();
            s_MatchedRulesExtractor.target = m_SelectedElement;
            s_MatchedRulesExtractor.Traverse(m_SelectedElement.elementPanel.visualTree, 0, s_MatchedRulesExtractor.ruleMatchers);
            s_MatchedRulesExtractor.ruleMatchers.Clear();
        }

        private static int GetSpecificity<T>(StyleValue<T> style)
        {
            return style.specificity;
        }

        private void DrawProperties()
        {
            EditorGUILayout.LabelField(Styles.elementStylesContent, Styles.KInspectorTitle);

            GUILayout.BeginHorizontal();
            m_DetailFilter = EditorGUILayout.ToolbarSearchField(m_DetailFilter);
            m_ShowDefaults = EditorGUILayout.Toggle(Styles.showDefaultsContent, m_ShowDefaults);
            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Name", m_SelectedElement.name);
            EditorGUILayout.LabelField("Classes", string.Join(",", m_SelectedElement.GetClasses().ToArray()));
            EditorGUILayout.LabelField("Use Pixel Caching", m_SelectedElement.usePixelCaching.ToString());
            EditorGUILayout.LabelField("Visible", m_SelectedElement.visible.ToString());

            var styles = m_SelectedElement.effectiveStyle;
            bool anyChanged = false;

            foreach (var field in k_FieldInfos)
            {
                if (!string.IsNullOrEmpty(m_DetailFilter) &&
                    field.Name.IndexOf(m_DetailFilter, StringComparison.InvariantCultureIgnoreCase) == -1)
                    continue;

                if (!field.FieldType.IsGenericType || field.FieldType.GetGenericTypeDefinition() != typeof(StyleValue<>))
                    continue;

                var val = field.GetValue(styles);
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                int specificity;

                if (val is StyleValue<float>)
                {
                    var style = (StyleValue<float>)val;
                    specificity = GetSpecificity(style);
                    if (m_ShowDefaults || specificity > 0)
                    {
                        style.specificity = Int32.MaxValue;
                        style.value = EditorGUILayout.FloatField(field.Name, ((StyleValue<float>)val).value);
                        val = style;
                    }
                }
                else if (val is StyleValue<int>)
                {
                    var style = (StyleValue<int>)val;
                    specificity = GetSpecificity(style);
                    if (m_ShowDefaults || specificity > 0)
                    {
                        style.specificity = Int32.MaxValue;
                        style.value = EditorGUILayout.IntField(field.Name, ((StyleValue<int>)val).value);
                        val = style;
                    }
                }
                else if (val is StyleValue<bool>)
                {
                    var style = (StyleValue<bool>)val;
                    specificity = GetSpecificity(style);
                    if (m_ShowDefaults || specificity > 0)
                    {
                        style.specificity = Int32.MaxValue;
                        style.value = EditorGUILayout.Toggle(field.Name, ((StyleValue<bool>)val).value);
                        val = style;
                    }
                }
                else if (val is StyleValue<Color>)
                {
                    var style = (StyleValue<Color>)val;
                    specificity = GetSpecificity(style);
                    if (m_ShowDefaults || specificity > 0)
                    {
                        style.specificity = Int32.MaxValue;
                        style.value = EditorGUILayout.ColorField(field.Name, ((StyleValue<Color>)val).value);
                        val = style;
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(field.Name, val == null ? "null" : val.ToString());
                    specificity = -1;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    styles = m_SelectedElement.effectiveStyle;
                    anyChanged = true;
                    field.SetValue(m_SelectedElement.style, val);
                }

                if (specificity > 0)
                    GUILayout.Label(specificity == int.MaxValue ? "inline" : specificity.ToString());

                EditorGUILayout.EndHorizontal();
            }

            if (anyChanged)
            {
                m_CurPanel.Value.Panel.visualTree.Dirty(ChangeType.Transform);
                m_CurPanel.Value.Panel.visualTree.Dirty(ChangeType.Styles);
                m_CurPanel.Value.Panel.visualTree.Dirty(ChangeType.Layout);
                m_CurPanel.Value.Panel.visualTree.Dirty(ChangeType.Repaint);
                m_CurPanel.Value.View.RepaintImmediately();
            }
        }

        private void DrawMatchingRules()
        {
            if (s_MatchedRulesExtractor.selectedElementStylesheets != null && s_MatchedRulesExtractor.selectedElementStylesheets.Count > 0)
            {
                EditorGUILayout.LabelField(Styles.stylesheetsContent, Styles.KInspectorTitle);
                foreach (var sheet in s_MatchedRulesExtractor.selectedElementStylesheets)
                {
                    GUILayout.Label(sheet);
                }
            }

            if (s_MatchedRulesExtractor.selectedElementRules != null && s_MatchedRulesExtractor.selectedElementRules.Count > 0)
            {
                EditorGUILayout.LabelField(Styles.selectorsContent, Styles.KInspectorTitle);
                for (var i = 0; i < s_MatchedRulesExtractor.selectedElementRules.Count; i++)
                {
                    var builder = new StringBuilder();
                    for (var j = 0; j < s_MatchedRulesExtractor.selectedElementRules[i].complexSelector.selectors.Length; j++)
                    {
                        var sel = s_MatchedRulesExtractor.selectedElementRules[i].complexSelector.selectors[j];
                        switch (sel.previousRelationship)
                        {
                            case StyleSelectorRelationship.Child:
                                builder.Append(" ");
                                break;
                            case StyleSelectorRelationship.Descendent:
                                builder.Append(" > ");
                                break;
                        }
                        for (var k = 0; k < sel.parts.Length; k++)
                        {
                            var part = sel.parts[k];
                            switch (part.type)
                            {
                                case StyleSelectorType.Class:
                                    builder.Append(".");
                                    break;
                                case StyleSelectorType.ID:
                                    builder.Append("#");
                                    break;
                                case StyleSelectorType.PseudoClass:
                                case StyleSelectorType.RecursivePseudoClass:
                                    builder.Append(":");
                                    break;
                                case StyleSelectorType.Wildcard: break;
                            }
                            builder.Append(part.value);
                        }
                    }

                    var props = s_MatchedRulesExtractor.selectedElementRules[i].complexSelector.rule.properties;
                    var expanded = m_CurFoldout == i;
                    EditorGUILayout.BeginHorizontal();
                    var foldout = EditorGUILayout.Foldout(m_CurFoldout == i, new GUIContent(builder.ToString()), true);
                    var path = AssetDatabase.GetAssetPath(s_MatchedRulesExtractor.selectedElementRules[i].sheet) ?? "<unknown>";
                    var line = s_MatchedRulesExtractor.selectedElementRules[i].complexSelector.rule.line;
                    if (GUILayout.Button(Path.GetFileName(path) + ":" + line,
                            EditorStyles.miniBoldLabel, GUILayout.MaxWidth(150)))
                        InternalEditorUtility.OpenFileAtLineExternal(path, line);
                    EditorGUILayout.EndHorizontal();

                    if (expanded && !foldout)
                        m_CurFoldout = -1;
                    else if (!expanded && foldout)
                        m_CurFoldout = i;

                    if (foldout)
                    {
                        EditorGUI.indentLevel++;
                        for (var j = 0; j < props.Length; j++)
                        {
                            var s = s_MatchedRulesExtractor.selectedElementRules[i].sheet.ReadAsString(props[j].values[0]);
                            EditorGUILayout.LabelField(new GUIContent(props[j].name), new GUIContent(s));
                        }

                        EditorGUI.indentLevel--;
                    }
                }
            }
        }

        private void DrawSize(Rect rect, VisualElement element)
        {
            var cursor = new Rect(rect);
            cursor.x += Styles.SizeRectBetweenSize;
            cursor.y += Styles.SizeRectBetweenSize;
            cursor.width -= 2 * Styles.SizeRectBetweenSize;
            cursor.height -= 2 * Styles.SizeRectBetweenSize;
            DrawRect(cursor, Styles.SizeRectLineSize, Styles.kSizeMarginPrimaryColor, Styles.kSizeMarginSecondaryColor);
            DrawSizeLabels(cursor, Styles.marginContent, element.style.marginTop, element.style.marginRight, element.style.marginBottom, element.style.marginLeft);

            cursor.x += Styles.SizeRectBetweenSize;
            cursor.y += Styles.SizeRectBetweenSize;
            cursor.width -= 2 * Styles.SizeRectBetweenSize;
            cursor.height -= 2 * Styles.SizeRectBetweenSize;
            DrawRect(cursor, Styles.SizeRectLineSize, Styles.kSizeBorderPrimaryColor, Styles.kSizeBorderSecondaryColor);
            DrawSizeLabels(cursor, Styles.borderContent, element.style.borderTop, element.style.borderRight, element.style.borderBottom, element.style.borderLeft);

            cursor.x += Styles.SizeRectBetweenSize;
            cursor.y += Styles.SizeRectBetweenSize;
            cursor.width -= 2 * Styles.SizeRectBetweenSize;
            cursor.height -= 2 * Styles.SizeRectBetweenSize;
            DrawRect(cursor, Styles.SizeRectLineSize, Styles.kSizePaddingPrimaryColor, Styles.kSizePaddingSecondaryColor);
            DrawSizeLabels(cursor, Styles.paddingContent, element.style.paddingTop, element.style.paddingRight, element.style.paddingBottom, element.style.paddingLeft);

            cursor.x += Styles.SizeRectBetweenSize;
            cursor.y += Styles.SizeRectBetweenSize;
            cursor.width -= 2 * Styles.SizeRectBetweenSize;
            cursor.height -= 2 * Styles.SizeRectBetweenSize;
            DrawRect(cursor, Styles.SizeRectLineSize, Styles.kSizePrimaryColor, Styles.kSizeSecondaryColor);
            EditorGUI.LabelField(cursor, string.Format("{0:F2} x {1:F2}", element.layout.width, element.layout.height), Styles.KSizeLabel);
        }

        private static void DrawSizeLabels(Rect cursor, GUIContent label, float top, float right, float bottom, float left)
        {
            var labelCursor = new Rect(
                    cursor.x + (cursor.width - Styles.LabelSizeSize) * 0.5f,
                    cursor.y + 2, Styles.LabelSizeSize,
                    EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelCursor, top.ToString("F2"), Styles.KSizeLabel);
            labelCursor.y = cursor.y + cursor.height + 2 - Styles.SizeRectBetweenSize;
            EditorGUI.LabelField(labelCursor, bottom.ToString("F2"), Styles.KSizeLabel);
            labelCursor.x = cursor.x;
            labelCursor.y = cursor.y + (cursor.height - EditorGUIUtility.singleLineHeight) * 0.5f;
            EditorGUI.LabelField(labelCursor, left.ToString("F2"), Styles.KSizeLabel);
            labelCursor.x = cursor.x + cursor.width - Styles.SizeRectBetweenSize - 4;
            EditorGUI.LabelField(labelCursor, right.ToString("F2"), Styles.KSizeLabel);

            labelCursor.x = cursor.x + 2;
            labelCursor.y = cursor.y + 2;
            labelCursor.width = Styles.LabelSizeSize;
            labelCursor.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(labelCursor, label, Styles.KSizeLabel);
        }

        public void OnEnable()
        {
            m_PickingData = new PickingData();
            titleContent = new GUIContent("UIElements Debugger");
            m_VisualTreeTreeViewState = new TreeViewState();
            m_VisualTreeTreeView = new VisualTreeTreeView(m_VisualTreeTreeViewState);
            if (m_SplitterState == null)
                m_SplitterState = new SplitterState(1, 2);
            m_TempTexture = new Texture2D(2, 2);
        }

        private void DrawRect(Rect rect, float borderSize, Color borderColor, Color fillingColor)
        {
            m_TempTexture.SetPixel(0, 0, fillingColor);
            m_TempTexture.SetPixel(1, 0, fillingColor);
            m_TempTexture.SetPixel(0, 1, fillingColor);
            m_TempTexture.SetPixel(1, 1, fillingColor);
            m_TempTexture.Apply();
            GUI.DrawTexture(rect, m_TempTexture);

            m_TempTexture.SetPixel(0, 0, borderColor);
            m_TempTexture.SetPixel(1, 0, borderColor);
            m_TempTexture.SetPixel(0, 1, borderColor);
            m_TempTexture.SetPixel(1, 1, borderColor);
            m_TempTexture.Apply();

            var cursor = new Rect(rect.x, rect.y, rect.width, borderSize);
            GUI.DrawTexture(cursor, m_TempTexture);
            cursor.y = rect.y + rect.height - borderSize;
            GUI.DrawTexture(cursor, m_TempTexture);
            cursor.width = borderSize;
            cursor.height = rect.height;
            cursor.y = rect.y;
            GUI.DrawTexture(cursor, m_TempTexture);
            cursor.x = rect.x + rect.width - borderSize;
            GUI.DrawTexture(cursor, m_TempTexture);
        }

        internal struct ViewPanel
        {
            public GUIView View;
            public Panel Panel;
        }

        internal static class Styles
        {
            internal const float LabelSizeSize = 50;
            internal const float SizeRectLineSize = 3;
            internal const float SizeRectBetweenSize = 35;
            internal const float SizeRectHeight = 350;
            internal const float SplitterSize = 2;
            internal const float LabelWidth = 150;
            public static GUIStyle KSizeLabel = new GUIStyle { alignment = TextAnchor.MiddleCenter };
            public static GUIStyle KInspectorTitle = new GUIStyle(EditorStyles.whiteLargeLabel) { alignment = TextAnchor.MiddleCenter };

            public static readonly GUIContent elementStylesContent = new GUIContent("Element styles");
            public static readonly GUIContent showDefaultsContent = new GUIContent("Show defaults");
            public static readonly GUIContent inlineContent = new GUIContent("INLINE");
            public static readonly GUIContent marginContent = new GUIContent("Margin");
            public static readonly GUIContent borderContent = new GUIContent("Border");
            public static readonly GUIContent paddingContent = new GUIContent("Padding");
            public static readonly GUIContent cancelPickingContent = new GUIContent("Cancel picking");
            public static readonly GUIContent pickPanelContent = new GUIContent("Pick Panel");
            public static readonly GUIContent pickElementInPanelContent = new GUIContent("Pick Element in panel");
            public static readonly GUIContent overlayContent = new GUIContent("Overlay");
            public static readonly GUIContent uxmlContent = new GUIContent("UXML Dump");
            public static readonly GUIContent stylesheetsContent = new GUIContent("Stylesheets");
            public static readonly GUIContent selectorsContent = new GUIContent("Matching Selectors");
            public static readonly GUIContent includeShadowHierarchyContent = new GUIContent("Include Shadow Hierarchy");

            private static readonly Color k_SeparatorColorPro = new Color(0.15f, 0.15f, 0.15f);
            private static readonly Color k_SeparatorColorNonPro = new Color(0.6f, 0.6f, 0.6f);
            internal static readonly Color kSizeMarginPrimaryColor = new Color(0, 0, 0);
            internal static readonly Color kSizeMarginSecondaryColor = new Color(249f / 255, 204f / 255, 157f / 255);
            internal static readonly Color kSizeBorderPrimaryColor = new Color(0, 0, 0);
            internal static readonly Color kSizeBorderSecondaryColor = new Color(253f / 255, 221f / 255, 155f / 255);
            internal static readonly Color kSizePaddingPrimaryColor = new Color(0, 0, 0);
            internal static readonly Color kSizePaddingSecondaryColor = new Color(194f / 255, 237f / 255, 138f / 255);
            internal static readonly Color kSizePrimaryColor = new Color(0, 0, 0);
            internal static readonly Color kSizeSecondaryColor = new Color(139f / 255, 181f / 255, 192f / 255);

            public static Color separatorColor
            {
                get { return EditorGUIUtility.isProSkin ? k_SeparatorColorPro : k_SeparatorColorNonPro; }
            }
        }
    }

    internal class MatchedRulesExtractor : HierarchyTraversal
    {
        internal List<RuleMatcher> ruleMatchers = new List<RuleMatcher>();

        internal List<RuleMatcher> selectedElementRules = new List<RuleMatcher>();
        internal HashSet<string> selectedElementStylesheets = new HashSet<string>();

        private VisualElement m_Target;
        private List<VisualElement> m_Hierarchy = new List<VisualElement>();
        private int m_Index;

        public VisualElement target
        {
            get { return m_Target; }
            set
            {
                m_Target = value;
                m_Hierarchy.Clear();
                m_Hierarchy.Add(value);
                VisualElement cursor = value;

                while (cursor != null)
                {
                    if (cursor.styleSheets != null)
                    {
                        foreach (var sheet in cursor.styleSheets)
                        {
                            selectedElementStylesheets.Add(AssetDatabase.GetAssetPath(sheet));
                            PushStyleSheet(sheet);
                        }
                    }

                    m_Hierarchy.Add(cursor);
                    cursor = cursor.shadow.parent;
                }
                m_Index = m_Hierarchy.Count - 1;
            }
        }

        private void PushStyleSheet(StyleSheet styleSheetData)
        {
            var complexSelectors = styleSheetData.complexSelectors;
            // To avoid excessive re-allocations, just resize the list right now
            int futureSize = ruleMatchers.Count + complexSelectors.Length;
            ruleMatchers.Capacity = Math.Max(ruleMatchers.Capacity, futureSize);

            for (int i = 0; i < complexSelectors.Length; i++)
            {
                StyleComplexSelector complexSelector = complexSelectors[i];
                // For every complex selector, push a matcher for first sub selector
                ruleMatchers.Add(new RuleMatcher()
                {
                    sheet = styleSheetData,
                    complexSelector = complexSelector,
                    simpleSelectorIndex = 0,
                    depth = Int32.MaxValue
                });
            }
        }

        public override bool ShouldSkipElement(VisualElement element)
        {
            return false;
        }

        public override bool OnRuleMatchedElement(RuleMatcher matcher, VisualElement element)
        {
            if (element == target)
                selectedElementRules.Add(matcher);
            return false;
        }

        protected override void Recurse(VisualElement element, int depth, List<RuleMatcher> allRuleMatchers)
        {
            m_Index--;
            if (m_Index >= 0)
                Traverse(m_Hierarchy[m_Index], depth + 1, allRuleMatchers);
        }
    }
}
