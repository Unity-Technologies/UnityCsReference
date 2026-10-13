// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor.PackageManager
{
    [NativeHeader("Modules/PackageManager/Editor/PackageManagerTrustPolicySettings.h")]
    [StaticAccessor("PackageManagerTrustPolicySettingsBindings", StaticAccessorType.DoubleColon)]
    internal sealed class TrustPolicySettingsUtils
    {
        extern internal static int GetPolicyLevel();
        extern internal static void SetPolicyLevel(int level);
        extern internal static bool GetTrustDisabled();
        extern internal static void SetTrustDisabled(bool disabled);
    }
}
