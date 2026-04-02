// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal;

[Serializable]
internal class InProjectErrorsAndWarningsPage : InProjectPage
{
    public new const string k_Id = "ErrorsAndWarnings";

    public override string id => k_Id;
    public override string displayName => L10n.Tr("Errors and Warnings");

    [SerializeField]
    private Icon m_Icon;
    public override Icon icon => m_Icon;

    public override bool visible => visualStates.countTotal > 0;

    protected override bool updateWhenInactive => true;

    protected override void RebuildVisualStateList()
    {
        var oldVisibility = visible;
        var oldIcon = m_Icon;

        base.RebuildVisualStateList();

        var errorsInPage = visualStates.AnyMatches(v => m_PackageDatabase.GetPackage(v.itemUniqueId)?.state == PackageState.Error);
        m_Icon = errorsInPage ? Icon.Error : Icon.Warning;

        if (oldVisibility != visible || oldIcon != m_Icon)
            TriggerOnStateChange();
    }

    public InProjectErrorsAndWarningsPage(IPackageDatabase packageDatabase) : base(packageDatabase) { }

    public override bool ShouldInclude(IPackage package)
    {
        return base.ShouldInclude(package) && package.state is PackageState.Error or PackageState.Warning;
    }
}
