// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TerrainTools;
using System;
using UnityEngine.Rendering;

namespace UnityEditor.TerrainTools
{
    internal class PaintDetailsTool : TerrainPaintToolWithOverlays<PaintDetailsTool>
    {
        internal const string k_ToolName = "Paint Details";
        public override string OnIcon => "TerrainOverlays/PaintDetails_On.png";
        public override string OffIcon => "TerrainOverlays/PaintDetails.png";

        private class Styles
        {
            public readonly GUIContent brushSize = EditorGUIUtility.TrTextContent("Brush Size", "Size of the brush used to paint.");
            public readonly GUIContent details = EditorGUIUtility.TrTextContent("Details");
            public readonly GUIContent detailTargetStrength = EditorGUIUtility.TrTextContent("Target Strength", "Target amount");
            public readonly GUIContent detailVertexWarning = EditorGUIUtility.TrTextContent("The currently selected detail will not render at full strength. Either paint with low opacity, or lower the terrain detail density. Alternatively consider use instanced rendering by setting \"Use GPU Instancing\" to true.");
            public readonly GUIContent editDetails = EditorGUIUtility.TrTextContent("Edit Details...", "Add or remove detail meshes");
            public readonly GUIContent noDetailObjectDefined = EditorGUIUtility.TrTextContent("No Detail objects defined.");
            public readonly GUIContent opacity = EditorGUIUtility.TrTextContent("Opacity", "Strength of the applied effect.");
        }

        private static Styles s_Styles;

        public const int kInvalidDetail = -1;

        private Terrain m_TargetTerrain;
        private DetailBrushRepresentation m_BrushRep;

        private float m_DetailsStrength = 0.8f;
        private int m_MouseOnPatchIndex = -1;

        public float detailStrength
        {
            get
            {
                return m_DetailsStrength;
            }
            set
            {
                m_DetailsStrength = m_TargetTerrain == null || m_TargetTerrain.terrainData == null ?
                    value :
                    Mathf.Clamp01(Mathf.Round(value * m_TargetTerrain.terrainData.maxDetailScatterPerRes) / m_TargetTerrain.terrainData.maxDetailScatterPerRes);

            }
        }
        internal static event Action BrushTargetStrengthChanged;

        // storing the previous detailStrength to trigger a strengthChanged Action
        internal float prevBrushDetailStrength
        {
            get;
            set;
        }

        public int selectedDetail { get; set; }
        private DetailPrototype m_LastSelectedDetailPrototype;

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
                m_BrushRep = new DetailBrushRepresentation();
            }

            PaintTreesDetailsContext ctx = PaintTreesDetailsContext.Create(terrain, editContext.uv);

            for (int t = 0; t < ctx.neighborTerrains.Length; ++t)
            {
                Terrain ctxTerrain = ctx.neighborTerrains[t];
                if (ctxTerrain != null)
                {
                    int detailPrototype = PaintDetailsToolUtility.FindDetailPrototype(ctxTerrain, m_TargetTerrain, selectedDetail);
                    if (detailPrototype == kInvalidDetail)
                    {
                        detailPrototype = PaintDetailsToolUtility.CopyDetailPrototype(ctxTerrain, m_TargetTerrain, selectedDetail);
                    }

                    TerrainData terrainData = ctxTerrain.terrainData;

                    TerrainPaintUtilityEditor.UpdateTerrainDataUndo(terrainData, "Terrain - Detail Edit");

                    int size = (int)Mathf.Max(1.0f, editContext.brushSize * ((float)terrainData.detailResolution / terrainData.size.x));

                    m_BrushRep.Update(brush, size);

                    float targetStrength = m_DetailsStrength;
                    if (Event.current.shift || Event.current.control)
                        targetStrength = -targetStrength;

                    DetailBrushBounds brushBounds = new DetailBrushBounds(terrainData, ctx, size, t);

                    if (brushBounds.bounds.size == Vector2.zero)
                    {
                        continue;
                    }

                    int[] layers = { detailPrototype };
                    if (targetStrength < 0.0F && !Event.current.control)
                        layers = terrainData.GetSupportedLayers(brushBounds.min, brushBounds.bounds.size);

                    for (int i = 0; i < layers.Length; i++)
                    {
                        int[,] alphamap = terrainData.GetDetailLayer(brushBounds.min, brushBounds.bounds.size, layers[i]);

                        for (int y = 0; y < brushBounds.bounds.height; y++)
                        {
                            for (int x = 0; x < brushBounds.bounds.width; x++)
                            {
                                Vector2Int brushOffset = brushBounds.GetBrushOffset(x, y);
                                float opa =  editContext.brushSize * m_BrushRep.GetStrength(brushOffset.x, brushOffset.y);

                                float targetValue = Mathf.Lerp(alphamap[y, x], targetStrength * terrainData.maxDetailScatterPerRes, opa);
                                alphamap[y, x] = Mathf.Min(Mathf.RoundToInt(targetValue - .5f + UnityEngine.Random.value), terrainData.maxDetailScatterPerRes);
                            }
                        }

                        terrainData.SetDetailLayer(brushBounds.min, layers[i], alphamap);
                    }
                }
            }

