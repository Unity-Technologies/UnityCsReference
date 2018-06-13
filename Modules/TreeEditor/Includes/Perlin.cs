// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System;
using UnityEngine;

namespace TreeEditor
{
    /* Perlin noise use example:

    Perlin perlin = new Perlin();
    var value : float = perlin.Noise(2);
    var value : float = perlin.Noise(2, 3, );
    var value : float = perlin.Noise(2, 3, 4);


    SmoothRandom use example:

    var p = SmoothRandom.GetVector3(3);

    */

    public class SmoothRandom
    {
        public static Vector3 GetVector3(float speed)
        {
            float time = Time.time * 0.01F * speed;
            return new Vector3(Get().HybridMultifractal(time, 15.73F, 0.58F), Get().HybridMultifractal(time, 63.94F, 0.58F), Get().HybridMultifractal(time, 0.2F, 0.58F));
        }

        public static float Get(float speed)
        {
            float time = Time.time * 0.01F * speed;
            return Get().HybridMultifractal(time * 0.01F, 15.7F, 0.65F);
        }

        private static FractalNoise Get()
        {
            if (s_Noise == null)
                s_Noise = new FractalNoise(1.27F, 2.04F, 8.36F);
            return s_Noise;
        }

        private static FractalNoise s_Noise;
    }


    public class Perlin
    {
        // Original C code derived from
        // http://astronomy.swin.edu.au/~pbourke/texture/perlin/perlin.c
        // http://astronomy.swin.edu.au/~pbourke/texture/perlin/perlin.h
        const int B = 0x100;
        const int BM = 0xff;
        const int N = 0x1000;

        int[] p = new int[B + B + 2];
        float[,] g3 = new float[B + B + 2, 3];
        float[,] g2 = new float[B + B + 2, 2];
        float[] g1 = new float[B + B + 2];

        float s_curve(float t)
        {
            return t * t * (3.0F - 2.0F * t);
        }

        float lerp(float t, float a, float b)
        {
            return a + t * (b - a);
        }

        void setup(float value, out int b0, out int b1, out float r0, out float r1)
        {
            float t = value + N;
            b0 = ((int)t) & BM;
            b1 = (b0 + 1) & BM;
            r0 = t - (int)t;
            r1 = r0 - 1.0F;
        }

        float at2(float rx, float ry, float x, float y) { return rx * x + ry * y; }
        float at3(float rx, float ry, float rz, float x, float y, float z) { return rx * x + ry * y + rz * z; }

        public float Noise(float arg)
        {
            int bx0, bx1;
            float rx0, rx1, sx, u, v;
            setup(arg, out bx0, out bx1, out rx0, out rx1);

            sx = s_curve(rx0);
            u = rx0 * g1[p[bx0]];
            v = rx1 * g1[p[bx1]];

            return (lerp(sx, u, v));
        }

        public float Noise(float x, float y)
        {
            int bx0, bx1, by0, by1, b00, b10, b01, b11;
            float rx0, rx1, ry0, ry1, sx, sy, a, b, u, v;
            int i, j;

            setup(x, out bx0, out bx1, out rx0, out rx1);
            setup(y, out by0, out by1, out ry0, out ry1);

            i = p[bx0];
            j = p[bx1];

            b00 = p[i + by0];
            b10 = p[j + by0];
            b01 = p[i + by1];
            b11 = p[j + by1];

            sx = s_curve(rx0);
            sy = s_curve(ry0);

            u = at2(rx0, ry0, g2[b00, 0], g2[b00, 1]);
            v = at2(rx1, ry0, g2[b10, 0], g2[b10, 1]);
            a = lerp(sx, u, v);

            u = at2(rx0, ry1, g2[b01, 0], g2[b01, 1]);
            v = at2(rx1, ry1, g2[b11, 0], g2[b11, 1]);
            b = lerp(sx, u, v);

            return lerp(sy, a, b);
        }

