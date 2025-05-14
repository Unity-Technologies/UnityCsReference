// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Properties;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.UIElements;
using UnityEngine.Bindings;

namespace UnityEditor.UIElements.Debugger
{
    internal class StylesDebugger : VisualElement
    {
        private DebuggerSelection m_DebuggerSelection;
        private BoxModelView m_BoxModelView;
        private StylePropertyDebugger m_StylePropertyDebugger;
        private IMGUIContainer m_MatchingRulesContainer;

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
                m_ClassList = null;

                this.Query<IMGUIContainer>().ForEach( i => i.IncrementVersion(VersionChangeType.Layout));
                UpdateMatches();
            }
        }

        //Used by StylePropertyDebugger to return to a "not Inline" style
        public void UpdateMatches()
        {
            GetElementMatchers();
            m_StylePropertyDebugger.SetMatchRecords(m_SelectedElement, m_MatchedRulesExtractor.matchRecords);
            m_MatchingRulesContainer.MarkDirtyLayout();
        }

        public StylesDebugger(DebuggerSelection debuggerSelection)
        {
            m_DebuggerSelection = debuggerSelection;
            m_DebuggerSelection.onPanelDebugChanged += pdbg => m_PanelDebug = pdbg;
            m_DebuggerSelection.onSelectedElementChanged += element => selectedElement = element;

            m_PanelDebug = m_DebuggerSelection.panelDebug;
            selectedElement = m_DebuggerSelection.element;

            Foldout layoutInfo = new() { text = "Layout", viewDataKey = "layoutInfo"};
            layoutInfo.contentContainer.style.flexDirection = FlexDirection.Row;
            layoutInfo.contentContainer.style.flexWrap = Wrap.Wrap;
            layoutInfo.contentContainer.style.alignItems = Align.Center;
            layoutInfo.contentContainer.style.alignContent = Align.Center;
            layoutInfo.contentContainer.style.justifyContent = Justify.SpaceAround;
            m_BoxModelView = new BoxModelView();
            layoutInfo.Add(m_BoxModelView);
            layoutInfo.Add(new IMGUIContainer(DrawLayoutInfo) { style = { flexShrink = 0, minWidth = 420 } });
            Add(layoutInfo);

            Add(m_MatchingRulesContainer = new IMGUIContainer(DrawMatchingRules));

            Add(new IMGUIContainer(DrawProperties));

            Foldout StylesInfo = new() { text = "Styles", viewDataKey = "StylesInfo" };
            m_StylePropertyDebugger = new StylePropertyDebugger(selectedElement);
            StylesInfo.Add(m_StylePropertyDebugger);
            Add(StylesInfo);
        }

        // ---

        private HashSet<int> m_CurFoldout = new HashSet<int>();

        private VisualElement m_SelectedElement;

        public void RefreshStylePropertyDebugger()
        {
            m_StylePropertyDebugger.Refresh();
        }

        public void RefreshBoxModelView(MeshGenerationContext mgc)
        {
            m_BoxModelView.Refresh(mgc);
        }

        private MatchedRulesExtractor m_MatchedRulesExtractor = new MatchedRulesExtractor();
        private ReorderableList m_ClassList;
        private string m_NewClass;

        private void GetElementMatchers()
        {
            if (m_SelectedElement == null || m_SelectedElement.elementPanel == null)
                return;
            m_MatchedRulesExtractor.selectedElementRules.Clear();
            m_MatchedRulesExtractor.selectedElementStylesheets.Clear();
            m_MatchedRulesExtractor.FindMatchingRules(m_SelectedElement);
        }

        private void DrawLayoutInfo()
        {
            if (m_PanelDebug == null || selectedElement == null)
                return;
            EditorGUILayout.LabelField("World Bound", m_SelectedElement.worldBound.ToString());
            EditorGUILayout.LabelField("World Clip", m_SelectedElement.worldClip.ToString());
            EditorGUILayout.LabelField("Bounding Box", m_SelectedElement.boundingBox.ToString());

            EditorGUILayout.LabelField("Layout", m_SelectedElement.layout.ToString());
            EditorGUILayout.LabelField("LastLayout", m_SelectedElement.lastLayout.ToString());
        }

        private void DrawProperties()
        {
            if (m_PanelDebug == null || m_SelectedElement == null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.elementStylesContent, Styles.KInspectorTitle);

            m_SelectedElement.name = EditorGUILayout.TextField("Name", m_SelectedElement.name);
            m_SelectedElement.tooltip = EditorGUILayout.TextField("Tooltip", m_SelectedElement.tooltip);
            var textElement = m_SelectedElement as TextElement;
            if (textElement != null)
            {
                EditorGUILayout.BeginHorizontal();
                textElement.text = EditorGUILayout.TextField("Text", textElement.text);

                if (Unsupported.IsDeveloperMode())
                {
                    string unicodeSequence = "";
                    if (!string.IsNullOrEmpty(textElement.text))
                            unicodeSequence = string.Join(", ", textElement.text.Select(c => $"U+{((int)c):X4}"));
                    GUILayout.Label("Unicode:", GUILayout.Width(55));
                    EditorGUILayout.SelectableLabel(
                        unicodeSequence,
                        EditorStyles.textField,
                        GUILayout.Height(EditorGUIUtility.singleLineHeight),
                        GUILayout.MinWidth(100)
                    );
                }
                EditorGUILayout.EndHorizontal();
            }


            m_SelectedElement.viewDataKey = EditorGUILayout.TextField("View Data Key", m_SelectedElement.viewDataKey);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Data Source (type)", null == m_SelectedElement.dataSource ? "<none>" : TypeUtility.GetTypeDisplayName(m_SelectedElement.dataSource.GetType()));
                EditorGUILayout.TextField("Data Source Path", m_SelectedElement.dataSourcePath.ToString());
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

            if (m_SelectedElement is IBindable bindableElement)
            {
                using (new EditorGUI.DisabledScope(true))
                    bindableElement.bindingPath = EditorGUILayout.TextField("Binding Path", bindableElement.bindingPath);
            }

            m_SelectedElement.usageHints = (UsageHints)EditorGUILayout.EnumFlagsField("Usage Hints", m_SelectedElement.usageHints);
            m_SelectedElement.tabIndex = EditorGUILayout.IntField("Tab Index", m_SelectedElement.tabIndex);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Increment Version");
            if (GUILayout.Button("Repaint"))
            {
                m_SelectedElement.IncrementVersion(VersionChangeType.Repaint);
            }

            if (Unsupported.IsDeveloperBuild())
            {
                if (GUILayout.Button("Size"))
                {
                    m_SelectedElement.IncrementVersion(VersionChangeType.Size);
                }
                if (GUILayout.Button("Transform"))
                {
                    m_SelectedElement.IncrementVersion(VersionChangeType.Transform);
                }
            }
            EditorGUILayout.EndHorizontal();


            if (m_ClassList == null)
                InitClassList();
            m_ClassList.DoLayoutList();
        }

        private void InitClassList()
        {
            Action refresh = () => m_ClassList.list = m_SelectedElement.GetClassesForIteration().ToList();
            m_ClassList = new ReorderableList(m_SelectedElement.GetClassesForIteration().ToList(), typeof(string), false, true, true, true);
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

        bool showStylesheet = false;
        bool showSelectors = false;
        private void DrawMatchingRules()
        {
            if (m_PanelDebug == null || m_SelectedElement == null)
                return;

            if (m_MatchedRulesExtractor.selectedElementStylesheets != null && m_MatchedRulesExtractor.selectedElementStylesheets.Count > 0)
            {
                showStylesheet = EditorGUILayout.Foldout(showStylesheet, Styles.stylesheetsContent);
                if (showStylesheet)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginVertical();
                    foreach (string sheet in m_MatchedRulesExtractor.selectedElementStylesheets)
                    {
                        if (GUILayout.Button(sheet) && CanOpenStyleSheet(sheet))
                            InternalEditorUtility.OpenFileAtLineExternal(sheet, 0, 0);
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (m_MatchedRulesExtractor.selectedElementRules != null && m_MatchedRulesExtractor.selectedElementRules.Count > 0)
            {

                showSelectors = EditorGUILayout.Foldout(showSelectors, Styles.selectorsContent);
                if (showSelectors)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginVertical();
                    int i = 0;
                    foreach (var rule in m_MatchedRulesExtractor.selectedElementRules)
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
                        if (rule.displayPath != null && GUILayout.Button(rule.displayPath, EditorStyles.miniButton, GUILayout.MaxWidth(250)) && CanOpenStyleSheet(rule.fullPath))
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
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        static bool CanOpenStyleSheet(string path) => File.Exists(path);

        internal static class Styles
        {
            public static GUIStyle KInspectorTitle = new GUIStyle("In TitleText") { alignment = TextAnchor.MiddleCenter };

            public static readonly GUIContent elementStylesContent = EditorGUIUtility.TrTextContent("Element styles");
            public static readonly GUIContent uxmlContent = EditorGUIUtility.TrTextContent("UXML Dump");
            public static readonly GUIContent stylesheetsContent = EditorGUIUtility.TrTextContent("Stylesheets");
            public static readonly GUIContent selectorsContent = EditorGUIUtility.TrTextContent("Matching Selectors");
        }
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = matchRecord.GetHashCode();
                hashCode = (hashCode * 397) ^ (displayPath != null ? displayPath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ lineNumber;
                hashCode = (hashCode * 397) ^ (fullPath != null ? fullPath.GetHashCode() : 0);
                return hashCode;
            }
        }

        private sealed class LineNumberFullPathEqualityComparer : IEqualityComparer<MatchedRule>
        {
            public bool Equals(MatchedRule x, MatchedRule y)
            {
                return x.lineNumber == y.lineNumber && string.Equals(x.fullPath, y.fullPath) && string.Equals(x.displayPath, y.displayPath);
            }

            public int GetHashCode(MatchedRule obj)
            {
                return obj.GetHashCode();
            }
        }

        public static IEqualityComparer<MatchedRule> lineNumberFullPathComparer = new LineNumberFullPathEqualityComparer();
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class MatchedRulesExtractor
    {
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal HashSet<MatchedRule> selectedElementRules = new HashSet<MatchedRule>(MatchedRule.lineNumberFullPathComparer);
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal HashSet<string> selectedElementStylesheets = new HashSet<string>();
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal List<SelectorMatchRecord> matchRecords = new List<SelectorMatchRecord>();

        public IEnumerable<MatchedRule> GetMatchedRules() => selectedElementRules;

        private void SetupParents(VisualElement cursor, StyleMatchingContext matchingContext)
        {
            if (cursor.hierarchy.parent != null)
                SetupParents(cursor.hierarchy.parent, matchingContext);

            // We populate the ancestor filter in order for the Bloom filter detection to work.
            matchingContext.ancestorFilter.PushElement(cursor);

            if (cursor.styleSheetList == null)
                return;

            foreach (StyleSheet sheet in cursor.styleSheetList)
            {
                // Skip deleted style sheets
                if (sheet == null)
                    continue;

                string name = AssetDatabase.GetAssetPath(sheet);
                if (string.IsNullOrEmpty(name) || sheet.isDefaultStyleSheet)
                    name = sheet.name;

                void RecursivePrintStyleSheetNames(StyleSheet importedSheet)
                {
                    for (int i = 0; i < importedSheet.imports.Length; i++)
                    {
                        var thisImportedSheet = importedSheet.imports[i].styleSheet;
                        if (thisImportedSheet != null)
                        {
                            name += "\n(" + thisImportedSheet.name + ")";
                            matchingContext.AddStyleSheet(thisImportedSheet);
                            RecursivePrintStyleSheetNames(thisImportedSheet);
                        }
                    }
                }

                RecursivePrintStyleSheetNames(sheet);

                selectedElementStylesheets.Add(name);
                matchingContext.AddStyleSheet(sheet);
            }
        }

        public void FindMatchingRules(VisualElement target)
        {
            var matchingContext = new StyleMatchingContext((element, info) => {}) { currentElement = target };
            SetupParents(target, matchingContext);

            matchRecords.Clear();
            StyleSelectorHelper.FindMatches(matchingContext, matchRecords);

            matchRecords.Sort(SelectorMatchRecord.Compare);

            foreach (var record in matchRecords)
            {
                selectedElementRules.Add(new MatchedRule(record));
            }
        }
    }
}
