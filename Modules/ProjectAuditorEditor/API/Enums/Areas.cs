// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Which area(s) of a project may be affected by a ReportItem.
    /// </summary>
    [Flags]
    public enum Areas : int
    {
        /// <summary>
        /// Indicates some error with the Descriptor data: A descriptor should never match no areas.
        /// </summary>
        None = 0,

        /// <summary>
        /// Application size
        /// </summary>
        BuildSize = 1 << 0,

        /// <summary>
        /// Build time
        /// </summary>
        BuildTime = 1 << 1,

        /// <summary>
        /// CPU Performance
        /// </summary>
        CPU = 1 << 2,

        /// <summary>
        /// GPU Performance
        /// </summary>
        GPU = 1 << 3,

        /// <summary>
        /// Issues which affect iteration time in the Editor and can hamper productivity during development
        /// </summary>
        IterationTime = 1 << 4,

        /// <summary>
        /// Load times
        /// </summary>
        LoadTime = 1 << 5,

        /// <summary>
        /// Memory consumption
        /// </summary>
        Memory = 1 << 6,

        /// <summary>
        /// Quality. For example, using preview packages, or settings that negatively affect visual quality
        /// </summary>
        Quality = 1 << 7,

        /// <summary>
        /// Required by platform. Typically this issue must be fixed before submitting to the platform store
        /// </summary>
        Requirement = 1 << 8,

        /// <summary>
        /// Lack of platform support. For example, using APIs that are not supported on a specific platform and might fail at runtime
        /// </summary>
        Support = 1 << 9,

        /// <summary>
        /// Upgrade. For example, issues that may prevent you from upgrading to a newer version of Unity.
        /// </summary>
        Upgrade = 1 << 10,

        // Add new items in alphabetical order and adjust the values (including "All") accordingly.
        // Areas are serialised as strings, so it doesn't matter if the values change between package versions so long as old reports have been saved.

        /// <summary>
        /// Bitmask value representing all areas
        /// </summary>
        All = (1 << 11) - 1
    }
}
