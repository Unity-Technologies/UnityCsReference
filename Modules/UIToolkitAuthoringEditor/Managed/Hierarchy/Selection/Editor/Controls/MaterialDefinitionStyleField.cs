// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.Rendering;

namespace Unity.UIToolkit.Editor
{
    [UxmlElement]
    [UsedImplicitly]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    partial class MaterialDefinitionStyleField : BaseField<MaterialDefinition>
    {
        internal const string k_MaterialPropertiesListViewName = "material-properties-list-view";
        internal const string k_MaterialWarningName = "material-warning";

        const string k_EmptyListText = "Click the + icon to add a material property.";
        const string k_NoPropertiesText = "This Shader Graph material doesn't expose any properties.";

        const string k_EmptyListClassName = "material-properties-list-empty";
        const string k_FieldClassName = "unity-material-properties-style-field";
        const string k_WarningClassName = "material-warning-style-field:";
        const string k_WarningHelpBoxClassName = "material-warning-label";
        const string k_UxmlPath = "UIToolkitAuthoring/Inspector/Controls/MaterialDefinitionStyleField.uxml";
        const string k_UssPathNoExt = "UIToolkitAuthoring/Inspector/Controls/MaterialDefinitionStyleField";

        const ShaderPropertyFlags k_NonOverridableFlags =
            ShaderPropertyFlags.HideInInspector |
            ShaderPropertyFlags.PerRendererData |
            ShaderPropertyFlags.NonModifiableTextureData;

        static readonly string k_MaterialPropertiesDropdownClassName = "inspector-variables-dropdown";
        static readonly string k_MaterialPropertiesListViewWithFooterClassName = "unity-list-view__scroll-view--with-footer";

        private ObjectField m_MaterialObjectField;
        private ListView m_MaterialPropertiesListView;
        private VisualElement m_MaterialWarningPlaceHolder;
        private List<MaterialPropertyValue> m_MaterialPropertiesSource;

        public MaterialDefinitionStyleField() : this(null) { }

        public MaterialDefinitionStyleField(string label) : base(label)
        {
            AddToClassList(k_FieldClassName);

            var template = EditorGUIUtility.Load(k_UxmlPath) as VisualTreeAsset;
            template.CloneTree(this);

            styleSheets.Add(EditorGUIUtility.Load(k_UssPathNoExt + ".uss") as StyleSheet);

            m_MaterialObjectField = this.Q<ObjectField>();
            m_MaterialObjectField.objectType = typeof(Material);
            m_MaterialObjectField.RegisterValueChangedCallback(OnMaterialSelected);

            m_MaterialPropertiesListView = this.Q<ListView>(k_MaterialPropertiesListViewName);
            m_MaterialPropertiesListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            m_MaterialPropertiesListView.selectionType = SelectionType.Multiple;
            m_MaterialPropertiesListView.makeNoneElement = () => new Label(L10n.Tr(k_EmptyListText)).WithClassList(k_EmptyListClassName);
            m_MaterialPropertiesListView.makeItem = () =>
            {
                return new MaterialPropertyValueListViewItem(this);
            };
            m_MaterialPropertiesListView.bindItem = (e, i) =>
            {
                var item = e as MaterialPropertyValueListViewItem;
                item.itemIndex = i;
                item.SetValue(m_MaterialPropertiesSource[i]);
            };

            m_MaterialPropertiesListView.onRemove += OnRemoveMaterialProperty;

            m_MaterialWarningPlaceHolder = this.Q<VisualElement>(k_MaterialWarningName);
            m_MaterialWarningPlaceHolder.AddToClassList(k_WarningClassName);

            UpdateDropDownMenu(value.material);
            UpdateInvalidMaterialWarning(value.material);
        }

        public override void SetValueWithoutNotify(MaterialDefinition newValue)
        {
            base.SetValueWithoutNotify(newValue);

            bool hasMaterial = newValue.material != null;

            m_MaterialObjectField.SetValueWithoutNotify(newValue.material);

            m_MaterialPropertiesSource = newValue.propertyValues ?? new List<MaterialPropertyValue>();
            m_MaterialPropertiesListView.itemsSource = m_MaterialPropertiesSource;
            m_MaterialPropertiesListView.RefreshItems();

            UpdateDropDownMenu(newValue.material);

            UpdateInvalidMaterialWarning(newValue.material);
        }

        void UpdateInvalidMaterialWarning(Material material)
        {
            m_MaterialWarningPlaceHolder.Clear();

            if (!MaterialDefinition.IsMaterialValid(material))
            {
                var helpBox = new UnityEngine.UIElements.HelpBox(
                    "Selected material '" + material.name + "' is not compatible with UITK",
                    HelpBoxMessageType.Warning);
                helpBox.AddToClassList(k_WarningHelpBoxClassName);
                m_MaterialWarningPlaceHolder.Add(helpBox);
                m_MaterialPropertiesListView.style.display = DisplayStyle.None;
            }
            else if (material != null)
            {
                m_MaterialPropertiesListView.style.display = DisplayStyle.Flex;
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void SetValueWithoutRefresh(MaterialDefinition newValue)
        {
            base.SetValueWithoutNotify(newValue);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static string SanitizePropertyName(string name)
        {
            if (name.StartsWith("_"))
                name = name.TrimStart('_');
            return name;
        }

        void UpdateDropDownMenu(Material mat)
        {
            m_MaterialPropertiesListView.style.display = (mat == null) ? DisplayStyle.None : DisplayStyle.Flex;

            var props = ExtractMaterialProperties(mat);
            if (props.Count == 0)
            {
                m_MaterialPropertiesListView.makeNoneElement = () => new Label(L10n.Tr(k_NoPropertiesText)).WithClassList(k_EmptyListClassName);
                m_MaterialPropertiesListView.showAddRemoveFooter = false;

                // Add this "with footer" class to keep the same scrollview style
                m_MaterialPropertiesListView.scrollView.AddToClassList(k_MaterialPropertiesListViewWithFooterClassName);
                return;
            }

            using var addedHandle = HashSetPool<string>.Get(out var addedNames);
            if (value.propertyValues != null)
            {
                foreach (var pv in value.propertyValues)
                    addedNames.Add(pv.name);
            }

            var menu = new GenericDropdownMenu();
            int availableCount = 0;
            foreach (var prop in props)
            {
                if (addedNames.Contains(prop.name))
                    continue;
                menu.AddItem(SanitizePropertyName(prop.name), false, (_) => OnMaterialPropertyAdded(prop), null);
                availableCount++;
            }

            m_MaterialPropertiesListView.makeNoneElement = () => new Label(L10n.Tr(k_EmptyListText)).WithClassList(k_EmptyListClassName);
            m_MaterialPropertiesListView.showAddRemoveFooter = true;
            m_MaterialPropertiesListView.overridingAddButtonBehavior = (_, btn) =>
            {
                menu.DropDown(btn.worldBound, btn, DropdownMenuSizeMode.Auto);
                menu.contentContainer.AddToClassList(k_MaterialPropertiesDropdownClassName);
            };

            var addButton = m_MaterialPropertiesListView.Q<Button>(BaseListView.footerAddButtonName);
            addButton.text = string.Empty;
            addButton.iconImage = EditorGUIUtility.IconContent("Toolbar Plus More", "Add new variable").image as Texture2D;
            addButton.SetEnabled(availableCount > 0);
        }

        List<MaterialPropertyValue> ExtractMaterialProperties(Material mat)
        {
            var props = new List<MaterialPropertyValue>();
            if (mat == null)
                return props;

            var matProps = ShaderUtil.GetMaterialProperties(new Material[] { mat });
            foreach (var prop in matProps)
            {
                if ((prop.propertyFlags & k_NonOverridableFlags) != 0)
                    continue;

                if (prop.propertyType == ShaderPropertyType.Texture)
                {
                    if (prop.textureDimension != TextureDimension.Tex2D)
                        continue;
                }

                var propValue = new MaterialPropertyValue() { name = prop.name };
                switch (prop.propertyType)
                {
                    case ShaderPropertyType.Float:
                        propValue.type = MaterialPropertyValueType.Float;
                        propValue.SetFloat(prop.floatValue);
                        break;
                    case ShaderPropertyType.Vector:
                        propValue.type = MaterialPropertyValueType.Vector;
                        propValue.SetVector(prop.vectorValue);
                        break;
                    case ShaderPropertyType.Color:
                        propValue.type = MaterialPropertyValueType.Color;
                        propValue.SetColor(prop.colorValue);
                        break;
                    case ShaderPropertyType.Texture:
                        propValue.type = MaterialPropertyValueType.Texture;
                        propValue.textureValue = prop.textureValue;
                        break;
                }
                props.Add(propValue);
            }
            return props;
        }

        void OnMaterialSelected(ChangeEvent<UnityEngine.Object> e)
        {
            using (var evt = MaterialSelectedEvent.GetPooled())
            {
                evt.elementTarget = this;
                evt.material = e.newValue as Material;
                SendEvent(evt);

                value = evt.material;
            }
        }

        internal void OnMaterialPropertyAdded(MaterialPropertyValue materialPropertyValue)
        {
            using (var evt = MaterialDefinitionChangedEvent.GetPooled())
            {
                evt.elementTarget = this;

                // Build the new MaterialDefinition with the added property
                var propertyValues = new List<MaterialPropertyValue>(value.propertyValues ?? new List<MaterialPropertyValue>());
                propertyValues.Add(materialPropertyValue);
                evt.newMaterialDefinition = new MaterialDefinition(value.material, propertyValues);
                evt.refreshField = true;

                SendEvent(evt);

                value = evt.newMaterialDefinition;
            }
        }

        internal void OnRemoveMaterialProperty(BaseListView listView)
        {
            var indicesToRemove = new List<int>();

            // If no items are selected, remove last item
            if (listView.selectedIndex == -1 && m_MaterialPropertiesSource.Count > 0)
            {
                indicesToRemove.Add(m_MaterialPropertiesSource.Count - 1);
            }
            else
            {
                foreach (var selectedIndex in listView.selectedIndicesList)
                {
                    if (selectedIndex >= m_MaterialPropertiesSource.Count)
                        indicesToRemove.Add(m_MaterialPropertiesSource.Count - 1);
                    else
                        indicesToRemove.Add(selectedIndex);
                }
            }

            using (var evt = MaterialDefinitionChangedEvent.GetPooled())
            {
                evt.elementTarget = this;

                // Build the new MaterialDefinition with the removed properties
                var propertyValues = new List<MaterialPropertyValue>(this.value.propertyValues ?? new List<MaterialPropertyValue>());
                var sortedIndices = new List<int>(indicesToRemove);
                sortedIndices.Sort((a, b) => b.CompareTo(a)); // Sort in descending order
                foreach (var index in sortedIndices)
                {
                    if (index >= 0 && index < propertyValues.Count)
                        propertyValues.RemoveAt(index);
                }
                evt.newMaterialDefinition = new MaterialDefinition(this.value.material, propertyValues);
                evt.refreshField = true;

                value = evt.newMaterialDefinition;
                SendEvent(evt);
            }
        }
    }
}
