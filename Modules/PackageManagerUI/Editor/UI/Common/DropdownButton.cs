// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    enum DropdownStatus { None = 0, Success, Error, Refresh }

    class DropdownButton : Button
    {
        static string HasDropDownClass = "hasDropDown";
        static string HasStatusClass = "hasStatus";

        internal new class UxmlFactory : UxmlFactory<DropdownButton, UxmlTraits> {}

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var button = ve as DropdownButton;
                button.Label.text = m_Text.GetValueFromBag(bag, cc);
                button.StatusIcon.pickingMode = PickingMode.Ignore;
            }
        }

        public TextElement Label;
        public VisualElement StatusIcon;

        public VisualElement DropDownArea;
        public VisualElement DropDown;

        DropdownStatus status = DropdownStatus.None;
        public DropdownStatus Status
        {
            get {return status;}
            set
            {
                status = value;
                StatusIcon.RemoveFromClassList("none");
                StatusIcon.RemoveFromClassList("success");
                StatusIcon.RemoveFromClassList("error");
                StatusIcon.RemoveFromClassList("refresh");

                StatusIcon.AddToClassList(status.ToString().ToLower());

                UIUtils.SetElementDisplay(StatusIcon, status != DropdownStatus.None);

                if (status == DropdownStatus.None)
                    RemoveFromClassList(HasStatusClass);
                else
                    AddToClassList(HasStatusClass);
            }
        }

        GenericMenu dropdownMenu;
        /// <summary>
        /// Sets a dropdown menu for this button. The dropdown menu icon will only show if
        /// there is a non-null menu set.
        /// </summary>
        public GenericMenu DropdownMenu
        {
            get { return dropdownMenu;}
            set
            {
                UIUtils.SetElementDisplay(DropDownArea, value != null);
                dropdownMenu = value;

                if (dropdownMenu == null)
                    RemoveFromClassList(HasDropDownClass);
                else
                    AddToClassList(HasDropDownClass);
            }
        }

        public new string text
        {
            get { return Label.text; }
            set { Label.text = value; }
        }

        public void OnDropdownButtonClicked()
        {
            if (DropdownMenu == null)
                return;
            var menuPosition = new Vector2(layout.xMin, layout.center.y + 2);
            menuPosition = parent.LocalToWorld(menuPosition);
            var menuRect = new Rect(menuPosition, Vector2.zero);
            DropdownMenu.DropDown(menuRect);
        }

        public DropdownButton()
        {
            style.flexDirection = FlexDirection.Row;

            StatusIcon = new VisualElement();
            StatusIcon.name = "statusIcon";
            Add(StatusIcon);

            Label = new TextElement();
            Label.name = "label";
            Add(Label);

            DropDown = new VisualElement();
            DropDown.name = "dropDown";

            DropDownArea = new VisualElement();
            DropDownArea.RegisterCallback<MouseDownEvent>(evt =>
            {
                evt.PreventDefault();
                evt.StopImmediatePropagation();

                OnDropdownButtonClicked();
            });
            DropDownArea.name = "dropDownArea";
            DropDownArea.Add(DropDown);
            Add(DropDownArea);

            DropdownMenu = null;

            Status = DropdownStatus.None;
        }
    }
}
