// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using UnityEditor.Modules;
using UnityEngine;

namespace UnityEditor
{
    internal class DesktopPluginImporterExtension : DefaultPluginImporterExtension
    {
        internal enum DesktopPluginCPUArchitecture
        {
            None,
            AnyCPU,
            x86,
            x86_64,
            ARM64
        }

        internal class DesktopSingleCPUProperty : Property
        {
            public DesktopSingleCPUProperty(BuildTarget buildTarget, DesktopPluginCPUArchitecture architecture)
                : base(EditorGUIUtility.TrTextContent(GetArchitectureNameInGUI(buildTarget, architecture)), cpuKey, architecture, BuildPipeline.GetBuildTargetName(buildTarget))
            {
            }

            public DesktopSingleCPUProperty(GUIContent name, BuildTarget buildTarget)
                : base(name, cpuKey, DesktopPluginCPUArchitecture.AnyCPU, BuildPipeline.GetBuildTargetName(buildTarget))
            {
            }

            internal bool IsTargetEnabled(PluginImporterInspector inspector)
            {
                PluginImporterInspector.Compatibility compatibililty = inspector.GetPlatformCompatibility(platformName);
                if (compatibililty == PluginImporterInspector.Compatibility.Mixed)
                    throw new Exception("Unexpected mixed value for '" + inspector.importer.assetPath + "', platform: " + platformName);
                if (compatibililty != PluginImporterInspector.Compatibility.Compatible)
                    return false;

                var pluginCPU = value as DesktopPluginCPUArchitecture ? ?? DesktopPluginCPUArchitecture.None;
                return pluginCPU == (DesktopPluginCPUArchitecture)defaultValue || pluginCPU == DesktopPluginCPUArchitecture.AnyCPU;
            }

            internal override void OnGUI(PluginImporterInspector inspector)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);
                EditorGUI.BeginChangeCheck();

