// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityEditor
{
    [StructLayout(LayoutKind.Sequential)]
    struct PickingObject : System.IEquatable<PickingObject>
    {
        EntityId m_TargetId;
        int m_MaterialIndex;

        public EntityId targetId => m_TargetId;
        public int materialIndex => m_MaterialIndex;

        public static PickingObject Empty => new (EntityId.None);

        public PickingObject(EntityId id, int matIndex = 0)
        {
            m_TargetId = id;
            m_MaterialIndex = Mathf.Max(0, matIndex);
        }

        public static explicit operator EntityId(PickingObject picking) => picking.targetId;
        public static explicit operator PickingObject(EntityId id) => new (id);

        public static bool operator ==(PickingObject lhs, PickingObject rhs) => lhs.Equals(rhs);
        public static bool operator !=(PickingObject lhs, PickingObject rhs) => !(lhs == rhs);

        public bool Equals(PickingObject other)
        {
            return Equals(m_TargetId, other.m_TargetId)
                && m_MaterialIndex == other.m_MaterialIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is PickingObject other && Equals(other);
        }

        public override string ToString()
        {
            return $"{m_TargetId} ({materialIndex})";
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = m_TargetId.GetHashCode();
                hashCode = (hashCode * 397) ^ m_MaterialIndex;
                return hashCode;
            }
        }

        public bool TryGetComponent<T>(out T value) where T : Component
        {
            var obj = EditorUtility.EntityIdToObject(targetId);
            return TryGetComponent(obj, out value);
        }

        internal static bool TryGetComponent<T>(Object obj, out T value) where T : Component
        {
            if (obj is T cast)
                value = cast;
            else if (obj is GameObject go)
                return go.TryGetComponent(out value);
            else if (obj is Component component)
                return component.TryGetComponent(out value);
            else
                value = null;

            return value != null;
        }

        public bool TryGetParent(out Transform parent)
        {
            if (TryGetComponent<Transform>(out var transform))
                return (parent = transform.parent) != null;
            parent = null;
            return false;
        }

        public bool TryGetObject(out Object obj)
        {
            obj = EditorUtility.EntityIdToObject(targetId);
            return obj != null;
        }

        public bool TryGetGameObject(out GameObject gameObject)
        {
            var obj = EditorUtility.EntityIdToObject(targetId);

            if (obj is GameObject go)
                gameObject = go;
            else if (obj is Component component)
                gameObject = component.gameObject;
            else
                gameObject = null;
            return gameObject != null;
        }
    }
}
