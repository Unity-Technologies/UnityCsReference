// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


/*
GUILayout.TextureGrid number of horiz elements doesnt work
*/

using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.AnimatedValues;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor
{
    internal enum TerrainTool
    {
        None = -1,
        PaintHeight = 0,
        SetHeight,
        SmoothHeight,
        PaintTexture,
        PlaceTree,
        PaintDetail,
        TerrainSettings,
        TerrainToolCount
    }

    internal class SplatPainter
    {
        public int size;
        public float strength;
        public Brush brush;
        public float target;
        public TerrainData terrainData;
        public TerrainTool tool;

        float ApplyBrush(float height, float brushStrength)
        {
            if (target > height)
            {
                height += brushStrength;
                height = Mathf.Min(height, target);
                return height;
            }
            else
            {
                height -= brushStrength;
                height = Mathf.Max(height, target);
                return height;
            }
        }

        // Normalize the alpha map at pixel x,y.
        // The alpha of splatIndex will be maintained, while all others will be made to fit
        void Normalize(int x, int y, int splatIndex, float[,,] alphamap)
        {
            float newAlpha = alphamap[y, x, splatIndex];
            float totalAlphaOthers = 0.0F;
            int alphaMaps = alphamap.GetLength(2);
            for (int a = 0; a < alphaMaps; a++)
            {
                if (a != splatIndex)
                    totalAlphaOthers += alphamap[y, x, a];
            }

            if (totalAlphaOthers > 0.01)
            {
                float adjust = (1.0F - newAlpha) / totalAlphaOthers;
                for (int a = 0; a < alphaMaps; a++)
                {
                    if (a != splatIndex)
                        alphamap[y, x, a] *= adjust;
                }
            }
            else
            {
                for (int a = 0; a < alphaMaps; a++)
                {
                    alphamap[y, x, a] = a == splatIndex ? 1.0F : 0.0F;
                }
            }
        }

        public void Paint(float xCenterNormalized, float yCenterNormalized, int splatIndex)
        {
            if (splatIndex >= terrainData.alphamapLayers)
                return;
            int xCenter = Mathf.FloorToInt(xCenterNormalized * terrainData.alphamapWidth);
            int yCenter = Mathf.FloorToInt(yCenterNormalized * terrainData.alphamapHeight);

            int intRadius = Mathf.RoundToInt(size) / 2;
            int intFraction = Mathf.RoundToInt(size) % 2;

            int xmin = Mathf.Clamp(xCenter - intRadius, 0, terrainData.alphamapWidth - 1);
            int ymin = Mathf.Clamp(yCenter - intRadius, 0, terrainData.alphamapHeight - 1);

            int xmax = Mathf.Clamp(xCenter + intRadius + intFraction, 0, terrainData.alphamapWidth);
            int ymax = Mathf.Clamp(yCenter + intRadius + intFraction, 0, terrainData.alphamapHeight);

            int width = xmax - xmin;
            int height = ymax - ymin;

            float[,,] alphamap = terrainData.GetAlphamaps(xmin, ymin, width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int xBrushOffset = (xmin + x) - (xCenter - intRadius + intFraction);
                    int yBrushOffset = (ymin + y) - (yCenter - intRadius + intFraction);
                    float brushStrength = brush.GetStrengthInt(xBrushOffset, yBrushOffset);

                    // Paint with brush
                    float newAlpha = ApplyBrush(alphamap[y, x, splatIndex], brushStrength * strength);
                    alphamap[y, x, splatIndex] = newAlpha;
                    Normalize(x, y, splatIndex, alphamap);
                }
            }

            terrainData.SetAlphamaps(xmin, ymin, alphamap);
        }
    }

    internal class DetailPainter
    {
        public int size;
        public float opacity;
        public float targetStrength;
        public Brush brush;
        public TerrainData terrainData;
        public TerrainTool tool;
        public bool randomizeDetails;
        public bool clearSelectedOnly;

        public void Paint(float xCenterNormalized, float yCenterNormalized, int detailIndex)
        {
            if (detailIndex >= terrainData.detailPrototypes.Length)
                return;

            int xCenter = Mathf.FloorToInt(xCenterNormalized * terrainData.detailWidth);
            int yCenter = Mathf.FloorToInt(yCenterNormalized * terrainData.detailHeight);

            int intRadius = Mathf.RoundToInt(size) / 2;
            int intFraction = Mathf.RoundToInt(size) % 2;

            int xmin = Mathf.Clamp(xCenter - intRadius, 0, terrainData.detailWidth - 1);
            int ymin = Mathf.Clamp(yCenter - intRadius, 0, terrainData.detailHeight - 1);

            int xmax = Mathf.Clamp(xCenter + intRadius + intFraction, 0, terrainData.detailWidth);
            int ymax = Mathf.Clamp(yCenter + intRadius + intFraction, 0, terrainData.detailHeight);

            int width = xmax - xmin;
            int height = ymax - ymin;

            int[] layers = { detailIndex };
            if (targetStrength < 0.0F && !clearSelectedOnly)
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
                        float opa = opacity * brush.GetStrengthInt(xBrushOffset, yBrushOffset);

                        float t = targetStrength;
                        float targetValue = Mathf.Lerp(alphamap[y, x], t, opa);
                        alphamap[y, x] = Mathf.RoundToInt(targetValue - .5f + Random.value);
                    }
                }

                terrainData.SetDetailLayer(xmin, ymin, layers[i], alphamap);
            }
        }
    }

    internal class TreePainter
    {
        public static float brushSize = 40;
        public static float spacing = .8f;

        public static bool lockWidthToHeight = true;
        public static bool randomRotation = true;

        public static bool allowHeightVar = true;
        public static bool allowWidthVar = true;

        public static float treeColorAdjustment = .4f;
        public static float treeHeight = 1;
        public static float treeHeightVariation = .1f;
        public static float treeWidth = 1;
        public static float treeWidthVariation = .1f;

        public static int selectedTree = -1;

        static Color GetTreeColor()
        {
            Color c = Color.white * Random.Range(1.0F, 1.0F - treeColorAdjustment);
            c.a = 1;
            return c;
        }

        static float GetTreeHeight()
        {
            float v = allowHeightVar ? treeHeightVariation : 0.0f;
            return treeHeight * Random.Range(1.0F - v, 1.0F + v);
        }

        static float GetTreeWidth()
        {
            float v = allowWidthVar ? treeWidthVariation : 0.0f;
            return treeWidth * Random.Range(1.0F - v, 1.0F + v);
        }

        static float GetTreeRotation()
        {
            return randomRotation ? Random.Range(0, 2 * Mathf.PI) : 0;
        }

        public static void PlaceTrees(Terrain terrain, float xBase, float yBase)
        {
            int prototypeCount = TerrainInspectorUtil.GetPrototypeCount(terrain.terrainData);
            if (selectedTree == -1 || selectedTree >= prototypeCount)
                return;

            if (!TerrainInspectorUtil.PrototypeIsRenderable(terrain.terrainData, selectedTree))
                return;

            int placedTreeCount = 0;

            // Plant a single tree first. At the location of the mouse
            TreeInstance instance = new TreeInstance();
            instance.position = new Vector3(xBase, 0, yBase);
            instance.color = GetTreeColor();
            instance.lightmapColor = Color.white;
            instance.prototypeIndex = selectedTree;
            instance.heightScale = GetTreeHeight();
            instance.widthScale = lockWidthToHeight ? instance.heightScale : GetTreeWidth();
            instance.rotation = GetTreeRotation();

            // When painting single tree
            // And just clicking we always place it, so you can do overlapping trees

            bool checkTreeDistance = Event.current.type == EventType.MouseDrag || brushSize > 1;
            if (!checkTreeDistance || TerrainInspectorUtil.CheckTreeDistance(terrain.terrainData, instance.position, instance.prototypeIndex, spacing))
            {
                terrain.AddTreeInstance(instance);
                placedTreeCount++;
            }

            Vector3 size = TerrainInspectorUtil.GetPrototypeExtent(terrain.terrainData, selectedTree);
            size.y = 0;
            float treeCountOneAxis = brushSize / (size.magnitude * spacing * .5f);
            int treeCount = (int)((treeCountOneAxis * treeCountOneAxis) * .5f);
            treeCount = Mathf.Clamp(treeCount, 0, 100);
            // Plant a bunch of trees
            for (int i = 1; i < treeCount && placedTreeCount < treeCount; i++)
            {
                Vector2 randomOffset = Random.insideUnitCircle;
                randomOffset.x *= brushSize / terrain.terrainData.size.x;
                randomOffset.y *= brushSize / terrain.terrainData.size.z;
                Vector3 position = new Vector3(xBase + randomOffset.x, 0, yBase + randomOffset.y);
                if (position.x >= 0 && position.x <= 1 && position.z >= 0 && position.z <= 1
                    && TerrainInspectorUtil.CheckTreeDistance(terrain.terrainData, position, selectedTree, spacing * .5f))
                {
                    instance = new TreeInstance();

                    instance.position = position;

                    instance.color = GetTreeColor();
                    instance.lightmapColor = Color.white;
                    instance.prototypeIndex = selectedTree;
                    instance.heightScale = GetTreeHeight();
                    instance.widthScale = lockWidthToHeight ? instance.heightScale : GetTreeWidth();
                    instance.rotation = GetTreeRotation();

                    terrain.AddTreeInstance(instance);
                    placedTreeCount++;
                }
            }
        }

        public static void RemoveTrees(Terrain terrain, float xBase, float yBase, bool clearSelectedOnly)
        {
            float radius = brushSize / terrain.terrainData.size.x;
            terrain.RemoveTrees(new Vector2(xBase, yBase), radius, clearSelectedOnly ? selectedTree : -1);
        }

        public static void MassPlaceTrees(TerrainData terrainData, int numberOfTrees, bool randomTreeColor, bool keepExistingTrees)
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
                    instance.widthScale = lockWidthToHeight ? instance.heightScale : GetTreeWidth();

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

            terrainData.treeInstances = instances;
            terrainData.RecalculateTreePositions();
        }
    }

    internal class HeightmapPainter
    {
        public int size;
        public float strength;
        public float targetHeight;
        public TerrainTool tool;
        public Brush brush;
        public TerrainData terrainData;

        float Smooth(int x, int y)
        {
            float h = 0.0F;
            float normalizeScale = 1.0F / terrainData.size.y;
            h += terrainData.GetHeight(x, y) * normalizeScale;
            h += terrainData.GetHeight(x + 1, y) * normalizeScale;
            h += terrainData.GetHeight(x - 1, y) * normalizeScale;
            h += terrainData.GetHeight(x + 1, y + 1) * normalizeScale * 0.75F;
            h += terrainData.GetHeight(x - 1, y + 1) * normalizeScale * 0.75F;
            h += terrainData.GetHeight(x + 1, y - 1) * normalizeScale * 0.75F;
            h += terrainData.GetHeight(x - 1, y - 1) * normalizeScale * 0.75F;
            h += terrainData.GetHeight(x, y + 1) * normalizeScale;
            h += terrainData.GetHeight(x, y - 1) * normalizeScale;
            h /= 8.0F;
            return h;
        }

        float ApplyBrush(float height, float brushStrength, int x, int y)
        {
            if (tool == TerrainTool.PaintHeight)
                return height + brushStrength;
            else if (tool == TerrainTool.SetHeight)
            {
                if (targetHeight > height)
                {
                    height += brushStrength;
                    height = Mathf.Min(height, targetHeight);
                    return height;
                }
                else
                {
                    height -= brushStrength;
                    height = Mathf.Max(height, targetHeight);
                    return height;
                }
            }
            else if (tool == TerrainTool.SmoothHeight)
            {
                return Mathf.Lerp(height, Smooth(x, y), brushStrength);
            }
            else
                return height;
        }

        public void PaintHeight(float xCenterNormalized, float yCenterNormalized)
        {
            int xCenter, yCenter;
            if (size % 2 == 0)
            {
                xCenter = Mathf.CeilToInt(xCenterNormalized * (terrainData.heightmapWidth - 1));
                yCenter = Mathf.CeilToInt(yCenterNormalized * (terrainData.heightmapHeight - 1));
            }
            else
            {
                xCenter = Mathf.RoundToInt(xCenterNormalized * (terrainData.heightmapWidth - 1));
                yCenter = Mathf.RoundToInt(yCenterNormalized * (terrainData.heightmapHeight - 1));
            }

            int intRadius = size / 2;
            int intFraction = size % 2;

            int xmin = Mathf.Clamp(xCenter - intRadius, 0, terrainData.heightmapWidth - 1);
            int ymin = Mathf.Clamp(yCenter - intRadius, 0, terrainData.heightmapHeight - 1);

            int xmax = Mathf.Clamp(xCenter + intRadius + intFraction, 0, terrainData.heightmapWidth);
            int ymax = Mathf.Clamp(yCenter + intRadius + intFraction, 0, terrainData.heightmapHeight);

            int width = xmax - xmin;
            int height = ymax - ymin;

            float[,] heights = terrainData.GetHeights(xmin, ymin, width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int xBrushOffset = (xmin + x) - (xCenter - intRadius);
                    int yBrushOffset = (ymin + y) - (yCenter - intRadius);
                    float brushStrength = brush.GetStrengthInt(xBrushOffset, yBrushOffset);
                    //Debug.Log(xBrushOffset + ", " + yBrushOffset + "=" + brushStrength);
                    float value = heights[y, x];
                    value = ApplyBrush(value, brushStrength * strength, x + xmin, y + ymin);
                    heights[y, x] = value;
                }
            }

            terrainData.SetHeightsDelayLOD(xmin, ymin, heights);
        }
    }

    [CustomEditor(typeof(Terrain))]
    internal class TerrainInspector : Editor
    {
        class Styles
        {
            public GUIStyle gridList = "GridList";
            public GUIStyle gridListText = "GridListText";
            public GUIStyle largeSquare = "Button";
            public GUIStyle command = "Command";
            public Texture settingsIcon = EditorGUIUtility.IconContent("SettingsIcon").image;
            // List of tools supported by the editor
            public GUIContent[] toolIcons =
            {
                EditorGUIUtility.IconContent("TerrainInspector.TerrainToolRaise", "|Raise/Lower Terrain)"),
                EditorGUIUtility.IconContent("TerrainInspector.TerrainToolSetHeight", "|Paint Height"),
                EditorGUIUtility.IconContent("TerrainInspector.TerrainToolSmoothHeight", "|Smooth Height"),
                EditorGUIUtility.IconContent("TerrainInspector.TerrainToolSplat", "|Paint Texture"),
                EditorGUIUtility.IconContent("TerrainInspector.TerrainToolTrees", "|Paint Trees"),
                EditorGUIUtility.IconContent("TerrainInspector.TerrainToolPlants", "|Paint Details"),
                EditorGUIUtility.IconContent("TerrainInspector.TerrainToolSettings", "|Terrain Settings")
            };

            public GUIContent[] toolNames =
            {
                EditorGUIUtility.TextContent("Raise/Lower Terrain|Click to raise.\n\nHold shift and click to lower."),
                EditorGUIUtility.TextContent("Paint Height|Click to paint height.\n\nHold shift and click to sample target height."),
                EditorGUIUtility.TextContent("Smooth Height|Click to average out height."),
                EditorGUIUtility.TextContent("Paint Texture|Select a texture below, then click to paint."),
                EditorGUIUtility.TextContent("Paint Trees|Click to paint trees.\n\nHold shift and click to erase trees.\n\nHold Ctrl and click to erase only trees of the selected type."),
                EditorGUIUtility.TextContent("Paint Details|Click to paint details.\n\nHold shift and click to erase details.\n\nHold Ctrl and click to erase only details of the selected type."),
                EditorGUIUtility.TextContent("Terrain Settings")
            };

            public GUIContent brushSize = EditorGUIUtility.TextContent("Brush Size|Size of the brush used to paint.");
            public GUIContent opacity = EditorGUIUtility.TextContent("Opacity|Strength of the applied effect.");
            public GUIContent targetStrength = EditorGUIUtility.TextContent("Target Strength|Maximum opacity you can reach by painting continuously.");
            public GUIContent settings = EditorGUIUtility.TextContent("Settings");
            public GUIContent brushes = EditorGUIUtility.TextContent("Brushes");

            public GUIContent mismatchedTerrainData = EditorGUIUtility.TextContentWithIcon(
                    "The TerrainData used by the TerrainCollider component is different from this terrain. Would you like to assign the same TerrainData to the TerrainCollider component?",
                    "console.warnicon");

            public GUIContent assign = EditorGUIUtility.TextContent("Assign");

            // Textures
            public GUIContent textures = EditorGUIUtility.TextContent("Textures");
            public GUIContent editTextures = EditorGUIUtility.TextContent("Edit Textures...");

            // Trees
            public GUIContent trees = EditorGUIUtility.TextContent("Trees");
            public GUIContent noTrees = EditorGUIUtility.TextContent("No Trees defined|Use edit button below to add new tree types.");
            public GUIContent editTrees = EditorGUIUtility.TextContent("Edit Trees...|Add/remove tree types.");
            public GUIContent treeDensity = EditorGUIUtility.TextContent("Tree Density|How dense trees are you painting");
            public GUIContent treeHeight = EditorGUIUtility.TextContent("Tree Height|Height of the planted trees");
            public GUIContent treeHeightRandomLabel = EditorGUIUtility.TextContent("Random?|Enable random variation in tree height (variation)");
            public GUIContent treeHeightRandomToggle = EditorGUIUtility.TextContent("|Enable random variation in tree height (variation)");
            public GUIContent lockWidth = EditorGUIUtility.TextContent("Lock Width to Height|Let the tree width be the same with height");
            public GUIContent treeWidth = EditorGUIUtility.TextContent("Tree Width|Width of the planted trees");
            public GUIContent treeWidthRandomLabel = EditorGUIUtility.TextContent("Random?|Enable random variation in tree width (variation)");
            public GUIContent treeWidthRandomToggle = EditorGUIUtility.TextContent("|Enable random variation in tree width (variation)");
            public GUIContent treeColorVar = EditorGUIUtility.TextContent("Color Variation|Amount of random shading applied to trees");
            public GUIContent treeRotation = EditorGUIUtility.TextContent("Random Tree Rotation|Enable?");
            public GUIContent massPlaceTrees = EditorGUIUtility.TextContent("Mass Place Trees|The Mass Place Trees button is a very useful way to create an overall covering of trees without painting over the whole landscape. Following a mass placement, you can still use painting to add or remove trees to create denser or sparser areas.");
            public GUIContent treeLightmapStatic = EditorGUIUtility.TextContent("Tree Lightmap Static|The state of the Lightmap Static flag for the tree prefab root GameObject. The flag can be changed on the prefab. When disabled, this tree will not be visible to the lightmapper. When enabled, any child GameObjects which also have the static flag enabled, will be present in lightmap calculations. Regardless of the Static flag, each tree instance receives its own light probe and no lightmap texels.");

            // Details
            public GUIContent details = EditorGUIUtility.TextContent("Details");
            public GUIContent editDetails = EditorGUIUtility.TextContent("Edit Details...|Add/remove detail meshes");
            public GUIContent detailTargetStrength = EditorGUIUtility.TextContent("Target Strength|Target amount");

            // Heightmaps
            public GUIContent height = EditorGUIUtility.TextContent("Height|You can set the Height property manually or you can shift-click on the terrain to sample the height at the mouse position (rather like the “eyedropper” tool in an image editor).");

            public GUIContent heightmap = EditorGUIUtility.TextContent("Heightmap");
            public GUIContent importRaw  = EditorGUIUtility.TextContent("Import Raw...|The Import Raw button allows you to set the terrain’s heightmap from an image file in the RAW grayscale format. RAW format can be generated by third party terrain editing tools (such as Bryce) and can also be opened, edited and saved by Photoshop. This allows for sophisticated generation and editing of terrains outside Unity.");
            public GUIContent exportRaw = EditorGUIUtility.TextContent("Export Raw...|The Export Raw button allows you to save the terrain’s heightmap to an image file in the RAW grayscale format. RAW format can be generated by third party terrain editing tools (such as Bryce) and can also be opened, edited and saved by Photoshop. This allows for sophisticated generation and editing of terrains outside Unity.");
            public GUIContent flatten = EditorGUIUtility.TextContent("Flatten|The Flatten button levels the whole terrain to the chosen height.");

            public GUIContent bakeLightProbesForTrees = EditorGUIUtility.TextContent("Bake Light Probes For Trees|If the option is enabled, Unity will create internal light probes at the position of each tree (these probes are internal and will not affect other renderers in the scene) and apply them to tree renderers for lighting. Otherwise trees are still affected by LightProbeGroups. The option is only effective for trees that have LightProbe enabled on their prototype prefab.");
            public GUIContent refresh = EditorGUIUtility.TextContent("Refresh|When you save a tree asset from the modelling app, you will need to click the Refresh button (shown in the inspector when the tree painting tool is selected) in order to see the updated trees on your terrain.");

            // Settings
            public GUIContent drawTerrain = EditorGUIUtility.TextContent("Draw|Toggle the rendering of terrain");
            public GUIContent pixelError = EditorGUIUtility.TextContent("Pixel Error|The accuracy of the mapping between the terrain maps (heightmap, textures, etc) and the generated terrain; higher values indicate lower accuracy but lower rendering overhead.");
            public GUIContent baseMapDist = EditorGUIUtility.TextContent("Base Map Dist.|The maximum distance at which terrain textures will be displayed at full resolution. Beyond this distance, a lower resolution composite image will be used for efficiency.");
            public GUIContent castShadows = EditorGUIUtility.TextContent("Cast Shadows|Does the terrain cast shadows?");
            public GUIContent material = EditorGUIUtility.TextContent("Material|The material used to render the terrain. This will affect how the color channels of a terrain texture are interpreted.");
            public GUIContent reflectionProbes = EditorGUIUtility.TextContent("Reflection Probes|How reflection probes are used on terrain. Only effective when using built-in standard material or a custom material which supports rendering with reflection.");
            public GUIContent thickness = EditorGUIUtility.TextContent("Thickness|How much the terrain collision volume should extend along the negative Y-axis. Objects are considered colliding with the terrain from the surface to a depth equal to the thickness. This helps prevent high-speed moving objects from penetrating into the terrain without using expensive continuous collision detection.");

            public GUIContent drawTrees = EditorGUIUtility.TextContent("Draw|Should trees, grass and details be drawn?");
            public GUIContent detailObjectDistance = EditorGUIUtility.TextContent("Detail Distance|The distance (from camera) beyond which details will be culled.");
            public GUIContent collectDetailPatches = EditorGUIUtility.TextContent("Collect Detail Patches|Should detail patches in the Terrain be removed from memory when not visible?");
            public GUIContent detailObjectDensity = EditorGUIUtility.TextContent("Detail Density|The number of detail/grass objects in a given unit of area. The value can be set lower to reduce rendering overhead.");
            public GUIContent treeDistance = EditorGUIUtility.TextContent("Tree Distance|The distance (from camera) beyond which trees will be culled.");
            public GUIContent treeBillboardDistance = EditorGUIUtility.TextContent("Billboard Start|The distance (from camera) at which 3D tree objects will be replaced by billboard images.");
            public GUIContent treeCrossFadeLength = EditorGUIUtility.TextContent("Fade Length|Distance over which trees will transition between 3D objects and billboards.");
            public GUIContent treeMaximumFullLODCount = EditorGUIUtility.TextContent("Max Mesh Trees|The maximum number of visible trees that will be represented as solid 3D meshes. Beyond this limit, trees will be replaced with billboards.");

            public GUIContent wavingGrassStrength = EditorGUIUtility.TextContent("Speed|The speed of the wind as it blows grass.");
            public GUIContent wavingGrassSpeed = EditorGUIUtility.TextContent("Size|The size of the “ripples” on grassy areas as the wind blows over them.");
            public GUIContent wavingGrassAmount = EditorGUIUtility.TextContent("Bending|The degree to which grass objects are bent over by the wind.");
            public GUIContent wavingGrassTint = EditorGUIUtility.TextContent("Grass Tint|Overall color tint applied to grass objects.");
        }
        static Styles styles;

        private const string kDisplayLightingKey = "TerrainInspector.Lighting.ShowSettings";

        static float PercentSlider(GUIContent content, float valueInPercent, float minVal, float maxVal)
        {
            EditorGUI.BeginChangeCheck();
            float v = EditorGUILayout.Slider(content, Mathf.Round(valueInPercent * 100f), minVal * 100f, maxVal * 100f);

            if (EditorGUI.EndChangeCheck())
            {
                return v / 100f;
            }
            return valueInPercent;
        }

        internal static PrefKey[] s_ToolKeys =
        {
            new PrefKey("Terrain/Raise Height", "f1"),
            new PrefKey("Terrain/Set Height", "f2"),
            new PrefKey("Terrain/Smooth Height", "f3"),
            new PrefKey("Terrain/Texture Paint", "f4"),
            new PrefKey("Terrain/Tree Brush", "f5"),
            new PrefKey("Terrain/Detail Brush", "f6")
        };
        internal static PrefKey s_PrevBrush = new PrefKey("Terrain/Previous Brush", ",");
        internal static PrefKey s_NextBrush = new PrefKey("Terrain/Next Brush", ".");
        internal static PrefKey s_PrevTexture = new PrefKey("Terrain/Previous Detail", "#,");
        internal static PrefKey s_NextTexture = new PrefKey("Terrain/Next Detail", "#.");
        // Source terrain
        Terrain m_Terrain;
        TerrainCollider m_TerrainCollider;

        Texture2D[]     m_SplatIcons = null;
        GUIContent[]    m_TreeContents = null;
        GUIContent[]    m_DetailContents = null;

        float m_TargetHeight;
        float m_Strength;
        int m_Size;
        float m_SplatAlpha;
        float m_DetailOpacity;
        float m_DetailStrength;

        int m_SelectedBrush = 0;
        int m_SelectedSplat = 0;
        int m_SelectedDetail = 0;

        const float kHeightmapBrushScale = 0.01F;
        const float kMinBrushStrength = (1.1F / ushort.MaxValue) / kHeightmapBrushScale;
        Brush m_CachedBrush;

        // TODO: Make an option for letting the user add textures to the brush list.
        static Texture2D[]  s_BrushTextures = null;

        static int s_TerrainEditorHash = "TerrainEditor".GetHashCode();

        // The instance ID of the active inspector.
        // It's defined as the last inspector that had one of its terrain tools selected.
        // If a terrain inspector is the only one when created, it also becomes active.
        static int s_activeTerrainInspector = 0;

        List<ReflectionProbeBlendInfo> m_BlendInfoList = new List<ReflectionProbeBlendInfo>();

        private AnimBool m_ShowBuiltinSpecularSettings = new AnimBool();
        private AnimBool m_ShowCustomMaterialSettings = new AnimBool();
        private AnimBool m_ShowReflectionProbesGUI = new AnimBool();

        bool m_LODTreePrototypePresent = false;

        private LightingSettingsInspector m_Lighting;

        void CheckKeys()
        {
            // If there is an active inspector, hot keys are exclusive to it.
            if (s_activeTerrainInspector != 0 && s_activeTerrainInspector != GetInstanceID())
                return;

            for (int i = 0; i < s_ToolKeys.Length; i++)
            {
                if (s_ToolKeys[i].activated)
                {
                    selectedTool = (TerrainTool)i;
                    Repaint();
                    Event.current.Use();
                }
            }

            if (s_PrevBrush.activated)
            {
                m_SelectedBrush--;
                if (m_SelectedBrush < 0)
                    m_SelectedBrush = s_BrushTextures.Length - 1;
                Repaint();
                Event.current.Use();
            }

            if (s_NextBrush.activated)
            {
                m_SelectedBrush++;
                if (m_SelectedBrush >= s_BrushTextures.Length)
                    m_SelectedBrush = 0;
                Repaint();
                Event.current.Use();
            }
            int delta = 0;
            if (s_NextTexture.activated)
                delta = 1;
            if (s_PrevTexture.activated)
                delta = -1;

            if (delta != 0)
            {
                switch (selectedTool)
                {
                    case TerrainTool.PaintDetail:
                        m_SelectedDetail = (int)Mathf.Repeat(m_SelectedDetail + delta, m_Terrain.terrainData.detailPrototypes.Length);
                        Event.current.Use();
                        Repaint();
                        break;
                    case TerrainTool.PlaceTree:
                        if (TreePainter.selectedTree >= 0)
                            TreePainter.selectedTree = (int)Mathf.Repeat(TreePainter.selectedTree + delta, m_TreeContents.Length);
                        else if (delta == -1 && m_TreeContents.Length > 0)
                            TreePainter.selectedTree = m_TreeContents.Length - 1;
                        else if (delta == 1 && m_TreeContents.Length > 0)
                            TreePainter.selectedTree = 0;
                        Event.current.Use();
                        Repaint();
                        break;
                    case TerrainTool.PaintTexture:
                        m_SelectedSplat = (int)Mathf.Repeat(m_SelectedSplat + delta, m_Terrain.terrainData.splatPrototypes.Length);
                        Event.current.Use();
                        Repaint();
                        break;
                }
            }
        }

        void LoadBrushIcons()
        {
            // Load the textures;
            ArrayList arr = new ArrayList();
            int idx = 1;
            Texture t = null;

            // Load brushes from editor resources
            do
            {
                t = (Texture2D)EditorGUIUtility.Load(EditorResourcesUtility.brushesPath + "builtin_brush_" + idx + ".png");
                if (t) arr.Add(t);
                idx++;
            }
            while (t);

            // Load user created brushes from the Assets/Gizmos folder
            idx = 0;
            do
            {
                t = EditorGUIUtility.FindTexture("brush_" + idx + ".png");
                if (t) arr.Add(t);
                idx++;
            }
            while (t);

            s_BrushTextures = arr.ToArray(typeof(Texture2D)) as Texture2D[];
        }

        void Initialize()
        {
            m_Terrain = target as Terrain;
            // Load brushes
            if (s_BrushTextures == null)
                LoadBrushIcons();
        }

        void LoadInspectorSettings()
        {
            m_TargetHeight = EditorPrefs.GetFloat("TerrainBrushTargetHeight", 0.2f);
            m_Strength = EditorPrefs.GetFloat("TerrainBrushStrength", 0.5f);
            m_Size = EditorPrefs.GetInt("TerrainBrushSize", 25);
            m_SplatAlpha = EditorPrefs.GetFloat("TerrainBrushSplatAlpha", 1.0f);
            m_DetailOpacity = EditorPrefs.GetFloat("TerrainDetailOpacity", 1.0f);
            m_DetailStrength = EditorPrefs.GetFloat("TerrainDetailStrength", 0.8f);

            m_SelectedBrush = EditorPrefs.GetInt("TerrainSelectedBrush", 0);
            m_SelectedSplat = EditorPrefs.GetInt("TerrainSelectedSplat", 0);
            m_SelectedDetail = EditorPrefs.GetInt("TerrainSelectedDetail", 0);
        }

        void SaveInspectorSettings()
        {
            EditorPrefs.SetInt("TerrainSelectedDetail", m_SelectedDetail);
            EditorPrefs.SetInt("TerrainSelectedSplat", m_SelectedSplat);
            EditorPrefs.SetInt("TerrainSelectedBrush", m_SelectedBrush);

            EditorPrefs.SetFloat("TerrainDetailStrength", m_DetailStrength);
            EditorPrefs.SetFloat("TerrainDetailOpacity", m_DetailOpacity);
            EditorPrefs.SetFloat("TerrainBrushSplatAlpha", m_SplatAlpha);
            EditorPrefs.SetInt("TerrainBrushSize", m_Size);
            EditorPrefs.SetFloat("TerrainBrushStrength", m_Strength);
            EditorPrefs.SetFloat("TerrainBrushTargetHeight", m_TargetHeight);
        }

        public void OnEnable()
        {
            // Acquire active inspector ownership if there is no other inspector active.
            if (s_activeTerrainInspector == 0)
                s_activeTerrainInspector = GetInstanceID();

            m_ShowBuiltinSpecularSettings.valueChanged.AddListener(Repaint);
            m_ShowCustomMaterialSettings.valueChanged.AddListener(Repaint);
            m_ShowReflectionProbesGUI.valueChanged.AddListener(Repaint);

            var terrain = target as Terrain;
            if (terrain != null)
            {
                m_ShowBuiltinSpecularSettings.value = terrain.materialType == Terrain.MaterialType.BuiltInLegacySpecular;
                m_ShowCustomMaterialSettings.value = terrain.materialType == Terrain.MaterialType.Custom;
                m_ShowReflectionProbesGUI.value = terrain.materialType == Terrain.MaterialType.BuiltInStandard || terrain.materialType == Terrain.MaterialType.Custom;
            }

            LoadInspectorSettings();

            SceneView.onPreSceneGUIDelegate += OnPreSceneGUICallback;
            SceneView.onSceneGUIDelegate += OnSceneGUICallback;

            InitializeLightingFields();
        }

        public void OnDisable()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUICallback;
            SceneView.onPreSceneGUIDelegate -= OnPreSceneGUICallback;

            SaveInspectorSettings();

            m_ShowReflectionProbesGUI.valueChanged.RemoveListener(Repaint);
            m_ShowCustomMaterialSettings.valueChanged.RemoveListener(Repaint);
            m_ShowBuiltinSpecularSettings.valueChanged.RemoveListener(Repaint);

            if (m_CachedBrush != null)
                m_CachedBrush.Dispose();

            // Return active inspector ownership.
            if (s_activeTerrainInspector == GetInstanceID())
                s_activeTerrainInspector = 0;
        }

        SavedInt m_SelectedTool = new SavedInt("TerrainSelectedTool", (int)TerrainTool.PaintHeight);
        TerrainTool selectedTool
        {
            get
            {
                if (Tools.current == Tool.None && GetInstanceID() == s_activeTerrainInspector)
                    return (TerrainTool)m_SelectedTool.value;
                return TerrainTool.None;
            }
            set
            {
                if (value != TerrainTool.None)
                    Tools.current = Tool.None;
                m_SelectedTool.value = (int)value;
                s_activeTerrainInspector = GetInstanceID();
            }
        }

        public void MenuButton(GUIContent title, string menuName, int userData)
        {
            GUIContent t = new GUIContent(title.text, styles.settingsIcon, title.tooltip);
            Rect r = GUILayoutUtility.GetRect(t, styles.largeSquare);
            if (GUI.Button(r, t, styles.largeSquare))
            {
                MenuCommand context = new MenuCommand(m_Terrain, userData);
                EditorUtility.DisplayPopupMenu(new Rect(r.x, r.y, 0, 0), menuName, context);
            }
        }

        public static int AspectSelectionGrid(int selected, Texture[] textures, int approxSize, GUIStyle style, string emptyString, out bool doubleClick)
        {
            GUILayout.BeginVertical("box", GUILayout.MinHeight(10));
            int retval = 0;

            doubleClick = false;

            if (textures.Length != 0)
            {
                float columns = (EditorGUIUtility.currentViewWidth - 20) / approxSize;
                int rows = (int)Mathf.Ceil(textures.Length / columns);
                Rect r = GUILayoutUtility.GetAspectRect(columns / rows);

                Event evt = Event.current;
                if (evt.type == EventType.MouseDown && evt.clickCount == 2 && r.Contains(evt.mousePosition))
                {
                    doubleClick = true;
                    evt.Use();
                }

                retval = GUI.SelectionGrid(r, System.Math.Min(selected, textures.Length - 1), textures, Mathf.RoundToInt(EditorGUIUtility.currentViewWidth - 20) / approxSize, style);
            }
            else
            {
                GUILayout.Label(emptyString);
            }

            GUILayout.EndVertical();
            return retval;
        }

        static Rect GetBrushAspectRect(int elementCount, int approxSize, int extraLineHeight, out int xCount)
        {
            xCount = (int)Mathf.Ceil((EditorGUIUtility.currentViewWidth - 20) / approxSize);
            int yCount = elementCount / xCount;
            if (elementCount % xCount != 0)
                yCount++;
            Rect r1 = GUILayoutUtility.GetAspectRect(xCount / (float)yCount);
            Rect r2 = GUILayoutUtility.GetRect(10, extraLineHeight * yCount);
            r1.height += r2.height;
            return r1;
        }

        public static int AspectSelectionGridImageAndText(int selected, GUIContent[] textures, int approxSize, GUIStyle style, string emptyString, out bool doubleClick)
        {
            EditorGUILayout.BeginVertical(GUIContent.none, EditorStyles.helpBox, GUILayout.MinHeight(10));
            int retval = 0;

            doubleClick = false;

            if (textures.Length != 0)
            {
                int xCount = 0;
                Rect rect = GetBrushAspectRect(textures.Length, approxSize, 12, out xCount);

                Event evt = Event.current;
                if (evt.type == EventType.MouseDown && evt.clickCount == 2 && rect.Contains(evt.mousePosition))
                {
                    doubleClick = true;
                    evt.Use();
                }
                retval = GUI.SelectionGrid(rect, System.Math.Min(selected, textures.Length - 1), textures, xCount, style);
            }
            else
            {
                GUILayout.Label(emptyString);
            }

            GUILayout.EndVertical();
            return retval;
        }

        void LoadSplatIcons()
        {
            // Use asset preview for splat textures so that alpha channel is not shown (used as Smoothness)
            SplatPrototype[] splats = m_Terrain.terrainData.splatPrototypes;

            m_SplatIcons = new Texture2D[splats.Length];
            for (int i = 0; i < m_SplatIcons.Length; ++i)
                m_SplatIcons[i] = AssetPreview.GetAssetPreview(splats[i].texture) ?? splats[i].texture;
        }

        void LoadTreeIcons()
        {
            // Locate the proto types asset preview textures
            TreePrototype[] trees = m_Terrain.terrainData.treePrototypes;

            m_TreeContents = new GUIContent[trees.Length];
            for (int i = 0; i < m_TreeContents.Length; i++)
            {
                m_TreeContents[i] = new GUIContent();
                Texture tex = AssetPreview.GetAssetPreview(trees[i].prefab);
                if (tex != null)
                    m_TreeContents[i].image = tex;

                if (trees[i].prefab != null)
                {
                    m_TreeContents[i].text = trees[i].prefab.name;
                    m_TreeContents[i].tooltip = m_TreeContents[i].text;
                }
                else
                    m_TreeContents[i].text = "Missing";
            }
        }

        void LoadDetailIcons()
        {
            // Locate the proto types asset preview textures
            DetailPrototype[] prototypes = m_Terrain.terrainData.detailPrototypes;
            m_DetailContents = new GUIContent[prototypes.Length];
            for (int i = 0; i < m_DetailContents.Length; i++)
            {
                m_DetailContents[i] = new GUIContent();

                if (prototypes[i].usePrototypeMesh)
                {
                    Texture tex = AssetPreview.GetAssetPreview(prototypes[i].prototype);
                    if (tex != null)
                        m_DetailContents[i].image = tex;

                    if (prototypes[i].prototype != null)
                        m_DetailContents[i].text = prototypes[i].prototype.name;
                    else
                        m_DetailContents[i].text = "Missing";
                }
                else
                {
                    Texture tex = prototypes[i].prototypeTexture;
                    if (tex != null)
                        m_DetailContents[i].image = tex;
                    if (tex != null)
                        m_DetailContents[i].text = tex.name;
                    else
                        m_DetailContents[i].text = "Missing";
                }
            }
        }

        public void ShowTrees()
        {
            LoadTreeIcons();

            // Tree picker
            GUI.changed = false;

            ShowUpgradeTreePrototypeScaleUI();

            GUILayout.Label(styles.trees, EditorStyles.boldLabel);
            bool doubleClick;
            TreePainter.selectedTree = AspectSelectionGridImageAndText(TreePainter.selectedTree, m_TreeContents, 64, styles.gridListText, "No trees defined", out doubleClick);

            if (TreePainter.selectedTree >= m_TreeContents.Length)
                TreePainter.selectedTree = -1;

            if (doubleClick)
            {
                TerrainTreeContextMenus.EditTree(new MenuCommand(m_Terrain, TreePainter.selectedTree));
                GUIUtility.ExitGUI();
            }

            GUILayout.BeginHorizontal();
            ShowMassPlaceTrees();
            GUILayout.FlexibleSpace();
            MenuButton(styles.editTrees, "CONTEXT/TerrainEngineTrees", TreePainter.selectedTree);
            ShowRefreshPrototypes();
            GUILayout.EndHorizontal();

            if (TreePainter.selectedTree == -1)
                return;

            GUILayout.Label(styles.settings, EditorStyles.boldLabel);
            // Placement distance
            TreePainter.brushSize = EditorGUILayout.Slider(styles.brushSize, TreePainter.brushSize, 1, 100); // former string formatting: ""
            float oldDens = (3.3f - TreePainter.spacing) / 3f;
            float newDens = PercentSlider(styles.treeDensity, oldDens, .1f, 1);
            // Only set spacing when value actually changes. Otherwise
            // it will lose precision because we're constantly doing math
            // back and forth with it.
            if (newDens != oldDens)
                TreePainter.spacing = (1.1f - newDens) * 3f;

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label(styles.treeHeight, GUILayout.Width(EditorGUIUtility.labelWidth - 6));
            GUILayout.Label(styles.treeHeightRandomLabel, GUILayout.ExpandWidth(false));
            TreePainter.allowHeightVar = GUILayout.Toggle(TreePainter.allowHeightVar, styles.treeHeightRandomToggle, GUILayout.ExpandWidth(false));
            if (TreePainter.allowHeightVar)
            {
                EditorGUI.BeginChangeCheck();
                float min = TreePainter.treeHeight * (1.0f - TreePainter.treeHeightVariation);
                float max = TreePainter.treeHeight * (1.0f + TreePainter.treeHeightVariation);
                EditorGUILayout.MinMaxSlider(ref min, ref max, 0.01f, 2.0f);
                if (EditorGUI.EndChangeCheck())
                {
                    TreePainter.treeHeight = (min + max) * 0.5f;
                    TreePainter.treeHeightVariation = (max - min) / (min + max);
                }
            }
            else
            {
                TreePainter.treeHeight = EditorGUILayout.Slider(TreePainter.treeHeight, 0.01f, 2.0f);
                TreePainter.treeHeightVariation = 0.0f;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            TreePainter.lockWidthToHeight = EditorGUILayout.Toggle(styles.lockWidth, TreePainter.lockWidthToHeight);
            if (TreePainter.lockWidthToHeight)
            {
                TreePainter.treeWidth = TreePainter.treeHeight;
                TreePainter.treeWidthVariation = TreePainter.treeHeightVariation;
                TreePainter.allowWidthVar = TreePainter.allowHeightVar;
            }

            GUILayout.Space(5);

            using (new EditorGUI.DisabledScope(TreePainter.lockWidthToHeight))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(styles.treeWidth, GUILayout.Width(EditorGUIUtility.labelWidth - 6));
                GUILayout.Label(styles.treeWidthRandomLabel, GUILayout.ExpandWidth(false));
                TreePainter.allowWidthVar = GUILayout.Toggle(TreePainter.allowWidthVar, styles.treeWidthRandomToggle, GUILayout.ExpandWidth(false));
                if (TreePainter.allowWidthVar)
                {
                    EditorGUI.BeginChangeCheck();
                    float min = TreePainter.treeWidth * (1.0f - TreePainter.treeWidthVariation);
                    float max = TreePainter.treeWidth * (1.0f + TreePainter.treeWidthVariation);
                    EditorGUILayout.MinMaxSlider(ref min, ref max, 0.01f, 2.0f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        TreePainter.treeWidth = (min + max) * 0.5f;
                        TreePainter.treeWidthVariation = (max - min) / (min + max);
                    }
                }
                else
                {
                    TreePainter.treeWidth = EditorGUILayout.Slider(TreePainter.treeWidth, 0.01f, 2.0f);
                    TreePainter.treeWidthVariation = 0.0f;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(5);

            if (TerrainEditorUtility.IsLODTreePrototype(m_Terrain.terrainData.treePrototypes[TreePainter.selectedTree].m_Prefab))
                TreePainter.randomRotation = EditorGUILayout.Toggle(styles.treeRotation, TreePainter.randomRotation);
            else
                TreePainter.treeColorAdjustment = EditorGUILayout.Slider(styles.treeColorVar, TreePainter.treeColorAdjustment, 0, 1);

            GameObject prefab = m_Terrain.terrainData.treePrototypes[TreePainter.selectedTree].m_Prefab;
            if (prefab != null)
            {
                StaticEditorFlags staticEditorFlags = GameObjectUtility.GetStaticEditorFlags(prefab);
                bool lightmapStatic = (staticEditorFlags & StaticEditorFlags.LightmapStatic) != 0;
                using (new EditorGUI.DisabledScope(true))   // Always disabled, because we don't want to edit the prefab.
                    lightmapStatic = EditorGUILayout.Toggle(styles.treeLightmapStatic, lightmapStatic);
            }
        }

        public void ShowDetails()
        {
            LoadDetailIcons();
            ShowBrushes();
            // Brush size

            // Detail picker
            GUI.changed = false;

            GUILayout.Label(styles.details, EditorStyles.boldLabel);
            bool doubleClick;
            m_SelectedDetail = AspectSelectionGridImageAndText(m_SelectedDetail, m_DetailContents, 64, styles.gridListText, "No Detail Objects defined", out doubleClick);
            if (doubleClick)
            {
                TerrainDetailContextMenus.EditDetail(new MenuCommand(m_Terrain, m_SelectedDetail));
                GUIUtility.ExitGUI();
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            MenuButton(styles.editDetails, "CONTEXT/TerrainEngineDetails", m_SelectedDetail);
            ShowRefreshPrototypes();
            GUILayout.EndHorizontal();

            GUILayout.Label(styles.settings, EditorStyles.boldLabel);

            // Brush size
            m_Size = Mathf.RoundToInt(EditorGUILayout.Slider(styles.brushSize, m_Size, 1, 100)); // former string formatting: ""
            m_DetailOpacity = EditorGUILayout.Slider(styles.opacity, m_DetailOpacity, 0, 1); // former string formatting: "%"

            // Strength
            m_DetailStrength = EditorGUILayout.Slider(styles.detailTargetStrength, m_DetailStrength, 0, 1); // former string formatting: "%"
            m_DetailStrength = Mathf.Round(m_DetailStrength * 16.0f) / 16.0f;
        }

        public void ShowSettings()
        {
            TerrainData terrainData = m_Terrain.terrainData;

            EditorGUI.BeginChangeCheck();

            GUILayout.Label("Base Terrain", EditorStyles.boldLabel);
            m_Terrain.drawHeightmap = EditorGUILayout.Toggle(styles.drawTerrain, m_Terrain.drawHeightmap);
            m_Terrain.heightmapPixelError = EditorGUILayout.Slider(styles.pixelError, m_Terrain.heightmapPixelError, 1, 200); // former string formatting: ""
            m_Terrain.basemapDistance = EditorGUILayout.Slider(styles.baseMapDist, m_Terrain.basemapDistance, 0, 2000); // former string formatting: ""
            m_Terrain.castShadows = EditorGUILayout.Toggle(styles.castShadows, m_Terrain.castShadows);

            m_Terrain.materialType = (Terrain.MaterialType)EditorGUILayout.EnumPopup(styles.material, m_Terrain.materialType);

            // Make sure we don't reference any custom material if we are to use a built-in shader.
            if (m_Terrain.materialType != Terrain.MaterialType.Custom)
                m_Terrain.materialTemplate = null;

            m_ShowBuiltinSpecularSettings.target = m_Terrain.materialType == Terrain.MaterialType.BuiltInLegacySpecular;
            m_ShowCustomMaterialSettings.target = m_Terrain.materialType == Terrain.MaterialType.Custom;
            m_ShowReflectionProbesGUI.target = m_Terrain.materialType == Terrain.MaterialType.BuiltInStandard || m_Terrain.materialType == Terrain.MaterialType.Custom;

            if (EditorGUILayout.BeginFadeGroup(m_ShowBuiltinSpecularSettings.faded))
            {
                EditorGUI.indentLevel++;
                m_Terrain.legacySpecular = EditorGUILayout.ColorField("Specular Color", m_Terrain.legacySpecular);
                m_Terrain.legacyShininess = EditorGUILayout.Slider("Shininess", m_Terrain.legacyShininess, 0.03f, 1.0f);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();

            if (EditorGUILayout.BeginFadeGroup(m_ShowCustomMaterialSettings.faded))
            {
                EditorGUI.indentLevel++;
                m_Terrain.materialTemplate = EditorGUILayout.ObjectField("Custom Material", m_Terrain.materialTemplate, typeof(Material), false) as Material;

                // Warn if shader needs tangent basis
                if (m_Terrain.materialTemplate != null)
                {
                    Shader s = m_Terrain.materialTemplate.shader;
                    if (ShaderUtil.HasTangentChannel(s))
                    {
                        GUIContent c = EditorGUIUtility.TextContent("Can't use materials with shaders which need tangent geometry on terrain, use shaders in Nature/Terrain instead.");
                        EditorGUILayout.HelpBox(c.text, MessageType.Warning, false);
                    }
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();

            if (EditorGUILayout.BeginFadeGroup(m_ShowReflectionProbesGUI.faded))
            {
                m_Terrain.reflectionProbeUsage = (ReflectionProbeUsage)EditorGUILayout.EnumPopup(styles.reflectionProbes, m_Terrain.reflectionProbeUsage);
                if (m_Terrain.reflectionProbeUsage != ReflectionProbeUsage.Off)
                {
                    EditorGUI.indentLevel++;
                    m_Terrain.GetClosestReflectionProbes(m_BlendInfoList);
                    RendererEditorBase.Probes.ShowClosestReflectionProbes(m_BlendInfoList);
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUILayout.EndFadeGroup();

            terrainData.thickness = EditorGUILayout.FloatField(styles.thickness, terrainData.thickness);

            GUILayout.Label("Tree & Detail Objects", EditorStyles.boldLabel);
            m_Terrain.drawTreesAndFoliage = EditorGUILayout.Toggle(styles.drawTrees, m_Terrain.drawTreesAndFoliage);
            m_Terrain.bakeLightProbesForTrees = EditorGUILayout.Toggle(styles.bakeLightProbesForTrees, m_Terrain.bakeLightProbesForTrees);

            if (m_Terrain.bakeLightProbesForTrees)
                EditorGUILayout.HelpBox("GPU instancing is disabled for trees if light probes are used. Performance may be affected.", MessageType.Info);

            m_Terrain.detailObjectDistance = EditorGUILayout.Slider(styles.detailObjectDistance, m_Terrain.detailObjectDistance, 0, 250); // former string formatting: ""
            m_Terrain.collectDetailPatches = EditorGUILayout.Toggle(styles.collectDetailPatches, m_Terrain.collectDetailPatches);
            m_Terrain.detailObjectDensity = EditorGUILayout.Slider(styles.detailObjectDensity, m_Terrain.detailObjectDensity, 0.0f, 1.0f);
            m_Terrain.treeDistance = EditorGUILayout.Slider(styles.treeDistance, m_Terrain.treeDistance, 0, 2000); // former string formatting: ""
            m_Terrain.treeBillboardDistance = EditorGUILayout.Slider(styles.treeBillboardDistance, m_Terrain.treeBillboardDistance, 5, 2000); // former string formatting: ""
            m_Terrain.treeCrossFadeLength = EditorGUILayout.Slider(styles.treeCrossFadeLength, m_Terrain.treeCrossFadeLength, 0, 200); // former string formatting: ""
            m_Terrain.treeMaximumFullLODCount = EditorGUILayout.IntSlider(styles.treeMaximumFullLODCount, m_Terrain.treeMaximumFullLODCount, 0, 10000);

            // Only do this check once per frame.
            if (Event.current.type == EventType.Layout)
            {
                m_LODTreePrototypePresent = false;
                for (int i = 0; i < terrainData.treePrototypes.Length; ++i)
                {
                    if (TerrainEditorUtility.IsLODTreePrototype(terrainData.treePrototypes[i].prefab))
                    {
                        m_LODTreePrototypePresent = true;
                        break;
                    }
                }
            }

            if (m_LODTreePrototypePresent)
                EditorGUILayout.HelpBox("Tree Distance, Billboard Start, Fade Length and Max Mesh Trees have no effect on SpeedTree trees. Please use the LOD Group component on the tree prefab to control LOD settings.", MessageType.Info);

            if (EditorGUI.EndChangeCheck())
            {
                EditorApplication.SetSceneRepaintDirty();
                EditorUtility.SetDirty(m_Terrain);

                if (!EditorApplication.isPlaying)
                    SceneManagement.EditorSceneManager.MarkSceneDirty(m_Terrain.gameObject.scene);
            }

            EditorGUI.BeginChangeCheck();

            GUILayout.Label("Wind Settings for Grass", EditorStyles.boldLabel);
            float wavingGrassStrength = EditorGUILayout.Slider(styles.wavingGrassStrength, terrainData.wavingGrassStrength, 0, 1); // former string formatting: "%"
            float wavingGrassSpeed = EditorGUILayout.Slider(styles.wavingGrassSpeed, terrainData.wavingGrassSpeed, 0, 1); // former string formatting: "%"
            float wavingGrassAmount = EditorGUILayout.Slider(styles.wavingGrassAmount, terrainData.wavingGrassAmount, 0, 1); // former string formatting: "%"
            Color wavingGrassTint = EditorGUILayout.ColorField(styles.wavingGrassTint, terrainData.wavingGrassTint);

            if (EditorGUI.EndChangeCheck())
            {
                // Apply terrain settings only when something has changed. Otherwise we needlessly dirty the object and it will show up as modified.
                terrainData.wavingGrassStrength = wavingGrassStrength;
                terrainData.wavingGrassSpeed = wavingGrassSpeed;
                terrainData.wavingGrassAmount = wavingGrassAmount;
                terrainData.wavingGrassTint = wavingGrassTint;

                // In cases where terrain data is embedded in the scene (i.e. it's not an asset),
                // we need to dirty the scene if terrainData has changed.
                if (!EditorUtility.IsPersistent(terrainData) && !EditorApplication.isPlaying)
                    SceneManagement.EditorSceneManager.MarkSceneDirty(m_Terrain.gameObject.scene);
            }

            ShowResolution();
            ShowHeightmaps();
        }

        public void ShowRaiseHeight()
        {
            ShowBrushes();
            GUILayout.Label(styles.settings, EditorStyles.boldLabel);
            ShowBrushSettings();
        }

        public void ShowSmoothHeight()
        {
            ShowBrushes();
            GUILayout.Label(styles.settings, EditorStyles.boldLabel);
            ShowBrushSettings();
        }

        public void ShowTextures()
        {
            LoadSplatIcons();
            ShowBrushes();

            GUILayout.Label(styles.textures, EditorStyles.boldLabel);
            GUI.changed = false;
            bool doubleClick;
            m_SelectedSplat = AspectSelectionGrid(m_SelectedSplat, m_SplatIcons, 64, styles.gridList, "No terrain textures defined.", out doubleClick);
            if (doubleClick)
            {
                TerrainSplatContextMenus.EditSplat(new MenuCommand(m_Terrain, m_SelectedSplat));
                GUIUtility.ExitGUI();
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            MenuButton(styles.editTextures, "CONTEXT/TerrainEngineSplats", m_SelectedSplat);
            GUILayout.EndHorizontal();

            // Brush size
            GUILayout.Label(styles.settings, EditorStyles.boldLabel);
            ShowBrushSettings();
            m_SplatAlpha = EditorGUILayout.Slider(styles.targetStrength, m_SplatAlpha, 0.0F, 1.0F); // former string formatting: "%"
        }

        public void ShowBrushes()
        {
            GUILayout.Label(styles.brushes, EditorStyles.boldLabel);
            bool dummy;
            m_SelectedBrush = AspectSelectionGrid(m_SelectedBrush, s_BrushTextures, 32, styles.gridList, "No brushes defined.", out dummy);
        }

        public void ShowHeightmaps()
        {
            GUILayout.Label(styles.heightmap, EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(styles.importRaw))
            {
                TerrainMenus.ImportRaw();
            }

            if (GUILayout.Button(styles.exportRaw))
            {
                TerrainMenus.ExportHeightmapRaw();
            }
            GUILayout.EndHorizontal();
        }

        public void ShowResolution()
        {
            GUILayout.Label("Resolution", EditorStyles.boldLabel);

            float terrainWidth = m_Terrain.terrainData.size.x;
            float terrainHeight = m_Terrain.terrainData.size.y;
            float terrainLength = m_Terrain.terrainData.size.z;
            int heightmapResolution = m_Terrain.terrainData.heightmapResolution;
            int detailResolution = m_Terrain.terrainData.detailResolution;
            int detailResolutionPerPatch = m_Terrain.terrainData.detailResolutionPerPatch;
            int controlTextureResolution = m_Terrain.terrainData.alphamapResolution;
            int baseTextureResolution = m_Terrain.terrainData.baseMapResolution;

            EditorGUI.BeginChangeCheck();

            const int kMaxTerrainSize = 100000;
            const int kMaxTerrainHeight = 10000;
            terrainWidth = EditorGUILayout.DelayedFloatField(EditorGUIUtility.TextContent("Terrain Width|Size of the terrain object in its X axis (in world units)."), terrainWidth);
            if (terrainWidth <= 0) terrainWidth = 1;
            if (terrainWidth > kMaxTerrainSize) terrainWidth = kMaxTerrainSize;

            terrainLength = EditorGUILayout.DelayedFloatField(EditorGUIUtility.TextContent("Terrain Length|Size of the terrain object in its Z axis (in world units)."), terrainLength);
            if (terrainLength <= 0) terrainLength = 1;
            if (terrainLength > kMaxTerrainSize) terrainLength = kMaxTerrainSize;

            terrainHeight = EditorGUILayout.DelayedFloatField(EditorGUIUtility.TextContent("Terrain Height|Difference in Y coordinate between the lowest possible heightmap value and the highest (in world units)."), terrainHeight);
            if (terrainHeight <= 0) terrainHeight = 1;
            if (terrainHeight > kMaxTerrainHeight) terrainHeight = kMaxTerrainHeight;

            heightmapResolution = EditorGUILayout.DelayedIntField(EditorGUIUtility.TextContent("Heightmap Resolution|Pixel resolution of the terrain’s heightmap (should be a power of two plus one, eg, 513 = 512 + 1)."), heightmapResolution);
            const int kMinimumResolution = 33; // 33 is the minimum that GetAdjustedSize will allow
            const int kMaximumResolution = 4097; // if you want to change the maximum value, also change it in SetResolutionWizard
            heightmapResolution = Mathf.Clamp(heightmapResolution, kMinimumResolution, kMaximumResolution);
            heightmapResolution = m_Terrain.terrainData.GetAdjustedSize(heightmapResolution);

            detailResolution = EditorGUILayout.DelayedIntField(EditorGUIUtility.TextContent("Detail Resolution|Resolution of the map that determines the separate patches of details/grass. Higher resolution gives smaller and more detailed patches."), detailResolution);
            detailResolution = Mathf.Clamp(detailResolution, 0, 4048);

            detailResolutionPerPatch = EditorGUILayout.DelayedIntField(EditorGUIUtility.TextContent("Detail Resolution Per Patch|Length/width of the square of patches renderered with a single draw call."), detailResolutionPerPatch);
            detailResolutionPerPatch = Mathf.Clamp(detailResolutionPerPatch, 8, 128);

            controlTextureResolution = EditorGUILayout.DelayedIntField(EditorGUIUtility.TextContent("Control Texture Resolution|Resolution of the “splatmap” that controls the blending of the different terrain textures."), controlTextureResolution);
            controlTextureResolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(controlTextureResolution), 16, 2048);

            baseTextureResolution = EditorGUILayout.DelayedIntField(EditorGUIUtility.TextContent("Base Texture Resolution|Resolution of the composite texture used on the terrain when viewed from a distance greater than the Basemap Distance."), baseTextureResolution);
            baseTextureResolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(baseTextureResolution), 16, 2048);

            if (EditorGUI.EndChangeCheck())
            {
                ArrayList undoObjects = new ArrayList();
                undoObjects.Add(m_Terrain.terrainData);
                undoObjects.AddRange(m_Terrain.terrainData.alphamapTextures);

                Undo.RegisterCompleteObjectUndo(undoObjects.ToArray(typeof(UnityEngine.Object)) as UnityEngine.Object[], "Set Resolution");

                if (m_Terrain.terrainData.heightmapResolution != heightmapResolution)
                    m_Terrain.terrainData.heightmapResolution = heightmapResolution;
                m_Terrain.terrainData.size = new Vector3(terrainWidth, terrainHeight, terrainLength);

                if (m_Terrain.terrainData.detailResolution != detailResolution || detailResolutionPerPatch != m_Terrain.terrainData.detailResolutionPerPatch)
                    ResizeDetailResolution(m_Terrain.terrainData, detailResolution, detailResolutionPerPatch);

                if (m_Terrain.terrainData.alphamapResolution != controlTextureResolution)
                    m_Terrain.terrainData.alphamapResolution = controlTextureResolution;

                if (m_Terrain.terrainData.baseMapResolution != baseTextureResolution)
                    m_Terrain.terrainData.baseMapResolution = baseTextureResolution;

                m_Terrain.Flush();
            }

            EditorGUILayout.HelpBox("Please note that modifying the resolution of the heightmap, detail map and control texture will clear their contents, respectively.", MessageType.Warning);
        }

        void ResizeDetailResolution(TerrainData terrainData, int resolution, int resolutionPerPatch)
        {
            if (resolution == terrainData.detailResolution)
            {
                var layers = new List<int[, ]>();
                for (int i = 0; i < terrainData.detailPrototypes.Length; i++)
                    layers.Add(terrainData.GetDetailLayer(0, 0, terrainData.detailWidth, terrainData.detailHeight, i));

                terrainData.SetDetailResolution(resolution, resolutionPerPatch);

                for (int i = 0; i < layers.Count; i++)
                    terrainData.SetDetailLayer(0, 0, i, layers[i]);
            }
            else
            {
                terrainData.SetDetailResolution(resolution, resolutionPerPatch);
            }
        }

        public void ShowUpgradeTreePrototypeScaleUI()
        {
            if (m_Terrain.terrainData != null && m_Terrain.terrainData.NeedUpgradeScaledTreePrototypes())
            {
                var msgContent = EditorGUIUtility.TempContent(
                        "Some of your prototypes have scaling values on the prefab. Since Unity 5.2 these scalings will be applied to terrain tree instances. Do you want to upgrade to this behaviour?",
                        EditorGUIUtility.GetHelpIcon(MessageType.Warning));
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label(msgContent, EditorStyles.wordWrappedLabel);
                GUILayout.Space(3);
                if (GUILayout.Button("Upgrade", GUILayout.ExpandWidth(false)))
                {
                    m_Terrain.terrainData.UpgradeScaledTreePrototype();
                    TerrainMenus.RefreshPrototypes();
                }
                GUILayout.Space(3);
                GUILayout.EndVertical();
            }
        }

        public void ShowRefreshPrototypes()
        {
            if (GUILayout.Button(styles.refresh))
            {
                TerrainMenus.RefreshPrototypes();
            }
        }

        public void ShowMassPlaceTrees()
        {
            using (new EditorGUI.DisabledScope(TreePainter.selectedTree == -1))
            {
                if (GUILayout.Button(styles.massPlaceTrees))
                {
                    TerrainMenus.MassPlaceTrees();
                }
            }
        }

        public void ShowBrushSettings()
        {
            m_Size = Mathf.RoundToInt(EditorGUILayout.Slider(styles.brushSize, m_Size, 1, 100));
            m_Strength = PercentSlider(styles.opacity, m_Strength, kMinBrushStrength, 1); // former string formatting: "0.0%"
        }

        public void ShowSetHeight()
        {
            ShowBrushes();
            GUILayout.Label(styles.settings, EditorStyles.boldLabel);
            ShowBrushSettings();

            GUILayout.BeginHorizontal();

            GUI.changed = false;
            float val = (m_TargetHeight * m_Terrain.terrainData.size.y);
            val = EditorGUILayout.Slider(styles.height, val, 0, m_Terrain.terrainData.size.y);
            if (GUI.changed)
                m_TargetHeight = val / m_Terrain.terrainData.size.y;

            if (GUILayout.Button(styles.flatten, GUILayout.ExpandWidth(false)))
            {
                Undo.RegisterCompleteObjectUndo(m_Terrain.terrainData, "Flatten Heightmap");
                HeightmapFilters.Flatten(m_Terrain.terrainData, m_TargetHeight);
            }

            GUILayout.EndHorizontal();
        }

        public void InitializeLightingFields()
        {
            m_Lighting = new LightingSettingsInspector(serializedObject);
            m_Lighting.showSettings = EditorPrefs.GetBool(kDisplayLightingKey, false);
        }

        public void RenderLightingFields()
        {
            bool oldShowLighting = m_Lighting.showSettings;

            if (m_Lighting.Begin())
            {
                m_Lighting.RenderTerrainSettings();
            }
            m_Lighting.End();

            if (m_Lighting.showSettings != oldShowLighting)
                EditorPrefs.SetBool(kDisplayLightingKey, m_Lighting.showSettings);
        }

        void OnInspectorUpdate()
        {
            if (AssetPreview.HasAnyNewPreviewTexturesAvailable())
                Repaint();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            Initialize();

            if (styles == null)
            {
                styles = new Styles();
            }

            if (!m_Terrain.terrainData)
            {
                GUI.enabled = false;
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Toolbar(-1, styles.toolIcons, styles.command);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUI.enabled = true;
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("Terrain Asset Missing");
                m_Terrain.terrainData = EditorGUILayout.ObjectField("Assign:", m_Terrain.terrainData, typeof(TerrainData), false) as TerrainData;
                GUILayout.EndVertical();
                return;
            }

            if (Event.current.type == EventType.Layout)
                m_TerrainCollider = m_Terrain.gameObject.GetComponent<TerrainCollider>();

            if (m_TerrainCollider && m_TerrainCollider.terrainData != m_Terrain.terrainData)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label(styles.mismatchedTerrainData, EditorStyles.wordWrappedLabel);
                GUILayout.Space(3);
                if (GUILayout.Button(styles.assign, GUILayout.ExpandWidth(false)))
                {
                    Undo.RecordObject(m_TerrainCollider, "Assign TerrainData");
                    m_TerrainCollider.terrainData = m_Terrain.terrainData;
                }
                GUILayout.Space(3);
                GUILayout.EndVertical();
            }

            // Show the master tool selector
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.changed = false;
            int tool = (int)selectedTool;
            int newlySelectedTool = GUILayout.Toolbar(tool, styles.toolIcons, styles.command);

            if (newlySelectedTool != tool)
            {
                selectedTool = (TerrainTool)newlySelectedTool;

                // Need to repaint other terrain inspectors as their previously selected tool is now deselected.
                InspectorWindow.RepaintAllInspectors();

                if (Toolbar.get != null)
                    Toolbar.get.Repaint();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            CheckKeys();

            GUILayout.BeginVertical(EditorStyles.helpBox);
            if (tool >= 0 && tool < styles.toolIcons.Length)
            {
                GUILayout.Label(styles.toolNames[(int)tool].text);
                GUILayout.Label(styles.toolNames[(int)tool].tooltip, EditorStyles.wordWrappedMiniLabel);
            }
            else
            {
                // TODO: Fix these somehow sensibly
                GUILayout.Label("No tool selected");
                GUILayout.Label("Please select a tool", EditorStyles.wordWrappedMiniLabel);
            }
            GUILayout.EndVertical();

            switch ((TerrainTool)tool)
            {
                case TerrainTool.PaintHeight:
                    ShowRaiseHeight();
                    break;
                case TerrainTool.SetHeight:
                    ShowSetHeight();
                    break;
                case TerrainTool.SmoothHeight:
                    ShowSmoothHeight();
                    break;
                case TerrainTool.PaintTexture:
                    ShowTextures();
                    break;
                case TerrainTool.PlaceTree:
                    ShowTrees();
                    break;
                case TerrainTool.PaintDetail:
                    ShowDetails();
                    break;
                case TerrainTool.TerrainSettings:
                    ShowSettings();
                    break;
            }

            RenderLightingFields();

            GUILayout.Space(5);
        }

        Brush GetActiveBrush(int size)
        {
            if (m_CachedBrush == null)
                m_CachedBrush  = new Brush();

            m_CachedBrush.Load(s_BrushTextures[m_SelectedBrush], size);

            return m_CachedBrush;
        }

        public bool Raycast(out Vector2 uv, out Vector3 pos)
        {
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

            RaycastHit hit;
            if (m_Terrain.GetComponent<Collider>().Raycast(mouseRay, out hit, Mathf.Infinity))
            {
                uv = hit.textureCoord;
                pos = hit.point;
                return true;
            }

            uv = Vector2.zero;
            pos = Vector3.zero;
            return false;
        }

        public bool HasFrameBounds()
        {
            // When selecting terrains using Scene search, they may be framed right after selecting, when the editor
            // is not initialized yet. Just return empty bounds in that case.
            return m_Terrain != null;
        }

        public Bounds OnGetFrameBounds()
        {
            Vector2 uv;
            Vector3 pos;

            // It's possible that something other the scene view invoked OnGetFrameBounds (e.g. double clicking the terrain in the hierarchy)
            // In this case we can't do a raycast, because there is no active camera.
            if (Camera.current && m_Terrain.terrainData && Raycast(out uv, out pos))
            {
                // Avoid locking when framing terrain (case 527647)
                if (SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.viewIsLockedToObject = false;

                // Use height editing tool for calculating the bounds by default
                Bounds bounds = new Bounds();
                float brushSize = selectedTool == TerrainTool.PlaceTree ? TreePainter.brushSize : m_Size;
                Vector3 size;
                size.x = brushSize / m_Terrain.terrainData.heightmapWidth * m_Terrain.terrainData.size.x;
                size.z = brushSize / m_Terrain.terrainData.heightmapHeight * m_Terrain.terrainData.size.z;
                size.y = (size.x + size.z) * 0.5F;
                bounds.center = pos;
                bounds.size = size;
                // detail painting needs to be much closer
                if (selectedTool == TerrainTool.PaintDetail && m_Terrain.terrainData.detailWidth != 0)
                {
                    size.x = brushSize / m_Terrain.terrainData.detailWidth * m_Terrain.terrainData.size.x * 0.7F;
                    size.z = brushSize / m_Terrain.terrainData.detailHeight * m_Terrain.terrainData.size.z * 0.7F;
                    size.y = 0;
                    bounds.size = size;
                }

                return bounds;
            }
            else
            {
                // We don't return bounds from the collider, because apparently they are not immediately
                // updated after changing the position. So if terrain is repositioned, then bounds will
                // still be wrong until after some times PhysX updates them (it gets them through some
                // pruning interface and not directly from heightmap shape).
                //return m_Terrain.collider.bounds;

                Vector3 position = m_Terrain.transform.position;

                if (m_Terrain.terrainData == null)
                {
                    return new Bounds(position, Vector3.zero);
                }

                Vector3 size = m_Terrain.terrainData.size;

                float[,] heights = m_Terrain.terrainData.GetHeights(0, 0, m_Terrain.terrainData.heightmapWidth, m_Terrain.terrainData.heightmapHeight);

                float maxHeight = float.MinValue;
                for (int y = 0; y < m_Terrain.terrainData.heightmapHeight; y++)
                    for (int x = 0; x < m_Terrain.terrainData.heightmapWidth; x++)
                        maxHeight = Mathf.Max(maxHeight, heights[x, y]);

                size.y = maxHeight * size.y;

                return new Bounds(position + size * 0.5f, size);
            }
        }

        private bool IsModificationToolActive()
        {
            if (!m_Terrain)
                return false;
            TerrainTool st = selectedTool;
            if (st == TerrainTool.TerrainSettings)
                return false;
            if ((int)st < 0 || st >= TerrainTool.TerrainToolCount)
                return false;
            return true;
        }

        bool IsBrushPreviewVisible()
        {
            if (!IsModificationToolActive())
                return false;

            Vector3 pos;
            Vector2 uv;
            return Raycast(out uv, out pos);
        }

        void DisableProjector()
        {
            if (m_CachedBrush != null)
                m_CachedBrush.GetPreviewProjector().enabled = false;
        }

        void UpdatePreviewBrush()
        {
            if (!IsModificationToolActive() || m_Terrain.terrainData == null)
            {
                DisableProjector();
                return;
            }

            Projector projector = GetActiveBrush(m_Size).GetPreviewProjector();
            float size = 1.0F;
            float aspect = m_Terrain.terrainData.size.x / m_Terrain.terrainData.size.z;

            Transform tr = projector.transform;
            Vector2 uv;
            Vector3 pos;

            bool isValid = true;
            if (Raycast(out uv, out pos))
            {
                if (selectedTool == TerrainTool.PlaceTree)
                {
                    projector.material.mainTexture = (Texture2D)EditorGUIUtility.Load(EditorResourcesUtility.brushesPath + "builtin_brush_4.png");

                    size = TreePainter.brushSize / 0.80f;
                    aspect = 1;
                }
                else if (selectedTool == TerrainTool.PaintHeight || selectedTool == TerrainTool.SetHeight || selectedTool == TerrainTool.SmoothHeight)
                {
                    if (m_Size % 2 == 0)
                    {
                        float offset = 0.5F;
                        uv.x = (Mathf.Floor(uv.x * (m_Terrain.terrainData.heightmapWidth - 1)) + offset) / (m_Terrain.terrainData.heightmapWidth - 1);
                        uv.y = (Mathf.Floor(uv.y * (m_Terrain.terrainData.heightmapHeight - 1)) + offset) / (m_Terrain.terrainData.heightmapHeight - 1);
                    }
                    else
                    {
                        uv.x = (Mathf.Round(uv.x * (m_Terrain.terrainData.heightmapWidth - 1))) / (m_Terrain.terrainData.heightmapWidth - 1);
                        uv.y = (Mathf.Round(uv.y * (m_Terrain.terrainData.heightmapHeight - 1))) / (m_Terrain.terrainData.heightmapHeight - 1);
                    }

                    pos.x = uv.x * m_Terrain.terrainData.size.x;
                    pos.z = uv.y * m_Terrain.terrainData.size.z;
                    pos += m_Terrain.transform.position;

                    size = m_Size * 0.5f / m_Terrain.terrainData.heightmapWidth * m_Terrain.terrainData.size.x;
                }
                else if (selectedTool == TerrainTool.PaintTexture || selectedTool == TerrainTool.PaintDetail)
                {
                    float offset = m_Size % 2 == 0 ? 0.0F : 0.5F;
                    int width, height;
                    if (selectedTool == TerrainTool.PaintTexture)
                    {
                        width = m_Terrain.terrainData.alphamapWidth;
                        height = m_Terrain.terrainData.alphamapHeight;
                    }
                    else
                    {
                        width = m_Terrain.terrainData.detailWidth;
                        height = m_Terrain.terrainData.detailHeight;
                    }

                    if (width == 0 || height == 0)
                        isValid = false;

                    uv.x = (Mathf.Floor(uv.x * width) + offset) / width;
                    uv.y = (Mathf.Floor(uv.y * height) + offset) / height;

                    pos.x = uv.x * m_Terrain.terrainData.size.x;
                    pos.z = uv.y * m_Terrain.terrainData.size.z;
                    pos += m_Terrain.transform.position;

                    size = m_Size * 0.5f / width * m_Terrain.terrainData.size.x;
                    aspect = (float)width / (float)height;
                }
            }
            else
                isValid = false;

            projector.enabled = isValid;
            if (isValid)
            {
                pos.y = m_Terrain.transform.position.y + m_Terrain.SampleHeight(pos);
                tr.position = pos + new Vector3(0.0f, 50.0f, 0.0f);
            }

            projector.orthographicSize = size / aspect;
            projector.aspectRatio = aspect;
        }

        public void OnSceneGUICallback(SceneView sceneView)
        {
            Initialize();

            if (m_Terrain == null || m_Terrain.terrainData == null)
                return;

            Event e = Event.current;

            CheckKeys();

            int id = GUIUtility.GetControlID(s_TerrainEditorHash, FocusType.Passive);
            switch (e.GetTypeForControl(id))
            {
                case EventType.Layout:
                    if (!IsModificationToolActive())
                        return;
                    HandleUtility.AddDefaultControl(id);
                    break;

                case EventType.MouseMove:
                    if (IsBrushPreviewVisible())
                        HandleUtility.Repaint();
                    break;

                case EventType.MouseDown:
                case EventType.MouseDrag:
                {
                    // Don't do anything at all if someone else owns the hotControl. Fixes case 677541.
                    if (EditorGUIUtility.hotControl != 0 && EditorGUIUtility.hotControl != id)
                        return;

                    // Don't do anything on MouseDrag if we don't own the hotControl.
                    if (e.GetTypeForControl(id) == EventType.MouseDrag && EditorGUIUtility.hotControl != id)
                        return;

                    // If user is ALT-dragging, we want to return to main routine
                    if (Event.current.alt)
                        return;

                    // Allow painting with LMB only
                    if (e.button != 0)
                        return;

                    if (!IsModificationToolActive())
                        return;

                    if (HandleUtility.nearestControl != id)
                        return;

                    if (e.type == EventType.MouseDown)
                        EditorGUIUtility.hotControl = id;

                    Vector2 uv;
                    Vector3 pos;
                    if (Raycast(out uv, out pos))
                    {
                        if (selectedTool == TerrainTool.SetHeight && Event.current.shift)
                        {
                            m_TargetHeight = m_Terrain.terrainData.GetInterpolatedHeight(uv.x, uv.y) / m_Terrain.terrainData.size.y;
                            InspectorWindow.RepaintAllInspectors();
                        }
                        else if (selectedTool == TerrainTool.PlaceTree)
                        {
                            if (e.type == EventType.MouseDown)
                                Undo.RegisterCompleteObjectUndo(m_Terrain.terrainData, "Place Tree");

                            if (!Event.current.shift && !Event.current.control)
                                TreePainter.PlaceTrees(m_Terrain, uv.x, uv.y);
                            else
                                TreePainter.RemoveTrees(m_Terrain, uv.x, uv.y, Event.current.control);
                        }
                        else if (selectedTool == TerrainTool.PaintTexture)
                        {
                            if (e.type == EventType.MouseDown)
                            {
                                var undoableObjects = new List<UnityEngine.Object>();
                                undoableObjects.Add(m_Terrain.terrainData);
                                undoableObjects.AddRange(m_Terrain.terrainData.alphamapTextures);

                                Undo.RegisterCompleteObjectUndo(undoableObjects.ToArray(), "Detail Edit");
                            }

                            // Setup splat painter
                            SplatPainter splatPainter = new SplatPainter();
                            splatPainter.size = m_Size;
                            splatPainter.strength = m_Strength;
                            splatPainter.terrainData = m_Terrain.terrainData;
                            splatPainter.brush = GetActiveBrush(splatPainter.size);
                            splatPainter.target = m_SplatAlpha;
                            splatPainter.tool = selectedTool;

                            m_Terrain.editorRenderFlags = TerrainRenderFlags.Heightmap;
                            splatPainter.Paint(uv.x, uv.y, m_SelectedSplat);


                            // Don't perform basemap calculation while painting
                            m_Terrain.terrainData.SetBasemapDirty(false);
                        }
                        else if (selectedTool == TerrainTool.PaintDetail)
                        {
                            if (e.type == EventType.MouseDown)
                                Undo.RegisterCompleteObjectUndo(m_Terrain.terrainData, "Detail Edit");
                            // Setup detail painter
                            DetailPainter detailPainter = new DetailPainter();
                            detailPainter.size = m_Size;
                            detailPainter.targetStrength = m_DetailStrength * 16F;
                            detailPainter.opacity = m_DetailOpacity;
                            if (Event.current.shift || Event.current.control)
                                detailPainter.targetStrength *= -1;
                            detailPainter.clearSelectedOnly = Event.current.control;
                            detailPainter.terrainData = m_Terrain.terrainData;
                            detailPainter.brush = GetActiveBrush(detailPainter.size);
                            detailPainter.tool = selectedTool;
                            detailPainter.randomizeDetails = true;

                            detailPainter.Paint(uv.x, uv.y, m_SelectedDetail);
                        }
                        else
                        {
                            if (e.type == EventType.MouseDown)
                                Undo.RegisterCompleteObjectUndo(m_Terrain.terrainData, "Heightmap Edit");

                            // Setup painter
                            HeightmapPainter painter = new HeightmapPainter();
                            painter.size = m_Size;
                            painter.strength = m_Strength * 0.01F;
                            if (selectedTool == TerrainTool.SmoothHeight)
                                painter.strength = m_Strength;
                            painter.terrainData = m_Terrain.terrainData;
                            painter.brush = GetActiveBrush(m_Size);
                            painter.targetHeight = m_TargetHeight;
                            painter.tool = selectedTool;

                            m_Terrain.editorRenderFlags = TerrainRenderFlags.Heightmap;

                            if (selectedTool == TerrainTool.PaintHeight && Event.current.shift)
                                painter.strength = -painter.strength;

                            painter.PaintHeight(uv.x, uv.y);
                        }

                        e.Use();
                    }
                }
                break;

                case EventType.MouseUp:
                {
                    if (GUIUtility.hotControl != id)
                    {
                        return;
                    }

                    // Release hot control
                    GUIUtility.hotControl = 0;

                    if (!IsModificationToolActive())
                        return;

                    // Perform basemap calculation after all painting has completed!
                    if (selectedTool == TerrainTool.PaintTexture)
                        m_Terrain.terrainData.SetBasemapDirty(true);

                    m_Terrain.editorRenderFlags = TerrainRenderFlags.All;
                    m_Terrain.ApplyDelayedHeightmapModification();

                    e.Use();
                }
                break;

                case EventType.Repaint:
                    DisableProjector();
                    break;
            }
        }

        private void OnPreSceneGUICallback(SceneView sceneView)
        {
            if (Event.current.type == EventType.Repaint)
                UpdatePreviewBrush();
        }
    }
} //namespace
