// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [CustomPropertyDrawer(typeof(Binding.UxmlSerializedData))]
    class BindingPropertyDrawer : UxmlSerializedDataPropertyDrawer
    {
        const string k_BindingMainContentUxmlPath = "UIToolkitAuthoring/Inspector/Binding/BindingPropertyDrawer.uxml";
        const string k_DataBindingMainContentUxmlPath = "UIToolkitAuthoring/Inspector/Binding/DataBindingPropertyDrawer.uxml";

        protected override void CreatePropertyGUI(VisualElement container, SerializedProperty property)
        {
            CreateChildPropertiesGUI(container, property);
        }

        protected override void CreateChildPropertiesGUI(VisualElement container, SerializedProperty property)
        {
            var uxmlSerializedData = property.managedReferenceValue as Binding.UxmlSerializedData;
            var mainContentUxmlPath = k_BindingMainContentUxmlPath;

            // Only generate data binding fields for inheritors of DataBinding type.
            var isDataBinding = typeof(DataBinding.UxmlSerializedData).IsAssignableFrom(uxmlSerializedData.GetType());
            if (isDataBinding)
            {
                mainContentUxmlPath = k_DataBindingMainContentUxmlPath;
            }

            var visualTreeAsset = EditorGUIUtility.LoadRequired(mainContentUxmlPath) as VisualTreeAsset;

            var mainContent = visualTreeAsset.CloneTree();
            container.Add(mainContent);

            var additionalSettingsContainer = container.Q("AdditionalSettingsContainer");

            base.CreateChildPropertiesGUI(additionalSettingsContainer, property);
        }
    }

    /// <summary>
    /// Property drawer for DataSourceDrawerAttribute
    /// </summary>
    [CustomPropertyDrawer(typeof(DataSourceDrawerAttribute))]
    class DataSourcePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new AnyObjectField()
            {
                bindingPath = property.propertyPath,
                objectType = typeof(ScriptableObject),
                label = " ",
            };
            field.AddToClassList(AnyObjectField.alignedFieldUssClassName);
            return field;
        }
    }

    /// <summary>
    /// Property drawer for BindingPathDrawerAttribute
    /// </summary>
    [CustomPropertyDrawer(typeof(BindingPathDrawerAttribute))]
    class BindingPathPropertyDrawer : PropertyDrawer
    {
        const string k_EditorBindingPathLabel = "Editor Binding Path";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new TextField(k_EditorBindingPathLabel)
            {
                bindingPath = property.propertyPath, isDelayed = true
            };
            field.AddToClassList(AnyObjectField.alignedFieldUssClassName);
            return field;
        }
    }
}
