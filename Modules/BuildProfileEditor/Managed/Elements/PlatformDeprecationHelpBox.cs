// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements;

/// <summary>
/// Shows platform deprecation for a build profile using the inspector <see cref="HelpBox"/> from
/// <c>BuildProfileEditor.uxml</c>. Updated in-place; no reparenting.
/// </summary>
internal sealed class PlatformDeprecationHelpBox
{
    readonly HelpBox m_InspectorHelpBox;

    internal PlatformDeprecationHelpBox(HelpBox inspectorHelpBox)
    {
        m_InspectorHelpBox = inspectorHelpBox;
    }

    internal void Update(BuildProfile profile)
    {
        if (Util.TryGetBuildProfileDeprecationPlatformGuid(profile, out var deprecatedPlatformGuid))
            Util.UpdatePlatformDeprecationHelpBox(m_InspectorHelpBox, deprecatedPlatformGuid);
        else
            m_InspectorHelpBox.Hide();
    }
}
