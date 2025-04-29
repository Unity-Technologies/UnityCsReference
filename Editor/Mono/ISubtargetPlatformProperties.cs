// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Build.Profile;

namespace UnityEditor;

internal interface ISubtargetPlatformProperties : IPlatformProperties
{
    int GetSubtargetFromPlatformSettings(BuildProfilePlatformSettingsBase platformSettings = null) => -1;
}
