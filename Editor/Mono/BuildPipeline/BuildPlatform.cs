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
        public BuildTargetGroup targetGroup;
        public bool forceShowTarget;
        public string tooltip;
        public BuildTarget defaultTarget;

        private ScalableGUIContent m_Title;
        private ScalableGUIContent m_SmallTitle;

        public GUIContent title => m_Title;
        public Texture2D smallIcon => ((GUIContent)m_SmallTitle).image as Texture2D;

        public BuildPlatform(string locTitle, string iconId, BuildTargetGroup targetGroup, BuildTarget defaultTarget, bool forceShowTarget)
            : this(locTitle, "", iconId, targetGroup, defaultTarget, forceShowTarget)
        {
        }

        public BuildPlatform(string locTitle, string tooltip, string iconId, BuildTargetGroup targetGroup, BuildTarget defaultTarget, bool forceShowTarget)
        {
            this.targetGroup = targetGroup;
            name = targetGroup != BuildTargetGroup.Unknown ? BuildPipeline.GetBuildTargetGroupName(defaultTarget) : "";
            m_Title = new ScalableGUIContent(locTitle, null, iconId);
            m_SmallTitle = new ScalableGUIContent(null, null, iconId + ".Small");
            this.tooltip = tooltip;
            this.forceShowTarget = forceShowTarget;
            this.defaultTarget = defaultTarget;
        }
    }

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
            // Before we had BuildTarget.StandaloneWindows for BuildPlatform.defaultTarget
            // But that doesn't make a lot of sense, as editor use it in places, so it should agree with editor platform
            // TODO: should we poke module manager for target support? i think we can assume support for standalone for editor platform
            // TODO: even then - picking windows standalone unconditionally wasn't much better
            BuildTarget standaloneTarget = BuildTarget.StandaloneWindows;
            if (Application.platform == RuntimePlatform.OSXEditor)
                standaloneTarget = BuildTarget.StandaloneOSX;
            else if (Application.platform == RuntimePlatform.LinuxEditor)
                standaloneTarget = BuildTarget.StandaloneLinux64;

            buildPlatformsList.Add(new BuildPlatform(BuildPipeline.GetBuildTargetGroupDisplayName(BuildTargetGroup.Standalone), "BuildSettings.Standalone", BuildTargetGroup.Standalone, standaloneTarget, true));

            foreach (var target in buildTargets)
            {
                if (!target.HasFlag(TargetAttributes.IsStandalonePlatform))
                {
                    BuildTargetGroup btg = BuildPipeline.GetBuildTargetGroup(target.buildTargetPlatformVal);
                    buildPlatformsList.Add(new BuildPlatform(
                        BuildPipeline.GetBuildTargetGroupDisplayName(btg),
                        target.iconName,
                        btg,
                        target.buildTargetPlatformVal,
                        !target.HasFlag(TargetAttributes.HideInUI)));
                }
            }

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
                    // Deprecated
#pragma warning disable 612, 618
                case BuildTarget.StandaloneLinux:
                case BuildTarget.StandaloneLinuxUniversal:
#pragma warning restore 612, 618
                case BuildTarget.StandaloneLinux64:
                    return "Linux";
            }

            return "Unsupported Target";
        }

        public string GetModuleDisplayName(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
        {
            return GetBuildTargetDisplayName(buildTargetGroup, buildTarget);
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
            return BuildPlatformIndexFromTargetGroup(group) >= 0;
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
                if (bp.targetGroup == BuildTargetGroup.Standalone || BuildPipeline.IsBuildTargetSupported(bp.targetGroup, bp.defaultTarget))
                    platforms.Add(bp);

            return platforms;
        }

        public List<BuildPlatform> GetValidPlatforms()
        {
            return GetValidPlatforms(false);
        }
    }
}
