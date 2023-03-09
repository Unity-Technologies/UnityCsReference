// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using Object = UnityEngine.Object;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEditor
{
    // Defines a single modified property.
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeAsStruct]
    public sealed class PropertyModification
    {
        // Object that will be modified
        public Object target;
        // Property path of the property being modified (Matches as SerializedProperty.propertyPath)
        public string propertyPath;
        // The value being applied
        public string value;
        // The value being applied when it is a object reference (which can not be represented as a string)
        public Object objectReference;

        internal void Apply()
        {
            ApplyPropertyModificationToObject(target, this);
        }

        internal void ApplyToObject(Object obj)
        {
            ApplyPropertyModificationToObject(obj, this);
        }

        [NativeMethod("ApplyPropertyModificationToObject", IsFreeFunction = true)]
        [NativeHeader("Editor/Src/Prefabs/PropertyModification.h")]
        extern internal static void ApplyPropertyModificationToObject([NotNull] Object target, PropertyModification value);

        [NativeMethod("ApplyPropertyModificationsToObject", IsFreeFunction = true)]
        [NativeHeader("Editor/Src/Prefabs/PropertyModification.h")]
        extern internal static void ApplyPropertyModificationsToObject([NotNull] Object target, PropertyModification[] value);
    }
}
