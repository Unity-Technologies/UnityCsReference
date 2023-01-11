// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TerrainTools;

namespace UnityEditor.TerrainTools
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

    internal class PaintTreesTool : TerrainPaintToolWithOverlays<PaintTreesTool>
    {
        internal const string k_ToolName = "Paint Trees";
        public override string OnIcon => "TerrainOverlays/PaintTrees_On.png";
        public override string OffIcon => "TerrainOverlays/PaintTrees.png";
        public const int kInvalidTree = -1;

        static class Styles
        {
            // Trees
            public static readonly GUIContent trees = EditorGUIUtility.TrTextContent("Trees");
            public static readonly GUIContent editTrees = EditorGUIUtility.TrTextContent("Edit Trees...", "Add/remove tree types.");
            public static readonly GUIContent treeDensity = EditorGUIUtility.TrTextContent("Tree Density", "How dense trees are you painting");
            public static readonly GUIContent treeHeight = EditorGUIUtility.TrTextContent("Tree Height", "The height scale of the planted trees");
            public static readonly GUIContent treeHeightRandomLabel = EditorGUIUtility.TrTextContent("Random?", "Enable random variation in tree height (variation)");
            public static readonly GUIContent treeHeightRandomToggle = EditorGUIUtility.TrTextContent("", "Enable random variation in tree height (variation)");
            public static readonly GUIContent lockWidthToHeight = EditorGUIUtility.TrTextContent("Lock Width to Height", "Let the tree width scale be equal to the tree height scale");
            public static readonly GUIContent treeWidth = EditorGUIUtility.TrTextContent("Tree Width", "The width scale of the planted trees");
            public static readonly GUIContent treeWidthRandomLabel = EditorGUIUtility.TrTextContent("Random?", "Enable random variation in tree width (variation)");
            public static readonly GUIContent treeWidthRandomToggle = EditorGUIUtility.TrTextContent("", "Enable random variation in tree width (variation)");
            public static readonly GUIContent treeColorVar = EditorGUIUtility.TrTextContent("Color Variation", "Amount of random shading applied to trees. This only works if the shader supports _TreeInstanceColor (for example, Speedtree shaders do not use this)");
            public static readonly GUIContent treeRotation = EditorGUIUtility.TrTextContent("Random Tree Rotation", "Randomize tree rotation. This only works when the tree has an LOD group.");
            public static readonly GUIContent treeRotationDisabled = EditorGUIUtility.TrTextContent("The selected tree does not have an LOD group, so it will use the default impostor system and will not support rotation.");
            public static readonly GUIContent treeHasChildRenderers = EditorGUIUtility.TrTextContent("The selected tree does not have an LOD group, but has a hierarchy of MeshRenderers, only MeshRenderer on root GameObject in the trees hierarchy will be used. Use a tree with LOD group if you want a tree with hierarchy of MeshRenderers.");
            public static readonly GUIContent massPlaceTrees = EditorGUIUtility.TrTextContent("Mass Place Trees", "The Mass Place Trees button is a very useful way to create an overall covering of trees without painting over the whole landscape. Following a mass placement, you can still use painting to add or remove trees to create denser or sparser areas.");
            public static readonly GUIContent treeContributeGI = EditorGUIUtility.TrTextContent("Tree Contribute Global Illumination", "The state of the Contribute GI flag for the tree prefab root GameObject. The flag can be changed on the prefab. When disabled, this tree will not be visible to the lightmapper. When enabled, any child GameObjects which also have the static flag enabled, will be present in lightmap calculations. Regardless of the value of the flag, each tree instance receives its own light probe and no lightmap texels.");
            public static readonly GUIContent noTreesDefined = EditorGUIUtility.TrTextContent("No trees defined.");
        }

        private TreePrototype m_LastSelectedTreePrototype;
        private Terrain       m_TargetTerrain;

        GUIContent[] m_TreeContents = null;

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

            for (int i = 0; i < ctx.neighborTerrains.Length; ++i)
            {
                Terrain ctxTerrain = ctx.neighborTerrains[i];
                if (ctxTerrain != null)
                {
                    Vector2 ctxUV = ctx.neighborUvs[i];

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

            for (int i = 0; i < ctx.neighborTerrains.Length; ++i)
            {
                Terrain ctxTerrain = ctx.neighborTerrains[i];
                if (ctxTerrain != null)
                {
                    Vector2 ctxUV = ctx.neighborUvs[i];
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

        public override int IconIndex
        {
            get { return (int) FoliageIndex.PaintTrees; }
        }

        public override TerrainCategory Category
        {
            get { return TerrainCategory.Foliage; }
        }

        public override string GetName()
        {
            return k_ToolName;
        }

        public override string GetDescription()
        {
            return "Click to paint trees.\n\nHold shift and click to erase trees.\n\nHold Ctrl and click to erase only trees of the selected type.";
        }

        public override bool HasToolSettings => true;

        public override void OnRenderBrushPreview(Terrain terrain, IOnSceneGUI editContext)
        {
            // We're only doing painting operations, early out if it's not a repaint
            if (Event.current.type != EventType.Repaint)
                return;

            if (editContext.hitValidTerrain)
            {
                BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.raycastHit.textureCoord, brushSize, 0.0f);
                PaintContext ctx = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);
                TerrainPaintUtilityEditor.DrawBrushPreview(ctx, TerrainBrushPreviewMode.SourceRenderTexture, editContext.brushTexture, brushXform, TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial(), 0);
                TerrainPaintUtility.ReleaseContextResources(ctx);
            }
        }

        void LoadTreeIcons(Terrain terrain)
        {
            // Locate the proto types asset preview textures
            TreePrototype[] trees = terrain.terrainData.treePrototypes;

            m_TreeContents = new GUIContent[trees.Length];
            for (int i = 0; i < m_TreeContents.Length; i++)
            {
                m_TreeContents[i] = new GUIContent();
                Texture tex = AssetPreview.GetAssetPreview(trees[i].prefab);
                m_TreeContents[i].image = tex != null ? tex : null;
                m_TreeContents[i].text = m_TreeContents[i].tooltip = trees[i].prefab != null ? trees[i].prefab.name : "Missing";
            }
        }

        void ShowUpgradeTreePrototypeScaleUI(Terrain terrain)
        {
            if (terrain.terrainData != null && terrain.terrainData.NeedUpgradeScaledTreePrototypes())
            {
                var msgContent = EditorGUIUtility.TempContent(
                    "Some of your prototypes have scaling values on the prefab. Since Unity 5.2 these scalings will be applied to terrain tree instances. Do you want to upgrade to this behaviour?",
                    EditorGUIUtility.GetHelpIcon(MessageType.Warning));
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label(msgContent, EditorStyles.wordWrappedLabel);
                GUILayout.Space(3);
                if (GUILayout.Button("Upgrade", GUILayout.ExpandWidth(false)))
                {
                    terrain.terrainData.UpgradeScaledTreePrototype();
                    TerrainMenus.RefreshPrototypes();
                }
                GUILayout.Space(3);
                GUILayout.EndVertical();
            }
        }

        public override void OnToolSettingsGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            LoadTreeIcons(terrain);

            // Tree picker
            GUI.changed = false;

            ShowUpgradeTreePrototypeScaleUI(terrain);

            GUILayout.Label(Styles.trees, EditorStyles.boldLabel);
            selectedTree = TerrainInspector.AspectSelectionGridImageAndText(selectedTree, m_TreeContents, 64, Styles.noTreesDefined, out var doubleClick);

            if (selectedTree >= m_TreeContents.Length)
                selectedTree = PaintTreesTool.kInvalidTree;

            if (doubleClick)
            {
                TerrainTreeContextMenus.EditTree(new MenuCommand(terrain, selectedTree));
                GUIUtility.ExitGUI();
            }

            GUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(selectedTree == PaintTreesTool.kInvalidTree))
            {
                if (GUILayout.Button(Styles.massPlaceTrees))
                {
                    TerrainMenus.MassPlaceTrees();
                }
            }
            GUILayout.FlexibleSpace();
            TerrainInspector.MenuButton(Styles.editTrees, "CONTEXT/TerrainEngineTrees", terrain, selectedTree);
            TerrainInspector.ShowRefreshPrototypes();
            GUILayout.EndHorizontal();

            GUILayout.Label(TerrainInspector.styles.settings, EditorStyles.boldLabel);
            // Placement distance
            brushSize = TerrainInspectorUtility.PowerSlider(TerrainInspector.styles.brushSize, brushSize, 1, Mathf.Min(terrain.terrainData.size.x, terrain.terrainData.size.z), 4.0f);
            float oldDens = (3.3f - spacing) / 3f;
            float newDens = TerrainInspectorUtility.ScaledSliderWithRounding(Styles.treeDensity, oldDens, 0.1f, 1.0f, 100.0f, 1.0f);
            // Only set spacing when value actually changes. Otherwise
            // it will lose precision because we're constantly doing math
            // back and forth with it.
            if (newDens != oldDens)
                spacing = (1.1f - newDens) * 3f;

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label(Styles.treeHeight, GUILayout.Width(EditorGUIUtility.labelWidth - 6));
            GUILayout.Label(Styles.treeHeightRandomLabel, GUILayout.ExpandWidth(false));
            allowHeightVar = GUILayout.Toggle(allowHeightVar, Styles.treeHeightRandomToggle, GUILayout.ExpandWidth(false));
            if (allowHeightVar)
            {
                EditorGUI.BeginChangeCheck();
                float min = treeHeight * (1.0f - treeHeightVariation);
                float max = treeHeight * (1.0f + treeHeightVariation);
                EditorGUILayout.MinMaxSlider(ref min, ref max, 0.01f, 2.0f);
                if (EditorGUI.EndChangeCheck())
                {
                    treeHeight = (min + max) * 0.5f;
                    treeHeightVariation = (max - min) / (min + max);
                }
            }
            else
            {
                treeHeight = EditorGUILayout.Slider(treeHeight, 0.01f, 2.0f);
                treeHeightVariation = 0.0f;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            lockWidthToHeight = EditorGUILayout.Toggle(Styles.lockWidthToHeight, lockWidthToHeight);

            GUILayout.Space(5);

            using (new EditorGUI.DisabledScope(lockWidthToHeight))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Styles.treeWidth, GUILayout.Width(EditorGUIUtility.labelWidth - 6));
                GUILayout.Label(Styles.treeWidthRandomLabel, GUILayout.ExpandWidth(false));
                allowWidthVar = GUILayout.Toggle(allowWidthVar, Styles.treeWidthRandomToggle, GUILayout.ExpandWidth(false));
                if (allowWidthVar)
                {
                    EditorGUI.BeginChangeCheck();
                    float min = treeWidth * (1.0f - treeWidthVariation);
                    float max = treeWidth * (1.0f + treeWidthVariation);
                    EditorGUILayout.MinMaxSlider(ref min, ref max, 0.01f, 2.0f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        treeWidth = (min + max) * 0.5f;
                        treeWidthVariation = (max - min) / (min + max);
                    }
                }
                else
                {
                    treeWidth = EditorGUILayout.Slider(treeWidth, 0.01f, 2.0f);
                    treeWidthVariation = 0.0f;
                }
                GUILayout.EndHorizontal();
            }

            if (selectedTree == PaintTreesTool.kInvalidTree)
                return;

            GUILayout.Space(5);

            GameObject prefab = terrain.terrainData.treePrototypes[selectedTree].m_Prefab;
            string treePrototypeWarning;
            terrain.terrainData.treePrototypes[selectedTree].Validate(out treePrototypeWarning);
            bool isLodTreePrototype = TerrainEditorUtility.IsLODTreePrototype(prefab);
            using (new EditorGUI.DisabledScope(!isLodTreePrototype))
            {
                randomRotation = EditorGUILayout.Toggle(Styles.treeRotation, randomRotation);
            }

            if (!isLodTreePrototype)
            {
                EditorGUILayout.HelpBox(Styles.treeRotationDisabled.text, MessageType.Info);
            }

            if (!string.IsNullOrEmpty(treePrototypeWarning))
            {
                EditorGUILayout.HelpBox(treePrototypeWarning, MessageType.Warning);
            }

            // TODO: we should check if the shaders assigned to this 'tree' support _TreeInstanceColor or not..  complicated check though
            treeColorAdjustment = EditorGUILayout.Slider(Styles.treeColorVar, treeColorAdjustment, 0, 1);

            if (prefab != null)
            {
                StaticEditorFlags staticEditorFlags = GameObjectUtility.GetStaticEditorFlags(prefab);
                bool contributeGI = (staticEditorFlags & StaticEditorFlags.ContributeGI) != 0;
                using (new EditorGUI.DisabledScope(true))   // Always disabled, because we don't want to edit the prefab.
                    contributeGI = EditorGUILayout.Toggle(Styles.treeContributeGI, contributeGI);
            }
        }

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            OnToolSettingsGUI(terrain, editContext);
        }

    }
}
