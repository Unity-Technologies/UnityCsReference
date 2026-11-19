// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.SmartStrings.PersistentVariables;

namespace Unity.SmartStrings;

/// <summary>
/// Provides additional information to the smart formatter for sources and formatters to use during formatting.
/// </summary>
public class AdditionalFormatData
{
    /// <summary>
    /// Storage for local variables that may be used during formatting by the <see cref="Unity.SmartStrings.Extensions.PersistentVariablesSource"/>.
    /// </summary>
    public IVariableGroup LocalVariables { get; set; }

    /// <summary>
    /// Any <see cref="IVariableValueChanged"/> that may have been used during formatting.
    /// This can then be used to subscribe to update events in order to trigger a regeneration of the string.
    /// </summary>
    public List<IVariableValueChanged> VariableTriggers { get; } = new List<IVariableValueChanged>();
}
