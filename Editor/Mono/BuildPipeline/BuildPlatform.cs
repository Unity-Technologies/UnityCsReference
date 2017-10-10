// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

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

        public BuildPlatform(string locTitle, string iconId, BuildTargetGroup targetGroup, bool forceShowTarget)
            : this(locTitle, "", iconId, targetGroup, forceShowTarget)
        {
        }

        public BuildPlatform(string locTitle, string tooltip, string iconId, BuildTargetGroup targetGroup, bool forceShowTarget)
        {
            this.targetGroup = targetGroup;
            name = targetGroup != BuildTargetGroup.Unknown ? BuildPipeline.GetBuildTargetGroupName(defaultTarget) : "";
            title = EditorGUIUtility.TextContentWithIcon(locTitle, iconId);
            smallIcon = EditorGUIUtility.IconContent(iconId + ".Small").image as Texture2D;
            this.tooltip = tooltip;
            this.forceShowTarget = forceShowTarget;
        }

        // ADD_NEW_PLATFORM_HERE
        public BuildTarget defaultTarget
        {
            get
            {
                switch (targetGroup)
                {
                    case BuildTargetGroup.Standalone:
                        return BuildTarget.StandaloneWindows;
                    case BuildTargetGroup.iOS:
                        return BuildTarget.iOS;
                    case BuildTargetGroup.tvOS:
                        return BuildTarget.tvOS;
                    case BuildTargetGroup.PSP2:
                        return BuildTarget.PSP2;
                    case BuildTargetGroup.PS4:
                        return BuildTarget.PS4;
                    case BuildTargetGroup.XboxOne:
                        return BuildTarget.XboxOne;
                    case BuildTargetGroup.Android:
                        return BuildTarget.Android;
                    case BuildTargetGroup.N3DS:
                        return BuildTarget.N3DS;
                    case BuildTargetGroup.Switch:
                        return BuildTarget.Switch;
                    case BuildTargetGroup.Tizen:
                        return BuildTarget.Tizen;
                    case BuildTargetGroup.WiiU:
                        return BuildTarget.WiiU;
                    case BuildTargetGroup.WebGL:
                        return BuildTarget.WebGL;
                    case BuildTargetGroup.WSA:
                        return BuildTarget.WSAPlayer;
                    case BuildTargetGroup.Facebook:
                        return BuildTarget.StandaloneWindows64;
                    default:
                        return (BuildTarget)(-1);
                }
            }
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
            // This is pretty brittle, notLicensedMessages and buildTargetNotInstalled below must match the order here
            // and since NaCl isn't listed in the build settings like the other platforms you must not add anything after it, if it
            // must also be added in the license/notinstalled arrays.
            // ADD_NEW_PLATFORM_HERE
            List<BuildPlatform> buildPlatformsList = new List<BuildPlatform>();
            buildPlatformsList.Add(new BuildPlatform("PC, Mac & Linux Standalone", "BuildSettings.Standalone", BuildTargetGroup.Standalone, true));
            buildPlatformsList.Add(new BuildPlatform("iOS", "BuildSettings.iPhone", BuildTargetGroup.iOS, true));
            // TVOS TODO change the icon when it's ready
            buildPlatformsList.Add(new BuildPlatform("tvOS", "BuildSettings.tvOS", BuildTargetGroup.tvOS, true));
            buildPlatformsList.Add(new BuildPlatform("Android", "BuildSettings.Android", BuildTargetGroup.Android, true));
            buildPlatformsList.Add(new BuildPlatform("Tizen", "BuildSettings.Tizen", BuildTargetGroup.Tizen, false));
            buildPlatformsList.Add(new BuildPlatform("Xbox One", "BuildSettings.XboxOne", BuildTargetGroup.XboxOne, true));
            buildPlatformsList.Add(new BuildPlatform("PS Vita", "BuildSettings.PSP2", BuildTargetGroup.PSP2, true));
            buildPlatformsList.Add(new BuildPlatform("PS4", "BuildSettings.PS4", BuildTargetGroup.PS4, true));
            buildPlatformsList.Add(new BuildPlatform("Wii U", "BuildSettings.WiiU", BuildTargetGroup.WiiU, false));
            buildPlatformsList.Add(new BuildPlatform("Universal Windows Platform", "BuildSettings.Metro", BuildTargetGroup.WSA, true));
            buildPlatformsList.Add(new BuildPlatform("WebGL", "BuildSettings.WebGL", BuildTargetGroup.WebGL, true));
            buildPlatformsList.Add(new BuildPlatform("Nintendo 3DS", "BuildSettings.N3DS", BuildTargetGroup.N3DS, false));
            buildPlatformsList.Add(new BuildPlatform("Facebook", "BuildSettings.Facebook", BuildTargetGroup.Facebook, true));
            buildPlatformsList.Add(new BuildPlatform("Nintendo Switch", "BuildSettings.Switch", BuildTargetGroup.Switch, false));

            foreach (var buildPlatform in buildPlatformsList)
            {
                buildPlatform.tooltip = BuildPipeline.GetBuildTargetGroupDisplayName(buildPlatform.targetGroup) + " settings";
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

        public int BuildPlatformIndexFromTargetGroup(BuildTargetGroup group)
        {
            for (int i = 0; i < buildPlatforms.Length; i++)
                if (group == buildPlatforms[i].targetGroup)
                    return i;
            return -1;
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
