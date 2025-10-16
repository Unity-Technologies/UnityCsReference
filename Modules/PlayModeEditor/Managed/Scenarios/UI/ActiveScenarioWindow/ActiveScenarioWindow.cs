// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlayMode.Editor;

class ActiveScenarioWindow : EditorWindow
{
    const string k_Stylesheet = "PlayMode/UI/Framework.uss";
    const string k_WindowTitle = "Active Scenario";
    const string k_EditButtonText = "Edit Scenario";
    const string k_HeaderClassName = "unity-active-scenario__header";
    const string k_TitleBarClassName = "unity-active-scenario__title-bar";
    const string k_TitleLabelClassName = "unity-active-scenario__title-label";
    const string k_DescriptionLabelClassName = "unity-active-scenario__description-label";
    const string k_EditButtonClassName = "unity-active-scenario__edit-button";
    const string k_ContentClassName = "unity-active-scenario__content";

    internal static void OpenWindow()
    {
        GetWindow<ActiveScenarioWindow>(typeof(InspectorWindow));
    }

    void OnEnable()
    {
        titleContent = new GUIContent(k_WindowTitle);
        ScenarioManagerProvider.instance.ConfigAssetChanged -= Refresh;
        ScenarioManagerProvider.instance.ConfigAssetChanged += Refresh;
    }

    void OnDisable()
    {
        ScenarioManagerProvider.instance.ConfigAssetChanged -= Refresh;
        EditorApplication.delayCall -= Refresh;
    }

    void CreateGUI()
    {
        rootVisualElement.styleSheets.Add(EditorGUIUtility.LoadRequired(k_Stylesheet) as StyleSheet);
        Refresh();
    }

    void Refresh()
    {
        var activeScenario = PlayModeScenarioManager.ActiveScenario;

        rootVisualElement.Clear();

        var container = new ScrollView();

        container.Add(CreateHeader(activeScenario));
        container.Add(CreateContent(activeScenario));

        rootVisualElement.Add(container);
    }

    void ScheduleRefresh()
    {
        EditorApplication.delayCall -= Refresh;
        EditorApplication.delayCall += Refresh;
    }

    private VisualElement CreateHeader(PlayModeScenario scenario)
    {
        var container = new VisualElement();
        container.AddToClassList(k_HeaderClassName);

        var titleBar = new VisualElement();
        titleBar.AddToClassList(k_TitleBarClassName);

        var title = new Label(scenario.name);
        title.AddToClassList(k_TitleLabelClassName);

        var editButton = new Button(PlayModeScenariosWindow.ShowWindow) { text = k_EditButtonText };
        editButton.AddToClassList(k_EditButtonClassName);

        var description = new Label(scenario.Description);
        description.AddToClassList(k_DescriptionLabelClassName);

        titleBar.Add(title);
        titleBar.Add(editButton);
        container.Add(titleBar);
        container.Add(description);

        return container;
    }

    private VisualElement CreateContent(PlayModeScenario scenario)
    {
        var container = new VisualElement();
        container.AddToClassList(k_ContentClassName);
        container.Add(scenario.CreateScenarioUI());
        container.TrackSerializedObjectValue(new SerializedObject(scenario), _ => ScheduleRefresh());
        return container;
    }
}

