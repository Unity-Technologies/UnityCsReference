// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.UI.Builder
{
    [Flags]
    enum TransitionChangeType
    {
        None = 0,
        Property = 1 << 0,
        Duration = 1 << 1,
        TimingFunction = 1 << 2,
        Delay = 1 << 3,
        All = Property | Duration | TimingFunction | Delay
    }

    static class TransitionChangeTypeExtensions
    {
        public static bool HasAnyFlag(this TransitionChangeType value)
        {
            return (value & TransitionChangeType.All) != TransitionChangeType.None;
        }

        public static bool IsSet(this TransitionChangeType value, TransitionChangeType flag)
        {
            return (value & flag) == flag;
        }
    }
}
