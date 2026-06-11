// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Properties;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using static UnityEditor.UIElements.Debugger.UIElementsDebuggerImpl;

namespace UnityEditor.UIElements.Debugger
{
    internal class StylesDebugger : VisualElement
    {
        private DebuggerSelection m_DebuggerSelection;
        private BoxModelView m_BoxModelView;
        private readonly LayoutInfo m_LayoutInfo;
        private StylePropertyDebugger m_StylePropertyDebugger;
        private IMGUIContainer m_MatchingRulesContainer;
        private AttributesSection m_attributeSection;

        private IPanelDebug m_PanelDebug;
        private VisualElement selectedElement
        {
            get
            {
                return m_SelectedElement;
            }
            set
            {
                // Do this before the early return as the selected element is null for the first callback
                style.display = value == null ? DisplayStyle.None : DisplayStyle.Flex;

                if (m_SelectedElement == value)
                    return;

                m_SelectedElement = value;



                m_BoxModelView.selectedElement = m_SelectedElement;
                m_attributeSection.RefreshIfNeeded();
                m_LayoutInfo.Update(m_PanelDebug, selectedElement);

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

            Foldout layoutInfo = new() { text = L10n.Tr("Layout"), viewDataKey = "layoutInfo"};
            layoutInfo.contentContainer.style.flexDirection = FlexDirection.Row;
            layoutInfo.contentContainer.style.flexWrap = Wrap.Wrap;
            layoutInfo.contentContainer.style.alignItems = Align.Center;
            layoutInfo.contentContainer.style.alignContent = Align.Center;
            layoutInfo.contentContainer.style.justifyContent = Justify.SpaceAround;
            layoutInfo.contentContainer.style.flexShrink = 0;

            layoutInfo.Add(m_BoxModelView = new BoxModelView());
            layoutInfo.Add(m_LayoutInfo = new LayoutInfo() { style = { flexShrink = 0, minWidth = 420 } });
            Add(layoutInfo);


            Foldout stylesheets = new() { text = L10n.Tr("Stylesheets"), viewDataKey = "StylesheetsFoldout", value = false };
            stylesheets.Add(new IMGUIContainer(DrawStylesheet));
            Add(stylesheets);

            Foldout matchingRules = new() { text = L10n.Tr( "Matching Selectors"), viewDataKey = "MatchingRulesFoldout" , value = false};
            matchingRules.Add(m_MatchingRulesContainer = new IMGUIContainer(DrawMatchingRules));
            Add(matchingRules);


            Add(m_attributeSection = new AttributesSection(debuggerSelection));

            Foldout StylesInfo = new() { text = L10n.Tr("Styles"), viewDataKey = "StylesInfo" };
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
            m_attributeSection.RefreshIfNeeded();

        }

        public void RefreshBoxModelView(MeshGenerationContext mgc)
        {
            m_BoxModelView.Refresh(mgc);
            m_LayoutInfo.Update(m_PanelDebug, selectedElement);
        }


        private MatchedRulesExtractor m_MatchedRulesExtractor = new (AssetDatabase.GetAssetPath);


        private void GetElementMatchers()
        {
            if (m_SelectedElement == null || m_SelectedElement.elementPanel == null)
                return;
            m_MatchedRulesExtractor.selectedElementRules.Clear();
            m_MatchedRulesExtractor.selectedElementStylesheets.Clear();
            m_MatchedRulesExtractor.FindMatchingRules(m_SelectedElement);
        }

        private class LayoutInfo : VisualElement
        {
            readonly TextField m_WorldBound;
            readonly TextField m_WorldClip;
            readonly TextField m_ContentRect;
            readonly TextField m_PickingBoundingBox;
            readonly TextField m_Layout;

            public LayoutInfo()
            {
                Add(m_WorldBound = new TextField("World Bound") { isReadOnly = true });
                Add(m_WorldClip = new TextField("World Clip") { isReadOnly = true });
                Add(m_ContentRect = new TextField("Content Rect") { isReadOnly = true });
                Add(m_PickingBoundingBox = new TextField(L10n.Tr("Picking Bounding Box")) { isReadOnly = true });
                Add(m_Layout = new TextField("Layout") { isReadOnly = true });
            }

            // with isReadOnly set to true.
            public void Update(IPanelDebug panel,VisualElement selectedElement )
            {
                m_PickingBoundingBox.style.display = UIToolkitProjectSettings.EnableLowLevelDebugger? DisplayStyle.Flex:DisplayStyle.None;

                if (panel == null || selectedElement == null)
                {
                    m_WorldBound.text = "";
                    m_WorldClip.text = "";
                    m_ContentRect.text = "";
                    m_PickingBoundingBox.text = "";
                    m_Layout.text = m_WorldBound.text;

                }
                else
                {
                    m_WorldBound.text = selectedElement.worldBound.ToString();
                    m_WorldClip.text = selectedElement.worldBound.ToString();
                    m_ContentRect.text = selectedElement.contentRect.ToString();
                    m_PickingBoundingBox.text = selectedElement.boundingBox.ToString();
                    m_Layout.text = selectedElement.worldBound.ToString();
                }
            }
        }

        private class AttributesSection : DebuggerFoldout
        {
            private const string k_NewClassName = "newStyle";

            readonly TextField m_name;
            readonly IntegerField m_IdField;
            readonly ObjectField m_VisualTreeAsset;
            readonly TextField m_tooltip;
            readonly TextAttributesSection m_text;
            readonly TextField m_viewDataKey;
            readonly TextField m_dataSource;
            readonly EnumField m_pickingMode;
            readonly EnumFlagsField m_pseudoStyles;
            readonly Toggle m_enabled;
            const string k_enabledLabelName = "EnabledInHierarchy";
            readonly TextField m_foccusable;
            readonly EnumFlagsField m_usageHints;
            readonly IntegerField m_tabIndex;
            readonly TextField m_bindingPath;
            private ListView m_ClassList;
            private readonly List<string> m_ClassesCopy = new();

            public AttributesSection(DebuggerSelection debuggerSelection):base("Attributes", debuggerSelection, false)
            {
                value = true; //This foldout is expanded by default

                Add(m_name = new TextField("Name"));
                m_name.RegisterValueChangedCallback((v) => { if (m_SelectedElement != null) { m_SelectedElement.name = v.newValue; } });

                Add(m_IdField = new IntegerField("Authoring Id") { isReadOnly = true });

                Add(m_VisualTreeAsset = new("Visual Tree Asset") { enabledSelf = false });

                Add(m_tooltip = new TextField("Tooltip"));
                m_tooltip.RegisterValueChangedCallback((v) => { if (m_SelectedElement != null) { m_SelectedElement.tooltip = v.newValue; } });

                Add(m_text = new TextAttributesSection(debuggerSelection));

                Add(m_viewDataKey = new TextField("ViewDataKey"));
                m_viewDataKey.RegisterValueChangedCallback((v) => { if (m_SelectedElement != null) { m_SelectedElement.viewDataKey = v.newValue; } });

                Add(m_dataSource = new TextField("Data Source") { isReadOnly = true });

                Add(m_pickingMode = new EnumField("Picking Mode", PickingMode.Ignore));
                m_pickingMode.RegisterValueChangedCallback((v) => { if (m_SelectedElement != null) { m_SelectedElement.pickingMode = (PickingMode)v.newValue; } });

                Add(m_pseudoStyles = new EnumFlagsField("Pseudo States", PseudoStates.None) { tooltip = "This pseudo style only represent the visual state of the element." });
                m_pseudoStyles.RegisterValueChangedCallback((v) => { if (m_SelectedElement != null) { m_SelectedElement.pseudoStates = (PseudoStates)v.newValue; } });

                Add(m_enabled = new("Enabled"));
                m_enabled.Add(new Label() { name = k_enabledLabelName });
                m_enabled.RegisterValueChangedCallback((v) => { if (m_SelectedElement != null) { m_SelectedElement.SetEnabled(v.newValue); } });

                Add(m_foccusable = new TextField("Focusable") { isReadOnly = true });

                Add(m_tabIndex = new IntegerField("Tab Index"));
                m_tabIndex.RegisterCallback<ChangeEvent<int>>((v) => { if (m_SelectedElement != null) { m_SelectedElement.tabIndex = v.newValue; } });

                Add(m_usageHints = new EnumFlagsField("Usage Hints", UsageHints.None));
                m_usageHints.RegisterValueChangedCallback((v) => { if (m_SelectedElement != null) { m_SelectedElement.usageHints = (UsageHints)v.newValue; } });

                Add(m_bindingPath = new TextField("Binding Path") { isReadOnly = true });

                Add(m_ClassList = new ListView() {
                    showFoldoutHeader = true,
                    headerTitle = "Classes",
                    showBorder = true,
                    showAddRemoveFooter = true,
                    makeItem = () =>
                    {
                        //TODO we should probably sanitize a bit more (spaces?)
                        var item = new TextField();
                        item.RegisterValueChangedCallback(t =>
                        {
                            m_SelectedElement.RemoveFromClassList(t.previousValue);
                            m_SelectedElement.AddToClassList(t.newValue);
                            SyncClassList();
                        });
                        return item;
                    },
                    bindItem = (e, i) =>
                    {
                        var tf = e as TextField;
                        tf.SetValueWithoutNotify(m_ClassesCopy[i]);
                    },

                    onAdd = (t) =>
                    {
                        AddNewClass();
                        SyncClassList();
                    },

                    onRemove = _ =>
                    {
                        RemoveLastClass();
                        SyncClassList();
                    },

                    // Don't use the classList directly. It's unreliable, can be reassigned, etc.
                    // Instead we maintain a local copy and properly monitor the content we set on the element.
                    itemsSource = m_ClassesCopy
                });

                m_ClassList.viewController.itemsSourceSizeChanged += () =>
                {
                    if (m_ClassesCopy.Count < m_SelectedElement.classListCount)
                    {
                        int toRemove = m_SelectedElement.classListCount - m_ClassesCopy.Count;
                        for (int i = 0; i < toRemove; i++)
                            RemoveLastClass();
                    }
                    else
                    {
                        int toAdd = m_ClassesCopy.Count - m_SelectedElement.classListCount;
                        for (int i = 0; i < toAdd; i++)
                            AddNewClass();
                    }

                    SyncClassList();
                };
            }

            protected override void Refresh()
            {
                if (m_SelectedElement == null)
                    return;

                m_name.text = selectedElement.name;

                if (selectedElement.visualElementAsset?.hasAuthoringId == true)
                    m_IdField.value = selectedElement.visualElementAsset.id;
                else
                    m_IdField.value = 0;

                m_VisualTreeAsset.value = selectedElement.visualTreeAssetSource;
                m_tooltip.text = selectedElement.tooltip;
                m_text.RefreshIfNeeded();

                m_viewDataKey.text = m_SelectedElement.viewDataKey;

                m_dataSource.text = null == m_SelectedElement.dataSource ? "<none>" : TypeUtility.GetTypeDisplayName(m_SelectedElement.dataSource.GetType()) + " : " + m_SelectedElement.dataSourcePath.ToString();
                m_pickingMode.value = m_SelectedElement.pickingMode;
                m_pseudoStyles.value = m_SelectedElement.pseudoStates;
                m_enabled.value = m_SelectedElement.enabledSelf;
                m_enabled.Q<Label>(k_enabledLabelName).text = m_SelectedElement.enabledInHierarchy ? L10n.Tr("Enabled in hierarchy") : L10n.Tr("Disabled in hierarchy");
                m_foccusable.text = m_SelectedElement.focusable.ToString();
                m_usageHints.value = m_SelectedElement.usageHints;
                m_tabIndex.value = m_SelectedElement.tabIndex;

                if (m_SelectedElement is IBindable bindableElement)
                {
                    m_bindingPath.text = bindableElement.bindingPath;
                    m_bindingPath.style.display = DisplayStyle.Flex;
                }
                else
                    m_bindingPath.style.display = DisplayStyle.None;

                SyncClassList();
                m_ClassList.RefreshItems();
            }

            private void SyncClassList()
            {
                m_ClassesCopy.Clear();
                m_ClassesCopy.AddRange(m_SelectedElement.GetClasses());
            }

            private void AddNewClass()
            {
                // Attempt to add a new unique class name using a simple suffix
                // - newStyle
                // - newStyle1
                // - newStyle2
                // ...
                // until the class count is effectively changed.

                int suffix = 0;
                int originalCount = m_SelectedElement.classListCount;
                while (m_SelectedElement.classListCount == originalCount)
                {
                    var className = suffix > 0 ? k_NewClassName + suffix : k_NewClassName;
                    m_SelectedElement.AddToClassList(className);
                    suffix++;
                }
            }

            private void RemoveLastClass()
            {
                var classes = m_SelectedElement.GetClassesForIteration();
                m_SelectedElement.RemoveFromClassList(classes[^1]);
            }
        }

        private class TextAttributesSection : DebuggerFoldout
        {
            readonly TextField m_text;
            readonly Toggle m_EnableRichText;
            readonly Toggle m_EmojiFallbackSupport;
            readonly Toggle m_ParseEscapeSequences;
            readonly Toggle m_IsSelectable;
            readonly Toggle m_DoubleClickSelectsWord;
            readonly Toggle m_TripleClickSelectsLine;
            readonly Toggle m_DisplayTooltipWhenElided;
            readonly EnumField m_LanguageDirection;

            private TextElement textElement => m_SelectedElement as TextElement;


            public TextAttributesSection(DebuggerSelection debuggerSelection) : base("Text", debuggerSelection, false)
            {
                toggle.visualInput.style.minWidth = 135;
                toggle.visualInput.style.flexGrow = 0;
                toggle.style.flexGrow = 0;
                toggle.Add(m_text = new TextField() { style = { flexGrow = 1 } });
                m_text.textInputBase.textElement.style.marginLeft = 0; // We are probably triggering a selector of the toggle as nobody planned to nest textFields in toggle

                m_text.RegisterValueChangedCallback((v) => { if (textElement != null) { textElement.text = v.newValue; } });

                Add(m_EnableRichText = new("EnableRichText"));
                m_EnableRichText.RegisterValueChangedCallback((v) => { if (m_SelectedElement != null) { textElement.enableRichText = v.newValue; } });

                Add(m_EmojiFallbackSupport = new("Emoji Fallback Support"));
                m_EmojiFallbackSupport.RegisterValueChangedCallback((v) => { if (m_SelectedElement != null) { textElement.emojiFallbackSupport = v.newValue; } });

                Add(m_ParseEscapeSequences = new("Parse EscapeSequences"));
                m_ParseEscapeSequences.RegisterValueChangedCallback((v) => { if (m_SelectedElement != null) { textElement.parseEscapeSequences = v.newValue; } });

                Add(m_IsSelectable = new("Is Selectable"));
                m_IsSelectable.RegisterValueChangedCallback((v) => { if (m_SelectedElement != null) { textElement.isSelectable = v.newValue; } });

                Add(m_DoubleClickSelectsWord = new("Double Click Selects Word"));
                m_DoubleClickSelectsWord.RegisterValueChangedCallback((v) => { if (m_SelectedElement != null) { textElement.doubleClickSelectsWord = v.newValue; } });

                Add(m_TripleClickSelectsLine = new("Triple Click Selects Line"));
                m_TripleClickSelectsLine.RegisterValueChangedCallback((v) => { if (m_SelectedElement != null) { textElement.tripleClickSelectsLine = v.newValue; } });

                Add(m_DisplayTooltipWhenElided = new("Display Tooltip When Elided"));
                m_DisplayTooltipWhenElided.RegisterValueChangedCallback((v) => { if (m_SelectedElement != null) {textElement.displayTooltipWhenElided = v.newValue; } });

                Add(m_LanguageDirection = new("Language Direction", LanguageDirection.LTR));
                m_LanguageDirection.RegisterValueChangedCallback((v) => { if (m_SelectedElement != null) { m_SelectedElement.languageDirection = (LanguageDirection)v.newValue; } });

                foreach (VisualElement child in contentContainer.Children())
                {
                    var field = child as IPrefixLabel;
                    field.labelElement.style.minWidth = 180;
                }
            }

            protected override void Refresh()
            {
                //m_text is always updated in UpdateVisiblity
                m_EnableRichText.value = textElement.enableRichText;
                m_EmojiFallbackSupport.value = textElement.emojiFallbackSupport;
                m_ParseEscapeSequences.value = textElement.parseEscapeSequences;
                m_IsSelectable.value = textElement.isSelectable;
                m_DoubleClickSelectsWord.value = textElement.doubleClickSelectsWord;
                m_TripleClickSelectsLine.value = textElement.tripleClickSelectsLine;
                m_DisplayTooltipWhenElided.value = textElement.displayTooltipWhenElided;
                m_LanguageDirection.value = m_SelectedElement.languageDirection;
            }

            protected override void UpdateVisiblity()
            {
                if (textElement != null)
                {
                    style.display = DisplayStyle.Flex;
                    m_text.text = textElement.text; // Always update the text here as it is shown when collapsed.
                }
                else
                    style.display = DisplayStyle.None;

            }
        }

        static readonly string k_noAssetText = L10n.Tr("No source available for already imported asset");

        private void DrawStylesheet()
        {
            if (m_PanelDebug == null || m_SelectedElement == null)
                return;

            if (m_MatchedRulesExtractor.selectedElementStylesheets != null && m_MatchedRulesExtractor.selectedElementStylesheets.Count > 0)
            {

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginVertical();
                    foreach (string sheet in m_MatchedRulesExtractor.selectedElementStylesheets)
                    {
                        bool canOpen = CanOpenStyleSheet(sheet);
                        using (new EditorGUI.DisabledScope(!canOpen))
                        {
                            if (GUILayout.Button(new GUIContent(sheet, canOpen ? null : k_noAssetText)))
                                InternalEditorUtility.OpenFileAtLineExternal(sheet, 0, 0);
                        }
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawMatchingRules()
        {
            if (m_PanelDebug == null || m_SelectedElement == null)
                return;

            if (m_MatchedRulesExtractor.selectedElementRules != null && m_MatchedRulesExtractor.selectedElementRules.Count > 0)
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
                        bool canOpen = CanOpenStyleSheet(rule.fullPath);
                        using (new EditorGUI.DisabledScope(!canOpen))
                        {
                            if (rule.displayPath != null && GUILayout.Button(new GUIContent(rule.displayPath, canOpen ? null : k_noAssetText), EditorStyles.miniButton, GUILayout.MaxWidth(250)) )
                                InternalEditorUtility.OpenFileAtLineExternal(rule.fullPath, rule.lineNumber, -1);
                        }
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

        static bool CanOpenStyleSheet(string path) => File.Exists(path);
    }


}
