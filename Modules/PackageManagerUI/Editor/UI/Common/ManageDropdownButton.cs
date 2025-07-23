// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class ManageDropdownButton : BaseDropdownButton<GenericDropdownMenu>, IPackageToolBarButton
    {
        private IPackageVersion m_Version;
        private readonly IList<PackageAction> m_Actions;

        private readonly IApplicationProxy m_Application;
        private readonly IUpmCache m_UpmCache;
        private readonly IPackageManagerPrefs m_PackageManagerPrefs;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IPackageOperationDispatcher m_OperationDispatcher;
        private readonly IPageManager m_PageManager;
        private readonly IIOProxy m_IOProxy;
        private readonly ISelectionProxy m_SelectionProxy;
        private readonly IAssetDatabaseProxy m_AssetDatabaseProxy;

        public ManageDropdownButton(IApplicationProxy applicationProxy,
                                    IUpmCache upmCache,
                                    IPackageManagerPrefs packageManagePrefs,
                                    IPackageDatabase packageDatabase,
                                    IPackageOperationDispatcher operationDispatcher,
                                    IPageManager pageManager,
                                    IIOProxy ioProxy,
                                    ISelectionProxy selectionProxy,
                                    IAssetDatabaseProxy assetDatabaseProxy)
        {
            m_Application = applicationProxy;
            m_UpmCache = upmCache;
            m_PackageManagerPrefs = packageManagePrefs;
            m_PackageDatabase = packageDatabase;
            m_OperationDispatcher = operationDispatcher;
            m_PageManager = pageManager;
            m_IOProxy = ioProxy;
            m_SelectionProxy = selectionProxy;
            m_AssetDatabaseProxy = assetDatabaseProxy;

            m_Actions = new List<PackageAction>
            {
				new CustomizeAction(m_OperationDispatcher, m_Application),
                new OpenManifestAction(m_IOProxy, m_SelectionProxy, m_AssetDatabaseProxy),
                new OpenManifestExternallyAction(m_IOProxy),
                new RemoveAction(m_OperationDispatcher, m_Application, m_PackageManagerPrefs, m_PackageDatabase, m_PageManager),
                new RemoveCustomAction(m_OperationDispatcher, m_Application),
                new ResetAction(m_OperationDispatcher, m_Application, m_PackageDatabase, m_PageManager),
                new UnlockAction(m_PageManager),
                new UpdateAction(m_OperationDispatcher, m_Application, m_PackageDatabase, m_PageManager),
                new GitUpdateAction(m_OperationDispatcher, m_UpmCache, m_Application)
            };

            name = "manageDropdown";
        }

        public void Refresh(IPackageVersion version)
        {
            m_Version = version;
            var visibleActions = new List<PackageAction>();

            foreach (var action in m_Actions)
                if (action.IsVisible(version))
                    visibleActions.Add(action);

            UIUtils.SetElementDisplay(this, visibleActions.Count > 0);

            if (visibleActions.Count > 1)
                SetupDropdownMenu(visibleActions);
            else if (visibleActions.Count == 1)
                SetupSingleActionButton(visibleActions[0]);
        }

        public void Refresh(IEnumerable<IPackage> packages)
        {
            // Do nothing since this button is not available for multi-select
        }

        private void SetupDropdownMenu(IReadOnlyCollection<PackageAction> actions)
        {
            ClearClickedEvents();

            menu = new GenericDropdownMenu();
            foreach (var action in actions)
            {
                var state = action.GetActionState(m_Version, out var actionText, out var actionTooltip);
                var enabled = (state & PackageActionState.Disabled) == PackageActionState.None;
                menu.AppendAction(actionText, enabled, _ =>
                    {
                        action.TriggerAction(m_Version);
                        onActionTriggered?.Invoke();
                    }
                    , actionTooltip);
            }

            // Workaround for a UIToolkit bug [UUM-92991] that causes highlight flickering when the content width doesn't match the overall item width
            foreach (var menuItem in menu.items)
                menuItem.element[0].style.width = Length.Percent(100);

            tooltip = string.Empty;
            text = L10n.Tr("Manage");
            SetEnabled(true);

            alwaysShowDropdown = true;
        }

        private void SetupSingleActionButton(PackageAction action)
        {
            menu = null;

            ClearClickedEvents();

            clicked += () =>
            {
                action.TriggerAction(m_Version);
                onActionTriggered?.Invoke();
            };

            var state = action.GetActionState(m_Version, out var actionText, out var actionTooltip);
            var enabled = (state & PackageActionState.Disabled) == PackageActionState.None;

            text = actionText;
            tooltip = actionTooltip;
            SetEnabled(enabled);

            alwaysShowDropdown = false;
        }

        protected override void ShowDropdown()
        {
            menu?.DropDown(worldBound, this, true, true);
        }

        protected override int numDropdownItems => menu?.items.Count ?? 0;
        public event Action onActionTriggered;
        public VisualElement element => this;
    }
}
