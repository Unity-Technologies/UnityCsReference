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
    internal class PaintTreesUtils
    {
        public static bool ValidateTreePrototype(Terrain terrain, int treePrototype)
        {
            int prototypeCount = TerrainInspectorUtil.GetPrototypeCount(terrain.terrainData);
            if (treePrototype == PaintTreesTool.kInvalidTree || treePrototype >= prototypeCount)
                return false;

            if (!TerrainInspectorUtil.PrototypeIsRenderable(terrain.terrainData, treePrototype))
                return false;

            return true;
        }

        public static int FindTreePrototype(Terrain terrain, Terrain sourceTerrain, int sourceTree)
        {
            if (sourceTree == PaintTreesTool.kInvalidTree ||
                sourceTree >= sourceTerrain.terrainData.treePrototypes.Length)
            {
                return PaintTreesTool.kInvalidTree;
            }

            if (terrain == sourceTerrain)
            {
                return sourceTree;
            }

            TreePrototype sourceTreePrototype = sourceTerrain.terrainData.treePrototypes[sourceTree];
            for (int i = 0; i < terrain.terrainData.treePrototypes.Length; ++i)
            {
                if (sourceTreePrototype.Equals(terrain.terrainData.treePrototypes[i]))
                    return i;
            }

            return PaintTreesTool.kInvalidTree;
        }

        public static int CopyTreePrototype(Terrain terrain, Terrain sourceTerrain, int sourceTree)
        {
            TreePrototype sourceTreePrototype = sourceTerrain.terrainData.treePrototypes[sourceTree];
            TreePrototype[] newTreePrototypesArray = new TreePrototype[terrain.terrainData.treePrototypes.Length + 1];
            System.Array.Copy(terrain.terrainData.treePrototypes, newTreePrototypesArray, terrain.terrainData.treePrototypes.Length);
            newTreePrototypesArray[newTreePrototypesArray.Length - 1] = new TreePrototype(sourceTreePrototype);
            terrain.terrainData.treePrototypes = newTreePrototypesArray;
            return newTreePrototypesArray.Length - 1;
        }

        public static void PlaceTree(Terrain terrain, int treePrototype, Vector3 position, Color color, float height, float width, float rotation)
        {
            TreeInstance instance = new TreeInstance();
            instance.position = position;
            instance.color = color;
            instance.lightmapColor = Color.white;
            instance.prototypeIndex = treePrototype;
            instance.heightScale = height;
            instance.widthScale = width;
            instance.rotation = rotation;
            terrain.AddTreeInstance(instance);
        }
    }

    internal class PaintTreesTool : TerrainPaintTool<PaintTreesTool>
    {
        public const int kInvalidTree = -1;

        private TreePrototype m_LastSelectedTreePrototype;
        private Terrain       m_TargetTerrain;

        public float brushSize { get; set; } = 40;
        public float spacing { get; set; } = .8f;
        public bool lockWidthToHeight { get; set; } = true;
        public bool randomRotation { get; set; } = true;
        public bool allowHeightVar { get; set; } = true;
        public bool allowWidthVar { get; set; } = true;
        public float treeColorAdjustment { get; set; } = .4f;
        public float treeHeight { get; set; } = 1;
        public float treeHeightVariation { get; set; } = .1f;
        public float treeWidth { get; set; } = 1;
        public float treeWidthVariation { get; set; } = .1f;
        public int selectedTree { get; set; } = kInvalidTree;

        private Color GetTreeColor()
        {
            Color c = Color.white * Random.Range(1.0F, 1.0F - treeColorAdjustment);
            c.a = 1;
            return c;
        }

        private float GetTreeHeight()
        {
            float v = allowHeightVar ? treeHeightVariation : 0.0f;
            return treeHeight * Random.Range(1.0F - v, 1.0F + v);
        }

        private float GetRandomizedTreeWidth()
        {
            float v = allowWidthVar ? treeWidthVariation : 0.0f;
            return treeWidth * Random.Range(1.0F - v, 1.0F + v);
        }

        private float GetTreeWidth(float height)
        {
            float width;

            if (lockWidthToHeight)
            {
                // keep scales equal since these scales are applied to the
                // prefab scale to get the final tree instance scale
                width = height;
            }
            else
            {
                width = GetRandomizedTreeWidth();
            }

            return width;
        }

        private float GetTreeRotation()
        {
            return randomRotation ? Random.Range(0, 2 * Mathf.PI) : 0;
        }

        private void PlaceTrees(Terrain terrain, IOnPaint editContext)
        {
            if (m_TargetTerrain == null ||
                selectedTree == kInvalidTree ||
                selectedTree >= m_TargetTerrain.terrainData.treePrototypes.Length)
            {
                return;
            }

            PaintTreesDetailsContext ctx = PaintTreesDetailsContext.Create(terrain, editContext.uv);

            int placedTreeCount = 0;

            int treePrototype = PaintTreesUtils.FindTreePrototype(terrain, m_TargetTerrain, selectedTree);
            if (treePrototype == kInvalidTree)
            {
                treePrototype = PaintTreesUtils.CopyTreePrototype(terrain, m_TargetTerrain, selectedTree);
            }

            if (PaintTreesUtils.ValidateTreePrototype(terrain, treePrototype))
            {
                // When painting single tree
                // And just clicking we always place it, so you can do overlapping trees
                Vector3 position = new Vector3(editContext.uv.x, 0, editContext.uv.y);
                bool checkTreeDistance = Event.current.type == EventType.MouseDrag || brushSize > 1;
                if (!checkTreeDistance || TerrainInspectorUtil.CheckTreeDistance(terrain.terrainData, position, treePrototype, spacing))
                {
                    TerrainPaintUtilityEditor.UpdateTerrainDataUndo(terrain.terrainData, "Terrain - Place Trees");

                    var instanceHeight = GetTreeHeight();
                    PaintTreesUtils.PlaceTree(terrain, treePrototype, position, GetTreeColor(), instanceHeight, GetTreeWidth(instanceHeight), GetTreeRotation());
                    ++placedTreeCount;
                }
            }

            for (int i = 0; i < ctx.terrains.Length; ++i)
            {
                Terrain ctxTerrain = ctx.terrains[i];
                if (ctxTerrain != null)
                {
                    Vector2 ctxUV = ctx.uvs[i];

                    treePrototype = PaintTreesUtils.FindTreePrototype(ctxTerrain, m_TargetTerrain, selectedTree);
                    if (treePrototype == kInvalidTree)
                    {
                        treePrototype = PaintTreesUtils.CopyTreePrototype(ctxTerrain, m_TargetTerrain, selectedTree);
                    }

                    if (PaintTreesUtils.ValidateTreePrototype(ctxTerrain, treePrototype))
                    {
                        Vector3 size = TerrainInspectorUtil.GetPrototypeExtent(ctxTerrain.terrainData, treePrototype);
                        size.y = 0;
                        float treeCountOneAxis = brushSize / (size.magnitude * spacing * .5f);
                        int treeCount = (int)((treeCountOneAxis * treeCountOneAxis) * .5f);
                        treeCount = Mathf.Clamp(treeCount, 0, 100);
                        // Plant a bunch of trees
                        for (int j = ctxTerrain == terrain ? 1 : 0; j < treeCount && placedTreeCount < treeCount; ++j)
                        {
                            Vector2 randomOffset = 0.5f * Random.insideUnitCircle;
                            randomOffset.x *= brushSize / ctxTerrain.terrainData.size.x;
                            randomOffset.y *= brushSize / ctxTerrain.terrainData.size.z;
                            Vector3 position = new Vector3(ctxUV.x + randomOffset.x, 0, ctxUV.y + randomOffset.y);
                            if (position.x >= 0 && position.x <= 1 && position.z >= 0 && position.z <= 1
                                && TerrainInspectorUtil.CheckTreeDistance(ctxTerrain.terrainData, position, treePrototype, spacing * .5f))
                            {
                                TerrainPaintUtilityEditor.UpdateTerrainDataUndo(ctxTerrain.terrainData, "Terrain - Place Trees");

                                var instanceHeight = GetTreeHeight();
                                PaintTreesUtils.PlaceTree(ctxTerrain, treePrototype, position, GetTreeColor(), instanceHeight, GetTreeWidth(instanceHeight), GetTreeRotation());
                                ++placedTreeCount;
                            }
                        }
                    }
                }
            }
        }

        private void RemoveTrees(Terrain terrain, IOnPaint editContext, bool clearSelectedOnly)
        {
            PaintTreesDetailsContext ctx = PaintTreesDetailsContext.Create(terrain, editContext.uv);

            for (int i = 0; i < ctx.terrains.Length; ++i)
            {
                Terrain ctxTerrain = ctx.terrains[i];
                if (ctxTerrain != null)
                {
                    Vector2 ctxUV = ctx.uvs[i];
                    float radius = 0.5f * brushSize / ctxTerrain.terrainData.size.x;

                    int treePrototype = kInvalidTree;
                    if (clearSelectedOnly && selectedTree != kInvalidTree)
                    {
                        treePrototype = PaintTreesUtils.FindTreePrototype(ctxTerrain, m_TargetTerrain, selectedTree);
                    }

                    if (!clearSelectedOnly || treePrototype != kInvalidTree)
                    {
                        TerrainPaintUtilityEditor.UpdateTerrainDataUndo(ctxTerrain.terrainData, "Terrain - Remove Trees");

                        ctxTerrain.RemoveTrees(ctxUV, radius, treePrototype);
                    }
                }
            }
        }

        public void MassPlaceTrees(TerrainData terrainData, int numberOfTrees, bool randomTreeColor, bool keepExistingTrees)
        {
            int nbPrototypes = terrainData.treePrototypes.Length;
            if (nbPrototypes == 0)
            {
                Debug.Log("Can't place trees because no prototypes are defined");
                return;
            }

            Undo.RegisterCompleteObjectUndo(terrainData, "Mass Place Trees");

            TreeInstance[] instances = new TreeInstance[numberOfTrees];
            int i = 0;
            while (i < instances.Length)
            {
                TreeInstance instance = new TreeInstance();
                instance.position = new Vector3(Random.value, 0, Random.value);
                if (terrainData.GetSteepness(instance.position.x, instance.position.z) < 30)
                {
                    instance.color = randomTreeColor ? GetTreeColor() : Color.white;
                    instance.lightmapColor = Color.white;
                    instance.prototypeIndex = Random.Range(0, nbPrototypes);

                    instance.heightScale = GetTreeHeight();
                    instance.widthScale = GetTreeWidth(instance.heightScale);

                    instance.rotation = GetTreeRotation();

                    instances[i++] = instance;
                }
            }

            if (keepExistingTrees)
            {
                var existingTrees = terrainData.treeInstances;
                var allTrees = new TreeInstance[existingTrees.Length + instances.Length];
                System.Array.Copy(existingTrees, 0, allTrees, 0, existingTrees.Length);
                System.Array.Copy(instances, 0, allTrees, existingTrees.Length, instances.Length);
                instances = allTrees;
            }

            terrainData.SetTreeInstances(instances, true);
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            if (!Event.current.shift && !Event.current.control)
            {
                if (selectedTree != PaintTreesTool.kInvalidTree)
                {
                    PlaceTrees(terrain, editContext);
                }
            }
            else
            {
                RemoveTrees(terrain, editContext, Event.current.control);
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
                m_LastSelectedTreePrototype != null)
            {
                for (int i = 0; i < terrain.terrainData.treePrototypes.Length; ++i)
                {
                    if (m_LastSelectedTreePrototype.Equals(terrain.terrainData.treePrototypes[i]))
                    {
                        selectedTree = i;
                        break;
                    }
                }
            }

            m_TargetTerrain = terrain;

            m_LastSelectedTreePrototype = null;
        }

        public override void OnExitToolMode()
        {
            if (m_TargetTerrain != null &&
                m_TargetTerrain.terrainData != null &&
                selectedTree != kInvalidTree &&
                selectedTree < m_TargetTerrain.terrainData.treePrototypes.Length)
            {
                m_LastSelectedTreePrototype = new TreePrototype(m_TargetTerrain.terrainData.treePrototypes[selectedTree]);
            }

            selectedTree = kInvalidTree;
        }

        public override string GetName()
        {
            return "Paint Trees";
        }

        public override string GetDesc()
        {
            return "Paints the selected tree prototype onto the terrain";
        }

        public override void OnRenderBrushPreview(Terrain terrain, IOnSceneGUI editContext)
        {
            // We're only doing painting operations, early out if it's not a repaint
            if (Event.current.type != EventType.Repaint)
                return;

            if (editContext.hitValidTerrain)
            {
                BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.raycastHit.textureCoord, brushSize, 0.0f);
                PaintContext ctx = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);
                TerrainPaintUtilityEditor.DrawBrushPreview(ctx, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture, editContext.brushTexture, brushXform, TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial(), 0);
                TerrainPaintUtility.ReleaseContextResources(ctx);
            }
        }
    }
}
