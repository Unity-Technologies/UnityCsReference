// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;

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

        /// <summary>
        /// Migration To CoreCLR. Issues that prevent you from switching from Mono to the CoreCLR scripting backend.
        /// </summary>
        MigrationToCoreCLR = 1 << 11,

        /// <summary>
        /// Migration To URP. Issues that prevent you from migrating to the Universal Render Pipeline.
        /// </summary>
        MigrationToURP = 1 << 12,

        /// <summary>
        /// Bitmask value representing all areas
        /// </summary>
        All = (1 << 13) - 1
    }

    internal static class AreasExtensions
    {
        [NoAutoStaticsCleanup] // Lazy-initialized cache of area strings; data is still valid after code reload
        static Dictionary<Areas, string> s_FrontendStrings;

        // The individual area flags (i.e. excluding "None" and the "All" bitmask), in alphabetical order by name
        internal static Areas[] AlphabeticalAreas => s_AlphabeticalAreas;
        static readonly Areas[] s_AlphabeticalAreas = GetAlphabeticalAreas();

        internal static string ToFrontendString(this Areas areas)
        {
            if (s_FrontendStrings == null)
                s_FrontendStrings = new Dictionary<Areas, string>();
            if (s_FrontendStrings.TryGetValue(areas, out var frontendString))
                return frontendString;

            frontendString = BuildFrontendString(areas);
            s_FrontendStrings[areas] = frontendString;
            return frontendString;
        }

        static string BuildFrontendString(Areas areas)
        {
            var sb = new StringBuilder();

            // "None" and "All" have names of their own, so don't spell out the individual flags they represent
            if (areas != Areas.None && areas != Areas.All)
            {
                foreach (var area in s_AlphabeticalAreas)
                {
                    if ((areas & area) == 0)
                        continue;
                    if (sb.Length > 0)
                        sb.Append(", ");
                    sb.Append(area.ToString());
                }
            }

            // Fall back on the enum's own formatting for "None", "All", and any undefined flags
            return ObjectNames.NicifyVariableName(sb.Length > 0 ? sb.ToString() : areas.ToString());
        }

        static Areas[] GetAlphabeticalAreas()
        {
            var names = new List<string>(Enum.GetNames(typeof(Areas)));

            // We're not interested in "None" or "All"
            names.Remove(nameof(Areas.None));
            names.Remove(nameof(Areas.All));
            names.Sort(StringComparer.OrdinalIgnoreCase);

            var areas = new Areas[names.Count];
            for (var i = 0; i < names.Count; ++i)
                areas[i] = (Areas)Enum.Parse(typeof(Areas), names[i]);
            return areas;
        }
    }
}
