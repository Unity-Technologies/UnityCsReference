// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements.StyleSheets;
using UnityEditor.UIElements;

namespace Unity.UI.Builder
{
    [UxmlElement]
    [UsedImplicitly]
    class MaterialDefinitionStyleField : BaseField<MaterialDefinition>
    {
        [Serializable]
        public new class UxmlSerializedData : BindableElement.UxmlSerializedData
        {
            public override object CreateInstance() => new MaterialDefinitionStyleField();
        }

        internal const string k_MaterialPropertiesListViewName = "material-properties-list-view";
        internal const string k_MaterialWarningName = "material-warning";

        const string k_EmptyListText = "Click the + icon to add a material property.";
        const string k_NoPropertiesText = "This Shader Graph material doesn't expose any properties.";

        const string k_EmptyListClassName = "material-properties-list-empty";
        const string k_FieldClassName = "unity-material-properties-style-field";
        const string k_WarningClassName = "material-warning-style-field:";
        const string k_WarningHelpBoxClassName = "material-warning-label";
        const string k_UxmlPath = BuilderConstants.UtilitiesPath + "/StyleField/MaterialDefinitionStyleField.uxml";
        const string k_UssPathNoExt = BuilderConstants.UtilitiesPath + "/StyleField/MaterialDefinitionStyleField";

        static readonly string k_MaterialPropertiesDropdownClassName = "inspector-variables-dropdown";
        // static readonly string k_AddMoreIconClassName = BaseListView.footerAddButtonName + "--with-menu";
        static readonly string k_MaterialPropertiesListViewWithFooterClassName = "unity-list-view__scroll-view--with-footer";

        private ObjectField m_MaterialObjectField;
        private ListView m_MaterialPropertiesListView;
        private VisualElement m_MaterialWarningPlaceHolder;
        private List<MaterialPropertyValue> m_MaterialPropertiesSource;

        public MaterialDefinitionStyleField() : this(null) { }

        public MaterialDefinitionStyleField(string label) : base(label)
        {
            AddToClassList(BuilderConstants.InspectorContainerClassName);
            AddToClassList(k_FieldClassName);
            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssPathNoExt + ".uss"));

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(k_UxmlPath);
            template.CloneTree(this);

            m_MaterialObjectField = this.Q<ObjectField>();
            m_MaterialObjectField.objectType = typeof(Material);
            m_MaterialObjectField.RegisterValueChangedCallback(OnMaterialSelected);

            m_MaterialPropertiesListView = this.Q<ListView>(k_MaterialPropertiesListViewName);
            m_MaterialPropertiesListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            m_MaterialPropertiesListView.selectionType = SelectionType.Multiple;
            m_MaterialPropertiesListView.makeNoneElement = () => new Label(L10n.Tr(k_EmptyListText)) { classList = { k_EmptyListClassName } };
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
                helpBox.classList.Add(k_WarningHelpBoxClassName);
                m_MaterialWarningPlaceHolder.Add(helpBox);
                m_MaterialPropertiesListView.style.display = DisplayStyle.None;
            }
            else
            {
                m_MaterialPropertiesListView.style.display = DisplayStyle.Flex;
            }
        }

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
            if (props.Count > 0)
            {
                var menu = new GenericDropdownMenu();
                foreach (var prop in props)
                    menu.AddItem(SanitizePropertyName(prop.name), false, (_) => OnMaterialPropertyAdded(prop), null);

                m_MaterialPropertiesListView.makeNoneElement = () => new Label(L10n.Tr(k_EmptyListText)) { classList = { k_EmptyListClassName } };
                m_MaterialPropertiesListView.showAddRemoveFooter = true;
                m_MaterialPropertiesListView.overridingAddButtonBehavior = (_, btn) =>
                {
                    menu.DropDown(btn.worldBound, btn, DropdownMenuSizeMode.Auto);
                    menu.contentContainer.AddToClassList(k_MaterialPropertiesDropdownClassName);
                };

                var addButton = m_MaterialPropertiesListView.Q<Button>(BaseListView.footerAddButtonName);
                addButton.text = string.Empty;
                addButton.iconImage = EditorGUIUtility.IconContent("Toolbar Plus More", "Add new variable").image as Texture2D;
            }
            else
            {
                m_MaterialPropertiesListView.makeNoneElement = () => new Label(L10n.Tr(k_NoPropertiesText)) { classList = { k_EmptyListClassName } };
                m_MaterialPropertiesListView.showAddRemoveFooter = false;

                // Add this "with footer" class to keep the same scrollview style
                m_MaterialPropertiesListView.scrollView.AddToClassList(k_MaterialPropertiesListViewWithFooterClassName);
            }
        }

        List<MaterialPropertyValue> ExtractMaterialProperties(Material mat)
        {
            var props = new List<MaterialPropertyValue>();
            if (mat == null)
                return props;

            var matProps = ShaderUtil.GetMaterialProperties(new Material[] { mat });
            foreach (var prop in matProps)
            {
                if (prop.propertyFlags.HasFlag(ShaderPropertyFlags.HideInInspector))
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
            }
        }

        internal void OnMaterialPropertyAdded(MaterialPropertyValue value)
        {
            using (var evt = MaterialPropertyAddedEvent.GetPooled())
            {
                evt.elementTarget = this;
                evt.materialPropertyValue = value;
                SendEvent(evt);
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
                foreach (var selectedIndex in listView.selectedIndices)
                {
                    if (selectedIndex >= m_MaterialPropertiesSource.Count)
                        indicesToRemove.Add(m_MaterialPropertiesSource.Count - 1);
                    else
                        indicesToRemove.Add(selectedIndex);
                }
            }

            using (var evt = MaterialPropertyRemovedEvent.GetPooled())
            {
                evt.elementTarget = this;
                evt.indices = indicesToRemove;
                SendEvent(evt);
            }
        }
    }
}
