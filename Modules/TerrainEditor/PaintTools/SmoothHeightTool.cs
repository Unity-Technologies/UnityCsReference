// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    public class SmoothHeightTool : TerrainPaintTool<SmoothHeightTool>
    {
        Material m_Material = null;
        Material GetPaintMaterial()
        {
            if (m_Material == null)
                m_Material = new Material(Shader.Find("Hidden/TerrainEngine/PaintHeight"));
            return m_Material;
        }

        public override string GetName()
        {
            return "Smooth Height";
        }

        public override string GetDesc()
        {
            return "Click to average out the terrain height.";
        }

        public override void OnSceneGUI(SceneView sceneView, Terrain terrain, Texture brushTexture, float brushStrength, int brushSizeInTerrainUnits)
        {
            TerrainPaintUtilityEditor.ShowDefaultPreviewBrush(terrain, brushTexture, brushStrength, brushSizeInTerrainUnits, 0.0f);
        }

        public override bool Paint(Terrain terrain, Texture brushTexture, Vector2 uv, float brushStrength, int brushSizeInTerrainUnits)
        {
            Vector2Int brushSize = TerrainPaintUtility.CalculateBrushSizeInHeightmapSpace(terrain, brushSizeInTerrainUnits);
            TerrainPaintUtility.PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, uv, brushSize);
            TerrainPaintUtilityEditor.UpdateTerrainUndo(paintContext, TerrainPaintUtilityEditor.ToolAction.PaintHeightmap, "Terrain Paint - Smooth Height");

            Material mat = GetPaintMaterial();
            Vector4 brushParams = new Vector4(brushStrength, 0.0f, 0.0f, 0.0f);
            mat.SetTexture("_BrushTex", brushTexture);
            mat.SetVector("_BrushParams", brushParams);
            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, 3);

            TerrainPaintUtility.EndPaintHeightmap(paintContext);
            return false;
        }
    }
}
