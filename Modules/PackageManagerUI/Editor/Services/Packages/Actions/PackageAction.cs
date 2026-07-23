// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal;

internal abstract class PackageAction: ActionBase<IPackageVersion, IPackage>
{
    protected static readonly string k_InProgressGenericTooltip = L10n.Tr("This action is currently in progress.");

    // For now, this is only used for LegacyFormat dropdown button.
    public virtual bool isRecommended => false;

    public virtual Icon icon => Icon.None;

    // By default buttons does not support bulk action
    protected override bool TriggerActionImplementation(IReadOnlyCollection<IPackage> packages) => false;
    public virtual string GetMultiSelectText(IPackageVersion version, bool isInProgress) => GetText(version, isInProgress);

    public override ToolbarButtonBase<IPackageVersion, IPackage> CreateToolbarButton()
    {
        return new PackageToolBarSimpleButton(this);
    }
}
