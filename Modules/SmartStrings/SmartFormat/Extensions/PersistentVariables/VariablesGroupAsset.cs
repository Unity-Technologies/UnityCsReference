// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.SmartStrings.Core.Extensions;

namespace Unity.SmartStrings.PersistentVariables;

/// <summary>
/// Provides a reference to a <see cref="VariablesGroupAsset"/>.
/// </summary>
public class NestedVariablesGroup : Variable<VariablesGroupAsset>, IVariableGroup
{
    /// <inheritdoc/>
    public bool TryGetValue(string name, out IVariable value)
    {
        if (Value != null)
            return Value.TryGetValue(name, out value);
        value = default;
        return false;
    }
}

[Serializable]
internal class VariableNameValuePair
{
    public string name;

    [SerializeReference]
    public IVariable variable;

    public override string ToString() => $"{name} - {variable?.GetType().Name}";
}

/// <summary>
/// Collection of <see cref="IVariable"/> that can be used during formatting of a localized string.
/// </summary>
[CreateAssetMenu(menuName = "Smart Strings/Variables Group")]
[HelpURL("smart-strings/persistent-variables-source")]
public class VariablesGroupAsset : ScriptableObject, IVariableGroup, IVariable, IDictionary<string, IVariable>, ISerializationCallbackReceiver
{
    [SerializeField]
    internal List<VariableNameValuePair> m_Variables = new List<VariableNameValuePair>();

    Dictionary<string, VariableNameValuePair> m_VariableLookup = new Dictionary<string, VariableNameValuePair>();

    /// <summary>
    /// The number of variables in the group.
    /// </summary>
    public int Count => m_VariableLookup.Count;

    /// <summary>
    /// A collection of all the unique variable names.
    /// </summary>
    public ICollection<string> Keys => m_VariableLookup.Keys;

    /// <summary>
    /// All the variables in this group.
    /// </summary>
    public ICollection<IVariable> Values
    {
        get
        {
            var result = new List<IVariable>(m_VariableLookup.Count);
            foreach (var pair in m_VariableLookup.Values)
                result.Add(pair.variable);
            return result;
        }
    }

    /// <summary>
    /// Implemented as part of IDictionary but not used. Always returns <see langword="false"/>.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// The <see cref="IVariable"/> associated with the specified name.
    /// </summary>
    /// <param name="name">The name of the variable.</param>
    /// <returns>The found variable.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if a variable with the specified name does not exist.</exception>
    public IVariable this[string name]
    {
        get => m_VariableLookup[name].variable;
        set => Add(name, value);
    }

    /// <inheritdoc/>
    public object GetSourceValue(ISelectorInfo _) => this;

