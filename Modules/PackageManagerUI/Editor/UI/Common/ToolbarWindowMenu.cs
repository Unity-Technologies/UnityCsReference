// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class ToolbarWindowMenu : Button
    {
        private static readonly string oldUssClassName = "unity-button";
        private static readonly string newUssClassName = "unity-toolbar-menu";
        private static readonly string textUssClassName = newUssClassName + "__text";
        private static readonly string arrowUssClassName = newUssClassName + "__arrow";

        internal new class UxmlFactory : UxmlFactory<ToolbarWindowMenu, UxmlTraits> {}
        internal new class UxmlTraits : TextElement.UxmlTraits {}

        public override string text
        {
            get { return base.text; }
            set { m_TextElement.text = value; base.text = value; }
        }

        private TextElement m_TextElement;
        private VisualElement m_ArrowElement;

        public ToolbarWindowMenu()
        {
            generateVisualContent = null;
            style.backgroundImage = null;

            RemoveFromClassList(oldUssClassName);
            AddToClassList(newUssClassName);

            m_TextElement = new TextElement();
            m_TextElement.AddToClassList(textUssClassName);
            m_TextElement.pickingMode = PickingMode.Ignore;
            Add(m_TextElement);

            m_ArrowElement = new VisualElement();
            m_ArrowElement.AddToClassList(arrowUssClassName);
            m_ArrowElement.pickingMode = PickingMode.Ignore;
            Add(m_ArrowElement);
        }
    }
}
