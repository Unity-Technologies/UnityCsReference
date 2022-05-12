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
        Object m_Target;
        int m_MaterialIndex;

        public Object target => m_Target;
        public int materialIndex => m_MaterialIndex;

        public static PickingObject Empty => new PickingObject(null);
        public int GetInstanceID() => m_Target == null ? 0 : m_Target.GetInstanceID();

        public PickingObject(Object obj, int matIndex = 0)
        {
            m_Target = obj;
            m_MaterialIndex = Mathf.Max(0, matIndex);
        }

        public static explicit operator Object(PickingObject picking) => picking.target;
        public static explicit operator PickingObject(Object @object) => new PickingObject(@object);

        public static bool operator ==(PickingObject lhs, PickingObject rhs) => lhs.Equals(rhs);
        public static bool operator !=(PickingObject lhs, PickingObject rhs) => !(lhs == rhs);

        public bool Equals(PickingObject other)
        {
            return Equals(m_Target, other.m_Target)
                && m_MaterialIndex == other.m_MaterialIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is PickingObject other && Equals(other);
        }

        public override string ToString()
        {
            return $"{m_Target} ({materialIndex})";
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (m_Target != null ? m_Target.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ m_MaterialIndex;
                return hashCode;
            }
        }

        public bool TryGetComponent<T>(out T value) where T : Component
        {
            return TryGetComponent<T>(target, out value);
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

        public bool TryGetGameObject(out GameObject gameObject)
        {
            if (target is GameObject go)
                gameObject = go;
            else if (target is Component component)
                gameObject = component.gameObject;
            else
                gameObject = null;
            return gameObject != null;
        }
    }
}
