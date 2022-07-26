// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;

namespace UnityEditor.Build
{
    public readonly struct NamedBuildTarget : IEquatable<NamedBuildTarget>, IComparable<NamedBuildTarget>
    {
        private static readonly string[] k_ValidNames =
        {
            "",
            "Standalone",
            "Server",
            "iPhone",
            "Android",
            "WebGL",
            "Windows Store Apps",
            "PS4",
            "XboxOne",
            "tvOS",
            "Nintendo Switch",
            "Stadia",
            "CloudRendering",
            "LinuxHeadlessSimulation",
            "Lumin",
            "GameCoreScarlett",
            "GameCoreXboxOne",
            "PS5",
            "EmbeddedLinux",
            "QNX",
        };

        public static readonly NamedBuildTarget Unknown = new NamedBuildTarget("");
        public static readonly NamedBuildTarget Standalone = new NamedBuildTarget("Standalone");
        public static readonly NamedBuildTarget Server = new NamedBuildTarget("Server");
        public static readonly NamedBuildTarget iOS = new NamedBuildTarget("iPhone");
        public static readonly NamedBuildTarget Android = new NamedBuildTarget("Android");
        public static readonly NamedBuildTarget WebGL = new NamedBuildTarget("WebGL");
        public static readonly NamedBuildTarget WindowsStoreApps = new NamedBuildTarget("Windows Store Apps");
        public static readonly NamedBuildTarget PS4 = new NamedBuildTarget("PS4");
        public static readonly NamedBuildTarget XboxOne = new NamedBuildTarget("XboxOne");
        public static readonly NamedBuildTarget tvOS = new NamedBuildTarget("tvOS");
        public static readonly NamedBuildTarget NintendoSwitch = new NamedBuildTarget("Nintendo Switch");
        public static readonly NamedBuildTarget Stadia = new NamedBuildTarget("Stadia");
        public static readonly NamedBuildTarget LinuxHeadlessSimulation = new NamedBuildTarget("LinuxHeadlessSimulation");
        [System.Obsolete("CloudRendering is deprecated, please use LinuxHeadlessSimulation (UnityUpgradable) -> LinuxHeadlessSimulation", false)]
        public static readonly NamedBuildTarget CloudRendering = LinuxHeadlessSimulation;
        public static readonly NamedBuildTarget EmbeddedLinux  = new NamedBuildTarget("EmbeddedLinux");
        public static readonly NamedBuildTarget QNX  = new NamedBuildTarget("QNX");

        public string TargetName { get; }

        internal NamedBuildTarget(string targetName)
        {
            if (!k_ValidNames.Contains(targetName))
            {
                throw new ArgumentException($"'{targetName}' is not a valid build target name");
            }

            TargetName = targetName;
        }

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
                case BuildTargetGroup.Switch:
                    return NamedBuildTarget.NintendoSwitch;
                case BuildTargetGroup.Stadia:
                    return NamedBuildTarget.Stadia;
                case BuildTargetGroup.LinuxHeadlessSimulation:
                    return NamedBuildTarget.LinuxHeadlessSimulation;
                case BuildTargetGroup.EmbeddedLinux:
                    return NamedBuildTarget.EmbeddedLinux;
                case BuildTargetGroup.QNX:
                    return NamedBuildTarget.QNX;

                // Build targets that are not explicitly listed
                case BuildTargetGroup.Lumin:
                    return new NamedBuildTarget("Lumin");
                case BuildTargetGroup.GameCoreXboxSeries:
                    return new NamedBuildTarget("GameCoreScarlett");
                case BuildTargetGroup.GameCoreXboxOne:
                    return new NamedBuildTarget("GameCoreXboxOne");
                case BuildTargetGroup.PS5:
                    return new NamedBuildTarget("PS5");
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

        public static bool operator==(NamedBuildTarget lhs, NamedBuildTarget rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator!=(NamedBuildTarget lhs, NamedBuildTarget rhs)
        {
            return !lhs.Equals(rhs);
        }

        public override int GetHashCode()
        {
            return TargetName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            return Equals((NamedBuildTarget)obj);
        }

        public bool Equals(NamedBuildTarget other)
        {
            return TargetName == other.TargetName;
        }

        public int CompareTo(NamedBuildTarget other)
        {
            return TargetName.CompareTo(other.TargetName);
        }
    }
}
