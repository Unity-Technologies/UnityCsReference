// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    [UxmlElement]
    partial class VariablesInspector : VisualElement
    {
        public VariablesInspector()
        {
            m_VariablesListView = new ListView
            {
                name = k_VariablesListViewName,
                showBorder = true,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                showAddRemoveFooter = true
            };
            m_VariablesListView.AddToClassList("variables-list");
            Add(m_VariablesListView);

            m_VariablesListView.makeNoneElement = () =>
                new Label(L10n.Tr(k_EmptyListText)).WithClassList(k_EmptyListClassName);
            m_VariablesListView.makeItem = () => CreateListItem();
            m_VariablesListView.bindItem = BindItem;
            m_VariablesListView.unbindItem = UnbindItem;
            m_VariablesListView.destroyItem = DestroyItem;

            RegisterCallback<ChangeEvent<string>>(OnStringChanged);
            RegisterCallback<ChangeEvent<float>>(OnFloatChanged);
            RegisterCallback<ChangeEvent<Length>>(OnLengthChanged);
            RegisterCallback<ChangeEvent<Angle>>(OnAngleChanged);
            RegisterCallback<ChangeEvent<Color>>(OnColourChanged);
            RegisterCallback<ChangeEvent<TimeValue>>(OnTimeChanged);
            RegisterCallback<ChangeEvent<Object>>(OnAssetChanged);
            RegisterCallback<KeyUpEvent>(ValidateName);

            m_VariablesListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            m_VariablesListView.selectionType = SelectionType.Multiple;

            var menu = new GenericDropdownMenu();
            foreach (var choice in Enum.GetValues(typeof(VariableType)))
            {
                menu.AddItem(choice.ToString(), false, (_) => OnCreateVariable(choice.ToString()), null);
            }

            m_VariablesListView.overridingAddButtonBehavior = (_, btn) =>
            {
                menu.DropDown(btn.worldBound, btn, DropdownMenuSizeMode.Auto);
                menu.contentContainer.AddToClassList(k_VariableDropdownClassName);
                menu.contentContainer.style.fontSize = k_DefaultMenuFontSize;
            };

            m_VariablesListView.onRemove += DeleteVariable;
            m_VariablesListView.itemIndexChanged += OnListReordered;

            var addButton = m_VariablesListView.Q<Button>("unity-list-view__add-button");
            addButton.AddToClassList(BaseListView.footerAddButtonWithMenuNameUnique);
        }

        internal enum VariableType
        {
            Float,
            Color,
            Length,      // px, %
            Angle,       // deg, grad, rad, turn
            Time,
            String,
            Keyword,
            Enum,
            AssetReference
        }

        static VariableType? GetVariableType(StyleValueHandle handle, StyleSheet sheet)
        {
            if (handle.valueType != StyleValueType.Dimension)
                return handle.valueType switch
                {
                    StyleValueType.Float => VariableType.Float,
                    StyleValueType.Color => VariableType.Color,
                    StyleValueType.String => VariableType.String,
                    StyleValueType.Enum => VariableType.Enum,
                    StyleValueType.Keyword => VariableType.Keyword,
                    StyleValueType.AssetReference => VariableType.AssetReference,
                    _ => null
                };

            var dimension = sheet.ReadDimension(handle);
            return dimension.unit switch
            {
                Dimension.Unit.Degree or Dimension.Unit.Gradian
                    or Dimension.Unit.Radian or Dimension.Unit.Turn => VariableType.Angle,
                Dimension.Unit.Second or Dimension.Unit.Millisecond => VariableType.Time,
                _ => VariableType.Length
            };
        }


        Func<StyleRule> m_GetOrCreateStyleRule;

        protected StyleRule styleRule { get; set; }
        protected StyleSheet styleSheet => styleRule?.styleSheet;

        protected ListView variablesListView
        {
            get => m_VariablesListView;
            set => m_VariablesListView = value;
        }
        public List<StyleProperty> variablesItemsSource => m_VariablesItemsSource;
        public const string k_VariablesListViewName = "variables-list-view";
        protected const string k_VariablesSectionClassName = "inspector-style-section-foldout-variables";

        public const string k_VariablePrefix = "--";

        public static readonly StyleValueKeyword[] s_KeywordArray =
        {
            StyleValueKeyword.Auto, StyleValueKeyword.Initial, StyleValueKeyword.Inherit, StyleValueKeyword.Unset,
            StyleValueKeyword.None, StyleValueKeyword.True, StyleValueKeyword.False
        };

        ListView m_VariablesListView;
        List<StyleProperty> m_VariablesItemsSource = new();
        static readonly Regex k_ValidNameRegex = new(@"^$|^[a-zA-Z0-9-_]+$");

        const string k_EmptyListText = "Click the + icon to create a new Variable";
        const string k_VariableDropdownClassName = "inspector-variables-dropdown";
        const string k_EmptyListClassName = "variables-list-empty";

        const string k_ActiveFieldClassName = "active";
        const string k_DefaultVariableName = "--new-var";
        const string k_SelectedStyleRulePropertyName = "--ui-builder-selected-style-property";
        const string k_ChangeUIStyleValueUndoMessage = "Change UI Style Value";

        const string k_VariableNameFieldMustBeValidMessage =
            "Name must only contain letters, numbers, '-' or '_'. No spaces or other special characters allowed.";

        const string k_VariableEnumFieldMustBeValidMessage = "An empty Enum is not a valid value.";
        const string k_WarningBoxUssClassName = "warning-info-box";

        const int k_DefaultMenuFontSize = 12;

        protected virtual VariablesListItem CreateListItem() => new ();

        protected virtual StyleComplexSelector GetRootRule(StyleRule rule) { return null; }

        protected virtual void OnStyleSheetModified()
        {
            UpdateOverrideFoldoutTrackedProperties();
        }

        protected virtual void AfterAddVariable()
        {
            RefreshVariablesList();
        }

        public virtual void ExtractToGlobalVariable() { }

        void BindItem(VisualElement e, int i)
        {
            var item = e as VariablesListItem;
            var listItem = m_VariablesItemsSource[i].values[0];

            item.itemValueField.enabledSelf = true;
            item.itemValueField.textEdition.AcceptCharacter = (c) => c != '"';

            item.userData = m_VariablesItemsSource[i];
            item.Q<TextField>(VariablesListItem.k_NameFieldName)
                .SetValueWithoutNotify(m_VariablesItemsSource[i].name.TrimStart('-'));

            BindValueToItem(item, listItem, i);
            var variableType = GetVariableType(listItem, styleSheet);
            if (variableType == null)
                return;

            OnChangeVariableType(item, variableType.Value);
            item.itemTypeField.SetValueWithoutNotify(variableType.Value.ToString());

            EnableWarningBox(item, k_VariableNameFieldMustBeValidMessage, !IsValidName(item.itemNameField.text));

            item.contextualMenuManipulator = new ContextualMenuManipulator((evt) =>
            {
                m_VariablesListView.AddToSelection(i);
                BuildContextMenu(evt, item);
            });
            item.AddManipulator(item.contextualMenuManipulator);
        }

        public void Refresh(StyleRule rule, Func<StyleRule> getOrCreateRule = null)
        {
            styleRule = rule;
            m_GetOrCreateStyleRule = getOrCreateRule;
            RefreshVariablesList();
        }

        void BindValueToItem(VariablesListItem item, StyleValueHandle listItem, int index)
        {
            if (listItem.valueType == StyleValueType.Function)
            {
                var functionText = styleSheet.ReadVariable(m_VariablesItemsSource[index].values[2]);
                item.itemValueField.SetValueWithoutNotify(string.Join(" ", functionText?.Trim('"')));
                item.itemValueField.enabledSelf = false;
                return;
            }

            var variableType = GetVariableType(listItem, styleSheet);
            if (variableType == null)
                return;

            switch (variableType.Value)
            {
                case VariableType.Keyword:
                    var keywordText = styleSheet.ReadKeyword(listItem);
                    item.itemKeywordField.SetValueWithoutNotify(keywordText.ToString());
                    break;
                case VariableType.Color:
                    var color = styleSheet.ReadColor(listItem);
                    item.itemColorField.SetValueWithoutNotify(color);
                    break;
                case VariableType.AssetReference:
                    var asset = styleSheet.ReadAssetReference(listItem);
                    item.itemAssetField.SetValueWithoutNotify(asset);
                    break;
                case VariableType.Length:
                    var length = styleSheet.ReadLength(listItem);
                    item.itemLengthField.SetValueWithoutNotify(length);
                    break;
                case VariableType.Angle:
                    var angle = styleSheet.ReadAngle(listItem);
                    item.itemAngleField.SetValueWithoutNotify(angle);
                    break;
                case VariableType.Time:
                    var dimension = styleSheet.ReadDimension(listItem);
                    item.itemTimeValueField.SetValueWithoutNotify(dimension.ToTime());
                    break;
                case VariableType.Float:
                    var floatText = styleSheet.ReadFloat(listItem);
                    item.itemFloatField.SetValueWithoutNotify(floatText);
                    break;
                case VariableType.Enum:
                    var enumText = styleSheet.ReadEnum(listItem);
                    item.itemValueField.SetValueWithoutNotify(string.Join(" ", enumText?.Trim('"')));
                    break;
                case VariableType.String:
                    var str = styleSheet.ReadString(listItem);
                    item.itemValueField.SetValueWithoutNotify(string.Join(" ", str?.Trim('"')));
                    break;
            }
        }

        void UnbindItem(VisualElement e, int _)
        {
            var item = e as VariablesListItem;
            EnableWarningBox(item, null, false);
            item.RemoveManipulator(item.contextualMenuManipulator);
        }

        void DestroyItem(VisualElement e)
        {
            var item = e as VariablesListItem;
            item.RemoveManipulator(item.contextualMenuManipulator);
            item.contextualMenuManipulator = null;
        }

        void EnsureStyleRuleExists()
        {
            if (styleRule != null)
                return;

            if (m_GetOrCreateStyleRule != null)
                styleRule = m_GetOrCreateStyleRule();
        }

        public void OnCreateVariable(string choice)
        {
            EnsureStyleRuleExists();
            if (styleRule == null)
                return;

            Enum.TryParse<VariableType>(choice, out var type);
            var name = GenerateDefaultName();

            var command = new AddStyleRulePropertyCommand(styleSheet, styleRule, name, type);
            command.Execute();

            AfterAddVariable();
        }

        public void DuplicateVariable()
        {
            var selectedIndices = m_VariablesListView.selectedIndicesList;
            if (selectedIndices.Count == 0)
                return;

            var undoGroup = Undo.GetCurrentGroup();

            foreach (var selectedIndex in selectedIndices)
            {
                if (selectedIndex < 0 || selectedIndex >= m_VariablesItemsSource.Count)
                    continue;

                var styleProperty = m_VariablesItemsSource[selectedIndex];
                var newName = GenerateDefaultName(styleProperty.name);

                var command = new DuplicateStyleRulePropertyCommand(styleSheet, styleRule, styleProperty, newName);
                command.Execute();
            }

            Undo.CollapseUndoOperations(undoGroup);

            AfterAddVariable();
        }

        public void DeleteVariable(BaseListView listView)
        {
            var undoGroup = Undo.GetCurrentGroup();

            if (listView.selectedIndex == -1 && m_VariablesItemsSource.Count > 0)
            {
                var index = m_VariablesItemsSource.Count - 1;
                var prop = m_VariablesItemsSource[index];
                if (prop != null)
                    new RemoveStyleRulePropertyCommand(styleSheet, styleRule, prop).Execute();
            }
            else
            {
                foreach (var selectedIndex in listView.selectedIndicesList)
                {
                    if (selectedIndex < 0 || selectedIndex >= m_VariablesItemsSource.Count)
                        continue;

                    var prop = m_VariablesItemsSource[selectedIndex];
                    if (prop != null)
                        new RemoveStyleRulePropertyCommand(styleSheet, styleRule, prop).Execute();
                }
            }

            Undo.CollapseUndoOperations(undoGroup);

            OnStyleSheetModified();
            RefreshVariablesList();
        }

        public void MoveVariable(int index, int direction)
        {
            if (index == -1)
                return;

            var newIndex = index + direction;
            if (newIndex < 0 || newIndex >= m_VariablesItemsSource.Count || index < 0 ||
                index >= m_VariablesItemsSource.Count)
                return;

            (m_VariablesItemsSource[newIndex], m_VariablesItemsSource[index]) =
                (m_VariablesItemsSource[index], m_VariablesItemsSource[newIndex]);

            var newOrder = m_VariablesItemsSource.ToArray();
            new ReorderStyleRulePropertiesCommand(styleSheet, styleRule, newOrder).Execute();

            m_VariablesListView.selectedIndex = newIndex;

            OnStyleSheetModified();
            m_VariablesListView.RefreshItems();
        }


        static VariablesListItem GetListItem(EventBase evt)
        {
            return (evt.target as VisualElement)?.GetFirstAncestorOfType<VariablesListItem>();
        }

        void WritePropertyValue<T>(ChangeEvent<T> evt, Func<VariablesListItem, VisualElement> fieldSelector,
            VariableType type)
        {
            var item = GetListItem(evt);
            if (item == null || evt.target != fieldSelector(item))
                return;

            if (item.userData is not StyleProperty styleProperty)
                return;

            new WriteStyleRulePropertyValueCommand<T>(styleSheet, styleProperty, type, evt.newValue).Execute();
            OnStyleSheetModified();
        }

        void OnStringChanged(ChangeEvent<string> evt)
        {
            var item = GetListItem(evt);
            if (item == null)
                return;

            if (evt.target == item.itemNameField)
                OnNameChanged(evt, item);
            else if (evt.target == item.itemValueField || evt.target == item.itemKeywordField)
                OnValueChanged(evt, item);
            else if (evt.target == item.itemTypeField)
                OnTypeChanged(evt, item);
        }

        void OnNameChanged(ChangeEvent<string> evt, VariablesListItem item)
        {
            if (item.userData is not StyleProperty styleProperty)
                return;

            var newName = k_VariablePrefix + evt.newValue.TrimStart('-');
            if (!IsValidName(newName))
                return;

            new RenameStyleRulePropertyCommand(styleSheet, styleProperty, newName).Execute();
            OnStyleSheetModified();
        }

        void OnValueChanged(ChangeEvent<string> evt, VariablesListItem item)
        {
            if (item.userData is not StyleProperty styleProperty)
                return;

            var newValue = evt.newValue.Replace("\"", "");

            if (styleProperty.values[0].valueType == StyleValueType.Enum && string.IsNullOrEmpty(evt.newValue))
            {
                EnableWarningBox(item, k_VariableEnumFieldMustBeValidMessage, true);
                item.itemValueField.SetValueWithoutNotify(evt.previousValue);
                return;
            }

            switch (styleProperty.values[0].valueType)
            {
                case StyleValueType.String:
                    new WriteStyleRulePropertyValueCommand<string>(styleSheet, styleProperty,
                        VariableType.String, newValue).Execute();
                    break;
                case StyleValueType.Enum:
                    new WriteStyleRulePropertyValueCommand<string>(styleSheet, styleProperty,
                        VariableType.Enum, newValue).Execute();
                    break;
                case StyleValueType.Keyword:
                    new WriteStyleRulePropertyValueCommand<StyleValueKeyword>(styleSheet, styleProperty,
                        VariableType.Keyword, Enum.Parse<StyleValueKeyword>(evt.newValue)).Execute();
                    break;
            }

            OnStyleSheetModified();
        }

        void OnFloatChanged(ChangeEvent<float> evt) =>
            WritePropertyValue(evt, i => i.itemFloatField, VariableType.Float);

        void OnLengthChanged(ChangeEvent<Length> evt) =>
            WritePropertyValue(evt, i => i.itemLengthField, VariableType.Length);

        void OnAngleChanged(ChangeEvent<Angle> evt) =>
            WritePropertyValue(evt, i => i.itemAngleField, VariableType.Angle);

        void OnColourChanged(ChangeEvent<Color> evt) =>
            WritePropertyValue(evt, i => i.itemColorField, VariableType.Color);

        void OnTimeChanged(ChangeEvent<TimeValue> evt) =>
            WritePropertyValue(evt, i => i.itemTimeValueField, VariableType.Time);

        void OnAssetChanged(ChangeEvent<Object> evt) =>
            WritePropertyValue(evt, i => i.itemAssetField, VariableType.AssetReference);

        void OnTypeChanged(ChangeEvent<string> evt, VariablesListItem item)
        {
            if (item.userData is not StyleProperty styleProperty)
                return;

            Enum.TryParse<VariableType>(evt.newValue, out var type);

            new ResetStyleRulePropertyValueCommand(styleSheet, styleProperty, type).Execute();

            OnChangeVariableType(item, type);
            OnStyleSheetModified();
            RefreshVariablesList();
        }

        void OnListReordered(int previousIndex, int newIndex)
        {
            var newOrder = m_VariablesItemsSource.ToArray();
            new ReorderStyleRulePropertiesCommand(styleSheet, styleRule, newOrder).Execute();
            OnStyleSheetModified();
            m_VariablesListView.RefreshItems();
        }

        protected void RegisterStyleSheetUndo()
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, k_ChangeUIStyleValueUndoMessage);
        }

        string GenerateDefaultName(string baseName = k_DefaultVariableName)
        {
            var suffix = 1;
            if (baseName != k_DefaultVariableName)
            {
                var regex = new Regex(@"-(\d+)$");
                var match = regex.Match(baseName);
                if (match.Success)
                {
                    var number = int.Parse(match.Groups[1].Value);
                    suffix = number + 1;
                    baseName = baseName.Substring(0, match.Index);
                }
            }

            var name = $"{baseName}-{suffix}";

            if (!m_VariablesItemsSource.Exists(prop => prop.name.Equals(name)))
                return name;

            do
            {
                name = $"{baseName}-{suffix}";
                suffix++;
            } while (m_VariablesItemsSource.Exists(prop => prop.name.Equals(name)));

            return name;
        }

        void OnChangeVariableType(VariablesListItem item, VariableType type)
        {
            item.itemValueField.RemoveFromClassList(k_ActiveFieldClassName);
            item.itemFloatField.RemoveFromClassList(k_ActiveFieldClassName);
            item.itemLengthField.RemoveFromClassList(k_ActiveFieldClassName);
            item.itemAngleField.RemoveFromClassList(k_ActiveFieldClassName);
            item.itemTimeValueField.RemoveFromClassList(k_ActiveFieldClassName);
            item.itemColorField.RemoveFromClassList(k_ActiveFieldClassName);
            item.itemAssetField.RemoveFromClassList(k_ActiveFieldClassName);
            item.itemKeywordField.RemoveFromClassList(k_ActiveFieldClassName);

            switch (type)
            {
                case VariableType.Float:
                    item.itemFloatField.AddToClassList(k_ActiveFieldClassName);
                    break;
                case VariableType.Color:
                    item.itemColorField.AddToClassList(k_ActiveFieldClassName);
                    break;
                case VariableType.Length:
                    item.itemLengthField.AddToClassList(k_ActiveFieldClassName);
                    break;
                case VariableType.Angle:
                    item.itemAngleField.AddToClassList(k_ActiveFieldClassName);
                    break;
                case VariableType.Time:
                    item.itemTimeValueField.AddToClassList(k_ActiveFieldClassName);
                    break;
                case VariableType.String:
                case VariableType.Enum:
                    item.itemValueField.AddToClassList(k_ActiveFieldClassName);
                    break;
                case VariableType.Keyword:
                    item.itemKeywordField.AddToClassList(k_ActiveFieldClassName);
                    break;
                case VariableType.AssetReference:
                    item.itemAssetField.AddToClassList(k_ActiveFieldClassName);
                    break;
            }
        }

        void BuildContextMenu(ContextualMenuPopulateEvent evt, VariablesListItem currentRow)
        {
            var menu = evt.menu;
            var index = m_VariablesItemsSource.IndexOf(currentRow.userData as StyleProperty);
            var isRoot = GetRootRule(styleRule) != null;
            var multipleSelected = m_VariablesListView.selectedIndicesList.Count > 1;

            menu.AppendAction("Extract to Global variable", _ =>
            {
                ExtractToGlobalVariable();
            }, _ => isRoot ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

            menu.AppendAction("Delete", _ =>
            {
                DeleteVariable(m_VariablesListView);
            });

            menu.AppendAction("Duplicate", _ =>
            {
                DuplicateVariable();
            });

            menu.AppendAction("Move Up", _ =>
                {
                    MoveVariable(index, -1);
                },
                _ => index > 0 && !multipleSelected
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);

            menu.AppendAction("Move Down", _ =>
                {
                    MoveVariable(index, 1);
                },
                _ => index < m_VariablesItemsSource.Count - 1 && !multipleSelected
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
        }

        void ValidateName(KeyUpEvent evt)
        {
            var listItem = GetListItem(evt);
            if (listItem == null) return;

            var nameField = listItem.itemNameField;
            if (!nameField.Contains(evt.target as VisualElement))
                return;

            EnableWarningBox(listItem, k_VariableNameFieldMustBeValidMessage,
                !IsValidName(nameField.text));
        }

        static void EnableWarningBox(VisualElement item, string message, bool enabled)
        {
            var nameWarningHelpBox = item.Q<HelpBox>();

            if (enabled && nameWarningHelpBox == null)
            {
                nameWarningHelpBox =
                    new HelpBox(L10n.Tr(message), HelpBoxMessageType.Warning) { name = k_WarningBoxUssClassName };
                item.Add(nameWarningHelpBox);
            }

            if (nameWarningHelpBox != null)
            {
                nameWarningHelpBox.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        static bool IsValidName(string name)
        {
            return k_ValidNameRegex.Match(name.TrimStart('-')).Success;
        }

        protected void RefreshVariablesList()
        {
            if (styleRule == null)
            {
                m_VariablesItemsSource.Clear();
                if (m_VariablesListView != null)
                {
                    m_VariablesListView.itemsSource = m_VariablesItemsSource;
                    m_VariablesListView.Rebuild();
                }

                UpdateOverrideFoldoutTrackedProperties();
                return;
            }

            var newVariablesList = new List<StyleProperty>();
            foreach (var property in styleRule.properties)
            {
                if (property.isCustomProperty && property.name != k_SelectedStyleRulePropertyName)
                    newVariablesList.Add(property);
            }

            m_VariablesItemsSource = newVariablesList;
            m_VariablesListView.itemsSource = m_VariablesItemsSource;
            m_VariablesListView.RefreshItems();
            UpdateOverrideFoldoutTrackedProperties();
        }

        void UpdateOverrideFoldoutTrackedProperties()
        {
            // Find the parent OverrideFoldout that contains this VariablesInspector
            var parentFoldout = GetFirstAncestorOfType<OverrideFoldout>();
            if (parentFoldout == null)
                return;

            // Clear existing tracked properties
            parentFoldout.trackedProperties.Clear();

            // Add all variable names to the tracked properties
            foreach (var variable in m_VariablesItemsSource)
            {
                parentFoldout.AddTrackedProperty(variable.name);
            }

            var nameCount = m_VariablesItemsSource.Count;
            if (nameCount == 0)
            {
                parentFoldout.UpdateTrackedProperties(Array.Empty<string>());
            }
            else
            {
                var names = new string[nameCount];
                for (var i = 0; i < nameCount; i++)
                    names[i] = m_VariablesItemsSource[i].name;
                parentFoldout.UpdateTrackedProperties(names);
            }
        }
    }
}
