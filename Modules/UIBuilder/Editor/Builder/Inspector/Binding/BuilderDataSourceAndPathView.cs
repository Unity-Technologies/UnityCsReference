// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using UIEHelpBox = UnityEngine.UIElements.HelpBox;

namespace Unity.UI.Builder
{
    /// <summary>
    /// Provides a view to the data source, data source type and binding path property of a VisualElement and a Binding.
    /// </summary>
    internal class BuilderDataSourceAndPathView : BuilderUxmlAttributesView
    {
        internal const string k_BindingAttr_DataSource = "data-source";
        internal const string k_BindingAttr_DataSourceType = "data-source-type";
        internal const string k_BindingAttr_DataSourcePath = "data-source-path";
        internal const string k_DataSourceObjectTooltip = "Add an object to use as the data source for this binding.";
        internal const string k_DataSourceTypeTooltip = "If a source is not yet available, a data source type can be defined. It may provide assistance while authoring by populating the data source path field with options.";

        internal struct TestAccess
        {
            public BuilderObjectField dataSourceField;
            public BaseField<string> dataSourceTypeField;
            public TextField dataSourcePathField;
            public BuilderDataSourcePathCompleter dataSourcePathCompleter;
            public UIEHelpBox dataSourceWarningBox;
            public UIEHelpBox pathWarningBox;
        }

        // Note: They are internal only to be accessible in tests
        internal TestAccess testAccess => new()
        {
            dataSourceField = m_DataSourceField,
            dataSourceTypeField = m_DataSourceTypeField,
            dataSourcePathField = m_DataSourcePathField,
            dataSourcePathCompleter = m_DataSourcePathCompleter,
            dataSourceWarningBox = m_DataSourceWarningBox,
            pathWarningBox = m_PathWarningBox,
        };

        PersistedFoldout m_BindingsFoldout;
        ToggleButtonGroup m_ButtonStrip;
        VisualElement m_SourceWidgetContainer;
        VisualElement m_AssetFieldContainer;
        VisualElement m_TypeFieldContainer;
        VisualElement m_PathFieldContainer;

        protected BuilderObjectField m_DataSourceField;
        protected BaseField<string> m_DataSourceTypeField;
        protected TextField m_DataSourcePathField;
        BuilderDataSourcePathCompleter m_DataSourcePathCompleter;
        UIEHelpBox m_DataSourceWarningBox;
        UIEHelpBox m_PathWarningBox;

        private IVisualElementScheduledItem m_UpdateControlsScheduledItem;

        /// <summary>
        /// The data source to bind.
        /// </summary>
        public Object dataSource => m_DataSourceField?.value;

        /// <summary>
        /// Gets data source inherited from the selected VisualElement.
        /// </summary>
        public object inheritedDataSource
        {
            get
            {
                var startingElement = isBinding ? Builder.ActiveWindow.inspector.attributesSection.currentElement : currentElement.parent;
                if (startingElement == null)
                    return null;

                DataBindingUtility.TryGetRelativeDataSourceFromHierarchy(startingElement, out var source);
                return source;
            }
        }

        /// <summary>
        /// The type of the possible data source to bind.
        /// </summary>
        public Type dataSourceType
        {
            get
            {
                var fullTypeName = m_DataSourceTypeField?.value;

                if (!string.IsNullOrEmpty(fullTypeName))
                    return Type.GetType(fullTypeName);
                return null;
            }
        }

        /// <summary>
        /// Gets data source type inherited from the selected VisualElement.
        /// </summary>
        public Type inheritedDataSourceType
        {
            get
            {
                var startingElement = isBinding ? Builder.ActiveWindow.inspector.attributesSection.currentElement : currentElement.parent;
                if (startingElement == null)
                    return null;

                DataBindingUtility.TryGetRelativeDataSourceTypeFromHierarchy(startingElement, out var sourceType);
                return sourceType;
            }
        }

        /// <summary>
        /// The data source path to property of the data source to bind.
        /// </summary>
        public string dataSourcePath => m_DataSourcePathField?.text;

        public string bindingSerializedPropertyPathRoot { get; set; }

        public UxmlSerializedDataDescription bindingUxmlSerializedDataDescription { get; set; }

        public UxmlSerializedDataDescription uxmlSerializedDataDescription => bindingUxmlSerializedDataDescription ?? m_SerializedDataDescription;

        protected bool isBinding => bindingUxmlSerializedDataDescription != null;

        /// <summary>
        /// Notifies attributes have changed.
        /// </summary>
        public Action onNotifyAttributesChanged;

