// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Mono/EditorDialogEvents.bindings.h")]
    [NativeHeader("Editor/Src/Dialogs/OpenDialogRegistry.h")]
    public static partial class EditorDialogEvents
    {
        // Managed modals go in the native registry too, since it is read off the main thread.
        [FreeFunction("OpenDialogRegistry::AddManagedModalWindow")]
        static extern int AddToOpenDialogRegistry(string title);

        [FreeFunction("OpenDialogRegistry::Remove")]
        static extern void RemoveFromOpenDialogRegistry(int registryId);
    }
}
