// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine;

namespace Unity.GraphToolkit.Editor;

/// <summary>
/// Maps custom data types to their visual styles in a graph.
/// </summary>
/// <remarks>
/// Use this to register icons and colors for custom data types in a graph.
/// Do not use to override existing or built-in type styles. Overriding existing or built-in type styles can lead to inconsistent visuals and unexpected behavior in the graph editor.
/// The custom icon and color are used wherever the data type is visually represented, such as on ports, wires, and variables.
/// </remarks>
public class DataTypeStyleMapper
{
    /// <summary>
    /// Registers an icon and color style for a given data type.
    /// </summary>
    /// <param name="dataType">The data type to associate with the style.</param>
    /// <param name="icon">The icon representing the data type.</param>
    /// <param name="color">The color representing the data type.</param>
    /// <remarks>
    /// Call this from your custom <see cref="DataTypeStyleMapper"/> constructor to ensure registration.
    /// Graph type restrictions are determined by the <see cref="DataTypeStyleMapperAttribute"/> on the class.
    /// </remarks>
    public void Register(Type dataType, Texture2D icon, Color color)
    {
        BaseDataTypeStyleMapper.Register(dataType, icon, color, GetType().GetCustomAttribute<DataTypeStyleMapperAttribute>()?.GraphTypes);
    }
}
