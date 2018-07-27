// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace TreeEditor
{
    public class TreeVertex
    {
        public Vector3 pos;
        public Vector3 nor;
        public Vector4 tangent = new Vector4(1, 0, 0, 1);
        public Vector2 uv0;

        public Vector2 uv1 = new Vector2(0, 0);
        public Color color = new Color(0, 0, 0, 1);

        public bool flag = false;
        //
        // Pack animation properties in color and uv1 channel
        //
        public void SetAnimationProperties(float primaryFactor, float secondaryFactor, float edgeFactor, float phase)
        {
            color.r = phase;
            color.g = edgeFactor;
            uv1.x = primaryFactor;
            uv1.y = secondaryFactor;
        }

        public void SetAmbientOcclusion(float ao)
        {
            color.a = ao;

            // scale animation props
        }

        public void Lerp4(TreeVertex[] tv, Vector2 factor)
        {
            pos = Vector3.Lerp(
                Vector3.Lerp(tv[1].pos, tv[2].pos, factor.x),
                Vector3.Lerp(tv[0].pos, tv[3].pos, factor.x),
                factor.y);
            nor = Vector3.Lerp(
                Vector3.Lerp(tv[1].nor, tv[2].nor, factor.x),
                Vector3.Lerp(tv[0].nor, tv[3].nor, factor.x),
                factor.y).normalized;

            tangent = Vector4.Lerp(
                Vector4.Lerp(tv[1].tangent, tv[2].tangent, factor.x),
                Vector4.Lerp(tv[0].tangent, tv[3].tangent, factor.x),
                factor.y);
            Vector3 tangentNormalized = (new Vector3(tangent.x, tangent.y, tangent.z));
            tangentNormalized.Normalize();
            tangent.x = tangentNormalized.x;
            tangent.y = tangentNormalized.y;
            tangent.z = tangentNormalized.z;

            color = Color.Lerp(
                Color.Lerp(tv[1].color, tv[2].color, factor.x),
                Color.Lerp(tv[0].color, tv[3].color, factor.x),
                factor.y);
        }
    }
}
