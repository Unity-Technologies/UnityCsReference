// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
    internal class BrushRep
    {
        private const int kMinBrushSize = 3;

        private int m_Size;
        private float[] m_Strength;
        private Texture2D m_OldBrushTex;

        public float GetStrengthInt(int ix, int iy)
        {
            ix = Mathf.Clamp(ix, 0, m_Size - 1);
            iy = Mathf.Clamp(iy, 0, m_Size - 1);

            float s = m_Strength[iy * m_Size + ix];

            return s;
        }

        public void CreateFromBrush(Texture2D brushTex, int size)
        {
            if (size == m_Size && m_OldBrushTex == brushTex && m_Strength != null)
                return;

            Texture2D mask = brushTex;
            if (mask != null)
            {
                Texture2D readableTexture = null;
                if (!mask.isReadable)
                {
                    readableTexture = new Texture2D(mask.width, mask.height, mask.format, mask.mipmapCount > 1);
                    Graphics.CopyTexture(mask, readableTexture);
                    readableTexture.Apply();
                }
                else
                {
                    readableTexture = mask;
                }

                float fSize = size;
                m_Size = size;
                m_Strength = new float[m_Size * m_Size];
                if (m_Size > kMinBrushSize)
                {
                    for (int y = 0; y < m_Size; y++)
                    {
                        float v = y / fSize;
                        for (int x = 0; x < m_Size; x++)
                        {
                            float u = x / fSize;
                            m_Strength[y * m_Size + x] = readableTexture.GetPixelBilinear(u, v).r;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < m_Strength.Length; i++)
                        m_Strength[i] = 1.0F;
                }

                if (readableTexture != mask)
                    Object.DestroyImmediate(readableTexture);
            }
            else
            {
                m_Strength = new float[1];
                m_Strength[0] = 1.0F;
                m_Size = 1;
            }

            m_OldBrushTex = brushTex;
        }
    }

    internal class PaintDetailsUtils
    {
        public static int FindDetailPrototype(Terrain terrain, Terrain sourceTerrain, int sourceDetail)
        {
            if (sourceDetail == PaintDetailsTool.kInvalidDetail ||
                sourceDetail >= sourceTerrain.terrainData.detailPrototypes.Length)
            {
                return PaintDetailsTool.kInvalidDetail;
            }

            if (terrain == sourceTerrain)
            {
                return sourceDetail;
            }

            DetailPrototype sourceDetailPrototype = sourceTerrain.terrainData.detailPrototypes[sourceDetail];
            for (int i = 0; i < terrain.terrainData.detailPrototypes.Length; ++i)
            {
                if (sourceDetailPrototype.Equals(terrain.terrainData.detailPrototypes[i]))
                    return i;
            }

            return PaintDetailsTool.kInvalidDetail;
        }

        public static int CopyDetailPrototype(Terrain terrain, Terrain sourceTerrain, int sourceDetail)
        {
            DetailPrototype sourceDetailPrototype = sourceTerrain.terrainData.detailPrototypes[sourceDetail];
            DetailPrototype[] newDetailPrototypesArray = new DetailPrototype[terrain.terrainData.detailPrototypes.Length + 1];
            System.Array.Copy(terrain.terrainData.detailPrototypes, newDetailPrototypesArray, terrain.terrainData.detailPrototypes.Length);
            newDetailPrototypesArray[newDetailPrototypesArray.Length - 1] = new DetailPrototype(sourceDetailPrototype);
            terrain.terrainData.detailPrototypes = newDetailPrototypesArray;
            terrain.terrainData.RefreshPrototypes();
            return newDetailPrototypesArray.Length - 1;
        }
    }

    internal class PaintDetailsTool : TerrainPaintTool<PaintDetailsTool>
    {
        public const int kInvalidDetail = -1;

        private DetailPrototype m_LastSelectedDetailPrototype;
        private Terrain m_TargetTerrain;
        private BrushRep m_BrushRep;

        public float detailOpacity { get; set; }
        public float detailStrength { get; set; }
        public int selectedDetail { get; set; }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            if (m_TargetTerrain == null ||
                selectedDetail == kInvalidDetail ||
                selectedDetail >= m_TargetTerrain.terrainData.detailPrototypes.Length)
            {
                return false;
            }

            Texture2D brush = editContext.brushTexture as Texture2D;
            if (brush == null)
            {
                Debug.LogError("Brush texture is not a Texture2D.");
                return false;
            }

            if (m_BrushRep == null)
            {
                m_BrushRep = new BrushRep();
            }

            PaintTreesDetailsContext ctx = PaintTreesDetailsContext.Create(terrain, editContext.uv);

            for (int t = 0; t < ctx.terrains.Length; ++t)
            {
                Terrain ctxTerrain = ctx.terrains[t];
                if (ctxTerrain != null)
                {
                    int detailPrototype = PaintDetailsUtils.FindDetailPrototype(ctxTerrain, m_TargetTerrain, selectedDetail);
                    if (detailPrototype == kInvalidDetail)
                    {
                        detailPrototype = PaintDetailsUtils.CopyDetailPrototype(ctxTerrain, m_TargetTerrain, selectedDetail);
                    }

                    TerrainData terrainData = ctxTerrain.terrainData;

                    TerrainPaintUtilityEditor.UpdateTerrainDataUndo(terrainData, "Terrain - Detail Edit");

                    int size = (int)Mathf.Max(1.0f, editContext.brushSize * ((float)terrainData.detailResolution / terrainData.size.x));

                    m_BrushRep.CreateFromBrush(brush, size);

                    Vector2 ctxUV = ctx.uvs[t];

                    int xCenter = Mathf.FloorToInt(ctxUV.x * terrainData.detailWidth);
                    int yCenter = Mathf.FloorToInt(ctxUV.y * terrainData.detailHeight);

                    int intRadius = Mathf.RoundToInt(size) / 2;
                    int intFraction = Mathf.RoundToInt(size) % 2;

                    int xmin = xCenter - intRadius;
                    int ymin = yCenter - intRadius;

                    int xmax = xCenter + intRadius + intFraction;
                    int ymax = yCenter + intRadius + intFraction;

                    if (xmin >= terrainData.detailWidth || ymin >= terrainData.detailHeight || xmax <= 0 || ymax <= 0)
                    {
                        continue;
                    }

                    xmin = Mathf.Clamp(xmin, 0, terrainData.detailWidth - 1);
                    ymin = Mathf.Clamp(ymin, 0, terrainData.detailHeight - 1);

                    xmax = Mathf.Clamp(xmax, 0, terrainData.detailWidth);
                    ymax = Mathf.Clamp(ymax, 0, terrainData.detailHeight);

                    int width = xmax - xmin;
                    int height = ymax - ymin;

                    float targetStrength = detailStrength;
                    if (Event.current.shift || Event.current.control)
                        targetStrength = -targetStrength;

                    int[] layers = { detailPrototype };
                    if (targetStrength < 0.0F && !Event.current.control)
                        layers = terrainData.GetSupportedLayers(xmin, ymin, width, height);

                    for (int i = 0; i < layers.Length; i++)
                    {
                        int[,] alphamap = terrainData.GetDetailLayer(xmin, ymin, width, height, layers[i]);

                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                int xBrushOffset = (xmin + x) - (xCenter - intRadius + intFraction);
                                int yBrushOffset = (ymin + y) - (yCenter - intRadius + intFraction);
                                float opa = detailOpacity * m_BrushRep.GetStrengthInt(xBrushOffset, yBrushOffset);

                                float targetValue = Mathf.Lerp(alphamap[y, x], targetStrength, opa);
                                alphamap[y, x] = Mathf.RoundToInt(targetValue - .5f + Random.value);
                            }
                        }

                        terrainData.SetDetailLayer(xmin, ymin, layers[i], alphamap);
                    }
                }
            }

            return false;
        }

        public override void OnEnterToolMode()
        {
            Terrain terrain = null;
            if (Selection.activeGameObject != null)
            {
                terrain = Selection.activeGameObject.GetComponent<Terrain>();
            }

            if (terrain != null &&
                terrain.terrainData != null &&
                m_LastSelectedDetailPrototype != null)
            {
                for (int i = 0; i < terrain.terrainData.detailPrototypes.Length; ++i)
                {
                    if (m_LastSelectedDetailPrototype.Equals(terrain.terrainData.detailPrototypes[i]))
                    {
                        selectedDetail = i;
                        break;
                    }
                }
            }

            m_TargetTerrain = terrain;

            m_LastSelectedDetailPrototype = null;
        }

        public override void OnExitToolMode()
        {
            if (m_TargetTerrain != null &&
                m_TargetTerrain.terrainData != null &&
                selectedDetail != kInvalidDetail &&
                selectedDetail < m_TargetTerrain.terrainData.detailPrototypes.Length)
            {
                m_LastSelectedDetailPrototype = new DetailPrototype(m_TargetTerrain.terrainData.detailPrototypes[selectedDetail]);
            }

            selectedDetail = kInvalidDetail;
        }

        public override string GetName()
        {
            return "Paint Details";
        }

        public override string GetDesc()
        {
            return "Paints the selected detail prototype onto the terrain";
        }

        public override void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext)
        {
            // We're only doing painting operations, early out if it's not a repaint
            if (Event.current.type != EventType.Repaint)
                return;

            if (editContext.hitValidTerrain)
            {
                BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.raycastHit.textureCoord, editContext.brushSize, 0.0f);
                PaintContext ctx = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);
                TerrainPaintUtilityEditor.DrawBrushPreview(ctx, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture, editContext.brushTexture, brushXform, TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial(), 0);
                TerrainPaintUtility.ReleaseContextResources(ctx);
            }
        }
    }
}
