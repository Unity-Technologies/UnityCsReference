// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    public class AudioCurveRendering
    {
        // This slight adjustment is needed to make sure that numerical imprecision doesn't suddenly snap to the wrong pixel (causing vertical gaps and overdrawn lines)
        private static float pixelEpsilon = 0.005f;

        public delegate float AudioCurveEvaluator(float x);
        public delegate float AudioCurveAndColorEvaluator(float x, out Color col);
        public delegate void AudioMinMaxCurveAndColorEvaluator(float x, out Color col, out float minValue, out float maxValue);
        public static readonly Color kAudioOrange = new Color(255 / 255f, 168 / 255f, 7 / 255f);

        // -------------
        // Frame, background and clipping utils
        // -------------

        // Returns the content rect (clipped)
        public static Rect BeginCurveFrame(Rect r)
        {
            DrawCurveBackground(r);

            // Frame is subtracted from rect
            r = DrawCurveFrame(r);

            // Content is clipped with group
            GUI.BeginGroup(r);
            return new Rect(0, 0, r.width, r.height);
        }

        public static void EndCurveFrame()
        {
            GUI.EndGroup();
        }

        // Returns content rect
        public static Rect DrawCurveFrame(Rect r)
        {
            if (Event.current.type != EventType.Repaint)
                return r;

            EditorStyles.colorPickerBox.Draw(r, false, false, false, false);

            // Adjust rect for frame: colorPickerBox style has 1 px border
            r.x += 1f;
            r.y += 1f;
            r.width -= 2f;
            r.height -= 2f;
            return r;
        }

        public static void DrawCurveBackground(Rect r)
        {
            EditorGUI.DrawRect(r, new Color(0.3f, 0.3f, 0.3f));
        }

        // -------------
        // Curve rendering -- do not remove any of these, as they may be used by custom GUIs of native audio plugins
        // -------------

        public static void DrawFilledCurve(Rect r, AudioCurveEvaluator eval, Color curveColor)
        {
            DrawFilledCurve(
                r,
                delegate(float x, out Color color)
                {
                    color = curveColor;
                    return eval(x);
                }
                );
        }

        public static void DrawFilledCurve(Rect r, AudioCurveAndColorEvaluator eval)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            HandleUtility.ApplyWireMaterial();

            GL.Begin(GL.LINES);

            // Adjust by a half pixel to each side so that the transition covers a full pixel.
            // This is needed for very slowly rising edges.
            float pixelScale = (float)EditorGUIUtility.pixelsPerPoint;
            float pixelSize = 1.0f / pixelScale;
            float pixelHalfSize = 0.5f * pixelSize;
            float pixelWidth = Mathf.Ceil(r.width) * pixelScale;
            float startx = Mathf.Floor(r.x) + pixelEpsilon;
            float wx = 1.0f / (float)(pixelWidth - 1);
            float cy = r.height * 0.5f;
            float my = r.y + 0.5f * r.height;
            float y1 = r.y + r.height;
            Color color;

            float py = Mathf.Clamp(cy * eval(0.0f, out color), -cy, cy);
            for (int x = 0; x < pixelWidth; x++)
            {
                float nx = startx + x * pixelSize;
                float ny = Mathf.Clamp(cy * eval(x * wx, out color), -cy, cy);
                float e1 = Mathf.Min(ny, py) - pixelHalfSize;
                float e2 = Mathf.Max(ny, py) + pixelHalfSize;
                GL.Color(new Color(color.r, color.g, color.b, 0.0f));
                AudioMixerDrawUtils.Vertex(nx, my - e2);
                GL.Color(color);
                AudioMixerDrawUtils.Vertex(nx, my - e1);
                AudioMixerDrawUtils.Vertex(nx, my - e1);
                AudioMixerDrawUtils.Vertex(nx, y1);
                py = ny;
            }

            GL.End();
        }

        // Enforces minValue <= maxValue
        private static void Sort2(ref float minValue, ref float maxValue)
        {
            if (minValue > maxValue)
            {
                float tmp = minValue;
                minValue = maxValue;
                maxValue = tmp;
            }
        }

        public static void DrawMinMaxFilledCurve(Rect r, AudioMinMaxCurveAndColorEvaluator eval)
        {
            HandleUtility.ApplyWireMaterial();

            GL.Begin(GL.LINES);

            // Adjust by a half pixel to each side so that the transition covers a full pixel.
            // This is needed for very slowly rising edges.
            float pixelScale = (float)EditorGUIUtility.pixelsPerPoint;
            float pixelSize = 1.0f / pixelScale;
            float pixelHalfSize = 0.5f * pixelSize;
            float pixelWidth = Mathf.Ceil(r.width) * pixelScale;
            float startx = Mathf.Floor(r.x) + pixelEpsilon;
            float wx = 1.0f / (float)(pixelWidth - 1);
            float cy = r.height * 0.5f;
            float my = r.y + 0.5f * r.height;
            float minValue, maxValue;
            Color color;
            eval(0.0001f, out color, out minValue, out maxValue); Sort2(ref minValue, ref maxValue);
            float pyMax = my - cy * Mathf.Clamp(maxValue, -1.0f, 1.0f);
            float pyMin = my - cy * Mathf.Clamp(minValue, -1.0f, 1.0f);
            float y1 = r.y, y2 = r.y + r.height;
            for (int x = 0; x < pixelWidth; x++)
            {
                float nx = startx + x * pixelSize;
                eval(x * wx, out color, out minValue, out maxValue); Sort2(ref minValue, ref maxValue);
                Color edgeColor = new Color(color.r, color.g, color.b, 0.0f);
                float nyMax = my - cy * Mathf.Clamp(maxValue, -1.0f, 1.0f);
                float nyMin = my - cy * Mathf.Clamp(minValue, -1.0f, 1.0f);
                float a = Mathf.Clamp(Mathf.Min(nyMax, pyMax) - pixelHalfSize, y1, y2);
                float b = Mathf.Clamp(Mathf.Max(nyMax, pyMax) + pixelHalfSize, y1, y2);
                float c = Mathf.Clamp(Mathf.Min(nyMin, pyMin) - pixelHalfSize, y1, y2);
                float d = Mathf.Clamp(Mathf.Max(nyMin, pyMin) + pixelHalfSize, y1, y2);
                Sort2(ref a, ref c); Sort2(ref b, ref d); Sort2(ref a, ref b); Sort2(ref c, ref d); Sort2(ref b, ref c);
                GL.Color(edgeColor);
                AudioMixerDrawUtils.Vertex(nx, a);
                GL.Color(color);
                AudioMixerDrawUtils.Vertex(nx, b);
                AudioMixerDrawUtils.Vertex(nx, b);
                AudioMixerDrawUtils.Vertex(nx, c);
                AudioMixerDrawUtils.Vertex(nx, c);
                GL.Color(edgeColor);
                AudioMixerDrawUtils.Vertex(nx, d);
                pyMin = nyMin;
                pyMax = nyMax;
            }

            GL.End();
        }

        public static void DrawSymmetricFilledCurve(Rect r, AudioCurveAndColorEvaluator eval)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            HandleUtility.ApplyWireMaterial();

            GL.Begin(GL.LINES);

            // Adjust by a half pixel to each side so that the transition covers a full pixel.
            // This is needed for very slowly rising edges.
            float pixelScale = (float)EditorGUIUtility.pixelsPerPoint;
            float pixelSize = 1.0f / pixelScale;
            float pixelHalfSize = 0.5f * pixelSize;
            float pixelWidth = Mathf.Ceil(r.width) * pixelScale;
            float startx = Mathf.Floor(r.x) + pixelEpsilon;
            float wx = 1.0f / (float)(pixelWidth - 1);
            float cy = r.height * 0.5f;
            float my = r.y + 0.5f * r.height;
            Color color;

            float py = Mathf.Clamp(cy * eval(0.0001f, out color), 0.0f, cy);
            for (int x = 0; x < pixelWidth; x++)
            {
                float nx = startx + x * pixelSize;
                float ny = Mathf.Clamp(cy * eval(x * wx, out color), 0.0f, cy);
                float e1 = Mathf.Max(Mathf.Min(ny, py) - pixelHalfSize, 0.0f); // Avoid self-intersection
                float e2 = Mathf.Min(Mathf.Max(ny, py) + pixelHalfSize, cy);
                Color edgeColor = new Color(color.r, color.g, color.b, 0.0f);
                GL.Color(edgeColor);
                AudioMixerDrawUtils.Vertex(nx, my - e2);
                GL.Color(color);
                AudioMixerDrawUtils.Vertex(nx, my - e1);
                AudioMixerDrawUtils.Vertex(nx, my - e1);
                AudioMixerDrawUtils.Vertex(nx, my + e1);
                AudioMixerDrawUtils.Vertex(nx, my + e1);
                GL.Color(edgeColor);
                AudioMixerDrawUtils.Vertex(nx, my + e2);
                py = ny;
            }

            GL.End();
        }

        public static void DrawCurve(Rect r, AudioCurveEvaluator eval, Color curveColor)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            HandleUtility.ApplyWireMaterial();

            int numpoints = (int)Mathf.Ceil(r.width);
            float cy = r.height * 0.5f;
            float ws = 1.0f / (numpoints - 1);
            var line = GetPointCache(numpoints);
            for (int n = 0; n < numpoints; n++)
            {
                line[n].x = (int)n + r.x;
                line[n].y = cy - cy * eval(n * ws) + r.y;
                line[n].z = 0.0f;
            }

            GUI.BeginClip(r);
            Handles.color = curveColor;
            Handles.DrawAAPolyLine(3.0f, numpoints, line);
            GUI.EndClip();
        }

        static Vector3[] s_PointCache;
        static Vector3[] GetPointCache(int numPoints)
        {
            if (s_PointCache == null || s_PointCache.Length != numPoints)
            {
                s_PointCache = new Vector3[numPoints];
            }
            return s_PointCache;
        }

        // -------------
        // Curve gradient rect
        // -------------

        public static void DrawGradientRect(Rect r, Color c1, Color c2, float blend, bool horizontal)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            HandleUtility.ApplyWireMaterial();
            GL.Begin(GL.QUADS);
            if (horizontal)
            {
                GL.Color(new Color(c1.r, c1.g, c1.b, c1.a * blend));
                GL.Vertex3(r.x, r.y, 0);
                GL.Vertex3(r.x + r.width, r.y, 0);
                GL.Color(new Color(c2.r, c2.g, c2.b, c2.a * blend));
                GL.Vertex3(r.x + r.width, r.y + r.height, 0);
                GL.Vertex3(r.x, r.y + r.height, 0);
            }
            else
            {
                GL.Color(new Color(c1.r, c1.g, c1.b, c1.a * blend));
                GL.Vertex3(r.x, r.y + r.height, 0);
                GL.Vertex3(r.x, r.y, 0);
                GL.Color(new Color(c2.r, c2.g, c2.b, c2.a * blend));
                GL.Vertex3(r.x + r.width, r.y, 0);
                GL.Vertex3(r.x + r.width, r.y + r.height, 0);
            }
            GL.End();
        }
    }
}
