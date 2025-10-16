// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A source of <see cref="VariableDeclarationModelBase"/>.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal abstract class ExternalVariableSource
    {
        /// <summary>
        /// Tells whether this source is the same as another.
        /// </summary>
        /// <param name="other">The other source.</param>
        /// <returns>True if this source is the same as the other, false otherwise.</returns>
        public abstract bool IsSame(ExternalVariableSource other);

        /// <summary>
        /// Gets the variable declarations from this source.
        /// </summary>
        /// <param name="outList">The list to fill with the variable declarations from this source. The list is cleared from any previous content.</param>
        public abstract void GetVariableDeclarations(List<VariableDeclarationModelBase> outList);

        /// <summary>
        /// Gets the variable declaration with the given GUID.
        /// </summary>
        /// <param name="variableGuid">The GUID of the variable declaration to get.</param>
        /// <returns>The variable declaration with the given GUID, or null if no such variable declaration exists in this source.</returns>
        public abstract VariableDeclarationModelBase GetVariableDeclaration(Hash128 variableGuid);

        /// <summary>
        /// Sets the variable source dirty. Called when a variable declaration from the source is modified.
        /// </summary>
        public abstract void SetDirty();
    }
}
