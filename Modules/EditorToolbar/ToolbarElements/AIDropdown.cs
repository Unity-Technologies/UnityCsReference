// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Toolbars;

partial class AIDropdown : EditorToolbarDropdown
{
    [AutoStaticsCleanupOnCodeReload]
    // The main toolbar element factory Create() is called again when the toolbar is rebuilt after a code
    // reload and lazily recreates the dropdown, which re-assigns this slot from its constructor.
    [IgnoreForUAL0015("Toolbar element slot recreated by the main toolbar element factory on rebuild")]
    internal static AIDropdown instance;

    [UnityOnlyMainToolbarPreset]
    [MainToolbarElement("Services/AI", defaultDockIndex = 12, defaultDockPosition = MainToolbarDockPosition.Left)]
    static MainToolbarElement Create()
    {
        if (instance is null)
            instance = new AIDropdown();
        return new MainToolbarCustomElement(() => instance);
    }

    PopupWindowContent m_Content;
    static PopupWindowContent defaultContent => new AIDropdownContent();

    public AIDropdown()
    {
        name = "AIDropdown";
        text = L10n.Tr("AI", null);
        icon = EditorGUIUtility.FindTexture("AISparkle Icon");

        clicked += () =>
        {
            if (AIDropdownConfig.instance.config == null)
                EditorAIAssistantAnalytics.ReportAIDropdownOpenedEvent();

            #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
            PopupWindow.Show(worldBound, m_Content ??= defaultContent);
            #pragma warning restore UAL0015
        };

        #pragma warning disable UAL0015 // rebuilt/resubscribed wholesale on the next reload via this object's own lifecycle; a stale value in the interim is never observed
        instance = this;
        #pragma warning restore UAL0015
        RefreshContent();
    }

    internal void RefreshContent()
    {
        AIDropdownConfig.instance.config?.button?.Invoke(this);
        AIDropdownConfig.instance.config?.defaultContent?.Invoke(defaultContent);
        m_Content = AIDropdownConfig.instance.config?.content;
    }
}
