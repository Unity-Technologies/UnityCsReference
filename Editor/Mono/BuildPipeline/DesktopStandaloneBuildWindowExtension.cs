// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Modules;
using UnityEditor.Build;
using UnityEngine;

internal abstract class DesktopStandaloneBuildWindowExtension : DefaultBuildWindowExtension
{
    private GUIContent m_StandaloneTarget = EditorGUIUtility.TrTextContent("Target Platform", "Destination platform for standalone build");
    private GUIContent m_Architecture = EditorGUIUtility.TrTextContent("Architecture", "Build m_Architecture for standalone");
    private BuildTarget[] m_StandaloneSubtargets;
    private GUIContent[] m_StandaloneSubtargetStrings;

    protected bool m_HasMonoPlayers;
    protected bool m_HasIl2CppPlayers;
    protected bool m_HasCoreCLRPlayers;
    protected bool m_HasServerMonoPlayers;
    protected bool m_HasServerIl2CppPlayers;
    protected bool m_IsRunningOnHostPlatform;

    public bool MonoPlayersInstalled(NamedBuildTarget namedBuildTarget)
    {
        return namedBuildTarget == NamedBuildTarget.Server ? m_HasServerMonoPlayers : m_HasMonoPlayers;
    }

    public bool Il2CppPlayersInstalled(NamedBuildTarget namedBuildTarget)
    {
        return namedBuildTarget == NamedBuildTarget.Server ? m_HasServerIl2CppPlayers : m_HasIl2CppPlayers;
    }

    public static void SetArchitectureForPlatform(BuildTarget buildTarget, OSArchitecture architecture)
    {
        EditorUserBuildSettings.SetPlatformSettings(BuildPipeline.GetBuildTargetName(buildTarget), EditorUserBuildSettings.kSettingArchitecture, architecture.ToString().ToLower());
    }

    public DesktopStandaloneBuildWindowExtension(bool hasMonoPlayers, bool hasIl2CppPlayers, bool hasCoreCLRPlayers, bool hasServerMonoPlayers, bool hasServerIl2CppPlayers)
    {
        SetupStandaloneSubtargets();

        m_IsRunningOnHostPlatform = Application.platform == GetHostPlatform();
        m_HasIl2CppPlayers = hasIl2CppPlayers;
        m_HasCoreCLRPlayers = hasCoreCLRPlayers;
        m_HasMonoPlayers = hasMonoPlayers;
        m_HasServerMonoPlayers = hasServerMonoPlayers;
        m_HasServerIl2CppPlayers = hasServerIl2CppPlayers;
    }

    private void SetupStandaloneSubtargets()
    {
        List<BuildTarget> standaloneSubtargetsList = new List<BuildTarget>();
        List<GUIContent> standaloneSubtargetStringsList = new List<GUIContent>();

        if (ModuleManager.IsPlatformSupportLoadedByBuildTarget(BuildTarget.StandaloneWindows))
        {
            standaloneSubtargetsList.Add(BuildTarget.StandaloneWindows);
            standaloneSubtargetStringsList.Add(EditorGUIUtility.TrTextContent("Windows"));
        }
        if (ModuleManager.IsPlatformSupportLoadedByBuildTarget(BuildTarget.StandaloneOSX))
        {
            standaloneSubtargetsList.Add(BuildTarget.StandaloneOSX);
            standaloneSubtargetStringsList.Add(EditorGUIUtility.TrTextContent("macOS"));
        }
        if (ModuleManager.IsPlatformSupportLoadedByBuildTarget(BuildTarget.StandaloneLinux64))
        {
            standaloneSubtargetsList.Add(BuildTarget.StandaloneLinux64);
            standaloneSubtargetStringsList.Add(EditorGUIUtility.TrTextContent("Linux"));
        }

        m_StandaloneSubtargets = standaloneSubtargetsList.ToArray();
        m_StandaloneSubtargetStrings = standaloneSubtargetStringsList.ToArray();
    }

    internal static BuildTarget GetBestStandaloneTarget(BuildTarget selectedTarget)
    {
        if (ModuleManager.IsPlatformSupportLoadedByBuildTarget(selectedTarget))
            return selectedTarget;
        if (RuntimePlatform.WindowsEditor == Application.platform && ModuleManager.IsPlatformSupportLoadedByBuildTarget(BuildTarget.StandaloneWindows))
            return BuildTarget.StandaloneWindows64;
        if (RuntimePlatform.OSXEditor == Application.platform && ModuleManager.IsPlatformSupportLoadedByBuildTarget(BuildTarget.StandaloneOSX))
            return BuildTarget.StandaloneOSX;
        if (ModuleManager.IsPlatformSupportLoadedByBuildTarget(BuildTarget.StandaloneOSX))
            return BuildTarget.StandaloneOSX;
        if (ModuleManager.IsPlatformSupportLoadedByBuildTarget(BuildTarget.StandaloneLinux64))
            return BuildTarget.StandaloneLinux64;
        return BuildTarget.StandaloneWindows64;
    }

    struct BuildTargetInfo
    {
        public BuildTarget buildTarget;
        public OSArchitecture architecture;
    }

