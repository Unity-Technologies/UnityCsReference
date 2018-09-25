// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using DiscoveredTargetInfo = UnityEditor.BuildTargetDiscovery.DiscoveredTargetInfo;
using TargetAttributes = UnityEditor.BuildTargetDiscovery.TargetAttributes;

namespace UnityEditor.Build
{
    // All settings for a build platform.
    internal class BuildPlatform
    {
        // short name used for texture settings, etc.
        public string name;
        public GUIContent title;
        public Texture2D smallIcon;
        public BuildTargetGroup targetGroup;
        public bool forceShowTarget;
        public string tooltip;
        public BuildTarget defaultTarget;

        public BuildPlatform(string locTitle, string iconId, BuildTargetGroup targetGroup, BuildTarget defaultTarget, bool forceShowTarget)
            : this(locTitle, "", iconId, targetGroup, defaultTarget, forceShowTarget)
        {
        }

        public BuildPlatform(string locTitle, string tooltip, string iconId, BuildTargetGroup targetGroup, BuildTarget defaultTarget, bool forceShowTarget)
        {
            this.targetGroup = targetGroup;
            name = targetGroup != BuildTargetGroup.Unknown ? BuildPipeline.GetBuildTargetGroupName(defaultTarget) : "";
            title = EditorGUIUtility.TextContentWithIcon(locTitle, iconId);
            smallIcon = EditorGUIUtility.IconContent(iconId + ".Small").image as Texture2D;
            this.tooltip = tooltip;
            this.forceShowTarget = forceShowTarget;
            this.defaultTarget = defaultTarget;
        }
    };

    internal class BuildPlatforms
    {
        private static readonly BuildPlatforms s_Instance = new BuildPlatforms();

        public static BuildPlatforms instance
        {
            get
            {
                return s_Instance;
            }
        }

        internal BuildPlatforms()
        {
            List<BuildPlatform> buildPlatformsList = new List<BuildPlatform>();
            DiscoveredTargetInfo[] buildTargets = BuildTargetDiscovery.GetBuildTargetInfoList();

            // Standalone needs to be first
            buildPlatformsList.Add(new BuildPlatform(BuildPipeline.GetBuildTargetGroupDisplayName(BuildTargetGroup.Standalone), "BuildSettings.Standalone", BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows, true));

            foreach (var target in buildTargets)
            {
                if (!target.HasFlag(TargetAttributes.IsStandalonePlatform))
                {
                    BuildTargetGroup btg = BuildPipeline.GetBuildTargetGroup(target.buildTgtPlatformVal);
                    buildPlatformsList.Add(new BuildPlatform(
                        BuildPipeline.GetBuildTargetGroupDisplayName(btg),
                        target.iconName,
                        btg,
                        target.buildTgtPlatformVal,
                        !target.HasFlag(TargetAttributes.HideInUI)));
                }
            }

            // Facebook is a special case and needs to be added separately
            buildPlatformsList.Add(new BuildPlatform(BuildPipeline.GetBuildTargetGroupDisplayName(BuildTargetGroup.Facebook), "BuildSettings.Facebook", BuildTargetGroup.Facebook, BuildTarget.StandaloneWindows64, true));

            foreach (var buildPlatform in buildPlatformsList)
            {
                buildPlatform.tooltip = buildPlatform.title.text + " settings";
            }

            buildPlatforms = buildPlatformsList.ToArray();
        }

        public BuildPlatform[] buildPlatforms;

        public string GetBuildTargetDisplayName(BuildTargetGroup group, BuildTarget target)
        {
            foreach (BuildPlatform cur in buildPlatforms)
            {
                if (cur.defaultTarget == target && cur.targetGroup == group)
                    return cur.title.text;
            }

            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "Windows";
                case BuildTarget.StandaloneOSX:
                    // Deprecated
#pragma warning disable 612, 618
                case BuildTarget.StandaloneOSXIntel:
                case BuildTarget.StandaloneOSXIntel64:
#pragma warning restore 612, 618
                    return "Mac OS X";
                case BuildTarget.StandaloneLinux:
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.StandaloneLinuxUniversal:
                    return "Linux";
            }

            return "Unsupported Target";
        }

        public string GetModuleDisplayName(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
        {
            switch (buildTargetGroup)
            {
                case BuildTargetGroup.Facebook:
                    return BuildPipeline.GetBuildTargetGroupDisplayName(buildTargetGroup);
                default:
                    return GetBuildTargetDisplayName(buildTargetGroup, buildTarget);
            }
        }

        private int BuildPlatformIndexFromTargetGroup(BuildTargetGroup group)
        {
            for (int i = 0; i < buildPlatforms.Length; i++)
                if (group == buildPlatforms[i].targetGroup)
                    return i;
            return -1;
        }

        public bool ContainsBuildTarget(BuildTargetGroup group)
        {
            if (BuildPlatformIndexFromTargetGroup(group) < 0)
                return false;

            return true;
        }

        public BuildPlatform BuildPlatformFromTargetGroup(BuildTargetGroup group)
        {
            int index = BuildPlatformIndexFromTargetGroup(group);
            return index != -1 ? buildPlatforms[index] : null;
        }

        public List<BuildPlatform> GetValidPlatforms(bool includeMetaPlatforms)
        {
            List<BuildPlatform> platforms = new List<BuildPlatform>();
            foreach (BuildPlatform bp in buildPlatforms)
                if ((bp.targetGroup == BuildTargetGroup.Standalone || BuildPipeline.IsBuildTargetSupported(bp.targetGroup, bp.defaultTarget)) && (!(bp.targetGroup == BuildTargetGroup.Facebook) || includeMetaPlatforms))
                    platforms.Add(bp);

            return platforms;
        }

        public List<BuildPlatform> GetValidPlatforms()
        {
            return GetValidPlatforms(false);
        }
    };
}
