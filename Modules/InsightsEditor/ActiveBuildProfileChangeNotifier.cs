// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.EngineDiagnostics;

namespace UnityEditor.InsightsEditor;

sealed partial class ActiveBuildProfileChangeNotifier : IActiveBuildTargetChanged
{
    public int callbackOrder => 0;

    [OnCodeLoaded]
    static void Initialize()
    {
        BuildProfileContext.activeProfileChanged += OnActiveProfileChanged;
    }

    [OnCodeUnloading]
    static void Teardown()
    {
        BuildProfileContext.activeProfileChanged -= OnActiveProfileChanged;
        EditorApplication.delayCall -= Notify;
    }

    public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget) => NotifyNowAndAfterSwitch();

    static void OnActiveProfileChanged(BuildProfile previous, BuildProfile current) => NotifyNowAndAfterSwitch();

    // Platform switches update the active platform GUID after these callbacks fire
    static void NotifyNowAndAfterSwitch()
    {
        Notify();
        EditorApplication.delayCall -= Notify;
        EditorApplication.delayCall += Notify;
    }

    internal static void Notify() => EngineDiagnosticsSettings.NotifyActiveBuildProfileChanged();
}
