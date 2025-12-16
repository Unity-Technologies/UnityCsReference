// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.PlayMode.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor;

class EditorPlayModeGuard : ScriptableObject, IDisposable
{
    private const string k_ErrorMessage = "EditorApplication.isPlaying was set to true while a custom Play Mode Scenario is active. This is unsupported and can lead to errors. Please use PlayModeScenarioManager.Start() to enter Play Mode when using scenarios, or set the active scenario to the default one.";

    internal enum ResolutionStrategy
    {
        LogError,
        RevertToDefaultScenario,
    }

    private ResolutionStrategy m_ResolutionStrategy;
    private bool m_AllowPlayModeFlag = false;

    internal bool IgnoreOrchestratedScenarioCheck = false;

    internal ResolutionStrategy GetResolutionStrategy()
    {
        return m_ResolutionStrategy;
    }

    internal void SetResolutionStrategy(ResolutionStrategy strategy)
    {
        m_ResolutionStrategy = strategy;
    }

    public void Dispose()
    {
        if (this == null)
            return;

        DestroyImmediate(this);
    }

    private void OnEnable()
    {
        AssertSingleton();
        OrchestratedScenario.PreventScriptableObjectUnload(this);

        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDestroy()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void AssertSingleton()
    {
        var instances = Resources.FindObjectsOfTypeAll<EditorPlayModeGuard>();
        if (instances.Length > 1)
        {
            Debug.LogError($"Multiple instances of {nameof(EditorPlayModeGuard)} detected. There should only be one instance.");
            for (int i = 0; i < instances.Length; i++)
            {
                if (instances[i] != this)
                    DestroyImmediate(instances[i]);
            }
        }
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state is not PlayModeStateChange.ExitingEditMode)
            return;

        if (!ResolveEnteringPlayMode())
        {
            EditorApplication.isPlaying = false; // Cancel entering play mode
        }
    }

    /// <returns>Returns true if entering play mode is allowed, false otherwise.</returns>
    internal bool ResolveEnteringPlayMode()
    {
        if (!IgnoreOrchestratedScenarioCheck)
        {
            if (PlayModeScenarioManager.ActiveScenario is not OrchestratedScenario)
            {
                // This guard should be used only with OrchestratedScenario.
                Debug.LogError($"The active scenario is of type {PlayModeScenarioManager.ActiveScenario.GetType().Name}, but {nameof(EditorPlayModeGuard)} is only meant to be used with {nameof(OrchestratedScenario)}.");
                Dispose();
                return true;
            }
        }

        if (IsNextPlayModeEntryAllowed())
        {
            return true;
        }

        switch (m_ResolutionStrategy)
        {
            case ResolutionStrategy.LogError:
                Debug.LogError(k_ErrorMessage);
                return false;
            case ResolutionStrategy.RevertToDefaultScenario:
                PlayModeScenarioManager.ActiveScenario = null;
                return true;
            default:
                Debug.LogError($"Unknown resolution strategy {m_ResolutionStrategy} in {nameof(EditorPlayModeGuard)}.");
                return false;
        }
    }

    void ResetAllowPlayModeEntry() => m_AllowPlayModeFlag = false;
    internal void AllowNextPlayModeEntry() => m_AllowPlayModeFlag = true;
    internal bool IsNextPlayModeEntryAllowed() => m_AllowPlayModeFlag;

    internal static void EnterPlayModeSafely()
    {
        var instances = Resources.FindObjectsOfTypeAll<EditorPlayModeGuard>();
        if (instances.Length == 1 && instances[0] != null)
        {
            var instance = instances[0];
            instance.AllowNextPlayModeEntry();
            EditorApplication.EnterPlaymode();
            instance.ResetAllowPlayModeEntry();
            return;
        }

        EditorApplication.EnterPlaymode();
    }
}