    private static BuildTarget DefaultTargetForPlatform(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return BuildTarget.StandaloneWindows64;
                // Deprecated
#pragma warning disable 612, 618
            case BuildTarget.StandaloneLinux:
            case BuildTarget.StandaloneLinuxUniversal:
#pragma warning restore 612, 618
            case BuildTarget.StandaloneLinux64:
                return BuildTarget.StandaloneLinux64;
            case BuildTarget.StandaloneOSX:
                // Deprecated
#pragma warning disable 612, 618
            case BuildTarget.StandaloneOSXIntel:
            case BuildTarget.StandaloneOSXIntel64:
#pragma warning restore 612, 618
                return BuildTarget.StandaloneOSX;
            default:
                return target;
        }
    }

    private static BuildTargetInfo DefaultArchitectureForTarget(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return new BuildTargetInfo
                {
                    buildTarget = BuildTarget.StandaloneWindows64,
                    architecture = OSArchitecture.x64
                };
                // Deprecated
#pragma warning disable 612, 618
            case BuildTarget.StandaloneLinux:
            case BuildTarget.StandaloneLinuxUniversal:
#pragma warning restore 612, 618
            case BuildTarget.StandaloneLinux64:
                return new BuildTargetInfo
                {
                    buildTarget = BuildTarget.StandaloneLinux64,
                    architecture = OSArchitecture.x64
                };
            case BuildTarget.StandaloneOSX:
                // Deprecated
#pragma warning disable 612, 618
            case BuildTarget.StandaloneOSXIntel:
            case BuildTarget.StandaloneOSXIntel64:
#pragma warning restore 612, 618
                return new BuildTargetInfo
                {
                    buildTarget = BuildTarget.StandaloneOSX,
                    architecture = OSArchitecture.x64
                };
            default:
                return new BuildTargetInfo
                {
                    buildTarget = target,
                    architecture = OSArchitecture.x64
                };
        }
    }

    protected virtual void ShowArchitectureSpecificOptions() {}

    public override void ShowPlatformBuildOptions()
    {
        BuildTarget selectedTarget = GetBestStandaloneTarget(EditorUserBuildSettings.selectedStandaloneTarget);
        BuildTargetInfo newTarget = new BuildTargetInfo {buildTarget = EditorUserBuildSettings.selectedStandaloneTarget};

        int selectedIndex = Math.Max(0, Array.IndexOf(m_StandaloneSubtargets, DefaultTargetForPlatform(selectedTarget)));
        int newIndex = EditorGUILayout.Popup(m_StandaloneTarget, selectedIndex, m_StandaloneSubtargetStrings);

        if (newIndex != selectedIndex)
        {
            newTarget = DefaultArchitectureForTarget(m_StandaloneSubtargets[newIndex]);
        }

        if (newTarget.buildTarget != EditorUserBuildSettings.selectedStandaloneTarget)
        {
            // setting selectedStandaloneTarget has side-effect: stops playmode
            EditorUserBuildSettings.selectedStandaloneTarget = newTarget.buildTarget;
            SetArchitectureForPlatform(newTarget.buildTarget, newTarget.architecture);
            GUIUtility.ExitGUI();
        }

        ShowArchitectureSpecificOptions();
        ShowBackendErrorIfNeeded();
    }

    protected void ShowBackendErrorIfNeeded()
    {
        var error = GetCannotBuildPlayerInCurrentSetupError();
        if (string.IsNullOrEmpty(error))
            return;

        EditorGUILayout.HelpBox(error, MessageType.Error);
    }

    public override bool EnabledBuildButton()
    {
        return string.IsNullOrEmpty(GetCannotBuildPlayerInCurrentSetupError());
    }

    protected virtual string GetCannotBuildPlayerInCurrentSetupError()
    {
        var namedBuildTarget = EditorUserBuildSettingsUtils.CalculateSelectedNamedBuildTarget();
        var scriptingBackend = PlayerSettings.GetScriptingBackend(namedBuildTarget);

        if (namedBuildTarget == NamedBuildTarget.Server)
        {
            if (scriptingBackend == ScriptingImplementation.Mono2x && !m_HasServerMonoPlayers)
                return $"Dedicated Server support (Mono) for {GetHostPlatformName()} is not installed.";

            if (scriptingBackend == ScriptingImplementation.IL2CPP && !m_IsRunningOnHostPlatform)
                return string.Format("{0} IL2CPP player can only be built on {0}.", GetHostPlatformName());

            if (scriptingBackend == ScriptingImplementation.IL2CPP && !m_HasServerIl2CppPlayers)
                return $"Dedicated Server support (IL2CPP) for {GetHostPlatformName()} is not installed.";

            return null;
        }

        switch(scriptingBackend)
        {
            case ScriptingImplementation.Mono2x:
            {
                if (!MonoPlayersInstalled(namedBuildTarget))
                    return "Currently selected scripting backend (Mono) is not installed.";
                break;
            }
            #pragma warning disable 618
            case ScriptingImplementation.CoreCLR:
            {
                if (!m_HasCoreCLRPlayers)
                    return $"Currently selected scripting backend (CoreCLR) is not {(Unsupported.IsSourceBuild() ? "installed" : "supported")}."; // CORECLR_FIXME remove sourcebuild
                break;
            }
            case ScriptingImplementation.IL2CPP:
            {
                if (!m_IsRunningOnHostPlatform)
                    return string.Format("{0} IL2CPP player can only be built on {0}.", GetHostPlatformName());
                if (!Il2CppPlayersInstalled(namedBuildTarget))
                    return "Currently selected scripting backend (IL2CPP) is not installed.";
                break;
            }
            default:
            {
                return $"Unknown scripting backend: {scriptingBackend}";
            }
        }

        return null;
    }

    protected abstract RuntimePlatform GetHostPlatform();
    protected abstract string GetHostPlatformName();

    public override bool EnabledBuildAndRunButton()
    {
        return true;
    }

    public override bool ShouldDrawWaitForManagedDebugger()
    {
        return true;
    }
}
