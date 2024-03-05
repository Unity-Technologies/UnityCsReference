// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    static class RuntimeEventDispatcher
    {
        public static EventDispatcher Create()
        {
            return EventDispatcher.CreateForRuntime(new List<IEventDispatchingStrategy>
            {
                new PointerCaptureDispatchingStrategy(),
                new MouseCaptureDispatchingStrategy(),
                new KeyboardEventDispatchingStrategy(),
                new PointerEventDispatchingStrategy(),
                new MouseEventDispatchingStrategy(),
                new NavigationEventDispatchingStrategy(),
                new DefaultDispatchingStrategy(),
            });
        }
    }
}