        public float Noise(float x, float y, float z)
        {
            int bx0, bx1, by0, by1, bz0, bz1, b00, b10, b01, b11;
            float rx0, rx1, ry0, ry1, rz0, rz1, sy, sz, a, b, c, d, t, u, v;
            int i, j;

            setup(x, out bx0, out bx1, out rx0, out rx1);
            setup(y, out by0, out by1, out ry0, out ry1);
            setup(z, out bz0, out bz1, out rz0, out rz1);

            i = p[bx0];
            j = p[bx1];

            b00 = p[i + by0];
            b10 = p[j + by0];
            b01 = p[i + by1];
            b11 = p[j + by1];

            t = s_curve(rx0);
            sy = s_curve(ry0);
            sz = s_curve(rz0);

            u = at3(rx0, ry0, rz0, g3[b00 + bz0, 0], g3[b00 + bz0, 1], g3[b00 + bz0, 2]);
            v = at3(rx1, ry0, rz0, g3[b10 + bz0, 0], g3[b10 + bz0, 1], g3[b10 + bz0, 2]);
            a = lerp(t, u, v);

            u = at3(rx0, ry1, rz0, g3[b01 + bz0, 0], g3[b01 + bz0, 1], g3[b01 + bz0, 2]);
            v = at3(rx1, ry1, rz0, g3[b11 + bz0, 0], g3[b11 + bz0, 1], g3[b11 + bz0, 2]);
            b = lerp(t, u, v);

            c = lerp(sy, a, b);

            u = at3(rx0, ry0, rz1, g3[b00 + bz1, 0], g3[b00 + bz1, 2], g3[b00 + bz1, 2]);
            v = at3(rx1, ry0, rz1, g3[b10 + bz1, 0], g3[b10 + bz1, 1], g3[b10 + bz1, 2]);
            a = lerp(t, u, v);

            u = at3(rx0, ry1, rz1, g3[b01 + bz1, 0], g3[b01 + bz1, 1], g3[b01 + bz1, 2]);
            v = at3(rx1, ry1, rz1, g3[b11 + bz1, 0], g3[b11 + bz1, 1], g3[b11 + bz1, 2]);
            b = lerp(t, u, v);

            d = lerp(sy, a, b);

            return lerp(sz, c, d);
        }

        void normalize2(ref float x, ref float y)
        {
            float s;

            s = (float)Math.Sqrt(x * x + y * y);
            x = y / s;
            y = y / s;
        }

        void normalize3(ref float x, ref float y, ref float z)
        {
            float s;
            s = (float)Math.Sqrt(x * x + y * y + z * z);
            x = y / s;
            y = y / s;
            z = z / s;
        }

        public Perlin()
        {
            SetSeed(0);
        }

        public void SetSeed(int seed)
        {
            int i, j, k;
            System.Random rnd = new System.Random(seed);

            for (i = 0; i < B; i++)
            {
                p[i] = i;
                g1[i] = (float)(rnd.Next(B + B) - B) / B;

                for (j = 0; j < 2; j++)
                    g2[i, j] = (float)(rnd.Next(B + B) - B) / B;
                normalize2(ref g2[i, 0], ref g2[i, 1]);

                for (j = 0; j < 3; j++)
                    g3[i, j] = (float)(rnd.Next(B + B) - B) / B;


                normalize3(ref g3[i, 0], ref g3[i, 1], ref g3[i, 2]);
            }

            while (--i != 0)
            {
                k = p[i];
                p[i] = p[j = rnd.Next(B)];
                p[j] = k;
            }

            for (i = 0; i < B + 2; i++)
            {
                p[B + i] = p[i];
                g1[B + i] = g1[i];
                for (j = 0; j < 2; j++)
                    g2[B + i, j] = g2[i, j];
                for (j = 0; j < 3; j++)
                    g3[B + i, j] = g3[i, j];
            }
        }
    }

    public class FractalNoise
    {
        public FractalNoise(float inH, float inLacunarity, float inOctaves)
            : this(inH, inLacunarity, inOctaves, null)
        {
        }

