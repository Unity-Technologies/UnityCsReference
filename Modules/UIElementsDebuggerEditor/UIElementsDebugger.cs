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
        [SerializeField]
        private string m_LastWindowTitle;

        private bool m_ScheduleRestoreSelection;

        private HashSet<int> m_CurFoldout = new HashSet<int>();

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
        private bool m_Sort;
        private SplitterState m_SplitterState;

        private Texture2D m_TempTexture;
        private TreeViewState m_VisualTreeTreeViewState;
        private VisualTreeTreeView m_VisualTreeTreeView;
        private static readonly PropertyInfo[] k_FieldInfos = typeof(IStyle).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly PropertyInfo[] k_SortedFieldInfos = k_FieldInfos.OrderBy(f => f.Name).ToArray();

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
            VisualElement e = m_CurPanel.Value.Panel.Pick(ev.mousePosition);
            if (e != null)
                ((PanelDebug)m_CurPanel.Value.Panel.panelDebug).highlightedElement = e.controlid;

            // stop intercepting events
            if (ev.clickCount > 0 && ev.button == 0)
            {
                m_CurPanel.Value.Panel.panelDebug.interceptEvents = null;
                m_PickingElementInPanel = false;
                m_VisualTreeTreeView.ExpandAll();
                VisualTreeItem node = m_VisualTreeTreeView.GetRows()
                    .OfType<VisualTreeItem>()
                    .FirstOrDefault(vti => e != null && vti.elt.controlid == e.controlid);
                if (node != null)
                    m_VisualTreeTreeView.SetSelection(new List<int> { node.id }, TreeViewSelectionOptions.RevealAndFrame);
            }
            return true;
        }

        public void OnGUI()
        {
            if (m_ScheduleRestoreSelection)
            {
                m_ScheduleRestoreSelection = false;
                if (m_PickingData.TryRestoreSelectedWindow(m_LastWindowTitle))
                {
                    EndPicking(m_PickingData.Selected);
                    m_VisualTreeTreeView.ExpandAll();
                }
                else
                    m_LastWindowTitle = String.Empty;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));
            bool refresh = false;

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
                bool newPickingElementInPanel = GUILayout.Toggle(m_PickingElementInPanel, Styles.pickElementInPanelContent, EditorStyles.toolbarButton);
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
                float column2Width = position.width - column1Width;
                Rect column1Rect = new Rect(0, EditorGUI.kWindowToolbarHeight, column1Width, position.height - EditorGUI.kWindowToolbarHeight);
                Rect column2Rect = new Rect(column1Width, EditorGUI.kWindowToolbarHeight, column2Width, column1Rect.height);

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
            Dictionary<int, Panel>.Enumerator it = UIElementsUtility.GetPanelsIterator();
            while (it.MoveNext())
                it.Current.Value.panelDebug = null;
            m_CurPanel = viewPanel;
            if (m_CurPanel.HasValue)
            {
                m_LastWindowTitle = PickingData.GetName(m_CurPanel.Value);

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
            Event evt = Event.current;
            if (evt.type == EventType.Layout)
                CacheData();

            if (m_SelectedElement == null)
                return;

            GUILayout.BeginArea(rect);

            EditorGUILayout.LabelField(m_SelectedElement.GetType().Name, Styles.KInspectorTitle);

            m_DetailScroll = EditorGUILayout.BeginScrollView(m_DetailScroll);

            Rect sizeRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(Styles.SizeRectHeight));
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
                m_ClassList = null;

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
                m_ClassList = null;
            }

            // element picking uses the highlight
            if (!m_PickingElementInPanel)
                ((PanelDebug)m_CurPanel.Value.Panel.panelDebug).highlightedElement = element.controlid;
            GetElementMatchers();
        }

        private static MatchedRulesExtractor s_MatchedRulesExtractor = new MatchedRulesExtractor();
        private string m_SelectedElementUxml;
        private ReorderableList m_ClassList;
        private string m_NewClass;

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

            m_SelectedElement.name = EditorGUILayout.TextField("Name", m_SelectedElement.name);
            m_SelectedElement.text = EditorGUILayout.TextField("Text", m_SelectedElement.text);
            m_SelectedElement.clippingOptions = (VisualElement.ClippingOptions)EditorGUILayout.EnumPopup("Clipping Option", m_SelectedElement.clippingOptions);
            m_SelectedElement.visible = EditorGUILayout.Toggle("Visible", m_SelectedElement.visible);
            EditorGUILayout.LabelField("Layout", m_SelectedElement.layout.ToString());
            EditorGUILayout.LabelField("World Bound", m_SelectedElement.worldBound.ToString());

            if (m_ClassList == null)
                InitClassList();
            m_ClassList.DoLayoutList();

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            m_DetailFilter = EditorGUILayout.ToolbarSearchField(m_DetailFilter);
            m_ShowDefaults = GUILayout.Toggle(m_ShowDefaults, Styles.showDefaultsContent, EditorStyles.toolbarButton);
            m_Sort = GUILayout.Toggle(m_Sort, Styles.sortContent, EditorStyles.toolbarButton);
            GUILayout.EndHorizontal();

            VisualElementStylesData styles = m_SelectedElement.effectiveStyle;
            bool anyChanged = false;

            foreach (PropertyInfo field in m_Sort ? k_SortedFieldInfos : k_FieldInfos)
            {
                if (!string.IsNullOrEmpty(m_DetailFilter) &&
                    field.Name.IndexOf(m_DetailFilter, StringComparison.InvariantCultureIgnoreCase) == -1)
                    continue;

                if (!field.PropertyType.IsGenericType || field.PropertyType.GetGenericTypeDefinition() != typeof(StyleValue<>))
                    continue;

                object val = field.GetValue(m_SelectedElement, null);
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                int specificity;

                if (val is StyleValue<float>)
                {
                    StyleValue<float> style = (StyleValue<float>)val;
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
                    StyleValue<int> style = (StyleValue<int>)val;
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
                    StyleValue<bool> style = (StyleValue<bool>)val;
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
                    StyleValue<Color> style = (StyleValue<Color>)val;
                    specificity = GetSpecificity(style);
                    if (m_ShowDefaults || specificity > 0)
                    {
                        style.specificity = Int32.MaxValue;
                        style.value = EditorGUILayout.ColorField(field.Name, ((StyleValue<Color>)val).value);
                        val = style;
                    }
                }
                else if (val is StyleValue<Font>)
                {
                    specificity = HandleReferenceProperty<Font>(field, ref val);
                }
                else if (val is StyleValue<Texture2D>)
                {
                    specificity = HandleReferenceProperty<Texture2D>(field, ref val);
                }
                else
                {
                    Type type = val.GetType();
                    if (type.GetGenericArguments()[0].IsEnum)
                    {
                        specificity = (int)type.GetField("specificity", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(val);
                        if (m_ShowDefaults || specificity > 0)
                        {
                            FieldInfo fieldInfo = type.GetField("value");
                            Enum enumValue = fieldInfo.GetValue(val) as Enum;
                            Enum newEnumValue = EditorGUILayout.EnumPopup(field.Name, enumValue);
                            if (!Equals(enumValue, newEnumValue))
                                fieldInfo.SetValue(val, newEnumValue);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField(field.Name, val == null ? "null" : val.ToString());
                        specificity = -1;
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    anyChanged = true;
                    field.SetValue(m_SelectedElement, val, null);
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

        private void InitClassList()
        {
            Action refresh = () => m_ClassList.list = m_SelectedElement.GetClasses().ToList();
            m_ClassList = new ReorderableList(m_SelectedElement.GetClasses().ToList(), typeof(string), false, true, true, true);
            m_ClassList.onRemoveCallback = _ =>
                {
                    m_SelectedElement.RemoveFromClassList((string)m_ClassList.list[m_ClassList.index]);
                    refresh();
                };
            m_ClassList.drawHeaderCallback = r =>
                {
                    r.width /= 2;
                    EditorGUI.LabelField(r, "Classes");
                    r.x += r.width;
                    m_NewClass = EditorGUI.TextField(r, m_NewClass);
                };
            m_ClassList.onCanAddCallback = _ => !String.IsNullOrEmpty(m_NewClass) && !m_SelectedElement.ClassListContains(m_NewClass);
            m_ClassList.onAddCallback = _ =>
                {
                    m_SelectedElement.AddToClassList(m_NewClass);
                    m_NewClass = "";
                    refresh();
                };
        }

        private int HandleReferenceProperty<T>(PropertyInfo field, ref object val) where T : UnityEngine.Object
        {
            StyleValue<T> style = (StyleValue<T>)val;
            int specificity = GetSpecificity(style);
            if (m_ShowDefaults || specificity > 0)
            {
                style.specificity = Int32.MaxValue;
                style.value = EditorGUILayout.ObjectField(field.Name, ((StyleValue<T>)val).value, typeof(T), false) as T;
                val = style;
            }
            return specificity;
        }

        private void DrawMatchingRules()
        {
            if (s_MatchedRulesExtractor.selectedElementStylesheets != null && s_MatchedRulesExtractor.selectedElementStylesheets.Count > 0)
            {
                EditorGUILayout.LabelField(Styles.stylesheetsContent, Styles.KInspectorTitle);
                foreach (string sheet in s_MatchedRulesExtractor.selectedElementStylesheets)
                {
                    if (GUILayout.Button(sheet))
                        InternalEditorUtility.OpenFileAtLineExternal(sheet, 0);
                }
            }

            if (s_MatchedRulesExtractor.selectedElementRules != null && s_MatchedRulesExtractor.selectedElementRules.Count > 0)
            {
                EditorGUILayout.LabelField(Styles.selectorsContent, Styles.KInspectorTitle);
                int i = 0;
                foreach (MatchedRulesExtractor.MatchedRule rule in s_MatchedRulesExtractor.selectedElementRules)
                {
                    StringBuilder builder = new StringBuilder();
                    for (int j = 0; j < rule.ruleMatcher.complexSelector.selectors.Length; j++)
                    {
                        StyleSelector sel = rule.ruleMatcher.complexSelector.selectors[j];
                        switch (sel.previousRelationship)
                        {
                            case StyleSelectorRelationship.Child:
                                builder.Append(" ");
                                break;
                            case StyleSelectorRelationship.Descendent:
                                builder.Append(" > ");
                                break;
                        }
                        for (int k = 0; k < sel.parts.Length; k++)
                        {
                            StyleSelectorPart part = sel.parts[k];
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

                    StyleProperty[] props = rule.ruleMatcher.complexSelector.rule.properties;
                    bool expanded = m_CurFoldout.Contains(i);
                    EditorGUILayout.BeginHorizontal();
                    bool foldout = EditorGUILayout.Foldout(m_CurFoldout.Contains(i), new GUIContent(builder.ToString()), true);
                    if (rule.displayPath != null && GUILayout.Button(rule.displayPath, EditorStyles.miniButton, GUILayout.MaxWidth(150)))
                        InternalEditorUtility.OpenFileAtLineExternal(rule.fullPath, rule.lineNumber);
                    EditorGUILayout.EndHorizontal();

                    if (expanded && !foldout)
                        m_CurFoldout.Remove(i);
                    else if (!expanded && foldout)
                        m_CurFoldout.Add(i);

                    if (foldout)
                    {
                        EditorGUI.indentLevel++;
                        for (int j = 0; j < props.Length; j++)
                        {
                            string s = rule.ruleMatcher.sheet.ReadAsString(props[j].values[0]);
                            EditorGUILayout.LabelField(new GUIContent(props[j].name), new GUIContent(s));
                        }

                        EditorGUI.indentLevel--;
                    }
                    i++;
                }
            }
        }

        private void DrawSize(Rect rect, VisualElement element)
        {
            Rect cursor = new Rect(rect);
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
            Rect labelCursor = new Rect(
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

            if (!String.IsNullOrEmpty(m_LastWindowTitle))
            {
                // if the previous window is selected again and displayed too early, we might miss some elements added
                // during the window's OnEnable, so delay that
                m_ScheduleRestoreSelection = true;
            }
        }

        public void OnDisable()
        {
            Dictionary<int, Panel>.Enumerator it = UIElementsUtility.GetPanelsIterator();
            while (it.MoveNext())
                it.Current.Value.panelDebug = null;
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

            Rect cursor = new Rect(rect.x, rect.y, rect.width, borderSize);
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
            public static readonly GUIContent sortContent = new GUIContent("Sort");
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

        internal HashSet<MatchedRule> selectedElementRules = new HashSet<MatchedRule>(MatchedRule.lineNumberFullPathComparer);
        internal HashSet<string> selectedElementStylesheets = new HashSet<string>();

        private VisualElement m_Target;
        private List<VisualElement> m_Hierarchy = new List<VisualElement>();
        private int m_Index;

        internal struct MatchedRule
        {
            public readonly RuleMatcher ruleMatcher;
            public readonly string displayPath;
            public readonly int lineNumber;
            public readonly string fullPath;

            public MatchedRule(RuleMatcher ruleMatcher)
                : this()
            {
                this.ruleMatcher = ruleMatcher;
                fullPath = AssetDatabase.GetAssetPath(ruleMatcher.sheet);
                lineNumber = ruleMatcher.complexSelector.rule.line;
                if (fullPath != null)
                {
                    if (fullPath == "Library/unity editor resources")
                        displayPath = ruleMatcher.sheet.name + ":" + lineNumber;
                    else
                        displayPath = Path.GetFileNameWithoutExtension(fullPath) + ":" + lineNumber;
                }
            }

            private sealed class LineNumberFullPathEqualityComparer : IEqualityComparer<MatchedRule>
            {
                public bool Equals(MatchedRule x, MatchedRule y)
                {
                    return x.lineNumber == y.lineNumber && string.Equals(x.fullPath, y.fullPath);
                }

                public int GetHashCode(MatchedRule obj)
                {
                    unchecked
                    {
                        return (obj.lineNumber * 397) ^ (obj.fullPath != null ? obj.fullPath.GetHashCode() : 0);
                    }
                }
            }

            public static IEqualityComparer<MatchedRule> lineNumberFullPathComparer = new LineNumberFullPathEqualityComparer();
        }

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
                        foreach (StyleSheet sheet in cursor.styleSheets)
                        {
                            selectedElementStylesheets.Add(AssetDatabase.GetAssetPath(sheet) ?? "<unknown>");
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
            StyleComplexSelector[] complexSelectors = styleSheetData.complexSelectors;
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
                selectedElementRules.Add(new MatchedRule(matcher));
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
