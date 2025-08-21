// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    /// <summary>
    /// Describes the Free Running Mode state of an Instance
    /// </summary>
    enum RunModeState
    {
        ScenarioControl,
        ManualControl,
    }

    static class RunMode
    {
        internal static String IconName(this RunModeState state)
        {
            switch (state)
            {
                case RunModeState.ScenarioControl:
                    return "Linked";
                case RunModeState.ManualControl:
                    return "Unlinked";
                default:
                    throw new NotSupportedException($"No Icons for state: {state}");
            }
        }

        internal static void SetRunModeIcon(this Image imageElement, RunModeState state)
        {
            var iconResName = state.IconName();

            // If there's no image yet set, simply set it
            if (imageElement.image == null)
            {
                imageElement.image = EditorGUIUtility.FindTexture(iconResName);
                return;
            }

            // Else, compare the current texture name and set the image if needed.
            // To do this, strip out any additional dark mode prefixes (that Unity
            // Engine adds) before performing the compare.
            var regex = new Regex(Regex.Escape("d_"));
            var currImageName = regex.Replace(imageElement.image.name, "", 1);
            if (!currImageName.Equals(iconResName))
                imageElement.image = EditorGUIUtility.FindTexture(iconResName);
        }
    }
}
