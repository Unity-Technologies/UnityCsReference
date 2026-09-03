// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using Unity.SmartStrings.PersistentVariables;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.Localization;

/// <summary>
/// A named collection of Smart String variables owned by a localized string.
/// </summary>
/// <remarks>
/// A <see cref="LocalVariablesGroup"/> implements <see cref="IVariableGroup"/> to hold the local <see cref="IVariable"/>s
/// that a <see cref="LocalizedString"/> formats against. Smart String placeholders such as <c>{score}</c> look up values in
/// this group during formatting. Variables are stored polymorphically through <c>[SerializeReference]</c>, so a scalar, an
/// object, or a nested <see cref="LocalizedString"/> can all be added. Names are unique and whitespace-normalized:
/// <see cref="Add"/> returns the name a variable was actually stored under.
/// </remarks>
/// <example>
/// <para>Add a variable and check that it exists.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalVariablesGroupOverviewExample.cs"/>
/// </example>
/// <seealso cref="LocalizedString"/>
/// <seealso cref="IVariable"/>
[Serializable]
public class LocalVariablesGroup : IVariableGroup, ISerializationCallbackReceiver
{
    [SerializeField] List<NamedVariable> m_Variables = new();

    internal List<NamedVariable> Variables
    {
        get => m_Variables;
        set
        {
            m_Variables = value ?? new List<NamedVariable>();
            m_Lookup = null;
            MakeNamesUnique();
        }
    }

    // UXML assigns the whole list at once rather than going through Add, which is what normally applies these.
    void MakeNamesUnique()
    {
        using var _ = HashSetPool<string>.Get(out var taken);
        foreach (var v in m_Variables)
        {
            if (v == null || string.IsNullOrEmpty(v.name))
                continue;
            v.name = Unique(Normalize(v.name), taken);
            taken.Add(v.name);
        }
    }

    // Every named entry counts, including one whose variable never resolved, which Lookup leaves out.
    void CollectNames(HashSet<string> taken)
    {
        foreach (var v in m_Variables)
        {
            if (v != null && !string.IsNullOrEmpty(v.name))
                taken.Add(Normalize(v.name));
        }
    }

    Dictionary<string, IVariable> m_Lookup;

    Dictionary<string, IVariable> Lookup
    {
        get
        {
            if (m_Lookup == null)
            {
                m_Lookup = new Dictionary<string, IVariable>(m_Variables.Count);
                foreach (var v in m_Variables)
                {
                    if (v != null && !string.IsNullOrEmpty(v.name) && v.variable != null)
                        m_Lookup[Normalize(v.name)] = v.variable;
                }
            }
            return m_Lookup;
        }
    }

    /// <summary>
    /// The number of variables in the group.
    /// </summary>
    public int Count => m_Variables.Count;

    /// <inheritdoc/>
    public bool TryGetValue(string key, out IVariable value)
    {
        if (!string.IsNullOrEmpty(key))
            return Lookup.TryGetValue(Normalize(key), out value);
        value = null;
        return false;
    }

    /// <summary>
    /// Determines whether a variable is stored under the given name.
    /// </summary>
    /// <remarks>Lookup is case-sensitive and matches the whitespace-normalized name assigned by <see cref="Add"/>.</remarks>
    /// <param name="name">The variable name to look for.</param>
    /// <returns><c>true</c> when a variable is stored under the name; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>Check whether a variable exists before using it.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalVariablesGroupContainsNameExample.cs"/>
    /// </example>
    public bool ContainsName(string name) => !string.IsNullOrEmpty(name) && Lookup.ContainsKey(Normalize(name));

