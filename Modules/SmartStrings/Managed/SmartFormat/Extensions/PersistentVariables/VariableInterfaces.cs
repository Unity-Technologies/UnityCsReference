// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.SmartStrings.Core.Extensions;

namespace Unity.SmartStrings.PersistentVariables;

/// <summary>
/// Provides access to a collection of named <see cref="IVariable"/> values.
/// </summary>
public interface IVariableGroup
{
    /// <summary>
    /// Gets the variable with the matching key if one exists.
    /// </summary>
    /// <param name="key">The variable key or name to match.</param>
    /// <param name="value">The found variable or <see langword="null"/> if one could not be found.</param>
    /// <returns><see langword="true"/> if a variable could be found or <see langword="false"/> if one could not.</returns>
    bool TryGetValue(string key, out IVariable value);
}

/// <summary>
/// Represents a variable that can be provided through a global <see cref="VariablesGroupAsset"/> or as a local
/// variable through localized string instead of as a string format argument.
/// A variable can be a single variable, in which case the value should be returned in <see cref="GetSourceValue(ISelectorInfo)"/> or a
/// class with multiple variables which can then be further extracted with additional string format arguments.
/// </summary>
public interface IVariable
{
    /// <summary>
    /// Returns the value to use when the smart string matches this variable.
    /// </summary>
    /// <param name="selector">The details about the current format operation.</param>
    /// <returns>The value that additional sources or formatters can process further.</returns>
    object GetSourceValue(ISelectorInfo selector);
}

/// <summary>
/// Adds support for querying the Metadata in a Smart String.
/// </summary>
/// <remarks>
/// In some languages, such as Spanish, all nouns have a gender, that means they are either masculine or feminine.
/// The structure of the sentence will change according to the gender of the item.
/// This example shows how metadata can be used to mark an entry with a gender which can
/// then be queried to create a dynamic string with the correct gender forms.
/// This shows how the following metadata could be used in a Smart String, where `item` is a localized string local variable:
/// <c>{item.gender:choose(Male|Female):El|La}</c>
/// </remarks>
public interface IMetadataVariable : IVariable
{
    /// <summary>
    /// The named placeholder that will match this metadata when querying a localized string as a local or global variable.
    /// </summary>
    string VariableName { get; }
}

/// <summary>
/// Provides the ability to trigger an automatic update of a localized string when <see cref="ValueChanged"/> is invoked.
/// </summary>
public interface IVariableValueChanged : IVariable
{
    /// <summary>
    /// Raised when the variable's value changes, or when it otherwise needs to trigger an update to any localized string currently using it.
    /// </summary>
    event Action<IVariable> ValueChanged;
}
