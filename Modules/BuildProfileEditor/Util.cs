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
        internal const string k_PY_MediumUssClass = "py-medium";
        const int k_HelBoxChildItemCount = 2;

        /// <summary>
        /// Helper function for setting the platform settings helpbox based on
        /// license and module installation status.
        /// </summary>
        internal static bool UpdatePlatformRequirementsWarningHelpBox(HelpBox target, GUID platformId)
        {
            if (target.childCount > k_HelBoxChildItemCount)
            {
                // Remove extra element added by this method. HelpBox default is
                // two elements.
                target[2].RemoveFromHierarchy();
            }

            var licenseNotFoundElement = BuildProfileModuleUtil.CreateLicenseNotFoundElement(platformId);
            if (licenseNotFoundElement is not null)
            {
                target.text = string.Empty;
                target.Add(licenseNotFoundElement);
                target.Show();
                return true;
            }

            if (!BuildProfileModuleUtil.IsModuleInstalled(platformId))
            {
                target.text = string.Empty;
                target.Add(BuildProfileModuleUtil.CreateModuleNotInstalledElement(platformId));
                target.Show();
                return true;
            }

            if (!BuildProfileModuleUtil.IsBuildProfileSupported(platformId))
            {
                target.text = TrText.notSupportedWarning;
                target.Show();
                return true;
            }

            target.Hide();
            return false;
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
