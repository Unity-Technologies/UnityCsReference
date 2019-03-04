// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using Cursor = UnityEngine.UIElements.Cursor;

namespace UnityEditor.UIElements.Debugger
{
    internal class StylesDebugger : VisualElement
    {
        private DebuggerSelection m_DebuggerSelection;
        private ScrollView m_ScrollView;
        private BoxModelView m_BoxModelView;
        private IMGUIContainer m_IMGUIStylesDebugger;

        private IPanelDebug m_PanelDebug;
        private VisualElement selectedElement
        {
            get
            {
                return m_SelectedElement;
            }
            set
            {
                if (m_SelectedElement == value)
                    return;

                m_SelectedElement = value;

                m_BoxModelView.selectedElement = m_SelectedElement;
                m_SelectedElementUxml = null;
                m_ClassList = null;

                m_IMGUIStylesDebugger.IncrementVersion(VersionChangeType.Layout);
                GetElementMatchers();
            }
        }

        public StylesDebugger(DebuggerSelection debuggerSelection)
        {
            m_DebuggerSelection = debuggerSelection;
            m_DebuggerSelection.onPanelDebugChanged += pdbg => m_PanelDebug = pdbg;
            m_DebuggerSelection.onSelectedElementChanged += element => selectedElement = element;

            m_PanelDebug = m_DebuggerSelection.panelDebug;
            selectedElement = m_DebuggerSelection.element;

            m_ScrollView = new ScrollView();

            m_BoxModelView = new BoxModelView();
            m_ScrollView.Add(m_BoxModelView);

            m_IMGUIStylesDebugger = new IMGUIContainer(OnGUI);
            m_ScrollView.Add(m_IMGUIStylesDebugger);
            Add(m_ScrollView);
        }

        // ---

        private HashSet<int> m_CurFoldout = new HashSet<int>();

        private string m_DumpId = "dump";
        private bool m_UxmlDumpExpanded;
        private bool m_UxmlDumpStyleFields;
        private bool m_NewLineOnAttributes;
        private bool m_AutoNameElements;

        private string m_DetailFilter = string.Empty;
        private VisualElement m_SelectedElement;

        private bool m_ShowAll;
        private bool m_Sort;

        private static readonly PropertyInfo[] k_FieldInfos = typeof(ComputedStyle).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        private static readonly PropertyInfo[] k_SortedFieldInfos = k_FieldInfos.OrderBy(f => f.Name).ToArray();

        public void OnGUI()
        {
            if (m_PanelDebug == null || selectedElement == null)
                return;

            DrawSelection();
        }

        public void Refresh()
        {
            m_BoxModelView.Refresh();
        }

        private void DrawSelection()
        {
            if (m_SelectedElement == null)
                return;

            DrawUxmlDump(m_SelectedElement);
            DrawMatchingRules();
            DrawProperties();
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
            s_MatchedRulesExtractor.FindMatchingRules(m_SelectedElement);
        }

