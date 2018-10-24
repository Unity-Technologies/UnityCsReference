// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
    public static class TerrainPaintUtilityEditor
    {
        // This maintains the list of terrains we have touched in the current operation (and the current operation identifier, as an undo group)
        // We track this to have good cross-tile undo support: each modified tile should be added, at most, ONCE within a single operation
        private static int s_CurrentOperationUndoGroup = -1;
        private static List<UnityEngine.Object> s_CurrentOperationUndoStack = new List<UnityEngine.Object>();

        static TerrainPaintUtilityEditor()
        {
            PaintContext.onTerrainTileBeforePaint += (tile, action, editorUndoName) =>
            {
                // if we are in a new undo group (new operation) then start with an empty list
                if (Undo.GetCurrentGroup() != s_CurrentOperationUndoGroup)
                {
                    s_CurrentOperationUndoGroup = Undo.GetCurrentGroup();
                    s_CurrentOperationUndoStack.Clear();
                }

                if (tile == null || string.IsNullOrEmpty(editorUndoName))
                    return;

                if (!s_CurrentOperationUndoStack.Contains(tile.terrain))
                {
                    s_CurrentOperationUndoStack.Add(tile.terrain);
                    var undoObjects = new List<UnityEngine.Object>();
                    undoObjects.Add(tile.terrain.terrainData);
                    if (0 != (action & PaintContext.ToolAction.PaintTexture))
                        undoObjects.AddRange(tile.terrain.terrainData.alphamapTextures);
                    Undo.RegisterCompleteObjectUndo(undoObjects.ToArray(), editorUndoName);
                }
            };
        }

        internal static void UpdateTerrainDataUndo(TerrainData terrainData, string undoName)
        {
            // if we are in a new undo group (new operation) then start with an empty list
            if (Undo.GetCurrentGroup() != s_CurrentOperationUndoGroup)
            {
                s_CurrentOperationUndoGroup = Undo.GetCurrentGroup();
                s_CurrentOperationUndoStack.Clear();
            }

            if (!s_CurrentOperationUndoStack.Contains(terrainData))
            {
                s_CurrentOperationUndoStack.Add(terrainData);
                Undo.RegisterCompleteObjectUndo(terrainData, undoName);
            }
        }

        public static void ShowDefaultPreviewBrush(Terrain terrain, Texture brushTexture, float brushSize)
        {
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;
            if (terrain.GetComponent<Collider>().Raycast(mouseRay, out hit, Mathf.Infinity))
            {
                BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, hit.textureCoord, brushSize, 0.0f);
                PaintContext ctx = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);
                DrawBrushPreview(ctx, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture, brushTexture, brushXform, GetDefaultBrushPreviewMaterial(), 0);
                TerrainPaintUtility.ReleaseContextResources(ctx);
            }
        }

        public static Material GetDefaultBrushPreviewMaterial()
        {
            if (m_BrushPreviewMaterial == null)
                m_BrushPreviewMaterial = new Material(Shader.Find("Hidden/TerrainEngine/BrushPreview"));
            return m_BrushPreviewMaterial;
        }

        public enum BrushPreview
        {
            SourceRenderTexture,
            DestinationRenderTexture
        };

        public static void DrawBrushPreview(
            PaintContext heightmapPC,
            BrushPreview previewTexture,
            Texture brushTexture,                           // brush texture to apply
            BrushTransform brushXform,                      // brush transform that defines the brush UV space
            Material proceduralMaterial,                    // the material to render with (must support procedural quad-mesh generation)
            int materialPassIndex)                          // the pass to use within the material
        {
            // we want to build a quad mesh, with one vertex for each pixel in the heightmap
            // i.e. a 3x3 heightmap would create a mesh that looks like this:
            //
            //    +-+-+
            //    |\|\|
            //    +-+-+
            //    |\|\|
            //    +-+-+
            //
            int quadsX = heightmapPC.pixelRect.width - 1;
            int quadsY = heightmapPC.pixelRect.height - 1;
            int vertexCount = quadsX * quadsY * (2 * 3);  // two triangles (2 * 3 vertices) per quad

            // this is used to tessellate the quad mesh (from within the vertex shader)
            proceduralMaterial.SetVector("_QuadRez", new Vector4(quadsX, quadsY, vertexCount, 0.0f));

            // paint context pixels to heightmap uv:   uv = (pixels + 0.5) / width
            Texture heightmapTexture = (previewTexture == BrushPreview.SourceRenderTexture) ? heightmapPC.sourceRenderTexture : heightmapPC.destinationRenderTexture;
            float invWidth = 1.0f / heightmapTexture.width;
            float invHeight = 1.0f / heightmapTexture.height;
            proceduralMaterial.SetVector("_HeightmapUV_PCPixelsX",  new Vector4(invWidth, 0.0f, 0.0f, 0.0f));
            proceduralMaterial.SetVector("_HeightmapUV_PCPixelsY",  new Vector4(0.0f, invHeight, 0.0f, 0.0f));
            proceduralMaterial.SetVector("_HeightmapUV_Offset",     new Vector4(0.5f  * invWidth, 0.5f * invHeight, 0.0f, 0.0f));

            // make sure we point filter the heightmap
            FilterMode oldFilter = heightmapTexture.filterMode;
            heightmapTexture.filterMode = FilterMode.Point;
            proceduralMaterial.SetTexture("_Heightmap", heightmapTexture);

            // paint context pixels to object (terrain) position
            // objectPos.x = scaleX * pcPixels.x + heightmapRect.xMin * scaleX
            // objectPos.y = scaleY * H
            // objectPos.z = scaleZ * pcPixels.y + heightmapRect.yMin * scaleZ
            float scaleX = heightmapPC.pixelSize.x;
            float scaleY = 2.0f * heightmapPC.originTerrain.terrainData.heightmapScale.y;
            float scaleZ = heightmapPC.pixelSize.y;
            proceduralMaterial.SetVector("_ObjectPos_PCPixelsX", new Vector4(scaleX, 0.0f, 0.0f, 0.0f));
            proceduralMaterial.SetVector("_ObjectPos_HeightMapSample", new Vector4(0.0f, scaleY, 0.0f, 0.0f));
            proceduralMaterial.SetVector("_ObjectPos_PCPixelsY", new Vector4(0.0f, 0.0f, scaleZ, 0.0f));
            proceduralMaterial.SetVector("_ObjectPos_Offset", new Vector4(heightmapPC.pixelRect.xMin * scaleX, 0.0f, heightmapPC.pixelRect.yMin * scaleZ, 1.0f));

            // heightmap paint context pixels to brush UV
            // derivation:

            // BrushUV = f(terrainSpace.xz) = f(g(pcPixels.xy))
            //   f(ts.xy) = ts.x * brushXform.X + ts.y * brushXform.Y + brushXform.Origin
            //   g(pcPixels.xy) = ts.xz = pcOrigin + pcPixels.xy * pcSize
            //   f(g(pcPixels.uv)) == (pcOrigin + pcPixels.uv * pcSize).x * brushXform.X + (pcOrigin + pcPixels.uv * pcSize).y * brushXform.Y + brushXform.Origin
            //   f(g(pcPixels.uv)) == (pcOrigin.x + pcPixels.u * pcSize.x) * brushXform.X + (pcOrigin.y + pcPixels.v * pcSize.y) * brushXform.Y + brushXform.Origin
            //   f(g(pcPixels.uv)) == (pcOrigin.x * brushXform.X) + (pcPixels.u * pcSize.x) * brushXform.X + (pcOrigin.y * brushXform.Y) + (pcPixels.v * pcSize.y) * brushXform.Y + brushXform.Origin
            //   f(g(pcPixels.uv)) == pcPixels.u * (pcSize.x * brushXform.X) + pcPixels.v * (pcSize.y * brushXform.Y) + (brushXform.Origin + (pcOrigin.x * brushXform.X) + (pcOrigin.y * brushXform.Y))

            // paint context origin in terrain space
            // (note this is the UV space origin and size, not the mesh origin & size)
            float pcOriginX = heightmapPC.pixelRect.xMin * heightmapPC.pixelSize.x;
            float pcOriginZ = heightmapPC.pixelRect.yMin * heightmapPC.pixelSize.y;
            float pcSizeX = heightmapPC.pixelSize.x;
            float pcSizeZ = heightmapPC.pixelSize.y;

            Vector2 scaleU = pcSizeX * brushXform.targetX;
            Vector2 scaleV = pcSizeZ * brushXform.targetY;
            Vector2 offset = brushXform.targetOrigin + pcOriginX * brushXform.targetX + pcOriginZ * brushXform.targetY;
            proceduralMaterial.SetVector("_BrushUV_PCPixelsX",      new Vector4(scaleU.x, scaleU.y, 0.0f, 0.0f));
            proceduralMaterial.SetVector("_BrushUV_PCPixelsY",      new Vector4(scaleV.x, scaleV.y, 0.0f, 0.0f));
            proceduralMaterial.SetVector("_BrushUV_Offset",         new Vector4(offset.x, offset.y, 0.0f, 1.0f));
            proceduralMaterial.SetTexture("_BrushTex", brushTexture);

            Vector3 terrainPos = heightmapPC.originTerrain.GetPosition();
            proceduralMaterial.SetVector("_TerrainObjectToWorldOffset", terrainPos);

            proceduralMaterial.SetPass(materialPassIndex);
            Graphics.DrawProcedural(MeshTopology.Triangles, vertexCount);

            heightmapTexture.filterMode = oldFilter;
        }

        static Material m_BrushPreviewMaterial = null;
    }
}
