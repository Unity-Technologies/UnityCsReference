// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements;

/// <summary>
/// Shows deprecation for the build platform currently selected in a multi-target profile dropdown.
/// </summary>
internal sealed class SelectedPlatformDeprecationHelpBox
{
    readonly HelpBox m_HelpBox;

    internal SelectedPlatformDeprecationHelpBox(HelpBox helpBox) => m_HelpBox = helpBox;

    internal void Update(BuildProfile profile)
    {
        if (Util.TryGetSelectedBuildPlatformDeprecationGuid(profile, out var deprecatedPlatformGuid))
            Util.UpdatePlatformDeprecationHelpBox(m_HelpBox, deprecatedPlatformGuid);
        else
            m_HelpBox.Hide();
    }
}