        private void DrawProperties()
        {
            EditorGUILayout.LabelField(Styles.elementStylesContent, Styles.KInspectorTitle);

            m_SelectedElement.name = EditorGUILayout.TextField("Name", m_SelectedElement.name);
            EditorGUILayout.LabelField("Debug Id", m_SelectedElement.controlid.ToString());
            var textElement = m_SelectedElement as ITextElement;
            if (textElement != null)
            {
                textElement.text = EditorGUILayout.TextField("Text", textElement.text);
            }

            bool cacheContents = EditorGUILayout.Toggle("Cache Contents", m_SelectedElement.cacheAsBitmap);
            m_SelectedElement.cacheAsBitmap = cacheContents;
            if (m_SelectedElement.cacheAsBitmap && m_SelectedElement.computedStyle.overflow.value == Overflow.Visible)
            {
                EditorGUILayout.HelpBox("Bitmap caching will be ignored for this element because it's not clipped", MessageType.Warning);
            }

            m_SelectedElement.pickingMode = (PickingMode)EditorGUILayout.EnumPopup("Picking Mode", m_SelectedElement.pickingMode);

            if (m_SelectedElement.pseudoStates != 0)
            {
                EditorGUILayout.LabelField("Pseudo States", m_SelectedElement.pseudoStates.ToString());
            }
            else
            {
                EditorGUILayout.LabelField("Pseudo States", "None");
            }

            EditorGUILayout.LabelField("Focusable", m_SelectedElement.focusable.ToString());

            EditorGUILayout.LabelField("Layout", m_SelectedElement.layout.ToString());
            EditorGUILayout.LabelField("World Bound", m_SelectedElement.worldBound.ToString());
            EditorGUILayout.LabelField("World Clip", m_SelectedElement.worldClip.ToString());
            EditorGUILayout.LabelField("Bounding Box", m_SelectedElement.boundingBox.ToString());

            if (m_ClassList == null)
                InitClassList();
            m_ClassList.DoLayoutList();

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            m_DetailFilter = EditorGUILayout.ToolbarSearchField(m_DetailFilter);
            m_ShowAll = GUILayout.Toggle(m_ShowAll, Styles.showAllContent, EditorStyles.toolbarButton);
            m_Sort = GUILayout.Toggle(m_Sort, Styles.sortContent, EditorStyles.toolbarButton);
            GUILayout.EndHorizontal();

            var customProperties = m_SelectedElement.specifiedStyle.m_CustomProperties;
            bool anyChanged = false;

            if (customProperties != null && customProperties.Any())
            {
                foreach (KeyValuePair<string, CustomPropertyHandle> customProperty in customProperties)
                {
                    foreach (StyleValueHandle handle in customProperty.Value.handles)
                    {
                        EditorGUILayout.LabelField(customProperty.Key, customProperty.Value.data.ReadAsString(handle));
                    }
                }
            }

            foreach (PropertyInfo field in m_Sort ? k_SortedFieldInfos : k_FieldInfos)
            {
                if (!string.IsNullOrEmpty(m_DetailFilter) &&
                    field.Name.IndexOf(m_DetailFilter, StringComparison.InvariantCultureIgnoreCase) == -1)
                    continue;

                object val = field.GetValue(m_SelectedElement.computedStyle, null);
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                int specificity;

                if (val is StyleFloat)
                {
                    StyleFloat style = (StyleFloat)val;
                    specificity = style.specificity;
                    if (m_ShowAll || specificity != StyleValueExtensions.UndefinedSpecificity)
                    {
                        style.specificity = Int32.MaxValue;
                        style.value = EditorGUILayout.FloatField(field.Name, ((StyleFloat)val).value);
                        val = style;
                    }
                }
                else if (val is StyleInt)
                {
                    StyleInt style = (StyleInt)val;
                    specificity = style.specificity;
                    if (m_ShowAll || specificity != StyleValueExtensions.UndefinedSpecificity)
                    {
                        style.specificity = Int32.MaxValue;
                        style.value = EditorGUILayout.IntField(field.Name, ((StyleInt)val).value);
                        val = style;
                    }
                }
                else if (val is StyleLength)
                {
                    StyleLength style = (StyleLength)val;
                    specificity = style.specificity;
                    if (m_ShowAll || specificity != StyleValueExtensions.UndefinedSpecificity)
                    {
                        style.specificity = Int32.MaxValue;
                        style.value = EditorGUILayout.FloatField(field.Name, ((StyleLength)val).value.value);
                        val = style;
                    }
                }
                else if (val is StyleColor)
                {
                    StyleColor style = (StyleColor)val;
                    specificity = style.specificity;
                    if (m_ShowAll || specificity != StyleValueExtensions.UndefinedSpecificity)
                    {
                        style.specificity = Int32.MaxValue;
                        style.value = EditorGUILayout.ColorField(field.Name, ((StyleColor)val).value);
                        val = style;
                    }
                }
                else if (val is StyleFont)
                {
                    specificity = HandleReferenceProperty<Font>(field, ref val);
                }
                else if (val is StyleBackground)
                {
                    StyleBackground style = (StyleBackground)val;
                    specificity = style.specificity;
                    if (m_ShowAll || specificity != StyleValueExtensions.UndefinedSpecificity)
                    {
                        style.specificity = Int32.MaxValue;
                        Texture2D t = EditorGUILayout.ObjectField(field.Name, style.value.texture, typeof(Texture2D), false) as Texture2D;
                        style.value = new Background(t);
                        val = style;
                    }
                }
                else if (val is StyleCursor)
                {
                    StyleCursor style = (StyleCursor)val;
                    specificity = style.specificity;
                    if (m_ShowAll || specificity != StyleValueExtensions.UndefinedSpecificity)
                    {
                        if (style.value.texture != null)
                        {
                            style.specificity = Int32.MaxValue;
                            var texture = EditorGUILayout.ObjectField(field.Name + "'s texture2D", style.value.texture, typeof(Texture2D), false) as Texture2D;
                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();
                            EditorGUIUtility.wideMode = true;
                            var hotspot = EditorGUILayout.Vector2Field(field.Name + "'s hotspot", style.value.hotspot);

                            style.value = new Cursor() { texture = texture, hotspot = hotspot };
                            val = style;
                        }
                        else
                        {
                            int mouseId = style.value.defaultCursorId;
                            Enum newEnumValue = EditorGUILayout.EnumPopup(field.Name, (MouseCursor)mouseId);

                            int toCompare = Convert.ToInt32(newEnumValue);
                            if (!Equals(mouseId, toCompare))
                            {
                                style.specificity = Int32.MaxValue;
                                style.value = new Cursor() { defaultCursorId = toCompare };
                                val = style;
                            }
                        }
                    }
                }
                else
                {
                    Type type = val.GetType();
                    if (type.IsGenericType && type.GetGenericArguments()[0].IsEnum)
                    {
                        specificity = (int)type.GetProperty("specificity", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(val, null);
                        if (m_ShowAll || specificity != StyleValueExtensions.UndefinedSpecificity)
                        {
                            var propInfo = type.GetProperty("value");
                            Enum enumValue = propInfo.GetValue(val, null) as Enum;
                            Enum newEnumValue = EditorGUILayout.EnumPopup(field.Name, enumValue);
                            if (!Equals(enumValue, newEnumValue))
                                propInfo.SetValue(val, newEnumValue, null);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField(field.Name, val == null ? "null" : val.ToString());
                        specificity = StyleValueExtensions.UndefinedSpecificity;
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    anyChanged = true;
                    string propertyName = field.Name;
                    var inlineStyle = typeof(IStyle).GetProperty(propertyName);
                    inlineStyle.SetValue(m_SelectedElement.style, val, null);
                }

                if (m_ShowAll || specificity != StyleValueExtensions.UndefinedSpecificity)
                {
                    string specificityString = "";
                    switch (specificity)
                    {
                        case StyleValueExtensions.UnitySpecificity:
                            specificityString = "unity stylesheet";
                            break;
                        case StyleValueExtensions.InlineSpecificity:
                            specificityString = "inline";
                            break;
                        case StyleValueExtensions.UndefinedSpecificity:
                            break;
                        default:
                            specificityString = specificity.ToString();
                            break;
                    }
                    GUILayout.Label(specificityString, GUILayout.MinWidth(200), GUILayout.ExpandWidth(false));
                }

                EditorGUILayout.EndHorizontal();
            }

            if (anyChanged)
            {
                m_PanelDebug.visualTree.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet |
                    VersionChangeType.Layout | VersionChangeType.Transform | VersionChangeType.Repaint);
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
            IStyleValue<T> style = (IStyleValue<T>)val;
            int specificity = style.specificity;
            if (m_ShowAll || specificity != StyleValueExtensions.UndefinedSpecificity)
            {
                style.specificity = Int32.MaxValue;
                style.value = EditorGUILayout.ObjectField(field.Name, ((IStyleValue<T>)val).value, typeof(T), false) as T;
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
                    if (GUILayout.Button(sheet) && CanOpenStyleSheet(sheet))
                        InternalEditorUtility.OpenFileAtLineExternal(sheet, 0, 0);
                }
            }

            if (s_MatchedRulesExtractor.selectedElementRules != null && s_MatchedRulesExtractor.selectedElementRules.Count > 0)
            {
                EditorGUILayout.LabelField(Styles.selectorsContent, Styles.KInspectorTitle);
                int i = 0;
                foreach (MatchedRulesExtractor.MatchedRule rule in s_MatchedRulesExtractor.selectedElementRules)
                {
                    StringBuilder builder = new StringBuilder();
                    for (int j = 0; j < rule.matchRecord.complexSelector.selectors.Length; j++)
                    {
                        StyleSelector sel = rule.matchRecord.complexSelector.selectors[j];
                        switch (sel.previousRelationship)
                        {
                            case StyleSelectorRelationship.Child:
                                builder.Append(" > ");
                                break;
                            case StyleSelectorRelationship.Descendent:
                                builder.Append(" ");
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
                                case StyleSelectorType.Wildcard:
                                    break;
                            }
                            builder.Append(part.value);
                        }
                    }

                    StyleProperty[] props = rule.matchRecord.complexSelector.rule.properties;
                    bool expanded = m_CurFoldout.Contains(i);
                    EditorGUILayout.BeginHorizontal();
                    bool foldout = EditorGUILayout.Foldout(m_CurFoldout.Contains(i), new GUIContent(builder.ToString()), true);
                    if (rule.displayPath != null && GUILayout.Button(rule.displayPath, EditorStyles.miniButton, GUILayout.MaxWidth(150)) && CanOpenStyleSheet(rule.fullPath))
                        InternalEditorUtility.OpenFileAtLineExternal(rule.fullPath, rule.lineNumber, -1);
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
                            string s = "";
                            for (int k = 0; k < props[j].values.Length; k++)
                            {
                                if (k > 0)
                                    s += " ";

                                s += rule.matchRecord.sheet.ReadAsString(props[j].values[k]);
                            }

                            s = s.ToLower();
                            EditorGUILayout.LabelField(new GUIContent(props[j].name), new GUIContent(s));
                        }

                        EditorGUI.indentLevel--;
                    }
                    i++;
                }
            }
        }

