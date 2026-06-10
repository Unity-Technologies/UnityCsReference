// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Services/AI", typeof(DefaultMainToolbar))]
    sealed class AIDropdown : EditorToolbarDropdown
    {
        // Shared with com.unity.ai.assistant's AIToolbarButtonLegacy.k_ButtonClassName.
        const string k_AIButtonClassName = "ai-toolbar-button-legacy";
        const string k_ShowAIButtonPrefKey = "Unity.AI.ShowAIButton";
        const string k_AIAssistantPackageName = "com.unity.ai.assistant";

        AIDropdownContent m_Content;

        public AIDropdown()
        {
            name = "AIDropdown";
            text = L10n.Tr("AI");
            tooltip = L10n.Tr("Open AI dropdown");
            icon = EditorGUIUtility.FindTexture("AISparkle Icon");

            clicked += OnClicked;

            if (IsAIAssistantPackagePresent())
            {
                // Hide immediately — do NOT add k_AIButtonClassName or the package's dup-check would
                // find this button, skip injection, and leave nothing after deferred removal.
                // Deferred removal avoids a native crash (LayoutList.Dispose) from removing inside AttachToPanelEvent.
                style.display = DisplayStyle.None;
                RegisterCallback<AttachToPanelEvent>(_ => schedule.Execute(() =>
                {
                    m_Content = null;
                    RemoveFromHierarchy();
                }));
            }
            else
            {
                AddToClassList(k_AIButtonClassName);

                if (!EditorPrefs.GetBool(k_ShowAIButtonPrefKey, true))
                    style.display = DisplayStyle.None;

                RegisterCallback<MouseDownEvent>(OnRightClick);
            }
        }

        void OnClicked()
        {
            // Bail if the button has been scheduled for removal (package detected but defer hasn't fired).
            if (panel == null || resolvedStyle.display == DisplayStyle.None)
                return;

            // Package is absent when this button is alive; fire the engine-side analytics.
            EditorAIAssistantAnalytics.ReportAIDropdownOpenedEvent();
            PopupWindow.Show(worldBound, m_Content ??= new AIDropdownContent());
        }

        void OnRightClick(MouseDownEvent evt)
        {
            if (evt.button != 1)
                return;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent(L10n.Tr("Hide")), false, () =>
            {
                EditorPrefs.SetBool(k_ShowAIButtonPrefKey, false);
                style.display = DisplayStyle.None;
            });
            menu.ShowAsContext();
        }

        static bool IsAIAssistantPackagePresent() => PackageManager.PackageInfo.FindForPackageName(k_AIAssistantPackageName) != null;
    }
}
