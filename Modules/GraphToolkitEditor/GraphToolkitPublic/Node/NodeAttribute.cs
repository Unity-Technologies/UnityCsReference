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
    /// Initializes a new instance of the <see cref="NodeAttribute"/> class.
    /// </summary>
    /// <param name="iconPath">The file path to the node's icon.</param>
    public NodeAttribute(string iconPath)
    {
        if (string.IsNullOrEmpty(iconPath))
            Debug.LogWarning("Icon path cannot be null or empty. Please provide a valid icon path. Default icon will be used.");

        IconPath = iconPath;
    }
}
