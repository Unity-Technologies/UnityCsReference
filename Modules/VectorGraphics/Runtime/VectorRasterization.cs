// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.VectorGraphics
{
    public static partial class VectorUtils
    {
        static Color SampleGradient(GradientStop[] stops, float u)
        {
            if (stops == null)
                return Color.white;

            int stop;
            for (stop = 0; stop < stops.Length; stop++)
            {
                if (u < stops[stop].StopPercentage)
                    break;
            }
            if (stop >= stops.Length)
                return stops[stops.Length - 1].Color;
            if (stop == 0)
                return stops[0].Color;

            float percentageRange = stops[stop].StopPercentage - stops[stop - 1].StopPercentage;
            if (percentageRange > Epsilon)
            {
                float blend = (u - stops[stop - 1].StopPercentage) / percentageRange;
                return Color.LerpUnclamped(stops[stop - 1].Color, stops[stop].Color, blend);
            }
            else return stops[stop - 1].Color;
        }

        static Vector2 RayUnitCircleFirstHit(Vector2 rayStart, Vector2 rayDir)
        {
            float tca = Vector2.Dot(-rayStart, rayDir);
            float d2 = Vector2.Dot(rayStart, rayStart) - tca * tca;
            System.Diagnostics.Debug.Assert(d2 <= 1.0f);
            float thc = Mathf.Sqrt(1.0f - d2);
            // solutions for t if the ray intersects
            float t0 = tca - thc;
            float t1 = tca + thc;
            float t = Mathf.Min(t0, t1);
            if (t < 0.0f)
                t = Mathf.Max(t0, t1);
            System.Diagnostics.Debug.Assert(t >= 0);
            return rayStart + rayDir * t;
        }

        static float RadialAddress(Vector2 uv, Vector2 focus)
        {
            uv = (uv - new Vector2(0.5f, 0.5f)) * 2.0f;
            //focus = (focus - new Vector2(0.5f, 0.5f)) * 2.0f;
            var pointOnPerimiter = RayUnitCircleFirstHit(focus, (uv - focus).normalized);

            //return (uv - focus).magnitude / (pointOnPerimiter - focus).magnitude;
            // This is faster
            Vector2 diff = pointOnPerimiter - focus;
            if (Mathf.Abs(diff.x) > Epsilon)
                return (uv.x - focus.x) / diff.x;
            if (Mathf.Abs(diff.y) > Epsilon)
                return (uv.y - focus.y) / diff.y;
            return 0.0f;
        }

        static Color32[] RasterizeGradient(GradientFill gradient, int width, int height)
        {
            Color32[] pixels = new Color32[width * height];

            if (gradient.Type == GradientFillType.Linear)
            {
                int pixIndex = 0;
                for (int x = 0; x < width; x++)
                    pixels[pixIndex++] = SampleGradient(gradient.Stops, x / (float)(width - 1));
                for (int y = 1; y < height; y++)
                {
                    Array.Copy(pixels, 0, pixels, pixIndex, width);
                    pixIndex += width;
                }
            }
            else if (gradient.Type == GradientFillType.Radial)
            {
                int pixIndex = 0;
                for (int y = 0; y < height; y++)
                {
                    float v = y / ((float)height - 1);
                    for (int x = 0; x < width; x++)
                    {
                        float u = x / ((float)width - 1);
                        pixels[pixIndex++] = SampleGradient(gradient.Stops, RadialAddress(new Vector2(u, 1.0f - v), gradient.RadialFocus));
                    }
                }
            }

            return pixels;
        }

        static Color32[] RasterizeGradientStripe(GradientFill gradient, int width)
        {
            Color32[] pixels = new Color32[width];
            for (int x = 0; x < width; ++x)
            {
                float u = x / ((float)width - 1);
                pixels[x] = SampleGradient(gradient.Stops, u);
            }
            return pixels;
        }

        /// <summary>Struct to hold a texture atlas location.</summary>
        public struct PackRectItem
        {
            /// <summary>The position of the entry inside the atlas.</summary>
            public Vector2 Position;

            /// <summary>The size of the entry inside the atlas.</summary>
            public Vector2 Size;

            /// <summary>True if the entry is rotated by 90 degrees.</summary>
            public bool Rotated;

            /// <summary>The fill associated with this entry, may be null.</summary>
            public IFill Fill;

            internal int SettingIndex;
        }

        static List<PackRectItem> PackRects(IList<KeyValuePair<IFill, Vector2>> fillSizes, out Vector2 atlasDims)
        {
            var pack = new List<PackRectItem>(fillSizes.Count);
            var fillSetting = new Dictionary<IFill, int>();
            atlasDims = new Vector2(1024, 1024);
            var maxPos = Vector2.zero;
            var curPos = Vector2.zero;
            float curColThickness = 0.0f;
            int currentSetting = 1;

            foreach (var fillSize in fillSizes)
            {
                var fill = fillSize.Key;
                var size = fillSize.Value;
                if (atlasDims.y < curPos.y + size.y)
                {
                    if (atlasDims.y < size.y)
                        atlasDims.y = size.y;
                    if (curPos.y != 0)
                        curPos.x += curColThickness;
                    curPos.y = 0;
                    curColThickness = size.x;
                }

                curColThickness = Mathf.Max(curColThickness, size.x);

                int setting = 0;
                if (fill != null)
                {
                    if (!fillSetting.TryGetValue(fill, out setting))
                    {
                        setting = currentSetting++;
                        fillSetting[fill] = setting;
                    }
                }

                pack.Add(new PackRectItem() { Position = curPos, Size = size, Fill = fill, SettingIndex = setting });
                maxPos = Vector2.Max(maxPos, curPos + size);
                curPos.y += size.y;
            }
            atlasDims = maxPos;
            return pack;
        }

        static void BlitRawTexture(RawTexture src, RawTexture dest, int destX, int destY, bool rotate)
        {
            if (rotate)
            {
                for (int y = 0; y < src.Height; y++)
                {
                    int srcRowIndex = y * src.Width;
                    int destColumnIndex = destY * dest.Width + destX + y;
                    for (int x = 0; x < src.Width; x++)
                    {
                        int srcIndex = srcRowIndex + x;
                        int destIndex = destColumnIndex + x * dest.Width;
                        dest.Rgba[destIndex] = src.Rgba[srcIndex];
                    }
                }
            }
            else
            {
                for (int y = 0; y < src.Height; y++)
                    Array.Copy(src.Rgba, y * src.Width, dest.Rgba, (destY + y) * dest.Width + destX, src.Width);
            }
        }

        internal static void WriteRawInt2Packed(RawTexture dest, int v0, int v1, int destX, int destY)
        {
            byte r = (byte)(v0/255);
            byte g = (byte)(v0-r*255);
            byte b = (byte)(v1/255);
            byte a = (byte)(v1-b*255);
            int offset = destY * dest.Width + destX;
            dest.Rgba[offset] = new Color32(r, g, b, a);
        }

        internal static void WriteRawFloat4Packed(RawTexture dest, float f0, float f1, float f2, float f3, int destX, int destY)
        {
            byte r = (byte)(f0*255.0f+0.5f);
            byte g = (byte)(f1*255.0f+0.5f);
            byte b = (byte)(f2*255.0f+0.5f);
            byte a = (byte)(f3*255.0f+0.5f);
            int offset = destY * dest.Width + destX;
            if (offset >= dest.Rgba.Length)
            {
                int x = 0;
                ++x;
            }
            dest.Rgba[offset] = new Color32(r, g, b, a);
        }
    }
}
