// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [Serializable]
    [NativeHeader("Editor/Src/SceneObjectIdentifier.h")]
    struct SceneObjectIdentifier : IEquatable<SceneObjectIdentifier>, IComparable<SceneObjectIdentifier>
    {
        internal ulong TargetObject;
        internal ulong TargetPrefab;

        public int CompareTo(SceneObjectIdentifier other)
        {
            if (TargetObject != other.TargetObject)
                return TargetObject.CompareTo(other.TargetObject);
            if (TargetPrefab != other.TargetPrefab)
                return TargetPrefab.CompareTo(other.TargetPrefab);
            return 0;
        }

        public bool Equals(SceneObjectIdentifier other)
        {
            return TargetObject == other.TargetObject && TargetPrefab == other.TargetPrefab;
        }
    }
}
