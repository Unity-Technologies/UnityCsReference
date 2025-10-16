// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface to control a polymorphic port behavior
    /// </summary>
    [UnityRestricted]
    internal interface IPolymorphicPortHandler
    {
        /// <summary>
        /// Event called when the current port type is changed
        /// </summary>
        event Action<uint> SelectedTypeChanged;

        /// <summary>
        /// Current TypeHandle used by the polymorphic port
        /// </summary>
        TypeHandle SelectedType { get; }

        /// <summary>
        /// When the <see cref="SelectedType"/> is Automatic, this is the real TypeHandle to use (if resolved).
        /// When the current type is not Automatic, this returns Unknown
        /// </summary>
        TypeHandle ResolvedType { get; }

        /// <summary>
        /// Index of the currently selected <see cref="TypeHandle"/> (based on the list of supported <see cref="Types"/>)
        /// </summary>
        uint SelectedTypeIndex { get; }

        /// <summary>
        /// List of supported types for this PolymorphicPortHandler
        /// </summary>
        IReadOnlyList<TypeHandle> Types { get; }

        /// <summary>
        /// Tells if a specific <see cref="TypeHandle"/> is supported by this polymorphic port
        /// </summary>
        /// <param name="typeHandle">TypeHandle to check support for</param>
        /// <returns>True of the TypeHandle is supported, false otherwise</returns>
        bool CanConnect(TypeHandle typeHandle);

        /// <summary>
        /// Defines the resolved type (useful when <see cref="SelectedType"/> is set to Automatic)
        /// </summary>
        /// <param name="typeHandle">TypeHandle to use as resolved type</param>
        void Resolve(TypeHandle typeHandle);

        /// <summary>
        /// Reset the resolved type to Unknown
        /// </summary>
        void Unresolve();

        /// <summary>
        /// Change the currently selected type.
        /// </summary>
        /// <param name="index">Index of the type to use. It must be in the range of the list of supported <see cref="Types"/></param>
        void SetSelectedTypeIndex(uint index);
    }
}
