// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal;

[Flags]
internal enum PackageActionState : uint
{
    None = 0,
    Visible = 1 << 0,
    DisabledTemporarily = 1 << 1,
    DisabledForPackage = 1 << 2,
    InProgress = 1 << 3,

    Disabled = DisabledTemporarily | DisabledForPackage
}

internal abstract class PackageAction
{
    protected static readonly string k_InProgressGenericTooltip = L10n.Tr("This action is currently in progress.");

    public Action onActionTriggered;

    // For now, this is only used for LegacyFormat dropdown button.
    public virtual bool isRecommended => false;

    public virtual Icon icon => Icon.None;

    public virtual PackageActionState GetActionState(IPackageVersion version, out string text, out string tooltip)
    {
        if (!IsVisible(version))
        {
            text = string.Empty;
            tooltip = string.Empty;

            if (IsHiddenWhenInProgress(version) && IsInProgress(version))
                return PackageActionState.InProgress;
            return PackageActionState.None;
        }

        var isInProgress = IsInProgress(version);
        text = GetText(version, isInProgress);
        if (isInProgress)
        {
            tooltip = GetTooltip(version, true);
            return PackageActionState.Visible | PackageActionState.DisabledForPackage | PackageActionState.InProgress;
        }

        var disableCondition = GetActiveDisableCondition(version);
        if (disableCondition != null)
        {
            tooltip = disableCondition.tooltip;
            return PackageActionState.Visible | PackageActionState.DisabledForPackage;
        }

        var temporaryDisableCondition = GetActiveTemporaryDisableCondition();
        if (temporaryDisableCondition != null)
        {
            tooltip = temporaryDisableCondition.tooltip;
            return PackageActionState.Visible | PackageActionState.DisabledTemporarily;
        }

        tooltip = GetTooltip(version, false);
        return PackageActionState.Visible;
    }

    public void TriggerAction(IPackageVersion version)
    {
        if (TriggerActionImplementation(version))
            onActionTriggered?.Invoke();
    }
    // Returns true if the action is triggered, false otherwise.
    protected abstract bool TriggerActionImplementation(IPackageVersion version);

    public void TriggerAction(IList<IPackageVersion> versions)
    {
        if (TriggerActionImplementation(versions))
            onActionTriggered?.Invoke();
    }
    // By default buttons does not support bulk action
    protected virtual bool TriggerActionImplementation(IList<IPackageVersion> versions) => false;

    public abstract bool IsInProgress(IPackageVersion version);

    protected virtual bool IsHiddenWhenInProgress(IPackageVersion version) => false;

    public abstract bool IsVisible(IPackageVersion version);

    public abstract string GetTooltip(IPackageVersion version, bool isInProgress);

    public abstract string GetText(IPackageVersion version, bool isInProgress);

    public virtual string GetMultiSelectText(IPackageVersion version, bool isInProgress) => GetText(version, isInProgress);

    // Temporary disable conditions refer to conditions that are temporary and not related to the state of a package
    // For example, when the network is lost or when there are scripting compiling
    protected virtual IEnumerable<DisableCondition> GetAllTemporaryDisableConditions() => Enumerable.Empty<DisableCondition>();
    public virtual DisableCondition GetActiveTemporaryDisableCondition()
    {
        return GetAllTemporaryDisableConditions().FirstOrDefault(condition => condition.active);
    }

    protected virtual IEnumerable<DisableCondition> GetAllDisableConditions(IPackageVersion version) => Enumerable.Empty<DisableCondition>();

    public virtual DisableCondition GetActiveDisableCondition(IPackageVersion version)
    {
        return GetAllDisableConditions(version).FirstOrDefault(condition => condition.active);
    }
}
