// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.Modules;
using UnityEditor.Build;
using UnityEngine;

internal abstract class DesktopStandaloneBuildWindowExtension : DefaultBuildWindowExtension
{
    protected bool m_HasIl2CppPlayers;
    protected bool m_HasCoreCLRPlayers;
    protected bool m_HasServerIl2CppPlayers;
    protected bool m_HasServerCoreCLRPlayers;
    protected bool m_IsRunningOnHostPlatform;

    public bool Il2CppPlayersInstalled(NamedBuildTarget namedBuildTarget)
    {
        return namedBuildTarget == NamedBuildTarget.Server ? m_HasServerIl2CppPlayers : m_HasIl2CppPlayers;
    }

    public bool CoreCLRPlayersInstalled(NamedBuildTarget namedBuildTarget)
    {
        return namedBuildTarget == NamedBuildTarget.Server ? m_HasServerCoreCLRPlayers : m_HasCoreCLRPlayers;
    }

    public DesktopStandaloneBuildWindowExtension(bool hasIl2CppPlayers, bool hasCoreCLRPlayers, bool hasServerIl2CppPlayers, bool hasServerCoreCLRPlayers)
    {
        m_IsRunningOnHostPlatform = Application.platform == GetHostPlatform();
        m_HasIl2CppPlayers = hasIl2CppPlayers;
        m_HasCoreCLRPlayers = hasCoreCLRPlayers;
        m_HasServerIl2CppPlayers = hasServerIl2CppPlayers;
        m_HasServerCoreCLRPlayers = hasServerCoreCLRPlayers;
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

    protected virtual void ShowArchitectureSpecificOptions() {}

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
            if (scriptingBackend == ScriptingImplementation.IL2CPP && !m_IsRunningOnHostPlatform)
                return string.Format("{0} IL2CPP player can only be built on {0}.", GetHostPlatformName());

            if (scriptingBackend == ScriptingImplementation.IL2CPP && !m_HasServerIl2CppPlayers)
                return $"Dedicated Server support (IL2CPP) for {GetHostPlatformName()} is not installed.";

            if (scriptingBackend == ScriptingImplementation.CoreCLR && !m_HasServerCoreCLRPlayers)
                return $"Dedicated Server support (CoreCLR) for {GetHostPlatformName()} is not installed.";

            return null;
        }

        switch(scriptingBackend)
        {
            case ScriptingImplementation.CoreCLR:
            {
                if (!m_HasCoreCLRPlayers)
                    return "Currently selected scripting backend (CoreCLR) is not installed.";
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
}
