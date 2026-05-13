// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.Implementation;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    internal class VariableModePropertyField : BaseModelPropertyField
    {
        public new static readonly string ussClassName = "variable-mode-property-field";
        public new static readonly string labelUssClassName = ussClassName.WithUssElement(labelName);
        public new static readonly string inputUssClassName = ussClassName.WithUssElement("input");

        internal const string k_ModeArray = "Array";
        internal const string k_ModeList = "List";
        internal const string k_ModeSingle = "Single";

        readonly IReadOnlyList<VariableDeclarationModelBase> m_Variables;
        readonly DropdownField m_ModeDropdown;

        /// <summary>
        /// Bit flags representing which modes (Single, List, Array) a variable supports.
        /// </summary>
        [Flags]
        enum SupportedModes
        {
            None = 0,
            Single = 1 << 0,
            List = 1 << 1,
            Array = 1 << 2
        }

        struct VariableModeInfo
        {
            public VariableDeclarationModelBase Variable;
            public Type BaseType;
            public SupportedModes SupportedModes;
            public SupportedModes CurrentMode;
        }

        public VariableModePropertyField(RootView rootView, IReadOnlyList<VariableDeclarationModelBase> variables)
            : base(rootView)
        {
            m_Variables = variables;

            AddToClassList(ussClassName);
            this.AddPackageStylesheet("VariableModePropertyField.uss");

            var choices = new List<string> { k_ModeSingle, k_ModeList, k_ModeArray };
            m_ModeDropdown = new DropdownField("Mode", choices, 0);
            m_ModeDropdown.AddToClassList(inputUssClassName);

            m_ModeDropdown.labelElement.AddToClassList(labelUssClassName);
            m_ModeDropdown.labelElement.AddToClassList(BaseModelPropertyField.labelUssClassName);

            m_ModeDropdown.RegisterValueChangedCallback(OnModeChanged);

            Add(m_ModeDropdown);
        }

        void OnModeChanged(ChangeEvent<string> evt)
        {
            if (m_Variables.Count == 0 || string.IsNullOrEmpty(evt.newValue))
                return;

            if (!IsValidModeString(evt.newValue))
            {
                Debug.LogError($"Invalid mode string: {evt.newValue}");
                return;
            }

            var targetMode = StringToMode(evt.newValue);

            using (ListPool<VariableModeInfo>.Get(out var modeInfos))
            {
                GetModeInfoForVariables(modeInfos);

                if (!CanAllVariablesSwitchToMode(modeInfos, targetMode))
                    return;

                // Build types list - one per variable
                var types = new List<TypeHandle>(modeInfos.Count);
                for (int i = 0; i < modeInfos.Count; i++)
                {
                    var targetType = GetTargetType(targetMode, modeInfos[i].BaseType);
                    types.Add(targetType.GenerateTypeHandle());
                }

                CommandTarget.Dispatch(new ChangeVariableTypeCommand(m_Variables, types));
            }
        }

        public override void UpdateDisplayedValue()
        {
            if (m_Variables.Count < 1)
            {
                style.display = DisplayStyle.None;
                return;
            }

            using (ListPool<VariableModeInfo>.Get(out var modeInfos))
            {
                GetModeInfoForVariables(modeInfos);

                if (modeInfos.Count == 0)
                {
                    style.display = DisplayStyle.None;
                    return;
                }

                var commonModes = GetCommonSupportedModes(modeInfos);
                bool mixedModes = AreModesMixed(modeInfos);
                int modeCount = GetModeCount(commonModes);

                // Hide dropdown if no common modes, or if single variable with only 1 possible mode
                if (modeCount == 0 || (modeInfos.Count == 1 && modeCount == 1))
                {
                    style.display = DisplayStyle.None;
                    return;
                }

                style.display = DisplayStyle.Flex;

                UpdateDropdownChoices(commonModes);
                UpdateDropdownValue(modeInfos, mixedModes);
            }
        }

        void UpdateDropdownChoices(SupportedModes commonModes)
        {
            using (ListPool<string>.Get(out var orderedChoices))
            {
                if ((commonModes & SupportedModes.Single) != 0)
                    orderedChoices.Add(k_ModeSingle);
                if ((commonModes & SupportedModes.List) != 0)
                    orderedChoices.Add(k_ModeList);
                if ((commonModes & SupportedModes.Array) != 0)
                    orderedChoices.Add(k_ModeArray);

                if (!AreChoicesEqual(m_ModeDropdown.choices, orderedChoices))
                {
                    m_ModeDropdown.choices = new List<string>(orderedChoices);
                }
            }
        }

        void UpdateDropdownValue(List<VariableModeInfo> modeInfos, bool mixedModes)
        {
            if (mixedModes)
            {
                m_ModeDropdown.SetValueWithoutNotify(null);
                m_ModeDropdown.showMixedValue = true;
            }
            else
            {
                m_ModeDropdown.showMixedValue = false;
                m_ModeDropdown.SetValueWithoutNotify(ModeToString(modeInfos[0].CurrentMode));
            }
        }

        void GetModeInfoForVariables(List<VariableModeInfo> outInfos)
        {
            outInfos.Clear();

            for (int i = 0; i < m_Variables.Count; i++)
            {
                var variable = m_Variables[i];
                var currentType = variable.DataType.Resolve();

                if (currentType == null)
                    continue;

                var info = new VariableModeInfo { Variable = variable };

                if (currentType.IsArray)
                {
                    info.BaseType = currentType.GetElementType();
                    info.CurrentMode = SupportedModes.Array;
                }
                else if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    info.BaseType = currentType.GetGenericArguments()[0];
                    info.CurrentMode = SupportedModes.List;
                }
                else
                {
                    info.BaseType = currentType;
                    info.CurrentMode = SupportedModes.Single;
                }

                if (info.BaseType == null)
                    continue;

                info.SupportedModes = GetSupportedModesForVariable(variable, info.BaseType);
                outInfos.Add(info);
            }
        }

        SupportedModes GetSupportedModesForVariable(VariableDeclarationModelBase variable, Type baseType)
        {
            var modes = SupportedModes.None;

            if (variable.GraphModel is GraphModelImp graphImp)
            {
                var singleType = baseType;
                var listType = typeof(List<>).MakeGenericType(baseType);
                var arrayType = baseType.MakeArrayType();

                if (graphImp.SupportedTypes.Contains(singleType))
                    modes |= SupportedModes.Single;
                if (graphImp.SupportedTypes.Contains(listType))
                    modes |= SupportedModes.List;
                if (graphImp.SupportedTypes.Contains(arrayType))
                    modes |= SupportedModes.Array;
            }
            else
            {
                modes = SupportedModes.Single | SupportedModes.List | SupportedModes.Array;
            }

            return modes;
        }

        /// <summary>
        /// Returns the intersection of supported modes across all selected variables.
        /// </summary>
        static SupportedModes GetCommonSupportedModes(List<VariableModeInfo> infos)
        {
            if (infos.Count == 0)
                return SupportedModes.None;

            var commonModes = infos[0].SupportedModes;

            for (int i = 1; i < infos.Count; i++)
            {
                commonModes &= infos[i].SupportedModes;
            }

            return commonModes;
        }

        static bool AreModesMixed(List<VariableModeInfo> infos)
        {
            if (infos.Count <= 1)
                return false;

            var firstMode = infos[0].CurrentMode;
            for (int i = 1; i < infos.Count; i++)
            {
                if (infos[i].CurrentMode != firstMode)
                    return true;
            }

            return false;
        }

        static bool CanAllVariablesSwitchToMode(List<VariableModeInfo> infos, SupportedModes targetMode)
        {
            for (int i = 0; i < infos.Count; i++)
            {
                if ((infos[i].SupportedModes & targetMode) == 0)
                    return false;
            }

            return true;
        }

        static bool AreChoicesEqual(IList<string> a, IList<string> b)
        {
            if (a == null || b == null)
                return a == b;

            if (a.Count != b.Count)
                return false;

            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }

        static Type GetTargetType(SupportedModes mode, Type baseType)
        {
            switch (mode)
            {
                case SupportedModes.List:
                    return typeof(List<>).MakeGenericType(baseType);
                case SupportedModes.Array:
                    return baseType.MakeArrayType();
                case SupportedModes.Single:
                    return baseType;
                default:
                    Debug.LogError($"Unknown mode: {mode}");
                    return baseType;
            }
        }

        static string ModeToString(SupportedModes mode)
        {
            switch (mode)
            {
                case SupportedModes.Single:
                    return k_ModeSingle;
                case SupportedModes.List:
                    return k_ModeList;
                case SupportedModes.Array:
                    return k_ModeArray;
                default:
                    return k_ModeSingle;
            }
        }

        static SupportedModes StringToMode(string str)
        {
            switch (str)
            {
                case k_ModeSingle:
                    return SupportedModes.Single;
                case k_ModeList:
                    return SupportedModes.List;
                case k_ModeArray:
                    return SupportedModes.Array;
                default:
                    return SupportedModes.Single;
            }
        }

        static bool IsValidModeString(string mode)
        {
            return mode == k_ModeSingle || mode == k_ModeList || mode == k_ModeArray;
        }

        static int GetModeCount(SupportedModes modes)
        {
            int count = 0;
            if ((modes & SupportedModes.Single) != 0)
                count++;
            if ((modes & SupportedModes.List) != 0)
                count++;
            if ((modes & SupportedModes.Array) != 0)
                count++;
            return count;
        }
    }
}
