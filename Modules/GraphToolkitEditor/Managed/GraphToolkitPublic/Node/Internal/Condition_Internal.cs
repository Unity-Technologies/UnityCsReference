// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.GraphToolkit.Editor.Implementation;
using UnityEditor;

namespace Unity.GraphToolkit.Editor
{
    public abstract partial class Condition
    {
        [NonSerialized]
        internal UserConditionModelImp m_Implementation;

        internal string DisplayNameInternal => string.IsNullOrEmpty(Title) ? GetTypeDisplayName(GetType()) : Title;

        internal static string GetTypeDisplayName(Type conditionType)
        {
            var title = conditionType.GetCustomAttribute<ConditionAttribute>()?.Title;
            return string.IsNullOrEmpty(title) ? ObjectNames.NicifyVariableName(conditionType.Name) : title;
        }

        internal virtual bool DisplayComparisonDropdownInternal => false;

        internal virtual IReadOnlyList<ConditionComparison> SupportedComparisonsInternal => null;

        internal UserConditionModelImp GetImplementation()
        {
            if (m_Implementation == null)
            {
                CreateImplementation();
            }

            return m_Implementation;
        }

        internal void CreateImplementation()
        {
            new UserConditionModelImp().InitCustomCondition(this);
        }

        internal void SetImplementation(UserConditionModelImp implementation)
        {
            m_Implementation = implementation;
        }
    }

    public abstract partial class Condition<T>
    {
        internal override bool DisplayComparisonDropdownInternal => DisplayComparisonDropdown;

        internal override IReadOnlyList<ConditionComparison> SupportedComparisonsInternal => SupportedComparisons;
    }
}
