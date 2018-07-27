// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    public class PaintHeightTool : TerrainPaintTool<PaintHeightTool>
    {
        public override string GetName()
        {
            return "Raise or Lower Terrain";
        }

        public override string GetDesc()
        {
            return "Left click to raise.\n\nHold shift and left click to lower.";
        }

        public override void OnSceneGUI(SceneView sceneView, Terrain terrain, Texture brushTexture, float brushStrength, int brushSizeInTerrainUnits)
        {
            TerrainPaintUtilityEditor.ShowDefaultPreviewBrush(terrain, brushTexture, brushStrength * 0.01f, brushSizeInTerrainUnits, brushStrength * 0.01f);
        }

        public override bool Paint(Terrain terrain, Texture brushTexture, Vector2 uv, float brushStrength, int brushSize)
        {
            if (Event.current.shift)
                brushStrength = -brushStrength;

            Material mat = TerrainPaintUtility.GetBuiltinPaintMaterial();
            Rect brushRect = TerrainPaintUtility.CalculateBrushRectInTerrainUnits(terrain, uv, brushSize);
            TerrainPaintUtility.PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushRect);

            // apply brush
            Vector4 brushParams = new Vector4(brushStrength * 0.01f, 0.0f, 0.0f, 0.0f);
            mat.SetTexture("_BrushTex", brushTexture);
            mat.SetVector("_BrushParams", brushParams);
            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, (int)TerrainPaintUtility.BuiltinPaintMaterialPasses.RaiseLowerHeight);

            TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Raise or Lower Height");
            return false;
        }
    }
}
