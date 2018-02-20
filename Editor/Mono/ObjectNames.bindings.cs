// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

using UnityObject = UnityEngine.Object;

namespace UnityEditor
{
    // Helper class for constructing displayable names for objects.
    [NativeHeader("Editor/Src/Utility/ObjectNames.bindings.h")]
    public sealed partial class ObjectNames
    {
        // Make a displayable name for a variable.
        [FreeFunction("NicifyVariableName_Internal")]
        public static extern string NicifyVariableName(string name);

        // Class name of an object.
        [FreeFunction("GetClassName_Internal")]
        public static extern string GetClassName(UnityObject obj);

        // Drag and drop title for an object.
        [FreeFunction]
        public static extern string GetDragAndDropTitle(UnityObject obj);

        // Sets the name of an Object.
        [FreeFunction("SetObjectNameSmart")]
        public static extern void SetNameSmart(UnityObject obj, string name);

        [FreeFunction("SetNameSmartWithInstanceID_Internal")]
        internal static extern void SetNameSmartWithInstanceID(int instanceID, string name);

        [FreeFunction("GetUniqueName_Internal")]
        public static extern string GetUniqueName(string[] existingNames, string name);
    }
}
