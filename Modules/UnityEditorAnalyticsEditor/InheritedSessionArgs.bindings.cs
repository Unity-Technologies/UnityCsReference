// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Analytics
{
    [RequiredByNativeCode]
    [NativeHeader("Modules/UnityEditorAnalyticsEditor/UnityEditorAnalyticsManager.h")]
    [StaticAccessor("UnityEditor::Analytics", StaticAccessorType.DoubleColon)]
    internal static class InheritedSessionArgs
    {
        [NativeMethod("BuildInheritedSessionArgsForScripting")]
        internal static extern string[] Build();
    }
}