        /// <inheritdoc/>
        protected override bool IsAttributeIgnored(UxmlAttributeDescription attribute) => true;

        protected override BindableElement CreateTraitsAttributeField(UxmlAttributeDescription attribute)
        {
            var field =  base.CreateTraitsAttributeField(attribute);
            UpdateAttribute(field, attribute.name);
            return field;
        }

        protected override BuilderStyleRow CreateSerializedAttributeRow(UxmlSerializedAttributeDescription attribute, string propertyPath, VisualElement parent = null)
        {
            var row = base.CreateSerializedAttributeRow(attribute, propertyPath, parent);
            row.Q<PropertyField>()?.RegisterCallback<SerializedPropertyBindEvent, string>(OnPropertyFieldBound, attribute.name);
            return row;
        }

        void OnPropertyFieldBound(SerializedPropertyBindEvent evt, string attributeName)
        {
            var target = evt.elementTarget;
            var bindingAttribute = attributeName;
            fieldsContainer.schedule.Execute(() =>
            {
                UpdateAttribute(target, bindingAttribute);

                if (target?.panel == null)
                    return;

                UpdateFieldStatus(target);
            });
        }

        /// <inheritdoc/>
        protected override object GetAttributeValue(UxmlAttributeDescription attribute)
        {
            var attributeValue = base.GetAttributeValue(attribute);
            if (attribute.name is k_BindingAttr_DataSource)
            {
                return attributeValue ?? inheritedDataSource;
            }

            if (attribute.name is k_BindingAttr_DataSourceType)
            {
                return attributeValue ?? inheritedDataSourceType;
            }

            return attributeValue;
        }

        protected virtual void UpdateAttribute(VisualElement target, string bindingAttribute)
        {
            switch (bindingAttribute)
            {
                case k_BindingAttr_DataSource:
                    m_DataSourceField = target.Q<BuilderObjectField>();
                    if (m_DataSourceField.value == null)
                    {
                        m_DataSourceField.SetObjectWithoutNotify(inheritedDataSource);
                    }
                    UpdateWarningBox();
                    UpdateCompleter();
                    UpdateFoldoutOverride();
                    break;
                case k_BindingAttr_DataSourceType:
                    m_DataSourceTypeField = target.Q<BaseField<string>>();
                    if (string.IsNullOrEmpty(m_DataSourceTypeField.value))
                    {
                        var type = inheritedDataSourceType;
                        if (type != null)
                        {
                            m_DataSourceTypeField.SetValueWithoutNotify(type.GetFullNameWithAssembly());
                        }
                    }
                    UpdateCompleter();
                    UpdateFoldoutOverride();
                    break;
                case k_BindingAttr_DataSourcePath:
                    m_DataSourcePathField = target.Q<TextField>();
                    m_DataSourcePathField.isDelayed = true;
                    m_DataSourcePathCompleter = new BuilderDataSourcePathCompleter(m_DataSourcePathField);
                    UpdateCompleter();
                    UpdateFoldoutOverride();
                    break;
            }
        }

        /// <inheritdoc/>
        protected override void GenerateUxmlTraitsAttributeFields()
        {
            GenerateDataBindingFields(fieldsContainer);
            UpdateControls();
        }

        /// <inheritdoc/>
        protected override void GenerateSerializedAttributeFields()
        {
            var path = bindingSerializedPropertyPathRoot == null ? serializedRootPath : bindingSerializedPropertyPathRoot + ".";
            var root = new UxmlAssetSerializedDataRoot { dataDescription = uxmlSerializedDataDescription, rootPath = path, classList = { InspectorElement.ussClassName }};
            fieldsContainer.Add(root);
            GenerateDataBindingFields(root);
        }

