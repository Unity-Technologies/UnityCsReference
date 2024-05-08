// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Collections.Generic;
using DiscoveredTargetInfo = UnityEditor.BuildTargetDiscovery.DiscoveredTargetInfo;
using TargetAttributes = UnityEditor.BuildTargetDiscovery.TargetAttributes;

namespace UnityEditor.Build
{
    // All settings for a build platform.
    internal class BuildPlatform : ICloneable
    {
        // short name used for texture settings, etc.
        public string name;
        public NamedBuildTarget namedBuildTarget;
        public bool installed;
        public bool hideInUi;
        public string tooltip;
        public BuildTarget defaultTarget;

        // TODO: Some packages are still using targetGroup, so we keep it here as a getter for compatibility
        public BuildTargetGroup targetGroup => namedBuildTarget.ToBuildTargetGroup();

        ScalableGUIContent m_Title;
        ScalableGUIContent m_SmallTitle;

        public GUIContent title => m_Title;
        public Texture2D smallIcon => ((GUIContent)m_SmallTitle).image as Texture2D;

        public BuildPlatform(string locTitle, string iconId, NamedBuildTarget namedBuildTarget, BuildTarget defaultTarget, bool hideInUi, bool installed)
            : this(locTitle, "", iconId, namedBuildTarget, defaultTarget, hideInUi, installed)
        {
        }

        public BuildPlatform(string locTitle, string tooltip, string iconId, NamedBuildTarget namedBuildTarget, BuildTarget defaultTarget, bool hideInUi, bool installed)
        {
            this.namedBuildTarget = namedBuildTarget;
            name = namedBuildTarget.TargetName;

            // Workaround for some platforms which have | in their name which is also used as separator for tooltips
            if (locTitle.Contains("|"))      
                m_Title = new ScalableGUIContent(locTitle.Replace("|", " "), null, iconId);
            else
                m_Title = new ScalableGUIContent(locTitle, null, iconId);

            m_SmallTitle = new ScalableGUIContent(null, null, iconId + ".Small");
            this.tooltip = tooltip;
            this.hideInUi = hideInUi;
            this.defaultTarget = defaultTarget;
            this.installed = installed;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    internal class BuildPlatformWithSubtarget : BuildPlatform
    {
        public int subtarget;

        public BuildPlatformWithSubtarget(string locTitle, string tooltip, string iconId, NamedBuildTarget namedBuildTarget, BuildTarget defaultTarget, int subtarget, bool hideInUi, bool installed)
            : base(locTitle, tooltip, iconId, namedBuildTarget, defaultTarget, hideInUi, installed)
        {
            this.subtarget = subtarget;
            name = namedBuildTarget.TargetName;
        }
    }

    internal class BuildPlatforms
    {
        static readonly BuildPlatforms s_Instance = new BuildPlatforms();

        public static BuildPlatforms instance => s_Instance;

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

            buildPlatformsList.Add(new BuildPlatformWithSubtarget(BuildPipeline.GetBuildTargetGroupDisplayName(BuildTargetGroup.Standalone), "", "BuildSettings.Standalone",
                NamedBuildTarget.Standalone, standaloneTarget, (int)StandaloneBuildSubtarget.Player, false, true));

            // TODO: We should consider extend BuildTargetDiscovery to support named targets and subtargets,
            // specially if at some point other platforms use them.
            // The installed value is set by the linux, mac or win ExtensionModule.cs when they are loaded.
            buildPlatformsList.Add(new BuildPlatformWithSubtarget("Dedicated Server", "", "BuildSettings.DedicatedServer",
                NamedBuildTarget.Server, standaloneTarget, (int)StandaloneBuildSubtarget.Server, false, false));

            foreach (var target in buildTargets)
            {
                if (!target.HasFlag(TargetAttributes.IsStandalonePlatform))
                {
                    NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(target.buildTargetPlatformVal));
                    buildPlatformsList.Add(new BuildPlatform(
                        BuildPipeline.GetBuildTargetGroupDisplayName(namedBuildTarget.ToBuildTargetGroup()),
                        target.iconName,
                        namedBuildTarget,
                        target.buildTargetPlatformVal,
                        hideInUi: target.HasFlag(TargetAttributes.HideInUI),
                        installed: BuildPipeline.GetPlaybackEngineDirectory(target.buildTargetPlatformVal, BuildOptions.None, false) != string.Empty));
                }
            }

            foreach (var buildPlatform in buildPlatformsList)
            {
                buildPlatform.tooltip = buildPlatform.title.text + " settings";
            }

            buildPlatforms = buildPlatformsList.ToArray();
        }

        public BuildPlatform[] buildPlatforms;

        public string GetBuildTargetDisplayName(BuildTargetGroup buildTargetGroup, BuildTarget target, int subtarget)
        {
            if (buildTargetGroup == BuildTargetGroup.Standalone && subtarget == (int)StandaloneBuildSubtarget.Server)
                return GetBuildTargetDisplayName(NamedBuildTarget.Server, target);

            return GetBuildTargetDisplayName(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup), target);
        }

        public string GetBuildTargetDisplayName(NamedBuildTarget namedBuildTarget, BuildTarget target)
        {
            foreach (BuildPlatform cur in buildPlatforms)
            {
                if (cur.defaultTarget == target && cur.namedBuildTarget == namedBuildTarget)
                    return cur.title.text;
            }

#pragma warning disable CS0618 // Member is obsolete
            string targetName = BuildTargetDiscovery.BuildPlatformDisplayName(namedBuildTarget, target);
#pragma warning restore CS0618
            return targetName.Length == 0 ? "Unsupported Target" : targetName;
        }

        public string GetModuleDisplayName(NamedBuildTarget namedBuildTarget, BuildTarget buildTarget)
        {
            return GetBuildTargetDisplayName(namedBuildTarget, buildTarget);
        }

        int BuildPlatformIndexFromNamedBuildTarget(NamedBuildTarget target)
        {
            for (int i = 0; i < buildPlatforms.Length; i++)
                if (target == buildPlatforms[i].namedBuildTarget)
                    return i;
            return -1;
        }

        public BuildPlatform BuildPlatformFromNamedBuildTarget(NamedBuildTarget target)
        {
            int index = BuildPlatformIndexFromNamedBuildTarget(target);
            return index != -1 ? buildPlatforms[index] : null;
        }

        public List<BuildPlatform> GetValidPlatforms(bool includeMetaPlatforms)
        {
            List<BuildPlatform> platforms = new List<BuildPlatform>();
            foreach (BuildPlatform bp in buildPlatforms)
                if (bp.namedBuildTarget == NamedBuildTarget.Standalone ||
                    (bp.installed && BuildPipeline.IsBuildPlatformSupported(bp.defaultTarget)))
                    platforms.Add(bp);

            return platforms;
        }

        public List<BuildPlatform> GetValidPlatforms()
        {
            return GetValidPlatforms(false);
        }
    }
}
