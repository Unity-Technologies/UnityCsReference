// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal abstract class ToolbarButtonBase<SingleType, BulkType> : VisualElement
    {
        private readonly ActionBase<SingleType, BulkType> m_Action;

        // This is the list of items being affected by the action (could be one or many)
        protected readonly List<BulkType> m_Items = new List<BulkType>();

        // This field is only used for the single select case (when m_Items contains only one element for example),
        // because sometimes the button is for a non-primary version of a package when we are not handling multi-select
        protected SingleType m_SingleItem;

        public VisualElement element => this;
        protected abstract string text { set; }

        protected ToolbarButtonBase(ActionBase<SingleType, BulkType> action)
        {
            m_Action = action;
            m_Action.onActionTriggered += UpdateDisplay;
        }

        public void Refresh(SingleType item)
        {
            m_Items.Clear();
            m_SingleItem = item;
            UpdateSingleActionDisplay(item);
        }

        public void Refresh(IEnumerable<BulkType> items)
        {
            m_Items.Clear();
            if (items != null)
                m_Items.AddRange(items);

            m_SingleItem = default;
            UpdateBulkActionDisplay(m_Items);
        }

        protected void TriggerAction()
        {
            if (!EqualityComparer<SingleType>.Default.Equals(m_SingleItem, default))
            {
                m_Action.TriggerAction(m_SingleItem);
                return;
            }

            switch (m_Items.Count)
            {
                case > 1:
                    m_Action.TriggerAction(m_Items);
                    break;
                case 1:
                    m_Action.TriggerAction(GetSingleItemFromBulkItem(m_Items[0]));
                    break;
            }
        }

        private void UpdateDisplay()
        {
            if (!EqualityComparer<SingleType>.Default.Equals(m_SingleItem, default))
                UpdateSingleActionDisplay(m_SingleItem);
            else
                UpdateBulkActionDisplay(m_Items);
        }

        private void UpdateSingleActionDisplay(SingleType item)
        {
            var state = m_Action.GetActionState(item, out var actionText, out var tooltipText);
            var isVisible = (state & ActionState.Visible) != ActionState.None;
            UIUtils.SetElementDisplay(this, isVisible);
            if (!isVisible)
                return;

            text = actionText;
            tooltip = tooltipText;

            var isDisabled = (state & ActionState.Disabled) != ActionState.None;
            SetEnabled(!isDisabled);
        }

        private void UpdateBulkActionDisplay(List<BulkType> items)
        {
            if (items.Count == 0) return;

            var singleItem = GetSingleItemFromBulkItem(items[0]);
            var state = m_Action.GetActionState(singleItem, out var actionText, out var tooltipText);

            text = actionText;
            tooltip = tooltipText;

            var isDisabled = (state & ActionState.Disabled) != ActionState.None;
            SetEnabled(!isDisabled);
            UIUtils.SetElementDisplay(this, true);
        }

        protected abstract SingleType GetSingleItemFromBulkItem(BulkType bulk);
    }
}
