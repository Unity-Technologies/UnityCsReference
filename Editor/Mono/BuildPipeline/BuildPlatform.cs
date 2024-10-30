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

        string m_LocTitle;
        string m_IconId;

        ScalableGUIContent m_Title;
        ScalableGUIContent m_SmallTitle;

        Texture2D m_CompoundSmallIcon;
        Texture2D m_CompoundSmallIconForQualitySettings;
        string m_CompoundTooltip;
        ScalableGUIContent m_CompoundTitle;

        IEnumerable<BuildPlatform> m_DerivedPlatforms;

        public GUIContent title => m_CompoundTitle ?? m_Title;
        public Texture2D smallIcon => ((GUIContent)m_SmallTitle).image as Texture2D;

        public Texture2D compoundSmallIcon => GetCompoundSmallIcon();
        public Texture2D compoundSmallIconForQualitySettings => GetCompoundSmallIconForQualitySettings();
        public string compoundTooltip => m_CompoundTooltip ?? tooltip;

        public BuildPlatform(string locTitle, string iconId, NamedBuildTarget namedBuildTarget, BuildTarget defaultTarget, bool hideInUi, bool installed)
            : this(locTitle, "", iconId, namedBuildTarget, defaultTarget, hideInUi, installed)
        {
        }

        public BuildPlatform(string locTitle, string tooltip, string iconId, NamedBuildTarget namedBuildTarget, BuildTarget defaultTarget, bool hideInUi, bool installed)
        {
            this.namedBuildTarget = namedBuildTarget;
            name = namedBuildTarget.TargetName;

            m_IconId = iconId;
            m_LocTitle = locTitle;
            m_Title = CreateTitle(locTitle, iconId);

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

        Texture2D GetCompoundSmallIcon()
        {
            GenerateCompoundData();
            return m_CompoundSmallIcon ?? smallIcon;
        }

        Texture2D GetCompoundSmallIconForQualitySettings()
        {
            GenerateCompoundData();
            return m_CompoundSmallIconForQualitySettings ?? smallIcon;
        }

        void GenerateCompoundData()
        {
            if (m_DerivedPlatforms != null)
            {
                GenerateCompoundTooltip(m_DerivedPlatforms);
                GenerateCompoundTitle(m_DerivedPlatforms);
                GenerateCompoundIconTexture(m_DerivedPlatforms);
                m_DerivedPlatforms = null;
            }
        }

        internal void SetDerivedPlatforms(IEnumerable<BuildPlatform> derivedPlatforms)
        {
            m_DerivedPlatforms = derivedPlatforms;
        }

        static ScalableGUIContent CreateTitle(string locTitle, string iconId)
        {
            // Workaround for some platforms which have | in their name which is also used as separator for tooltips
            const string TooltipSeparator = "|";
            if (locTitle.Contains(TooltipSeparator))
                return new ScalableGUIContent(locTitle.Replace(TooltipSeparator, " "), null, iconId);
            else
                return new ScalableGUIContent(locTitle, null, iconId);
        }

        void GenerateCompoundIconTexture(IEnumerable<BuildPlatform> derivedPlatforms)
        {
            static Texture2D DuplicateAsReadableTexture(Texture2D sourceTexture)
            {
                var renderTexture = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height,
                    0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                Graphics.Blit(sourceTexture, renderTexture);
                var previouslyActiveRenderTexture = RenderTexture.active;
                RenderTexture.active = renderTexture;
                var readableTexture = new Texture2D(sourceTexture.width, sourceTexture.height);
                readableTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                readableTexture.Apply();
                RenderTexture.active = previouslyActiveRenderTexture;
                RenderTexture.ReleaseTemporary(renderTexture);
                return readableTexture;
            }

            static void ClearTexture(Texture2D texture)
            {
                Color TransparentBlack = new(0, 0, 0, 0);
                for (int y = 0; y < texture.height; y++)
                {
                    for (int x = 0; x < texture.width; x++)
                    {
                        texture.SetPixel(x, y, TransparentBlack);
                    }
                }
            }

            var readableSmallIcon = DuplicateAsReadableTexture(smallIcon);
            var qualityIconWidth = readableSmallIcon.width;
            var qualityIconHeight = readableSmallIcon.height;
            var compoundIconWidth = readableSmallIcon.width;
            var compoundIconHeight = readableSmallIcon.height;
            var horizontalGapSize = (int)Math.Round(4 * readableSmallIcon.pixelsPerPoint);
            var verticalGapSize = (int)Math.Round(2 * readableSmallIcon.pixelsPerPoint);
            foreach (var derivedPlatform in derivedPlatforms)
            {
                var derivedSmallIcon = derivedPlatform.smallIcon;
                compoundIconWidth += horizontalGapSize;
                qualityIconHeight += verticalGapSize;
                compoundIconHeight = Math.Max(compoundIconHeight, derivedSmallIcon.height);
                compoundIconWidth += derivedSmallIcon.width;
                qualityIconHeight += derivedSmallIcon.height;
            }
            var qualityTexture = new Texture2D(qualityIconWidth, qualityIconHeight, readableSmallIcon.format, false);
            var texture = new Texture2D(compoundIconWidth, compoundIconHeight, readableSmallIcon.format, false);
            ClearTexture(qualityTexture);
            ClearTexture(texture);
            var compoundIconXPosition = 0;
            var qualityIconYPosition = qualityIconHeight - readableSmallIcon.height;
            texture.CopyPixels(readableSmallIcon, 0, 0, 0, 0, readableSmallIcon.width, readableSmallIcon.height, 0, compoundIconXPosition, 0);
            qualityTexture.CopyPixels(readableSmallIcon, 0, 0, 0, 0, readableSmallIcon.width, readableSmallIcon.height, 0, 0, qualityIconYPosition);
            compoundIconXPosition += readableSmallIcon.width;
            qualityIconYPosition -= verticalGapSize;
            foreach (var derivedPlatform in derivedPlatforms)
            {
                var readableDerivedSmallIcon = DuplicateAsReadableTexture(derivedPlatform.smallIcon);
                compoundIconXPosition += horizontalGapSize;
                qualityIconYPosition -= readableDerivedSmallIcon.height;
                texture.CopyPixels(readableDerivedSmallIcon, 0, 0, 0, 0, readableDerivedSmallIcon.width, readableDerivedSmallIcon.height, 0, compoundIconXPosition, 0);
                qualityTexture.CopyPixels(readableDerivedSmallIcon, 0, 0, 0, 0, readableDerivedSmallIcon.width, readableDerivedSmallIcon.height, 0, 0, qualityIconYPosition);
                compoundIconXPosition += readableDerivedSmallIcon.width;
                qualityIconYPosition -= verticalGapSize;
            }
            texture.pixelsPerPoint = readableSmallIcon.pixelsPerPoint;
            texture.Apply();
            qualityTexture.pixelsPerPoint = readableSmallIcon.pixelsPerPoint;
            qualityTexture.Apply();
            m_CompoundSmallIcon = texture;
            m_CompoundSmallIconForQualitySettings = qualityTexture;
        }

        void GenerateCompoundTooltip(IEnumerable<BuildPlatform> derivedPlatforms)
        {
            var ttip = m_LocTitle;
            foreach (var derivedPlatform in derivedPlatforms)
            {
                ttip += $", {derivedPlatform.m_LocTitle}";
            }
            ttip += " settings";
            m_CompoundTooltip = ttip;
        }

        void GenerateCompoundTitle(IEnumerable<BuildPlatform> derivedPlatforms)
        {
            var title = m_LocTitle;
            foreach (var derivedPlatform in derivedPlatforms)
            {
                title += $", {derivedPlatform.m_LocTitle}";
            }
            m_CompoundTitle = CreateTitle(title, m_IconId);
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
                    var buildPlatform = new BuildPlatform(
                        BuildPipeline.GetBuildTargetGroupDisplayName(namedBuildTarget.ToBuildTargetGroup()),
                        target.iconName,
                        namedBuildTarget,
                        target.buildTargetPlatformVal,
                        hideInUi: target.HasFlag(TargetAttributes.HideInUI),
                        installed: BuildPipeline.GetPlaybackEngineDirectory(target.buildTargetPlatformVal, BuildOptions.None, false) != string.Empty);
                    buildPlatformsList.Add(buildPlatform);
                    var derivedBuildTargets = BuildTargetDiscovery.GetDerivedBuildTargetInfoList(target.buildTargetPlatformVal);
                    if (derivedBuildTargets.Length > 0)
                    {
                        var derivedBuildPlatforms = new List<BuildPlatform>();
                        foreach (var derivedTarget in derivedBuildTargets)
                        {
                            derivedBuildPlatforms.Add(new BuildPlatform(
                                derivedTarget.niceName,
                                derivedTarget.iconName,
                                namedBuildTarget,
                                target.buildTargetPlatformVal,
                                hideInUi: target.HasFlag(TargetAttributes.HideInUI),
                                installed: BuildPipeline.GetPlaybackEngineDirectory(target.buildTargetPlatformVal, BuildOptions.None, false) != string.Empty));
                        }
                        buildPlatform.SetDerivedPlatforms(derivedBuildPlatforms);
                    }
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
