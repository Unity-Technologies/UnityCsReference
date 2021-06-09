
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    struct BuilderTransition
    {
        public static readonly string DefaultProperty = "all";
        public static readonly string IgnoredProperty = "ignored";
        public static readonly TimeValue DefaultDuration = new TimeValue(0, TimeUnit.Second);
        public static readonly EasingFunction DefaultTimingFunction = EasingMode.Ease;
        public static readonly TimeValue DefaultDelay = new TimeValue(0, TimeUnit.Second);

        public static BuilderTransition Default => new BuilderTransition
        {
            property = DefaultProperty,
            duration = DefaultDuration,
            timingFunction = DefaultTimingFunction,
            delay = DefaultDelay
        };

        public UIStyleValue<string> property;
        public UIStyleValue<TimeValue> duration;
        public UIStyleValue<EasingFunction> timingFunction;
        public UIStyleValue<TimeValue> delay;
    }
}

