// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.ViewModel;

namespace Unity.Timeline.Foundation.View
{
    readonly struct MoveBehaviourBundle
    {
        public readonly MoveBehaviour behaviour;
        public readonly MoveBehaviourOverlay overlay;

        public MoveBehaviourBundle(MoveBehaviour behaviour, MoveBehaviourOverlay overlay)
        {
            this.behaviour = behaviour;
            this.overlay = overlay;
        }

        public bool SameTypeAs(MoveBehaviourBundle other)
        {
            return behaviour?.GetType() == other.behaviour?.GetType()
                && overlay?.GetType() == other.overlay?.GetType();
        }

        public bool IsDefault() => behaviour == null && overlay == null;
    }
}
