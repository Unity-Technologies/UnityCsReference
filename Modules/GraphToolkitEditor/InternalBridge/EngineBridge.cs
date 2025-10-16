// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.InternalBridge
{
    static class EngineBridge
    {
        public static void RemoveLogEntriesByIdentifier(int logIdentifier)
        {
            Debug.RemoveLogEntriesByIdentifier(logIdentifier);
        }

        public static Vector2 GetMousePosition()
        {
            return PointerDeviceState.GetPointerPosition(PointerId.mousePointerId, ContextType.Editor);
        }

        public static (ulong, ulong) ToParts(this Hash128 hash)
        {
            return (hash.u64_0, hash.u64_1);
        }

        public static void GetMovedFromData(this MovedFromAttribute self, out string className, out bool classNameHasChanged,
            out string nameSpace, out bool nameSpaceHasChanged, out string assemblyName, out bool assemblyNameHasChanged,
            out bool autoUpdateAPI)
        {
            className = self.data.className;
            classNameHasChanged = self.data.classHasChanged;
            nameSpace = self.data.nameSpace;
            nameSpaceHasChanged = self.data.nameSpaceHasChanged;
            assemblyName = self.data.assembly;
            assemblyNameHasChanged = self.data.assemblyHasChanged;
            autoUpdateAPI = self.data.autoUdpateAPI;
        }
    }
}
