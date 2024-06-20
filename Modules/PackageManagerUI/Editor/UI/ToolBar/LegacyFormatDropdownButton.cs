// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

internal class LegacyFormatDropdownButton : BaseDropdownButton<GenericDropdownMenu>, IPackageToolBarButton
{
    private static readonly string k_InProjectText = L10n.Tr("In Project");

    protected override int numDropdownItems => menu?.items.Count ?? 0;
    protected override void ShowDropdown() => menu?.DropDown(worldBound, this, true, true);

    private readonly IList<PackageAction> m_Actions;
    public event Action onActionTriggered
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

    public VisualElement element => this;

    public LegacyFormatDropdownButton(IPackageOperationDispatcher operationDispatcher,
        IAssetStoreDownloadManager assetStoreDownloadManager,
        IUnityConnectProxy unityConnect,
        IApplicationProxy application)
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
    }

    public void Refresh(IPackageVersion version)
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
        ClearClickedEvents();

        var mainActionIndex = FindMainActionIndex(visibleItems, true);
        if (mainActionIndex == -1)
        {
            if (version.importedAssets?.Any() == true)
            {
                SetIcon(Icon.Installed);
                text = k_InProjectText;
                mainButton.tooltip = string.Empty;
                mainButton.SetEnabled(true);
            }
            else
                // It is not possible that FindMainActionIndex returns -1 for both when isRecommended is true and false, so we can know for sure that
                // we will find a valid mainAction index for this second round
                mainActionIndex = FindMainActionIndex(visibleItems, false);
        }

        if (mainActionIndex != -1)
        {
            var mainItem = visibleItems[mainActionIndex];
            clicked += () => mainItem.action.TriggerAction(version);
            text = mainItem.text;
            SetIcon(mainItem.action.icon);
            mainButton.tooltip = mainItem.tooltip;
            mainButton.SetEnabled((mainItem.state & PackageActionState.Disabled) == PackageActionState.None);
        }

        // We need to create a new DropdownMenu every time instead of using the "Hidden" status of DropdownMenuAction,
        // because there's no API to change the DropdownMenuAction class after creating it.
        var dropdownMenu = new GenericDropdownMenu();
        for (var i = 0; i < visibleItems.Count; ++i)
        {
            if (i == mainActionIndex)
                continue;

            var item = visibleItems[i];
            var itemEnabled = (item.state & PackageActionState.Disabled) == PackageActionState.None;
            dropdownMenu.AppendAction(item.text, itemEnabled, _ => item.action.TriggerAction(version), tooltip: item.tooltip);
        }
        menu = dropdownMenu;
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
    public void Refresh(IEnumerable<IPackage> packages)
    {
        // Do nothing since this button is not available for multi-select
    }
}
