
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class TransitionAddedEvent : EventBase<TransitionAddedEvent>
    {
        public BuilderTransition transition;
    }

    class TransitionRemovedEvent : EventBase<TransitionRemovedEvent>
    {
        public int index;
    }

    class TransitionChangedEvent : EventBase<TransitionChangedEvent>
    {
        public FoldoutTransitionField field;
        public BuilderTransition transition;
        public TransitionChangeType changeType;
        public int index;
    }
}

