// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine.UIElements;
using UIEHelpBox = UnityEngine.UIElements.HelpBox;

namespace Unity.UI.Builder
{
    /// <summary>
    /// Provides a view to uxml attributes of a binding.
    /// </summary>
    internal class BuilderBindingUxmlAttributesView : BuilderDataSourceAndPathView
    {
        internal const string k_BindingAttr_BindingMode = "binding-mode";
        internal const string k_BindingAttr_ConvertersToUi = "source-to-ui-converters";
        internal const string k_BindingAttr_ConvertersToSource = "ui-to-source-converters";
        internal const string k_BindingAttr_UpdateTrigger = "update-trigger";

        internal new struct TestAccess
        {
            public ToggleButtonGroup buttonStrip;
            public BuilderObjectField dataSourceField;
            public BaseField<string> dataSourceTypeField;
            public TextField dataSourcePathField;
            public BuilderDataSourcePathCompleter dataSourcePathCompleter;
            public UIEHelpBox dataSourceWarningBox;
            public UIEHelpBox pathWarningBox;
	        public UIEHelpBox converterGroupWarningBox;
            public EnumField bindingModeField;
            public PersistedFoldout advancedSettings;
            public BindingConvertersField convertersToUi;
            public BindingConvertersField convertersToSource;
            public PopupField<string> updateTrigger;
        }

        // Note: They are internal only to be accessible in tests
        internal new TestAccess testAccess
        {
            get
            {
                var baseTestAccess = base.testAccess;
                return new TestAccess
                {
                    buttonStrip = baseTestAccess.buttonStrip,
                    dataSourceField = baseTestAccess.dataSourceField,
                    dataSourceTypeField = baseTestAccess.dataSourceTypeField,
                    dataSourcePathField = baseTestAccess.dataSourcePathField,
                    dataSourcePathCompleter = baseTestAccess.dataSourcePathCompleter,
                    dataSourceWarningBox = baseTestAccess.dataSourceWarningBox,
                    pathWarningBox = baseTestAccess.pathWarningBox,
                    converterGroupWarningBox = m_ConverterGroupWarningBox,
                    bindingModeField = m_BindingModeField,
                    advancedSettings = m_AdvancedSettings,
                    convertersToUi = m_ConvertersToUi,
                    convertersToSource = m_ConvertersToSource,
                    updateTrigger = m_UpdateTrigger,
                };
            }
        }

        PersistedFoldout m_AdvancedSettings;
        GroupBox m_ConvertersGroupBox;
        EnumField m_BindingModeField;
        BindingConvertersField m_ConvertersToUi;
        BindingConvertersField m_ConvertersToSource;
        PopupField<string> m_UpdateTrigger;
        UIEHelpBox m_ConverterGroupWarningBox;

        VisualElement m_RequiresConstantUpdateField;
        VisualElement m_ConvertersToUiField;
        VisualElement m_ConvertersToSourceField;
        VisualTreeAsset m_VisualTreeAssetCopy;

        /// <summary>
        /// The selection of the UI Builder.
        /// </summary>
        public BuilderSelection selection { get; set; }

