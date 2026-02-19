// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor;

/// <summary>
/// Attribute used to specify metadata for a <see cref="Node"/> class, such as its icon.
/// </summary>
/// <remarks>
/// Apply this attribute to a class derived from <see cref="Node"/> to define metadata like <see cref="IconPath"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class NodeAttribute : Attribute
{
    /// <summary>
    /// The file path to the node's icon.
    /// </summary>
    /// <remarks>
    /// For dark and light themes, provide two files with identical names, adding a d_ prefix to the dark theme icon.
    /// Use a higher resolution (such as 128x128) to ensure the icon appears clear when zoomed in the graph.
    /// The icon will appear on the node in the graph and in the graph item library.
    /// </remarks>
    public string IconPath { get; }

    /// <summary>
    /// The category path of this node in the graph item library.
    /// </summary>
    /// <remarks>
    /// This does not modify the node's title in the graph item library.
    /// For example, if class A that inherits from Node has a NodeAttribute that sets its CategoryPath to "My Nodes/A",
    /// then node A will have the path "My Nodes/A/A" inside the graph item library.
    /// If you wish to modify the node's title, use <see cref="Title"/>.
    /// </remarks>
    /// <seealso cref="Title"/>
    public string CategoryPath { get; }

    /// <summary>
    /// The title of this node in the graph item library.
    /// </summary>
    /// <remarks>
    /// If <see cref="Title"/> is not empty and non-null, it serves as the default title of the node when instantiated
    /// in a graph. You can overwrite this default title with <see cref="INode.Title"/>.
    /// </remarks>
    /// <seealso cref="INode.Title"/>
    /// <seealso cref="CategoryPath"/>
    public string Title { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NodeAttribute"/> class.
    /// </summary>
    /// <param name="categoryPath">The category path of the node in the graph item library.</param>
    public NodeAttribute(string categoryPath)
    {
        CategoryPath = categoryPath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NodeAttribute"/> class.
    /// </summary>
    /// <param name="categoryPath">The category path of the node in the graph item library.</param>
    /// <param name="iconPath">The file path to the node's icon.</param>
    public NodeAttribute(string categoryPath, string iconPath)
    {
        CategoryPath = categoryPath;
        IconPath = iconPath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NodeAttribute"/> class.
    /// </summary>
    /// <param name="categoryPath">The category path of the node in the graph item library.</param>
    /// <param name="iconPath">The file path to the node's icon.</param>
    /// <param name="title">The title of the node in the graph item library. It is also used as the title of this node when it is instantiated in a graph.</param>
    public NodeAttribute(string categoryPath, string iconPath, string title)
    {
        CategoryPath = categoryPath;
        IconPath = iconPath;
        Title = title;
    }
}
