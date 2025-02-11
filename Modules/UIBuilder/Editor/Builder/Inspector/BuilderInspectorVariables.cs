// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using Object = UnityEngine.Object;
using UIEHelpBox = UnityEngine.UIElements.HelpBox;

namespace Unity.UI.Builder
{
    class BuilderInspectorVariables : IBuilderInspectorSection
    {
        BuilderInspector m_Inspector;
        BuilderSelection m_Selection;
        VisualElement m_SectionFoldout;
        StyleSheet styleSheet => m_Inspector.styleSheet;
        StyleRule currentStyleRule => m_Inspector.currentRule;
        VisualElement currentVisualElement => m_Inspector.currentVisualElement;
        ListView m_VariablesListView;
        List<StyleProperty> m_VariablesItemsSource;

        // Allows '_' and '-' but no special characters or spaces.
        static readonly Regex k_ValidNameRegex = new(@"^$|^[a-zA-Z0-9-_]+$");

        public VisualElement root => m_SectionFoldout;

        // used for testing
        internal List<StyleProperty> variablesItemsSource => m_VariablesItemsSource;

        internal static readonly string s_InspectorVariableListName = "variables-list-view";
        static readonly string s_EmptyListText = "There are no variables within this selector. Click the + icon to create a new variable.";
        static readonly string s_VariablesDropdownClassName = "inspector-variables-dropdown";
        static readonly string s_EmptyListClassName = "variables-list-empty";
        static readonly string s_VariablesSectionClassName = "inspector-style-section-foldout-variables";
        static readonly string s_ActiveFieldClassName = "active";
        static readonly string s_DefaultVariableName = "--new-var";
        static readonly string s_WarningBoxUssClassName = "warning-info-box";

        internal static readonly StyleValueType[] typesArray =
        {
            StyleValueType.Float, StyleValueType.Color, StyleValueType.Dimension, StyleValueType.String,
            StyleValueType.Keyword, StyleValueType.Enum, StyleValueType.AssetReference
        };

        internal static readonly StyleValueKeyword[] keywordArray =
        {
            StyleValueKeyword.Auto, StyleValueKeyword.Initial, StyleValueKeyword.Inherit, StyleValueKeyword.Unset,
            StyleValueKeyword.None,  StyleValueKeyword.True, StyleValueKeyword.False
        };

        public BuilderInspectorVariables(BuilderInspector inspector)
        {
            m_Inspector = inspector;
            m_SectionFoldout = m_Inspector.Q(s_VariablesSectionClassName);
            m_SectionFoldout.styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UssPath_InspectorVariable));
            m_Selection = inspector.selection;
            m_VariablesListView = m_Inspector.Q(s_InspectorVariableListName) as ListView;

