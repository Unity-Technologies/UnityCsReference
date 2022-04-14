// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityObject = UnityEngine.Object;
using RefId = System.Int64;

namespace UnityEngine.Serialization
{
    [NativeHeader("Runtime/Serialize/ManagedReferenceUtility.h")]
    public sealed class ManagedReferenceUtility
    {
        // Must match the same declarations in "Runtime/Serialize/ReferenceId.h"
        public const RefId RefIdUnknown = -1;
        public const RefId RefIdNull = -2;

        [NativeMethod("SetManagedReferenceIdForObject")]
        static extern bool SetManagedReferenceIdForObjectInternal(UnityObject obj, object scriptObj, RefId refId);

        public static bool SetManagedReferenceIdForObject(UnityObject obj, object scriptObj, RefId refId)
        {
            if (scriptObj == null)
                return refId == RefIdNull; // There is no need to explicitly register RefIdNull

            var valueType = scriptObj.GetType();
            if (valueType == typeof(UnityObject) || valueType.IsSubclassOf(typeof(UnityObject)))
            {
                throw new System.InvalidOperationException(
                    $"Cannot assign an object deriving from UnityEngine.Object to a managed reference. This is not supported.");
            }

            return SetManagedReferenceIdForObjectInternal(obj, scriptObj, refId);
        }

        [NativeMethod("GetManagedReferenceIdForObject")]
        static extern RefId GetManagedReferenceIdForObjectInternal(UnityObject obj, object scriptObj);

        public static  RefId GetManagedReferenceIdForObject(UnityObject obj, object scriptObj)
        {
            return GetManagedReferenceIdForObjectInternal(obj, scriptObj);
        }

        [NativeMethod("GetManagedReference")]
        static extern object GetManagedReferenceInternal(UnityObject obj, RefId id);

        public static object GetManagedReference(UnityObject obj, RefId id)
        {
            return GetManagedReferenceInternal(obj, id);
        }

        [NativeMethod("GetManagedReferenceIds")]
        static extern RefId[] GetManagedReferenceIdsForObjectInternal(UnityObject obj);

        public static RefId[] GetManagedReferenceIds(UnityObject obj)
        {
            return GetManagedReferenceIdsForObjectInternal(obj);
        }
    };
}
