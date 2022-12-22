// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
