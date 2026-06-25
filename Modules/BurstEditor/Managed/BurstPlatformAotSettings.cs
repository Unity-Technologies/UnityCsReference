// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Burst.Editor
{
    internal enum DebugDataKind
    {
        LineOnly,
        Full
    }

    internal enum AvailX86Targets
    {
        SSE2 = (int)BurstTargetCpu.X86_SSE2,
        SSE4 = (int)BurstTargetCpu.X86_SSE4,
    }

    [Flags]
    internal enum BitsetX86Targets
    {
        SSE2 = 1 << AvailX86Targets.SSE2,
        SSE4 = 1 << AvailX86Targets.SSE4,
    }

    internal enum AvailX64Targets
    {
        SSE2 = (int)BurstTargetCpu.X64_SSE2,
        SSE4 = (int)BurstTargetCpu.X64_SSE4,
        AVX = (int)BurstTargetCpu.AVX,
        AVX2 = (int)BurstTargetCpu.AVX2,
    }

    [Flags]
    internal enum BitsetX64Targets
    {
        SSE2 = 1 << AvailX64Targets.SSE2,
        SSE4 = 1 << AvailX64Targets.SSE4,
        AVX = 1 << AvailX64Targets.AVX,
        AVX2 = 1 << AvailX64Targets.AVX2,
    }

    internal enum AvailArm64Targets
    {
        ARMV8A = BurstTargetCpu.ARMV8A_AARCH64,
        ARMV8A_HALFFP = BurstTargetCpu.ARMV8A_AARCH64_HALFFP,
        ARMV9A = BurstTargetCpu.ARMV9A,
    }

    [Flags]
    internal enum BitsetArm64Targets
    {
        ARMV8A = 1 << AvailArm64Targets.ARMV8A,
        ARMV8A_HALFFP = 1 << AvailArm64Targets.ARMV8A_HALFFP,
        ARMV9A = 1 << AvailArm64Targets.ARMV9A,
    }

    internal enum StackProtector
    {
        Off,
        Basic,
        Strong,
        All,
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal class BurstMetadataSettingAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field)]
    internal class BurstCommonSettingAttribute : Attribute {}

    class BurstPlatformLegacySettings : ScriptableObject
    {
        [SerializeField]
        internal bool DisableOptimisations;
        [SerializeField]
        internal bool DisableSafetyChecks;
        [SerializeField]
        internal bool DisableBurstCompilation;

        BurstPlatformLegacySettings(BuildTarget target)
        {
            DisableSafetyChecks = true;
            DisableBurstCompilation = false;
            DisableOptimisations = false;
        }
    }

    // To add a setting,
    //  Add a
    //          [SerializeField] internal type settingname;
    //  Add a
    //          internal static readonly string settingname_DisplayName = "Name of option to be displayed in the editor (and searched for)";
    //  Add a
    //          internal static readonly string settingname_ToolTip = "tool tip information to display when hovering mouse
    // If the setting should be restricted to e.g. Standalone platform :
    //
    //  Add a
    //          internal static bool settingname_Display(BuildTarget selectedTarget, string architecture) {}
    //
    //  Add a
    //          internal static bool settingname_Serialise(BuildTarget selectedTarget) {}
    class BurstPlatformAotSettings : ScriptableObject
    {
        [SerializeField]
        [BurstMetadataSetting] // We always need version to be in our saved settings!
        internal int Version;
        [SerializeField]
        internal bool EnableBurstCompilation;
        [SerializeField]
        internal bool EnableOptimisations;
        [SerializeField]
        internal bool EnableSafetyChecks;
        [SerializeField]
        internal bool EnableDebugInAllBuilds;
        [SerializeField]
        internal DebugDataKind DebugDataKind;
        [SerializeField]
        internal bool UsePlatformSDKLinker;
        [SerializeField]
        internal bool EnableArmv9SecurityFeatures;
        [SerializeField]
        internal AvailX86Targets CpuMinTargetX32;
        [SerializeField]
        internal AvailX86Targets CpuMaxTargetX32;
        [SerializeField]
        internal AvailX64Targets CpuMinTargetX64;
        [SerializeField]
        internal AvailX64Targets CpuMaxTargetX64;
        [SerializeField]
        internal BitsetX86Targets CpuTargetsX32;
        [SerializeField]
        internal BitsetX64Targets CpuTargetsX64;
        [SerializeField]
        internal BitsetArm64Targets CpuTargetsArm64;
        [SerializeField]
        internal OptimizeFor OptimizeFor;
        [SerializeField]
        internal FloatMode FloatMode;
        [SerializeField]
        [BurstCommonSetting]
        internal string DisabledWarnings;
        [SerializeField]
        internal StackProtector StackProtector;
        [SerializeField]
        internal uint StackProtectorBufferSize;

        internal static readonly string EnableDebugInAllBuilds_DisplayName = "Force Debug Information";
        internal static readonly string EnableDebugInAllBuilds_ToolTip = "Generates debug information for the Burst-compiled code, irrespective of if Development Mode is ticked. This can be used to generate symbols for release builds for platforms that need it.";

        internal static readonly string DebugDataKind_DisplayName = "Debug Information Kind";
        internal static readonly string DebugDataKind_ToolTip = "Choose which kind of debug information you want present in builds with debug information enabled.";

        internal static readonly string EnableOptimisations_DisplayName = "Enable Optimizations";
        internal static readonly string EnableOptimisations_ToolTip = "Enables all optimizations for the currently selected platform.";

        internal static readonly string EnableBurstCompilation_DisplayName = "Enable Burst Compilation";
        internal static readonly string EnableBurstCompilation_ToolTip = "Enables burst compilation for the selected platform.";

        internal static readonly string OptimizeFor_DisplayName = "Optimize For";
        internal static readonly string OptimizeFor_ToolTip = "Choose what optimization setting to compile Burst code for.";

        internal static readonly string FloatMode_DisplayName = "Floating Point Mode";
        internal static readonly string FloatMode_ToolTip = "Choose what floating point mode to compile Burst code for.";

        internal static readonly string DisabledWarnings_DisplayName = "Disabled Warnings*";
        internal static readonly string DisabledWarnings_ToolTip = "Burst warnings to disable (separated by ;).";

        internal static readonly string UsePlatformSDKLinker_DisplayName = "Use Platform SDK Linker";
        internal static readonly string UsePlatformSDKLinker_ToolTip = "Enabling this option will disable cross compilation support for desktops, and will require platform specific tools for Windows/Linux/Mac - use only if you encounter problems with the burst builtin solution.";

        // We do not support this option anymore, so the easiest thing is to just not display it.
        internal static bool UsePlatformSDKLinker_Display(BuildTarget selectedTarget, string architecture) => false;
        internal static bool UsePlatformSDKLinker_Serialise(BuildTarget selectedTarget) => false;

        internal static readonly string CpuTargetsX32_DisplayName = "Target 32Bit CPU Architectures";
        internal static readonly string CpuTargetsX32_ToolTip = "Use this to specify the set of target architectures to support for the currently selected platform.";
        internal static bool CpuTargetsX32_Display(BuildTarget selectedTarget, string architecture)
        {
            return (IsStandalone(selectedTarget) || selectedTarget == BuildTarget.WSAPlayer) && Has32BitSupport(selectedTarget);
        }
        internal static bool CpuTargetsX32_Serialise(BuildTarget selectedTarget)
        {
            return (IsStandalone(selectedTarget) || selectedTarget == BuildTarget.WSAPlayer) && Has32BitSupportForSerialise(selectedTarget);
        }

        internal static readonly string CpuTargetsX64_DisplayName = "Target 64Bit CPU Architectures";
        internal static readonly string CpuTargetsX64_ToolTip = "Use this to specify the target architectures to support for the currently selected platform.";
        internal static bool CpuTargetsX64_Display(BuildTarget selectedTarget, string architecture)
        {
            return (IsStandalone(selectedTarget) || selectedTarget == BuildTarget.WSAPlayer)
                && Has64BitSupport(selectedTarget)
                && architecture != "arm64";
        }
        internal static bool CpuTargetsX64_Serialise(BuildTarget selectedTarget)
        {
            return IsStandalone(selectedTarget) || selectedTarget == BuildTarget.WSAPlayer;
        }

        internal static readonly string CpuTargetsArm64_DisplayName = "Target Arm 64Bit CPU Architectures";
        internal static readonly string CpuTargetsArm64_ToolTip = "Use this to specify the target architectures to support for the currently selected platform.";
        internal static bool CpuTargetsArm64_Display(BuildTarget selectedTarget, string architecture)
        {
            return selectedTarget == BuildTarget.Android;
        }
        internal static bool CpuTargetsArm64_Serialise(BuildTarget selectedTarget)
        {
            return selectedTarget == BuildTarget.Android;
        }

        internal static readonly string StackProtector_DisplayName = "Stack Protector";
        internal static readonly string StackProtector_ToolTip = "Stack protector level for the selected platform.";

        internal static bool StackProtector_Display(BuildTarget selectedTarget, string architecture)
        {
            return selectedTarget == BuildTarget.Android;
        }

        internal static bool StackProtector_Serialise(BuildTarget selectedTarget)
        {
            return selectedTarget == BuildTarget.Android;
        }

        internal static readonly string StackProtectorBufferSize_DisplayName = "Stack Protector Buffer Size";
        internal static readonly string StackProtectorBufferSize_ToolTip = "Stack protector buffer size for the selected platform.";

        internal static bool StackProtectorBufferSize_Display(BuildTarget selectedTarget, string architecture)
        {
            return selectedTarget == BuildTarget.Android;
        }

        internal static bool StackProtectorBufferSize_Serialise(BuildTarget selectedTarget)
        {
            return selectedTarget == BuildTarget.Android;
        }

        internal static bool IsStandalone(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                    return true;
                default:
                    return false;
            }
        }

        BurstPlatformAotSettings(BuildTarget target)
        {
            InitialiseDefaults();
        }

        private const int DefaultVersion = 5;

        internal void InitialiseDefaults()
        {
            Version = DefaultVersion;
            EnableSafetyChecks = false;
            EnableBurstCompilation = true;
            EnableOptimisations = true;
            EnableDebugInAllBuilds = false;
            DebugDataKind = DebugDataKind.Full;
            UsePlatformSDKLinker = false; // Only applicable for desktop targets (Windows/Mac/Linux)
            CpuMinTargetX32 = 0;
            CpuMaxTargetX32 = 0;
            CpuMinTargetX64 = 0;
            CpuMaxTargetX64 = 0;
            CpuTargetsX32 = BitsetX86Targets.SSE2 | BitsetX86Targets.SSE4;
            CpuTargetsX64 = BitsetX64Targets.SSE2 | BitsetX64Targets.AVX2;
            CpuTargetsArm64 = BitsetArm64Targets.ARMV8A;
            DisabledWarnings = "";
            OptimizeFor = OptimizeFor.Default;
            FloatMode = FloatMode.Default;
            StackProtector = StackProtector.Off;
            StackProtectorBufferSize = 8;
        }

        internal static string GetPath(BuildTarget? target)
        {
            if (target.HasValue)
            {
                return "ProjectSettings/BurstAotSettings_" + target.ToString() + ".json";
            }
            else
            {
                return "ProjectSettings/CommonBurstAotSettings.json";
            }
        }

        internal static BuildTarget? ResolveTarget(BuildTarget? target)
        {
            if (!target.HasValue)
            {
                return target;
            }

            // Treat the 32/64 platforms the same from the point of view of burst settings
            // since there is no real way to distinguish from the platforms selector
            if (target == BuildTarget.StandaloneWindows64 || target == BuildTarget.StandaloneWindows)
                return BuildTarget.StandaloneWindows;

            // 32 bit linux support was deprecated
            if (target == BuildTarget.StandaloneLinux64)
                return BuildTarget.StandaloneLinux64;

            return target;
        }

        private static bool IsOutputPathToBuildFolder(BuildSummary summary) =>
            Directory.Exists(summary.outputPath)                // Path to either an executable or the specified build folder.
            && summary.platform != BuildTarget.StandaloneOSX;   // For MacOSX the folder pointed to is the .app folder
                                                                // that is contained within the build folder.

        private static string FetchBuildFolder(BuildSummary summary)
        {
            // Trouble is that it differs based on the build target whether summary refers to:
            //    - A specific executable.
            //    - The folder being build to.
            // No matter what, we want to place the debug information directory inside of this build
            // folder.
            var outputPath = summary.outputPath;
            return IsOutputPathToBuildFolder(summary)
                ? outputPath                            // outputPath = <path-to-build-directory>
                : Path.GetDirectoryName(outputPath);    // outputPath = <path-to-executable>
        }

        internal static readonly string BurstMiscPathPostFix = "_BurstDebugInformation_DoNotShip";
        internal static string FetchOutputPath(BuildSummary summary, string productName)
        {
            var burstMiscFolderName = $"{RemoveIllegalPathChars(productName)}{BurstMiscPathPostFix}";

            var finalOutputPath = FetchBuildFolder(summary);

            // For EmbeddedLinux and QNX, the burstMiscFolder is placed as a sibling of the build folder.
            // They also use the build directory name instead of the player product name for the BurstDebugInformation folder.
            if (summary.platform == BuildTarget.EmbeddedLinux || summary.platform == BuildTarget.QNX)
            {
                finalOutputPath = Path.GetDirectoryName(finalOutputPath);

                var buildDirectoryName = Path.GetFileNameWithoutExtension(summary.outputPath);
                burstMiscFolderName = $"{RemoveIllegalPathChars(buildDirectoryName)}{BurstMiscPathPostFix}";
            }

            return Path.Combine(finalOutputPath, burstMiscFolderName);
        }

        private static readonly Regex IllegalPathChars = new Regex("[/:\\\\*<>|?\"]");
        private static string RemoveIllegalPathChars(string name) => IllegalPathChars.Replace(name, "");

        internal static BurstPlatformAotSettings GetOrCreateSettings(BuildTarget? target, bool saveOnUpgrade = true)
        {
            target = ResolveTarget(target);
            var settings = CreateInstance<BurstPlatformAotSettings>();
            settings.InitialiseDefaults();
            string path = GetPath(target);

            var fileExists = File.Exists(path);
            var upgraded = false;
            if (fileExists)
            {
                var json = File.ReadAllText(path);
                settings = SerialiseIn(target, json, out upgraded);
            }

            if (upgraded && saveOnUpgrade)
            {
                // If we've just upgraded the settings file to a new version, save it to disk now.
                settings.Save(target);
            }

            // Overwrite the settings with any that are common and shared between all settings.
            if (target.HasValue)
            {
                var commonSettings = GetOrCreateSettings(null, saveOnUpgrade);

                var platformFields = typeof(BurstPlatformAotSettings).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var field in platformFields)
                {
                    if (null != field.GetCustomAttribute<BurstCommonSettingAttribute>())
                    {
                        field.SetValue(settings, field.GetValue(commonSettings));
                    }
                }
            }

            return settings;
        }

        delegate bool SerialiseItem(BuildTarget selectedPlatform);

        private static BurstPlatformAotSettings SerialiseIn(BuildTarget? target, string json, out bool upgraded)
        {
            var versioned = ScriptableObject.CreateInstance<BurstPlatformAotSettings>();
            EditorJsonUtility.FromJsonOverwrite(json, versioned);

            upgraded = false;

            if (versioned.Version == 0)
            {
                // Deal with pre versioned format
                var legacy = ScriptableObject.CreateInstance<BurstPlatformLegacySettings>();
                EditorJsonUtility.FromJsonOverwrite(json, legacy);

                // Legacy file, upgrade it
                versioned.InitialiseDefaults();
                versioned.EnableOptimisations = !legacy.DisableOptimisations;
                versioned.EnableBurstCompilation = !legacy.DisableBurstCompilation;
                versioned.EnableSafetyChecks = !legacy.DisableSafetyChecks;

                // Destroy the legacy object so Unity doesn't try to backup / restore it later during domain reload.
                ScriptableObject.DestroyImmediate(legacy);

                upgraded = true;
            }

            if (versioned.Version < 3)
            {
                // Upgrade the version first
                versioned.Version = 3;

                // Upgrade from min..max targets to bitset
                versioned.CpuTargetsX32 |= (BitsetX86Targets)(1 << (int)versioned.CpuMinTargetX32);
                versioned.CpuTargetsX32 |= (BitsetX86Targets)(1 << (int)versioned.CpuMaxTargetX32);

                versioned.CpuTargetsX64 |= (BitsetX64Targets)(1 << (int)versioned.CpuMinTargetX64);
                versioned.CpuTargetsX64 |= (BitsetX64Targets)(1 << (int)versioned.CpuMaxTargetX64);

                // Extra checks to add targets in the min..max range for 64-bit targets.
                switch (versioned.CpuMinTargetX64)
                {
                    default:
                        break;
                    case AvailX64Targets.SSE2:
                        switch (versioned.CpuMaxTargetX64)
                        {
                            default:
                                break;
                            case AvailX64Targets.AVX2:
                                versioned.CpuTargetsX64 |= (BitsetX64Targets)(1 << (int)AvailX64Targets.AVX);
                                goto case AvailX64Targets.AVX;
                            case AvailX64Targets.AVX:
                                versioned.CpuTargetsX64 |= (BitsetX64Targets)(1 << (int)AvailX64Targets.SSE4);
                                break;
                        }
                        break;
                    case AvailX64Targets.SSE4:
                        switch (versioned.CpuMaxTargetX64)
                        {
                            default:
                                break;
                            case AvailX64Targets.AVX2:
                                versioned.CpuTargetsX64 |= (BitsetX64Targets)(1 << (int)AvailX64Targets.AVX);
                                break;
                        }
                        break;
                }

                // Wipe the old min/max targets
                versioned.CpuMinTargetX32 = 0;
                versioned.CpuMaxTargetX32 = 0;
                versioned.CpuMinTargetX64 = 0;
                versioned.CpuMaxTargetX64 = 0;

                upgraded = true;
            }

            if (versioned.Version < 4)
            {
                // Upgrade the version first.
                versioned.Version = 4;

                // When we upgrade we'll set the optimization level to default (which is, as expected, the default).
                versioned.OptimizeFor = OptimizeFor.Default;

                // This option has been removed as user-setting options, so switch them to false here.
                versioned.EnableSafetyChecks = false;

                upgraded = true;
            }

            if (versioned.Version < 5)
            {
                // Upgrade with stack protector options.
                versioned.Version = 5;
                versioned.StackProtector = StackProtector.Off;
                versioned.StackProtectorBufferSize = 8;
            }

            // Otherwise should be a modern file with a valid version (we can use that to upgrade when the time comes)
            return versioned;
        }

        private static bool ShouldSerialiseOut(BuildTarget? target, FieldInfo field)
        {
            var method = typeof(BurstPlatformAotSettings).GetMethod(field.Name + "_Serialise", BindingFlags.Static | BindingFlags.NonPublic);
            if (method != null)
            {
                var shouldSerialise = (SerialiseItem)Delegate.CreateDelegate(typeof(SerialiseItem), method);
                if (!target.HasValue || !shouldSerialise(target.Value))
                {
                    return false;
                }
            }

            // If we always need to write out the attribute, return now.
            if (null != field.GetCustomAttribute<BurstMetadataSettingAttribute>())
            {
                return true;
            }

            var isCommon = !target.HasValue;
            var hasCommonAttribute = null != field.GetCustomAttribute<BurstCommonSettingAttribute>();

            if ((isCommon && hasCommonAttribute) || (!isCommon && !hasCommonAttribute))
            {
                return true;
            }

            return false;
        }

        internal string SerialiseOut(BuildTarget? target)
        {
            // Version 2 and onwards serialise a custom object in order to avoid serialising all the settings.
            StringBuilder s = new StringBuilder();
            s.Append("{\n");
            s.Append("  \"MonoBehaviour\": {\n");
            var platformFields = typeof(BurstPlatformAotSettings).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            int total = 0;
            for (int i = 0; i < platformFields.Length; i++)
            {
                if (ShouldSerialiseOut(target, platformFields[i]))
                {
                    total++;
                }
            }
            for (int i = 0; i < platformFields.Length; i++)
            {
                if (ShouldSerialiseOut(target, platformFields[i]))
                {
                    s.Append($"    \"{platformFields[i].Name}\": ");
                    if (platformFields[i].FieldType.IsEnum)
                        s.Append((int)platformFields[i].GetValue(this));
                    else if (platformFields[i].FieldType == typeof(string))
                        s.Append($"\"{platformFields[i].GetValue(this)}\"");
                    else if (platformFields[i].FieldType == typeof(bool))
                        s.Append(((bool)platformFields[i].GetValue(this)) ? "true" : "false");
                    else if (platformFields[i].FieldType == typeof(uint))
                        s.Append((uint)platformFields[i].GetValue(this));
                    else
                        s.Append((int)platformFields[i].GetValue(this));

                    total--;
                    if (total != 0)
                        s.Append(",");
                    s.Append("\n");
                }
            }
            s.Append("  }\n");
            s.Append("}\n");

            return s.ToString();
        }

        internal void Save(BuildTarget? target)
        {
            if (target.HasValue)
            {
                target = ResolveTarget(target);
            }

            var path = GetPath(target);

            if (!AssetDatabase.IsOpenForEdit(path))
            {
                if (!AssetDatabase.MakeEditable(path))
                {
                    Debug.LogWarning($"Burst could not save AOT settings file {path}");
                    return;
                }
            }

            File.WriteAllText(path, SerialiseOut(target));
        }

        internal static SerializedObject GetCommonSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings(null));
        }

        internal static SerializedObject GetSerializedSettings(BuildTarget target)
        {
            return new SerializedObject(GetOrCreateSettings(target));
        }

        internal static bool Has32BitSupport(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.WSAPlayer:
                    return true;
                default:
                    return false;
            }
        }

        internal static bool Has32BitSupportForSerialise(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.WSAPlayer:
                    return true;
                default:
                    return false;
            }
        }

        internal static bool Has64BitSupport(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.WSAPlayer:
                case BuildTarget.StandaloneOSX:
                    return true;
                default:
                    return false;
            }
        }

        private static BurstTargetCpu GetCpu(int v)
        {
            // https://graphics.stanford.edu/~seander/bithacks.html#IntegerLog
            var r = ((v > 0xFFFF) ? 1 : 0) << 4; v >>= r;
            var shift = ((v > 0xFF) ? 1 : 0) << 3; v >>= shift; r |= shift;
            shift = ((v > 0xF) ? 1 : 0) << 2; v >>= shift; r |= shift;
            shift = ((v > 0x3) ? 1 : 0) << 1; v >>= shift; r |= shift;
            r |= (v >> 1);
            return (BurstTargetCpu)r;
        }

        private static IEnumerable<Enum> GetFlags(Enum input)
        {
            foreach (Enum value in Enum.GetValues(input.GetType()))
            {
                if (input.HasFlag(value))
                {
                    yield return value;
                }
            }
        }

        internal TargetCpus GetDesktopCpu32Bit()
        {
            var cpus = new TargetCpus();

            foreach (var target in GetFlags(CpuTargetsX32))
            {
                cpus.Cpus.Add(GetCpu((int)(BitsetX86Targets)target));
            }

            // If no targets were specified just default to the oldest CPU supported.
            if (cpus.Cpus.Count == 0)
            {
                cpus.Cpus.Add(BurstTargetCpu.X86_SSE2);
            }

            return cpus;
        }

        internal TargetCpus GetDesktopCpu64Bit()
        {
            var cpus = new TargetCpus();

            foreach (var target in GetFlags(CpuTargetsX64))
            {
                cpus.Cpus.Add(GetCpu((int)(BitsetX64Targets)target));
            }

            // If no targets were specified just default to the oldest CPU supported.
            if (cpus.Cpus.Count == 0)
            {
                cpus.Cpus.Add(BurstTargetCpu.X64_SSE2);
            }

            return cpus;
        }

        internal TargetCpus GetAndroidCpuArm64()
        {
            var cpus = new TargetCpus();

            foreach (var target in GetFlags(CpuTargetsArm64))
            {
                cpus.Cpus.Add(GetCpu((int)(BitsetArm64Targets)target));
            }

            // If no targets were specified just default to the oldest CPU supported.
            if (cpus.Cpus.Count == 0)
            {
                cpus.Cpus.Add(BurstTargetCpu.ARMV8A_AARCH64);
            }

            return cpus;
        }
    }

    static class BurstAotSettingsIMGUIRegister
    {
        class BurstAotSettingsProvider : SettingsProvider
        {
            SerializedObject[] m_PlatformSettings;
            SerializedProperty[][] m_PlatformProperties;
            DisplayItem[][] m_PlatformVisibility;
            GUIContent[][] m_PlatformToolTips;
            BuildPlatform[] m_ValidPlatforms;
            SerializedObject m_CommonPlatformSettings;

            delegate bool DisplayItem(BuildTarget selectedTarget, string architecture);

            static bool DefaultShow(BuildTarget selectedTarget, string architecture)
            {
                return true;
            }
            static bool DefaultHide(BuildTarget selectedTarget, string architecture)
            {
                return false;
            }

            public BurstAotSettingsProvider()
                : base("Project/Burst AOT Settings", SettingsScope.Project, null)
            {
                int a;

                m_ValidPlatforms = BuildPlatforms.instance.GetValidPlatforms().ToArray();

                var platformFields = typeof(BurstPlatformAotSettings).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                int numPlatformFields = platformFields.Length;
                int numKeywords = numPlatformFields;
                var tempKeywords = new string[numKeywords];

                for (a = 0; a < numPlatformFields; a++)
                {
                    tempKeywords[a] = typeof(BurstPlatformAotSettings).GetField(platformFields[a].Name + "_ToolTip", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as string;
                }

                keywords = new HashSet<string>(tempKeywords);

                m_PlatformSettings = new SerializedObject[m_ValidPlatforms.Length];
                m_PlatformProperties = new SerializedProperty[m_ValidPlatforms.Length][];
                m_PlatformVisibility = new DisplayItem[m_ValidPlatforms.Length][];
                m_PlatformToolTips = new GUIContent[m_ValidPlatforms.Length][];

                m_CommonPlatformSettings = null;
            }

            public override void OnActivate(string searchContext, VisualElement rootElement)
            {
                var platformFields = typeof(BurstPlatformAotSettings).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                for (int p = 0; p < m_ValidPlatforms.Length; p++)
                {
                    InitialiseSettingsForCommon(platformFields);
                    InitialiseSettingsForPlatform(p, platformFields);
                }
            }

            private void InitialiseSettingsForCommon(FieldInfo[] commonFields)
            {
                m_CommonPlatformSettings = BurstPlatformAotSettings.GetCommonSerializedSettings();
            }

            private void InitialiseSettingsForPlatform(int platform, FieldInfo[] platformFields)
            {
                if (m_ValidPlatforms[platform].targetGroup == BuildTargetGroup.Standalone)
                    m_PlatformSettings[platform] = BurstPlatformAotSettings.GetSerializedSettings(EditorUserBuildSettings.selectedStandaloneTarget);
                else
                    m_PlatformSettings[platform] = BurstPlatformAotSettings.GetSerializedSettings(m_ValidPlatforms[platform].defaultTarget);

                m_PlatformProperties[platform] = new SerializedProperty[platformFields.Length];
                m_PlatformToolTips[platform] = new GUIContent[platformFields.Length];
                m_PlatformVisibility[platform] = new DisplayItem[platformFields.Length];
                for (int i = 0; i < platformFields.Length; i++)
                {
                    m_PlatformProperties[platform][i] = m_PlatformSettings[platform].FindProperty(platformFields[i].Name);
                    var displayName = typeof(BurstPlatformAotSettings).GetField(platformFields[i].Name + "_DisplayName", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as string;
                    var toolTip = typeof(BurstPlatformAotSettings).GetField(platformFields[i].Name + "_ToolTip", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as string;
                    m_PlatformToolTips[platform][i] = EditorGUIUtility.TrTextContent(displayName, toolTip);

                    var method = typeof(BurstPlatformAotSettings).GetMethod(platformFields[i].Name + "_Display", BindingFlags.Static | BindingFlags.NonPublic);
                    if (method == null)
                    {
                        if (displayName == null)
                        {
                            m_PlatformVisibility[platform][i] = DefaultHide;
                        }
                        else
                        {
                            m_PlatformVisibility[platform][i] = DefaultShow;
                        }
                    }
                    else
                    {
                        m_PlatformVisibility[platform][i] = (DisplayItem)Delegate.CreateDelegate(typeof(DisplayItem), method);
                    }
                }
            }

            private string FetchStandaloneTargetName()
            {
                switch (EditorUserBuildSettings.selectedStandaloneTarget)
                {
                    case BuildTarget.StandaloneOSX:
                        return "Mac OS X";    // Matches the Build Settings Dialog names
                    case BuildTarget.StandaloneWindows:
                    case BuildTarget.StandaloneWindows64:
                        return "Windows";
                    default:
                        return "Linux";
                }
            }

            public override void OnGUI(string searchContext)
            {
                var rect = EditorGUILayout.BeginVertical();

                EditorGUIUtility.labelWidth = rect.width / 2;

                int selectedPlatform = EditorGUILayout.BeginPlatformGrouping(m_ValidPlatforms, null);

                // During a build and other cases, the settings object can become invalid, if it does, we re-build it for the current platform
                // this fixes the settings failing to save if modified after a build has finished, and the settings were still open
                if (!m_PlatformSettings[selectedPlatform].isValid)
                {
                    var platformFields = typeof(BurstPlatformAotSettings).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                    InitialiseSettingsForCommon(platformFields);

                    // If the selected platform is invalid, it means all of them will be. So we do a pass to reinitialize all now.
                    for (var platform = 0; platform < m_PlatformSettings.Length; platform++)
                    {
                        InitialiseSettingsForPlatform(platform, platformFields);
                    }
                }

                var selectedTarget = m_ValidPlatforms[selectedPlatform].defaultTarget;
                if (m_ValidPlatforms[selectedPlatform].targetGroup == BuildTargetGroup.Standalone)
                    selectedTarget = EditorUserBuildSettings.selectedStandaloneTarget;

                var buildTargetName = BuildPipeline.GetBuildTargetName(selectedTarget);
                var architecture = EditorUserBuildSettings.GetPlatformSettings(buildTargetName, "Architecture").ToLowerInvariant();

                if (m_ValidPlatforms[selectedPlatform].targetGroup == BuildTargetGroup.Standalone)
                {
                    // Note burst treats Windows and Windows32 as the same target from a settings point of view (same for linux)
                    // So we only display the standalone platform
                    EditorGUILayout.LabelField(EditorGUIUtility.TrTextContent("Target Platform", "Shows the currently selected standalone build target, can be switched in the Build Settings dialog"), EditorGUIUtility.TrTextContent(FetchStandaloneTargetName()));
                }

                for (int i = 0; i < m_PlatformProperties[selectedPlatform].Length; i++)
                {
                    if (m_PlatformVisibility[selectedPlatform][i](selectedTarget, architecture))
                    {
                        EditorGUILayout.PropertyField(m_PlatformProperties[selectedPlatform][i], m_PlatformToolTips[selectedPlatform][i]);
                    }
                }

                if (m_ValidPlatforms[selectedPlatform].targetGroup == BuildTargetGroup.Android)
                    EditorGUILayout.HelpBox("Armv9A (SVE2) target CPU architecture is experimental", MessageType.Warning);

                EditorGUILayout.EndPlatformGrouping();

                EditorGUILayout.EndVertical();

                EditorGUILayout.LabelField("* Shared setting common across all platforms");

                if (m_PlatformSettings[selectedPlatform].hasModifiedProperties)
                {
                    m_PlatformSettings[selectedPlatform].ApplyModifiedPropertiesWithoutUndo();

                    var commonAotSettings = ((BurstPlatformAotSettings)m_CommonPlatformSettings.targetObject);
                    var platformAotSettings = ((BurstPlatformAotSettings)m_PlatformSettings[selectedPlatform].targetObject);

                    var platformFields = typeof(BurstPlatformAotSettings).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

                    foreach (var field in platformFields)
                    {
                        if (null != field.GetCustomAttribute<BurstCommonSettingAttribute>())
                        {
                            field.SetValue(commonAotSettings, field.GetValue(platformAotSettings));

                            foreach (var platformSetting in m_PlatformSettings)
                            {
                                field.SetValue(platformSetting.targetObject, field.GetValue(commonAotSettings));
                            }
                        }
                    }

                    commonAotSettings.Save(null);
                    platformAotSettings.Save(selectedTarget);
                }
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateBurstAotSettingsProvider()
        {
            return new BurstAotSettingsProvider();
        }
    }
}
