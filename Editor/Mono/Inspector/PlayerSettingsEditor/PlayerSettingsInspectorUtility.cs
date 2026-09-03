// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// Utility methods for opening the player settings at a specific setting.
    /// </summary>
    public static partial class PlayerSettingsInspectorUtility
    {
        /// <summary>
        /// The top-level sections of the Player settings, in the order they are shown in the inspector.
        /// </summary>
        public enum Section
        {
            /// <summary>The Icon section.</summary>
            Icon = 0,
            /// <summary>The Resolution and Presentation section.</summary>
            ResolutionAndPresentation = 1,
            /// <summary>The Splash Image section.</summary>
            SplashImage = 2,
            /// <summary>The Debugging and crash reporting section.</summary>
            DebuggingAndCrashReporting = 3,
            /// <summary>The Other Settings section.</summary>
            OtherSettings = 4,
            /// <summary>The Publishing Settings section.</summary>
            PublishingSettings = 5,
        }

        [AutoStaticsCleanupOnCodeReload]
        static EditorApplication.CallbackFunction s_ActiveReveal;

        /// <summary>
        /// Opens the player settings, expands the given section, and scrolls to the given setting.
        /// </summary>
        /// <remarks>
        /// Opens the Player section of the Project Settings window on the active build target's tab. When the active build profile has its own player settings, opens the Build Profiles window instead, without scrolling. The setting is found by the label it draws in the expanded section, so it only reaches settings that are directly visible there: settings nested inside a foldout within the section, sections added by platform extensions, and sections or settings that don't exist for the active platform are not reachable. When the setting is not found, the window still opens and a warning is logged.
        /// </remarks>
        /// <param name="section">The section that contains the setting.</param>
        /// <param name="settingLabel">The label of the setting, as shown in the inspector, in English.</param>
        /// <exception cref="ArgumentException">Thrown when the setting label is null or empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the section is not a valid <see cref="Section"/> value.</exception>
        public static void OpenAndScrollTo(Section section, string settingLabel)
        {
            if (string.IsNullOrEmpty(settingLabel))
                throw new ArgumentException("The setting label can't be null or empty.", nameof(settingLabel));

            // Decouples the published enum values from the accordion draw order in PlayerSettingsEditor.OnInspectorGUI.
            int sectionIndex = section switch
            {
                Section.Icon => 0,
                Section.ResolutionAndPresentation => 1,
                Section.SplashImage => 2,
                Section.DebuggingAndCrashReporting => 3,
                Section.OtherSettings => 4,
                Section.PublishingSettings => 5,
                _ => throw new ArgumentOutOfRangeException(nameof(section)),
            };

            if (BuildProfileContext.ProjectHasActiveProfileWithPlayerSettings())
            {
                // The active build profile owns the player settings; the global page can't change the effective values.
                BuildPipeline.ShowBuildProfileWindow();
                return;
            }

            var window = SettingsService.OpenProjectSettings("Project/Player");
            Debug.Assert(window != null, "The Project Settings window could not be opened.");
            var windowTitle = window.titleContent.text;

            foreach (var platform in BuildPlatforms.instance.GetValidPlatforms(true))
            {
                if (platform.IsActive() && !platform.IsSelected())
                {
                    platform.Select();
                    break;
                }
            }

            // The Highlighter matches the drawn label, so translate here where the editor's localization group applies.
            var localizedLabel = L10n.Tr(settingLabel, null);

            if (s_ActiveReveal != null)
                #pragma warning disable UAL0018 // unregistering a possibly-stale delegate is a harmless no-op; nothing is retained past this call
                EditorApplication.update -= s_ActiveReveal;
                #pragma warning restore UAL0018

            // Retry until the freshly opened window has laid out and painted the player settings.
            double deadline = EditorApplication.timeSinceStartup + 5.0;
            EditorApplication.CallbackFunction attempt = null;
            attempt = () =>
            {
                bool lastAttempt = EditorApplication.timeSinceStartup > deadline;
                bool done = lastAttempt;
                foreach (var editor in Resources.FindObjectsOfTypeAll<PlayerSettingsEditor>())
                {
                    if (!editor.IsActivePlayerSettingsEditor())
                        continue;

                    editor.ShowSection(sectionIndex);
                    done = Highlighter.ScrollTo(windowTitle, localizedLabel, HighlightSearchMode.Auto, lastAttempt) || lastAttempt;
                    break;
                }

                if (done)
                {
                    EditorApplication.update -= attempt;
                    if (s_ActiveReveal == attempt)
                        s_ActiveReveal = null;
                }
            };
            s_ActiveReveal = attempt;
            EditorApplication.update += attempt;
        }
    }
}
