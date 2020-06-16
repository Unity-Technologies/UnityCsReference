// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageManagerProjectSettingsProvider : SettingsProvider
    {
        protected VisualElement rootVisualElement { get; private set; }

        const string k_GeneralServicesTemplatePath = "UXML/PackageManager/PackageManagerProjectSettings.uxml";
        protected VisualTreeAsset m_GeneralTemplate;

        private static readonly string k_Message =
            "Preview packages are in the early stage of development and not yet ready for production.\n" +
            "We recommend using these only for testing purpose and to give us direct feedback.";

        internal static class StylesheetPath
        {
            internal static readonly string projectSettings = "StyleSheets/PackageManager/PackageManagerProjectSettings.uss";
            internal static readonly string projectSettingsDark = "StyleSheets/PackageManager/Dark.uss";
            internal static readonly string projectSettingsLight = "StyleSheets/PackageManager/Light.uss";
            internal static readonly string packageManagerCommon = "StyleSheets/PackageManager/Common.uss";
            internal static readonly string stylesheetCommon = "StyleSheets/Extensions/base/common.uss";
            internal static readonly string stylesheetDark = "StyleSheets/Extensions/base/dark.uss";
            internal static readonly string stylesheetLight = "StyleSheets/Extensions/base/light.uss";
        }

        public PackageManagerProjectSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
            path = "Project/Package Manager";
            scopes = SettingsScope.Project;
            keywords = new List<string>(new[] { "enable", "package", "preview" });
            activateHandler = (s, element) =>
            {
                // Create a child to make sure all the style sheets are not added to the root.
                rootVisualElement = new ScrollView();
                rootVisualElement.AddStyleSheetPath(StylesheetPath.projectSettings);
                rootVisualElement.AddStyleSheetPath(EditorGUIUtility.isProSkin ? StylesheetPath.projectSettingsDark : StylesheetPath.projectSettingsLight);
                rootVisualElement.AddStyleSheetPath(StylesheetPath.packageManagerCommon);
                rootVisualElement.AddStyleSheetPath(EditorGUIUtility.isProSkin ? StylesheetPath.stylesheetDark : StylesheetPath.stylesheetLight);
                rootVisualElement.AddStyleSheetPath(StylesheetPath.stylesheetCommon);

                element.Add(rootVisualElement);

                m_GeneralTemplate = EditorGUIUtility.Load(k_GeneralServicesTemplatePath) as VisualTreeAsset;

                VisualElement newVisualElement = new VisualElement();
                m_GeneralTemplate.CloneTree(newVisualElement);
                rootVisualElement.Add(newVisualElement);

                cache = new VisualElementCache(rootVisualElement);

                previewInfoBox.Q<Button>().clickable.clicked += () =>
                {
                    var applicationProxy = ServicesContainer.instance.Resolve<ApplicationProxy>();
                    applicationProxy.OpenURL($"https://docs.unity3d.com/{applicationProxy.shortUnityVersion}/Documentation/Manual/pack-preview.html");
                };

                var settings = ServicesContainer.instance.Resolve<PackageManagerProjectSettingsProxy>();
                enablePreviewPackages.SetValueWithoutNotify(settings.enablePreviewPackages);

                enablePreviewPackages.RegisterValueChangedCallback(changeEvent =>
                {
                    enablePreviewPackages.SetValueWithoutNotify(changeEvent.newValue);
                    var newValue = changeEvent.newValue;

                    if (newValue != settings.enablePreviewPackages)
                    {
                        var saveIt = true;
                        if (newValue && !settings.oneTimeWarningShown)
                        {
                            if (EditorUtility.DisplayDialog(L10n.Tr("Package Manager"), L10n.Tr(k_Message), L10n.Tr("I understand"), L10n.Tr("Cancel")))
                                settings.oneTimeWarningShown = true;
                            else
                                saveIt = false;
                        }

                        if (saveIt)
                        {
                            settings.enablePreviewPackages = newValue;
                            settings.Save();
                        }
                    }
                });
            };
        }

        [SettingsProvider]
        public static SettingsProvider CreateProjectSettingsProvider()
        {
            return new PackageManagerProjectSettingsProvider("Project/Package Manager", SettingsScope.Project, new List<string>(new[] { "enable", "package", "preview" }));
        }

        private VisualElementCache cache { get; set; }

        private HelpBox previewInfoBox { get { return cache.Get<HelpBox>("previewInfoBox"); } }
        private Toggle enablePreviewPackages { get { return rootVisualElement.Q<Toggle>("enablePreviewPackages"); } }
    }
}
