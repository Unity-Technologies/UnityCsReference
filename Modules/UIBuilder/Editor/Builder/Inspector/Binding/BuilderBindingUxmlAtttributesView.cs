// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UIEHelpBox = UnityEngine.UIElements.HelpBox;

namespace Unity.UI.Builder
{
    /// <summary>
    /// Provides a view to uxml attributes of a binding.
    /// </summary>
    internal class BuilderBindingUxmlAttributesView : BuilderDataSourceAndPathView, IDisposable
    {
        internal const string k_BindingAttr_BindingMode = "binding-mode";
        internal const string k_BindingAttr_ConvertersToUi = "source-to-ui-converters";
        internal const string k_BindingAttr_ConvertersToSource = "ui-to-source-converters";
        internal const string k_BindingAttr_UpdateTrigger = "update-trigger";

        internal new struct TestAccess
        {
            public BuilderObjectField dataSourceField;
            public TextField dataSourceTypeField;
            public TextField dataSourcePathField;
            public BuilderDataSourcePathCompleter dataSourcePathCompleter;
            public UIEHelpBox dataSourceWarningBox;
            public UIEHelpBox pathWarningBox;
            public EnumField bindingModeField;
            public PersistedFoldout advancedSettings;
            public BindingConvertersField convertersToUi;
            public BindingConvertersField convertersToSource;
            public EnumField updateTrigger;
        }

