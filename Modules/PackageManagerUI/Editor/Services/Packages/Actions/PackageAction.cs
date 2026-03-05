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

    public override ActionState GetActionState(IPackageVersion version, out string text, out string tooltip)
    {
        if (!IsVisible(version))
        {
            text = string.Empty;
            tooltip = string.Empty;

            if (IsHiddenWhenInProgress(version) && IsInProgress(version))
                return ActionState.InProgress;
            return ActionState.None;
        }

        var isInProgress = IsInProgress(version);
        text = GetText(version, isInProgress);
        if (isInProgress)
        {
            tooltip = GetTooltip(version, true);
            return ActionState.Visible | ActionState.DisabledForItem | ActionState.InProgress;
        }

        var disableCondition = GetActiveDisableCondition(version);
        if (disableCondition != null)
        {
            tooltip = disableCondition.tooltip;
            return ActionState.Visible | ActionState.DisabledForItem;
        }

        var temporaryDisableCondition = GetActiveTemporaryDisableCondition();
        if (temporaryDisableCondition != null)
        {
            tooltip = temporaryDisableCondition.tooltip;
            return ActionState.Visible | ActionState.DisabledTemporarily;
        }

        tooltip = GetTooltip(version, false);
        return ActionState.Visible;
    }

    // By default buttons does not support bulk action
    protected override bool TriggerActionImplementation(IReadOnlyCollection<IPackage> packages) => false;

    public abstract bool IsInProgress(IPackageVersion version);

    protected virtual bool IsHiddenWhenInProgress(IPackageVersion version) => false;

    public abstract bool IsVisible(IPackageVersion version);

    public virtual string GetMultiSelectText(IPackageVersion version, bool isInProgress) => GetText(version, isInProgress);

    // Temporary disable conditions refer to conditions that are temporary and not related to the state of a package
    // For example, when the network is lost or when there are scripting compiling
    protected virtual IEnumerable<DisableCondition> GetAllTemporaryDisableConditions() => Array.Empty<DisableCondition>();
    public virtual DisableCondition GetActiveTemporaryDisableCondition()
    {
        return GetAllTemporaryDisableConditions().FirstMatch(condition => condition.active);
    }

    protected virtual IEnumerable<DisableCondition> GetAllDisableConditions(IPackageVersion version) => Array.Empty<DisableCondition>();

    public virtual DisableCondition GetActiveDisableCondition(IPackageVersion version)
    {
        return GetAllDisableConditions(version).FirstMatch(condition => condition.active);
    }

    public override ToolbarButtonBase<IPackageVersion, IPackage> CreateToolbarButton()
    {
        return new PackageToolBarSimpleButton(this);
    }
}