            // Setup the variables section
            m_VariablesListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            m_VariablesListView.selectionType = SelectionType.Multiple;
            m_VariablesListView.makeNoneElement = () => new Label(L10n.Tr(s_EmptyListText)) { classList = { s_EmptyListClassName } };
            m_VariablesListView.makeItem = () => new BuilderInspectorVariablesListItem();
            m_VariablesListView.bindItem = (e, i) =>
            {
                var item = e as BuilderInspectorVariablesListItem;
                var listItem = m_VariablesItemsSource[i].values[0];

                item.itemNameField.RegisterValueChangedCallback(OnNameChanged);
                item.itemNameField.RegisterCallback<KeyUpEvent>(ValidateName);
                item.itemValueField.RegisterValueChangedCallback(OnValueChanged);
                item.itemFloatField.RegisterValueChangedCallback(OnFloatChanged);
                item.itemKeywordField.RegisterValueChangedCallback(OnValueChanged);
                item.itemDimensionField.RegisterValueChangedCallback(OnDimensionChanged);
                item.itemColorField.RegisterValueChangedCallback(OnColourChanged);
                item.itemAssetField.RegisterValueChangedCallback(OnAssetChanged);
                item.itemTypeField.RegisterValueChangedCallback(OnTypeChanged);

                item.itemValueField.enabledSelf = true;
                item.itemValueField.textEdition.AcceptCharacter = (c) => c != '"';

                item.userData = m_VariablesItemsSource[i];
                item.Q<TextField>(BuilderInspectorVariablesListItem.nameFieldName).SetValueWithoutNotify(m_VariablesItemsSource[i].name.TrimStart('-'));
                OnChangeVariableType(listItem.valueType, item);

                // We display the value in the active field.
                switch (listItem.valueType)
                {
                    case StyleValueType.Keyword:
                        var keywordText = styleSheet.ReadKeyword(listItem);
                        item.itemKeywordField.SetValueWithoutNotify(keywordText.ToString());
                        break;
                    case StyleValueType.Color:
                        var color = styleSheet.ReadColor(listItem);
                        item.itemColorField.SetValueWithoutNotify(color);
                        break;
                    case StyleValueType.AssetReference:
                        var asset = styleSheet.ReadAssetReference(listItem);
                        item.itemAssetField.SetValueWithoutNotify(asset);
                        break;
                    case StyleValueType.Dimension:
                        var dimensionText = StyleSheetToUss.ValueHandleToUssString(styleSheet, new UssExportOptions(), "",
                            listItem);
                        item.itemDimensionField.SetValueWithoutNotify(string.Join(" ", dimensionText));
                        break;
                    case StyleValueType.Function:
                        var functionText = StyleSheetToUss.ValueHandleToUssString(styleSheet, new UssExportOptions(), "", m_VariablesItemsSource[i].values[2]);
                        item.itemValueField.SetValueWithoutNotify(string.Join(" ", functionText?.Trim('"')));
                        item.itemValueField.enabledSelf = false;
                        break;
                    case StyleValueType.Float:
                        var floatText = styleSheet.ReadFloat(listItem);
                        item.itemFloatField.SetValueWithoutNotify(floatText);
                        break;
                    case StyleValueType.String:
                    case StyleValueType.Enum:
                    default:
                        var valueText = StyleSheetToUss.ValueHandleToUssString(styleSheet, new UssExportOptions(), "",
                            listItem);
                        item.itemValueField.SetValueWithoutNotify(string.Join(" ", valueText?.Trim('"')));
                        break;
                }

                item.itemTypeField.SetValueWithoutNotify(string.Join(" ", listItem.valueType.ToString()));

                EnableWarningBox(item, BuilderConstants.VariableNameFieldMustBeValidMessage, !IsValidName(item.itemNameField.text));
            };
            m_VariablesListView.unbindItem = (e, _) =>
            {
                var item = e as BuilderInspectorVariablesListItem;

                item.itemNameField.UnregisterValueChangedCallback(OnNameChanged);
                item.itemNameField.UnregisterCallback<KeyUpEvent>(ValidateName);
                item.itemValueField.UnregisterValueChangedCallback(OnValueChanged);
                item.itemFloatField.UnregisterValueChangedCallback(OnFloatChanged);
                item.itemKeywordField.UnregisterValueChangedCallback(OnValueChanged);
                item.itemDimensionField.UnregisterValueChangedCallback(OnDimensionChanged);
                item.itemColorField.UnregisterValueChangedCallback(OnColourChanged);
                item.itemAssetField.UnregisterValueChangedCallback(OnAssetChanged);
                item.itemTypeField.UnregisterValueChangedCallback(OnTypeChanged);

                EnableWarningBox(item, null, false);
            };

            var menu = new GenericDropdownMenu();
            foreach (var choice in typesArray)
            {
                menu.AddItem(choice.ToString(), false, (_) => OnCreateVariable(choice.ToString()), null);
            }

            // We want to override the add button behavior so that we can create the variable before the list item is created.
            m_VariablesListView.overridingAddButtonBehavior = (_, btn) =>
            {
                menu.DropDown(btn.worldBound, btn, true, true);
                menu.contentContainer.AddToClassList(s_VariablesDropdownClassName);
            };

