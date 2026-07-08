// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Internal;
using Unity.Collections;

namespace UnityEditor.Build
{
    ///<summary>Identifies which platform's build settings an API applies to.</summary>
    ///<remarks>Use <see cref="BuildTarget" /> to choose the platform output to build. Use <see cref="NamedBuildTarget" /> to choose which platform's settings to read or change.</remarks>
    public readonly struct NamedBuildTarget : IEquatable<NamedBuildTarget>, IComparable<NamedBuildTarget>
    {
        private static readonly string[] k_ValidNames =
        {
            "",
            "FakePlatform",
            "Standalone",
            "Server",
            "iPhone",
            "Android",
            "WebGL",
            "Windows Store Apps",
            "PS4",
            "XboxOne",
            "tvOS",
            "VisionOS",
            "Nintendo Switch",
            "Stadia",
            "CloudRendering",
            "LinuxHeadlessSimulation",
            "Lumin",
            // This should have been "GameCoreXboxSeries"
            "GameCoreScarlett",
            "GameCoreXboxOne",
            "PS5",
            "EmbeddedLinux",
            "QNX",
            "Nintendo Switch 2",
            "Kepler",
        };

        ///<summary>An unknown or unspecified named build target, typically used as a placeholder.</summary>
        public static readonly NamedBuildTarget Unknown = new NamedBuildTarget("");
        ///<summary>Desktop Standalone.</summary>
        public static readonly NamedBuildTarget Standalone = new NamedBuildTarget("Standalone");
        ///<summary>Server.</summary>
        public static readonly NamedBuildTarget Server = new NamedBuildTarget("Server");
        ///<summary>iOS.</summary>
        public static readonly NamedBuildTarget iOS = new NamedBuildTarget("iPhone");
        ///<summary>Android.</summary>
        public static readonly NamedBuildTarget Android = new NamedBuildTarget("Android");
        ///<summary>WebGL.</summary>
        public static readonly NamedBuildTarget WebGL = new NamedBuildTarget("WebGL");
        ///<summary>Windows Store Apps.</summary>
        public static readonly NamedBuildTarget WindowsStoreApps = new NamedBuildTarget("Windows Store Apps");
        // NDA platforms should not have been added here into the public API
        ///<summary>PS4.</summary>
        public static readonly NamedBuildTarget PS4 = new NamedBuildTarget("PS4");
        ///<summary>PS5.</summary>
        public static readonly NamedBuildTarget PS5 = new NamedBuildTarget("PS5");
        ///<summary>Xbox One.</summary>
        public static readonly NamedBuildTarget XboxOne = new NamedBuildTarget("XboxOne");
        ///<summary>TvOS.</summary>
        public static readonly NamedBuildTarget tvOS = new NamedBuildTarget("tvOS");
        ///<summary>Apple visionOS.</summary>
        public static readonly NamedBuildTarget VisionOS = new NamedBuildTarget("VisionOS");
        ///<summary>Nintendo Switch.</summary>
        public static readonly NamedBuildTarget NintendoSwitch = new NamedBuildTarget("Nintendo Switch");
        ///<summary>Nintendo Switch 2.</summary>
        public static readonly NamedBuildTarget NintendoSwitch2 = new NamedBuildTarget("Nintendo Switch 2");
        [System.Obsolete("Stadia has been removed in 2023.1")]
        [ExcludeFromDocs]
        public static readonly NamedBuildTarget Stadia = new NamedBuildTarget("Stadia");
        ///<summary>LinuxHeadlessSimulation.</summary>
        public static readonly NamedBuildTarget LinuxHeadlessSimulation = new NamedBuildTarget("LinuxHeadlessSimulation");
        ///<summary>CloudRendering.</summary>
        [System.Obsolete("CloudRendering is deprecated, please use LinuxHeadlessSimulation (UnityUpgradable) -> LinuxHeadlessSimulation", false)]
        public static readonly NamedBuildTarget CloudRendering = LinuxHeadlessSimulation;
        ///<summary>EmbeddedLinux.</summary>
        public static readonly NamedBuildTarget EmbeddedLinux  = new NamedBuildTarget("EmbeddedLinux");
        ///<summary>QNX.</summary>
        public static readonly NamedBuildTarget QNX  = new NamedBuildTarget("QNX");

        ///<summary>Name of the build target.</summary>
        public string TargetName { get; }

        internal NamedBuildTarget(string targetName)
        {
            if (!k_ValidNames.Contains(targetName))
            {
                throw new ArgumentException($"'{targetName}' is not a valid build target name");
            }

            TargetName = targetName;
        }

        ///<summary>Returns the appropriate BuildTargetGroup that corresponds to the specified NamedBuildTarget.</summary>
        ///<param name="namedBuildTarget">Named build target.</param>
        public BuildTargetGroup ToBuildTargetGroup()
        {
            switch (TargetName)
            {
                case "Server":
                    return BuildTargetGroup.Standalone;
                default:
                    return BuildPipeline.GetBuildTargetGroupByName(TargetName);
            }
        }

        ///<summary>Returns the appropriate NamedBuildTarget that corresponds to the specified BuildTargetGroup.</summary>
        ///<param name="buildTargetGroup">Build target group.</param>
        public static NamedBuildTarget FromBuildTargetGroup(BuildTargetGroup buildTargetGroup)
        {
            switch (buildTargetGroup)
            {
                case BuildTargetGroup.Unknown:
                    return NamedBuildTarget.Unknown;
                case BuildTargetGroup.Standalone:
                    return NamedBuildTarget.Standalone;
                case BuildTargetGroup.iOS:
                    return NamedBuildTarget.iOS;
                case BuildTargetGroup.Android:
                    return NamedBuildTarget.Android;
                case BuildTargetGroup.WebGL:
                    return NamedBuildTarget.WebGL;
                case BuildTargetGroup.WSA:
                    return NamedBuildTarget.WindowsStoreApps;
                case BuildTargetGroup.PS4:
                    return NamedBuildTarget.PS4;
                case BuildTargetGroup.XboxOne:
                    return NamedBuildTarget.XboxOne;
                case BuildTargetGroup.tvOS:
                    return NamedBuildTarget.tvOS;
                case BuildTargetGroup.VisionOS:
                    return NamedBuildTarget.VisionOS;
                case BuildTargetGroup.Switch:
                    return NamedBuildTarget.NintendoSwitch;
                case BuildTargetGroup.Switch2:
                    return NamedBuildTarget.NintendoSwitch2;
                case BuildTargetGroup.LinuxHeadlessSimulation:
                    return NamedBuildTarget.LinuxHeadlessSimulation;
                case BuildTargetGroup.EmbeddedLinux:
                    return NamedBuildTarget.EmbeddedLinux;
                case BuildTargetGroup.QNX:
                    return NamedBuildTarget.QNX;

                // Build targets that are not explicitly listed
                case BuildTargetGroup.GameCoreXboxSeries:
                    return new NamedBuildTarget("GameCoreScarlett");
                case BuildTargetGroup.GameCoreXboxOne:
                    return new NamedBuildTarget("GameCoreXboxOne");
                case BuildTargetGroup.PS5:
                    return new NamedBuildTarget("PS5");
                case BuildTargetGroup.Kepler:
                    return new NamedBuildTarget("Kepler");
            }

            throw new ArgumentException($"There is no a valid NamedBuildTarget for BuildTargetGroup '{buildTargetGroup}'");
        }

        // TODO: We shouldn't be assuming that the namedBuildTarget can be extracted from the
        // active settings. This should be passed through the callstack instead when building.
        // We will need to use BuildTargetSelection (BuildTarget + Subtarget) that is in the cpp side.
        // For now this fixes an issue where Dedicated Server compiles with the Standalone settings.
        internal static NamedBuildTarget FromActiveSettings(BuildTarget target)
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);

