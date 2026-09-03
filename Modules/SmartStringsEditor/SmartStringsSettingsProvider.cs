// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.UIElements.ProjectSettings;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.SmartStrings.Editor;

// Project Settings page (Project/Smart Strings) that shows the active settings, or a button to create them.
class SmartStringsSettingsProvider : AssetSettingsProvider
{
    const string k_ProjectSettingsStyleSheet = "StyleSheets/ProjectSettings/ProjectSettingsCommon.uss";

    public SmartStringsSettingsProvider()
        : base("Project/Smart Strings", () => SmartStringsEditorSettings.ActiveSettings)
    {
    }

    public override void OnActivate(string searchContext, VisualElement rootElement)
    {
        var root = new ScrollView
        {
            horizontalScrollerVisibility = ScrollerVisibility.Hidden,
            style = { marginLeft = 9, marginTop = 1 }
        };
        if (EditorGUIUtility.Load(k_ProjectSettingsStyleSheet) is StyleSheet styleSheet)
            root.styleSheets.Add(styleSheet);
        rootElement.Add(root);
        BuildContent(root);
    }

    static void BuildContent(VisualElement root)
    {
        root.Clear();
        root.Add(CreateTitleBar());

        var active = SmartStringsEditorSettings.ActiveSettings;
        if (active != null)
        {
            root.Add(new InspectorElement(active));
            return;
        }

        root.Add(new HelpBox(L10n.Tr("There are no active Smart Strings settings. Create one to customize the default formatter and include it in builds.", null), HelpBoxMessageType.Info));
        root.Add(new Button(() =>
        {
            var created = CreateSettingsAsset();
            if (created != null)
            {
                SmartStringsEditorSettings.ActiveSettings = created;
                BuildContent(root);
            }
        })
        {
            text = L10n.Tr("Create", null),
            style = { width = 100, marginTop = 4 }
        });
    }

    static VisualElement CreateTitleBar()
    {
        // The page root already insets its content, so drop the shared class's own left padding.
        var titleBar = new VisualElement { style = { paddingLeft = 0 } };
        titleBar.AddToClassList(ProjectSettingsTitleBar.Styles.k_TitleBarClassName);

        var title = new Label(L10n.Tr("Smart Strings", null));
        title.AddToClassList(ProjectSettingsTitleBar.Styles.k_TitleLabelClassName);
        titleBar.Add(title);

        return titleBar;
    }

    static SmartStringsSettings CreateSettingsAsset()
    {
        var path = EditorUtility.SaveFilePanelInProject(L10n.Tr("Create Smart Strings Settings", null), L10n.Tr("Smart Strings Settings", null), "asset",
            L10n.Tr("Choose where to save the Smart Strings settings asset.", null));
        if (string.IsNullOrEmpty(path))
            return null;

        var settings = ScriptableObject.CreateInstance<SmartStringsSettings>();
        settings.SmartFormatter = Smart.CreateDefaultSmartFormat();
        AssetDatabase.CreateAsset(settings, path);
        AssetDatabase.SaveAssets();
        return settings;
    }

    [SettingsProvider]
    static SettingsProvider CreateProvider() => new SmartStringsSettingsProvider();
}
