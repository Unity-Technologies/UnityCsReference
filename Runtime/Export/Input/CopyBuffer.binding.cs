// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
namespace UnityEngine
{
    [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
    [NativeHeader("Runtime/Utilities/CopyPaste.h")]
    internal partial class StytemCopyBuffer
    {
        // Get access to the system-wide pasteboard.
        public static extern string systemCopyBuffer
        {
            [FreeFunction("GetCopyBuffer")]
            get;
            [FreeFunction("SetCopyBuffer")]
            set;
        }
    }
}
