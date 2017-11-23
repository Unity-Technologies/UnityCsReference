// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Export/UnityEventQueueSystem.bindings.h")]
    public class UnityEventQueueSystem
    {
        public static string GenerateEventIdForPayload(string eventPayloadName)
        {
            byte[] bs = Guid.NewGuid().ToByteArray();
            return string.Format("REGISTER_EVENT_ID(0x{0:X2}{1:X2}{2:X2}{3:X2}{4:X2}{5:X2}{6:X2}{7:X2}ULL,0x{8:X2}{9:X2}{10:X2}{11:X2}{12:X2}{13:X2}{14:X2}{15:X2}ULL,{16})"
                , bs[0], bs[1], bs[2], bs[3], bs[4], bs[5], bs[6], bs[7]
                , bs[8], bs[9], bs[10], bs[11], bs[12], bs[13], bs[14], bs[15]
                , eventPayloadName);
        }

        // Used to pass the GlobalEventQueue to native plugins. This allows native plugins to intra communicate
        // and schedule cross thread work (to be executed on the main thread) without having to touch managed
        // systems.
        [FreeFunction]
        public static extern IntPtr GetGlobalEventQueue();
    }
}
