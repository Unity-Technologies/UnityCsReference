// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// String extension methods.
    /// </summary>
    static class StringExtensions
    {
        /// <summary>
        /// Makes a displayable name for a variable.
        /// </summary>
        /// <remarks>This is merely a wrapper for <see cref="ObjectNames.NicifyVariableName"/>.</remarks>
        /// <param name="value">The variable name to nicify.</param>
        /// <returns>The nicified variable name.</returns>
        public static string Nicify(this string value)
        {
            return ObjectNames.NicifyVariableName(value);
        }
    }
}