            if (buildTargetGroup == BuildTargetGroup.Standalone && (StandaloneBuildSubtarget)EditorUserBuildSettings.GetActiveSubtargetFor(target) == StandaloneBuildSubtarget.Server)
            {
                return NamedBuildTarget.Server;
            }

            return NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
        }

        internal static NamedBuildTarget FromTargetAndSubtarget(BuildTarget target, int subtarget)
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
            if (buildTargetGroup == BuildTargetGroup.Standalone)
            {
                var standaloneSubtarget = (StandaloneBuildSubtarget)subtarget;
                switch (standaloneSubtarget)
                {
                    case StandaloneBuildSubtarget.Player:
                        return NamedBuildTarget.Standalone;
                    case StandaloneBuildSubtarget.Server:
                        return NamedBuildTarget.Server;
                    default:
                        throw new ArgumentException($"'{standaloneSubtarget}' is not a valid subtarget for the Standalone build target");
                }
            }

            return NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
        }

        [ExcludeFromDocs]
        public static bool operator==(NamedBuildTarget lhs, NamedBuildTarget rhs)
        {
            return lhs.Equals(rhs);
        }

        [ExcludeFromDocs]
        public static bool operator!=(NamedBuildTarget lhs, NamedBuildTarget rhs)
        {
            return !lhs.Equals(rhs);
        }

        /// <summary>
        /// Returns a hash code for the current NamedBuildTarget's target name.
        /// </summary>
        /// <returns>A hash code for the current NamedBuildTarget's target name.</returns>
        public override int GetHashCode()
        {
            return TargetName.GetHashCode();
        }

        /// <summary>
        /// Returns true if the given object type and target name is exactly equal to this type and target name.
        /// </summary>
        /// <param name="other">The other object that is used for equality check</param>
        /// <returns>True if the given object target name and type is exactly equal to this target name and type.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            return Equals((NamedBuildTarget)obj);
        }

        /// <summary>
        /// Returns true if the given NamedBuildTarget target name is exactly equal to this target name.
        /// </summary>
        /// <param name="other">The other NameBuildTarget that is used for equality check</param>
        /// <returns>True if the given NamedBuildTarget target name is exactly equal to this target name.</returns>
        public bool Equals(NamedBuildTarget other)
        {
            return TargetName == other.TargetName;
        }

        /// <summary>
        /// Determines the sorting relation of another NamedBuildTarget's target name to the current instance.
        /// </summary>
        /// <param name="other">The other NamedBuildTarget to compare</param>
        /// <returns>An integer value comparing the target name property of two NamedBuildTarget types.</returns>
        public int CompareTo(NamedBuildTarget other)
        {
            return TargetName.CompareTo(other.TargetName);
        }
    }
}
