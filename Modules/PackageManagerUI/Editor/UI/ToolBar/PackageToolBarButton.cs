// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;
using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal abstract class PackageToolBarButton : VisualElement
    {
        public abstract void Refresh(IPackageVersion version);

        public abstract void Refresh(IEnumerable<IPackageVersion> versions);
        public abstract event Action onActionTriggered;
    }

    internal abstract class PackageToolBarButtonSingleAction : PackageToolBarButton
    {
        private readonly List<IPackageVersion> m_Versions = new();
        private readonly PackageAction m_Action;
        protected PackageToolBarButtonSingleAction(PackageAction action)
        {
            m_Action = action;
        }

        protected abstract string text { set; }

        public override event Action onActionTriggered
        {
            add => m_Action.onActionTriggered += value;
            remove => m_Action.onActionTriggered -= value;
        }

        private void SetPackageVersion(IPackageVersion version)
        {
            m_Versions.Clear();
            m_Versions.Add(version);
        }

        private void SetPackageVersions(IEnumerable<IPackageVersion> versions)
        {
            m_Versions.Clear();
            m_Versions.AddRange(versions);
        }

        public override void Refresh(IEnumerable<IPackageVersion> versions)
        {
            SetPackageVersions(versions);
            if (m_Versions.Count == 0)
                return;

            // Since the refresh for multiple versions is called directly from the foldouts, we assume that the button is always visible
            // so we can skip that check and just update the state of the button directly.
            var version = m_Versions[0];
            text = m_Action.GetMultiSelectText(version, m_Action.IsInProgress(version));

            var temporaryDisableCondition = m_Action.GetActiveTemporaryDisableCondition();
            SetEnabled(temporaryDisableCondition == null);
            tooltip = temporaryDisableCondition?.tooltip ?? string.Empty;
        }

        public override void Refresh(IPackageVersion version)
        {
            SetPackageVersion(version);

            var state = m_Action.GetActionState(version, out var actionText, out var actionTooltip);

            var isVisible = (state & PackageActionState.Visible) != PackageActionState.None;
            UIUtils.SetElementDisplay(this, isVisible);
            if (!isVisible)
                return;

            var isDisabled = (state & PackageActionState.Disabled) != PackageActionState.None;
            text = actionText;
            tooltip = actionTooltip;
            SetEnabled(!isDisabled);
        }

        // Returns true if the action is triggered, false otherwise.
        protected void TriggerAction()
        {
            switch (m_Versions.Count)
            {
                case 0:
                    return;
                case 1:
                    m_Action.TriggerAction(m_Versions[0]);
                    break;
                default:
                    m_Action.TriggerAction(m_Versions);
                    break;
            }
        }
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
