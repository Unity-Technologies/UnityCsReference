// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class DropdownContainer : EditorWindow
    {
        internal static DropdownContainer instance { get; private set; }
        private DropdownContent m_Content;

        private void OnEnable()
        {
            hideFlags = HideFlags.DontSave;
            this.SetAntiAliasing(4);
        }

        private void OnDisable()
        {
            instance = null;

            m_Content?.OnDropdownClosed();
            m_Content = null;
        }

        public static void ShowDropdown(DropdownContent content)
        {
            if (instance != null || content == null)
                return;

            instance = CreateInstance<DropdownContainer>();
            instance.rootVisualElement.Add(content);

            content.container = instance;
            instance.m_Content = content;

            instance.ShowAsDropDown(content.position, content.windowSize);
            content.OnDropdownShown();
        }

        static void ShowDropdownContainer()
        {
            instance.ShowAsDropDown(instance.m_Content.position, instance.m_Content.windowSize);
            instance.m_Content.OnDropdownShown();

            // Make sure delayCall has no chance to execute twice or more for the same menu. We had some issues like this in UI Elements tests suite
            EditorApplication.delayCall -= ShowDropdownContainer;
        }
    }
}
