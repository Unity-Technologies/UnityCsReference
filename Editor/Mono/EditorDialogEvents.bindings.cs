// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Mono/EditorDialogEvents.bindings.h")]
    public static partial class EditorDialogEvents
    {
        [StaticAccessor(nameof(EditorDialogEvents), StaticAccessorType.DoubleColon)]
        static extern int GetNextDialogEventId();
    }
}
