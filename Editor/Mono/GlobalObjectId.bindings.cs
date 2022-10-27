// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor.Experimental;

#pragma warning disable 649
#pragma warning disable 169
namespace UnityEditor
{
    [Serializable]
    [NativeHeader("Editor/Src/GlobalObjectId.h")]
    public struct GlobalObjectId : IEquatable<GlobalObjectId>, IComparable<GlobalObjectId>
    {
        internal SceneObjectIdentifier  m_SceneObjectIdentifier;
        internal GUID                   m_AssetGUID;
        internal int                    m_IdentifierType;

        internal enum IdentifierType { NullIdentifier = 0, ImportedAsset = 1, SceneObject = 2, SourceAsset = 3, BuiltInAsset = 4 };

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
            return m_SceneObjectIdentifier.Equals(other.m_SceneObjectIdentifier) &&
                m_AssetGUID == other.m_AssetGUID &&
                m_IdentifierType == other.m_IdentifierType;
        }

        public int CompareTo(GlobalObjectId other)
        {
            if (m_AssetGUID != other.m_AssetGUID)
                return m_AssetGUID.CompareTo(other.assetGUID);
            if (!m_SceneObjectIdentifier.Equals(other.m_SceneObjectIdentifier))
                return m_SceneObjectIdentifier.CompareTo(other.m_SceneObjectIdentifier);
            if (m_IdentifierType != other.m_IdentifierType)
                return m_IdentifierType.CompareTo(m_IdentifierType);
            return 0;
        }

        public static bool TryParse(string stringValue, out GlobalObjectId id)
        {
            id = new GlobalObjectId();
            string[] tokens = stringValue.Split('-');
            if (tokens.Length != 5 || !string.Equals(tokens[0], "GlobalObjectId_V1", StringComparison.Ordinal))
                return false;

            if (!int.TryParse(tokens[1], out var identifierType) ||
                !GUID.TryParse(tokens[2], out var assetGUID) ||
                !ulong.TryParse(tokens[3], out var targetObject) ||
                !ulong.TryParse(tokens[4], out var targetPrefab))
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
