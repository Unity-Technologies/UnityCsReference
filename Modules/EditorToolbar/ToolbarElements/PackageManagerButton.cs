// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.PackageManager.UI;
using UnityEditor.PackageManager.UI.Internal;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Package Management/Package Manager", typeof(DefaultMainToolbar))]
    sealed class PackageManagerButton : ToolbarButton
    {
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly VisualElement m_Icon;
        private Action m_ClickAction;

        public PackageManagerButton()
        {
            m_PackageDatabase = ServicesContainer.instance.Resolve<IPackageDatabase>();
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            name = "PackageManager";

            m_Icon = new VisualElement { name = "image" };
            Add(m_Icon);
            AddToClassList("unity-toolbar-button-package-manager");

            clicked += OnPackageManagerButtonClicked;

            RefreshState();
        }

        private void OnAttachedToPanel(AttachToPanelEvent _)
        {
            m_PackageDatabase.onPackagesChanged += OnPackagesChanged;
        }

        private void OnPackagesChanged(PackagesChangeArgs _)
        {
            RefreshState();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent _)
        {
            m_PackageDatabase.onPackagesChanged -= OnPackagesChanged;
        }

        private void RefreshState()
        {
            m_Icon.ClearClassList();

            if (m_PackageDatabase.AnyNonCompliantPackagesInUse())
            {
                m_Icon.AddToClassList("error");
                tooltip = L10n.Tr("Restricted Packages In Use");
                m_ClickAction = () => PackageManagerWindow.OpenAndSelectPage(InProjectNonCompliancePage.k_Id);
            }
            else if (m_PackageDatabase.AnyExperimentalPackagesInUse())
            {
                m_Icon.AddToClassList("warning");
                tooltip = L10n.Tr("Experimental Packages In Use");
                m_ClickAction = () => PackageManagerWindow.OpenAndSelectPage(InProjectPage.k_Id, "experimental");
            }
            else
            {
                m_Icon.AddToClassList("default");
                tooltip = L10n.Tr("Package Manager");
                m_ClickAction = () => PackageManagerWindow.OpenAndSelectPackage(null);
            }
        }

        private void OnPackageManagerButtonClicked()
        {
            m_ClickAction?.Invoke();
        }
    }
}
