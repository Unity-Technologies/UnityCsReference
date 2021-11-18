// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Connect.Fallback
{
    class FallbackProjectSettings : SettingsProvider
    {
        InstallPackageSection m_InstallPackageSection;

        public FallbackProjectSettings(SingleService service, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(service.projectSettingsPath, scopes, keywords)
        {
            activateHandler += Initialize;

            void Initialize(string s, VisualElement element)
            {
                AddStyleSheets(element);
                AddGeneralTemplate(element, service.title);

                var scrollView = new ScrollView();
                var scrollContainer = element.Q(className: VisualElementConstants.ClassNames.ScrollContainer);
                if (scrollContainer != null)
                {
                    scrollContainer.Add(scrollView);
                }

                AddInstallPackageSection(scrollView, service);

                TranslateStringsInTree(element);
            }
        }

        static void TranslateStringsInTree(VisualElement rootElement)
        {
            rootElement.Query<TextElement>().ForEach((label) => label.text = L10n.Tr(label.text));
        }

        static void AddStyleSheets(VisualElement styleSheetElement)
        {
            styleSheetElement.AddStyleSheetPath(ServicesUtils.StylesheetPath.servicesWindowCommon);
            styleSheetElement.AddStyleSheetPath(EditorGUIUtility.isProSkin ? ServicesUtils.StylesheetPath.servicesWindowDark : ServicesUtils.StylesheetPath.servicesWindowLight);
            styleSheetElement.AddStyleSheetPath(ServicesUtils.StylesheetPath.servicesCommon);
            styleSheetElement.AddStyleSheetPath(EditorGUIUtility.isProSkin ? ServicesUtils.StylesheetPath.servicesDark : ServicesUtils.StylesheetPath.servicesLight);
            styleSheetElement.AddStyleSheetPath(VisualElementConstants.StyleSheetPaths.PackageInstallation);
        }

        static void AddGeneralTemplate(VisualElement templateContainer, string serviceTitle)
        {
            VisualElementUtils.AddUxmlToVisualElement(templateContainer, VisualElementConstants.UxmlPaths.GeneralServicesTemplate);
            SetupTitle(templateContainer, serviceTitle);
        }

        static void SetupTitle(VisualElement labelsContainer, string serviceTitle)
        {
            var titleTextElement = labelsContainer.Q<TextElement>(className: VisualElementConstants.ClassNames.ServiceTitle);
            if (titleTextElement != null)
            {
                titleTextElement.text = serviceTitle;
            }
        }

        void AddInstallPackageSection(VisualElement installPackageContainer, SingleService service)
        {
            m_InstallPackageSection = new InstallPackageSection(installPackageContainer, service);
            m_InstallPackageSection.packageInstalled += Repaint;
        }
    }
}
