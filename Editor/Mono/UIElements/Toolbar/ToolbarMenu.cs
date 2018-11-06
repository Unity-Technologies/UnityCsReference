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

        Clickable clickable;

        public DropdownMenu menu { get; }

        public new static readonly string ussClassName = "unity-toolbar-menu";
        public static readonly string popupVariantUssClassName = ussClassName + "--popup";

        public ToolbarMenu()
        {
            Toolbar.SetToolbarStyleSheet(this);

            clickable = new Clickable(this.ShowMenu);
            this.AddManipulator(clickable);
            menu = new DropdownMenu();

            AddToClassList(ussClassName);
        }

        Variant m_Variant;
        public Variant variant
        {
            get { return m_Variant; }
            set
            {
                m_Variant = value;

                if (m_Variant == Variant.Popup)
                {
                    AddToClassList(popupVariantUssClassName);
                }
                else
                {
                    RemoveFromClassList(popupVariantUssClassName);
                }
            }
        }
    }
}
