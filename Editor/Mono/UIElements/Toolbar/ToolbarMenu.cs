// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public class ToolbarMenu : TextElement, IToolbarMenuElement
    {
        public new class UxmlFactory : UxmlFactory<ToolbarMenu, UxmlTraits> {}
        public new class UxmlTraits : TextElement.UxmlTraits {}

        public enum Variant
        {
            Default,
            Popup
        }

        PointerClickable clickable;

        public DropdownMenu menu { get; }
        public override string text
        {
            get { return base.text; }
            set { m_TextElement.text = value; base.text = value; }
        }

        public new static readonly string ussClassName = "unity-toolbar-menu";
        public static readonly string popupVariantUssClassName = ussClassName + "--popup";
        public static readonly string textUssClassName = ussClassName + "__text";
        public static readonly string arrowUssClassName = ussClassName + "__arrow";

        private TextElement m_TextElement;
        private VisualElement m_ArrowElement;

        public ToolbarMenu()
        {
            Toolbar.SetToolbarStyleSheet(this);
            generateVisualContent = null;

            clickable = new PointerClickable(this.ShowMenu);
            this.AddManipulator(clickable);
            menu = new DropdownMenu();

            AddToClassList(ussClassName);

            m_TextElement = new TextElement();
            m_TextElement.AddToClassList(textUssClassName);
            m_TextElement.pickingMode = PickingMode.Ignore;
            Add(m_TextElement);

            m_ArrowElement = new VisualElement();
            m_ArrowElement.AddToClassList(arrowUssClassName);
            m_ArrowElement.pickingMode = PickingMode.Ignore;
            Add(m_ArrowElement);
        }

        Variant m_Variant;
        public Variant variant
        {
            get { return m_Variant; }
            set
            {
                m_Variant = value;
                EnableInClassList(popupVariantUssClassName, value == Variant.Popup);
            }
        }
    }
}
