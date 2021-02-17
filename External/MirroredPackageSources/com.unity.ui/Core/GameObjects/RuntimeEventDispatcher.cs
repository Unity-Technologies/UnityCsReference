using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    static class RuntimeEventDispatcher
    {
        public static EventDispatcher Create()
        {
            return EventDispatcher.CreateForRuntime(new List<IEventDispatchingStrategy>
            {
                new NavigationEventDispatchingStrategy(),
                new PointerCaptureDispatchingStrategy(),
                new KeyboardEventDispatchingStrategy(),
                new PointerEventDispatchingStrategy(),
                new MouseEventDispatchingStrategy(), //TODO: remove all runtime mouse events (PointerWheelEvent?)
                new DefaultDispatchingStrategy(),
            });
        }
    }
}
