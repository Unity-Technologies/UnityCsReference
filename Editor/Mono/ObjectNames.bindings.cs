// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Bindings;

using UnityObject = UnityEngine.Object;

namespace UnityEditor
{
    // Helper class for constructing displayable names for objects.
    [NativeHeader("Editor/Src/Utility/ObjectNames.bindings.h")]
    public sealed partial class ObjectNames
    {
        // Class name of an object.
        [FreeFunction("GetClassName_Internal")]
        public static extern string GetClassName([NotNull] UnityObject obj);

        // Drag and drop title for an object.
        [FreeFunction]
        public static extern string GetDragAndDropTitle(UnityObject obj);

        // Sets the name of an Object.
        [FreeFunction("SetObjectNameSmart")]
        public static extern void SetNameSmart(UnityObject obj, string name);

        [FreeFunction("SetNameSmartWithEntityId_Internal")]
        internal static extern void SetNameSmartWithEntityId(UnityEngine.EntityId entityId, string name);

        [FreeFunction("GetUniqueName_Internal")]
        public static extern string GetUniqueName(string[] existingNames, string name);

        [FreeFunction("GetUniqueObjectName_Internal")]
        public static extern string GetUniqueObjectName(List<UnityEngine.Object> existingObjects, string name);
    }
}
