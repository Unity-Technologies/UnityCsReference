// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

internal class NonCompliantPackageHelpBox : PackageBaseHelpBox
{
    public NonCompliantPackageHelpBox()
    {
        messageType = HelpBoxMessageType.Error;
    }

    public override void Refresh(IPackageVersion version)
    {
        var compliance = version?.package?.compliance;
        var isVisible = compliance != null && compliance.status != PackageComplianceStatus.Compliant;
        UIUtils.SetElementDisplay(this, isVisible);

        if (!isVisible)
            return;

        var message = compliance.violation.message;
        text = string.Format(L10n.Tr("The provider must revise this registry to comply with Unity's Terms of Service. Contact the provider for further assistance. {0}"), message);
        readMoreUrl = compliance.violation.readMoreLink;
    }
}
