// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    public class SmoothHeightTool : TerrainPaintTool<SmoothHeightTool>
    {
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

        public override bool Paint(Terrain terrain, Texture brushTexture, Vector2 uv, float brushStrength, int brushSize)
        {
            Rect brushRect = TerrainPaintUtility.CalculateBrushRectInTerrainUnits(terrain, uv, brushSize);
            TerrainPaintUtility.PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushRect);

            Material mat = TerrainPaintUtility.GetBuiltinPaintMaterial();
            Vector4 brushParams = new Vector4(brushStrength, 0.0f, 0.0f, 0.0f);
            mat.SetTexture("_BrushTex", brushTexture);
            mat.SetVector("_BrushParams", brushParams);
            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, (int)TerrainPaintUtility.BuiltinPaintMaterialPasses.SmoothHeights);

            TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Smooth Height");
            return false;
        }
    }
}