        // Note: They are internal only to be accessible in tests
        internal new TestAccess testAccess
        {
            get
            {
                var baseTestAccess = base.testAccess;
                return new TestAccess
                {
                    dataSourceField = baseTestAccess.dataSourceField,
                    dataSourceTypeField = baseTestAccess.dataSourceTypeField,
                    dataSourcePathField = baseTestAccess.dataSourcePathField,
                    dataSourcePathCompleter = baseTestAccess.dataSourcePathCompleter,
                    dataSourceWarningBox = baseTestAccess.dataSourceWarningBox,
                    pathWarningBox = baseTestAccess.pathWarningBox,
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
        EnumField m_UpdateTrigger;

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
        public BuilderBindingUxmlAttributesView(BuilderBindingView parentView)
        {
            this.parentView = parentView;
        }

        public void SetAttributesOwnerFromCopy(VisualTreeAsset asset, VisualElement visualElement)
        {
            // Work on a copy of the VisualTreeAsset so that we can discard or apply the changes later.
            m_VisualTreeAssetCopy = asset.DeepCopy();
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
                    m_UpdateTrigger = target.Q<EnumField>();
                    UpdateAdvancedSettingsOverride();
                    break;
            }

            UpdateConverterCompleter();
        }

        internal override void UnsetAllAttributes()
        {
            var undoGroup = Undo.GetCurrentGroup();
            UndoRecordDocument(BuilderConstants.ChangeAttributeValueUndoMessage);
            var builder = Builder.ActiveWindow;

            var styleRows = fieldsContainer.Query<BuilderStyleRow>().ToList();
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
                        SynchronizePath(dataRoot.rootPath, false, out var uxmlOwner, out var serializedData, out var _);
                        currentAttributesUxmlOwner = uxmlOwner as UxmlAsset;
                        currentSerializedData = serializedData as UxmlSerializedData;

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
                    description.SetSerializedValue(currentSerializedData, description.defaultValue);
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
            var root = new UxmlAssetSerializedDataRoot { dataDescription = desc, rootPath = bindingSerializedPropertyPathRoot + "." };

            // Only generate data binding fields for inheritors of DataBinding type.
            if (typeof(DataBinding.UxmlSerializedData).IsAssignableFrom(uxmlSerializedDataDescription.serializedDataType))
            {
                base.GenerateSerializedAttributeFields();

                var attribute = desc.FindAttributeWithUxmlName(k_BindingAttr_BindingMode);
                CreateSerializedAttributeRow(attribute, $"{root.rootPath}{attribute.serializedField.Name}", root);

                root.Add(new VisualElement() { classList = { BuilderConstants.SeparatorLineStyleClassName } });

                if (m_AdvancedSettings == null)
                {
                    m_AdvancedSettings = new PersistedFoldout()
                    {
                        text = "Advanced Settings",
                        classList = { PersistedFoldout.unindentedUssClassName },
                        value = false
                    };
                    m_ConvertersGroupBox = new GroupBox("Converters");
                }
                else
                {
                    m_AdvancedSettings.Clear();
                    m_ConvertersGroupBox.Clear();
                }

                root.Add(m_AdvancedSettings);

                attribute = desc.FindAttributeWithUxmlName(k_BindingAttr_UpdateTrigger);
                var row = CreateSerializedAttributeRow(attribute, $"{root.rootPath}{attribute.serializedField.Name}", m_AdvancedSettings);
                m_RequiresConstantUpdateField = row.GetLinkedFieldElements()[0];

                m_AdvancedSettings.Add(m_ConvertersGroupBox);

                attribute = desc.FindAttributeWithUxmlName(k_BindingAttr_ConvertersToUi);
                row = CreateSerializedAttributeRow(attribute, $"{root.rootPath}{attribute.serializedField.Name}", m_ConvertersGroupBox);
                m_ConvertersToUiField = row.GetLinkedFieldElements()[0];

                attribute = desc.FindAttributeWithUxmlName(k_BindingAttr_ConvertersToSource);
                row = CreateSerializedAttributeRow(attribute, $"{root.rootPath}{attribute.serializedField.Name}", m_ConvertersGroupBox);
                m_ConvertersToSourceField = row.GetLinkedFieldElements()[0];
            }

            fieldsContainer.Add(root);

            // Add any additional fields from inherited types.
            GenerateSerializedAttributeFields(uxmlSerializedDataDescription, root);
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

        public void TransferBindingInstance(string path, BuilderUxmlAttributesView targetView, string boundProperty)
        {
            var undoGroup = Undo.GetCurrentGroup();

            var property = targetView.m_CurrentElementSerializedObject.FindProperty(path);
            property.InsertArrayElementAtIndex(property.arraySize);
            property = property.GetArrayElementAtIndex(property.arraySize - 1);
            path = property.propertyPath;

            // Extract the SerializedData and UxmlObject
            SynchronizePath(path, true, out var uxmlAsset, out var serializedData, out var _);

            var data = serializedData as Binding.UxmlSerializedData;
            data.property = boundProperty;

            // Apply the serialized data back to the original view
            property.managedReferenceValue = serializedData;
            property.serializedObject.ApplyModifiedProperties();
            Undo.IncrementCurrentGroup();

            // Get the UxmlAsset for our new serialized data.
            targetView.SynchronizePath(path, true, out var newUxmlAsset, out var _, out var _);
            Undo.IncrementCurrentGroup();

            // Copy attribute values
            targetView.UndoRecordDocument("Add Binding");
            var bindingWindowUxmlObject = (UxmlAsset)uxmlAsset;
            var viewUxmlObject = (UxmlAsset)newUxmlAsset;

            viewUxmlObject.SetAttribute("property", boundProperty);

            CopyAttributesRecursively(path, bindingWindowUxmlObject, viewUxmlObject, targetView);

            targetView.uxmlDocument.TransferAssetEntries(uxmlDocument);

            Undo.CollapseUndoOperations(undoGroup);

            // Apply changes to the element
            targetView.uxmlSerializedData.Deserialize(targetView.currentElement);
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
                        SynchronizePath(arrayPath, false, out var uxmlAsset, out _, out _);
                        targetView.SynchronizePath(arrayPath, true, out var newUxmlAsset, out _, out _);

                        CopyAttributesRecursively(arrayPath, (UxmlAsset)uxmlAsset, (UxmlAsset)newUxmlAsset, targetView);

                        arrayProperty = targetView.m_CurrentElementSerializedObject.FindProperty($"{attributePath}.Array.data[{++i}]");
                    }
                }
                else
                {
                    // Copy this uxml object's attributes.
                    SynchronizePath(attributePath, false, out var uxmlAsset, out _, out _);
                    targetView.SynchronizePath(attributePath, true, out var newUxmlAsset, out _, out _);

                    CopyAttributesRecursively(attributePath, (UxmlAsset)uxmlAsset, (UxmlAsset)newUxmlAsset, targetView);
                }
            }
        }

        /// <inheritdoc/>
        protected override void NotifyAttributesChanged()
        {
            parentView.NotifyAttributesChanged();
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

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(m_VisualTreeAssetCopy);
        }
    }
}
