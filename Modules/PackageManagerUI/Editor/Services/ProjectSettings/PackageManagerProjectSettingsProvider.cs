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

        internal const string k_PackageManagerSettingsPath = "Project/Package Manager";
        const string k_GeneralServicesTemplatePath = "UXML/PackageManager/PackageManagerProjectSettings.uxml";
        protected VisualTreeAsset m_GeneralTemplate;

        private static readonly string k_Message =
            "Preview packages are in the early stage of development and not yet ready for production.\n" +
            "We recommend using these only for testing purpose and to give us direct feedback.";

        private PackageManagerProjectSettings m_Settings;

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

                m_Settings = PackageManagerProjectSettings.instance;

                SetExpanded(m_Settings.advancedSettingsExpanded);
                advancedSettingsFoldout.RegisterValueChangedCallback(changeEvent =>
                {
                    if (changeEvent.target == advancedSettingsFoldout)
                        SetExpanded(changeEvent.newValue);
                });

                previewInfoBox.Q<Button>().clickable.clicked += () =>
                {
                    var unityVersionParts = Application.unityVersion.Split('.');
                    Application.OpenURL($"https://docs.unity3d.com/{unityVersionParts[0]}.{unityVersionParts[1]}/Documentation/Manual/pack-preview.html");
                };

                enablePreviewPackages.SetValueWithoutNotify(m_Settings.enablePreviewPackages);
                enablePreviewPackages.RegisterValueChangedCallback(changeEvent =>
                {
                    var newValue = changeEvent.newValue;

                    if (newValue != m_Settings.enablePreviewPackages)
                    {
                        var saveIt = true;
                        if (newValue && !m_Settings.oneTimeWarningShown)
                        {
                            if (EditorUtility.DisplayDialog(L10n.Tr("Package Manager"), L10n.Tr(k_Message), L10n.Tr("I understand"), L10n.Tr("Cancel")))
                                m_Settings.oneTimeWarningShown = true;
                            else
                                saveIt = false;
                        }

                        if (saveIt)
                        {
                            m_Settings.enablePreviewPackages = newValue;
                            m_Settings.Save();
                            PackageManagerWindowAnalytics.SendEvent("togglePreviewPackages");
                        }
                    }
                    enablePreviewPackages.SetValueWithoutNotify(m_Settings.enablePreviewPackages);
                });

                enablePackageDependencies.SetValueWithoutNotify(m_Settings.enablePackageDependencies);
                enablePackageDependencies.RegisterValueChangedCallback(changeEvent =>
                {
                    enablePackageDependencies.SetValueWithoutNotify(changeEvent.newValue);
                    var newValue = changeEvent.newValue;

                    if (newValue != m_Settings.enablePackageDependencies)
                    {
                        m_Settings.enablePackageDependencies = newValue;
                        m_Settings.Save();
                        PackageManagerWindowAnalytics.SendEvent("toggleDependencies");
                    }
                });
            };
        }

        [SettingsProvider]
        public static SettingsProvider CreateProjectSettingsProvider()
        {
            return new PackageManagerProjectSettingsProvider(k_PackageManagerSettingsPath, SettingsScope.Project, new List<string>(new[] { "enable", "package", "preview" }));
        }

        private void SetExpanded(bool expanded)
        {
            if (advancedSettingsFoldout.value != expanded)
                advancedSettingsFoldout.value = expanded;
            if (m_Settings.advancedSettingsExpanded != expanded)
                m_Settings.advancedSettingsExpanded = expanded;
        }

        private VisualElementCache cache { get; set; }

        private HelpBox previewInfoBox { get { return cache.Get<HelpBox>("previewInfoBox"); } }
        private Toggle enablePreviewPackages { get { return rootVisualElement.Q<Toggle>("enablePreviewPackages"); } }
        private Toggle enablePackageDependencies { get { return rootVisualElement.Q<Toggle>("enableDependencies"); } }
        private Foldout advancedSettingsFoldout { get { return rootVisualElement.Q<Foldout>("advancedSettingsFoldout"); } }
    }
}
