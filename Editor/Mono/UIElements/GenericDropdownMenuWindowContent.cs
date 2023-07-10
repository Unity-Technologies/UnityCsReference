// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal class EditorGenericDropdownMenuWindowContent : PopupWindowContent
    {
        internal Vector2 windowSize { get; set; }

        private GenericDropdownMenu m_GenericDropdownMenu;
        private static readonly string s_PopupUssClassName = GenericDropdownMenu.ussClassName + "__popup";

        public EditorGenericDropdownMenuWindowContent(GenericDropdownMenu genericDropdownMenu)
        {
            m_GenericDropdownMenu = genericDropdownMenu;
        }

        public override Vector2 GetWindowSize()
        {
            if (windowSize == Vector2.zero)
            {
                return base.GetWindowSize();
            }

            return windowSize;
        }

        public override void OnOpen()
        {
            base.OnOpen();

            editorWindow.rootVisualElement.AddToClassList(s_PopupUssClassName);
            editorWindow.rootVisualElement.Add(m_GenericDropdownMenu.menuContainer);

            editorWindow.rootVisualElement.schedule.Execute(() =>
            {
                if (editorWindow != null)
                {
                    m_GenericDropdownMenu.innerContainer.Focus();
                }
            });
        }

        public override void OnGUI(Rect rect) { }
    }
}
