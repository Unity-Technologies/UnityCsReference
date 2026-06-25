// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Burst.LowLevel;
using UnityEditor;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using System;

namespace Unity.Burst.Editor
{
    /// <summary>
    /// Register all menu entries for burst to the Editor
    /// </summary>
    internal static class BurstMenu
    {
        private const string EnableBurstCompilationText = "Jobs/Burst/Enable Compilation";
        private const string EnableSafetyChecksTextOff = "Jobs/Burst/Safety Checks/Off";
        private const string EnableSafetyChecksTextOn = "Jobs/Burst/Safety Checks/On";
        private const string EnableSafetyChecksTextForceOn = "Jobs/Burst/Safety Checks/Force On";
        private const string ForceSynchronousCompilesText = "Jobs/Burst/Synchronous Compilation";
        private const string EnableDebugCompilationText = "Jobs/Burst/Native Debug Mode Compilation";
        private const string ShowBurstTimingsText = "Jobs/Burst/Show Timings";
        private const string BurstInspectorText = "Jobs/Burst/Open Inspector...";

        // ----------------------------------------------------------------------------------------------
        // #1 Enable Compilation
        // ----------------------------------------------------------------------------------------------
        [MenuItem(EnableBurstCompilationText, false, 1)]
        private static void EnableBurstCompilation()
        {
            ChangeOptionSafely(() => BurstEditorOptions.EnableBurstCompilation = !BurstEditorOptions.EnableBurstCompilation);
        }

        [MenuItem(EnableBurstCompilationText, true, 1)]
        private static bool EnableBurstCompilationValidate()
        {
            Menu.SetChecked(EnableBurstCompilationText, BurstEditorOptions.EnableBurstCompilation);
            return BurstCompilerService.IsInitialized;
        }

        // ----------------------------------------------------------------------------------------------
        // #2 Safety Checks
        // ----------------------------------------------------------------------------------------------
        [MenuItem(EnableSafetyChecksTextOff, false, 1)]
        private static void EnableBurstSafetyChecksOff()
        {
            ChangeOptionSafely(() =>
            {
                BurstEditorOptions.EnableBurstSafetyChecks = false;
                BurstEditorOptions.ForceEnableBurstSafetyChecks = false;
            });
            Menu.SetChecked(EnableSafetyChecksTextOff, true);
            Menu.SetChecked(EnableSafetyChecksTextOn, false);
            Menu.SetChecked(EnableSafetyChecksTextForceOn, false);
        }

        [MenuItem(EnableSafetyChecksTextOff, true, 1)]
        private static bool EnableBurstSafetyChecksOffValidate()
        {
            Menu.SetChecked(EnableSafetyChecksTextOff, !BurstEditorOptions.EnableBurstSafetyChecks && !BurstEditorOptions.ForceEnableBurstSafetyChecks);
            return BurstCompilerService.IsInitialized && BurstEditorOptions.EnableBurstCompilation;
        }

        [MenuItem(EnableSafetyChecksTextOn, false, 2)]
        private static void EnableBurstSafetyChecksOn()
        {
            ChangeOptionSafely(() =>
            {
                BurstEditorOptions.EnableBurstSafetyChecks = true;
                BurstEditorOptions.ForceEnableBurstSafetyChecks = false;
            });
            Menu.SetChecked(EnableSafetyChecksTextOff, false);
            Menu.SetChecked(EnableSafetyChecksTextOn, true);
            Menu.SetChecked(EnableSafetyChecksTextForceOn, false);
        }

        [MenuItem(EnableSafetyChecksTextOn, true, 2)]
        private static bool EnableBurstSafetyChecksOnValidate()
        {
            Menu.SetChecked(EnableSafetyChecksTextOn, BurstEditorOptions.EnableBurstSafetyChecks && !BurstEditorOptions.ForceEnableBurstSafetyChecks);
            return BurstCompilerService.IsInitialized && BurstEditorOptions.EnableBurstCompilation;
        }

