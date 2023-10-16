// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderInspectorHeader
    {
        // Shared variables regardless of selected type
        BuilderInspector m_Inspector;
        BuilderSelection m_Selection;
        VisualElement m_Header;

        VisualElement m_InnerHeader;
        VisualElement m_Icon;

        FieldStatusIndicator m_StatusIndicator;
        Label m_Pill;
        TextField m_TextField;
        internal BuilderDataSourceAndPathView m_DataSourceAndPathView;
        private VisualElement m_DataSourceViewContainer;

        UnityEngine.UIElements.HelpBox m_EditorWarningHelpBox;
        VisualElement m_ErrorIcon;

        VisualElement currentVisualElement => m_Inspector.currentVisualElement;

        StyleSheet styleSheet => m_Inspector.styleSheet;
        public VisualElement header => m_Header;

        // Store callbacks to reduce delegate allocations
        EventCallback<ChangeEvent<string>> m_ElementNameChangeCallback;
        EventCallback<ChangeEvent<string>> m_SelectorNameChangeCallback;
        EventCallback<KeyDownEvent> m_SelectorEnterKeyDownCallback;

        IManipulator m_RightClickManipulator;

        // ReSharper disable once MemberCanBePrivate.Global
        internal const string refreshMarkerName = "BuilderInspectorHeader.Refresh";
        static readonly ProfilerMarker k_RefreshMarker = new (refreshMarkerName);

        public BuilderInspectorHeader(BuilderInspector inspector)
        {
            m_Inspector = inspector;
            m_Selection = inspector.selection;
            m_Header = m_Inspector.Q<VisualElement>("header-container");
            m_StatusIndicator = m_Header.Q<FieldStatusIndicator>("header-field-status-indicator");
            m_StatusIndicator.populateMenuItems = (menu) =>
            {
                BuildNameFieldContextualMenu(menu, m_TextField);
            };

            m_InnerHeader = m_Inspector.Q<VisualElement>("header-container-minor");
            m_Icon = m_Inspector.Q<VisualElement>("header-icon");

            m_Pill = m_Inspector.Q<Label>("header-selected-pill");
            m_Pill.AddToClassList("unity-builder-tag-pill");

            m_TextField = m_Inspector.Q<TextField>("header-selected-text-field");
            m_TextField.isDelayed = true;
            m_TextField.tooltip = "name";
            m_TextField.bindingPath = "name";

            m_EditorWarningHelpBox = m_Inspector.Q<UnityEngine.UIElements.HelpBox>("header-editor-warning-help-box");
            m_EditorWarningHelpBox.text = BuilderConstants.HeaderSectionHelpBoxMessage;
            m_ErrorIcon = m_Inspector.Q<VisualElement>("header-error-icon");

            // Warning must be hidden at first
            m_ErrorIcon.style.backgroundImage = EditorGUIUtility.LoadIcon("console.erroricon");
            AdjustBottomPadding(false);

            // Store callbacks to reduce delegate allocations
            m_ElementNameChangeCallback = new EventCallback<ChangeEvent<string>>(OnNameAttributeChange);
            m_SelectorNameChangeCallback = new EventCallback<ChangeEvent<string>>(OnStyleSelectorNameChange);
            m_SelectorEnterKeyDownCallback = new EventCallback<KeyDownEvent>(OnEnterStyleSelectorNameChange);

            m_TextField.RegisterValueChangedCallback(m_ElementNameChangeCallback);
            m_RightClickManipulator = new ContextualMenuManipulator(BuildNameFieldContextualMenu);

            m_DataSourceViewContainer = new VisualElement();

            m_Header.Add(m_DataSourceViewContainer);
            m_DataSourceAndPathView = new BuilderDataSourceAndPathView(m_Inspector)
            {
                fieldsContainer = m_DataSourceViewContainer,
                onNotifyAttributesChanged = () => m_Inspector.selection.NotifyOfHierarchyChange(m_Inspector)
            };
        }

        public void Refresh()
        {
            using var marker = k_RefreshMarker.Auto();

            if (currentVisualElement == null)
            {
                return;
            }

            if (m_Selection.selectionType == BuilderSelectionType.Nothing)
            {
                return;
            }

            m_TextField.UnregisterValueChangedCallback(m_SelectorNameChangeCallback);
            m_TextField.UnregisterCallback(m_SelectorEnterKeyDownCallback);
            m_TextField.UnregisterValueChangedCallback(m_ElementNameChangeCallback);
            m_TextField.RemoveManipulator(m_RightClickManipulator);

            if (m_Selection.selectionType == BuilderSelectionType.StyleSelector ||
                m_Selection.selectionType == BuilderSelectionType.ParentStyleSelector)
            {
                m_TextField.RegisterValueChangedCallback(m_SelectorNameChangeCallback);
                m_TextField.RegisterCallback(m_SelectorEnterKeyDownCallback);

                ToggleNameOverrideBox(false);

                m_StatusIndicator.style.visibility = Visibility.Hidden;

                (m_TextField.labelElement as INotifyValueChanged<string>).SetValueWithoutNotify(BuilderConstants.BuilderInspectorSelector);

                m_TextField.SetValueWithoutNotify(BuilderSharedStyles.GetSelectorString(currentVisualElement));

                m_Icon.style.backgroundImage = EditorGUIUtility.Load("StyleSheet Icon") as Texture2D;
            }
            else
            {
                ToggleNameOverrideBox(!string.IsNullOrEmpty(currentVisualElement.name));
                m_StatusIndicator.style.visibility = Visibility.Visible;
                m_TextField.SetValueWithoutNotify(currentVisualElement.name);
                SetTypeAndIcon();

                if (m_Selection.selectionType == BuilderSelectionType.Element)
                {
                    m_TextField.RegisterValueChangedCallback(m_ElementNameChangeCallback);
                    m_TextField.AddManipulator(m_RightClickManipulator);
                }
            }

            var currentElementType = currentVisualElement.GetType();
            var isEditorOnlyElement = false;

            if (currentElementType.Namespace != null)
            {
                isEditorOnlyElement = currentElementType.Namespace.Contains("UnityEditor");
            }

            m_Pill.style.display = isEditorOnlyElement ? DisplayStyle.Flex : DisplayStyle.None;
            AdjustBottomPadding(isEditorOnlyElement && !m_Inspector.document.fileSettings.editorExtensionMode);

            if (m_Selection.selectionType is
                BuilderSelectionType.ElementInTemplateInstance or
                BuilderSelectionType.ElementInControlInstance or
                BuilderSelectionType.ElementInParentDocument or
                BuilderSelectionType.Element)
            {
                m_DataSourceViewContainer.style.display = DisplayStyle.Flex;
                m_DataSourceAndPathView.SetAttributesOwner(m_Inspector.visualTreeAsset, currentVisualElement, m_Selection.selectionType == BuilderSelectionType.ElementInTemplateInstance);

                if (m_DataSourceAndPathView.refreshScheduledItem != null)
                {
                    // Pause to stop it in case it's already running; and then restart it to execute it.
                    m_DataSourceAndPathView.refreshScheduledItem.Pause();
                    m_DataSourceAndPathView.refreshScheduledItem.Resume();
                }
                else
                {
                    m_DataSourceAndPathView.refreshScheduledItem = m_DataSourceAndPathView.fieldsContainer.schedule.Execute(() => m_DataSourceAndPathView.Refresh());
                }
            }
            else
            {
                m_DataSourceViewContainer.style.display = DisplayStyle.None;
            }
        }

        private void SetTypeAndIcon()
        {
            string typeName;

            if (currentVisualElement.typeName == nameof(TemplateContainer))
            {
                typeName = BuilderConstants.BuilderInspectorTemplateInstance;
                m_Icon.style.backgroundImage = EditorGUIUtility.Load("VisualTreeAsset Icon") as Texture2D;
            }
            else
            {
                typeName = currentVisualElement.typeName;
                m_Icon.style.backgroundImage =
                    BuilderLibraryContent.GetTypeLibraryLargeIcon(currentVisualElement.GetType());
            }

            (m_TextField.labelElement as INotifyValueChanged<string>).SetValueWithoutNotify(typeName);
        }

        private void BuildNameFieldContextualMenu(DropdownMenu menu, object target)
        {
            menu.AppendAction(
                BuilderConstants.ContextMenuUnsetMessage,
                UnsetName,
                action =>
                {
                    var vea = currentVisualElement.GetVisualElementAsset();

                    return vea != null && vea.HasAttribute("name")
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled;
                },
                target);
        }

        private void BuildNameFieldContextualMenu(ContextualMenuPopulateEvent evt)
        {
            BuildNameFieldContextualMenu(evt.menu, evt.elementTarget);
        }

        private void UnsetName(DropdownMenuAction action)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(m_Inspector.visualTreeAsset,
                BuilderConstants.ChangeAttributeValueUndoMessage);

            // Unset value in asset.
            var vea = currentVisualElement.GetVisualElementAsset();

            vea.RemoveAttribute("name");

            m_TextField.SetValueWithoutNotify(string.Empty);

            m_Inspector.CallInitOnElement();

            // Notify of changes.
            m_Selection.NotifyOfHierarchyChange(m_Inspector);

            ToggleNameOverrideBox(false);
        }

        void OnNameAttributeChange(ChangeEvent<string> evt)
        {
            if (m_Selection.selectionType == BuilderSelectionType.Element)
            {
                m_Inspector.attributesSection.OnValidatedAttributeValueChange(evt, BuilderNameUtilities.attributeRegex,
                    BuilderConstants.AttributeValidationSpacialCharacters);
                ToggleNameOverrideBox(true);
            }
        }

        void OnStyleSelectorNameChange(ChangeEvent<string> evt)
        {
            if (m_Selection.selectionType != BuilderSelectionType.StyleSelector)
                return;

            if (evt.newValue.Length == 0)
            {
                Refresh();
                return;
            }

            if (evt.newValue == evt.previousValue)
                return;

            ValidateStyleSelectorNameChange(evt.newValue);
        }

        void OnEnterStyleSelectorNameChange(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
                return;

            if (m_Selection.selectionType != BuilderSelectionType.StyleSelector)
                return;

            if (m_TextField.text.Length == 0)
            {
                Refresh();
                return;
            }

            if (m_TextField.text == currentVisualElement.name)
                return;

            ValidateStyleSelectorNameChange(m_TextField.text);

            evt.StopImmediatePropagation();
        }

        void ValidateStyleSelectorNameChange(string value)
        {
            if (!BuilderNameUtilities.styleSelectorRegex.IsMatch(value))
            {
                Builder.ShowWarning(string.Format(BuilderConstants.StyleSelectorValidationSpacialCharacters, "Name"));
                m_TextField.schedule.Execute(() =>
                {
                    var baseInput = m_TextField.Q(TextField.textInputUssName);
                    if (baseInput.focusController != null)
                        baseInput.focusController.DoFocusChange(baseInput);

                    m_TextField.SetValueWithoutNotify(value);
                    m_TextField.textSelection.SelectAll();
                });
                return;
            }

            Undo.RegisterCompleteObjectUndo(
                styleSheet, BuilderConstants.RenameSelectorUndoMessage);

            BuilderSharedStyles.SetSelectorString(currentVisualElement, styleSheet, value);

            m_Selection.NotifyOfHierarchyChange(m_Inspector);
            m_Selection.NotifyOfStylingChange(m_Inspector);
        }

        public void Enable()
        {
            m_Header.SetEnabled(true);
        }

        public void Disable()
        {
            m_Header.SetEnabled(false);
        }

        void AdjustBottomPadding(bool isHelpBoxShowing)
        {
            if (isHelpBoxShowing)
            {
                m_EditorWarningHelpBox.style.display = DisplayStyle.Flex;
                m_ErrorIcon.style.display = DisplayStyle.Flex;
                m_Header.style.paddingBottom = 0;
            }
            else
            {
                m_EditorWarningHelpBox.style.display = DisplayStyle.None;
                m_ErrorIcon.style.display = DisplayStyle.None;
                m_Header.style.paddingBottom = 12;
            }
        }

        void ToggleNameOverrideBox(bool isOverridden)
        {
            m_InnerHeader.EnableInClassList(BuilderConstants.InspectorLocalStyleOverrideClassName, isOverridden);
            m_StatusIndicator.tooltip = isOverridden
                ? BuilderConstants.FieldStatusIndicatorInlineTooltip
                : BuilderConstants.FieldStatusIndicatorDefaultTooltip;
        }
    }
}
