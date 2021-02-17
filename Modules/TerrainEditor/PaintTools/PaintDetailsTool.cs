// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEngine.Rendering;

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
            return newDetailPrototypesArray.Length - 1;
        }

        public static int GetMaxDetailInstances(TerrainData terrainData)
        {
            return terrainData.detailResolutionPerPatch * terrainData.detailResolutionPerPatch * TerrainData.maxDetailsPerRes;
        }
    }

    internal class PaintDetailsTool : TerrainPaintTool<PaintDetailsTool>
    {
        private class Styles
        {
            public readonly GUIContent brushSize = EditorGUIUtility.TrTextContent("Brush Size", "Size of the brush used to paint.");
            public readonly GUIContent details = EditorGUIUtility.TrTextContent("Details");
            public readonly GUIContent detailTargetStrength = EditorGUIUtility.TrTextContent("Target Strength", "Target amount");
            public readonly GUIContent detailVertexWarning = EditorGUIUtility.TrTextContent("The currently selected detail will not render at full strength. Either paint with low opacity, or lower the terrain detail density. Alternatively consider use instanced rendering by setting \"Use GPU Instancing\" to true.");
            public readonly GUIContent editDetails = EditorGUIUtility.TrTextContent("Edit Details...", "Add or remove detail meshes");
            public readonly GUIContent noDetailObjectDefined = EditorGUIUtility.TrTextContent("No Detail objects defined.");
            public readonly GUIContent opacity = EditorGUIUtility.TrTextContent("Opacity", "Strength of the applied effect.");
            public readonly GUIContent tooManyDetails = EditorGUIUtility.TrTextContent("This area contains too many detail objects.\nDecrease the detail object density or remove some by pressing Ctrl while you paint.", EditorGUIUtility.GetHelpIcon(MessageType.Warning));
        }

        private static Styles s_Styles;

        public const int kInvalidDetail = -1;

        private Terrain m_TargetTerrain;
        private BrushRep m_BrushRep;

        private float m_DetailsStrength = 0.8f;
        private int m_MouseOnPatchIndex = -1;

        public float detailOpacity { get; set; }
        public float detailStrength
        {
            get
            {
                return m_DetailsStrength;
            }
            set
            {
                m_DetailsStrength = Mathf.Clamp01(Mathf.Round(value * TerrainData.maxDetailsPerRes) / TerrainData.maxDetailsPerRes);
            }
        }

        public int selectedDetail { get; set; }
        private DetailPrototype m_LastSelectedDetailPrototype;

        private Vector2Int[] m_CachedClampedPatches;
        private int m_LastTerrainDataDirtyCount = -1;
        private float m_LastTerrainDetailDensity = 0.0f;
        private Dictionary<Vector2Int, Vector2> m_CachedPatchHeightMinMax = new Dictionary<Vector2Int, Vector2>();
        private bool m_ShowTooManyDetailText = false;

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            if (m_TargetTerrain == null
                || selectedDetail == kInvalidDetail
                || selectedDetail >= m_TargetTerrain.terrainData.detailPrototypes.Length)
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

                    float targetStrength = m_DetailsStrength;
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

                                float targetValue = Mathf.Lerp(alphamap[y, x], targetStrength * TerrainData.maxDetailsPerRes, opa);
                                alphamap[y, x] = Mathf.Min(Mathf.RoundToInt(targetValue - .5f + Random.value), TerrainData.maxDetailsPerRes);
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
            detailOpacity = EditorPrefs.GetFloat("TerrainDetailOpacity", 1.0f);
            detailStrength = EditorPrefs.GetFloat("TerrainDetailStrength", 0.8f);
            selectedDetail = EditorPrefs.GetInt("TerrainSelectedDetail", 0);

            m_TargetTerrain = null;
            if (Selection.activeGameObject != null)
                m_TargetTerrain = Selection.activeGameObject.GetComponent<Terrain>();

            if (m_TargetTerrain != null && m_TargetTerrain.terrainData != null)
            {
                var prototypes = m_TargetTerrain.terrainData.detailPrototypes;
                if (m_LastSelectedDetailPrototype != null)
                {
                    for (int i = 0; i < prototypes.Length; ++i)
                    {
                        if (m_LastSelectedDetailPrototype.Equals(prototypes[i]))
                        {
                            selectedDetail = i;
                            break;
                        }
                    }
                }
                selectedDetail = prototypes.Length > 0 ? Mathf.Clamp(selectedDetail, 0, prototypes.Length) : kInvalidDetail;
            }
            else
            {
                selectedDetail = kInvalidDetail;
            }
            m_LastSelectedDetailPrototype = null;
        }

        public override void OnExitToolMode()
        {
            m_CachedClampedPatches = null;
            m_ShowTooManyDetailText = false;
            m_LastTerrainDataDirtyCount = -1;
            m_LastTerrainDetailDensity = 0.0f;
            m_CachedPatchHeightMinMax.Clear();

            if (m_TargetTerrain != null && m_TargetTerrain.terrainData != null)
            {
                var prototypes = m_TargetTerrain.terrainData.detailPrototypes;
                if (selectedDetail != kInvalidDetail && selectedDetail < m_TargetTerrain.terrainData.detailPrototypes.Length)
                    m_LastSelectedDetailPrototype = new DetailPrototype(prototypes[selectedDetail]);
            }

            EditorPrefs.SetInt("TerrainSelectedDetail", selectedDetail);
            EditorPrefs.SetFloat("TerrainDetailStrength", detailStrength);
            EditorPrefs.SetFloat("TerrainDetailOpacity", detailOpacity);
        }

        public override string GetName()
        {
            return "Paint Details";
        }

        public override string GetDesc()
        {
            return "Paints the selected detail prototype onto the terrain";
        }

        private static GUIContent[] LoadDetailIcons(DetailPrototype[] prototypes)
        {
            // Locate the proto types asset preview textures
            var guiContents = new GUIContent[prototypes.Length];
            for (int i = 0; i < guiContents.Length; i++)
            {
                guiContents[i] = new GUIContent();

                if (prototypes[i].usePrototypeMesh)
                {
                    Texture tex = AssetPreview.GetAssetPreview(prototypes[i].prototype);
                    guiContents[i].image = tex != null ? tex : null;
                    guiContents[i].text = guiContents[i].tooltip = prototypes[i].prototype != null ? prototypes[i].prototype.name : "Missing";
                }
                else
                {
                    Texture tex = prototypes[i].prototypeTexture;
                    guiContents[i].image = tex != null ? tex : null;
                    guiContents[i].text = guiContents[i].tooltip = tex != null ? tex.name : "Missing";
                }
            }
            return guiContents;
        }

        private void ShowDetailPrototypeMessages(DetailPrototype detailPrototype, Terrain terrain)
        {
            if (!DetailPrototype.IsModeSupportedByRenderPipeline(detailPrototype.renderMode, detailPrototype.useInstancing, out var msg)
                || !detailPrototype.Validate(out msg))
            {
                EditorGUILayout.HelpBox(msg, MessageType.Error);
            }
            else if ((detailPrototype.renderMode != DetailRenderMode.VertexLit || !detailPrototype.useInstancing)
                     && detailPrototype.usePrototypeMesh && detailPrototype.prototype != null
                     && detailPrototype.prototype.TryGetComponent<MeshFilter>(out var meshFilter)
                     && meshFilter.sharedMesh != null)
            {
                var maxVertCount = meshFilter.sharedMesh.vertexCount * PaintDetailsUtils.GetMaxDetailInstances(terrain.terrainData) * terrain.detailObjectDensity;
                if (maxVertCount >= 65536)
                    EditorGUILayout.HelpBox(s_Styles.detailVertexWarning.text, MessageType.Warning);
            }
        }

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            DetailPrototype[] prototypes = terrain.terrainData.detailPrototypes;
            var detailIcons = LoadDetailIcons(prototypes);

            // Detail picker
            GUILayout.Label(s_Styles.details, EditorStyles.boldLabel);

            selectedDetail = TerrainInspector.AspectSelectionGridImageAndText(selectedDetail, prototypes.Length, (i, rect, style, controlID) =>
            {
                bool renderModeSupported = DetailPrototype.IsModeSupportedByRenderPipeline(prototypes[i].renderMode, prototypes[i].useInstancing, out var errorMessage);
                bool mouseHover = rect.Contains(Event.current.mousePosition);

                if (Event.current.type == EventType.Repaint)
                {
                    bool wasEnabled = GUI.enabled;
                    GUI.enabled &= renderModeSupported;
                    style.Draw(rect, detailIcons[i], GUI.enabled && mouseHover && (GUIUtility.hotControl == 0 || GUIUtility.hotControl == controlID), GUI.enabled && GUIUtility.hotControl == controlID, i == selectedDetail, false);
                    GUI.enabled = wasEnabled;
                }

                if (!renderModeSupported)
                {
                    var tmpContent = EditorGUIUtility.TempContent(EditorGUIUtility.GetHelpIcon(MessageType.Error));
                    tmpContent.tooltip = errorMessage;
                    GUI.Label(new Rect(rect.xMax - 16, rect.yMin + 1, 19, 19), tmpContent);
                }

                if (mouseHover)
                {
                    GUIUtility.mouseUsed = true;
                    GUIStyle.SetMouseTooltip(detailIcons[i].tooltip, rect);
                }
            }, 64, s_Styles.noDetailObjectDefined, out var doubleClick);

            if (doubleClick)
            {
                TerrainDetailContextMenus.EditDetail(new MenuCommand(terrain, selectedDetail));
                GUIUtility.ExitGUI();
            }

            if (selectedDetail >= 0 && selectedDetail < prototypes.Length)
                ShowDetailPrototypeMessages(prototypes[selectedDetail], terrain);

            var terrainInspector = TerrainInspector.s_activeTerrainInspectorInstance;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            TerrainInspector.MenuButton(s_Styles.editDetails, "CONTEXT/TerrainEngineDetails", terrain, selectedDetail);
            terrainInspector.ShowRefreshPrototypes();
            GUILayout.EndHorizontal();

            terrainInspector.ShowDetailStats();
            EditorGUILayout.Space();

            // Brush selector
            editContext.ShowBrushesGUI(0, BrushGUIEditFlags.Select, 0);

            // Brush size
            terrainInspector.brushSize = EditorGUILayout.PowerSlider(s_Styles.brushSize, Mathf.Clamp(terrainInspector.brushSize, 1, 100), 1, 100, 4);
            detailOpacity = EditorGUILayout.Slider(s_Styles.opacity, detailOpacity, 0, 1);

            // Strength
            detailStrength = EditorGUILayout.Slider(s_Styles.detailTargetStrength, detailStrength, 0, 1);

            // Brush editor
            editContext.ShowBrushesGUI((int)EditorGUIUtility.singleLineHeight, BrushGUIEditFlags.Inspect);
        }

        private Vector2 CalculatePatchHeightMinMaxCached(Vector2Int patch, float patchUVSize, int heightmapRes, TerrainData terrainData)
        {
            if (m_CachedPatchHeightMinMax.TryGetValue(patch, out var heightMinMax))
                return heightMinMax;

            var patchHmXMin = Mathf.FloorToInt(patch.x * patchUVSize * heightmapRes);
            var patchHmYMin = Mathf.FloorToInt(patch.y * patchUVSize * heightmapRes);
            var patchHmXMax = Mathf.CeilToInt((patch.x + 1) * patchUVSize * heightmapRes);
            var patchHmYMax = Mathf.CeilToInt((patch.y + 1) * patchUVSize * heightmapRes);
            var heights = terrainData.GetHeights(patchHmXMin, patchHmYMin, Mathf.Min(patchHmXMax + 1, heightmapRes) - patchHmXMin, Mathf.Min(patchHmYMax + 1, heightmapRes) - patchHmYMin);

            float heightMin = heights[0, 0];
            float heightMax = heights[0, 0];
            foreach (var height in heights)
            {
                heightMin = Mathf.Min(heightMin, height);
                heightMax = Mathf.Max(heightMax, height);
            }

            heightMinMax = new Vector2(heightMin, heightMax);
            m_CachedPatchHeightMinMax.Add(patch, heightMinMax);
            return heightMinMax;
        }

        private const float kIconSize = 24;

        private List<Vector4> CalculateClampedDetailPatchIconScreenPositions(Vector2 detailMinMaxHeight, Vector3 terrainPosition, TerrainData terrainData)
        {
            float patchUVSize = 1.0f / terrainData.detailPatchCount;
            int heightmapRes = terrainData.heightmapResolution;
            var sceneViewRect = SceneView.lastActiveSceneView.cameraRect;
            sceneViewRect.y = 0;

            var projectedPointsWithDepth = new List<Vector4>(m_CachedClampedPatches.Length);
            for (int i = 0; i < m_CachedClampedPatches.Length; ++i)
            {
                var patch = m_CachedClampedPatches[i];
                var heightMinMax = CalculatePatchHeightMinMaxCached(patch, patchUVSize, heightmapRes, terrainData);

                var p = new Vector3((patch.x + 0.5f) * patchUVSize * terrainData.size.x, heightMinMax.y * terrainData.size.y + detailMinMaxHeight.y, (patch.y + 0.5f) * patchUVSize * terrainData.size.z);
                var sp = HandleUtility.WorldToGUIPointWithDepth(p + terrainPosition);
                if (sp.z > 0.0f && sp.x >= sceneViewRect.xMin - kIconSize / 2 && sp.y >= sceneViewRect.yMin - kIconSize / 2 && sp.x <= sceneViewRect.xMax + kIconSize / 2 && sp.y <= sceneViewRect.yMax + kIconSize / 2)
                    projectedPointsWithDepth.Add(new Vector4(sp.x, sp.y, sp.z, i));
            }

            projectedPointsWithDepth.Sort((lhs, rhs) => rhs.z.CompareTo(lhs.z));
            return projectedPointsWithDepth;
        }

        private int ClampedDetailPatchesGUI(Terrain terrain, out Vector2 detailMinMaxHeight, out List<Vector4> clampedDetailPatchIconScreenPositions)
        {
            detailMinMaxHeight = Vector2.zero;
            clampedDetailPatchIconScreenPositions = null;

            if (!AnnotationWindow.ShowTerrainDebugWarnings || !SceneView.currentDrawingSceneView.drawGizmos || terrain == null || terrain.terrainData == null)
                return -1;

            var terrainData = terrain.terrainData;
            var detailPrototypes = terrainData.detailPrototypes;
            if (detailPrototypes.Length == 0)
                return -1;

            if (m_LastTerrainDataDirtyCount != EditorUtility.GetDirtyCount(terrainData)
                || m_LastTerrainDetailDensity != terrain.detailObjectDensity)
            {
                m_LastTerrainDataDirtyCount = EditorUtility.GetDirtyCount(terrainData);
                m_LastTerrainDetailDensity = terrain.detailObjectDensity;
                m_CachedClampedPatches = null;
                m_ShowTooManyDetailText = false;
            }

            if (m_CachedClampedPatches == null)
                m_CachedClampedPatches = terrainData.GetClampedDetailPatches(terrain.detailObjectDensity);

            float maxHeight = 0.0f;
            float minHeight = 0.0f;
            foreach (var prototype in detailPrototypes)
            {
                var meshTop = 1.0f;
                var meshBottom = 0.0f;
                if (prototype.renderMode != DetailRenderMode.GrassBillboard
                    && prototype.prototype != null
                    && prototype.prototype.TryGetComponent<MeshFilter>(out var meshFilter)
                    && meshFilter.sharedMesh != null)
                {
                    meshTop = meshFilter.sharedMesh.bounds.max.y;
                    meshBottom = Mathf.Min(meshFilter.sharedMesh.bounds.min.y, 0);
                }
                maxHeight = Mathf.Max(maxHeight, meshTop * prototype.maxHeight);
                minHeight = Mathf.Min(minHeight, meshBottom * prototype.maxHeight);
            }
            detailMinMaxHeight = new Vector2(minHeight, maxHeight);

            var sceneViewRect = SceneView.currentDrawingSceneView.cameraRect;
            sceneViewRect.y = 0;

            var patchUVSize = 1.0f / terrainData.detailPatchCount;
            var heightmapRes = terrainData.heightmapResolution;
            clampedDetailPatchIconScreenPositions = CalculateClampedDetailPatchIconScreenPositions(detailMinMaxHeight, terrain.GetPosition(), terrainData);

            if (GUIUtility.hotControl == 0)
            {
                // Mouse test from front to back
                for (int i = clampedDetailPatchIconScreenPositions.Count - 1; i >= 0; --i)
                {
                    var p = clampedDetailPatchIconScreenPositions[i];
                    var rect = new Rect(p.x - kIconSize / 2, p.y - kIconSize / 2, kIconSize, kIconSize);
                    if (rect.Contains(Event.current.mousePosition))
                    {
                        int patchIndex = (int)p.w;

                        if (Event.current.type == EventType.MouseUp)
                            m_ShowTooManyDetailText = !m_ShowTooManyDetailText;

                        // Use up the mouse events to prevent painting on clicking.
                        if (Event.current.type == EventType.MouseDown
                            || Event.current.type == EventType.MouseUp)
                            Event.current.Use();
                        HandleUtility.Repaint();

                        return patchIndex;
                    }
                }
            }

            return -1;
        }

        private void DrawClampedDetailPatchGUI(int mouseOnPatchIndex, List<Vector4> clampedDetailPatchIconScreenPositions, Vector2 detailMinMaxHeight, Terrain terrain, IOnSceneGUI editContext)
        {
            if (mouseOnPatchIndex == -1)
                m_ShowTooManyDetailText = false;

            if (clampedDetailPatchIconScreenPositions == null
                || Event.current.type != EventType.Repaint)
                return;

            var terrainData = terrain.terrainData;
            float patchUVSize = 1.0f / terrainData.detailPatchCount;
            int heightmapRes = terrainData.heightmapResolution;

            HandleUtility.ApplyWireMaterial(CompareFunction.LessEqual);
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.TRS(terrain.GetPosition(), Quaternion.identity, terrainData.size));
            GL.Begin(GL.LINES);

            float centerOffset = (detailMinMaxHeight.y + detailMinMaxHeight.x) / terrainData.size.y * 0.5f;
            float halfSizeOffset = (detailMinMaxHeight.y - detailMinMaxHeight.x) / terrainData.size.y * 0.5f + 0.001f;

            if (GUIUtility.hotControl != 0 && editContext.hitValidTerrain)
            {
                // hotControl != 0 && hitValidTerrain: during painting
                for (int i = 0; i < m_CachedClampedPatches.Length; ++i)
                {
                    if (m_CachedClampedPatches[i].x * patchUVSize <= editContext.raycastHit.textureCoord.x
                        && m_CachedClampedPatches[i].y * patchUVSize <= editContext.raycastHit.textureCoord.y
                        && (m_CachedClampedPatches[i].x + 1) * patchUVSize > editContext.raycastHit.textureCoord.x
                        && (m_CachedClampedPatches[i].y + 1) * patchUVSize > editContext.raycastHit.textureCoord.y)
                    {
                        mouseOnPatchIndex = i;
                        break;
                    }
                }
            }

            for (int i = 0; i < m_CachedClampedPatches.Length; ++i)
            {
                var patch = m_CachedClampedPatches[i];
                var heightMinMax = CalculatePatchHeightMinMaxCached(patch, patchUVSize, heightmapRes, terrainData);
                var center = new Vector3((patch.x + 0.5f) * patchUVSize, (heightMinMax.y + heightMinMax.x) * 0.5f, (patch.y + 0.5f) * patchUVSize);
                var halfSize = new Vector3(patchUVSize, heightMinMax.y - heightMinMax.x, patchUVSize) * 0.5f;
                center.y += centerOffset;
                halfSize.y += halfSizeOffset;

                GL.Color(i == mouseOnPatchIndex ? new Color(1, 0.2f, 0.2f, 1) : new Color(0.75f, 0, 0, 0.33f));
                GL.Vertex(center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, -halfSize.y, halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, -halfSize.y, halfSize.z));
                GL.Vertex(center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z));
                GL.Vertex(center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z));
                GL.Vertex(center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z));

                GL.Vertex(center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, halfSize.y, -halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, halfSize.y, -halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, halfSize.y, halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, halfSize.y, halfSize.z));
                GL.Vertex(center + new Vector3(-halfSize.x, halfSize.y, halfSize.z));
                GL.Vertex(center + new Vector3(-halfSize.x, halfSize.y, halfSize.z));
                GL.Vertex(center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z));

                GL.Vertex(center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z));
                GL.Vertex(center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, halfSize.y, -halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, -halfSize.y, halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, halfSize.y, halfSize.z));
                GL.Vertex(center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z));
                GL.Vertex(center + new Vector3(-halfSize.x, halfSize.y, halfSize.z));
            }

            GL.End();
            GL.PopMatrix();

            if (s_Styles == null)
                s_Styles = new Styles();

            Handles.BeginGUI();

            // Draw icons from back to front
            var showTooManyDetailTextPos = Vector4.zero;
            for (int i = 0; i < clampedDetailPatchIconScreenPositions.Count; ++i)
            {
                var p = clampedDetailPatchIconScreenPositions[i];
                var rect = new Rect(p.x - kIconSize / 2, p.y - kIconSize / 2, kIconSize, kIconSize);
                GUI.DrawTexture(rect, s_Styles.tooManyDetails.image, ScaleMode.StretchToFill, true, 1.0f, Color.white * ((int)p.w == mouseOnPatchIndex ? 1 : 0.75f), 0, 0);
                if ((int)p.w == mouseOnPatchIndex && m_ShowTooManyDetailText)
                    showTooManyDetailTextPos = p;
            }

            if (mouseOnPatchIndex >= 0 && m_ShowTooManyDetailText)
            {
                var sceneViewRect = SceneView.lastActiveSceneView.cameraRect;
                sceneViewRect.y = 0;

                var textSize = EditorStyles.tooltip.CalcSize(s_Styles.tooManyDetails);
                var textRect = new Rect(showTooManyDetailTextPos.x + kIconSize / 2 + 4, showTooManyDetailTextPos.y - kIconSize / 2, textSize.x, textSize.y);
                if (textRect.xMax > sceneViewRect.xMax)
                {
                    // move left
                    var newX = showTooManyDetailTextPos.x - kIconSize / 2 - 4 - textSize.x;
                    if (newX >= 0)
                        textRect.x = newX;
                }
                if (textRect.yMax > sceneViewRect.yMax)
                {
                    // move up
                    var newY = showTooManyDetailTextPos.y + kIconSize / 2 - textSize.y;
                    if (newY >= 0)
                        textRect.y = newY;
                }

                EditorStyles.tooltip.Draw(textRect, s_Styles.tooManyDetails, false, false, false, false);
            }

            Handles.EndGUI();
        }

        public override void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext)
        {
            // grab m_MouseOnPatchIndex here to avoid calling again in OnRenderBrushPreview
            m_MouseOnPatchIndex = ClampedDetailPatchesGUI(terrain, out var detailMinMaxHeight, out var clampedDetailPatchIconScreenPositions);

            DrawClampedDetailPatchGUI(m_MouseOnPatchIndex, clampedDetailPatchIconScreenPositions, detailMinMaxHeight, terrain, editContext);
        }

        public override void OnRenderBrushPreview(Terrain terrain, IOnSceneGUI editContext)
        {
            if (m_MouseOnPatchIndex == -1 && editContext.hitValidTerrain && Event.current.type == EventType.Repaint)
            {
                BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.raycastHit.textureCoord, editContext.brushSize, 0.0f);
                PaintContext ctx = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);
                TerrainPaintUtilityEditor.DrawBrushPreview(ctx, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture, editContext.brushTexture, brushXform, TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial(), 0);
                TerrainPaintUtility.ReleaseContextResources(ctx);
            }
        }
    }
}
