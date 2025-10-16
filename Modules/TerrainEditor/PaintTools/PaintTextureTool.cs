// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.TerrainTools;
using UnityEditor.ShortcutManagement;
using System.Collections.Generic;

namespace UnityEditor.TerrainTools
{
    [FilePathAttribute("Library/TerrainTools/PaintTexture", FilePathAttribute.Location.ProjectFolder)]
    internal class PaintTextureTool : TerrainPaintToolWithOverlays<PaintTextureTool>
    {
        internal const string k_ToolName = "Paint Texture";
        public override string OnIcon => "TerrainOverlays/PaintMaterials_On.png";
        public override string OffIcon => "TerrainOverlays/PaintMaterials.png";

        Editor m_TemplateMaterialEditor = null;
        Editor m_SelectedTerrainLayerInspector = null;

        [SerializeField]
        TerrainLayer m_SelectedTerrainLayer = null;

        // Keep this separate from m_SelectedTerrainLayer so that it allows selecting null TerrainLayers (like those deleted from Assets).
        private int m_SelectedTerrainLayerIndex = -1;

        [FormerlyPrefKeyAs("Terrain/Texture Paint", "f4")]
        [Shortcut("Terrain/Paint Texture", typeof(TerrainToolShortcutContext), KeyCode.F4)]
        static void SelectShortcut(ShortcutArguments args)
        {
            TerrainToolShortcutContext context = (TerrainToolShortcutContext)args.context;
            context.SelectPaintToolWithOverlays<PaintTextureTool>();
        }

        public override int IconIndex
        {
            get { return (int) MaterialIndex.PaintTexture; }
        }

        public override TerrainCategory Category
        {
            get { return TerrainCategory.Materials; }
        }

        public override string GetName()
        {
            return k_ToolName;
        }

        public override string GetDescription()
        {
            return "Paints the selected material layer onto the terrain texture";
        }

        public override bool HasToolSettings => true;
        public override bool HasBrushMask => true;
        public override bool HasBrushAttributes => true;

        public override void OnEnterToolMode()
        {
            m_SelectedTerrainLayerIndex = -1;
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.uv, editContext.brushSize, 0.0f);
            PaintContext paintContext = TerrainPaintUtility.BeginPaintTexture(terrain, brushXform.GetBrushXYBounds(), m_SelectedTerrainLayer);
            if (paintContext == null)
                return false;

            Material mat = TerrainPaintUtility.GetBuiltinPaintMaterial();

            // apply brush
            float targetAlpha = 1.0f;       // always 1.0 now -- no subtractive painting (we assume this in the ScatterAlphaMap)
            Vector4 brushParams = new Vector4(editContext.brushStrength, targetAlpha, 0.0f, 0.0f);
            mat.SetTexture("_BrushTex", editContext.brushTexture);
            mat.SetVector("_BrushParams", brushParams);

