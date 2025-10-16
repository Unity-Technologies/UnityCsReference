// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor;

class EditorInstanceStatusElement : VisualElement
{
    const string k_PillContainerClass = "unity-instance-status__pill-container";
    const string k_PillClass = "unity-instance-status__pill";
    const string k_RolePillClass = "unity-instance-status__role-pill";
    const string k_TagPillClass = "unity-instance-status__tag-pill";

    public EditorInstanceStatusElement(EditorInstanceDescription instanceDescription)
    {
        var pillsContainer = new VisualElement();
        pillsContainer.AddToClassList(k_PillContainerClass);
        Add(pillsContainer);

        var rolePill = new Label(instanceDescription.RoleMask.ToString());
        rolePill.AddToClassList(k_PillClass, k_RolePillClass);
        pillsContainer.Add(rolePill);

        if (!string.IsNullOrEmpty(instanceDescription.PlayerTag))
        {
            var tagPill = new Label(instanceDescription.PlayerTag);
            tagPill.AddToClassList(k_PillClass, k_TagPillClass);
            pillsContainer.Add(tagPill);
        }

        if (instanceDescription is VirtualEditorInstanceDescription)
        {
            Add(CreateVirtualEditorContent(instanceDescription));
        }
    }

    static VisualElement CreateVirtualEditorContent(EditorInstanceDescription instanceDescription)
    {
        var container = new VisualElement();

        return container;
    }
}
