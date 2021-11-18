// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;
using Application = UnityEngine.Device.Application;

namespace UnityEditor.Connect.Fallback
{
    class InstallPackageSection : IDisposable
    {
        SingleService m_ServiceInstance;
        Button m_InstallButton;
        PackageInstallationHandler m_PackageInstallationHandler;

        public event Action packageInstalled;

        public InstallPackageSection(VisualElement containerElement, SingleService service)
        {
            m_ServiceInstance = service;

            VisualElementUtils.AddUxmlToVisualElement(containerElement, VisualElementConstants.UxmlPaths.InstallPackageTemplate);
            SetupInstallMessage(containerElement, service.title);
            SetupInstallButton(containerElement);
            SetupInstallationHandler(service);
        }

        ~InstallPackageSection()
        {
            Dispose();
        }

        public void Dispose()
        {
            m_PackageInstallationHandler.Dispose();
        }

        static void SetupInstallMessage(VisualElement messageContainer, string packageTitle)
        {
            var messageTextElement = messageContainer.Q<TextElement>(className: VisualElementConstants.ClassNames.InstallMessage);
            if (messageTextElement != null)
            {
                messageTextElement.text = string.Format(messageTextElement.text, packageTitle);
            }
        }

        void SetupInstallButton(VisualElement buttonContainer)
        {
            m_InstallButton = buttonContainer.Q<Button>(className: VisualElementConstants.ClassNames.InstallButton);
            if (m_InstallButton != null)
            {
                m_InstallButton.clicked += InstallPackage;
                m_InstallButton.SetEnabled(false);
                SetInstallButtonTooltip(Tooltips.Choice.None);
            }
        }

        void SetInstallButtonTooltip(Tooltips.Choice tooltipChoice)
        {
            if (m_InstallButton != null)
            {
                m_InstallButton.tooltip = Tooltips.GetFormattedTooltip(tooltipChoice, m_ServiceInstance.title);
            }
        }

        void SetupInstallationHandler(SingleService singleService)
        {
            m_PackageInstallationHandler = new PackageInstallationHandler(singleService);
            m_PackageInstallationHandler.packageSearchComplete += OnPackageSearchComplete;
            m_PackageInstallationHandler.packageInstallationComplete += OnPackageInstallationComplete;

            if (IsInternetReachable())
            {
                m_PackageInstallationHandler.StartPackageSearch();
                SetInstallButtonTooltip(Tooltips.Choice.SearchingForPackage);
            }
            else
            {
                SetInstallButtonTooltip(Tooltips.Choice.InternetUnreachable);
            }
        }

        static bool IsInternetReachable()
        {
            return Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork;
        }

        void InstallPackage()
        {
            m_PackageInstallationHandler.StartInstallation();
        }

        void OnPackageSearchComplete(bool packageFound)
        {
            m_PackageInstallationHandler.packageSearchComplete -= OnPackageSearchComplete;
            m_InstallButton?.SetEnabled(packageFound);
            SetInstallButtonTooltip(packageFound ? Tooltips.Choice.None : Tooltips.Choice.CannotFindPackage);
        }

        void OnPackageInstallationComplete()
        {
            m_PackageInstallationHandler.packageInstallationComplete -= OnPackageInstallationComplete;
            packageInstalled?.Invoke();
        }

        static class Tooltips
        {
            const string k_SearchingForPackage = "Searching for the {0} package.";
            const string k_InternetUnreachable = "An internet connection is required to install the {0} package.";
            const string k_CannotFindPackage = "Could not find the {0} package";

            public enum Choice
            {
                None,
                SearchingForPackage,
                InternetUnreachable,
                CannotFindPackage
            }

            public static string GetFormattedTooltip(Choice choice, string serviceTitle)
            {
                var tooltip = string.Empty;
                var translatedServiceTitle = L10n.Tr(serviceTitle);
                var translatedTooltipText = string.Empty;
                switch (choice)
                {
                    case Choice.InternetUnreachable:
                        translatedTooltipText = L10n.Tr(k_InternetUnreachable);
                        break;
                    case Choice.SearchingForPackage:
                        translatedTooltipText = L10n.Tr(k_SearchingForPackage);
                        break;
                    case Choice.CannotFindPackage:
                        translatedTooltipText = L10n.Tr(k_CannotFindPackage);
                        break;
                }

                if (choice != Choice.None)
                {
                    tooltip = string.Format(translatedTooltipText, translatedServiceTitle);
                }

                return tooltip;
            }
        }
    }
}
