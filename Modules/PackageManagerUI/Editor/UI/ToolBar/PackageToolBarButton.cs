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

    internal abstract class PackageToolBarButton
    {
        // Returns true if the action is triggered, false otherwise.
        protected abstract bool TriggerAction();

        protected abstract bool isInProgress { get; }

        protected abstract bool isVisible { get; }

        protected abstract string GetTooltip(bool isInProgress);

        protected abstract string GetText(bool isInProgress);

        protected abstract void RegisterClickAction();

        public abstract void Refresh(IPackage package, IPackageVersion version);
    }

    internal abstract class PackageToolBarButton<T> : PackageToolBarButton where T : VisualElement, ITextElement, new()
    {
        protected static readonly string k_InProgressGenericTooltip = L10n.Tr("This action is currently in progress.");

        protected IPackage m_Package;
        protected IPackageVersion m_Version;

        public Action onAction;

        private ButtonDisableCondition[] m_CommonDisableConditions;

        protected T m_Element;
        public T element => m_Element;

        protected virtual void SetPackageAndVersion(IPackage package, IPackageVersion version)
        {
            m_Package = package;
            m_Version = version;
        }

        protected virtual IEnumerable<ButtonDisableCondition> GetDisableConditions()
        {
            return Enumerable.Empty<ButtonDisableCondition>();
        }

        public void SetCommonDisableConditions(params ButtonDisableCondition[] disableConditions)
        {
            m_CommonDisableConditions = disableConditions;
        }

        protected void OnClicked()
        {
            if (TriggerAction())
                onAction?.Invoke();
        }

        public PackageToolBarButton()
        {
            m_Element = new T();
            m_Element.name = GetType().Name;
            RegisterClickAction();
            m_CommonDisableConditions = new ButtonDisableCondition[0];
        }

        public override void Refresh(IPackage package, IPackageVersion version)
        {
            SetPackageAndVersion(package, version);

            var isVisible = this.isVisible;
            UIUtils.SetElementDisplay(m_Element, isVisible);
            if (!isVisible)
                return;

            var isInProgress = this.isInProgress;
            m_Element.text = GetText(isInProgress);

            if (isInProgress)
            {
                m_Element.SetEnabled(false);
                m_Element.tooltip = GetTooltip(true);
                return;
            }

            var disableCondition = GetDisableConditions().Concat(m_CommonDisableConditions).FirstOrDefault(condition => condition.value);
            m_Element.SetEnabled(disableCondition == null);
            m_Element.tooltip = disableCondition?.tooltip ?? GetTooltip(false);
        }
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
