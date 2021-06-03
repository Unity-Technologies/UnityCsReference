// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Services/Cloud", typeof(DefaultMainToolbar))]
    sealed class CloudButton : EditorToolbarButton
    {
        public CloudButton() : base(OpenCloudWindow)
        {
            name = "Cloud";

            icon = EditorGUIUtility.FindTexture("CloudConnect");
            tooltip = L10n.Tr("Manage services");

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            EditorApplication.update += CheckAvailability;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            EditorApplication.update -= CheckAvailability;
        }

        void CheckAvailability()
        {
            style.display = MPE.ProcessService.level == MPE.ProcessLevel.Main ? DisplayStyle.Flex : DisplayStyle.None;
        }

        static void OpenCloudWindow()
        {
            Connect.ServicesEditorWindow.ShowServicesWindow();
        }
    }
}
