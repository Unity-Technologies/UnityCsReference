// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;

#pragma warning disable 649
#pragma warning disable 169
namespace UnityEditor
{
    [NativeHeader("Editor/Src/GlobalObjectId.h")]
    public struct GlobalObjectId : IEquatable<GlobalObjectId>
    {
        internal SceneObjectIdentifier  m_SceneObjectIdentifier;
        internal GUID                   m_AssetGUID;
        internal int                    m_IdentifierType;

        public ulong targetObjectId { get { return m_SceneObjectIdentifier.TargetObject; } }
        public ulong targetPrefabId { get { return m_SceneObjectIdentifier.TargetPrefab; } }
        public GUID assetGUID { get { return m_AssetGUID; } }
        public int identifierType { get { return m_IdentifierType; } }

        // Converts an Object reference to a global unique ID
        [FreeFunction]
        extern public static GlobalObjectId GetGlobalObjectIdSlow(UnityEngine.Object targetObject);

        [FreeFunction]
        extern public static void GetGlobalObjectIdsSlow(UnityEngine.Object[] objects, [Out] GlobalObjectId[] outputIdentifiers);

        // Converts an Object reference to a global unique ID
        public static GlobalObjectId GetGlobalObjectIdSlow(int instanceId)
        {
            return GetGlobalObjectIdFromInstanceIdSlow(instanceId);
        }

        public static void GetGlobalObjectIdsSlow(int[] instanceIds, [Out] GlobalObjectId[] outputIdentifiers)
        {
            GetGlobalObjectIdsFromInstanceIdsSlow(instanceIds, outputIdentifiers);
        }

        // Converts an Object reference to a global unique ID
        [FreeFunction]
        extern static GlobalObjectId GetGlobalObjectIdFromInstanceIdSlow(int instanceId);

        [FreeFunction]
        extern static void GetGlobalObjectIdsFromInstanceIdsSlow(int[] instanceIds, [Out] GlobalObjectId[] outputIdentifiers);

        override public string ToString()
        {
            return string.Format("GlobalObjectId_V1-{0}-{1}-{2}-{3}", m_IdentifierType, m_AssetGUID, m_SceneObjectIdentifier.TargetObject, m_SceneObjectIdentifier.TargetPrefab);
        }

        public bool Equals(GlobalObjectId other)
        {
            return m_SceneObjectIdentifier.TargetObject == other.m_SceneObjectIdentifier.TargetObject &&
                m_SceneObjectIdentifier.TargetPrefab == other.m_SceneObjectIdentifier.TargetPrefab &&
                m_AssetGUID == other.m_AssetGUID &&
                m_IdentifierType == other.m_IdentifierType;
        }

        public static bool TryParse(string stringValue, out GlobalObjectId id)
        {
            id = new GlobalObjectId();
            string[] tokens = stringValue.Split('-');
            if (tokens.Length != 5 || tokens[0] != "GlobalObjectId_V1")
                return false;

            int identifierType;
            GUID assetGUID;
            ulong targetObject;
            ulong targetPrefab;

            if (!int.TryParse(tokens[1], out identifierType) ||
                !GUID.TryParse(tokens[2], out assetGUID) ||
                !ulong.TryParse(tokens[3], out targetObject) ||
                !ulong.TryParse(tokens[4], out targetPrefab))
                return false;

            id.m_IdentifierType = identifierType;
            id.m_AssetGUID = assetGUID;
            id.m_SceneObjectIdentifier = new SceneObjectIdentifier
            {
                TargetObject = targetObject,
                TargetPrefab = targetPrefab
            };

            return true;
        }

        // Converting one object at a time is incredibly slow. (Have to iterate whole scene to grab one object...)
        // Always prefer using batch API when multiple objects need to be looked up.
        [FreeFunction]
        extern public static UnityEngine.Object GlobalObjectIdentifierToObjectSlow(GlobalObjectId id);
        [FreeFunction]
        extern public static void GlobalObjectIdentifiersToObjectsSlow(GlobalObjectId[] identifiers, [Out] UnityEngine.Object[] outputObjects);

        // Converting one object at a time is incredibly slow. (Have to iterate whole scene to grab one object...)
        // Always prefer using batch API when multiple objects need to be looked up.
        [FreeFunction]
        extern public static int GlobalObjectIdentifierToInstanceIDSlow(GlobalObjectId id);
        [FreeFunction]
        extern public static void GlobalObjectIdentifiersToInstanceIDsSlow(GlobalObjectId[] identifiers, [Out] int[] outputInstanceIDs);
    }
}