            m_VariablesListView.onRemove += OnRemoveVariable;
            m_VariablesListView.itemIndexChanged += OnListReordered;
        }

        static string GetTextFieldDefaultValue(StyleValueType type)
        {
            return (type) switch
            {
                StyleValueType.Float => "0",
                StyleValueType.String => "String",
                _ => ""
            };
        }

        string GenerateDefaultName()
        {
            var suffix = 1;
            var baseName = s_DefaultVariableName;
            var name = $"{baseName}{suffix}";

            if (!m_VariablesItemsSource.Any(prop => prop.name.Equals(name)))
                return name;

            do
            {
                name = $"{baseName}{suffix}";
                suffix++;
            } while (m_VariablesItemsSource.Any(prop => prop.name.Equals(name)));

            return name;
        }

        internal void OnCreateVariable(string choice)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            Enum.TryParse<StyleValueType>(choice, out var type);

            var name = GenerateDefaultName();

            switch (type)
            {
                case (StyleValueType.Float):
                case (StyleValueType.String):
                    styleSheet.AddVariable(currentVisualElement.GetStyleComplexSelector(), type, name, GetTextFieldDefaultValue(type));
                    break;
                case (StyleValueType.Enum):
                    styleSheet.AddVariable(currentVisualElement.GetStyleComplexSelector(), type, name, "auto");
                    break;
                case (StyleValueType.Keyword):
                    styleSheet.AddVariable(currentVisualElement.GetStyleComplexSelector(), type, name, StyleKeyword.Auto.ToString());
                    break;
                case (StyleValueType.Color):
                    styleSheet.AddVariable(currentVisualElement.GetStyleComplexSelector(), name, Color.black);
                    break;
                case (StyleValueType.AssetReference):
                    styleSheet.AddVariable(currentVisualElement.GetStyleComplexSelector(), name, new Object());
                    break;
                case (StyleValueType.Dimension):
                    styleSheet.AddVariable(currentVisualElement.GetStyleComplexSelector(), name, new Dimension(0, Dimension.Unit.Pixel));
                    break;
            }

            AfterAddVariable();
        }

        void OnChangeVariableType(StyleValueType type, VisualElement currentRow)
        {
            var row = currentRow.Q(className: s_ActiveFieldClassName);
            if (row == null)
            {
                row = (currentRow as BuilderInspectorVariablesListItem)?.itemValueField;
                row.AddToClassList(s_ActiveFieldClassName);
            }

            row.RemoveFromClassList(s_ActiveFieldClassName);

            switch (type)
            {
                case (StyleValueType.Keyword):
                    currentRow.Q(name: BuilderInspectorVariablesListItem.keywordFieldName).AddToClassList(s_ActiveFieldClassName);
                    break;
                case (StyleValueType.Color):
                    currentRow.Q(name: BuilderInspectorVariablesListItem.colorFieldName).AddToClassList(s_ActiveFieldClassName);
                    break;
                case (StyleValueType.ScalableImage):
                case (StyleValueType.AssetReference):
                case (StyleValueType.MissingAssetReference):
                    currentRow.Q(name: BuilderInspectorVariablesListItem.assetFieldName).AddToClassList(s_ActiveFieldClassName);
                    break;
                case (StyleValueType.Dimension):
                    currentRow.Q(name: BuilderInspectorVariablesListItem.dimensionFieldName).AddToClassList(s_ActiveFieldClassName);
                    break;
                case (StyleValueType.Float):
                    currentRow.Q(name: BuilderInspectorVariablesListItem.floatFieldName).AddToClassList(s_ActiveFieldClassName);
                    break;
                case (StyleValueType.String):
                case (StyleValueType.Enum):
                default:
                    currentRow.Q(name: BuilderInspectorVariablesListItem.valueFieldName).AddToClassList(s_ActiveFieldClassName);
                    break;
            }
        }

        void AfterAddVariable()
        {
            // We remove the selected style rule from properties and add it to the end so that it doesn't affect reordering.
            var props = new List<StyleProperty>(currentStyleRule.properties);
            var index = props.FindIndex(p => p.name == BuilderConstants.SelectedStyleRulePropertyName);
            var selectedStyleVar = props[index];
            props.RemoveAt(index);
            props.Add(selectedStyleVar);
            for (int i = 0; i < props.Count; i++)
            {
                currentStyleRule.properties[i] = props[i];
            }

            m_Inspector.panel.visualTree.IncrementVersion(VersionChangeType.StyleSheet);
            m_Selection.NotifyOfStylingChange();
            m_VariablesListView.RefreshItems();
        }

        // This method is used by the ListView remove footer.
        internal void OnRemoveVariable(BaseListView listView)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            // If no items are selected, remove last item
            if (listView.selectedIndex == -1 && m_VariablesItemsSource.Count > 0)
            {
                var index = m_VariablesItemsSource.Count - 1;
                var prop = m_VariablesItemsSource[index];
                if (prop != null)
                {
                    styleSheet.RemoveProperty(currentVisualElement.GetStyleComplexSelector(), prop);
                }
            }
            else
            {
                foreach (var selectedIndex in listView.selectedIndices)
                {
                    if (selectedIndex >= m_VariablesItemsSource.Count)
                    {
                        continue;
                    }
                    var prop = m_VariablesItemsSource[selectedIndex];
                    if (prop != null)
                    {
                        styleSheet.RemoveProperty(currentVisualElement.GetStyleComplexSelector(), prop);
                    }
                }
            }

            m_Inspector.panel.visualTree.IncrementVersion(VersionChangeType.StyleSheet);
            m_Selection.NotifyOfStylingChange();
            m_VariablesListView.ClearSelection();
        }

        void OnListReordered(int previousIndex, int newIndex)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            ReorderVariables();

            m_Inspector.panel.visualTree.IncrementVersion(VersionChangeType.StyleSheet);
            m_Selection.NotifyOfStylingChange();
            m_VariablesListView.RefreshItems();
        }

        void ReorderVariables()
        {
            var variableIndices = new List<int>();

            for (var i = 0; i < currentStyleRule.properties.Length; i++)
            {
                if (m_VariablesItemsSource.Contains(currentStyleRule.properties[i]))
                {
                    variableIndices.Add(i);
                }
            }

            for (var i = 0; i < variableIndices.Count; i++)
            {
                currentStyleRule.properties[variableIndices[i]] = m_VariablesItemsSource[i];
            }
        }

        void OnNameChanged(ChangeEvent<string> evt)
        {
            if (evt.elementTarget.GetFirstAncestorOfType<BuilderInspectorVariablesListItem>().userData is not StyleProperty styleProperty)
                return;

            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            var newName = BuilderConstants.VariablePrefix + evt.newValue.TrimStart('-');

            if (!IsValidName(newName))
                 return;

            styleProperty.name = newName;

            m_Inspector.panel.visualTree.IncrementVersion(VersionChangeType.StyleSheet);
            m_Selection.NotifyOfStylingChange();
        }

        void OnValueChanged(ChangeEvent<string> evt)
        {
            var listItem = evt.elementTarget.GetFirstAncestorOfType<BuilderInspectorVariablesListItem>();
            if (listItem.userData is not StyleProperty styleProperty)
                return;

            var newValue = evt.newValue.Replace("\"", "");

            // if it's an empty string, we don't want to set the value
            if (styleProperty.values[0].valueType == StyleValueType.Enum && string.IsNullOrEmpty(evt.newValue))
            {
                EnableWarningBox(listItem, BuilderConstants.VariableEnumFieldMustBeValidMessage, true);
                listItem.itemValueField.SetValueWithoutNotify(evt.previousValue);
                return;
            }

            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            switch (styleProperty.values[0].valueType)
            {
                case StyleValueType.String:
                    styleSheet.SetValue(styleProperty.values[0], newValue);
                    break;
                case StyleValueType.Enum:
                    styleSheet.SetValue(styleProperty.values[0], newValue);
                    break;
                case StyleValueType.Keyword:
                    var styleValue = styleProperty.values[0];
                    styleValue.valueIndex = (int)Enum.Parse<StyleValueKeyword>(evt.newValue);
                    styleProperty.values[0] = styleValue;
                    break;
            }

            m_Inspector.panel.visualTree.IncrementVersion(VersionChangeType.StyleSheet);
            m_Selection.NotifyOfStylingChange();
        }

        void OnFloatChanged(ChangeEvent<float> evt)
        {
            if (evt.elementTarget.GetFirstAncestorOfType<BuilderInspectorVariablesListItem>().userData is not StyleProperty styleProperty)
                return;

            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            styleSheet.SetValue(styleProperty.values[0], evt.newValue);

            m_Inspector.panel.visualTree.IncrementVersion(VersionChangeType.StyleSheet);
            m_Selection.NotifyOfStylingChange();
        }

        void OnDimensionChanged(ChangeEvent<string> evt)
        {
            if (evt.elementTarget.GetFirstAncestorOfType<BuilderInspectorVariablesListItem>().userData is not StyleProperty styleProperty)
                return;

            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            if (evt.currentTarget is USSVariablesStyleField { isKeyword: false } field)
            {
                styleSheet.SetValue(styleProperty.values[0], new Dimension(float.Parse(field.value), field.unit));
            }

            m_Inspector.panel.visualTree.IncrementVersion(VersionChangeType.StyleSheet);
            m_Selection.NotifyOfStylingChange();
        }

        void OnTypeChanged(ChangeEvent<string> evt)
        {
            if (evt.elementTarget.GetFirstAncestorOfType<BuilderInspectorVariablesListItem>().userData is not StyleProperty styleProperty)
                return;

            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            ChangeType(evt.newValue, styleProperty);
        }

        void ChangeType(string newType, StyleProperty styleProperty)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            Enum.TryParse<StyleValueType>(newType, out var type);
            styleProperty.values[0].valueType = type;
            styleSheet.RemoveValue(styleProperty, styleProperty.values[0]);

            switch (type)
            {
                case (StyleValueType.Float):
                    styleSheet.AddValue(styleProperty, float.Parse(GetTextFieldDefaultValue(type)));
                    break;
                case (StyleValueType.Color):
                    styleSheet.AddValue(styleProperty, Color.black);
                    break;
                case (StyleValueType.AssetReference):
                    styleSheet.AddValue(styleProperty, new Object());
                    break;
                case (StyleValueType.Dimension):
                    styleSheet.AddValue(styleProperty, new Dimension(0, Dimension.Unit.Pixel));
                    break;
                case (StyleValueType.Keyword):
                    styleSheet.AddValue(styleProperty, StyleValueKeyword.Auto);
                    break;
                case (StyleValueType.String):
                    styleSheet.AddValue(styleProperty, GetTextFieldDefaultValue(type));
                    break;
                case (StyleValueType.Enum):
                    styleSheet.AddValue(styleProperty, (Enum)StyleValueKeyword.Auto);
                    break;
            }

            m_Inspector.panel.visualTree.IncrementVersion(VersionChangeType.StyleSheet);
            m_Selection.NotifyOfStylingChange();
        }

        void OnColourChanged(ChangeEvent<Color> evt)
        {
            if (evt.elementTarget.GetFirstAncestorOfType<BuilderInspectorVariablesListItem>().userData is not StyleProperty styleProperty)
                return;

            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            styleSheet.SetValue(styleProperty.values[0], evt.newValue);

            m_Inspector.panel.visualTree.IncrementVersion(VersionChangeType.StyleSheet);
            m_Selection.NotifyOfStylingChange();
        }

        void OnAssetChanged(ChangeEvent<Object> evt)
        {
            if (evt.elementTarget.GetFirstAncestorOfType<BuilderInspectorVariablesListItem>().userData is not StyleProperty styleProperty)
                return;

            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            styleSheet.SetValue(styleProperty.values[0], evt.newValue);

            m_Inspector.panel.visualTree.IncrementVersion(VersionChangeType.StyleSheet);
            m_Selection.NotifyOfStylingChange();
        }

        public void Refresh()
        {
            if (currentStyleRule == null)
                return;

            var newVariablesList = new List<StyleProperty>();
            foreach (var property in currentStyleRule.properties)
            {
                if (property.isCustomProperty && property.name != BuilderConstants.SelectedStyleRulePropertyName)
                {
                    newVariablesList.Add(property);
                }
            }

            m_VariablesItemsSource = newVariablesList;
            m_VariablesListView.itemsSource = m_VariablesItemsSource;
            m_VariablesListView.RefreshItems();
        }

        void ValidateName(KeyUpEvent evt)
        {
            if (evt.target is not TextElement field)
                return;

            EnableWarningBox(field.GetFirstAncestorOfType<BuilderInspectorVariablesListItem>(), BuilderConstants.VariableNameFieldMustBeValidMessage,
                !IsValidName(field.text));
        }

        static void EnableWarningBox(VisualElement item, string message, bool enabled)
        {
            var nameWarningHelpBox = item.Q<UIEHelpBox>();

            if (enabled && nameWarningHelpBox == null)
            {
                nameWarningHelpBox = new UIEHelpBox(L10n.Tr(message), HelpBoxMessageType.Warning) { name = s_WarningBoxUssClassName };
                item.Add(nameWarningHelpBox);
            }

            if (nameWarningHelpBox != null)
            {
                nameWarningHelpBox.EnableInClassList(BuilderConstants.InspectorShownWarningMessageClassName, enabled);
                nameWarningHelpBox.EnableInClassList(BuilderConstants.InspectorHiddenWarningMessageClassName, !enabled);
            }
        }

        static bool IsValidName(string name)
        {
            return k_ValidNameRegex.Match(name.TrimStart('-')).Success;
        }

        public void Enable() { }

        public void Disable() { }
    }
}
