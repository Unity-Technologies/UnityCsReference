// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    [EventCategory(EventCategory.Keyboard)]
    internal class IMEEvent : EventBase<IMEEvent>
    {
        public string compositionString { get; protected set; }

        static IMEEvent()
        {
            SetCreateFunction(() => new IMEEvent());
        }

        /// <summary>
        /// Gets a ime event from the event pool and initializes it with the given values. Use this
        /// function instead of creating new events. Events obtained using this method need to be released
        /// back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="compositionString">IME's current composition string.</param>
        /// <returns>An initialized event.</returns>
        public static IMEEvent GetPooled(string compositionString)
        {
            var evt = GetPooled();
            evt.compositionString = compositionString;
            return evt;
        }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown |
                EventPropagation.SkipDisabledElements;
            compositionString = default(string);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public IMEEvent()
        {
            LocalInit();
        }
    }
}
