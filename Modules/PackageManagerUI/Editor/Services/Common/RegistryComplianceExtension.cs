// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal static class RegistryComplianceExtension
{
    public static bool IsEquivalentTo(this RegistryCompliance compliance, RegistryCompliance otherCompliance)
    {
        if (compliance.status != otherCompliance.status)
            return false;

        if (compliance.status == RegistryComplianceStatus.Compliant)
            return true;

        if (compliance.violations.Length != otherCompliance.violations.Length)
            return false;

        for (var i = 0; i < compliance.violations.Length; i++)
            if (!compliance.violations[i].IsEquivalentTo(otherCompliance.violations[i]))
                return false;

        return true;
    }
}
