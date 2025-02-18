// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.TerrainTools;
using UnityEditor.ShortcutManagement;

namespace UnityEditor.TerrainTools
{
    [FilePathAttribute("Library/TerrainTools/PaintTexture", FilePathAttribute.Location.ProjectFolder)]
    internal class PaintTextureTool : TerrainPaintTool<PaintTextureTool>
    {
        const string toolName = "Paint Texture";

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
            context.SelectPaintTool<PaintTextureTool>();
        }

        public override string GetName()
        {
            return toolName;
        }

        public override string GetDescription()
        {
            return "Paints the selected material layer onto the terrain texture";
        }

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
        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            if (QualitySettings.masterTextureLimit != 0)
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

            EditorGUILayout.Space();
            var cacheFieldWidth = EditorGUIUtility.fieldWidth;
            var cacheLabelWIdth = EditorGUIUtility.labelWidth;
            Editor.DrawFoldoutInspector(terrain.materialTemplate, ref m_TemplateMaterialEditor);
            EditorGUIUtility.fieldWidth = cacheFieldWidth;
            EditorGUIUtility.labelWidth = cacheLabelWIdth;
            EditorGUILayout.Space();

            if (m_SelectedTerrainLayerIndex == -1)
                m_SelectedTerrainLayerIndex = TerrainPaintUtility.FindTerrainLayerIndex(terrain, m_SelectedTerrainLayer);

            m_SelectedTerrainLayerIndex = TerrainLayerUtility.ShowTerrainLayersSelectionHelper(terrain, m_SelectedTerrainLayerIndex);
            EditorGUILayout.Space();

            if (EditorGUI.EndChangeCheck())
            {
                m_SelectedTerrainLayer = m_SelectedTerrainLayerIndex != -1 ? terrain.terrainData.terrainLayers[m_SelectedTerrainLayerIndex] : null;
                Save(true);
            }

            TerrainLayerUtility.ShowTerrainLayerGUI(terrain, m_SelectedTerrainLayer, ref m_SelectedTerrainLayerInspector,
                (m_TemplateMaterialEditor as MaterialEditor)?.customShaderGUI as ITerrainLayerCustomUI);
            EditorGUILayout.Space();

            // Texture painting needs to know the largest of the splat map and the height map, as the result goes to
            // the splat map, but the height map is used for previewing.
            int resolution = Mathf.Max(terrain.terrainData.heightmapResolution, terrain.terrainData.alphamapResolution);
            editContext.ShowBrushesGUI(5, BrushGUIEditFlags.All, resolution);
        }
    }
}
