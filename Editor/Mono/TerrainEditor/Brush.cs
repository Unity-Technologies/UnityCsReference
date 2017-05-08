// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

/*
GUILayout.TextureGrid number of horiz elements doesnt work
*/

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;
using UnityEngine.Rendering;

namespace UnityEditor
{
    internal class Brush
    {
        float[] m_Strength;
        int      m_Size;
        Texture2D m_Brush;
        Texture2D m_Preview;
        Projector m_BrushProjector;

        internal const int kMinBrushSize = 3;

        public bool Load(Texture2D  brushTex, int size)
        {
            // Always restore the preview texture before using the brush because:
            // 1) The material of the projector might be shared among multiple brushes.
            // 2) Tree painter overrides the preview texture.
            if (m_BrushProjector != null && m_Preview != null)
                m_BrushProjector.material.mainTexture = m_Preview;

            if (m_Brush == brushTex && size == m_Size && m_Strength != null)
                return true;

            if (brushTex != null)
            {
                float fSize = size;
                m_Size = size;
                m_Strength = new float[m_Size * m_Size];
                if (m_Size > kMinBrushSize)
                {
                    for (int y = 0; y < m_Size; y++)
                    {
                        for (int x = 0; x < m_Size; x++)
                        {
                            m_Strength[y * m_Size + x] = brushTex.GetPixelBilinear((x + 0.5F) / fSize, (y) / fSize).a;
                            //                      m_Strength[y * m_Size + x] = brushTex.GetPixelBilinear(x / fSize, y / fSize).a;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < m_Strength.Length; i++)
                        m_Strength[i] = 1.0F;
                }

                // Create preview texture
                Object.DestroyImmediate(m_Preview);
                m_Preview = new Texture2D(m_Size, m_Size, TextureFormat.RGBA32, false);
                m_Preview.hideFlags = HideFlags.HideAndDontSave;
                m_Preview.wrapMode = TextureWrapMode.Repeat;
                m_Preview.filterMode = FilterMode.Point;
                Color[] pixels = new Color[m_Size * m_Size];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color(1, 1, 1, m_Strength[i]);

                m_Preview.SetPixels(0, 0, m_Size, m_Size, pixels, 0);
                m_Preview.Apply();

                if (m_BrushProjector == null)
                    CreatePreviewBrush();

                m_BrushProjector.material.mainTexture = m_Preview;
                if (GraphicsSettings.renderPipelineAsset != null)
                    m_BrushProjector.material.renderQueue = GraphicsSettings.renderPipelineAsset.GetTerrainBrushPassIndex();
                m_Brush = brushTex;

                return true;
            }
            else
            {
                m_Strength = new float[1];
                m_Strength[0] = 1.0F;
                m_Size = 1;
                return false;
            }
        }

        public float GetStrengthInt(int ix, int iy)
        {
            ix = Mathf.Clamp(ix, 0, m_Size - 1);
            iy = Mathf.Clamp(iy, 0, m_Size - 1);

            float s = m_Strength[iy * m_Size + ix];

            return s;
        }

        public void Dispose()
        {
            if (m_BrushProjector)
            {
                Object.DestroyImmediate(m_BrushProjector.gameObject);
                m_BrushProjector = null;
            }
            Object.DestroyImmediate(m_Preview);
            m_Preview = null;
        }

        public Projector GetPreviewProjector()
        {
            return m_BrushProjector;
        }

        private void CreatePreviewBrush()
        {
            GameObject go = EditorUtility.CreateGameObjectWithHideFlags("TerrainInspectorBrushPreview", HideFlags.HideAndDontSave, typeof(Projector));

            m_BrushProjector = go.GetComponent(typeof(Projector)) as Projector;
            m_BrushProjector.enabled = false;
            m_BrushProjector.nearClipPlane = -1000.0f;
            m_BrushProjector.farClipPlane = 1000.0f;
            m_BrushProjector.orthographic = true;
            m_BrushProjector.orthographicSize = 10.0f;
            m_BrushProjector.transform.Rotate(90.0f, 0.0f, 0.0f);

            Material mat = EditorGUIUtility.LoadRequired("SceneView/TerrainBrushMaterial.mat") as Material;
            mat.SetTexture("_CutoutTex", (Texture2D)EditorGUIUtility.Load(EditorResourcesUtility.brushesPath + "brush_cutout.png"));
            m_BrushProjector.material = mat;
        }
    }
} //namespace
