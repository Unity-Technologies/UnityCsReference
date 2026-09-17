// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;
using Unity.SmartStrings.Core.Extensions;
using Unity.SmartStrings.Core.Settings;

namespace Unity.SmartStrings.Extensions;

/// <summary>
/// Evaluates a selector by reading the member of the same name through the Unity.Properties data model.
/// </summary>
/// <remarks>
/// Resolution goes through <see cref="PropertyContainer"/>, so any type Unity.Properties can build a
/// property bag for is supported, including plain classes and structs and
/// <see cref="UnityEngine.MonoBehaviour"/> fields, with no property bag registered in advance.
/// 
/// The members that resolve mirror Unity's serialization rules: public instance fields and members
/// marked with <see cref="CreatePropertyAttribute"/> or <see cref="UnityEngine.SerializeField"/>.
/// Methods and plain, non-attributed properties are not visited, so those selectors return
/// <see langword="false"/> and are left to a later source.
/// 
/// 
/// Matching is ordinal by default. When the formatter is configured for case-insensitive placeholders,
/// a selector with no exact member match is resolved by a case-insensitive scan of the members,
/// returning the first match.
/// 
/// 
/// On fully ahead-of-time compiled players, Unity.Properties may be unable to build a property bag
/// for a type that is not referenced elsewhere.
/// 
/// </remarks>
[Serializable]
public class PropertiesSource : Source
{
    [NonSerialized]
    CaseInsensitiveVisitor m_CaseInsensitiveVisitor;

    /// <inheritdoc />
    public override bool TryEvaluateSelector(ISelectorInfo selectorInfo)
    {
        var current = selectorInfo.CurrentValue;

        if (TrySetResultForNullableOperator(selectorInfo)) return true;

        // strings are processed by StringSource
        if (current is null or string) return false;

        var selector = selectorInfo.SelectorText;

        // Fast path: an exact (ordinal) member match.
        if (PropertyContainer.TryGetValue(ref current, PropertyPath.FromName(selector), out object value))
        {
            selectorInfo.Result = value;
            return true;
        }

        // Case-insensitive formatters fall back to a scan, since the ordinal lookup above only
        // matches the exact case.
        if (selectorInfo.FormatDetails.Settings.CaseSensitivity == CaseSensitivityType.CaseInsensitive)
        {
            var visitor = m_CaseInsensitiveVisitor ??= new CaseInsensitiveVisitor();
            visitor.Reset(selector);
            if (PropertyContainer.TryAccept(visitor, ref current) && visitor.Found)
            {
                selectorInfo.Result = visitor.Value;
                return true;
            }
        }

        return false;
    }

    sealed class CaseInsensitiveVisitor : IPropertyBagVisitor
    {
        string m_Target;

        public bool Found { get; private set; }
        public object Value { get; private set; }

        public void Reset(string target)
        {
            m_Target = target;
            Found = false;
            Value = null;
        }

        public void Visit<TContainer>(IPropertyBag<TContainer> properties, ref TContainer container)
        {
            foreach (var property in properties.GetProperties(ref container))
            {
                // Collection elements are indexed by position, not named members, so skip them; otherwise a selector like {0} matches element 0.
                if (property is ICollectionElementProperty)
                    continue;

                if (!string.Equals(property.Name, m_Target, StringComparison.OrdinalIgnoreCase))
                    continue;

                Value = property.GetValue(ref container);
                Found = true;
                return;
            }
        }
    }
}
