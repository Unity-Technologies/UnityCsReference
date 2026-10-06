// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor;

internal class ScenarioControllerStatusElement : VisualElement
{
    internal const string k_TitleLabelName = "scenario-controller-title-label";
    internal const string k_TitleCustomName = "scenario-controller-title-custom";
    internal const string k_ContentName = "scenario-controller-content";

    internal static ScenarioControllerStatusElement Create(PlayModeController controller)
    {
        var titleBarUI = controller.CreateTitleBarUI(null);
        var contentUI = controller.CreateControllerUI(null);

        if (titleBarUI == null && contentUI == null)
            return null;

        return new ScenarioControllerStatusElement(controller.name, titleBarUI, contentUI);
    }

    ScenarioControllerStatusElement(string title, VisualElement titleBarUI, VisualElement contentUI)
    {
        var foldout = new Foldout();
        foldout.AddToClassList(InstanceStatusElement.k_InstanceFoldoutClass);
        foldout.viewDataKey = $"{nameof(ScenarioControllerStatusElement)}.{title}";

        var titlebar = foldout.Q<Toggle>();
        titlebar.AddToClassList(InstanceStatusElement.k_InstanceFoldoutTitleBarClass);
        titlebar.ElementAt(0).AddToClassList(InstanceStatusElement.k_InstanceFoldoutTitleCheckmarkClass);
        titlebar.Add(CreateTitleContent(title, titleBarUI));

        var content = CreateContent(contentUI);
        if (content != null)
        {
            foldout.Q("unity-content").AddToClassList(InstanceStatusElement.k_InstanceFoldoutContentClass);
            foldout.Add(content);
        }

        Add(foldout);
    }

    static VisualElement CreateTitleContent(string title, VisualElement titleBarUI)
    {
        var titleContent = new VisualElement();
        titleContent.AddToClassList(InstanceStatusElement.k_InstanceFoldoutTitleContentClass);

        var label = new Label(title) { name = k_TitleLabelName };
        label.AddToClassList(InstanceStatusElement.k_InstanceFoldoutTitleLabelClass);

        var custom = new VisualElement() { name = k_TitleCustomName };
        custom.AddToClassList(InstanceStatusElement.k_InstanceFoldoutTitleCustomClass);
        custom.Add(titleBarUI);

        titleContent.Add(label);
        titleContent.Add(custom);

        return titleContent;
    }

    static VisualElement CreateContent(VisualElement contentUI)
    {
        if (contentUI == null)
            return null;

        var container = new VisualElement() { name = k_ContentName };
        container.AddToClassList(InstanceStatusElement.k_InstanceInspectorClass);
        container.AddToClassList("unity-inspector-element");
        container.Add(contentUI);
        return container;
    }
}
