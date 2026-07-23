// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class NonCompliantPackageHelpBox : PackageBaseHelpBox
{
    public NonCompliantPackageHelpBox(IApplicationProxy application) : base(application)
    {
        customIcon = Icon.PackageErrorLarge;
        readMoreAnalyticsId = "non-compliant-package-help-box";
    }

    public override void Refresh(IPackageVersion version)
    {
        var compliance = version?.package?.compliance;
        var isVisible = compliance != null && compliance.status != PackageComplianceStatus.Compliant;
        UIUtils.SetElementDisplay(this, isVisible);

        if (!isVisible)
            return;

        text = compliance.violation.message;
        readMoreUrl = compliance.violation.readMoreLink;
    }
}
