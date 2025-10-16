// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    sealed internal class FilterFunctionListViewItem : VisualElement
    {
        const string k_UxmlPath = BuilderConstants.UtilitiesPath + "/FoldoutField/FoldoutFilterField.uxml";
        const string k_UssPath = BuilderConstants.UtilitiesPath + "/FoldoutField/FoldoutFilterField.uss";
        const string k_UssDarkSkinPath = BuilderConstants.UtilitiesPath + "/FoldoutField/FoldoutFilterFieldDark.uss";
        const string k_UssLightSkinPath = BuilderConstants.UtilitiesPath + "/FoldoutField/FoldoutFilterFieldLight.uss";

        const string k_BaseClass = "unity-foldout-filter-field";
        const string k_RemoveButtonClass = k_BaseClass + "__remove-filter-button";
        const string k_FilterFunctionTypeName = "filter-function-type";
        const string k_ParamtersContainerName = "parameters-container";

        private VisualElement m_ParametersContainer;
        public VisualElement parametersContainer => m_ParametersContainer;

        private EnumField m_FilterFunctionTypeField;

        FilterFunction m_FilterFunction;
        public FilterFunction filterFunction => m_FilterFunction;

        public int index;

        public FilterFunctionListViewItem()
        {
            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(k_UxmlPath);
            template.CloneTree(contentContainer);

            var styleSheet = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssPath);
            styleSheets.Add(styleSheet);

            if (EditorGUIUtility.isProSkin)
                styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssDarkSkinPath));
            else
                styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssLightSkinPath));

            m_FilterFunctionTypeField = this.Q<EnumField>(k_FilterFunctionTypeName);
            m_FilterFunctionTypeField.RegisterValueChangedCallback(OnFilterFunctionTypeChanged);

            m_ParametersContainer = this.Q<VisualElement>(k_ParamtersContainerName);

            AddToClassList(k_BaseClass);
        }

        void OnFilterFunctionTypeChanged(ChangeEvent<Enum> evt)
        {
            var filterType = (FilterFunctionType)evt.newValue;
            var f = new FilterFunction(filterType);

            var def = f.GetDefinition();
            if (def != null)
            {
                for (int i = 0; i < def.parameters.Length; ++i)
                    f.AddParameter(def.parameters[i].defaultValue);
            }

            m_FilterFunction = f;
            GetFirstAncestorOfType<FilterStyleField>().FilterFunctionTypeChanged(this);
            evt.StopPropagation();
        }

        public void SetFilterFunction(FilterFunction func)
        {
            m_FilterFunction = func;
            m_FilterFunctionTypeField.SetValueWithoutNotify(func.type);

            foreach (var field in m_ParametersContainer.Children())
            {
                if (field is FloatField floatField)
                    floatField.UnregisterValueChangedCallback(OnParameterValueChanged);
                else if (field is ColorField colorField)
                    colorField.UnregisterValueChangedCallback(OnParameterValueChanged);
                else if (field is ObjectField objectField)
                    objectField.UnregisterValueChangedCallback(OnCustomValueChanged);
            }
            m_ParametersContainer.Clear();

            var def = func.GetDefinition();

            if (func.type == FilterFunctionType.Custom)
            {
                var field = new ObjectField("Definition");
                field.objectType = typeof(FilterFunctionDefinition);
                field.value = def;
                field.RegisterValueChangedCallback(OnCustomValueChanged);
                m_ParametersContainer.Add(field);
            }

            int paramCount = def?.parameters.Length ?? 0;
            for (int i = 0; i < paramCount; ++i)
            {
                var pDef = def?.parameters[i] ?? new FilterParameterDeclaration();
                var pVal = func.GetParameter(i);
                if (pVal.type == FilterParameterType.Float)
                {
                    var field = new FloatField();

                    var label = pDef.name;
                    if (string.IsNullOrEmpty(label))
                        label = paramCount == 1 ? "Value" : $"Value {i + 1}";

                    field.label = label;
                    field.value = pVal.floatValue;
                    field.userData = i;
                    field.RegisterValueChangedCallback(OnParameterValueChanged);
                    m_ParametersContainer.Add(field);
                }
                else if (pVal.type == FilterParameterType.Color)
                {
                    var field = new ColorField();
                    field.userData = i;
                    field.label = paramCount == 1 ? "Value" : $"Value {i + 1}";
                    field.value = pVal.colorValue;
                    field.RegisterValueChangedCallback(OnParameterValueChanged);
                    m_ParametersContainer.Add(field);
                }
            }
        }

        void OnCustomValueChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            var f = m_FilterFunction;
            var newDef = evt.newValue as FilterFunctionDefinition;
            f.customDefinition = newDef;

            f.ClearParameters();
            for (int i = 0; i < newDef?.parameters.Length; ++i)
            {
                // Default values aren't specifed for custom definitions, so we use the interpolation default value instead.
                f.AddParameter(newDef.parameters[i].interpolationDefaultValue);
            }

            m_FilterFunction = f;
            GetFirstAncestorOfType<FilterStyleField>().FilterFunctionTypeChanged(this);
        }

        void OnParameterValueChanged<T>(ChangeEvent<T> evt)
        {
            var field = (VisualElement) evt.target;
            var paramIndex = (int)field.userData;

            var f = m_FilterFunction;
            if (evt.newValue is float floatValue)
            {
                f.SetParameter(paramIndex, new FilterParameter(floatValue));
            }
            else if (evt.newValue is Color colorValue)
            {
                f.SetParameter(paramIndex, new FilterParameter(colorValue));
            }
            m_FilterFunction = f;
            GetFirstAncestorOfType<FilterStyleField>().FilterFunctionValueChanged(this, paramIndex);
        }
    }
}
