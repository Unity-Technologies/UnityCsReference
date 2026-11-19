// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Class containing build profile window utility functions.
    /// </summary>
    internal static class Util
    {
        internal const string k_StyleSheet = "BuildProfile/StyleSheets/BuildProfile.uss";
        internal const string k_UssClassDark = "build-profile-window--dark";
        internal const string k_UssClassLight = "build-profile-window--light";

        internal const string k_PY_MediumUssClass = "py-medium";

        /// <summary>
        /// Helper function for setting the platform settings helpbox based on
        /// license and module installation status.
        /// </summary>
        internal static bool UpdatePlatformRequirementsWarningHelpBox(HelpBox helpbox, GUID platformId)
        {
            if (helpbox == null)
                return false;

            ClearHelpBox(helpbox);

            if (!BuildProfileModuleUtil.IsBuildProfileLicensed(platformId))
            {
                BuildProfileModuleUtil.UpdateHelpBoxForLicenseNotFound(helpbox, platformId);
                helpbox.Show();
                return true;
            }

            if (!BuildProfileModuleUtil.IsModuleInstalled(platformId))
            {
                BuildProfileModuleUtil.UpdateHelpBoxForModuleNotInstalled(helpbox, platformId);
                helpbox.Show();
                return true;
            }

            if (!BuildProfileModuleUtil.IsBuildProfileSupported(platformId))
            {
                helpbox.text = TrText.notSupportedWarning;
                helpbox.Show();
                return true;
            }

            helpbox.Hide();
            return false;
        }

        /// <summary>
        /// Returns whether the build profile's platform is deprecated via <see cref="BuildProfile.platformGuid"/>.
        /// For multi-target build profiles, this will check the selected platform as well.
        /// </summary>
        internal static bool TryGetBuildProfileDeprecationPlatformGuid(BuildProfile profile, out GUID deprecatedPlatformGuid)
        {
            deprecatedPlatformGuid = default;

            if (profile == null)
                return false;

            if (BuildProfileModuleUtil.BuildPlatformTryGetDeprecationMessage(profile.platformGuid, out _))
            {
                deprecatedPlatformGuid = profile.platformGuid;
                return true;
            }

            if (profile.isMultiTarget && !profile.selectedPlatformGuid.Empty()
                && BuildProfileModuleUtil.BuildPlatformTryGetDeprecationMessage(profile.selectedPlatformGuid, out _))
            {
                deprecatedPlatformGuid = profile.selectedPlatformGuid;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Shows a warning when <paramref name="platformGuid"/> is marked deprecated.
        /// </summary>
        /// <returns>True when the help box is shown.</returns>
        internal static bool UpdatePlatformDeprecationHelpBox(HelpBox helpbox, GUID platformGuid)
        {
            if (helpbox == null)
                return false;

            ClearHelpBox(helpbox);

            if (!BuildProfileModuleUtil.BuildPlatformTryGetDeprecationMessage(platformGuid, out var message))
            {
                helpbox.Hide();
                return false;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                message = string.Format(
                    TrText.platformDeprecatedDefaultWithDisplayName,
                    BuildTargetDiscovery.BuildPlatformDisplayName(platformGuid));
            }

            helpbox.text = message;
            helpbox.Show();
            return true;
        }

        internal static bool UpdateSupportedPlatformStatusHelpBox(HelpBox helpbox, GUID platformGuid)
        {
            if (helpbox == null)
                return false;

            ClearHelpBox(helpbox);

            if (BuildProfileModuleUtil.UpdateHelpBoxForSupportedPlatformStatus(platformGuid, helpbox))
            {
                helpbox.Show();
                return true;
            }

            helpbox.Hide();
            return false;
        }

        static void ClearHelpBox(HelpBox helpbox)
        {
            if (helpbox == null)
                return;

            helpbox.text = string.Empty;
            helpbox.buttonText = string.Empty;
            helpbox.linkText = string.Empty;
            helpbox.linkHref = string.Empty;
        }

        internal static void ApplyActionState(this VisualElement elem, ActionState state)
        {
            switch (state)
            {
                case ActionState.Hidden:
                    elem.Hide();
                    elem.SetEnabled(false);
                    break;
                case ActionState.Disabled:
                    elem.Show();
                    elem.SetEnabled(false);
                    break;
                case ActionState.Enabled:
                    elem.Show();
                    elem.SetEnabled(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, nameof(elem));
            }
        }

        internal static void Hide(this VisualElement elem) => elem.style.display = DisplayStyle.None;
        internal static void Show(this VisualElement elem) => elem.style.display = DisplayStyle.Flex;
    }
}
