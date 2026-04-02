// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/Windowing/ContainerWindow.bindings.h")]
    public partial class EditorWindow
    {
        [FreeFunction("ContainerWindowBindings::MakeModal")]
        internal static extern void Internal_MakeModal(
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(ContainerWindow.NativeHandleMarshaller))]
            ContainerWindow win);

    }
}
