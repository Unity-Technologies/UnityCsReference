// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("Runtime/Interfaces/IRaycast.h")]
    [NativeHeader("PhysicsScriptingClasses.h")]
    [NativeHeader("Modules/Physics/RaycastHit.h")]
    [UsedByNativeCode]
    public partial struct RaycastHit
    {
        [NativeName("point")] internal Vector3 m_Point;
        [NativeName("normal")] internal Vector3 m_Normal;
        [NativeName("faceID")] internal uint m_FaceID;
        [NativeName("distance")] internal float m_Distance;
        [NativeName("uv")] internal Vector2 m_UV;
        [NativeName("collider")] internal int m_Collider;

        public Collider collider { get { return Object.FindObjectFromInstanceID(m_Collider) as Collider; } }
        public int colliderInstanceID { get { return m_Collider; } }

        public Vector3 point { get { return m_Point; } set { m_Point = value; } }
        public Vector3 normal { get { return m_Normal; } set { m_Normal = value; } }
        public Vector3 barycentricCoordinate { get { return new Vector3(1.0F - (m_UV.y + m_UV.x), m_UV.x, m_UV.y); } set { m_UV = value; } }
        public float distance { get { return m_Distance; } set { m_Distance = value; } }
        public int triangleIndex { get { return (int)m_FaceID; } }

        [NativeMethod("CalculateRaycastTexCoord", true, true)]
        extern static private Vector2 CalculateRaycastTexCoord(int colliderInstanceID, Vector2 uv, Vector3 pos, uint face, int textcoord);

        public Vector2 textureCoord { get { return CalculateRaycastTexCoord(m_Collider, m_UV, m_Point, m_FaceID, 0); } }
        public Vector2 textureCoord2 { get { return CalculateRaycastTexCoord(m_Collider, m_UV, m_Point, m_FaceID, 1); } }

        public Transform transform
        {
            get
            {
                Rigidbody body = rigidbody;
                if (body != null)
                    return body.transform;
                else if (collider != null)
                    return collider.transform;
                else
                    return null;
            }
        }

        public Rigidbody rigidbody { get { return collider != null ? collider.attachedRigidbody : null; } }
        public ArticulationBody articulationBody { get { return collider != null ? collider.attachedArticulationBody : null; } }

        public Vector2 lightmapCoord
        {
            get
            {
                Vector2 coord = CalculateRaycastTexCoord(m_Collider, m_UV, m_Point, m_FaceID, 1);
                if (collider.GetComponent<Renderer>() != null)
                {
                    Vector4 st = collider.GetComponent<Renderer>().lightmapScaleOffset;
                    coord.x = coord.x * st.x + st.z;
                    coord.y = coord.y * st.y + st.w;
                }
                return coord;
            }
        }
    }
}
