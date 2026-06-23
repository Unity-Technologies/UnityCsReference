// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using System;
using UnityEngine.Bindings;

namespace UnityEngine.TextCore
{
    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
    internal struct RichTextLinkInfo
    {
        public int    id;
        public bool   isHyperlink;
        public string value;
    }

    [NativeHeader("Modules/TextCoreTextEngine/Native/RichTextParser.h")]
    [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
    internal static class NativeRichTextParser
    {
        [NativeMethod(Name = "RichTextParser::GetAllLinks", IsThreadSafe = true)]
        public static extern RichTextLinkInfo[] GetAllLinks(IntPtr textGenerationInfo);
    }
}
