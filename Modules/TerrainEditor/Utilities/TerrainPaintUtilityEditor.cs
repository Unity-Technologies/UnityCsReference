// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    public static class TerrainPaintUtilityEditor
    {
        internal enum BrushPreviewMeshType
        {
            QuadOutline = 0,
            QuadPatch
        }

        // This maintains the list of terrains we have touched in the current operation (and the current operation identifier, as an undo group)
        // We track this to have good cross-tile undo support: each modified tile should be added, at most, ONCE within a single operation
        private static int s_CurrentOperationUndoGroup = -1;
        private static List<Terrain> s_CurrentOperationUndoStack = new List<Terrain>();

        static TerrainPaintUtilityEditor()
        {
            TerrainPaintUtility.onTerrainTileBeforePaint += (tile, action, editorUndoName) =>
            {
                // if we are in a new undo group (new operation) then start with an empty list
                if (Undo.GetCurrentGroup() != s_CurrentOperationUndoGroup)
                {
                    s_CurrentOperationUndoGroup = Undo.GetCurrentGroup();
                    s_CurrentOperationUndoStack.Clear();
                }

                if (tile == null || string.IsNullOrEmpty(editorUndoName) || tile.rect.width == 0 || tile.rect.height == 0)
                    return;

                if (!s_CurrentOperationUndoStack.Contains(tile.terrain))
                {
                    s_CurrentOperationUndoStack.Add(tile.terrain);
                    var undoObjects = new List<UnityEngine.Object>();
                    undoObjects.Add(tile.terrain.terrainData);
                    if (0 != (action & TerrainPaintUtility.ToolAction.PaintTexture))
                        undoObjects.AddRange(tile.terrain.terrainData.alphamapTextures);
                    Undo.RegisterCompleteObjectUndo(undoObjects.ToArray(), editorUndoName);
                }
            };
        }

        public static void ShowDefaultPreviewBrush(Terrain terrain, Texture brushTexture, float brushStrength, int brushSize, float futurePreviewScale)
        {
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;
            if (terrain.GetComponent<Collider>().Raycast(mouseRay, out hit, Mathf.Infinity))
            {
                if (Event.current.shift)
                    brushStrength = -brushStrength;

                Rect brushRect = TerrainPaintUtility.CalculateBrushRectInTerrainUnits(terrain, hit.textureCoord, brushSize);
                TerrainPaintUtility.PaintContext ctx = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushRect);

                ctx.sourceRenderTexture.filterMode = FilterMode.Bilinear;
                brushTexture.filterMode = FilterMode.Bilinear;

                Vector2 topLeft = ctx.brushRect.min;
                float xfrac = ((topLeft.x - (int)topLeft.x) / (float)ctx.sourceRenderTexture.width);
                float yfrac = ((topLeft.y - (int)topLeft.y) / (float)ctx.sourceRenderTexture.height);

                Vector4 texScaleOffset = new Vector4(0.5f, 0.5f, 0.5f + xfrac + 0.5f / (float)ctx.sourceRenderTexture.width, 0.5f + yfrac + 0.5f / (float)ctx.sourceRenderTexture.height);

                DrawDefaultBrushPreviewMesh(terrain, hit, ctx.sourceRenderTexture, brushTexture, brushStrength * 0.01f, brushSize, defaultPreviewPatchMesh, false, texScaleOffset);
                if ((futurePreviewScale > Mathf.Epsilon) && Event.current.control)
                    DrawDefaultBrushPreviewMesh(terrain, hit, ctx.sourceRenderTexture, brushTexture, futurePreviewScale, brushSize, defaultPreviewPatchMesh, true, texScaleOffset);

                TerrainPaintUtility.ReleaseContextResources(ctx);
            }
        }

        public static Material GetDefaultBrushPreviewMaterial()
        {
            if (m_BrushPreviewMaterial == null)
                m_BrushPreviewMaterial = new Material(Shader.Find("Hidden/TerrainEngine/BrushPreview"));
            return m_BrushPreviewMaterial;
        }

        // drawing utilities

        internal static Mesh GenerateDefaultBrushPreviewMesh(int tesselationLevel)
        {
            if (tesselationLevel < 1)
            {
                Debug.LogWarning("Invalid tessellation level passed to GenerateBrushPreviewMesh.");
                return null;
            }

            var verts = new List<Vector3>();
            var indices = new List<int>();

            Mesh m = new Mesh();

            float incr = 2.0f / (float)tesselationLevel;

            Vector3 v = new Vector3(-1.0f, 0, -1.0f);

            for (int i = 0; i <= tesselationLevel; i++)
            {
                v.x = -1.0f;
                for (int j = 0; j <= tesselationLevel; j++)
                {
                    verts.Add(v);
                    v.x += incr;

                    if (i != tesselationLevel && j != tesselationLevel)
                    {
                        int index = i * (tesselationLevel + 1) + j;

                        indices.Add(index);
                        indices.Add(index + tesselationLevel + 1);
                        indices.Add(index + 1);

                        indices.Add(index + 1);
                        indices.Add(index + tesselationLevel + 1);
                        indices.Add(index + tesselationLevel + 2);
                    }
                }
                v.z += incr;
            }

            m.vertices = verts.ToArray();
            m.triangles = indices.ToArray();

            return m;
        }

        public static void DrawDefaultBrushPreviewMesh(Terrain terrain, RaycastHit hit, Texture heightmapTexture, Texture brushTexture, float brushStrength, int brushSize, Mesh mesh, bool showPreviewPostBrush, Vector4 texScaleOffset)
        {
            Vector4 brushParams = new Vector4(brushStrength, 2.0f * terrain.terrainData.heightmapScale.y, 0.0f, 0.0f);

            Material mat = GetDefaultBrushPreviewMaterial();
            mat.SetTexture("_MainTex", heightmapTexture);
            mat.SetTexture("_BrushTex", brushTexture);
            mat.SetVector("_BrushParams", brushParams);
            mat.SetVector("_TexScaleOffet", texScaleOffset);
            mat.SetPass(showPreviewPostBrush ? 1 : 0);

            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.SetTRS(new Vector3(hit.point.x, terrain.GetPosition().y, hit.point.z), Quaternion.identity, new Vector3(brushSize / 2.0f, 1, brushSize / 2.0f));

            Graphics.DrawMeshNow(mesh, matrix);
        }

        static Mesh m_DefaultPreviewPatchMesh = null;
        static Material m_BrushPreviewMaterial = null;

        public static Mesh defaultPreviewPatchMesh
        {
            get
            {
                if (m_DefaultPreviewPatchMesh == null)
                    m_DefaultPreviewPatchMesh = GenerateDefaultBrushPreviewMesh(100);
                return m_DefaultPreviewPatchMesh;
            }
        }
    }
}
