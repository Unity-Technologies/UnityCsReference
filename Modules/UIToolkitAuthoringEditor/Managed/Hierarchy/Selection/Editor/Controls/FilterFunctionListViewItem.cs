// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    sealed internal class FilterFunctionListViewItem : VisualElement
    {
        const string k_UxmlPath =  "UIToolkitAuthoring/Inspector/Controls/Filters/FoldoutFilterField.uxml";
        const string k_UssPathNoExt = "UIToolkitAuthoring/Inspector/Controls/Filters/FoldoutFilterField";

        const string k_BaseClass = "unity-foldout-filter-field";
        const string k_RemoveButtonClass = k_BaseClass + "__remove-filter-button";
        const string k_FilterFunctionTypeName = "filter-function-type";
        const string k_ParamtersContainerName = "parameters-container";
        const int k_MaxParameters = 4;

        private VisualElement m_ParametersContainer;
        public VisualElement parametersContainer => m_ParametersContainer;

        private EnumField m_FilterFunctionTypeField;
        private DropdownField m_FilterFunctionTypeDropdown;
        private ObjectField m_CustomDefinitionField;
        private FloatField[] m_FloatFields;
        private ColorField[] m_ColorFields;

        FilterFunction m_FilterFunction;
        public FilterFunction filterFunction => m_FilterFunction;

        public int index;

        public FilterFunctionListViewItem()
        {
            var template = EditorGUIUtility.Load(k_UxmlPath) as VisualTreeAsset;
            template.CloneTree(contentContainer);

            styleSheets.Add(EditorGUIUtility.Load(k_UssPathNoExt + ".uss") as StyleSheet);
            styleSheets.Add(EditorGUIUtility.Load(k_UssPathNoExt + (EditorGUIUtility.isProSkin ? "Dark" : "Light") + ".uss") as StyleSheet);

            m_FilterFunctionTypeField = this.Q<EnumField>(k_FilterFunctionTypeName);
            m_FilterFunctionTypeField.includeObsoleteValues = false;
            m_FilterFunctionTypeField.RegisterValueChangedCallback(OnFilterFunctionTypeChanged);

            // Will be replaced with DropdownField if custom filters are not allowed
            m_FilterFunctionTypeDropdown = null;

            m_ParametersContainer = this.Q<VisualElement>(k_ParamtersContainerName);

            // Create custom definition field
            m_CustomDefinitionField = new ObjectField("Definition");
            m_CustomDefinitionField.allowBuiltinResources = false;
            m_CustomDefinitionField.objectType = typeof(FilterFunctionDefinition);
            m_CustomDefinitionField.RegisterValueChangedCallback(OnCustomValueChanged);
            m_CustomDefinitionField.style.display = DisplayStyle.None;
            m_ParametersContainer.Add(m_CustomDefinitionField);

            // Create pool of float fields
            m_FloatFields = new FloatField[k_MaxParameters];
            for (int i = 0; i < k_MaxParameters; ++i)
            {
                var field = new FloatField();
                field.RegisterValueChangedCallback(OnParameterValueChanged);
                field.style.display = DisplayStyle.None;
                m_FloatFields[i] = field;
                m_ParametersContainer.Add(field);
            }

            // Create pool of color fields
            m_ColorFields = new ColorField[k_MaxParameters];
            for (int i = 0; i < k_MaxParameters; ++i)
            {
                var field = new ColorField();
                field.setAlphaIfTransparentWhenPicked = true;
                field.RegisterValueChangedCallback(OnParameterValueChanged);
                field.style.display = DisplayStyle.None;
                m_ColorFields[i] = field;
                m_ParametersContainer.Add(field);
            }

            AddToClassList(k_BaseClass);
        }

        void ReplaceEnumFieldWithDropdown()
        {
            // Get enum choices excluding Custom
            var enumData = UnityEngine.EnumDataUtility.GetCachedEnumData(
                typeof(FilterFunctionType),
                UnityEngine.EnumDataUtility.CachedType.ExcludeObsolete,
                NameFormatter.FormatVariableName);

            var choices = new System.Collections.Generic.List<string>();
            var choiceToEnum = new System.Collections.Generic.Dictionary<string, FilterFunctionType>();

            for (int i = 0; i < enumData.displayNames.Length; i++)
            {
                var enumValue = (FilterFunctionType)enumData.values[i];
                if (enumValue != FilterFunctionType.Custom)
                {
                    var displayName = enumData.displayNames[i];
                    choices.Add(displayName);
                    choiceToEnum[displayName] = enumValue;
                }
            }

            // Create dropdown field with formatSelectedValueCallback to show display names
            m_FilterFunctionTypeDropdown = new DropdownField(m_FilterFunctionTypeField.label, choices, 0);
            m_FilterFunctionTypeDropdown.name = m_FilterFunctionTypeField.name;
            m_FilterFunctionTypeDropdown.userData = choiceToEnum;

            // Copy classes
            foreach (var className in m_FilterFunctionTypeField.GetClasses())
            {
                m_FilterFunctionTypeDropdown.AddToClassList(className);
            }

            // Register callback
            m_FilterFunctionTypeDropdown.RegisterValueChangedCallback(OnFilterFunctionTypeDropdownChanged);

            // Replace in hierarchy
            var parent = m_FilterFunctionTypeField.parent;
            var index = parent.IndexOf(m_FilterFunctionTypeField);
            parent.Remove(m_FilterFunctionTypeField);
            parent.Insert(index, m_FilterFunctionTypeDropdown);
        }

        void OnFilterFunctionTypeDropdownChanged(ChangeEvent<string> evt)
        {
            // Get the enum value from the stored dictionary
            var choiceToEnum = m_FilterFunctionTypeDropdown.userData as System.Collections.Generic.Dictionary<string, FilterFunctionType>;
            if (choiceToEnum != null && choiceToEnum.TryGetValue(evt.newValue, out var filterType))
            {
                var f = new FilterFunction(filterType);

                var def = f.GetDefinition();
                if (def != null)
                {
                    for (int i = 0; i < def.parameters.Length; ++i)
                        f.AddParameter(def.parameters[i].defaultValue);
                }

                m_FilterFunction = f;
                GetFirstAncestorOfType<FilterStyleField>()?.FilterFunctionTypeChanged(this);
            }
            evt.StopPropagation();
        }

        void OnFilterFunctionTypeChanged(ChangeEvent<Enum> evt)
        {
            var filterType = (FilterFunctionType)evt.newValue;

            // Check if Custom filter is allowed
            var parentField = GetFirstAncestorOfType<FilterStyleField>();
            if (filterType == FilterFunctionType.Custom && parentField != null && !parentField.allowCustomFilters)
            {
                // Revert to previous value
                m_FilterFunctionTypeField.SetValueWithoutNotify(evt.previousValue);
                Debug.LogWarning("Custom filters are not supported for backdrop-filter.");
                evt.StopPropagation();
                return;
            }

            var f = new FilterFunction(filterType);

            var def = f.GetDefinition();
            if (def != null)
            {
                for (int i = 0; i < def.parameters.Length; ++i)
                    f.AddParameter(def.parameters[i].defaultValue);
            }

            m_FilterFunction = f;
            parentField?.FilterFunctionTypeChanged(this);
            evt.StopPropagation();
        }

        public void SetFilterFunction(FilterFunction func)
        {
            m_FilterFunction = func;

            // Check if we need to replace EnumField with DropdownField
            var parentField = GetFirstAncestorOfType<FilterStyleField>();
            if (parentField != null && !parentField.allowCustomFilters && m_FilterFunctionTypeDropdown == null)
            {
                ReplaceEnumFieldWithDropdown();
            }

            if (m_FilterFunctionTypeDropdown != null)
            {
                // Use dropdown (custom filters excluded)
                m_FilterFunctionTypeDropdown.SetValueWithoutNotify(func.type.ToString());
            }
            else
            {
                // Use enum field (all filters allowed)
                m_FilterFunctionTypeField.SetValueWithoutNotify(func.type);
            }

            // Hide all fields initially
            m_CustomDefinitionField.style.display = DisplayStyle.None;
            for (int i = 0; i < k_MaxParameters; ++i)
            {
                m_FloatFields[i].style.display = DisplayStyle.None;
                m_ColorFields[i].style.display = DisplayStyle.None;
            }

            var def = func.GetDefinition();

            // Show custom definition field if needed
            if (func.type == FilterFunctionType.Custom)
            {
                m_CustomDefinitionField.SetValueWithoutNotify(def);
                m_CustomDefinitionField.style.display = DisplayStyle.Flex;
            }

            // Show and configure parameter fields
            int paramCount = def?.parameters.Length ?? 0;
            var floatFieldIndex = 0;
            var colorFieldIndex = 0;

            for (int i = 0; i < paramCount; ++i)
            {
                var pDef = def?.parameters[i] ?? new FilterParameterDeclaration();
                var pVal = func.GetParameter(i);

                var label = pDef.name;
                if (string.IsNullOrEmpty(label))
                    label = paramCount == 1 ? "Value" : $"Value {i + 1}";

                if (pVal.type == FilterParameterType.Float)
                {
                    var field = m_FloatFields[floatFieldIndex++];
                    field.label = label;
                    field.SetValueWithoutNotify(pVal.floatValue);
                    field.userData = i;
                    field.style.display = DisplayStyle.Flex;
                }
                else if (pVal.type == FilterParameterType.Color)
                {
                    var field = m_ColorFields[colorFieldIndex++];
                    field.label = label;
                    field.SetValueWithoutNotify(pVal.colorValue);
                    field.userData = i;
                    field.style.display = DisplayStyle.Flex;
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
                // Default values aren't specified for custom definitions, so we use the interpolation default value instead.
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
