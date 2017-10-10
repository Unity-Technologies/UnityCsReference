// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Modules;
using System.Collections.Generic;
using System.Text;

internal class DesktopStandaloneBuildWindowExtension : DefaultBuildWindowExtension
{
    private GUIContent m_StandaloneTarget = EditorGUIUtility.TextContent("Target Platform|Destination platform for standalone build");
    private GUIContent m_Architecture = EditorGUIUtility.TextContent("Architecture|Build m_Architecture for standalone");

    private BuildTarget[] m_StandaloneSubtargets;
    private GUIContent[] m_StandaloneSubtargetStrings;

    public DesktopStandaloneBuildWindowExtension()
    {
        SetupStandaloneSubtargets();
    }

    private void SetupStandaloneSubtargets()
    {
        List<BuildTarget> standaloneSubtargetsList = new List<BuildTarget>();
        List<GUIContent> standaloneSubtargetStringsList = new List<GUIContent>();

        if (ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneWindows)))
        {
            standaloneSubtargetsList.Add(BuildTarget.StandaloneWindows);
            standaloneSubtargetStringsList.Add(EditorGUIUtility.TextContent("Windows"));
        }
        if (ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneOSX)))
        {
            standaloneSubtargetsList.Add(BuildTarget.StandaloneOSX);
            standaloneSubtargetStringsList.Add(EditorGUIUtility.TextContent("Mac OS X"));
        }
        if (ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneLinux)))
        {
            standaloneSubtargetsList.Add(BuildTarget.StandaloneLinux);
            standaloneSubtargetStringsList.Add(EditorGUIUtility.TextContent("Linux"));
        }

        m_StandaloneSubtargets = standaloneSubtargetsList.ToArray();
        m_StandaloneSubtargetStrings = standaloneSubtargetStringsList.ToArray();
    }

    internal static BuildTarget GetBestStandaloneTarget(BuildTarget selectedTarget)
    {
        if (ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(selectedTarget)))
            return selectedTarget;
        if (RuntimePlatform.WindowsEditor == Application.platform && ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneWindows)))
            return BuildTarget.StandaloneWindows;
        if (RuntimePlatform.OSXEditor == Application.platform && ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneOSX)))
            return BuildTarget.StandaloneOSX;
        if (ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneOSX)))
            return BuildTarget.StandaloneOSX;
        if (ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneLinux)))
            return BuildTarget.StandaloneLinux;
        return BuildTarget.StandaloneWindows;
    }

    private static Dictionary<GUIContent, BuildTarget> GetArchitecturesForPlatform(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return new Dictionary<GUIContent, BuildTarget>() {
                    { EditorGUIUtility.TextContent("x86"), BuildTarget.StandaloneWindows },
                    { EditorGUIUtility.TextContent("x86_64"), BuildTarget.StandaloneWindows64 },
                };
            case BuildTarget.StandaloneLinux:
            case BuildTarget.StandaloneLinux64:
            case BuildTarget.StandaloneLinuxUniversal:
                return new Dictionary<GUIContent, BuildTarget>() {
                    { EditorGUIUtility.TextContent("x86"), BuildTarget.StandaloneLinux },
                    { EditorGUIUtility.TextContent("x86_64"), BuildTarget.StandaloneLinux64 },
                    { EditorGUIUtility.TextContent("x86 + x86_64 (Universal)"), BuildTarget.StandaloneLinuxUniversal },
                };
            default:
                return null;
        }
    }

    private static BuildTarget DefaultTargetForPlatform(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return BuildTarget.StandaloneWindows;
            case BuildTarget.StandaloneLinux:
            case BuildTarget.StandaloneLinux64:
            case BuildTarget.StandaloneLinuxUniversal:
                return BuildTarget.StandaloneLinux;
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

    public override void ShowPlatformBuildOptions()
    {
        BuildTarget selectedTarget = GetBestStandaloneTarget(EditorUserBuildSettings.selectedStandaloneTarget);
        BuildTarget newTarget = EditorUserBuildSettings.selectedStandaloneTarget;

        int selectedIndex = Math.Max(0, Array.IndexOf(m_StandaloneSubtargets, DefaultTargetForPlatform(selectedTarget)));
        int newIndex = EditorGUILayout.Popup(m_StandaloneTarget, selectedIndex, m_StandaloneSubtargetStrings);

        if (newIndex == selectedIndex)
        {
            Dictionary<GUIContent, BuildTarget> architectures = GetArchitecturesForPlatform(selectedTarget);
            if (null != architectures)
            {
                // Display architectures for the current target platform
                GUIContent[] architectureNames = new List<GUIContent>(architectures.Keys).ToArray();
                int selectedArchitecture = 0;

                if (newIndex == selectedIndex)
                {
                    // Grab m_Architecture index for currently selected target
                    foreach (var architecture in architectures)
                    {
                        if (architecture.Value == selectedTarget)
                        {
                            selectedArchitecture = System.Math.Max(0, System.Array.IndexOf(architectureNames, architecture.Key));
                            break;
                        }
                    }
                }
                selectedArchitecture = EditorGUILayout.Popup(m_Architecture, selectedArchitecture, architectureNames);
                newTarget = architectures[architectureNames[selectedArchitecture]];
            }
        }
        else
        {
            newTarget = m_StandaloneSubtargets[newIndex];
        }

        if (newTarget != EditorUserBuildSettings.selectedStandaloneTarget)
        {
            // setting selectedStandaloneTarget has side-effect: stops playmode
            EditorUserBuildSettings.selectedStandaloneTarget = newTarget;
            GUIUtility.ExitGUI();
        }
    }

    public override bool EnabledBuildButton()
    {
        return true;
    }

    public override bool EnabledBuildAndRunButton()
    {
        return true;
    }
}