        void GenerateDataBindingFields(VisualElement root)
        {
            if (m_BindingsFoldout == null)
            {
                m_BindingsFoldout = new PersistedFoldout() { text = "Bindings", classList = { PersistedFoldout.unindentedUssClassName } };

                m_ButtonStrip = new ToggleButtonGroup("Data Source");
                m_ButtonStrip.Add(new Button { text = "Object", style = { flexGrow = 1 }, tooltip = k_DataSourceObjectTooltip });
                m_ButtonStrip.Add(new Button { text = "Type", style = { flexGrow = 1 }, tooltip = k_DataSourceTypeTooltip});
                m_ButtonStrip.isMultipleSelection = false;
                m_ButtonStrip.AddToClassList(ToggleButtonGroup.alignedFieldUssClassName);
                m_ButtonStrip.RegisterValueChangedCallback(evt => SetSourceVisibility(evt.newValue[0]));
                m_BindingsFoldout.Add(m_ButtonStrip);
                m_BindingsFoldout.Add(m_SourceWidgetContainer = new VisualElement() { style = { flexGrow = 1 } });
            }
            else
            {
                m_DataSourceField = null;
                m_DataSourceTypeField = null;
                m_DataSourcePathField = null;
                m_DataSourcePathCompleter = null;
                m_SourceWidgetContainer?.Clear();
                m_PathFieldContainer?.RemoveFromHierarchy();
                m_DataSourceWarningBox?.RemoveFromHierarchy();
            }

            if (isBinding)
            {
                root.Add(m_SourceWidgetContainer);
            }
            else
            {
                root.Add(m_BindingsFoldout);
            }

            // We create a style row and share it between the two data source fields (hackish)
            var styleRow = CreateAttributeRow(k_BindingAttr_DataSource, m_SourceWidgetContainer);
            m_AssetFieldContainer = styleRow.GetLinkedFieldElements()[0];
            m_AssetFieldContainer.parent.Insert(0, m_ButtonStrip);

            // Only create the field and link it to the builder style row created above
            m_TypeFieldContainer = CreateAttributeField(k_BindingAttr_DataSourceType);
            m_AssetFieldContainer.parent.Add(m_TypeFieldContainer);
            var attribute = uxmlSerializedDataDescription?.FindAttributeWithUxmlName(k_BindingAttr_DataSourceType) ?? FindAttribute(k_BindingAttr_DataSourceType);
            SetupStyleRow(styleRow, m_TypeFieldContainer, attribute);

            // Show Asset by default.
            m_ButtonStrip.value = new ToggleButtonGroupState(0b01, 2);
            SetSourceVisibility(true);

            if (isBinding)
            {
                m_DataSourceWarningBox ??= new UIEHelpBox(BuilderConstants.BindingWindowMissingDataSourceErrorMessage, HelpBoxMessageType.Warning);
                m_DataSourceWarningBox.style.display = DisplayStyle.None;

                // Insert the warning box right after the data source field.
                m_BindingsFoldout.Add(m_DataSourceWarningBox);

                m_PathFieldContainer = CreateAttributeRow(k_BindingAttr_DataSourcePath, root);
            }
            else
            {
                m_PathFieldContainer = CreateAttributeRow(k_BindingAttr_DataSourcePath, m_BindingsFoldout);
            }

            m_PathWarningBox ??= new UIEHelpBox("", HelpBoxMessageType.Warning);
            m_PathWarningBox.style.display = DisplayStyle.None;
            root.Add(m_PathWarningBox);
        }

        BuilderStyleRow CreateAttributeRow(string attribute, VisualElement parent)
        {
            if (currentFieldSource == AttributeFieldSource.UxmlTraits)
            {
                return CreateTraitsAttributeRow(FindAttribute(attribute), parent);
            }

            var attributeDesc = uxmlSerializedDataDescription.FindAttributeWithUxmlName(attribute);
            var path = (bindingSerializedPropertyPathRoot == null ? serializedRootPath : bindingSerializedPropertyPathRoot + ".") + attributeDesc.serializedField.Name;
            return CreateSerializedAttributeRow(attributeDesc, path, parent);
        }

        VisualElement CreateAttributeField(string attribute)
        {
            if (currentFieldSource == AttributeFieldSource.UxmlTraits)
            {
                return CreateTraitsAttributeField(FindAttribute(attribute));
            }

            var fieldElement = new UxmlSerializedDataAttributeField();
            var attributeDesc = uxmlSerializedDataDescription.FindAttributeWithUxmlName(attribute);
            var path = (bindingSerializedPropertyPathRoot == null ? serializedRootPath : bindingSerializedPropertyPathRoot + ".") + attributeDesc.serializedField.Name;
            var propertyField = new PropertyField
            {
                name = builderSerializedPropertyFieldName,
                bindingPath = path,
                label = BuilderNameUtilities.ConvertDashToHuman(attribute)
            };
            propertyField.Bind(m_CurrentElementSerializedObject);

            if (!readOnly)
            {
                TrackElementPropertyValue(propertyField, path);
            }

            propertyField.RegisterCallback<SerializedPropertyBindEvent, string>(OnPropertyFieldBound, attribute);
            fieldElement.Add(propertyField);
            return fieldElement;
        }

