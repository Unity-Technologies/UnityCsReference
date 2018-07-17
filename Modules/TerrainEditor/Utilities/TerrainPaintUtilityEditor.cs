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

        [Flags]
        public enum ToolAction
        {
            PaintHeightmap = 1 << 0,
            PaintTexture = 1 << 1,
        }

        // This maintains the list of terrains we have touched in the current operation (and the current operation identifier, as an undo group)
        // We track this to have good cross-tile undo support: each modified tile should be added, at most, ONCE within a single operation
        static int currentOperationUndoGroup = -1;
        static ArrayList currentOperationUndoStack = new ArrayList();

        public static void UpdateTerrainUndo(TerrainPaintUtility.PaintContext ctx, ToolAction toolAction, string undoActionName = "Terrain Paint")
        {
            // if we are in a new undo group (new operation) then start with an empty list
            if (Undo.GetCurrentGroup() != currentOperationUndoGroup)
            {
                currentOperationUndoGroup = Undo.GetCurrentGroup();
                currentOperationUndoStack.Clear();
            }

            foreach (TerrainPaintUtility.TerrainTile modifiedTile in ctx.terrainTiles)
            {
                if (modifiedTile.rect.width == 0 || modifiedTile.rect.height == 0)
                    continue;

                if (!currentOperationUndoStack.Contains(modifiedTile.terrain))
                {
                    currentOperationUndoStack.Add(modifiedTile.terrain);
                    var undoObjects = new List<UnityEngine.Object>();
                    undoObjects.Add(modifiedTile.terrain.terrainData);
                    if (0 != ((uint)toolAction & (uint)ToolAction.PaintTexture))
                        undoObjects.AddRange(modifiedTile.terrain.terrainData.alphamapTextures);
                    Undo.RegisterCompleteObjectUndo(undoObjects.ToArray(), undoActionName);
                }
            }
        }

        public static void ShowDefaultPreviewBrush(Terrain terrain, Texture brushTexture, float brushStrength, int brushSizeInTerrainUnits, float futurePreviewScale)
        {
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;
            if (terrain.GetComponent<Collider>().Raycast(mouseRay, out hit, Mathf.Infinity))
            {
                if (Event.current.shift)
                    brushStrength = -brushStrength;

                Vector2Int brushSize = TerrainPaintUtility.CalculateBrushSizeInHeightmapSpace(terrain, brushSizeInTerrainUnits);
                TerrainPaintUtility.PaintContext ctx = TerrainPaintUtility.BeginPaintHeightmap(terrain, hit.textureCoord, brushSize);

                ctx.sourceRenderTexture.filterMode = FilterMode.Bilinear;
                brushTexture.filterMode = FilterMode.Bilinear;

                Vector2 topLeft = TerrainPaintUtility.CalcTopLeftOfBrushRect(hit.textureCoord, brushSize, terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight);
                float xfrac = ((topLeft.x - (int)topLeft.x) / (float)ctx.sourceRenderTexture.width);
                float yfrac = ((topLeft.y - (int)topLeft.y) / (float)ctx.sourceRenderTexture.height);

                Vector4 texScaleOffset = new Vector4(0.5f, 0.5f, 0.5f + xfrac + 0.5f / (float)ctx.sourceRenderTexture.width, 0.5f + yfrac + 0.5f / (float)ctx.sourceRenderTexture.height);

                DrawDefaultBrushPreviewMesh(terrain, hit, ctx.sourceRenderTexture, brushTexture, brushStrength * 0.01f, brushSizeInTerrainUnits, defaultPreviewPatchMesh, false, texScaleOffset);
                if ((futurePreviewScale > Mathf.Epsilon) && Event.current.control)
                    DrawDefaultBrushPreviewMesh(terrain, hit, ctx.sourceRenderTexture, brushTexture, futurePreviewScale, brushSizeInTerrainUnits, defaultPreviewPatchMesh, true, texScaleOffset);

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

            ArrayList verts = new ArrayList();
            ArrayList indices = new ArrayList();

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

            m.vertices = verts.ToArray(typeof(Vector3)) as Vector3[];
            m.triangles = indices.ToArray(typeof(int)) as int[];

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
            matrix.SetTRS(new Vector3(hit.point.x, 0, hit.point.z), Quaternion.identity, new Vector3(brushSize / 2.0f, 1, brushSize / 2.0f));

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
