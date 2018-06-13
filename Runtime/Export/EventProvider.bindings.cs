// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Export/EventProvider.bindings.h")]
    public sealed class EventProvider
    {
        [FreeFunction("EventProvider_Bindings::WriteCustomEvent")]
        extern public static void WriteCustomEvent(int value, string text);
    }
}
