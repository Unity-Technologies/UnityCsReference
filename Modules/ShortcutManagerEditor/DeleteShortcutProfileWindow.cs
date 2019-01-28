// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShortcutManagement
{
    class DeleteShortcutProfileWindow : EditorWindow
    {
        private TextElement m_HeaderTextElement;
        private TextElement m_MessageTextElement;
        Button m_SubmitButton;
        Action m_Action;

        public static void Show(string profileName, Action action)
        {
            var deleteProfilePromptWindow = GetWindow<DeleteShortcutProfileWindow>(true, "Delete Profile", true);

            deleteProfilePromptWindow.minSize = new Vector2(320, 120);

            deleteProfilePromptWindow.m_HeaderTextElement.text = "Delete a shortcut profile";
            deleteProfilePromptWindow.m_MessageTextElement.text = string.Format("Are you sure you want to delete the shortcut profile '{0}'?", profileName);
            deleteProfilePromptWindow.m_Action = action;

            deleteProfilePromptWindow.ShowModal();
        }

        void OnEnable()
        {
            // Load elements
            var root = new VisualElement { name = "root-container" };
            var visualTreeAsset = (VisualTreeAsset)EditorResources.Load<UnityEngine.Object>("UXML/ShortcutManager/DeleteShortcutProfileWindow.uxml");
            visualTreeAsset.CloneTree(root, null);
            rootVisualElement.Add(root);

            // Load styles
            if (EditorGUIUtility.isProSkin)
                root.AddToClassList("isProSkin");
            root.AddStyleSheetPath("StyleSheets/ShortcutManager/PromptWindow.uss");

            // Find elements
            m_HeaderTextElement = root.Q<TextElement>("header");
            m_MessageTextElement = root.Q<TextElement>("message");
            var buttons = root.Q<VisualElement>("buttons");
            m_SubmitButton = root.Q<Button>("submit");
            var cancelButton = root.Q<Button>("cancel");

            // Set localized text
            m_SubmitButton.text = L10n.Tr("Delete Profile");
            cancelButton.text = L10n.Tr("Cancel");

            // Set up event handlers
            m_SubmitButton.clickable.clicked += Submit;
            cancelButton.clickable.clicked += Close;

            // Flip submit and cancel buttons on macOS
            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
            {
                var parent = m_SubmitButton.parent;
                parent.Remove(m_SubmitButton);
                parent.Add(m_SubmitButton);
            }

            // Mark last button with class
            buttons.Children().Last().AddToClassList("last");
        }

        void Submit()
        {
            m_Action();
            Close();
        }
    }
}
