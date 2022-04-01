// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine;

namespace UnityEditor.Remote
{
    [NativeHeader("Modules/GenericRemoteEditor/Public/GenericRemote.bindings.h")]
    internal static class GenericRemote
    {
        // We're keeping a separate list of C# delegates so that they get lost
        // en bloc in domain reload. Simplifies the handling of that as opposed
        // to having GenericRemote.cpp having these delegates directly on its list.
        private static List<Func<IntPtr, bool>> s_Handlers = new List<Func<IntPtr, bool>>();

        public static void AddMessageHandler(Func<IntPtr, bool> handler)
        {
            s_Handlers.Add(handler);
        }

        public static void RemoveMessageHandler(Func<IntPtr, bool> handler)
        {
            s_Handlers.Remove(handler);
        }

        [RequiredByNativeCode]
        internal static bool CallMessageHandlers(IntPtr messageData)
        {
            foreach (var handler in s_Handlers)
            {
                if (handler(messageData))
                    return true;
            }
            return false;
        }

        public static extern void SetGyroEnabled(bool enabled);
        public static extern void SetGyroUpdateInterval(float interval);
    }
}
