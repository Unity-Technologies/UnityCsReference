// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Toolbars.Playbar;
using Unity.Scripting.LifecycleManagement;

namespace Unity.PlayMode.Editor;

static partial class PlayModeButtons
{
    const string k_ElementId = "Play Mode Controls";

    [AutoStaticsCleanupOnCodeReload] // delegate field: re-registered by [OnCodeLoaded] after each reload
    internal static Action RefreshToolbarCallback;

    [OnCodeLoaded]
    static void Initialize()
    {
        RefreshToolbarCallback = RefreshMainToolbar;

        EditorApplication.delayCall += () =>
        {
            ScenarioManagerProvider.instance.ConfigAssetChanged -= RefreshToolbar;
            ScenarioManagerProvider.instance.ConfigAssetChanged += RefreshToolbar;
            PlayModeScenarioUtils.AssetsChanged -= RefreshToolbar;
            PlayModeScenarioUtils.AssetsChanged += RefreshToolbar;
            ScenarioManagerProvider.instance.StateChanged -= OnStateChanged;
            ScenarioManagerProvider.instance.StateChanged += OnStateChanged;

            RefreshToolbar();
        };
    }

    static void RefreshToolbar() => (RefreshToolbarCallback ?? RefreshMainToolbar)();

    static void RefreshMainToolbar()
    {
        try
        {
            MainToolbar.Refresh(k_ElementId);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    static void OnStateChanged(PlayModeScenarioState _) => RefreshToolbar();

    internal static void ScheduleRefresh()
    {
        EditorApplication.delayCall -= RefreshToolbar;
        EditorApplication.delayCall += RefreshToolbar;
    }

    [PlaybarDefaultButtons]
    internal static IEnumerable<MainToolbarElement> CreateDefaultButtons(string elementId)
    {
        var isScenarioValid = true;
        if (PlayModeScenarioManager.ScenarioTypesCount != 0)
        {
            var activeScenario = PlayModeScenarioManager.ActiveScenario;
            if (activeScenario != null)
            {
                isScenarioValid = activeScenario.IsValid(out _);
                var topbarElements = activeScenario.CreateTopbarUI();
                if (topbarElements != null)
                {
                    foreach (var element in topbarElements)
                    {
                        yield return element;
                    }
                }
            }

            yield return new MainToolbarDropdown(GetScenarioDropdownContent(), ShowPlaymodeConfigPopup);
        }

        var isPlaying = EditorApplication.isPlayingOrWillChangePlaymode || PlayModeScenarioManager.IsScenarioRunningOrStarting;

        yield return new MainToolbarToggle(
            isPlaying
                ? new MainToolbarContent(EditorGUIUtility.LoadIcon("StopButton"), "Play")
                : new MainToolbarContent(EditorGUIUtility.LoadIcon("PlayButton"), "Play"),
            isPlaying,
            OnPlayButtonValueChanged)
        {
            populateContextMenu = PopulatePlayContextMenu,
            enabled = isScenarioValid
        };

        var pauseEnabled = PlayModeScenarioManager.ActiveScenario == null ||
                           ScenarioManagerProvider.instance.SupportsPauseAndStep;

        var scenarioStepEnabled = PlayModeScenarioManager.ActiveScenario == null
            ? EditorApplication.isPlaying
            : ScenarioManagerProvider.instance.SupportsPauseAndStep &&
              (ScenarioManagerProvider.instance.CurrentState == PlayModeScenarioState.Running || EditorApplication.isPlaying);

        yield return new MainToolbarToggle(
            EditorApplication.isPaused
                ? new MainToolbarContent(EditorGUIUtility.LoadIcon("PauseButton On"), "Pause")
                : new MainToolbarContent(EditorGUIUtility.LoadIcon("PauseButton"), "Pause"),
            EditorApplication.isPaused,
            OnPauseButtonValueChanged)
        {
            enabled = pauseEnabled
        };

        yield return new MainToolbarButton(new MainToolbarContent(EditorGUIUtility.LoadIcon("StepButton"), "Step"), OnStepButtonClicked)
        {
            enabled = scenarioStepEnabled,
        };
    }

    static void PopulatePlayContextMenu(DropdownMenu menu)
    {
        menu.AppendAction(
            L10n.Tr("Open Game View On Play", null),
            ChangeOpenGameViewOnPlayModeBehavior,
            PlayModeView.openWindowOnEnteringPlayMode ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
    }

    static void ChangeOpenGameViewOnPlayModeBehavior(DropdownMenuAction action)
    {
        PlayModeView.openWindowOnEnteringPlayMode = !PlayModeView.openWindowOnEnteringPlayMode;
    }

    static void OnPlayButtonValueChanged(bool value)
    {
        if (PlayModeScenarioManager.ScenarioTypesCount != 0)
        {
            if (value)
            {
                try
                {
                    ScenarioManagerProvider.instance.Start();
                }
                finally
                {
                    ScheduleRefresh();
                }
            }
            else
            {
                if (!PlayModeScenarioManager.IsScenarioRunningOrStarting)
                {
                    EditorApplication.isPlaying = false;
                }

                PlayModeScenarioManager.Stop();
            }
        }
        else
        {
            if (value)
            {
                EditorApplication.EnterPlaymode();
            }
            else
            {
                EditorApplication.ExitPlaymode();
            }
        }
    }

    static void OnPauseButtonValueChanged(bool value)
    {
        EditorApplication.isPaused = value;
    }

    static void OnStepButtonClicked()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.Step();
        }
    }

    static MainToolbarContent GetScenarioDropdownContent()
    {
        var activeScenario = PlayModeScenarioManager.ActiveScenario;
        if (activeScenario == null)
        {
            return new MainToolbarContent("No Scenario", EditorGUIUtility.FindTexture("UnityLogo"), "No active scenario");
        }

        var icon = activeScenario.Icon;
        var tooltip = string.IsNullOrEmpty(activeScenario.Description) ? "" : activeScenario.Description;

        if (!activeScenario.IsValid(out var error))
        {
            icon = EditorGUIUtility.FindTexture("console.warnicon");
            tooltip = error;
        }

        return new MainToolbarContent(activeScenario.name, icon, tooltip);
    }

    static void ShowPlaymodeConfigPopup(Rect buttonRect)
    {
        UnityEditor.PopupWindow.Show(
            new Rect(buttonRect.x + buttonRect.width - PlaymodePopupContent.windowSize.x, buttonRect.y, buttonRect.width, buttonRect.height),
            new PlaymodePopupContent());
    }
}
