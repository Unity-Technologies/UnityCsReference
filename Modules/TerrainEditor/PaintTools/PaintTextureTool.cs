// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;

namespace UnityEditor
{
    [FilePathAttribute("Library/TerrainTools/PaintTexture", FilePathAttribute.Location.ProjectFolder)]
    public class PaintTextureTool : TerrainPaintTool<PaintTextureTool>
    {
        [SerializeField]
        TerrainLayer m_SelectedTerrainLayer = null;
        [SerializeField]
        float m_SplatAlpha = 1.0f;
        public override string GetName()
        {
            return "Paint Texture";
        }

        public override string GetDesc()
        {
            return "Paints the selected material layer onto the terrain texture";
        }

        public override bool Paint(Terrain terrain, Texture brushTexture, Vector2 uv, float brushStrength, int brushSize)
        {
            Rect brushRect = TerrainPaintUtility.CalculateBrushRectInTerrainUnits(terrain, uv, brushSize);

            TerrainPaintUtility.PaintContext paintContext = TerrainPaintUtility.BeginPaintTexture(terrain, brushRect, m_SelectedTerrainLayer);
            if (paintContext == null)
                return false;

            Material mat = TerrainPaintUtility.GetBuiltinPaintMaterial();
            // apply brush
            Vector4 brushParams = new Vector4(brushStrength, m_SplatAlpha, 0.0f, 0.0f);
            mat.SetTexture("_BrushTex", brushTexture);
            mat.SetVector("_BrushParams", brushParams);
            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, (int)TerrainPaintUtility.BuiltinPaintMaterialPasses.PaintTexture);

            TerrainPaintUtility.EndPaintTexture(paintContext, "Terrain Paint - Texture");
            return true;
        }

        public override void OnSceneGUI(SceneView sceneView, Terrain terrain, Texture brushTexture, float brushStrength, int brushSizeInTerrainUnits)
        {
            TerrainPaintUtilityEditor.ShowDefaultPreviewBrush(terrain, brushTexture, brushStrength, brushSizeInTerrainUnits, 0.0f);
        }

        public override void OnInspectorGUI(Terrain terrain)
        {
            GUILayout.Label("Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            m_SplatAlpha = EditorGUILayout.Slider("Target Strength", m_SplatAlpha, 0.0F, 1.0F);
            int layerIndex = TerrainPaintUtility.FindTerrainLayerIndex(terrain, m_SelectedTerrainLayer);
            layerIndex = TerrainLayerUtility.ShowTerrainLayersSelectionHelper(terrain, layerIndex);
            if (EditorGUI.EndChangeCheck())
            {
                if (layerIndex != -1)
                    m_SelectedTerrainLayer = terrain.terrainData.terrainLayers[layerIndex];

                Save(true);
            }
        }
    }
}
