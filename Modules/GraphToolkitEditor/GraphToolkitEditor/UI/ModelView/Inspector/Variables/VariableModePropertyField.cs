// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.Implementation;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    internal class VariableModePropertyField : BaseModelPropertyField
    {
        public new static readonly string ussClassName = "variable-mode-property-field";
        public new static readonly string labelUssClassName = ussClassName.WithUssElement(labelName);
        public new static readonly string inputUssClassName = ussClassName.WithUssElement("input");

        internal const string k_ModeList = "List";
        internal const string k_ModeSingle = "Single";

        IReadOnlyList<VariableDeclarationModelBase> m_Variables;
        DropdownField m_ModeDropdown;

        public VariableModePropertyField(RootView rootView, IReadOnlyList<VariableDeclarationModelBase> variables)
            : base(rootView)
        {
            m_Variables = variables;

            AddToClassList(ussClassName);
            this.AddPackageStylesheet("VariableModePropertyField.uss");

            var choices = new List<string> { k_ModeSingle, k_ModeList };
            m_ModeDropdown = new DropdownField("Mode", choices, 0);
            m_ModeDropdown.AddToClassList(inputUssClassName);

            m_ModeDropdown.RegisterValueChangedCallback(OnModeChanged);

            Add(m_ModeDropdown);
        }

        void OnModeChanged(ChangeEvent<string> evt)
        {
            if (m_Variables.Count == 0) 
                return;

            // Determine the Base Type from the first variable.
            var firstVar = m_Variables[0];
            var firstVarType = firstVar.DataType.Resolve();
            Type baseType = firstVarType;

            if (TypeExtensions.IsListOrArray(firstVarType))
            {
                if (firstVarType.IsArray)
                    baseType = firstVarType.GetElementType();
                else if (firstVarType.IsGenericType && firstVarType.GetGenericTypeDefinition() == typeof(List<>))
                    baseType = firstVarType.GetGenericArguments()[0];
            }

            // Calculate the Target Type based on Graph Support
            var newModeIsList = evt.newValue == k_ModeList;
            Type targetType = null;

            if (newModeIsList)
            {
                // We want a collection. Check what the graph allows.
                var listType = typeof(List<>).MakeGenericType(baseType);
                var arrayType = baseType.MakeArrayType();
                
                bool supportsList = true; // Default to true if we can't check
                bool supportsArray = false;

                if (firstVar.GraphModel is GraphModelImp graphImp)
                {
                    supportsList = graphImp.SupportedTypes.Contains(listType);
                    supportsArray = graphImp.SupportedTypes.Contains(arrayType);
                }

                // Prefer List, fallback to Array, otherwise default to List (let backend validate)
                if (supportsList)
                    targetType = listType;
                else if (supportsArray)
                    targetType = arrayType;
                else
                    targetType = listType;
            }
            else
            {
                // Switch to Single
                targetType = baseType;
            }

            if (targetType != null)
            {
                CommandTarget.Dispatch(new ChangeVariableTypeCommand(m_Variables, targetType.GenerateTypeHandle()));
            }
        }

        public override void UpdateDisplayedValue()
        {
            if (m_Variables.Count < 1) return;

            var firstType = m_Variables[0].DataType.Resolve();
            var isList = TypeExtensions.IsListOrArray(firstType);

            // Ensure ALL selected variables support switching modes.
            // If even one variable is in a graph that doesn't support the "other" mode, we hide the dropdown.
            if (!AreModesSupported(m_Variables))
            {
                style.display = DisplayStyle.None;
                return;
            }

            style.display = DisplayStyle.Flex;

            // Check if mixed
            for (int i = 1; i < m_Variables.Count; ++i)
            {
                var type = m_Variables[i].DataType.Resolve();
                if (TypeExtensions.IsListOrArray(type) != isList)
                {
                    m_ModeDropdown.SetValueWithoutNotify(null);
                    m_ModeDropdown.showMixedValue = true;
                    return;
                }
            }

            m_ModeDropdown.showMixedValue = false;
            m_ModeDropdown.SetValueWithoutNotify(isList ? k_ModeList : k_ModeSingle);
        }

        bool AreModesSupported(IReadOnlyList<VariableDeclarationModelBase> variables)
        {
            foreach (var variable in variables)
            {
                if (variable.GraphModel is not GraphModelImp graphImp)
                    continue;

                // Resolve the Base Type
                var currentType = variable.DataType.Resolve();
                Type baseType = currentType;
                
                if (TypeExtensions.IsListOrArray(currentType))
                {
                    if (currentType.IsArray)
                        baseType = currentType.GetElementType();
                    else if (currentType.IsGenericType)
                        baseType = currentType.GetGenericArguments()[0];
                }

                if (baseType == null) return false;

                // Construct the Single and List variants
                var singleType = baseType;
                var listType = typeof(List<>).MakeGenericType(baseType);

                // Check if the Graph supports BOTH
                // According to design: "If there are <2 of data structures available... dropdown should not show"
                bool supportsSingle = graphImp.SupportedTypes.Contains(singleType);
                bool supportsList = graphImp.SupportedTypes.Contains(listType);

                // We also check if Array is supported as a proxy for List support if List isn't explicitly there,
                // depending on how strict your graph is.
                if (!supportsList)
                {
                    var arrayType = baseType.MakeArrayType();
                    if (graphImp.SupportedTypes.Contains(arrayType))
                        supportsList = true;
                }

                if (!supportsSingle || !supportsList)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
