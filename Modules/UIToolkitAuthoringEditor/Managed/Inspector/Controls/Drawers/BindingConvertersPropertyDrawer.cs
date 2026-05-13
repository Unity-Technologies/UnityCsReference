// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [CustomPropertyDrawer(typeof(ConverterDrawerAttribute))]
    class BindingConvertersPropertyDrawer : PropertyDrawer
    {
        protected static readonly string k_BindingMode = nameof(DataBinding.bindingMode);
        protected static readonly string k_DataSource = nameof(DataBinding.dataSource);
        protected static readonly string k_DataSourceType = nameof(DataBinding.dataSourceType);
        protected static readonly string k_DataSourcePathString = nameof(DataBinding.dataSourcePathString);
        protected static readonly string k_Property = nameof(DataBinding.property);

        public static readonly string BindingWindowLocalConverterNotApplicableMessage = L10n.Tr("It is not applicable for the specified binding mode");

        private BindingConvertersField m_ConvertersField;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            m_ConvertersField = new BindingConvertersField()
            {
                bindingPath = property.propertyPath,
                label = property.propertyPath.Contains("uiToSourceConverters") ? "To data source" : "To target property (UI)",
            };

            m_ConvertersField.AddToClassList(BindingConvertersField.alignedFieldUssClassName);
            return m_ConvertersField;
        }
    }
}
