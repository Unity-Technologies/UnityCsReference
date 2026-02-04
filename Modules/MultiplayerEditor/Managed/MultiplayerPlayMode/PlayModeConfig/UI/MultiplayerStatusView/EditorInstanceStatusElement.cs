// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Multiplayer.Internal;
using UnityEngine.Multiplayer.Internal;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor;

class EditorInstanceStatusElement : VisualElement
{
    internal const string k_PillContainerClass = "unity-instance-status__pill-container";
    internal const string k_PillClass = "unity-instance-status__pill";
    internal const string k_RolePillClass = "unity-instance-status__role-pill";
    internal const string k_TagPillClass = "unity-instance-status__tag-pill";

    public EditorInstanceStatusElement(MultiplayerRoleFlags role, string playerTag)
    {
        var pillsContainer = new VisualElement();
        pillsContainer.AddToClassList(k_PillContainerClass);
        Add(pillsContainer);

        if (EditorMultiplayerManager.enableMultiplayerRoles)
        {
            var rolePill = new Label(role.ToString());
            rolePill.AddToClassList(k_PillClass, k_RolePillClass);
            pillsContainer.Add(rolePill);
        }

        if (!string.IsNullOrEmpty(playerTag))
        {
            var tagPill = new Label(playerTag);
            tagPill.AddToClassList(k_PillClass, k_TagPillClass);
            pillsContainer.Add(tagPill);
        }
    }
}
