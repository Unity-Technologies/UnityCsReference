// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class DropdownElement : VisualElement
    {
        internal static DropdownElement instance { get; private set; }
        private DropdownContent m_Content;

        private DropdownElement()
        {
            RegisterCallback<MouseDownEvent>(evt => Hide());
        }

        public static void ShowDropdown(VisualElement parent, DropdownContent content)
        {
            if (parent == null || content == null)
                return;

            instance?.Hide();
            instance = new DropdownElement();
            instance.Add(content);
            instance.m_Content = content;
            parent.Add(instance);
            content.OnDropdownShown();
        }

        internal void Hide()
        {
            if (parent == null || instance == null)
                return;

            foreach (var element in parent.Children())
                element.SetEnabled(true);
            parent.Remove(instance);
            instance = null;
            m_Content.OnDropdownClosed();
        }
    }
}
