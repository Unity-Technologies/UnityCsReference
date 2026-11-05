// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.ObjectModel;

namespace UnityEditor.ShaderApiReflection
{
    /// <summary>
    /// Interface for accessing hints associated with a shader object such as a struct or function.
    /// </summary>
    /// <remarks>
    /// Use this interface to reference objects that contain hints, regardless of the object's type.
    /// For example, a function with an <see cref="IHasHints"/> parameter may be called with either
    /// a <see cref="ReflectedStruct"/> or a <see cref="ReflectedFunction"/>.
    /// </remarks>
    /// <example nocheck="true">
    /// In the following example, <c>TryGetSummaryHint</c> is used to fetch the "summary" hint from
    /// both <see cref="ReflectedStruct"/>s and <see cref="ReflectedFunction"/>s.
    /// <code source="../../Tests/UTFTests/FunctionalTests/Editor/Documentation/ReflectedFunctionExample.cs"/>
    /// </example>
    /// <seealso cref="ReflectedStruct"/>
    /// <seealso cref="ReflectedParameter"/>
    /// <seealso cref="ReflectedFunction"/>
    /// <seealso cref="ReflectedField"/>
    public interface IHasHints
    {
        /// <summary>
        /// The object's metadata, extracted from any structured comments decorating its declaration in source code.
        /// </summary>
        /// <remarks>
        /// A hint is a key-value pair of strings that may be used to store arbitrary metadata about a shader object.
        /// Hints are pure data, so they don't do anything in isolation. Certain tools like Shader Graph make use of
        /// certain named hints.
        /// </remarks>
        public ReadOnlyDictionary<string, string> Hints { get; }
    }
}
