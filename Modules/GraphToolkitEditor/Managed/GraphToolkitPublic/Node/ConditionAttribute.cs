// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor;

/// <summary>
/// Attribute used to specify metadata for a <see cref="Condition"/> class, such as its title.
/// </summary>
/// <remarks>
/// Apply this attribute to a class derived from <see cref="Condition"/> to define its <see cref="Title"/>, used to
/// name the condition in the add-condition menu of the transition inspector. Without the attribute, the menu shows
/// the condition type name formatted for display. The label of the condition row in the transition inspector is
/// controlled by <see cref="Condition.Title"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ConditionAttribute : Attribute
{
    /// <summary>
    /// The title of this condition type.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConditionAttribute"/> class.
    /// </summary>
    /// <param name="title">The title displayed for the condition type in the add-condition menu.</param>
    public ConditionAttribute(string title)
    {
        Title = title;
    }
}
