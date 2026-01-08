// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// <see cref="VariableLibraryItem"/> representing a Variable to be created.
    /// </summary>
    [UnityRestricted]
    internal class VariableLibraryItem : TypeLibraryItem
    {
        /// <summary>
        /// The type of the created variable that must derived from VariableDeclarationModel.
        /// </summary>
        public Type VariableType { get; set; }

        /// <summary>
        /// The scope of the created variable.
        /// </summary>
        public VariableScope Scope { get; set; }

        /// <summary>
        /// The modifier flags of the created variable.
        /// </summary>
        public ModifierFlags ModifierFlags { get; set; }

        /// <summary>
        /// Initializes a new instance of the VariableLibraryItem class.
        /// </summary>
        /// <param name="name">The name used to search the item.</param>
        /// <param name="type">The type represented by the item.</param>
        /// <param name="variableType">The type that must derived from VariableDeclarationModel.</param>
        /// <param name="graphModel">The graph model associated with the item.</param>
        public VariableLibraryItem(string name, TypeHandle type, Type variableType = null, GraphModel graphModel = null)
            : base(name, type, graphModel)
        {
            VariableType = variableType;
        }
    }
}
