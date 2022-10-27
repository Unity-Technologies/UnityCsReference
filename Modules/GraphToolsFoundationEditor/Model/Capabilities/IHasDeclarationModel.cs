// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Interface for node models that own a <see cref="DeclarationModel"/>.
    /// </summary>
    interface IHasDeclarationModel
    {
        /// <summary>
        /// The declaration model.
        /// </summary>
        DeclarationModel DeclarationModel { get; set; }
    }
}
