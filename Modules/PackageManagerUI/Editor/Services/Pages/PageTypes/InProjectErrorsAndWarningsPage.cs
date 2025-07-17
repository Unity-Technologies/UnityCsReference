// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal;

[Serializable]
internal class InProjectErrorsAndWarningsPage : InProjectPage
{
    public new const string k_Id = "ErrorsAndWarnings";

    public override string id => k_Id;
    public override string displayName => L10n.Tr("Errors & Warnings");
    public override Icon icon
    {
        get
        {
            if (m_PackageDatabase.allPackages.AnyMatches(package => base.ShouldInclude(package) && package.state == PackageState.Error))
                return Icon.Error;
            else
                return Icon.Warning;
        }
    }

    public InProjectErrorsAndWarningsPage(IPackageDatabase packageDatabase) : base(packageDatabase) { }

    public override bool ShouldInclude(IPackage package)
    {
        return base.ShouldInclude(package)
            && package.compliance.status == PackageComplianceStatus.Compliant
            && (package.state is PackageState.Error or PackageState.Warning);
    }
}
