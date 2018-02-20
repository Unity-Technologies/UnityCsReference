// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/Highlighter/HighlighterCore.bindings.h")]
    public sealed partial class Highlighter
    {
        // Internal API

        [NativeProperty("s_SearchMode", false, TargetType.Field)]
        internal static extern HighlightSearchMode searchMode { get; set; }

        [FreeFunction("Internal_Handle")]
        internal static extern void Handle(Rect position, string text);

        public static extern string activeText
        {
            [FreeFunction]
            get;
            [FreeFunction]
            private set;
        }

        [NativeProperty("s_ActiveRect", false, TargetType.Field)]
        public static extern Rect activeRect { get; private set; }

        [NativeProperty("s_ActiveVisible", false, TargetType.Field)]
        public static extern bool activeVisible { get; private set; }
    }
}
