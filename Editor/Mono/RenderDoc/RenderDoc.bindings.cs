// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditorInternal
{
    [NativeHeader("Editor/Src/RenderDoc/RenderDoc.h")]
    [StaticAccessor("RenderDoc", StaticAccessorType.DoubleColon)]
    internal partial class RenderDoc
    {
        public static extern bool IsInstalled();
        public static extern bool IsLoaded();
        public static extern bool IsSupported();
        public static extern void Load();
    }
}