    /// <summary>
    /// Gets the <see cref="IVariable"/> with the specified name.
    /// </summary>
    /// <param name="name">The name of the variable.</param>
    /// <param name="value">The variable that was found or <see langword="default"/>.</param>
    /// <returns><see langword="true"/> if a variable was found and <see langword="false"/> if one could not.</returns>
    public bool TryGetValue(string name, out IVariable value)
    {
        if (m_VariableLookup.TryGetValue(name, out var v))
        {
            value = v.variable;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Adds a new Global Variable to use during formatting.
    /// </summary>
    /// <param name="name">The name of the variable, must be unique. Note the name should not contain any whitespace, if any is found then it will be replaced with with '-'.</param>
    /// <param name="variable">The variable to use when formatting. See also <seealso cref="BoolVariable"/>, <seealso cref="FloatVariable"/>, <seealso cref="IntVariable"/>, <seealso cref="StringVariable"/>, <seealso cref="ObjectVariable"/>.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when variable is null.</exception>
    public void Add(string name, IVariable variable)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException(nameof(name), "Name must not be null or empty.");
        if (variable == null)
            throw new ArgumentNullException(nameof(variable));

        name = name.ReplaceWhiteSpaces("-");
        var v = new VariableNameValuePair { name = name, variable = variable };
        m_VariableLookup.Add(name, v);
        m_Variables.Add(v);
    }

    /// <summary>
    /// <inheritdoc cref="Add(string, IVariable)"/>
    /// </summary>
    /// <param name="item">Name and variable pair to add.</param>
    public void Add(KeyValuePair<string, IVariable> item) => Add(item.Key, item.Value);

    /// <summary>
    /// Removes a variable with the specified name.
    /// </summary>
    /// <param name="name">Name of the variable to remove.</param>
    /// <returns><see langword="true"/> if a variable with the specified name was removed, <see langword="false"/> if one was not.</returns>
    public bool Remove(string name)
    {
        if (m_VariableLookup.TryGetValue(name, out var v))
        {
            m_Variables.Remove(v);
            m_VariableLookup.Remove(name);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes a variable with the specified key.
    /// </summary>
    /// <param name="item">The item to be removed, only the Key field will be considered.</param>
    /// <returns><see langword="true"/> if a variable with the specified name was removed, <see langword="false"/> if one was not.</returns>
    public bool Remove(KeyValuePair<string, IVariable> item) => Remove(item.Key);

    /// <summary>
    /// Returns <see langword="true"/> if a variable with the specified name exists.
    /// </summary>
    /// <param name="name">The variable name to check for.</param>
    /// <returns><see langword="true"/> if a matching variable could be found or <see langword="false"/> if one could not.</returns>
    public bool ContainsKey(string name) => m_VariableLookup.ContainsKey(name);

    /// <summary>
    /// <inheritdoc cref="ContainsKey(string)"/>
    /// </summary>
    /// <param name="item">The item to check for. Both the Key and Value must match.</param>
    /// <returns><see langword="true"/> if a matching variable could be found or <see langword="false"/> if one could not.</returns>
    public bool Contains(KeyValuePair<string, IVariable> item) => TryGetValue(item.Key, out var v) && v == item.Value;

    /// <summary>
    /// Copies the variables into an array starting at <paramref name="arrayIndex"/>.
    /// </summary>
    /// <param name="array">The array to copy the variables into.</param>
    /// <param name="arrayIndex">The index to start copying the items into.</param>
    /// <exception cref="ArgumentNullException">Thrown when the array is null.</exception>
    public void CopyTo(KeyValuePair<string, IVariable>[] array, int arrayIndex)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));

        foreach (var entry in m_VariableLookup)
        {
            array[arrayIndex++] = new KeyValuePair<string, IVariable>(entry.Key, entry.Value.variable);
        }
    }

    /// <summary>
    /// <inheritdoc cref="GetEnumerator"/>
    /// </summary>
    /// <returns>The enumerator that can be used to iterate through all the variables.</returns>
    IEnumerator<KeyValuePair<string, IVariable>> IEnumerable<KeyValuePair<string, IVariable>>.GetEnumerator()
    {
        foreach (var v in m_VariableLookup)
        {
            yield return new KeyValuePair<string, IVariable>(v.Key, v.Value.variable);
        }
    }

    /// <summary>
    /// Returns an enumerator for all variables in this group.
    /// </summary>
    /// <returns>The enumerator that can be used to iterate through all the variables.</returns>
    public IEnumerator GetEnumerator()
    {
        foreach (var v in m_VariableLookup)
        {
            yield return new KeyValuePair<string, IVariable>(v.Key, v.Value.variable);
        }
    }
    /// <summary>
    /// Removes all variables in the group.
    /// </summary>
    public void Clear()
    {
        m_VariableLookup.Clear();
        m_Variables.Clear();
    }

    void ISerializationCallbackReceiver.OnBeforeSerialize() {}

    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        if (m_VariableLookup == null)
            m_VariableLookup = new Dictionary<string, VariableNameValuePair>();

        m_VariableLookup.Clear();
        foreach (var v in m_Variables)
        {
            if (!string.IsNullOrEmpty(v.name))
            {
                m_VariableLookup[v.name] = v;
            }
        }
    }
}
