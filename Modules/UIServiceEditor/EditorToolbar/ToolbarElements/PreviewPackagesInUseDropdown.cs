// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Package Manager/PreviewPackagesInUse", typeof(DefaultMainToolbar))]
    sealed class PreviewPackagesInUseDropdown : ToolbarButton
    {
        private const int k_MinWidthChangePreviewPackageInUseToIcon = 1235;

        private bool m_IsPreviewPackagesInUse;

        [NonSerialized]
        private PackageManagerPrefs m_PackageManagerPrefs;
        [NonSerialized]
        private ApplicationProxy m_ApplicationProxy;
        [NonSerialized]
        private UpmClient m_UpmClient;

        private const string previewLabel = "unity-editor-toolbar-element-preview-packages-in-use__label";
        private const string previewIcon = "unity-editor-toolbar-preview-package-in-use__icon";
        private const string previewArrowIcon = "unity-icon-arrow-preview-packages-in-use";

        public PreviewPackagesInUseDropdown()
        {
            m_PackageManagerPrefs = ServicesContainer.instance.Resolve<PackageManagerPrefs>();
            m_ApplicationProxy = ServicesContainer.instance.Resolve<ApplicationProxy>();
            m_UpmClient = ServicesContainer.instance.Resolve<UpmClient>();

            name = "PreviewPackagesInUseDropdown";

            AddToClassList("unity-toolbar-button-preview-packages-in-use");

            AddTextElement(this).text = L10n.Tr("Experimental Packages In Use");
            AddIconElement(this);
            AddArrowElement(this);
            clicked += () => ShowUserMenu(worldBound);

            RegisterCallback<GeometryChangedEvent>(OnSizeChanged);
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            RefreshIsPreviewPackagesInUse();
            CheckAvailability();
        }

        private void RegisteredPackagesEventHandler(PackageRegistrationEventArgs packageRegistrationEventArgs)
        {
            RefreshIsPreviewPackagesInUse();
        }

        private void RefreshIsPreviewPackagesInUse()
        {
            m_IsPreviewPackagesInUse = m_UpmClient.IsAnyExperimentalPackagesInUse();
        }

        internal static void AddArrowElement(VisualElement target)
        {
            var arrow = new VisualElement();
            arrow.AddToClassList(previewArrowIcon);
            target.Add(arrow);
        }

        internal static TextElement AddTextElement(VisualElement target)
        {
            var label = new TextElement();
            label.AddToClassList(previewLabel);
            target.Add(label);
            return label;
        }

        internal static VisualElement AddIconElement(VisualElement target)
        {
            var icon = new VisualElement();
            icon.AddToClassList(previewIcon);
            target.Add(icon);
            target.AddToClassList("icon");
            return icon;
        }

        void OnSizeChanged(GeometryChangedEvent evt)
        {
            if (m_IsPreviewPackagesInUse && !m_PackageManagerPrefs.dismissPreviewPackagesInUse)
            {
                var useIcon = Toolbar.get.window.position.width < k_MinWidthChangePreviewPackageInUseToIcon;
                EnableInClassList("icon", useIcon);
            }
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            EditorApplication.update += CheckAvailability;
            PackageManager.Events.registeredPackages += RegisteredPackagesEventHandler;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            EditorApplication.update -= CheckAvailability;
            PackageManager.Events.registeredPackages -= RegisteredPackagesEventHandler;
        }

        void CheckAvailability()
        {
            style.display = m_IsPreviewPackagesInUse && !m_PackageManagerPrefs.dismissPreviewPackagesInUse ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void ShowUserMenu(Rect dropDownRect)
        {
            var menu = new GenericMenu();

            menu.AddItem(EditorGUIUtility.TrTextContent("Dismiss for now"), false, () =>
            {
                m_PackageManagerPrefs.dismissPreviewPackagesInUse = true;
                style.display = DisplayStyle.None;
            });
            menu.AddSeparator("");

            menu.AddItem(EditorGUIUtility.TrTextContent("Show Experimental Packages..."), false, () =>
            {
                PackageManagerWindow.SelectPackageAndFilterStatic(string.Empty, PackageFilterTab.InProject, true, "experimental");
            });
            menu.AddSeparator("");

            menu.AddItem(EditorGUIUtility.TrTextContent("Why am I seeing this?"), false, () =>
            {
                m_ApplicationProxy.OpenURL($"https://docs.unity3d.com/{m_ApplicationProxy.shortUnityVersion}/Documentation/Manual/pack-exp.html");
            });

            menu.DropDown(dropDownRect, true);
        }
    }
}
