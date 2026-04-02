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

        protected abstract bool TriggerActionImplementation(SingleType item);
        // By default, bulk actions are not supported
        protected virtual bool TriggerActionImplementation(IReadOnlyCollection<BulkType> items) => false;
        public abstract string GetText(SingleType item, bool isInProgress);
        public abstract string GetTooltip(SingleType item,  bool isInProgress);
        public abstract ActionState GetActionState(SingleType item, out string text, out string tooltip);
        public abstract ToolbarButtonBase<SingleType, BulkType> CreateToolbarButton();
    }

    internal abstract class ActionBase<T> : ActionBase<T, T>
    {}
}
