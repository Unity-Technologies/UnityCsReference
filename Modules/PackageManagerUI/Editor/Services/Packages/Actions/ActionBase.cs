// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Flags]
    internal enum ActionState : uint
    {
        None = 0,
        Visible = 1 << 0,
        DisabledTemporarily = 1 << 1,
        DisabledForItem = 1 << 2,
        InProgress = 1 << 3,

        Disabled = DisabledTemporarily | DisabledForItem
    }

    internal abstract class ActionBase<SingleType, BulkType>
    {
        public event Action onActionTriggered;

        public void TriggerAction(SingleType item)
        {
            if (TriggerActionImplementation(item))
                onActionTriggered?.Invoke();
        }

        public void TriggerAction(IReadOnlyCollection<BulkType> items)
        {
            if (TriggerActionImplementation(items))
                onActionTriggered?.Invoke();
        }

        public ActionState GetActionState(SingleType item, out string text, out string tooltip)
        {
            if (!IsVisible(item))
            {
                text = string.Empty;
                tooltip = string.Empty;

                if (IsHiddenWhenInProgress(item) && IsInProgress(item))
                    return ActionState.InProgress;
                return ActionState.None;
            }

            var isInProgress = IsInProgress(item);
            text = GetText(item, isInProgress);
            if (isInProgress)
            {
                tooltip = GetTooltip(item, true);
                return ActionState.Visible | ActionState.DisabledForItem | ActionState.InProgress;
            }
            var disableCondition = GetActiveDisableCondition(item);
            if (disableCondition != null)
            {
                tooltip = disableCondition.tooltip;
                return ActionState.Visible | ActionState.DisabledForItem;
            }

            var temporaryDisableCondition = GetActiveTemporaryDisableCondition();
            if (temporaryDisableCondition != null)
            {
                tooltip = temporaryDisableCondition.tooltip;
                return ActionState.Visible | ActionState.DisabledTemporarily;
            }

            tooltip = GetTooltip(item, false);
            return ActionState.Visible;
        }

        protected abstract bool TriggerActionImplementation(SingleType item);
        // By default, bulk actions are not supported
        protected virtual bool TriggerActionImplementation(IReadOnlyCollection<BulkType> items) => false;
        public abstract string GetText(SingleType item, bool isInProgress);
        public abstract string GetTooltip(SingleType item,  bool isInProgress);
        public virtual bool IsInProgress(SingleType item) => false;
        protected virtual bool IsHiddenWhenInProgress(SingleType item) => false;
        public virtual bool IsVisible(SingleType item) => true;

        public abstract ToolbarButtonBase<SingleType, BulkType> CreateToolbarButton();

        // Temporary disable conditions refer to conditions that are temporary and not related to the state of a package
        // For example, when the network is lost or when there are scripting compiling
        protected virtual IEnumerable<DisableCondition> GetAllTemporaryDisableConditions() => Array.Empty<DisableCondition>();
        public virtual DisableCondition GetActiveTemporaryDisableCondition()
        {
            return GetAllTemporaryDisableConditions().FirstMatch(condition => condition.active);
        }

        protected virtual IEnumerable<DisableCondition> GetAllDisableConditions(SingleType item) => Array.Empty<DisableCondition>();

        public virtual DisableCondition GetActiveDisableCondition(SingleType item)
        {
            return GetAllDisableConditions(item).FirstMatch(condition => condition.active);
        }
    }

    internal abstract class ActionBase<T> : ActionBase<T, T>
    {}
}
