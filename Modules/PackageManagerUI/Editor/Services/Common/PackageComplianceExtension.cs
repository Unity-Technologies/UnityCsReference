// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal static class PackageComplianceExtension
{
    public static bool IsEquivalentTo(this PackageCompliance compliance, PackageCompliance otherCompliance)
    {
        if (compliance.status != otherCompliance.status)
            return false;

        if (compliance.status == PackageComplianceStatus.Compliant)
            return true;

        return compliance.violation.IsEquivalentTo(otherCompliance.violation);
    }
}