        [MenuItem(EnableSafetyChecksTextForceOn, false, 3)]
        private static void EnableBurstSafetyChecksForceOn()
        {
            ChangeOptionSafely(() =>
            {
                BurstEditorOptions.EnableBurstSafetyChecks = true;
                BurstEditorOptions.ForceEnableBurstSafetyChecks = true;
            });
            Menu.SetChecked(EnableSafetyChecksTextOff, false);
            Menu.SetChecked(EnableSafetyChecksTextOn, false);
            Menu.SetChecked(EnableSafetyChecksTextForceOn, true);
        }

        [MenuItem(EnableSafetyChecksTextForceOn, true, 3)]
        private static bool EnableBurstSafetyChecksForceOnValidate()
        {
            Menu.SetChecked(EnableSafetyChecksTextForceOn, BurstEditorOptions.ForceEnableBurstSafetyChecks);
            return BurstCompilerService.IsInitialized && BurstEditorOptions.EnableBurstCompilation;
        }

        // ----------------------------------------------------------------------------------------------
        // #3 Synchronous Compilation
        // ----------------------------------------------------------------------------------------------
        [MenuItem(ForceSynchronousCompilesText, false, 5)]
        private static void ForceSynchronousCompiles()
        {
            BurstEditorOptions.EnableBurstCompileSynchronously = !BurstEditorOptions.EnableBurstCompileSynchronously;
        }

        [MenuItem(ForceSynchronousCompilesText, true, 5)]
        private static bool ForceSynchronousCompilesValidate()
        {
            Menu.SetChecked(ForceSynchronousCompilesText, BurstEditorOptions.EnableBurstCompileSynchronously);
            return BurstCompilerService.IsInitialized && BurstEditorOptions.EnableBurstCompilation;
        }

        // ----------------------------------------------------------------------------------------------
        // #4 Synchronous Compilation
        // ----------------------------------------------------------------------------------------------
        [MenuItem(EnableDebugCompilationText, false, 6)]
        private static void EnableDebugMode()
        {
            ChangeOptionSafely(() =>
            {
                BurstEditorOptions.EnableBurstDebug = !BurstEditorOptions.EnableBurstDebug;
            });
        }

        [MenuItem(EnableDebugCompilationText, true, 6)]
        private static bool EnableDebugModeValidate()
        {
            Menu.SetChecked(EnableDebugCompilationText, BurstEditorOptions.EnableBurstDebug);
            return BurstCompilerService.IsInitialized && BurstEditorOptions.EnableBurstCompilation;
        }

        // ----------------------------------------------------------------------------------------------
        // #5 Show Timings
        // ----------------------------------------------------------------------------------------------
        [MenuItem(ShowBurstTimingsText, false, 7)]
        private static void ShowBurstTimings()
        {
            BurstEditorOptions.EnableBurstTimings = !BurstEditorOptions.EnableBurstTimings;
        }

        [MenuItem(ShowBurstTimingsText, true, 7)]
        private static bool ShowBurstTimingsValidate()
        {
            Menu.SetChecked(ShowBurstTimingsText, BurstEditorOptions.EnableBurstTimings);
            return BurstCompilerService.IsInitialized && BurstEditorOptions.EnableBurstCompilation;
        }

        // ----------------------------------------------------------------------------------------------
        // #6 Open Inspector...
        // ----------------------------------------------------------------------------------------------
        [MenuItem(BurstInspectorText, false, 8)]
        private static void BurstInspector()
        {
            // Get existing open window or if none, make a new one:
            BurstInspectorGUI window = EditorWindow.GetWindow<BurstInspectorGUI>("Burst Inspector");
            window.Show();
        }

        [MenuItem(BurstInspectorText, true, 8)]
        private static bool BurstInspectorValidate()
        {
            return BurstCompilerService.IsInitialized;
        }

        private static void ChangeOptionSafely(Action callback)
        {
            try
            {
                RequiresRestartUtility.CalledFromUI = true;

                callback();

                if (RequiresRestartUtility.RequiresRestart)
                {
                    EditorUtility.DisplayDialog(
                        "Editor Restart Required",
                        "This setting will not be applied until the Editor has been restarted. Please restart the Editor to continue.",
                        "OK");
                    BurstCompiler.Shutdown();
                }
            }
            finally
            {
                RequiresRestartUtility.RequiresRestart = false;
                RequiresRestartUtility.CalledFromUI = false;
            }
        }
    }
}