            TerrainPaintUtility.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);

            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, (int)TerrainBuiltinPaintMaterialPasses.PaintTexture);

            TerrainPaintUtility.EndPaintTexture(paintContext, "Terrain Paint - Texture");
            return true;
        }

        public override void OnRenderBrushPreview(Terrain terrain, IOnSceneGUI editContext)
        {
            // We're only doing painting operations, early out if it's not a repaint
            if (Event.current.type != EventType.Repaint)
                return;

            if (editContext.hitValidTerrain)
            {
                BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.raycastHit.textureCoord, editContext.brushSize, 0.0f);
                PaintContext ctx = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);
                TerrainPaintUtilityEditor.DrawBrushPreview(ctx, TerrainBrushPreviewMode.SourceRenderTexture, editContext.brushTexture, brushXform, TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial(), 0);
                TerrainPaintUtility.ReleaseContextResources(ctx);
            }
        }

        private GUIContent globalMipmapWarning;
        private void TextureToolSettingsGUI(Terrain terrain, IOnInspectorGUI editContext, bool overlays)
        {
            if (QualitySettings.globalTextureMipmapLimit != 0)
            {
                if (globalMipmapWarning == null)
                {
                    globalMipmapWarning = EditorGUIUtility.TrTextContent(
                        "The Global Mipmap Limit is a non-zero value. This will result in poor painting performance and reduced paint quality. Unity recommends that you change the limit to zero in the project quality settings.",
                        EditorGUIUtility.FindTexture("console.warnicon"));
                }
                EditorGUILayout.HelpBox(globalMipmapWarning);
            }
            GUILayout.Label("Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            if (!overlays)
            {
                EditorGUILayout.Space();
                var cacheFieldWidth = EditorGUIUtility.fieldWidth;
                var cacheLabelWIdth = EditorGUIUtility.labelWidth;
                Editor.DrawFoldoutInspector(terrain.materialTemplate, ref m_TemplateMaterialEditor); //todo: why doesn't this line work for overlays...?
                EditorGUIUtility.fieldWidth = cacheFieldWidth;
                EditorGUIUtility.labelWidth = cacheLabelWIdth;
                EditorGUILayout.Space();
            }

            Rect dropAreaRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (m_SelectedTerrainLayerIndex == -1)
                m_SelectedTerrainLayerIndex = FindLayerIndex(terrain, m_SelectedTerrainLayer);

            // Show the selection grid for terrain layers
            int newSelectedTerrainLayerIndex = TerrainLayerUtility.ShowTerrainLayersSelectionHelper(terrain, m_SelectedTerrainLayerIndex);
            EditorGUILayout.Space();

            // Update m_SelectedTerrainLayer if the selection index changed
            if (newSelectedTerrainLayerIndex != m_SelectedTerrainLayerIndex)
            {
                m_SelectedTerrainLayerIndex = newSelectedTerrainLayerIndex;
                m_SelectedTerrainLayer = m_SelectedTerrainLayerIndex != -1 ? terrain.terrainData.terrainLayers[m_SelectedTerrainLayerIndex] : null;
            }

            // Show the detailed inspector for the currently selected terrain layer
            TerrainLayerUtility.ShowTerrainLayerGUI(terrain, m_SelectedTerrainLayer, ref m_SelectedTerrainLayerInspector,
                (m_TemplateMaterialEditor as MaterialEditor)?.customShaderGUI as ITerrainLayerCustomUI);
            EditorGUILayout.Space();

            EditorGUILayout.EndVertical();

            // Handle drag and drop for this specific area
            HandleLayerDragAndDrop(dropAreaRect, terrain);

            EditorGUI.EndChangeCheck();
        }

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            TextureToolSettingsGUI(terrain, editContext, false);
            // Texture painting needs to know the largest of the splat map and the height map, as the result goes to
            // the splat map, but the height map is used for previewing.
            int resolution = Mathf.Max(terrain.terrainData.heightmapResolution, terrain.terrainData.alphamapResolution);
            editContext.ShowBrushesGUI(5, BrushGUIEditFlags.All, resolution);
        }

        public override void OnToolSettingsGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            TextureToolSettingsGUI(terrain, editContext, true);
        }

        private int FindLayerIndex(Terrain terrain, TerrainLayer layer)
        {
            if (terrain == null || terrain.terrainData == null || layer == null)
                return -1;

            TerrainLayer[] layers = terrain.terrainData.terrainLayers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] == layer)
                    return i;
            }
            return -1;
        }

        private static List<TerrainLayer> s_inspectorDraggedLayersBuffer = new List<TerrainLayer>();
        private static bool s_isDraggingTerrainLayersInInspector = false;

        private static void CleanupInspectorDragState()
        {
            s_inspectorDraggedLayersBuffer.Clear();
            s_isDraggingTerrainLayersInInspector = false;
        }

        private void HandleLayerDragAndDrop(Rect dropAreaRect, Terrain terrain)
        {
            Event evt = Event.current;

            bool isMouseOverDropArea = dropAreaRect.Contains(evt.mousePosition);
            bool isDragEvent = (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform || evt.type == EventType.DragExited);

            // If not a drag event and no active drag, early exit.
            if (!isDragEvent && !s_isDraggingTerrainLayersInInspector)
            {
                return;
            }

            // Handle DragExited globally to clean up state.
            if (evt.type == EventType.DragExited)
            {
                if (s_isDraggingTerrainLayersInInspector)
                {
                    CleanupInspectorDragState();
                }
                return;
            }

            // For DragUpdated/DragPerform, require mouse to be over drop area.
            if (!isMouseOverDropArea && (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform))
            {
                return;
            }

            // Populate buffer on first DragUpdated over the area.
            if (evt.type == EventType.DragUpdated && !s_isDraggingTerrainLayersInInspector)
            {
                s_inspectorDraggedLayersBuffer.Clear();
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is TerrainLayer layer)
                    {
                        s_inspectorDraggedLayersBuffer.Add(layer);
                    }
                }
                s_isDraggingTerrainLayersInInspector = true;
            }

            // If no valid TerrainLayers are dragged, reject visually.
            if (s_inspectorDraggedLayersBuffer.Count == 0 && (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                evt.Use();
                return;
            }

            switch (evt.type)
            {
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    evt.Use();
                    break;

                case EventType.DragPerform:
                    DragAndDrop.AcceptDrag();
                    bool anyLayerAdded = false;
                    TerrainLayer lastDraggedLayer = null;

                    if (terrain != null && terrain.terrainData != null)
                    {
                        foreach (var layer in s_inspectorDraggedLayersBuffer)
                        {
                            if (FindLayerIndex(terrain, layer) == -1)
                            {
                                TerrainLayerUtility.AddTerrainLayer(terrain, layer);
                                anyLayerAdded = true;
                            }
                            lastDraggedLayer = layer;
                        }

                        if (anyLayerAdded)
                        {
                            EditorUtility.SetDirty(terrain);
                        }

                        if (lastDraggedLayer != null)
                        {
                            m_SelectedTerrainLayer = lastDraggedLayer;
                            m_SelectedTerrainLayerIndex = FindLayerIndex(terrain, lastDraggedLayer);
                        }
                    }
                    CleanupInspectorDragState();
                    evt.Use();
                    break;
            }
        }
    }
}
