
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
        public static bool Any(this TransitionChangeType value)
        {
            return (value & TransitionChangeType.All) != TransitionChangeType.None;
        }

        public static bool IsSet(this TransitionChangeType value, TransitionChangeType flag)
        {
            return (value & flag) == flag;
        }
    }
}

