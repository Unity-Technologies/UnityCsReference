// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;
using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal abstract class PackageToolBarButtonSingleAction : ToolbarButtonBase<IPackageVersion, IPackage>, IPackageToolBarButton
    {
        private readonly PackageAction m_PackageAction;

        protected PackageToolBarButtonSingleAction(PackageAction action) : base(action)
        {
            m_PackageAction = action;
        }

        public event Action onActionTriggered
        {
            add => m_PackageAction.onActionTriggered += value;
            remove => m_PackageAction.onActionTriggered -= value;
        }

        protected override IPackageVersion GetSingleItemFromBulkItem(IPackage package) => package.versions.primary;
    }

    // This button only shows text
    internal class PackageToolBarSimpleButton : PackageToolBarButtonSingleAction
    {
        protected readonly DropdownButton m_Button;
        public PackageToolBarSimpleButton(PackageAction action) : base(action)
        {
            m_Button = new DropdownButton();
            m_Button.clicked += TriggerAction;

            Add(m_Button);
        }

        protected override string text { set => m_Button.text = value; }
    }

    // This button shows text and an optional icon
    internal class PackageToolBarButtonWithIcon : PackageToolBarSimpleButton
    {
        public PackageToolBarButtonWithIcon(PackageAction action) : base(action)
        {
            m_Button.SetIcon(action.icon);
        }
    }

    // This button shows only icon
    internal class PackageToolBarIconOnlyButton : PackageToolBarButtonSingleAction
    {
        private readonly Button m_Button;
        public PackageToolBarIconOnlyButton(PackageAction action) : base(action)
        {
            m_Button = new Button();
            m_Button.clicked += TriggerAction;
            m_Button.text = string.Empty;
            m_Button.AddToClassList("icon");
            m_Button.AddToClassList(action.icon.ClassName());

            Add(m_Button);
        }

        protected override string text
        {
            // Since icon buttons are strictly icon only, we never allow the text to be set to anything other than empty
            set { }
        }
    }
}