        void SetSourceVisibility(bool showAsset)
        {
            m_AssetFieldContainer.style.display = showAsset ? DisplayStyle.Flex : DisplayStyle.None;
            m_TypeFieldContainer.style.display = showAsset ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /// <inheritdoc/>
        protected override void ResetAttributeFieldToDefault(VisualElement fieldElement, UxmlAttributeDescription attribute)
        {
            if (m_DataSourceField == fieldElement)
            {
                m_DataSourceField.SetObjectWithoutNotify(inheritedDataSource);
                return;
            }

            if (m_DataSourceTypeField == fieldElement)
            {
                var type = inheritedDataSourceType;

                if (type != null)
                {
                    m_DataSourceTypeField.SetValueWithoutNotify(type.GetFullNameWithAssembly());
                    return;
                }
            }
            base.ResetAttributeFieldToDefault(fieldElement, attribute);
            UpdateFieldStatus(fieldElement);
        }

        /// <inheritdoc/>
        protected override void NotifyAttributesChanged(string attributeName = null)
        {
            ScheduleUpdateControls();
            onNotifyAttributesChanged?.Invoke();
        }

        internal override void UpdateAttributeOverrideStyle(VisualElement fieldElement)
        {
            base.UpdateAttributeOverrideStyle(fieldElement);
            UpdateFoldoutOverride();
        }

        void UpdateFoldoutOverride()
        {
            if (m_DataSourceField?.panel == null || m_DataSourceTypeField?.panel == null || m_DataSourcePathField?.panel == null)
                return;

            m_BindingsFoldout.EnableInClassList(BuilderConstants.InspectorFoldoutOverrideClassName,
                IsAttributeOverriden(m_DataSourceField)
                || IsAttributeOverriden(m_DataSourceTypeField)
                || IsAttributeOverriden(m_DataSourcePathField));
        }

        /// <inheritdoc/>
        protected override FieldValueInfo GetValueInfo(VisualElement field)
        {
            var valueInfo = base.GetValueInfo(field);
            var dataSourceIsInherited = false;
            var dataSourceTypeIsInherited = false;
            var attributeIsOverriden = IsAttributeOverriden(field);

            if (!attributeIsOverriden)
            {
                var dataSourceRootElement = GetRootFieldElement(m_DataSourceField);
                var dataSourceTypeRootElement = GetRootFieldElement(m_DataSourceTypeField);
                // if the data source or the data source type of the target binding is inherited from its VisualElement owner then show the Inherited icon
                if (dataSourceRootElement == field || dataSourceTypeRootElement == field)
                {
                    var startingElement = isBinding ? Builder.ActiveWindow.inspector.attributesSection.currentElement : currentElement;
                    if (DataBindingUtility.TryGetDataSourceOrDataSourceTypeFromHierarchy(startingElement, out var dataSource, out var dataSourceType, out _))
                    {
                        // If the current element is not the one providing the data source,
                        // If the data source set or if the binding path is specified,
                        // and If the data source type is null, then show the inherited status
                        dataSourceIsInherited = dataSource != null && startingElement.dataSource != dataSource && dataSourceType == null;
                        dataSourceTypeIsInherited = dataSource == null && dataSourceType != null;
                    }
                }

                if (dataSourceIsInherited || dataSourceTypeIsInherited)
                {
                    valueInfo.valueSource = new FieldValueSourceInfo(FieldValueSourceInfoType.Inherited);
                }
            }

            return valueInfo;
        }

        protected override void BuildAttributeFieldContextualMenu(DropdownMenu menu, BuilderStyleRow styleRow)
        {
            var fieldElement = styleRow.GetLinkedFieldElements()[0];
            var desc = fieldElement.GetLinkedAttributeDescription();

            var currentUxmlAttributeOwner = attributesUxmlOwner;

            var result = SynchronizePath(bindingSerializedPropertyPathRoot, false);
            if (isBinding && result.success)
            {
                currentUxmlAttributeOwner = result.uxmlAsset;
            }

            if (desc.name is k_BindingAttr_DataSource or k_BindingAttr_DataSourceType)
            {
                menu.AppendAction(
                    BuilderConstants.ContextMenuUnsetObjectMessage,
                    (a) => UnsetAttributeProperty(m_DataSourceField, false),
                    action =>
                    {
                        var attributeName = k_BindingAttr_DataSource;
                        var bindingProperty = GetRemapAttributeNameToCSProperty(k_BindingAttr_DataSource);
                        var isAttributeOverrideAttribute =
                            isInTemplateInstance
                            && BuilderAssetUtilities.HasAttributeOverrideInRootTemplate(currentElement,
                                attributeName);
                        var canUnsetBinding = !isInTemplateInstance && DataBindingUtility.TryGetBinding(currentElement, new PropertyPath(bindingProperty), out _);

                        return (attributesUxmlOwner != null && currentUxmlAttributeOwner.HasAttribute(attributeName)) || isAttributeOverrideAttribute || canUnsetBinding
                            ? DropdownMenuAction.Status.Normal
                            : DropdownMenuAction.Status.Disabled;
                    },
                    styleRow);

                menu.AppendAction(
                    BuilderConstants.ContextMenuUnsetTypeMessage,
                    (a) => UnsetAttributeProperty(m_DataSourceTypeField, false),
                    action =>
                    {
                        var bindingProperty = GetRemapAttributeNameToCSProperty(k_BindingAttr_DataSourceType);
                        var isAttributeOverrideAttribute =
                            isInTemplateInstance
                            && BuilderAssetUtilities.HasAttributeOverrideInRootTemplate(currentElement, k_BindingAttr_DataSourceType);
                        var canUnsetBinding = !isInTemplateInstance && DataBindingUtility.TryGetBinding(currentElement, new PropertyPath(bindingProperty), out _);

                        return (attributesUxmlOwner != null && currentUxmlAttributeOwner.HasAttribute(k_BindingAttr_DataSourceType)) || isAttributeOverrideAttribute || canUnsetBinding
                            ? DropdownMenuAction.Status.Normal
                            : DropdownMenuAction.Status.Disabled;
                    },
                    styleRow);
            }
            else
            {
                base.BuildAttributeFieldContextualMenu(menu, styleRow);
            }
        }

        void ScheduleUpdateControls()
        {
            if (m_UpdateControlsScheduledItem == null)
            {
                m_UpdateControlsScheduledItem = fieldsContainer.schedule.Execute(UpdateControls);
            }
            else
            {
                m_UpdateControlsScheduledItem.Pause();
                m_UpdateControlsScheduledItem.Resume();
            }
        }

        /// <summary>
        ///  Updates the state of controls.
        /// </summary>
        void UpdateControls()
        {
            UpdateCompleter();
            UpdateWarningBox();
        }

        void UpdateCompleter()
        {
            if (m_DataSourcePathCompleter == null || m_DataSourceField == null || m_DataSourceTypeField == null)
                return;

            m_DataSourcePathCompleter.element = currentElement;
            m_DataSourcePathCompleter.bindingDataSource = dataSource ? dataSource : inheritedDataSource;
            m_DataSourcePathCompleter.bindingDataSourceType = dataSourceType ?? inheritedDataSourceType;

            if (bindingSerializedPropertyPathRoot != null)
            {
                CallDeserializeOnElement();
                using (new DisableUndoScope(this))
                {
                    var result = SynchronizePath(bindingSerializedPropertyPathRoot, true);
                    m_DataSourcePathCompleter.binding = result.attributeOwner as DataBinding;
                }
            }

            m_DataSourcePathCompleter.UpdateResults();
        }

        void UpdateWarningBox()
        {
            if (m_DataSourceField == null)
                return;

            if (m_DataSourceWarningBox != null)
                m_DataSourceWarningBox.style.display = dataSource == null && dataSourceType == null ? DisplayStyle.Flex : DisplayStyle.None;

            if (m_PathWarningBox == null)
                return;

            string pathWarningMessage = null;

            if (dataSource != null)
            {
                object source = dataSource;

                if (source is BuilderObjectField.NonUnityObjectValue value)
                    source = value.data;

                if (!string.IsNullOrEmpty(dataSourcePath) && DataBindingUtility.IsPathValid(source, dataSourcePath).returnCode != VisitReturnCode.Ok)
                {
                    pathWarningMessage = BuilderConstants.BindingWindowNotResolvedPathErrorMessage;
                }
            }
            else if (dataSourceType != null)
            {
                if (string.IsNullOrEmpty(dataSourcePath))
                {
                    if (isBinding)
                        pathWarningMessage = BuilderConstants.BindingWindowMissingPathErrorMessage;
                }
                else
                {
                    if (DataBindingUtility.IsPathValid(dataSourceType, dataSourcePath).returnCode != VisitReturnCode.Ok)
                    {
                        pathWarningMessage = BuilderConstants.BindingWindowNotResolvedPathErrorMessage;
                    }
                }
            }

            if (!string.IsNullOrEmpty(pathWarningMessage))
            {
                m_PathWarningBox.text = pathWarningMessage;
                m_PathWarningBox.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_PathWarningBox.style.display = DisplayStyle.None;
            }
        }

        public BuilderDataSourceAndPathView(BuilderInspector inspector) : base(inspector)
        {
        }
    }
}
