// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Internal;

namespace UnityEngine
{
    // GUI STUFF
    // --------------------------------
    [NativeHeader("Modules/IMGUI/GUIStyle.h")]
    [UsedByNativeCode]
    public partial class RectOffset
    {
        [ThreadAndSerializationSafe]
        private static extern IntPtr InternalCreate();

        [ThreadAndSerializationSafe]
        private static extern void InternalDestroy(IntPtr ptr);

        // Left edge size.
        [NativeProperty("left", false, TargetType.Field)]
        public extern int left { get; set; }

        // Right edge size.
        [NativeProperty("right", false, TargetType.Field)]
        public extern int right { get; set; }

        // Top edge size.
        [NativeProperty("top", false, TargetType.Field)]
        public extern int top { get; set; }

        // Bottom edge size.
        [NativeProperty("bottom", false, TargetType.Field)]
        public extern int bottom { get; set; }

        // shortcut for left + right (RO)
        public extern int horizontal { get; }

        // shortcut for top + bottom (RO)
        public extern int vertical { get; }

        // Add the border offsets to a /rect/.
        public extern Rect Add(Rect rect);

        // Remove the border offsets from a /rect/.
        public extern Rect Remove(Rect rect);
    }
}