        /// <summary>
        /// The parent view.
        /// </summary>
        public BuilderBindingView parentView { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parentView">The parent view</param>
        public BuilderBindingUxmlAttributesView(BuilderInspector inspector, BuilderBindingView parentView): base(inspector)
        {
            this.parentView = parentView;
            this.inspector = inspector;
            this.parentView.closing += Dispose;
        }

        public void SetAttributesOwnerFromCopy(VisualTreeAsset asset, VisualElement visualElement)
        {
            // Work on a copy of the VisualTreeAsset so that we can discard or apply the changes later.
            m_VisualTreeAssetCopy = asset.DeepCopy(false);
            m_VisualTreeAssetCopy.name += "(Binding Copy)";

            // Create a copy of the VisualElement as well.
            VisualElementAsset vea = null;
            foreach (var v in m_VisualTreeAssetCopy.visualElementAssets)
            {
                if (v.id == visualElement.GetVisualElementAsset().id)
                {
                    vea = v;
                    break;
                }
            }

            if (vea == null)
            {
                foreach (var t in m_VisualTreeAssetCopy.templateAssets)
                {
                    if (t.id == visualElement.GetVisualElementAsset().id)
                    {
                        vea = t;
                        break;
                    }
                }
            }

            var desc = UxmlSerializedDataRegistry.GetDescription(vea.fullTypeName);
            desc.SyncDefaultValues(vea.serializedData, false);
            var elementCopy = vea.Instantiate(new CreationContext(m_VisualTreeAssetCopy));
            elementCopy.SetVisualElementAsset(vea);

            SetAttributesOwner(m_VisualTreeAssetCopy, elementCopy);
        }

        protected override void UpdateAttribute(VisualElement target, string bindingAttribute)
        {
            base.UpdateAttribute(target, bindingAttribute);

            switch (bindingAttribute)
            {
                case k_BindingAttr_BindingMode:
                    m_BindingModeField = target.Q<EnumField>();
                    break;
                case k_BindingAttr_ConvertersToUi:
                    m_ConvertersToUi = target.Q<BindingConvertersField>();
                    UpdateAdvancedSettingsOverride();
                    break;
                case k_BindingAttr_ConvertersToSource:
                    m_ConvertersToSource = target.Q<BindingConvertersField>();
                    UpdateAdvancedSettingsOverride();
                    break;
                case k_BindingAttr_UpdateTrigger:
                    m_UpdateTrigger = target.Q<PopupField<string>>();
                    UpdateAdvancedSettingsOverride();
                    break;
            }

            UpdateConverterCompleter();
            UpdateWarningBox();
        }

        internal override void UnsetAllAttributes()
        {
            var undoGroup = Undo.GetCurrentGroup();
            UndoRecordDocument(BuilderConstants.ChangeAttributeValueUndoMessage);
            var builder = Builder.ActiveWindow;

            var styleRows = attributesContainer.Query<BuilderStyleRow>().ToList();
            foreach (var styleRow in styleRows)
            {
                var fields = styleRow.GetLinkedFieldElements();

                // needed for data source and data source type that are sharing a style row
                foreach (var fieldElement in fields)
                {
                    var attributeName = GetAttributeName(fieldElement);

                    var currentAttributesUxmlOwner = attributesUxmlOwner;
                    var currentSerializedData = uxmlSerializedData;

                    if (fieldElement.GetFirstAncestorOfType<UxmlAssetSerializedDataRoot>() is { } dataRoot && dataRoot.dataDescription.isUxmlObject)
                    {
                        var result = SynchronizePath(dataRoot.rootPath, false);
                        currentAttributesUxmlOwner = result.uxmlAsset;
                        currentSerializedData = result.serializedData as UxmlSerializedData;

                        if (currentAttributesUxmlOwner != null)
                        {
                            var entry = uxmlDocument.GetUxmlObjectEntry(currentAttributesUxmlOwner.id);
                            if (entry.uxmlObjectAssets?.Count > 0)
                            {
                                for (var i = entry.uxmlObjectAssets.Count - 1 ; i >= 0; i--)
                                {
                                    uxmlDocument.RemoveUxmlObject(entry.uxmlObjectAssets[i].id);
                                }
                            }
                        }
                    }

                    if (currentSerializedData == null)
                        continue;

                    currentAttributesUxmlOwner.RemoveAttribute(attributeName);
                    var description = fieldElement.GetLinkedAttributeDescription() as UxmlSerializedAttributeDescription;
                    description.SyncDefaultValue(currentSerializedData, true);
                    CallDeserializeOnElement();

                    UnsetEnumValue(attributeName, false);
                }
            }

            // Notify of changes.
            NotifyAttributesChanged();
            Refresh();
            builder.inspector.headerSection.Refresh();
            Undo.CollapseUndoOperations(undoGroup);
        }

        /// <inheritdoc/>
        protected override void GenerateSerializedAttributeFields()
        {
            m_BindingModeField = null;
            m_UpdateTrigger = null;
            m_ConvertersToUi = null;
            m_ConvertersToSource = null;

            var desc = bindingUxmlSerializedDataDescription;
            var root = new UxmlAssetSerializedDataRoot { dataDescription = desc, rootPath = bindingSerializedPropertyRootPath };

            if (m_AdvancedSettings == null)
            {
                m_AdvancedSettings = new PersistedFoldout()
                {
                    text = "Advanced Settings",
                    classList = { PersistedFoldout.unindentedUssClassName },
                    value = false
                };
                m_ConvertersGroupBox = new GroupBox("Local converters");
                m_ConverterGroupWarningBox ??= new UIEHelpBox(BuilderConstants.BindingWindowCompatibleWarningBoxText, HelpBoxMessageType.Info);
                m_ConverterGroupWarningBox.style.display = DisplayStyle.None;
            }
            else
            {
                m_AdvancedSettings.Clear();
                m_ConvertersGroupBox.Clear();
            }

            // Only generate data binding fields for inheritors of DataBinding type.
            var isDataBinding = typeof(DataBinding.UxmlSerializedData).IsAssignableFrom(uxmlSerializedDataDescription.serializedDataType);
            if (isDataBinding)
            {
                base.GenerateSerializedAttributeFields();

                var attribute = desc.FindAttributeWithUxmlName(k_BindingAttr_BindingMode);
                CreateSerializedAttributeRow(attribute, $"{root.rootPath}.{attribute.serializedField.Name}", root);

                root.Add(new VisualElement() { classList = { BuilderConstants.SeparatorLineStyleClassName } });
                root.Add(m_AdvancedSettings);

                attribute = desc.FindAttributeWithUxmlName(k_BindingAttr_UpdateTrigger);
                var row = CreateSerializedAttributeRow(attribute, $"{root.rootPath}.{attribute.serializedField.Name}", m_AdvancedSettings);
                m_RequiresConstantUpdateField = row.GetLinkedFieldElements()[0];

                m_AdvancedSettings.Add(m_ConvertersGroupBox);
                m_AdvancedSettings.Add(m_ConverterGroupWarningBox);

                attribute = desc.FindAttributeWithUxmlName(k_BindingAttr_ConvertersToUi);
                row = CreateSerializedAttributeRow(attribute, $"{root.rootPath}.{attribute.serializedField.Name}", m_ConvertersGroupBox);
                m_ConvertersToUiField = row.GetLinkedFieldElements()[0];

                attribute = desc.FindAttributeWithUxmlName(k_BindingAttr_ConvertersToSource);
                row = CreateSerializedAttributeRow(attribute, $"{root.rootPath}.{attribute.serializedField.Name}", m_ConvertersGroupBox);
                m_ConvertersToSourceField = row.GetLinkedFieldElements()[0];
            }

            attributesContainer.Add(root);

            // Add any additional fields from inherited types.
            var property = m_CurrentElementSerializedObject.FindProperty(bindingSerializedPropertyRootPath);
            CreateUxmlObjectField(property, uxmlSerializedDataDescription, root);

            if (!isDataBinding)
            {
                root.Add(m_AdvancedSettings);

                var attribute = desc.FindAttributeWithUxmlName(k_BindingAttr_UpdateTrigger);
                var row = CreateSerializedAttributeRow(attribute, $"{root.rootPath}.{attribute.serializedField.Name}", m_AdvancedSettings);
                m_RequiresConstantUpdateField = row.GetLinkedFieldElements()[0];
            }

            root.Bind(m_CurrentElementSerializedObject);
        }

        /// <inheritdoc/>
        protected override object GetAttributeValue(UxmlAttributeDescription attribute)
        {
            if (isBinding)
            {
                // For the converters attributes, the only way to get the value is to read the value from the uxml binding asset directly.
                if (attribute.name is k_BindingAttr_ConvertersToUi or k_BindingAttr_ConvertersToSource)
                {
                    return attributesUxmlOwner.GetAttributeValue(attribute.name);
                }
            }

            return base.GetAttributeValue(attribute);
        }

        internal override void UpdateAttributeOverrideStyle(VisualElement fieldElement)
        {
            base.UpdateAttributeOverrideStyle(fieldElement);
            UpdateAdvancedSettingsOverride();
        }

        void UpdateConverterCompleter()
        {
            // Make sure we have all the required fields available.
            if (m_ConvertersToUi == null || m_ConvertersToSource == null || m_DataSourceField == null || m_DataSourceTypeField == null || m_DataSourcePathField == null)
                return;

            m_ConvertersToUi.SetDataSourceContext(currentElement, dataSourcePath, parentView.bindingPropertyName, dataSource, dataSourceType, false);
            m_ConvertersToSource.SetDataSourceContext(currentElement, dataSourcePath, parentView.bindingPropertyName, dataSource, dataSourceType, true);
        }

        void UpdateAdvancedSettingsOverride()
        {
            if (m_RequiresConstantUpdateField?.panel == null || m_ConvertersToUiField?.panel == null || m_ConvertersToSourceField?.panel == null)
                return;

            m_AdvancedSettings.EnableInClassList(BuilderConstants.InspectorFoldoutOverrideClassName,
                IsAttributeOverriden(m_RequiresConstantUpdateField)
                || IsAttributeOverriden(m_ConvertersToUiField)
                || IsAttributeOverriden(m_ConvertersToSourceField));
        }

        void UpdateWarningBox()
        {
            if (m_ConverterGroupWarningBox == null || m_ConvertersToSource == null || m_ConvertersToUi == null)
                return;

            m_ConverterGroupWarningBox.style.display = m_ConvertersToUi.ContainsUnknownCompatibilityGroup() || m_ConvertersToSource.ContainsUnknownCompatibilityGroup() ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void TransferBindingInstance(string path, BuilderUxmlAttributesView targetView, string boundProperty)
        {
            var undoGroup = Undo.GetCurrentGroup();

            var property = targetView.m_CurrentElementSerializedObject.FindProperty(path);

            var undoMessage = $"Modified {property.name}";
            if (property.m_SerializedObject.targetObject.name != string.Empty)
                undoMessage += $" in {property.m_SerializedObject.targetObject.name}";

            Undo.RegisterCompleteObjectUndo(property.m_SerializedObject.targetObject, undoMessage);

            property.InsertArrayElementAtIndex(property.arraySize);
            property = property.GetArrayElementAtIndex(property.arraySize - 1);
            path = property.propertyPath;

            // Extract the SerializedData and UxmlObject
            var result = SynchronizePath(path, true);

            var data = result.serializedData as Binding.UxmlSerializedData;
            data.property = boundProperty;

            // Reapply the serialized data to the original view.
            // Note: We create a copy of the serialized data to avoid both views referencing the same instance.
            // This ensures that changes, such as modifications to the UxmlAssetID during SynchronizePath,
            // do not inadvertently break nested object connections. Without this safeguard, connections may be lost
            // during the CopyAttributesRecursively process (UUM-99975).
            property.managedReferenceValue = UxmlUtility.CloneObject(result.serializedData);
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCompleteObjectUndo(property.m_SerializedObject.targetObject, undoMessage);
            Undo.IncrementCurrentGroup();

            // Get the UxmlAsset for our new serialized data.
            var resultTarget = targetView.SynchronizePath(path, true);
            Undo.IncrementCurrentGroup();

            // Copy attribute values
            targetView.UndoRecordDocument("Add Binding");
            var bindingWindowUxmlObject = result.uxmlAsset;
            var viewUxmlObject = resultTarget.uxmlAsset;

            viewUxmlObject.SetAttribute("property", boundProperty);

            CopyAttributesRecursively(path, bindingWindowUxmlObject, viewUxmlObject, targetView);

            targetView.uxmlDocument.TransferAssetEntries(uxmlDocument);

            Undo.CollapseUndoOperations(undoGroup);

            // Apply changes to the element
            targetView.CallDeserializeOnElement();
            targetView.SendNotifyAttributesChanged();
        }

        void CopyAttributesRecursively(string path, UxmlAsset origin, UxmlAsset destination, BuilderUxmlAttributesView targetView)
        {
            // We reached the end.
            if (origin == null || destination == null)
                return;

            // Copy asset over to the target view's asset.
            var propertiesToCopy = origin.GetProperties();
            if (propertiesToCopy != null)
            {
                for (var i = 0; i < propertiesToCopy.Count; i += 2)
                {
                    destination.SetAttribute(propertiesToCopy[i], propertiesToCopy[i + 1]);
                }
            }

            // Find any uxmlObjectAsset deeper in the serializedData.
            var dataDescription = UxmlSerializedDataRegistry.GetDescription(origin.fullTypeName);
            foreach (var attributeDescription in dataDescription.serializedAttributes)
            {
                if (!attributeDescription.isUxmlObject)
                    continue;

                var attributePath = $"{path}.{attributeDescription.serializedField.Name}";
                if (attributeDescription.isList)
                {
                    var i = 0;
                    var arrayProperty = targetView.m_CurrentElementSerializedObject.FindProperty($"{attributePath}.Array.data[{i}]");
                    while (arrayProperty != null)
                    {
                        // Copy this uxml object's attributes.
                        var arrayPath = arrayProperty.propertyPath;
                        var result = SynchronizePath(arrayPath, false);
                        var resultTarget = targetView.SynchronizePath(arrayPath, true);

                        CopyAttributesRecursively(arrayPath, result.uxmlAsset, resultTarget.uxmlAsset, targetView);

                        arrayProperty = targetView.m_CurrentElementSerializedObject.FindProperty($"{attributePath}.Array.data[{++i}]");
                    }
                }
                else
                {
                    // Copy this uxml object's attributes.
                    var result = SynchronizePath(attributePath, false);
                    var resultTarget = targetView.SynchronizePath(attributePath, true);

                    CopyAttributesRecursively(attributePath, result.uxmlAsset, resultTarget.uxmlAsset, targetView);
                }
            }
        }

        /// <inheritdoc/>
        protected override void NotifyAttributesChanged(string attributeName = null)
        {
            parentView.NotifyAttributesChanged();
            UpdateWarningBox();
            base.NotifyAttributesChanged();
        }

        internal static string GetSerializedDataBindingRoot(string path)
        {
            // Extract the root path, it will look like:
            // "m_VisualElementAssets.Array.data[x].m_SerializedData.bindings.Array.data[x]"
            var searchIndex = path.IndexOf("bindings.Array.data");
            var endIndex = path.IndexOf(']', searchIndex);
            return path.Substring(0, endIndex + 1);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                Undo.ClearUndo(m_VisualTreeAssetCopy);
                UnityEngine.Object.DestroyImmediate(m_VisualTreeAssetCopy);
            }
        }
    }
}
