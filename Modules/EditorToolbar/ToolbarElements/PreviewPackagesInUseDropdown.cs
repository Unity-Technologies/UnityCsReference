// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEditor.PackageManager.UI.Internal;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Package Manager/PreviewPackagesInUse", typeof(DefaultMainToolbar))]
    sealed class PreviewPackagesInUseDropdown : ToolbarButton
    {
        private const int k_FullTextRequiredWidth = 207;

        private bool m_IsPreviewPackagesInUse;

        [NonSerialized]
        private IApplicationProxy m_ApplicationProxy;
        [NonSerialized]
        private IUpmClient m_UpmClient;
        [NonSerialized]
        private IProjectSettingsProxy m_SettingsProxy;

        private const string previewLabel = "unity-editor-toolbar-element-preview-packages-in-use__label";
        private const string previewIcon = "unity-editor-toolbar-preview-package-in-use__icon";
        private const string previewArrowIcon = "unity-icon-arrow-preview-packages-in-use";

        private static readonly string k_ExpPackagesInUseText = L10n.Tr("Experimental Packages In Use");

        public PreviewPackagesInUseDropdown()
        {
            m_ApplicationProxy = ServicesContainer.instance.Resolve<IApplicationProxy>();
            m_UpmClient = ServicesContainer.instance.Resolve<IUpmClient>();
            m_SettingsProxy = ServicesContainer.instance.Resolve<IProjectSettingsProxy>();

            name = "PreviewPackagesInUseDropdown";

            AddToClassList("unity-toolbar-button-preview-packages-in-use");

            tooltip = k_ExpPackagesInUseText;

            AddTextElement(this).text = k_ExpPackagesInUseText;
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
            if (m_IsPreviewPackagesInUse && !m_SettingsProxy.dismissPreviewPackagesInUse)
            {
                var toolbarRightAlign = parent;
                var allButtonsExcludingPreviewDropdownWidth = toolbarRightAlign.Children().Where(button => button.name != "PreviewPackagesInUseDropdown").Sum(button =>
                    button.rect.width + button.resolvedStyle.paddingRight + button.resolvedStyle.paddingLeft + button.resolvedStyle.marginLeft
                    + button.resolvedStyle.marginRight + button.resolvedStyle.borderLeftWidth + button.resolvedStyle.borderRightWidth);

                var useIcon = toolbarRightAlign.rect.width - allButtonsExcludingPreviewDropdownWidth < k_FullTextRequiredWidth;
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
            style.display = m_IsPreviewPackagesInUse && !m_SettingsProxy.dismissPreviewPackagesInUse ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void ShowUserMenu(Rect dropDownRect)
        {
            var menu = new GenericMenu();

            menu.AddItem(EditorGUIUtility.TrTextContent("Dismiss"), false, () =>
            {
                m_SettingsProxy.dismissPreviewPackagesInUse = true;
                m_SettingsProxy.Save();
                style.display = DisplayStyle.None;
            });
            menu.AddSeparator("");

            menu.AddItem(EditorGUIUtility.TrTextContent("Show Experimental Packages..."), false, () =>
            {
                PackageManagerWindow.OpenAndSelectPage(InProjectPage.k_Id, "experimental");
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
