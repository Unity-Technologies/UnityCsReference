// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
/// Defines a custom toolbar element to be displayed in the toolbar.
/// </summary>
internal struct ToolbarElementDefinition
{
    /// <summary>
    /// The display order of this toolbar element. Lower values appear first.
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// The type of the toolbar element to instantiate.
    /// </summary>
    public Type ElementType { get; }

    /// <summary>
    /// Creates a new <see cref="ToolbarElementDefinition"/>.
    /// </summary>
    /// <param name="order">The display order of the element. Lower values appear first.</param>
    /// <param name="elementType">The type of the toolbar element to instantiate.</param>
    public ToolbarElementDefinition(int order, Type elementType)
    {
        Order = order;
        ElementType = elementType;
    }
}

    /// <summary>
    /// Base class for toolbar definitions.
    /// </summary>
    [UnityRestricted]
    internal abstract class ToolbarDefinition
    {
        /// <summary>
        /// The ids of the element to display in the toolbar. The ids are the one specified using the <see cref="EditorToolbarElementAttribute"/>.
        /// </summary>
        public abstract IEnumerable<string> ElementIds { get; }

        // Maps element id -> ToolbarElementDefinition for custom elements
        public virtual IReadOnlyDictionary<string, ToolbarElementDefinition> CustomElementMap
            => new Dictionary<string, ToolbarElementDefinition>();
    }
}
