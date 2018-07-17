// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public abstract class ToolbarMenuBase : TextElement, IToolbarMenuElement
    {
        Clickable clickable;

        public DropdownMenu menu { get; }

        protected ToolbarMenuBase(string classList) :
            this()
        {
            Toolbar.SetToolbarStyleSheet(this);
            AddToClassList(classList);
        }

        ToolbarMenuBase()
        {
            clickable = new Clickable(this.ShowMenu);
            this.AddManipulator(clickable);
            menu = new DropdownMenu();
        }
    }

    public class ToolbarMenu : ToolbarMenuBase
    {
        public new class UxmlFactory : UxmlFactory<ToolbarMenu, UxmlTraits> {}
        public new class UxmlTraits : TextElement.UxmlTraits {}

        const string k_ClassName = "toolbarMenu";
        public ToolbarMenu() :
            base(k_ClassName)
        {
        }
    }

    public class ToolbarPopup : ToolbarMenuBase
    {
        public new class UxmlFactory : UxmlFactory<ToolbarPopup, UxmlTraits> {}
        public new class UxmlTraits : TextElement.UxmlTraits {}

        const string k_ClassName = "toolbarPopup";
        public ToolbarPopup() :
            base(k_ClassName)
        {
        }
    }
}