        public FractalNoise(float inH, float inLacunarity, float inOctaves, Perlin noise)
        {
            m_Lacunarity = inLacunarity;
            m_Octaves = inOctaves;
            m_IntOctaves = (int)inOctaves;
            m_Exponent = new float[m_IntOctaves + 1];
            float frequency = 1.0F;
            for (int i = 0; i < m_IntOctaves + 1; i++)
            {
                m_Exponent[i] = (float)Math.Pow(m_Lacunarity, -inH);
                frequency *= m_Lacunarity;
            }

            if (noise == null)
                m_Noise = new Perlin();
            else
                m_Noise = noise;
        }

        public float HybridMultifractal(float x, float y, float offset)
        {
            float weight, signal, remainder, result;

            result = (m_Noise.Noise(x, y) + offset) * m_Exponent[0];
            weight = result;
            x *= m_Lacunarity;
            y *= m_Lacunarity;
            int i;
            for (i = 1; i < m_IntOctaves; i++)
            {
                if (weight > 1.0F) weight = 1.0F;
                signal = (m_Noise.Noise(x, y) + offset) * m_Exponent[i];
                result += weight * signal;
                weight *= signal;
                x *= m_Lacunarity;
                y *= m_Lacunarity;
            }
            remainder = m_Octaves - m_IntOctaves;
            result += remainder * m_Noise.Noise(x, y) * m_Exponent[i];

            return result;
        }

        public float RidgedMultifractal(float x, float y, float offset, float gain)
        {
            float weight, signal, result;
            int i;

            signal = Mathf.Abs(m_Noise.Noise(x, y));
            signal = offset - signal;
            signal *= signal;
            result = signal;
            weight = 1.0F;

            for (i = 1; i < m_IntOctaves; i++)
            {
                x *= m_Lacunarity;
                y *= m_Lacunarity;

                weight = signal * gain;
                weight = Mathf.Clamp01(weight);

                signal = Mathf.Abs(m_Noise.Noise(x, y));
                signal = offset - signal;
                signal *= signal;
                signal *= weight;
                result += signal * m_Exponent[i];
            }

            return result;
        }

        public float BrownianMotion(float x, float y)
        {
            float value, remainder;
            long i;

            value = 0.0F;
            for (i = 0; i < m_IntOctaves; i++)
            {
                value = m_Noise.Noise(x, y) * m_Exponent[i];
                x *= m_Lacunarity;
                y *= m_Lacunarity;
            }
            remainder = m_Octaves - m_IntOctaves;
            value += remainder * m_Noise.Noise(x, y) * m_Exponent[i];

            return value;
        }

