// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A model that represents the placeholder of a variable declaration.
    /// </summary>
    [Serializable]
    class VariableDeclarationPlaceholder : VariableDeclarationModelBase, IPlaceholder
    {
        /// <inheritdoc />
        public long ReferenceId { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableDeclarationPlaceholder" /> class.
        /// </summary>
        public VariableDeclarationPlaceholder()
        {
            PlaceholderModelHelper.SetPlaceholderCapabilities(this);
        }

        /// <inheritdoc />
        public override VariableFlags VariableFlags { get; set; }

        /// <inheritdoc />
        public override ModifierFlags Modifiers { get; set; }

        /// <inheritdoc />
        public override TypeHandle DataType { get; set; }

        /// <inheritdoc />
        public override VariableScope Scope { get; set; }

        /// <inheritdoc />
        public override bool ShowOnInspectorOnly { get; set; }

        /// <inheritdoc />
        public override string Tooltip { get; set; }

        /// <inheritdoc />
        public override Constant InitializationModel { get; set; }

        /// <inheritdoc />
        public override void CreateInitializationValue() { }
    }
}
