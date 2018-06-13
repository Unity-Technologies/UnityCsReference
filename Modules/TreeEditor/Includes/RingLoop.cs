// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace TreeEditor
{
    public class RingLoop
    {
        public float radius;
        private Matrix4x4 matrix;
        private int segments;
        public float baseOffset;
        private Vector4 animParams;
        private float spreadTop = 0.0f;
        private float spreadBot = 0.0f;
        private float noiseScale = 0.0f;
        private float noiseScaleU = 1.0f;
        private float noiseScaleV = 1.0f;
        private float flareRadius = 0.0f;
        private float flareNoise = 0.0f;
        private float surfAngleCos = 0.0f;
        private float surfAngleSin = 0.0f;

        private int vertOffset;

        private static Perlin perlin = new Perlin();
        private static int noiseSeed = -1;

        public static void SetNoiseSeed(int seed)
        {
            if (noiseSeed != seed)
            {
                perlin.SetSeed(seed);
            }
        }

        public RingLoop Clone()
        {
            RingLoop r2 = new RingLoop();
            r2.radius = radius;
            r2.matrix = matrix;
            r2.segments = segments;
            r2.baseOffset = baseOffset;
            r2.animParams = animParams;
            r2.spreadTop = spreadTop;
            r2.spreadBot = spreadBot;
            r2.noiseScale = noiseScale;
            r2.noiseScaleU = noiseScaleU;
            r2.noiseScaleV = noiseScaleV;
            r2.flareRadius = flareRadius;
            r2.flareNoise = flareNoise;
            r2.surfAngleCos = surfAngleCos;
            r2.surfAngleSin = surfAngleSin;
            return r2;
        }

        public void Reset(float r, Matrix4x4 m, float bOffset, int segs)
        {
            radius = r;
            matrix = m;
            baseOffset = bOffset;
            segments = segs;
            vertOffset = 0;
        }

        public void SetSurfaceAngle(float angleDeg)
        {
            surfAngleCos =  Mathf.Cos(angleDeg * Mathf.Deg2Rad);
            surfAngleSin = -Mathf.Sin(angleDeg * Mathf.Deg2Rad);
        }

        // used for wind animation
        public void SetAnimationProperties(float primaryFactor, float secondaryFactor, float edgeFactor, float phase)
        {
            animParams = new Vector4(primaryFactor, secondaryFactor, edgeFactor, phase);
        }

        public void SetSpread(float top, float bottom)
        {
            spreadTop = top;
            spreadBot = bottom;
        }

        public void SetNoise(float scale, float scaleU, float scaleV)
        {
            noiseScale = scale;
            noiseScaleU = scaleU;
            noiseScaleV = scaleV;
        }

        public void SetFlares(float radius, float noise)
        {
            flareRadius = radius;
            flareNoise = noise;
        }

        public void BuildVertices(List<TreeVertex> verts)
        {
            this.vertOffset = verts.Count;

            for (int i = 0; i <= segments; i++)
            {
                float a = (i * Mathf.PI * 2.0f) / segments;

                TreeVertex v = new TreeVertex();

                float rad = radius;
                float uvx = 1.0f - ((float)(i) / segments);
                float uvy = baseOffset;

                float noiseX = uvx;
                float noiseY = uvy;
                if (i == segments) noiseX = 1.0f; // Loop around

                // Weld spreading
                float spreadFactor = Mathf.Cos(a);
                float spreadRad = 0.0f;
                if (spreadFactor > 0.0f)
                {
                    spreadRad = Mathf.Pow(spreadFactor, 3.0f) * radius * spreadBot;
                }
                else
                {
                    spreadRad = Mathf.Pow(Mathf.Abs(spreadFactor), 3.0f) * radius * spreadTop;
                }

                // Noise..
                float perlinX = noiseX * noiseScaleU;
                float perlinY = noiseY * noiseScaleV;

                rad += (radius * (perlin.Noise(perlinX, perlinY) * noiseScale));

                // Flare..
                perlinX = noiseX * flareNoise;
                rad += flareRadius * Mathf.Abs(perlin.Noise(perlinX, 0.12932f));

                v.pos = matrix.MultiplyPoint(new Vector3(Mathf.Sin(a) * (rad + (spreadRad * 0.25f)), 0.0f, Mathf.Cos(a) * (rad + spreadRad)));

                v.uv0 = new Vector2(uvx, uvy);

                v.SetAnimationProperties(animParams.x, animParams.y, animParams.z, animParams.w);

                verts.Add(v);
            }

            if (radius == 0.0f)
            {
                for (int i = 0; i <= segments; i++)
                {
                    TreeVertex v = verts[i + vertOffset];
                    float a = (i * Mathf.PI * 2.0f) / segments;
                    float b = a - Mathf.PI * 0.5f;

                    Vector3 normal = Vector3.zero;
                    normal.x = Mathf.Sin(a) * surfAngleCos;
                    normal.y = surfAngleSin;
                    normal.z = Mathf.Cos(a) * surfAngleCos;

                    v.nor = Vector3.Normalize(matrix.MultiplyVector(normal));

                    Vector3 tangent = Vector3.zero;
                    tangent.x = Mathf.Sin(b);
                    tangent.y = 0.0f;
                    tangent.z = Mathf.Cos(b);

                    tangent = Vector3.Normalize(matrix.MultiplyVector(tangent));
                    v.tangent = new Vector4(tangent.x, tangent.y, tangent.z, -1.0f);
                }
                return;
            }

            // Rethink normals.. to take noise into account..
            Matrix4x4 matrixInv = matrix.inverse;
            for (int i = 0; i <= segments; i++)
            {
                int a = i - 1;
                if (a < 0) a = segments - 1;
                int b = i + 1;
                if (b > segments) b = 1;

                TreeVertex va = verts[a + vertOffset];
                TreeVertex vb = verts[b + vertOffset];
                TreeVertex vi = verts[i + vertOffset];

                // Use the previous and the next vertices in the loop to create the tangent.
                // It will be tangent to the perfect ring loop in case of no noise.
                Vector3 tangent = Vector3.Normalize(va.pos - vb.pos);
                Vector3 normal = matrixInv.MultiplyVector(va.pos - vb.pos);

                // rotate 90 degrees and normalize
                normal.y = normal.x;
                normal.x = normal.z;
                normal.z = -normal.y;
                normal.y = 0.0f;
                normal.Normalize();

                // tilt accoring to surface angle
                normal.x = surfAngleCos * normal.x;
                normal.y = surfAngleSin;
                normal.z = surfAngleCos * normal.z;

                // set normal
                vi.nor = Vector3.Normalize(matrix.MultiplyVector(normal));

                // set tangent
                vi.tangent.x = tangent.x;
                vi.tangent.y = tangent.y;
                vi.tangent.z = tangent.z;
                vi.tangent.w = -1.0f;
            }
        }

        public void Cap(float sphereFactor, float noise, int mappingMode, float mappingScale, List<TreeVertex> verts, List<TreeTriangle> tris, int materialIndex)
        {
            // half number of segments for the cap, fade with spherefactor..
            int loops = Mathf.Max(1, (int)((segments / 2) * Mathf.Clamp01(sphereFactor)));
            int segs = segments;
            int vloops = loops;
            if (mappingMode == 1)
            {
                // follow mapping requires one extra segment and one extra loop
                segs += 1;
                vloops += 1;
                mappingScale /= Mathf.Max(1.0f, sphereFactor);
            }

            int centerV = verts.Count;

            Vector3 upVector = Vector3.Normalize(matrix.MultiplyVector(Vector3.up));
            Vector3 centerPos = matrix.MultiplyPoint(Vector3.zero);

            // add central vertex
            TreeVertex v = new TreeVertex();
            v.nor = upVector;
            v.pos = centerPos + (v.nor * sphereFactor * radius);
            Vector3 tmpTangent = Vector3.Normalize(matrix.MultiplyVector(Vector3.right));
            v.tangent = new Vector4(tmpTangent.x, tmpTangent.y, tmpTangent.z, -1.0f);
            v.SetAnimationProperties(animParams.x, animParams.y, animParams.z, animParams.w);

            // planar mapping
            if (mappingMode == 0)
            {
                v.uv0 = new Vector2(0.5f, 0.5f);
            }
            else
            {
                v.uv0 = new Vector2(0.5f, baseOffset + sphereFactor * mappingScale);
            }

            verts.Add(v);

            int voffset = verts.Count;

            Matrix4x4 invMatrix = matrix.inverse;

            // add edge vertices
            for (int l = 0; l < vloops; l++)
            {
                float stepAngle = (1.0f - ((float)l / loops)) * Mathf.PI * 0.5f;

                // when doing top projection use this as the scale of the uvs
                float uvScale = Mathf.Sin(stepAngle);

                // position blending
                float posBlend = uvScale;

                // normal blending
                // blend normals depending on sphere factor
                float normalBlend = (uvScale * Mathf.Clamp01(sphereFactor)) + (uvScale * 0.5f * Mathf.Clamp01(1.0f - sphereFactor));

                float cosine = Mathf.Cos(stepAngle);

                for (int i = 0; i < segs; i++)
                {
                    TreeVertex copyV = verts[vertOffset + i];

                    Vector3 uv = invMatrix.MultiplyPoint(copyV.pos).normalized * 0.5f * uvScale;

                    TreeVertex newV = new TreeVertex();
                    newV.pos = (copyV.pos * posBlend) + (centerPos * (1.0f - posBlend)) + (upVector * cosine * sphereFactor * radius);
                    newV.nor = ((copyV.nor * normalBlend) + (upVector * (1.0f - normalBlend))).normalized; // mix center and original vertex normal..
                    newV.SetAnimationProperties(animParams.x, animParams.y, animParams.z, animParams.w);

                    if (mappingMode == 0)
                    {
                        // planar mapping
                        newV.tangent = v.tangent; // same tangent as center vertex..
                        newV.uv0 = new Vector2(0.5f + uv.x, 0.5f + uv.z);
                    }
                    else
                    {
                        // follow mapping
                        newV.tangent = copyV.tangent; // same tangent as copy vertex..
                        newV.uv0 = new Vector2((float)(i) / segments, baseOffset + sphereFactor * cosine * mappingScale);
                    }

                    verts.Add(newV);
                }
            }

            float capNoiseScale = 3.0f;
            for (int i = vertOffset; i < verts.Count; i++)
            {
                // Noise from position..
                float perlinX = verts[i].pos.x * capNoiseScale;
                float perlinY = verts[i].pos.z * capNoiseScale;
                float push = (radius * (perlin.Noise(perlinX, perlinY) * noise));

                // push along center vertex normal.. ie. local up vector
                verts[i].pos += v.nor * push;
            }

            // add triangles
            /*
            int materialIndex = 3;
            if (mappingMode == 1)
            {
                materialIndex = 0;
            }
             */

            for (int l = 0; l < loops; l++)
            {
                for (int i = 0; i < segs; i++)
                {
                    if (l == (vloops - 1))
                    {
                        // inner loop connects to center vertex..
                        // this only happens with planar mapping..
                        int v1 = i + voffset + (segs * l);
                        int v2 = v1 + 1;
                        if (i == (segs - 1)) v2 = voffset + (segs * l);
                        tris.Add(new TreeTriangle(materialIndex, centerV, v1, v2, false, false, false));
                    }
                    else
                    {
                        // connect to next segment
                        int v0 = i + voffset + (segs * l);
                        int v1 = v0 + 1;
                        int v2 = i + voffset + (segs * (l + 1));
                        int v3 = v2 + 1;
                        if (i == (segs - 1))
                        {
                            v1 = voffset + (segs * l);
                            v3 = voffset + (segs * (l + 1));
                        }
                        tris.Add(new TreeTriangle(materialIndex, v0, v1, v3, false, false, false));
                        tris.Add(new TreeTriangle(materialIndex, v0, v3, v2, false, false, false));
                    }
                }
            }
        }

        public void Connect(RingLoop other, List<TreeTriangle> tris, int materialIndex, bool flipTris, bool lowres)
        {
            // Must connect from higher to lower/equal res
            if (other.segments > segments)
            {
                other.Connect(this, tris, materialIndex, true, lowres);
                return;
            }

            if (lowres)
            {
                //
                // Lowres should always have an even number of vertices per ring-loop, and the same amount for each loop
                //
                for (int i = 0; i < (segments / 2); i++)
                {
                    int a0 = 0 + i + other.vertOffset;
                    int a1 = (other.segments / 2) + i + other.vertOffset;

                    int b0 = 0 + i + vertOffset;
                    int b1 = (segments / 2) + i + vertOffset;

                    // reverse order, if needed
                    if (flipTris)
                    {
                        int temp = a0;
                        a0 = a1;
                        a1 = temp;

                        temp = b0;
                        b0 = b1;
                        b1 = temp;
                    }

                    // add to triangle list
                    tris.Add(new TreeTriangle(materialIndex, a0, a1, b0, false, true, false));
                    tris.Add(new TreeTriangle(materialIndex, a1, b1, b0, false, true, false));
                }
            }
            else
            {
                for (int i = 0; i < segments; i++)
                {
                    // find matching vertices on the other ring loop
                    int connect0 = Mathf.Min((int)Mathf.Round((i / (float)segments) * other.segments), other.segments);
                    int connect1 = Mathf.Min((int)Mathf.Round(((i + 1) / (float)segments) * other.segments), other.segments);

                    // next vertex on this ring loop
                    int k = Mathf.Min(i + 1, segments);

                    // first triangle for this segment
                    int v0 = connect0 + other.vertOffset;
                    int v1 = i + vertOffset;
                    int v2 = connect1 + other.vertOffset;

                    // second triangle for this segment
                    int v3 = i + vertOffset;
                    int v4 = k + vertOffset;
                    int v5 = connect1 + other.vertOffset;

                    // reverse order, if needed
                    if (flipTris)
                    {
                        int temp = v1;
                        v1 = v0;
                        v0 = temp;

                        temp = v4;
                        v4 = v3;
                        v3 = temp;
                    }

                    // add to triangle list
                    tris.Add(new TreeTriangle(materialIndex, v0, v1, v2, false, true, false));
                    tris.Add(new TreeTriangle(materialIndex, v3, v4, v5, false, true, false));
                }
            }
        }
    }
}
