// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace UnityEditor
{
    // NOTE : Corresponds to the native TypeFlags
    [Flags]
    enum UnityTypeFlags
    {
        Abstract    = 1 << 0,
        Sealed      = 1 << 1,
        EditorOnly  = 1 << 2
    }

    sealed partial class UnityType
    {
        public string name { get; private set;  }
        public string nativeNamespace { get; private set; }
        public int persistentTypeID { get; private set; }
        public UnityType baseClass { get; private set; }

        public UnityTypeFlags flags { get; private set; }

        public bool isAbstract { get { return (flags & UnityTypeFlags.Abstract) != 0; } }
        public bool isSealed { get { return (flags & UnityTypeFlags.Sealed) != 0; } }
        public bool isEditorOnly { get { return (flags & UnityTypeFlags.EditorOnly) != 0; } }

        uint runtimeTypeIndex;
        uint descendantCount;

        public string qualifiedName
        {
            get { return hasNativeNamespace ? nativeNamespace + "::" + name : name; }
        }

        // NOTE : nativeNamespace == "" for types with no namespace so added this helper for convenience
        // in case the caller wasn't sure whether to compare nativeNamespace with null or empty
        public bool hasNativeNamespace
        {
            get { return nativeNamespace.Length > 0; }
        }

        public bool IsDerivedFrom(UnityType baseClass)
        {
            // NOTE : Type indices are ordered so all derived classes are immediately following the
            // base class allowing us to test inheritance with only a range check
            return (runtimeTypeIndex - baseClass.runtimeTypeIndex) < baseClass.descendantCount;
        }

        public static UnityType FindTypeByPersistentTypeID(int persistentTypeId)
        {
            UnityType result = null;
            ms_idToType.TryGetValue(persistentTypeId, out result);
            return result;
        }

        public static uint TypeCount { get { return (uint)ms_types.Length; } }

        public static UnityType GetTypeByRuntimeTypeIndex(uint index)
        {
            return ms_types[index];
        }

        public static UnityType FindTypeByName(string name)
        {
            UnityType result = null;
            ms_nameToType.TryGetValue(name, out result);
            return result;
        }

        public static UnityType FindTypeByNameCaseInsensitive(string name)
        {
            return ms_types.FirstOrDefault(t => string.Equals(name, t.name, StringComparison.OrdinalIgnoreCase));
        }

        public static ReadOnlyCollection<UnityType> GetTypes()
        {
            return ms_typesReadOnly;
        }

        static UnityType()
        {
            var types = UnityType.Internal_GetAllTypes();

            ms_types = new UnityType[types.Length];
            ms_idToType = new Dictionary<int, UnityType>();
            ms_nameToType = new Dictionary<string, UnityType>();

            for (int i = 0; i < types.Length; ++i)
            {
                // Types are sorted so base < derived and null baseclass is passed from native as 0xffffffff
                UnityType baseClass = null;
                if (types[i].baseClassIndex < types.Length)
                    baseClass = ms_types[types[i].baseClassIndex];

                var newType = new UnityType
                {
                    runtimeTypeIndex = types[i].runtimeTypeIndex,
                    descendantCount = types[i].descendantCount,
                    name = types[i].className,
                    nativeNamespace = types[i].classNamespace,
                    persistentTypeID = types[i].persistentTypeID,
                    baseClass = baseClass,
                    flags = (UnityTypeFlags)types[i].flags
                };

                Debug.Assert(types[i].runtimeTypeIndex == i);

                ms_types[i] = newType;
                ms_typesReadOnly = new ReadOnlyCollection<UnityType>(ms_types);
                ms_idToType[newType.persistentTypeID] = newType;
                ms_nameToType[newType.name] = newType;
            }
        }

        static UnityType[] ms_types;
        static ReadOnlyCollection<UnityType> ms_typesReadOnly;
        static Dictionary<int, UnityType> ms_idToType;
        static Dictionary<string, UnityType> ms_nameToType;
    }
}