        private Perlin m_Noise;
        private float[] m_Exponent;
        private int m_IntOctaves;
        private float m_Octaves;
        private float m_Lacunarity;
    }
    /*

    /// This is an alternative implementation of perlin noise
    public class Noise
    {
        public float Noise(float x)
        {
            return Noise(x, 0.5F);
        }

        public float Noise(float x, float y)
        {
            int Xint = (int)x;
            int Yint = (int)y;
            float Xfrac = x - Xint;
            float Yfrac = y - Yint;

            float x0y0 = Smooth_Noise(Xint, Yint);  //find the noise values of the four corners
            float x1y0 = Smooth_Noise(Xint+1, Yint);
            float x0y1 = Smooth_Noise(Xint, Yint+1);
            float x1y1 = Smooth_Noise(Xint+1, Yint+1);

            //interpolate between those values according to the x and y fractions
            float v1 = Interpolate(x0y0, x1y0, Xfrac); //interpolate in x direction (y)
            float v2 = Interpolate(x0y1, x1y1, Xfrac); //interpolate in x direction (y+1)
            float fin = Interpolate(v1, v2, Yfrac);  //interpolate in y direction

            return fin;
        }

        private float Interpolate(float x, float y, float a)
        {
            float b = 1-a;
            float fac1 = (float)(3*b*b - 2*b*b*b);
            float fac2 = (float)(3*a*a - 2*a*a*a);

            return x*fac1 + y*fac2; //add the weighted factors
        }

        private float GetRandomValue(int x, int y)
        {
            x = (x+m_nNoiseWidth) % m_nNoiseWidth;
            y = (y+m_nNoiseHeight) % m_nNoiseHeight;
            float fVal = (float)m_aNoise[(int)(m_fScaleX*x), (int)(m_fScaleY*y)];
            return fVal/255*2-1f;
        }

        private float Smooth_Noise(int x, int y)
        {
            float corners = ( Noise2d(x-1, y-1) + Noise2d(x+1, y-1) + Noise2d(x-1, y+1) + Noise2d(x+1, y+1) ) / 16.0f;
            float sides = ( Noise2d(x-1, y) +Noise2d(x+1, y) + Noise2d(x, y-1) + Noise2d(x, y+1) ) / 8.0f;
            float center = Noise2d(x, y) / 4.0f;
            return corners + sides + center;
        }

        private float Noise2d(int x, int y)
        {
            x = (x+m_nNoiseWidth) % m_nNoiseWidth;
            y = (y+m_nNoiseHeight) % m_nNoiseHeight;

            float fVal = (float)m_aNoise[(int)(m_fScaleX*x), (int)(m_fScaleY*y)];

            return fVal/255*2-1f;
        }

        public Noise()
        {
            m_nNoiseWidth = 100;
            m_nNoiseHeight = 100;
            m_fScaleX = 1.0F;
            m_fScaleY = 1.0F;
            System.Random rnd = new System.Random();
            m_aNoise = new int[m_nNoiseWidth,m_nNoiseHeight];
            for (int x = 0; x<m_nNoiseWidth; x++)
            {
                for (int y = 0; y<m_nNoiseHeight; y++)
                {
                    m_aNoise[x,y] = rnd.Next(255);
                }
            }
        }

        private int[,] m_aNoise;
        protected int m_nNoiseWidth, m_nNoiseHeight;
        private float m_fScaleX, m_fScaleY;
    }


    /*  Yet another perlin noise implementation. This one is not even completely ported to C#


        float noise1[];
        float noise2[];
        float noise3[];
        int indices[];

        float PerlinSmoothStep (float t)
        {
            return t * t * (3.0f - 2.0f * t);
        }

        float PerlinLerp(float t, float a, float b)
        {
            return a + t * (b - a);
        }

        float PerlinRand()
        {
            return Random.rand () / float(RAND_MAX)  * 2.0f - 1.0f;
        }


        PerlinNoise::PerlinNoise ()
        {
            long i, j, k;
            float x, y, z, denom;

            Random rnd = new Random();


            noise1 = new float[1 * (PERLIN_B + PERLIN_B + 2)];
            noise2 = new float[2 * (PERLIN_B + PERLIN_B + 2)];
            noise3 = new float[3 * (PERLIN_B + PERLIN_B + 2)];
            indices = new long[PERLIN_B + PERLIN_B + 2];

            for (i = 0; i < PERLIN_B; i++)
            {
                indices[i] = i;

                x = PerlinRand();
                y = PerlinRand();
                z = PerlinRand();

                noise1[i] = x;

                denom = sqrt(x * x + y * y);
                if (denom > 0.0001f) denom = 1.0f / denom;

                j = i << 1;
                noise2[j + 0] = x * denom;
                noise2[j + 1] = y * denom;

                denom = sqrt(x * x + y * y + z * z);
                if (denom > 0.0001f) denom = 1.0f / denom;

                j += i;
                noise3[j + 0] = x * denom;
                noise3[j + 1] = y * denom;
                noise3[j + 2] = z * denom;
            }

            while (--i != 0)
            {
                j = rand() & PERLIN_BITMASK;
                std::swap (indices[i], indices[j]);
            }

            for (i = 0; i < PERLIN_B + 2; i++)
            {
                j = i + PERLIN_B;

                indices[j] = indices[i];

                noise1[j] = noise1[i];

                j = j << 1;
                k = i << 1;
                noise2[j + 0] = noise2[k + 0];
                noise2[j + 1] = noise2[k + 1];

                j += i + PERLIN_B;
                k += i + PERLIN_B;
                noise3[j + 0] = noise3[k + 0];
                noise3[j + 1] = noise3[k + 1];
                noise3[j + 2] = noise3[k + 2];
            }
        }

        PerlinNoise::~PerlinNoise ()
        {
            delete []noise1;
            delete []noise2;
            delete []noise3;
            delete []indices;
        }

        void PerlinSetup (float v, long& b0, long& b1, float& r0, float& r1);
        void PerlinSetup(
            float v,
            long& b0,
            long& b1,
            float& r0,
            float& r1)
        {
            v += PERLIN_N;

            long vInt = (long)v;

            b0 = vInt & PERLIN_BITMASK;
            b1 = (b0 + 1) & PERLIN_BITMASK;
            r0 = v - (float)vInt;
            r1 = r0 - 1.0f;
        }


        float PerlinNoise::Noise1 (float x)
        {
            long bx0, bx1;
            float rx0, rx1, sx, u, v;

            PerlinSetup(x, bx0, bx1, rx0, rx1);

            sx = PerlinSmoothStep(rx0);

            u = rx0 * noise1[indices[bx0]];
            v = rx1 * noise1[indices[bx1]];

            return PerlinLerp (sx, u, v);
        }

        float PerlinNoise::Noise2(float x, float y)
        {
            long bx0, bx1, by0, by1, b00, b01, b10, b11;
            float rx0, rx1, ry0, ry1, sx, sy, u, v, a, b;

            PerlinSetup (x, bx0, bx1, rx0, rx1);
            PerlinSetup (y, by0, by1, ry0, ry1);

            sx = PerlinSmoothStep (rx0);
            sy = PerlinSmoothStep (ry0);

            b00 = indices[indices[bx0] + by0] << 1;
            b10 = indices[indices[bx1] + by0] << 1;
            b01 = indices[indices[bx0] + by1] << 1;
            b11 = indices[indices[bx1] + by1] << 1;

            u = rx0 * noise2[b00 + 0] + ry0 * noise2[b00 + 1];
            v = rx1 * noise2[b10 + 0] + ry0 * noise2[b10 + 1];
            a = PerlinLerp (sx, u, v);

            u = rx0 * noise2[b01 + 0] + ry1 * noise2[b01 + 1];
            v = rx1 * noise2[b11 + 0] + ry1 * noise2[b11 + 1];
            b = PerlinLerp (sx, u, v);

            u = PerlinLerp (sy, a, b);

            return u;
        }

        float PerlinNoise::Noise3(float x, float y, float z)
        {
            long bx0, bx1, by0, by1, bz0, bz1, b00, b10, b01, b11;
            float rx0, rx1, ry0, ry1, rz0, rz1, *q, sy, sz, a, b, c, d, t, u, v;

            PerlinSetup (x, bx0, bx1, rx0, rx1);
            PerlinSetup (y, by0, by1, ry0, ry1);
            PerlinSetup (z, bz0, bz1, rz0, rz1);

            b00 = indices[indices[bx0] + by0] << 1;
            b10 = indices[indices[bx1] + by0] << 1;
            b01 = indices[indices[bx0] + by1] << 1;
            b11 = indices[indices[bx1] + by1] << 1;

            t = PerlinSmoothStep (rx0);
            sy = PerlinSmoothStep (ry0);
            sz = PerlinSmoothStep (rz0);

            #define at3(rx,ry,rz) ( rx * q[0] + ry * q[1] + rz * q[2] )

            q = &noise3[b00 + bz0]; u = at3(rx0,ry0,rz0);
            q = &noise3[b10 + bz0]; v = at3(rx1,ry0,rz0);
            a = PerlinLerp(t, u, v);

            q = &noise3[b01 + bz0]; u = at3(rx0,ry1,rz0);
            q = &noise3[b11 + bz0]; v = at3(rx1,ry1,rz0);
            b = PerlinLerp(t, u, v);

            c = PerlinLerp(sy, a, b);

            q = &noise3[b00 + bz1]; u = at3(rx0,ry0,rz1);
            q = &noise3[b10 + bz1]; v = at3(rx1,ry0,rz1);
            a = PerlinLerp(t, u, v);

            q = &noise3[b01 + bz1]; u = at3(rx0,ry1,rz1);
            q = &noise3[b11 + bz1]; v = at3(rx1,ry1,rz1);
            b = PerlinLerp(t, u, v);

            d = PerlinLerp(sy, a, b);

            return PerlinLerp (sz, c, d);
        }
    */
}