        static bool CanOpenStyleSheet(string path) => File.Exists(path);

        internal static class Styles
        {
            public static GUIStyle KInspectorTitle = "WhiteLargeCenterLabel";

            public static readonly GUIContent elementStylesContent = EditorGUIUtility.TrTextContent("Element styles");
            public static readonly GUIContent showAllContent = EditorGUIUtility.TrTextContent("Show all");
            public static readonly GUIContent sortContent = EditorGUIUtility.TrTextContent("Sort");
            public static readonly GUIContent uxmlContent = EditorGUIUtility.TrTextContent("UXML Dump");
            public static readonly GUIContent stylesheetsContent = EditorGUIUtility.TrTextContent("Stylesheets");
            public static readonly GUIContent selectorsContent = EditorGUIUtility.TrTextContent("Matching Selectors");
        }
    }

    internal class MatchedRulesExtractor
    {
        internal HashSet<MatchedRule> selectedElementRules = new HashSet<MatchedRule>(MatchedRule.lineNumberFullPathComparer);
        internal HashSet<string> selectedElementStylesheets = new HashSet<string>();

        internal struct MatchedRule
        {
            public readonly SelectorMatchRecord matchRecord;
            public readonly string displayPath;
            public readonly int lineNumber;
            public readonly string fullPath;

            public MatchedRule(SelectorMatchRecord matchRecord)
                : this()
            {
                this.matchRecord = matchRecord;
                fullPath = AssetDatabase.GetAssetPath(matchRecord.sheet);
                lineNumber = matchRecord.complexSelector.rule.line;
                if (string.IsNullOrEmpty(fullPath))
                {
                    displayPath = matchRecord.sheet.name + ":" + lineNumber;
                }
                else
                {
                    if (fullPath == "Library/unity editor resources")
                        displayPath = matchRecord.sheet.name + ":" + lineNumber;
                    else
                        displayPath = Path.GetFileName(fullPath) + ":" + lineNumber;
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

        private void FindStyleSheets(VisualElement cursor, StyleMatchingContext matchingContext)
        {
            if (cursor.hierarchy.parent != null)
                FindStyleSheets(cursor.hierarchy.parent, matchingContext);

            if (cursor.styleSheetList == null)
                return;

            foreach (StyleSheet sheet in cursor.styleSheetList)
            {
                string path = AssetDatabase.GetAssetPath(sheet);
                selectedElementStylesheets.Add(string.IsNullOrEmpty(path) ? sheet.name : path);
                matchingContext.styleSheetStack.Add(sheet);
            }
        }

        public void FindMatchingRules(VisualElement target)
        {
            var matchingContext = new StyleMatchingContext((element, info) => {}) { currentElement = target };
            FindStyleSheets(target, matchingContext);

            List<SelectorMatchRecord> matches = new List<SelectorMatchRecord>();
            StyleSelectorHelper.FindMatches(matchingContext, matches);

            matches.Sort(SelectorMatchRecord.Compare);

            foreach (var record in matches)
            {
                selectedElementRules.Add(new MatchedRule(record));
            }
        }
    }
}
