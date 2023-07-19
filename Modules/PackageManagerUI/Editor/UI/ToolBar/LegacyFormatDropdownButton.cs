// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

internal class LegacyFormatDropdownButton : PackageToolBarButton
{
    private static readonly string k_InProjectText = L10n.Tr("In Project");

    private readonly DropdownButton m_DropdownButton;

    private readonly IList<PackageAction> m_Actions;
    public override event Action onActionTriggered
    {
        add
        {
            foreach (var action in m_Actions)
                action.onActionTriggered += value;
        }
        remove
        {
            foreach (var action in m_Actions)
                action.onActionTriggered -= value;
        }
    }

    public LegacyFormatDropdownButton(PackageOperationDispatcher operationDispatcher,
        AssetStoreDownloadManager assetStoreDownloadManager,
        UnityConnectProxy unityConnect,
        ApplicationProxy application)
    {
        // We use the order of the list to determine which action to show in priority as the main action.
        // E.g. If DownloadUpdate and Import are available actions, we will show DownloadUpdate as the main action,
        // because it was added first to the list.
        m_Actions = new PackageAction[]
        {
            new DownloadNewAction(operationDispatcher, assetStoreDownloadManager, unityConnect, application),
            new DownloadUpdateAction(operationDispatcher, assetStoreDownloadManager, unityConnect, application),
            new ImportNewAction(operationDispatcher, assetStoreDownloadManager, application, unityConnect),
            new ImportUpdateAction(operationDispatcher, assetStoreDownloadManager, application, unityConnect),
            new ReImportAction(operationDispatcher, assetStoreDownloadManager, application, unityConnect),
            new RemoveImportedAction(operationDispatcher, application),
            new ReDownloadAction(operationDispatcher, assetStoreDownloadManager, unityConnect, application),
        };

        name = "legacyFormatDropdownButton";
        m_DropdownButton = new DropdownButton();
        Add(m_DropdownButton);
    }

    public override void Refresh(IPackageVersion version)
    {
        // We do this early return for performance reasons. This avoids doing expensive calls to GetActionState for multiple actions.
        if (version?.HasTag(PackageTag.LegacyFormat) != true)
        {
            UIUtils.SetElementDisplay(this, false);
            return;
        }

        var visibleItems = FindVisibleActions(version);
        if (visibleItems.Count == 0)
        {
            UIUtils.SetElementDisplay(this, false);
            return;
        }
        UIUtils.SetElementDisplay(this, true);
        m_DropdownButton.ClearClickedEvents();

        var mainActionIndex = FindMainActionIndex(visibleItems, true);
        if (mainActionIndex == -1)
        {
            if (version.importedAssets?.Any() == true)
            {
                m_DropdownButton.SetIcon(Icon.Installed);
                m_DropdownButton.text = k_InProjectText;
                m_DropdownButton.mainButton.tooltip = string.Empty;
                m_DropdownButton.mainButton.SetEnabled(true);
            }
            else
                // It is not possible that FindMainActionIndex returns -1 for both when isRecommended is true and false, so we can know for sure that
                // we will find a valid mainAction index for this second round
                mainActionIndex = FindMainActionIndex(visibleItems, false);
        }

        if (mainActionIndex != -1)
        {
            var mainItem = visibleItems[mainActionIndex];
            m_DropdownButton.clicked += () => mainItem.action.TriggerAction(version);
            m_DropdownButton.text = mainItem.text;
            m_DropdownButton.SetIcon(mainItem.action.icon);
            m_DropdownButton.mainButton.tooltip = mainItem.tooltip;
            m_DropdownButton.mainButton.SetEnabled((mainItem.state & PackageActionState.Disabled) == PackageActionState.None);
        }

        // We need to create a new DropdownMenu every time instead of using the "Hidden" status of DropdownMenuAction,
        // because there's no API to change the DropdownMenuAction class after creating it.
        var dropdownMenu = new DropdownMenu();
        for (var i = 0; i < visibleItems.Count; ++i)
        {
            if (i == mainActionIndex)
                continue;

            var item = visibleItems[i];
            dropdownMenu.AppendAction(item.text, _ => item.action.TriggerAction(version), a =>
            {
                a.tooltip = item.tooltip;
                return (item.state & PackageActionState.Disabled) == PackageActionState.None ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
            });
        }
        m_DropdownButton.menu = dropdownMenu;
    }

    private List<(PackageAction action, string text, string tooltip, PackageActionState state)> FindVisibleActions(IPackageVersion version)
    {
        var result = new List<(PackageAction action, string text, string tooltip, PackageActionState state)>();
        foreach (var action in m_Actions)
        {
            var state = action.GetActionState(version, out var text, out var actionTooltip);
            if ((state & PackageActionState.Visible) != PackageActionState.None)
                result.Add((action, text, actionTooltip, state));
        }
        return result;
    }

    private int FindMainActionIndex(IList<(PackageAction action, string text, string tooltip, PackageActionState state)> visibleItems, bool isRecommended)
    {
        for (var i = 0; i < visibleItems.Count; ++i)
            if (visibleItems[i].action.isRecommended == isRecommended && (visibleItems[i].state & PackageActionState.DisabledForPackage) == PackageActionState.None)
                return i;
        for (var i = 0; i < visibleItems.Count; ++i)
            if (visibleItems[i].action.isRecommended == isRecommended)
                return i;
        return -1;
    }

    [ExcludeFromCodeCoverage]
    public override void Refresh(IEnumerable<IPackageVersion> versions)
    {
        // Do nothing since this button is not available for multi-select
    }
}
