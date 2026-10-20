// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Entities
{
    /// <summary>
    /// Utilities to manipulate version numbers
    /// </summary>
    static public class ChangeVersionUtility
    {
        /// <summary>
        /// Test whether two version numbers indicate that a change has occurred.
        /// </summary>
        /// <param name="changeVersion">The newer/current version number</param>
        /// <param name="requiredVersion">The previous version number.</param>
        /// <returns>True if <paramref name="changeVersion"/> is greater than <paramref name="requiredVersion"/>,
        /// or if <paramref name="requiredVersion"/> is zero.</returns>
        public static bool DidChange(uint changeVersion, uint requiredVersion)
        {
            // When a system runs for the first time, everything is considered changed.
            if (requiredVersion == 0)
                return true;
            // Supporting wrap around for version numbers, change must be bigger than last system run.
            // (Never detect change of something the system itself changed)
            return (int)(changeVersion - requiredVersion) > 0;
        }

        internal static void IncrementGlobalSystemVersion(ref uint globalSystemVersion)
        {
            globalSystemVersion++;
            // Handle wrap around, 0 is reserved for systems that have never run..
            if (globalSystemVersion == 0)
                globalSystemVersion++;
        }

        // 0 is reserved for systems that have never run
        internal const uint InitialGlobalSystemVersion = 1;
    }
}
