// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars;

[EditorToolbarElement("Services/AI", typeof(DefaultMainToolbar))]
class AIDropdown : EditorToolbarDropdown
{
    internal static AIDropdown instance;

    PopupWindowContent m_Content;
    static PopupWindowContent defaultContent => new AIDropdownContent();

    public AIDropdown()
    {
        // Only showing the button in developer mode until beta period where packages to install will be published.
        style.display = Unsupported.IsDeveloperMode() ? DisplayStyle.Flex : DisplayStyle.None;

        name = "AIDropdown";
        text = L10n.Tr("AI");
        icon = EditorGUIUtility.FindTexture("AISparkle Icon");

        clicked += () => PopupWindow.Show(worldBound, m_Content ??= defaultContent);

        instance = this;
        RefreshContent();
    }

    internal void RefreshContent()
    {
        AIDropdownConfig.instance.config?.button?.Invoke(this);
        AIDropdownConfig.instance.config?.defaultContent?.Invoke(defaultContent);
        m_Content = AIDropdownConfig.instance.config?.content;
    }
}
