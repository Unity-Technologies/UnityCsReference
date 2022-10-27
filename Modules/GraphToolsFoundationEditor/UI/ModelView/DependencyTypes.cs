// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Type of dependencies for a <see cref="ModelView"/>.
    /// </summary>
    [Flags]
    enum DependencyTypes
    {
        None = 0,
        Style = 1,
        Geometry = 2,
        Removal = 4,
    }

    /// <summary>
    /// Extensions methods for <see cref="DependencyTypes"/>.
    /// </summary>
    static class DependencyTypeExtensions
    {
        /// <summary>
        /// Checks if <paramref name="value"/> has the flag <paramref name="flag"/> set.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="flag">The flag to check.</param>
        /// <returns>True if value has the flag set.</returns>
        public static bool HasFlagFast(this DependencyTypes value, DependencyTypes flag)
        {
            return (value & flag) != 0;
        }
    }
}
