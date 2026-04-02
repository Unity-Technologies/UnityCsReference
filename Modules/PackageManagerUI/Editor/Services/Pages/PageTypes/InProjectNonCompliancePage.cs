// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal;

[Serializable]
internal class InProjectNonCompliancePage: InProjectPage
{
    public new const string k_Id = "NonCompliance";

    public override string id => k_Id;
    public override string displayName => L10n.Tr("Restricted Packages");
    public override Icon icon => Icon.Error;

    public override bool visible => visualStates.countTotal > 0;

    protected override bool updateWhenInactive => true;

    public InProjectNonCompliancePage(IPackageDatabase packageDatabase) : base(packageDatabase) { }

    protected override void RebuildVisualStateList()
    {
        var oldVisibility = visible;

        base.RebuildVisualStateList();

        if (oldVisibility != visible)
            TriggerOnStateChange();
    }

    public override bool ShouldInclude(IPackage package)
    {
        return base.ShouldInclude(package) && package.state == PackageState.Restricted;
    }
}
