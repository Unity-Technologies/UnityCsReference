// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    // this class is only partially migrated to the new binding system
    // see GUIUtility.bindings
    [NativeHeader("Runtime/IMGUI/GUIUtility.h")]
    [NativeHeader("Runtime/Mono/MonoBehaviour.h")]
    public partial class GUIUtility
    {
        extern internal static void BeginContainerFromOwner(ScriptableObject owner);
        extern internal static void BeginContainer(ObjectGUIState objectGUIState);

        [NativeMethod("EndContainer")]
        extern internal static void Internal_EndContainer();
    }
}
