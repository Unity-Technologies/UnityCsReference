// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor;

/// <summary>
/// Attribute used to specify metadata for a <see cref="State"/> class, such as its icon.
/// </summary>
/// <remarks>
/// Apply this attribute to a class derived from <see cref="State"/> to define metadata like <see cref="NodeAttribute.IconPath"/>.
/// <br/>
/// This is the <see cref="State"/> counterpart of <see cref="NodeAttribute"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class StateAttribute : NodeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StateAttribute"/> class.
    /// </summary>
    /// <param name="categoryPath">The category path of the state in the graph item library.</param>
    public StateAttribute(string categoryPath)
        : base(categoryPath)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StateAttribute"/> class.
    /// </summary>
    /// <param name="categoryPath">The category path of the state in the graph item library.</param>
    /// <param name="iconPath">The file path to the state's icon.</param>
    public StateAttribute(string categoryPath, string iconPath)
        : base(categoryPath, iconPath)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StateAttribute"/> class.
    /// </summary>
    /// <param name="categoryPath">The category path of the state in the graph item library.</param>
    /// <param name="iconPath">The file path to the state's icon.</param>
    /// <param name="title">The title of the state in the graph item library. It is also used as the title of this state when it is instantiated in a graph.</param>
    public StateAttribute(string categoryPath, string iconPath, string title)
        : base(categoryPath, iconPath, title)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StateAttribute"/> class.
    /// </summary>
    /// <param name="categoryPath">The category path of the state in the graph item library.</param>
    /// <param name="iconPath">The file path to the state's icon.</param>
    /// <param name="title">The title of the state in the graph item library. It is also used as the title of this state when it is instantiated in a graph.</param>
    /// <param name="stylesheet">Path to a stylesheet (.uss) used to customize the state's <see cref="VisualElement"/> appearance.</param>
    public StateAttribute(string categoryPath, string iconPath, string title, string stylesheet)
        : base(categoryPath, iconPath, title, stylesheet)
    {
    }
}
