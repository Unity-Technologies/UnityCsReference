// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    enum DropdownStatus { None = 0, Success, Error };

    class DropdownButton : VisualElement
    {
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
                button.MainButton.text = m_Text.GetValueFromBag(bag, cc);
                button.StatusIcon.pickingMode = PickingMode.Ignore;
            }
        }

        readonly VisualElement root;

        const string RerunWarningTooltip = "\n*Click to trigger this action again, all previous results will be overwritten";

        DropdownStatus status = DropdownStatus.None;
        public DropdownStatus Status
        {
            get {return status;}
            set
            {
                status = value;
                StatusIcon.RemoveFromClassList("success");
                StatusIcon.RemoveFromClassList("error");
                UIUtils.SetElementDisplay(DropDown, status != DropdownStatus.None);

                if (status != DropdownStatus.None)
                {
                    StatusIcon.AddToClassList(status.ToString().ToLower());
                    if (!tooltip.Contains(RerunWarningTooltip))
                        tooltip += RerunWarningTooltip;
                }
                else
                {
                    tooltip = tooltip.Replace(RerunWarningTooltip, "");
                }
            }
        }

        public void RegisterClickCallback(EventCallback<MouseDownEvent> value)
        {
            StatusIcon.RegisterCallback(value);
            MainButton.clickable.clicked += () => value(null);    // RegisterCallback does not work here, so using this instead
        }

        public void UnregisterClickCallback(EventCallback<MouseDownEvent> value)
        {
            StatusIcon.UnregisterCallback(value);
            MainButton.UnregisterCallback(value);
        }

        public GenericMenu DropdownMenu { get; set; }
        void OnDropdownButtonClicked()
        {
            if (DropdownMenu == null)
                return;
            var menuPosition = new Vector2(DropDown.layout.center.x, DropDown.layout.center.y);
            menuPosition = this.LocalToWorld(menuPosition);
            var menuRect = new Rect(menuPosition, Vector2.zero);
            DropdownMenu.DropDown(menuRect);
        }

        public DropdownButton()
        {
            root = Resources.GetTemplate("DropdownButton.uxml");
            Add(root);
            Cache = new VisualElementCache(root);

            DropDown.clickable.clicked += OnDropdownButtonClicked;
        }

        internal VisualElement StatusIcon { get { return Cache.Get<VisualElement>("statusIcon"); } }
        internal Button MainButton { get { return Cache.Get<Button>("mainButton"); } }
        internal Button DropDown { get { return Cache.Get<Button>("dropDownIcon"); } }
        VisualElementCache Cache { get; set; }
    }
}
