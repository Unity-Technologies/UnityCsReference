// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace TreeEditor
{
    public class TreeAOSphere
    {
        public bool flag;
        public float area;
        public float radius;
        public float density = 1.0f;
        public Vector3 position;

        public TreeAOSphere(Vector3 pos, float radius, float density)
        {
            this.position = pos;
            this.radius = radius;
            this.area = radius * radius; // PI factored out..
            this.density = density;
        }

        public float PointOcclusion(Vector3 pos, Vector3 nor)
        {
            Vector3 delta = position - pos;
            float ds = delta.sqrMagnitude;
            float d2 = Mathf.Max(0.0f, ds - area);
            if (ds > Mathf.Epsilon)
            {
                delta.Normalize();
            }
            return (1.0f - (1.0f / Mathf.Sqrt(area / d2 + 1))) * Mathf.Clamp01(4.0f * Vector3.Dot(nor, delta));
        }
    }
}