                // This toggle controls two things:
                // * Is platform enabled/disabled?
                // * Platform CPU value
                bool isTargetEnabled = EditorGUILayout.Toggle(name, IsTargetEnabled(inspector));
                if (EditorGUI.EndChangeCheck())
                {
                    value = isTargetEnabled ? defaultValue : DesktopPluginCPUArchitecture.None;
                    inspector.SetPlatformCompatibility(platformName, isTargetEnabled);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        class DesktopMultiCPUProperty : Property
        {
            private readonly DesktopPluginCPUArchitecture[] m_SupportedArchitectures;
            private readonly GUIContent[] m_SupportedArchitectureNames;

            public DesktopMultiCPUProperty(BuildTarget buildTarget, params DesktopPluginCPUArchitecture[] supportedArchitectures) :
                base(cpuKey, cpuKey, DesktopPluginCPUArchitecture.None, BuildPipeline.GetBuildTargetName(buildTarget))
            {
                // Add "None" and "AnyCPU" architectures to the supported architecture list
                m_SupportedArchitectures = new DesktopPluginCPUArchitecture[supportedArchitectures.Length + 2];
                m_SupportedArchitectures[0] = DesktopPluginCPUArchitecture.None;

                var architectureCount = supportedArchitectures.Length;
                for (int i = 0; i < architectureCount; i++)
                    m_SupportedArchitectures[i + 1] = supportedArchitectures[i];

                m_SupportedArchitectures[m_SupportedArchitectures.Length - 1] = DesktopPluginCPUArchitecture.AnyCPU;

                architectureCount = m_SupportedArchitectures.Length;
                m_SupportedArchitectureNames = new GUIContent[architectureCount];
                for (int i = 0; i < architectureCount; i++)
                    m_SupportedArchitectureNames[i] = EditorGUIUtility.TrTextContent(GetArchitectureNameInGUI(buildTarget, m_SupportedArchitectures[i]));
            }

            DesktopPluginCPUArchitecture GetCurrentArchitecture(PluginImporterInspector inspector)
            {
                if (inspector.GetPlatformCompatibility(platformName) != PluginImporterInspector.Compatibility.Compatible)
                    return DesktopPluginCPUArchitecture.None;

                // Previous Unity versions had only two states: enabled or disabled. If it was enabled, then it means
                // it was compatible with x64 as that was the only architecture available at the time.
                var architecture = value as DesktopPluginCPUArchitecture ? ;
                if (architecture == null || architecture == DesktopPluginCPUArchitecture.None)
                    return DesktopPluginCPUArchitecture.x86_64;

                return architecture.Value;
            }

            int GetArchitectureIndex(DesktopPluginCPUArchitecture architecture)
            {
                for (int i = 0; i < m_SupportedArchitectures.Length; i++)
                {
                    if (architecture == m_SupportedArchitectures[i])
                        return i;
                }

                if (architecture == DesktopPluginCPUArchitecture.None)
                    throw new InvalidOperationException("Supported architectures did not contain DesktopPluginCPUArchitecture.None!");

                // If current architecture is something that's not in supported list, we treat it as the plugin is set to "No architecture".
                return GetArchitectureIndex(DesktopPluginCPUArchitecture.None);
            }

            internal override void OnGUI(PluginImporterInspector inspector)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);
                EditorGUI.BeginChangeCheck();

                int selectedIndex = GetArchitectureIndex(GetCurrentArchitecture(inspector));
                selectedIndex = EditorGUILayout.Popup(name, selectedIndex, m_SupportedArchitectureNames);

                if (EditorGUI.EndChangeCheck())
                {
                    value = m_SupportedArchitectures[selectedIndex];
                    inspector.SetPlatformCompatibility(platformName, m_SupportedArchitectures[selectedIndex] != DesktopPluginCPUArchitecture.None);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        // Windows has 2 build targets. One build target for 32bit that supports 1 CPU architectue. The other for 64 bit that supports multiple 64 bit architectures (x64 and ARM64).
        private readonly DesktopSingleCPUProperty m_Windows32;
        private readonly DesktopMultiCPUProperty m_Windows64;

        // Linux has 1 build target and 1 supported CPU architecture
        private readonly DesktopSingleCPUProperty m_Linux;

        // macOS has multiple architectures, but one target.
        private readonly DesktopMultiCPUProperty m_MacOS;

        // For Managed plugins the only options for CPU architecture should be "None" or "AnyCPU", so it can be DesktopSingleCPUProperty
        private readonly DesktopSingleCPUProperty m_Windows32Managed;
        private readonly DesktopSingleCPUProperty m_Windows64Managed;
        private readonly DesktopSingleCPUProperty m_LinuxManaged;
        private readonly DesktopSingleCPUProperty m_MacOSManaged;

        public DesktopPluginImporterExtension()
            : base(null)
        {
            m_Windows32 = new DesktopSingleCPUProperty(BuildTarget.StandaloneWindows, DesktopPluginCPUArchitecture.x86);
            m_Windows64 = new DesktopMultiCPUProperty(BuildTarget.StandaloneWindows64, DesktopPluginCPUArchitecture.x86_64, DesktopPluginCPUArchitecture.ARM64);

            m_Linux = new DesktopSingleCPUProperty(BuildTarget.StandaloneLinux64, DesktopPluginCPUArchitecture.x86_64);
            m_MacOS = new DesktopMultiCPUProperty(BuildTarget.StandaloneOSX, DesktopPluginCPUArchitecture.x86_64, DesktopPluginCPUArchitecture.ARM64);

            // Windows 32-bit (x86) and Windows 64-bit (ARM64/x86_64) are separate targets, so they have separate checkboxes
            // Linux only has x64 architecture and Mac has a single target for both 64-bit architectures (ARM64/Intel-64bit)
            m_Windows32Managed = new DesktopSingleCPUProperty(EditorGUIUtility.TrTextContent("Windows x86"), BuildTarget.StandaloneWindows);
            m_Windows64Managed = new DesktopSingleCPUProperty(EditorGUIUtility.TrTextContent("Windows 64-bit"), BuildTarget.StandaloneWindows64);
            m_LinuxManaged = new DesktopSingleCPUProperty(EditorGUIUtility.TrTextContent("Linux x64"),BuildTarget.StandaloneLinux64);
            m_MacOSManaged = new DesktopSingleCPUProperty(EditorGUIUtility.TrTextContent("macOS 64-bit"),BuildTarget.StandaloneOSX);

            properties = new Property[]
            {
                m_Windows32,
                m_Windows64,
                m_Linux,
                m_MacOS,
                m_Windows32Managed,
                m_Windows64Managed,
                m_LinuxManaged,
                m_MacOSManaged
            };
        }

        private bool IsUsableOnWindows(PluginImporter imp)
        {
            if (!imp.isNativePlugin)
                return true;

            string ext = Path.GetExtension(imp.assetPath).ToLower();
            return ext == ".dll" || IsCppPluginFile(imp.assetPath);
        }

        private bool IsUsableOnOSX(PluginImporter imp)
        {
            if (!imp.isNativePlugin)
                return true;

            string ext = FileUtil.GetPathExtension(imp.assetPath).ToLower();
            return ext == "bundle" || ext == "dylib" || IsLinuxLibrary(imp.assetPath) || IsCppPluginFile(imp.assetPath);
        }

        private bool IsUsableOnLinux(PluginImporter imp)
        {
            if (!imp.isNativePlugin)
                return true;

            return IsLinuxLibrary(imp.assetPath) || IsCppPluginFile(imp.assetPath);
        }

        public override void OnPlatformSettingsGUI(PluginImporterInspector inspector)
        {
            PluginImporter imp = inspector.importer;
            EditorGUI.BeginChangeCheck();
            // skip CPU property for things that aren't native libs
            if (imp.isNativePlugin)
            {
                if (IsUsableOnWindows(imp))
                {
                    EditorGUILayout.LabelField(EditorGUIUtility.TrTextContent("Windows"), EditorStyles.boldLabel);
                    m_Windows32.OnGUI(inspector);
                    m_Windows64.OnGUI(inspector);
                    EditorGUILayout.Space();
                }

                if (IsUsableOnLinux(imp))
                {
                    EditorGUILayout.LabelField(EditorGUIUtility.TrTextContent("Linux"), EditorStyles.boldLabel);
                    m_Linux.OnGUI(inspector);
                    EditorGUILayout.Space();
                }

                if (IsUsableOnOSX(imp))
                {
                    EditorGUILayout.LabelField(EditorGUIUtility.TrTextContent("macOS"), EditorStyles.boldLabel);
                    m_MacOS.OnGUI(inspector);
                    EditorGUILayout.Space();
                }
            }
            else
            {
                // Managed plugins are usable on all platforms
                m_Windows32Managed.OnGUI(inspector);
                m_Windows64Managed.OnGUI(inspector);
                EditorGUILayout.Space();

                m_LinuxManaged.OnGUI(inspector);
                EditorGUILayout.Space();

                m_MacOSManaged.OnGUI(inspector);
                EditorGUILayout.Space();
            }

            if (EditorGUI.EndChangeCheck())
                hasModified = true;
        }

        public void ValidateSingleCPUTargets(PluginImporterInspector inspector)
        {
            var singleCPUTargets = properties.OfType<DesktopSingleCPUProperty>();

            foreach (var target in singleCPUTargets)
            {
                target.value = target.IsTargetEnabled(inspector) ? target.defaultValue : DesktopPluginCPUArchitecture.None;

                foreach (var importer in inspector.importers)
                {
                    importer.SetPlatformData(target.platformName, cpuKey, target.value.ToString());
                }
            }
        }

        public override string CalculateFinalPluginPath(string platformName, PluginImporter imp)
        {
            BuildTarget target = BuildPipeline.GetBuildTargetByName(platformName);
            bool pluginForWindows = target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64;
#pragma warning disable 612, 618
            bool pluginForOSX = target == BuildTarget.StandaloneOSXIntel || target == BuildTarget.StandaloneOSXIntel64 || target == BuildTarget.StandaloneOSX;
            bool pluginForLinux = target == BuildTarget.StandaloneLinux || target == BuildTarget.StandaloneLinux64 || target == BuildTarget.StandaloneLinuxUniversal || target == BuildTarget.LinuxHeadlessSimulation;
#pragma warning restore 612, 618

            if (!pluginForLinux && !pluginForOSX && !pluginForWindows)
                throw new Exception(string.Format("Failed to resolve standalone platform, platform string '{0}', resolved target '{1}'",
                    platformName, target.ToString()));

            if (pluginForWindows && !IsUsableOnWindows(imp))
                return string.Empty;
            if (pluginForOSX && !IsUsableOnOSX(imp))
                return string.Empty;
            if (pluginForLinux && !IsUsableOnLinux(imp))
                return string.Empty;

            string cpu = imp.GetPlatformData(platformName, cpuKey);

            if (string.Compare(cpu, "None", true) == 0)
                return string.Empty;

            if (pluginForWindows)
            {
                if (string.Compare(cpu, nameof(DesktopPluginCPUArchitecture.ARM64), true) == 0)
                {
                    return Path.Combine(cpu, Path.GetFileName(imp.assetPath));
                }

                // Fix case 1185926: plugins for x86_64 are supposed to be copied to Plugins/x86_64
                // Plugins for x86 are supposed to be copied to Plugins/x86
                var cpuName = target == BuildTarget.StandaloneWindows ? nameof(DesktopPluginCPUArchitecture.x86) : nameof(DesktopPluginCPUArchitecture.x86_64);
                return Path.Combine(cpuName, Path.GetFileName(imp.assetPath));
            }

            if (pluginForOSX)
            {
                // Add the correct architecture if not AnyCPU
                return base.CalculateFinalPluginPath(platformName, imp);
            }

            // For files this will return filename, for directories, this will return last path component
            return Path.GetFileName(imp.assetPath);
        }

        // Regex that matchers strings ending in ".so" or ".so.12" or ".so.4.7" and so on.
        private static Regex LinuxLibraryRegex = new Regex(@"\.so(\.[0-9]+)*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        internal static bool IsLinuxLibrary(string assetPath)
        {
            return LinuxLibraryRegex.IsMatch(assetPath);
        }

        internal static bool IsCppPluginFile(string assetPath)
        {
            var extension = Path.GetExtension(assetPath).ToLower();
            return extension == ".cpp" || extension == ".c" || extension == ".h" || extension == ".mm" || extension == ".m";
        }

        static string GetArchitectureNameInGUI(BuildTarget buildTarget, DesktopPluginCPUArchitecture architecture)
        {
            switch (architecture)
            {
                case DesktopPluginCPUArchitecture.None :
                    return "None";

                case DesktopPluginCPUArchitecture.x86 :
                    return buildTarget == BuildTarget.StandaloneOSX ? "Intel 32-bit" : "x86";

                case DesktopPluginCPUArchitecture.x86_64:
                    return buildTarget == BuildTarget.StandaloneOSX ? "Intel 64-bit" : "x64";

                case DesktopPluginCPUArchitecture.ARM64:
                    return buildTarget == BuildTarget.StandaloneOSX ? "Apple silicon" : "ARM64";

                case DesktopPluginCPUArchitecture.AnyCPU:
                    return "Any CPU";

                default:
                    throw new NotSupportedException("Unknown DesktopPluginCPUArchitecture value: " + architecture);
            }
        }
    }
}
