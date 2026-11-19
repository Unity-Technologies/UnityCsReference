// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor;

/// <summary>
/// Attribute used to specify metadata for a <see cref="SelfTransition"/> class.
/// </summary>
/// <remarks>
/// Apply this attribute to a class derived from <see cref="SelfTransition"/> to define its default
/// <see cref="CategoryPath"/>, <see cref="IconPath"/> and <see cref="Title"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class TransitionAttribute : Attribute
{
    /// <summary>
    /// The file path to the transition's icon.
    /// </summary>
    /// <remarks>
    /// For dark and light themes, provide two files with identical names, adding a d_ prefix to the dark theme icon.
    /// Use a higher resolution (such as 128x128) to ensure the icon appears clear when zoomed in the graph.
    /// </remarks>
    public string IconPath { get; }

    /// <summary>
    /// The title of this transition type.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// The category path used to place the transition creation action in a submenu of the state context menu.
    /// </summary>
    /// <remarks>
    /// Use <c>/</c> to separate nested submenus. For example, a <see cref="CategoryPath"/> of <c>"Transitions/Advanced"</c>
    /// places the transition under the <c>Transitions/Advanced</c> submenu.
    /// If the category path is null or empty, the transition creation action appears at the root of the context menu.
    /// </remarks>
    public string CategoryPath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TransitionAttribute"/> class.
    /// </summary>
    /// <param name="categoryPath">The category path used to place the transition in the <c>Create Self Transition</c> menu.</param>
    public TransitionAttribute(string categoryPath)
    {
        CategoryPath = categoryPath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TransitionAttribute"/> class.
    /// </summary>
    /// <param name="categoryPath">The category path used to place the transition in the <c>Create Self Transition</c> menu.</param>
    /// <param name="iconPath">The file path to the transition's icon.</param>
    public TransitionAttribute(string categoryPath, string iconPath)
    {
        CategoryPath = categoryPath;
        IconPath = iconPath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TransitionAttribute"/> class.
    /// </summary>
    /// <param name="categoryPath">The category path used to place the transition in the <c>Create Self Transition</c> menu.</param>
    /// <param name="iconPath">The file path to the transition's icon.</param>
    /// <param name="title">The title displayed for the transition type.</param>
    public TransitionAttribute(string categoryPath, string iconPath, string title)
    {
        CategoryPath = categoryPath;
        IconPath = iconPath;
        Title = title;
    }
}
