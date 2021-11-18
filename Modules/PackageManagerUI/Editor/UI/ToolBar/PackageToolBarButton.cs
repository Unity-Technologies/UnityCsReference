// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;
using System;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class ButtonDisableCondition
    {
        private Func<bool> m_Condition;
        public string tooltip { get; set; }

        private bool? m_Value;
        public bool value => m_Value ?? m_Condition?.Invoke() ?? false;

        public ButtonDisableCondition(Func<bool> condition, string tooltip)
        {
            m_Condition = condition;
            this.tooltip = tooltip;
        }

        public ButtonDisableCondition(bool value, string tooltip)
        {
            m_Value = value;
            this.tooltip = tooltip;
        }
    }

    [Flags]
    internal enum PackageActionState : uint
    {
        None = 0,
        Visible = 1 << 0,
        DisabledGlobally = 1 << 1,
        DisabledForPackage = 1 << 2,
        Disabled = DisabledGlobally | DisabledForPackage,
        InProgress = 1 << 3
    }

    internal abstract class PackageToolBarButton
    {
        // Returns true if the action is triggered, false otherwise.
        protected abstract bool TriggerAction(IPackageVersion version);

        // Returns true if the action is triggered, false otherwise.
        protected abstract bool TriggerAction(IList<IPackageVersion> versions);

        protected abstract bool IsInProgress(IPackageVersion version);

        protected abstract bool IsHiddenWhenInProgress(IPackageVersion version);

        protected abstract bool IsVisible(IPackageVersion version);

        protected abstract string GetTooltip(IPackageVersion version, bool isInProgress);

        protected abstract string GetText(IPackageVersion version, bool isInProgress);

        public abstract PackageActionState GetActionState(IPackageVersion version, out string text, out string tooltip);

        public abstract void Refresh(IPackageVersion version);

        public abstract void Refresh(IEnumerable<IPackageVersion> versions);
    }

    internal abstract class PackageToolBarButton<T> : PackageToolBarButton where T : VisualElement, ITextElement, new()
    {
        protected static readonly string k_InProgressGenericTooltip = L10n.Tr("This action is currently in progress.");

        protected List<IPackageVersion> m_Versions = new List<IPackageVersion>();

        public Action onAction;

        private ButtonDisableCondition[] m_GlobalDisableConditions;

        protected T m_Element;
        public T element => m_Element;

        protected virtual void SetPackageVersion(IPackageVersion version)
        {
            m_Versions.Clear();
            m_Versions.Add(version);
        }

        protected virtual void SetPackageVersions(IEnumerable<IPackageVersion> versions)
        {
            m_Versions.Clear();
            m_Versions.AddRange(versions);
        }

        protected virtual IEnumerable<ButtonDisableCondition> GetDisableConditions(IPackageVersion version)
        {
            return Enumerable.Empty<ButtonDisableCondition>();
        }

        public void SetGlobalDisableConditions(params ButtonDisableCondition[] disableConditions)
        {
            m_GlobalDisableConditions = disableConditions;
        }

        protected void OnClicked()
        {
            if (TriggerAction())
                onAction?.Invoke();
        }

        protected abstract void RegisterClickAction();

        public PackageToolBarButton()
        {
            m_Element = new T();
            m_Element.name = GetType().Name;
            RegisterClickAction();
            m_GlobalDisableConditions = new ButtonDisableCondition[0];
        }

        public override PackageActionState GetActionState(IPackageVersion version, out string text, out string tooltip)
        {
            if (!IsVisible(version))
            {
                text = string.Empty;
                tooltip = string.Empty;

                if (IsHiddenWhenInProgress(version) && IsInProgress(version))
                    return PackageActionState.InProgress;
                return PackageActionState.None;
            }

            var isInProgress = IsInProgress(version);
            text = GetText(version, isInProgress);
            if (isInProgress)
            {
                tooltip = GetTooltip(version, true);
                return PackageActionState.Visible | PackageActionState.DisabledForPackage | PackageActionState.InProgress;
            }

            var disableCondition = GetDisableConditions(version).FirstOrDefault(condition => condition.value);
            if (disableCondition != null)
            {
                tooltip = disableCondition.tooltip;
                return PackageActionState.Visible | PackageActionState.DisabledForPackage;
            }

            var globalDisableCondition = m_GlobalDisableConditions.FirstOrDefault(condition => condition.value);
            if (globalDisableCondition != null)
            {
                tooltip = globalDisableCondition.tooltip;
                return PackageActionState.Visible | PackageActionState.DisabledGlobally;
            }

            tooltip = GetTooltip(version, false);
            return PackageActionState.Visible;
        }

        public override void Refresh(IEnumerable<IPackageVersion> versions)
        {
            SetPackageVersions(versions);
            if (!versions.Any())
                return;

            // Since the refresh for multiple versions is called directly from the foldouts, we assume that the button is always visible
            // so we can skip that check and just update the state of the button directly.
            var version = versions.FirstOrDefault();
            m_Element.text = GetText(version, IsInProgress(version));

            var globalDisableCondition = m_GlobalDisableConditions.FirstOrDefault(condition => condition.value);
            m_Element.SetEnabled(globalDisableCondition == null);
            m_Element.tooltip = globalDisableCondition?.tooltip ?? string.Empty;
        }

        public override void Refresh(IPackageVersion version)
        {
            SetPackageVersion(version);

            var state = GetActionState(version, out var text, out var tooltip);

            var isVisible = (state & PackageActionState.Visible) != PackageActionState.None;
            UIUtils.SetElementDisplay(m_Element, isVisible);
            if (!isVisible)
                return;

            var isDisabled = (state & PackageActionState.Disabled) != PackageActionState.None;
            m_Element.SetEnabled(!isDisabled);
            m_Element.text = text;
            m_Element.tooltip = tooltip;
        }

        // Returns true if the action is triggered, false otherwise.
        protected virtual bool TriggerAction()
        {
            if (!m_Versions.Any())
                return false;
            if (m_Versions.Count == 1)
                return TriggerAction(m_Versions.FirstOrDefault());
            return TriggerAction(m_Versions);
        }

        // By default buttons does not support bulk action
        protected override bool TriggerAction(IList<IPackageVersion> versions) => false;

        protected override bool IsHiddenWhenInProgress(IPackageVersion version) => false;
    }

    internal abstract class PackageToolBarRegularButton : PackageToolBarButton<Button>
    {
        protected override void RegisterClickAction()
        {
            m_Element.clickable.clicked += OnClicked;
        }
    }

    internal abstract class PackageToolBarDropdownButton : PackageToolBarButton<DropdownButton>
    {
        protected override void RegisterClickAction()
        {
            m_Element.clickable.clicked += OnClicked;
        }
    }
}
