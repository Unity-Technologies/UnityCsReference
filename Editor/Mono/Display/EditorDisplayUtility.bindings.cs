// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Platform/Interface/GUIView.h")]
    [NativeHeader("Runtime/Graphics/EditorDisplayManager.h")]
    internal static partial class EditorDisplayUtility
    {
        [FreeFunction]
        public static extern int GetNumberOfConnectedDisplays();

        [FreeFunction]
        public static extern void AddVirtualDisplay(int index, int width, int height);

        [FreeFunction]
        public static extern void RemoveVirtualDisplay(int index);

        [FreeFunction]
        public static extern void SetSortDisplayOrder(bool enabled);

        [FreeFunction]
        public static extern string GetDisplayName(int index);

        [FreeFunction]
        public static extern int GetDisplayId(int index);

        [FreeFunction]
        public static extern int GetDisplayWidth(int index);

        [FreeFunction]
        public static extern int GetDisplayHeight(int index);

        [FreeFunction]
        public static extern int GetMainDisplayId();
    }
}
