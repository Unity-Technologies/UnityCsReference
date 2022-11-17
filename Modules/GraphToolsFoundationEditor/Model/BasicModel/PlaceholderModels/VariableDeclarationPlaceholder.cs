// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A model that represents the placeholder of a variable declaration.
    /// </summary>
    [Serializable]
    class VariableDeclarationPlaceholder : VariableDeclarationModel, IPlaceholder
    {
        /// <inheritdoc />
        public long ReferenceId { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableDeclarationPlaceholder" /> class.
        /// </summary>
        public VariableDeclarationPlaceholder()
        {
            this.ClearCapabilities();
            this.SetCapability(Editor.Capabilities.Deletable, true);
            this.SetCapability(Editor.Capabilities.Selectable, true);
        }
    }
}
