// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm = System.ComponentModel;
using uei = UnityEngine.Internal;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;


using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable 649 //Field is never assigned to and will always have its default value

namespace UnityEngine
{
    // NOTE: must match memory layout of native RaycastHit2D
    [UsedByNativeCode]
    public partial struct RaycastHit2D
    {
        private Vector2 m_Centroid;
        private Vector2 m_Point;
        private Vector2 m_Normal;
        private float m_Distance;
        private float m_Fraction;
        private Collider2D m_Collider;

        public Vector2 centroid
        {
            get { return m_Centroid; }
            set { m_Centroid = value; }
        }

        public Vector2 point
        {
            get { return m_Point; }
            set { m_Point = value; }
        }

        public Vector2 normal
        {
            get { return m_Normal; }
            set { m_Normal = value; }
        }

        public float distance
        {
            get { return m_Distance; }
            set { m_Distance = value; }
        }

        public float fraction
        {
            get { return m_Fraction; }
            set { m_Fraction = value; }
        }

        public Collider2D collider
        {
            get { return m_Collider; }
        }


        public Rigidbody2D rigidbody
        {
            get { return collider != null ? collider.attachedRigidbody : null; }
        }

        public Transform transform
        {
            get
            {
                Rigidbody2D body = rigidbody;
                if (body != null)
                    return body.transform;
                else if (collider != null)
                    return collider.transform;
                else
                    return null;
            }
        }

        // Implicitly convert a hit to a boolean based upon whether a collider reference exists or not.
        public static implicit operator bool(RaycastHit2D hit)
        {
            return hit.collider != null;
        }

        // Compare the hit by fraction along the ray.  If no colliders exist then fraction is moved "up".  This allows sorting an array of sparse results.
        public int CompareTo(RaycastHit2D other)
        {
            if (collider == null) return 1;
            if (other.collider == null) return -1;
            return fraction.CompareTo(other.fraction);
        }
    }
}

#pragma warning restore 649
