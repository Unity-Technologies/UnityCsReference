// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Experimental;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal class Draw2D
    {
        public enum Origin
        {
            TopLeft,
            BottomLeft
        };

        readonly string m_ShaderName;

        Origin m_Origin = Origin.TopLeft;
        GUIStyle m_GLStyle;
        Material m_Material;
        Rect m_Rect;
        Vector4 m_ClipRect;
        bool m_ClipRectEnabled = false;

        public Draw2D(string shaderName)
        {
            m_ShaderName = shaderName;
            CheckAndSetupMaterial();
        }

        bool CheckAndSetupMaterial()
        {
            if (m_Material == null)
            {
                var shader = EditorResources.Load<Shader>(m_ShaderName);
                if (shader == null)
                {
                    Debug.LogFormat("Unable to locate shader {0}", m_ShaderName);
                    return false;
                }

                m_Material = new Material(shader);
                if (m_Material == null)
                {
                    Debug.LogFormat("Unable to create material for {0}", m_ShaderName);
                    return false;
                }
            }

            return true;
        }

        bool IsMaterialValid()
        {
            return m_Material != null;
        }

        public void OnGUI()
        {
            if (m_GLStyle == null)
            {
                m_GLStyle = new GUIStyle(GUI.skin.box);
                m_GLStyle.padding = new RectOffset(0, 0, 0, 0);
                m_GLStyle.margin = new RectOffset(0, 0, 0, 0);
            }
        }

        public void SetClipRect(Rect clipRect)
        {
            m_ClipRect = new Vector4(clipRect.x, clipRect.y, clipRect.x + clipRect.width, clipRect.y + clipRect.height);
            m_ClipRectEnabled = true;

            if (CheckAndSetupMaterial())
            {
                m_Material.SetFloat("_UseClipRect", m_ClipRectEnabled ? 1f : 0f);
                m_Material.SetVector("_ClipRect", m_ClipRect);
            }
        }

        public void ClearClipRect()
        {
            m_ClipRectEnabled = false;

            if (CheckAndSetupMaterial())
            {
                m_Material.SetFloat("_UseClipRect", m_ClipRectEnabled ? 1f : 0f);
                m_Material.SetVector("_ClipRect", m_ClipRect);
            }
        }

        public Rect GetClipRect()
        {
            return new Rect(m_ClipRect.x, m_ClipRect.y, m_ClipRect.z - m_ClipRect.x, m_ClipRect.w - m_ClipRect.y);
        }

        public bool DrawStart(Rect r, Origin origin = Origin.TopLeft)
        {
            if (Event.current.type != EventType.Repaint)
                return false;

            if (!CheckAndSetupMaterial())
                return false;

            m_Material.SetPass(0);

            m_Rect = r;
            m_Origin = origin;
            return true;
        }

        public bool DrawStart(float w, float h, Origin origin = Origin.TopLeft, GUIStyle style = null)
        {
            Rect r = GUILayoutUtility.GetRect(w, h, style == null ? m_GLStyle : style, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            return DrawStart(r, origin);
        }

        public void DrawEnd()
        {
        }

        void Translate(ref float x, ref float y)
        {
            // Translation done CPU side so we have world space coords in the shader for clipping.
            if (m_Origin == Origin.BottomLeft)
            {
                x = m_Rect.xMin + x;
                y = m_Rect.yMax - y;
            }
            else
            {
                x = m_Rect.xMin + x;
                y = m_Rect.yMin + y;
            }
        }

        public void DrawFilledBox(float x, float y, float w, float h, Color col)
        {
            float x2 = x + w;
            float y2 = y + h;

            Translate(ref x, ref y);
            Translate(ref x2, ref y2);

            if (m_Origin == Origin.BottomLeft)
            {
                GL.Begin(GL.TRIANGLE_STRIP);
                GL.Color(col);
                GL.Vertex3(x, y, 0);
                GL.Vertex3(x, y2, 0);
                GL.Vertex3(x2, y, 0);
                GL.Vertex3(x2, y2, 0);
                GL.End();
            }
            else
            {
                GL.Begin(GL.TRIANGLE_STRIP);
                GL.Color(col);
                GL.Vertex3(x, y, 0);
                GL.Vertex3(x2, y, 0);
                GL.Vertex3(x, y2, 0);
                GL.Vertex3(x2, y2, 0);
                GL.End();
            }
        }

        public void DrawFilledBox(float x, float y, float w, float h, float r, float g, float b)
        {
            DrawFilledBox(x, y, w, h, new Color(r, g, b));
        }

        public void DrawFilledCircle(float x, float y, float r, Color col, float quality = 12)
        {
            Translate(ref x, ref y);

            GL.Begin(GL.TRIANGLES);
            GL.Color(col);

            float x1 = x + r * Mathf.Cos(0);
            float y1 = y + r * Mathf.Sin(0);
            float step = Mathf.PI / quality;
            for (float angle = 0; angle < Mathf.PI * 2; angle += step)
            {
                float nextAngle = angle + step;
                float x2 = x + r * Mathf.Cos(nextAngle);
                float y2 = y + r * Mathf.Sin(nextAngle);
                GL.Vertex3(x, y, 0);
                GL.Vertex3(x1, y1, 0);
                GL.Vertex3(x2, y2, 0);
                x1 = x2;
                y1 = y2;
            }

            GL.End();
        }

        public void DrawCircle(float x, float y, float r, Color col, float quality = 12)
        {
            Translate(ref x, ref y);

            GL.Begin(GL.LINES);
            GL.Color(col);

            float x1 = x + r * Mathf.Cos(0);
            float y1 = y + r * Mathf.Sin(0);
            float step = Mathf.PI / quality;
            for (float angle = 0; angle < Mathf.PI * 2; angle += step)
            {
                float nextAngle = angle + step;
                float x2 = x + r * Mathf.Cos(nextAngle);
                float y2 = y + r * Mathf.Sin(nextAngle);
                GL.Vertex3(x1, y1, 0);
                GL.Vertex3(x2, y2, 0);
                x1 = x2;
                y1 = y2;
            }

            GL.End();
        }

        public void DrawLine(float x, float y, float x2, float y2, Color col)
        {
            Translate(ref x, ref y);
            Translate(ref x2, ref y2);

            GL.Begin(GL.LINES);
            GL.Color(col);
            GL.Vertex3(x, y, 0);
            GL.Vertex3(x2, y2, 0);
            GL.End();
        }

        public void DrawLine(float x, float y, float x2, float y2, float r, float g, float b)
        {
            DrawLine(x, y, x2, y2, new Color(r, g, b));
        }

        public void DrawBox(float x, float y, float w, float h, Color col)
        {
            float x2 = x + w;
            float y2 = y + h;

            Translate(ref x, ref y);
            Translate(ref x2, ref y2);

            GL.Begin(GL.LINE_STRIP);
            GL.Color(col);
            GL.Vertex3(x, y, 0);
            GL.Vertex3(x2, y, 0);
            GL.Vertex3(x2, y2, 0);
            GL.Vertex3(x, y2, 0);
            GL.Vertex3(x, y, 0);
            GL.End();
        }

        public void DrawBox(float x, float y, float w, float h, float r, float g, float b)
        {
            DrawBox(x, y, w, h, new Color(r, g, b));
        }
    }
}
