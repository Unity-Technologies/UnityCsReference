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
    /// Registers an icon and a color for a given data type.
    /// </summary>
    /// <param name="dataType">The data type to associate with the style.</param>
    /// <param name="icon">The icon to display for the data type. Pass <see langword="null"/> to suppress the icon so no icon is shown for this data type.</param>
    /// <param name="color">The color representing the data type.</param>
    /// <remarks>
    /// Call this from your custom <see cref="DataTypeStyleMapper"/> constructor to ensure registration.
    /// Graph type restrictions are determined by the <see cref="DataTypeStyleMapperAttribute"/> on the class.
    /// </remarks>
    /// <example>
    /// <code>
    /// [DataTypeStyleMapper(typeof(MyGraphModel))]
    /// class MyDataTypeStyleMapper : DataTypeStyleMapper
    /// {
    ///     MyDataTypeStyleMapper()
    ///     {
    ///         // Register a type with a custom icon and color.
    ///         Register(typeof(MyCustomType), Resources.Load&lt;Texture2D&gt;("MyIcon"), Color.cyan);
    ///
    ///         // Register a type with the default icon and a custom color.
    ///         Register(typeof(MyDefaultIconType), Color.green);
    ///
    ///         // Register a type with a color but no icon shown.
    ///         Register(typeof(MyOtherType), null, Color.magenta);
    ///     }
    /// }
    /// </code>
    /// </example>
    public void Register(Type dataType, Texture2D icon, Color color)
    {
        BaseDataTypeStyleMapper.Register(dataType, icon, color, suppressIcon: icon == null, GetType().GetCustomAttribute<DataTypeStyleMapperAttribute>()?.GraphTypes);
    }

    /// <summary>
    /// Registers a color for a given data type while keeping the default USS icon.
    /// </summary>
    /// <param name="dataType">The data type to associate with the color.</param>
    /// <param name="color">The color representing the data type.</param>
    /// <remarks>
    /// Call this from your custom <see cref="DataTypeStyleMapper"/> constructor to ensure registration.
    /// The default icon for the data type is preserved; only the color is overridden.
    /// Graph type restrictions are determined by the <see cref="DataTypeStyleMapperAttribute"/> on the class.
    /// </remarks>
    public void Register(Type dataType, Color color)
    {
        BaseDataTypeStyleMapper.Register(dataType, null, color, suppressIcon: false, GetType().GetCustomAttribute<DataTypeStyleMapperAttribute>()?.GraphTypes);
    }
}
