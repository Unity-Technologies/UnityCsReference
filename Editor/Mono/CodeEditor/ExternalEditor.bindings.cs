// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace Unity.CodeEditor
{
    [NativeHeader("Editor/Platform/Interface/ExternalEditor.h")]
    internal class ExternalEditor
    {
        [FreeFunction("PlatformSpecificOpenFileAtLine")]
        internal static extern bool OSOpenFileWithArgument(string appPath, string arguments);
    }
}