            return false;
        }

        public override void OnEnterToolMode()
        {
            detailStrength = EditorPrefs.GetFloat("TerrainDetailStrength", 0.8f);
            prevBrushDetailStrength = detailStrength;
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
            PaintDetailsToolUtility.ResetDetailsUtilityData();

            if (m_TargetTerrain != null && m_TargetTerrain.terrainData != null)
            {
                var prototypes = m_TargetTerrain.terrainData.detailPrototypes;
                if (selectedDetail != kInvalidDetail && selectedDetail < m_TargetTerrain.terrainData.detailPrototypes.Length)
                    m_LastSelectedDetailPrototype = new DetailPrototype(prototypes[selectedDetail]);
            }

            EditorPrefs.SetInt("TerrainSelectedDetail", selectedDetail);
            EditorPrefs.SetFloat("TerrainDetailStrength", detailStrength);
        }

        public override int IconIndex
        {
            get { return (int) FoliageIndex.PaintDetails; }
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
            return "Click to paint details.\n\nHold shift and click to erase details.\n\nHold Ctrl and click to erase only details of the selected type.";
        }

        public override bool HasToolSettings => true;
        public override bool HasBrushMask => true;
        public override bool HasBrushAttributes => true;

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
                var maxVertCount = meshFilter.sharedMesh.vertexCount * PaintDetailsToolUtility.GetMaxDetailInstancesPerPatch(terrain.terrainData) * terrain.detailObjectDensity;
                if (maxVertCount >= 65536)
                    EditorGUILayout.HelpBox(s_Styles.detailVertexWarning.text, MessageType.Warning);
            }
        }

        public override void OnToolSettingsGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            DetailPrototype[] prototypes = terrain.terrainData.detailPrototypes;
            var detailIcons = PaintDetailsToolUtility.LoadDetailIcons(prototypes);

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
            TerrainInspector.ShowRefreshPrototypes();
            GUILayout.EndHorizontal();

            if(prototypes.Length > 0)
            {
                ShowTextureFallbackWarning(ref terrain);
            }

            terrainInspector.ShowDetailStats();
            EditorGUILayout.Space();

        }


        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            int textureRez = terrain.terrainData.heightmapResolution;

            // show just size and opacity (not the inspect until the end of this function)
            editContext.ShowBrushesGUI(5, BrushGUIEditFlags.Select | BrushGUIEditFlags.Size | BrushGUIEditFlags.Opacity, textureRez);

            // Strength
            detailStrength = EditorGUILayout.Slider(s_Styles.detailTargetStrength, detailStrength, 0, 1);

            // check for detail strength changing, and invoke appropriate action
            if (!Mathf.Approximately(detailStrength, prevBrushDetailStrength))
            {
                if (BrushTargetStrengthChanged != null) BrushTargetStrengthChanged();
                prevBrushDetailStrength = detailStrength;
            }

            // Brush editor
            editContext.ShowBrushesGUI((int)EditorGUIUtility.singleLineHeight, BrushGUIEditFlags.Inspect);

            OnToolSettingsGUI(terrain, editContext);

        }

        private void ShowTextureFallbackWarning(ref Terrain terrain)
        {
            var hasCompressedPrototypeTexture = false;
            var hasGPUInstancing = false;
            foreach (var prototype in terrain.terrainData.detailPrototypes)
            {
                if (prototype.useInstancing)
                {
                    hasGPUInstancing = true;
                    continue;
                }
                if (hasCompressedPrototypeTexture && hasGPUInstancing)
                    break;
                if (hasCompressedPrototypeTexture)
                    continue;
                var sourceTexture = prototype.prototypeTexture;
                if (prototype.usePrototypeMesh)
                {
                    var renderer = prototype.prototype.GetComponent<Renderer>();
                    sourceTexture = renderer?.sharedMaterial.mainTexture as Texture2D;
                }

                hasCompressedPrototypeTexture = sourceTexture != null && UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsCompressedFormat(sourceTexture.graphicsFormat);
            }
            if (hasCompressedPrototypeTexture && !UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsCompressedFormat(terrain.terrainData.atlasFormat))
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Space(3);
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        var message = hasGPUInstancing ? "The detail texture atlas cannot be compressed due to a mix of GPU instanced and non-GPU instanced details." : "The detail texture atlas cannot be compressed due to a mix of detail texture formats.";
                        // Copied from LabelField called by HelpBox - using so that label and button link are in the same helpbox
                        var infoLabel = EditorGUIUtility.TempContent(message, EditorGUIUtility.GetHelpIcon(MessageType.Warning));
                        Rect r = GUILayoutUtility.GetRect(infoLabel, EditorStyles.wordWrappedLabel);
                        int oldIndent = EditorGUI.indentLevel;
                        EditorGUI.indentLevel = 0;
                        EditorGUI.LabelField(r, infoLabel, EditorStyles.wordWrappedLabel);
                        EditorGUI.indentLevel = oldIndent;

                        GUILayout.FlexibleSpace();
                        EditorGUI.indentLevel = 2;
                        if (EditorGUILayout.LinkButton("Read more about formats"))
                        {
                            Help.BrowseURL($"https://docs.unity3d.com//{Application.unityVersionVer}.{Application.unityVersionMaj}/Documentation/ScriptReference/Texture2D.PackTextures.html");
                        }
                        EditorGUI.indentLevel = oldIndent;
                        GUILayout.Space(3);
                    }
                }
            }
        }

        public override void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext)
        {
            // grab m_MouseOnPatchIndex here to avoid calling again in OnRenderBrushPreview
            m_MouseOnPatchIndex = PaintDetailsToolUtility.ClampedDetailPatchesGUI(terrain, out var detailMinMaxHeight, out var clampedDetailPatchIconScreenPositions);

            PaintDetailsToolUtility.DrawClampedDetailPatchGUI(m_MouseOnPatchIndex, clampedDetailPatchIconScreenPositions, detailMinMaxHeight, terrain, editContext);
        }

        public override void OnRenderBrushPreview(Terrain terrain, IOnSceneGUI editContext)
        {
            if (m_MouseOnPatchIndex == -1 && editContext.hitValidTerrain && Event.current.type == EventType.Repaint)
            {
                BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.raycastHit.textureCoord, editContext.brushSize, 0.0f);
                PaintContext ctx = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);
                TerrainPaintUtilityEditor.DrawBrushPreview(ctx, TerrainBrushPreviewMode.SourceRenderTexture, editContext.brushTexture, brushXform, TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial(), 0);
                TerrainPaintUtility.ReleaseContextResources(ctx);
            }
        }
    }
}
