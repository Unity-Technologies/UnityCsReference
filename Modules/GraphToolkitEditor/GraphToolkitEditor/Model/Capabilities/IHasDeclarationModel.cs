// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for node models that own a <see cref="DeclarationModel"/>.
    /// </summary>
    [UnityRestricted]
    internal interface IHasDeclarationModel
    {
        /// <summary>
        /// The declaration model.
        /// </summary>
        DeclarationModel DeclarationModel { get; }

        /// <summary>
        /// Sets the declaration model.
        /// </summary>
        void SetDeclarationModel(DeclarationModel value);
    }
}
