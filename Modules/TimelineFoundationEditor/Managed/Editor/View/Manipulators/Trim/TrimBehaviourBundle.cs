// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.ViewModel;

namespace Unity.Timeline.Foundation.View
{
    readonly struct TrimBehaviourBundle
    {
        public readonly TrimBehaviour behaviour;
        public readonly TrimBehaviourOverlay overlay;

        public TrimBehaviourBundle(TrimBehaviour behaviour, TrimBehaviourOverlay overlay)
        {
            this.behaviour = behaviour;
            this.overlay = overlay;
        }

        public bool SameTypeAs(TrimBehaviourBundle other)
        {
            return behaviour?.GetType() == other.behaviour?.GetType()
                && overlay?.GetType() == other.overlay?.GetType();
        }

        public bool IsDefault() => behaviour == null && overlay == null;
    }
}