    /// <summary>
    /// Adds a variable under a unique, whitespace-normalized name and returns that name.
    /// </summary>
    /// <remarks>
    /// The requested name is normalized by replacing whitespace with hyphens, then made unique by appending a numeric suffix
    /// when needed (for example, <c>variable</c>, then <c>variable-2</c>). When no name is given, it defaults to
    /// <c>variable</c>. The returned name is the one the variable is actually stored under.
    /// </remarks>
    /// <param name="variable">The variable to store in the group.</param>
    /// <param name="name">A preferred name for the variable, or null to use the default.</param>
    /// <returns>The unique name that the variable was stored under.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="variable"/> is null.</exception>
    /// <example>
    /// <para>Add a variable and keep the name it was stored under.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalVariablesGroupAddExample.cs"/>
    /// </example>
    public string Add(IVariable variable, string name = null)
    {
        if (variable == null)
            throw new ArgumentNullException(nameof(variable));
        name = UniqueName(Normalize(string.IsNullOrEmpty(name) ? "variable" : name));
        m_Variables.Add(new NamedVariable { name = name, variable = variable });
        m_Lookup = null;
        return name;
    }

    /// <summary>
    /// Removes the variable stored under the given name.
    /// </summary>
    /// <remarks>The name is whitespace-normalized the way <see cref="Add"/> normalizes it. Does nothing when no variable matches.</remarks>
    /// <param name="name">The name of the variable to remove.</param>
    /// <returns><c>true</c> when a variable was removed; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>Remove a variable by name.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalVariablesGroupRemoveExample.cs"/>
    /// </example>
    public bool Remove(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        name = Normalize(name);
        for (var i = 0; i < m_Variables.Count; i++)
        {
            if (m_Variables[i] != null && Normalize(m_Variables[i].name ?? string.Empty) == name)
            {
                m_Variables.RemoveAt(i);
                m_Lookup = null;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Removes every variable from the group.
    /// </summary>
    /// <remarks>Leaves the group empty, so Smart String placeholders that referenced the removed variables no longer resolve.</remarks>
    /// <example>
    /// <para>Remove all variables.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalVariablesGroupClearExample.cs"/>
    /// </example>
    public void Clear()
    {
        m_Variables.Clear();
        m_Lookup = null;
    }

    static string Normalize(string name)
    {
        var hasWhitespace = false;
        foreach (var c in name)
        {
            if (char.IsWhiteSpace(c))
            {
                hasWhitespace = true;
                break;
            }
        }
        if (!hasWhitespace)
            return name;
        var builder = new StringBuilder(name.Length);
        foreach (var c in name)
            builder.Append(char.IsWhiteSpace(c) ? '-' : c);
        return builder.ToString();
    }

    string UniqueName(string baseName)
    {
        using var _ = HashSetPool<string>.Get(out var taken);
        CollectNames(taken);
        return Unique(baseName, taken);
    }

    static string Unique(string baseName, ICollection<string> taken)
    {
        if (!taken.Contains(baseName))
            return baseName;
        for (var i = 2; ; ++i)
        {
            var candidate = $"{baseName}-{i}";
            if (!taken.Contains(candidate))
                return candidate;
        }
    }

    void ISerializationCallbackReceiver.OnBeforeSerialize() { }

    void ISerializationCallbackReceiver.OnAfterDeserialize() => m_Lookup = null;
}

/// <summary>
/// One named variable in a <see cref="LocalVariablesGroup"/>.
/// </summary>
/// <remarks>
/// A group is a name to variable map, which neither Unity serialization nor UXML can express directly, so each pair is
/// stored as one of these.
/// </remarks>
[Serializable]
[UxmlObject]
[VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
internal partial class NamedVariable
{
    [UxmlAttribute, Delayed]
    public string name;

    [SerializeReference, UxmlObjectReference]
    public IVariable variable;
}

/// <summary>
/// A read-through of two variable groups: a primary group with a fallback. Used to give a nested localized
/// string its own variables while still reading the variables defined on the parent (chained scope).
/// </summary>
sealed class ChainedVariableGroup : IVariableGroup
{
    readonly IVariableGroup m_Primary;
    readonly IVariableGroup m_Fallback;

    public ChainedVariableGroup(IVariableGroup primary, IVariableGroup fallback)
    {
        m_Primary = primary;
        m_Fallback = fallback;
    }

    public bool TryGetValue(string key, out IVariable value)
    {
        if (m_Primary != null && m_Primary.TryGetValue(key, out value))
            return true;
        if (m_Fallback != null)
            return m_Fallback.TryGetValue(key, out value);
        value = null;
        return false;
    }
}
