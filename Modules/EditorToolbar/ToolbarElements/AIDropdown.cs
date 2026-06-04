// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Toolbars;

class AIDropdown : EditorToolbarDropdown
{
    internal static AIDropdown instance;

    [UnityOnlyMainToolbarPreset]
    [MainToolbarElement("Services/AI", defaultDockIndex = 12, defaultDockPosition = MainToolbarDockPosition.Left)]
    static MainToolbarElement Create()
    {
        if (instance is null)
            instance = new AIDropdown();
        return new MainToolbarCustom(() => instance);
    }

    PopupWindowContent m_Content;
    static PopupWindowContent defaultContent => new AIDropdownContent();

    public AIDropdown()
    {
        name = "AIDropdown";
        text = L10n.Tr("AI");
        icon = EditorGUIUtility.FindTexture("AISparkle Icon");

        clicked += () =>
        {
            if (AIDropdownConfig.instance.config == null)
                EditorAIAssistantAnalytics.ReportAIDropdownOpenedEvent();

            PopupWindow.Show(worldBound, m_Content ??= defaultContent);
        };

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
