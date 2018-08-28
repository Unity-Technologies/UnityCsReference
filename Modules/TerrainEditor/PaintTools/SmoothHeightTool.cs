// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
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

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            editContext.ShowBrushesGUI(5);
        }

        public override void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext)
        {
            TerrainPaintUtilityEditor.ShowDefaultPreviewBrush(terrain, editContext.brushTexture, editContext.brushStrength, editContext.brushSize, 0.0f);
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            Rect brushRect = TerrainPaintUtility.CalculateBrushRectInTerrainUnits(terrain, editContext.uv, editContext.brushSize);
            TerrainPaintUtility.PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushRect);

            Material mat = TerrainPaintUtility.GetBuiltinPaintMaterial();
            Vector4 brushParams = new Vector4(editContext.brushStrength, 0.0f, 0.0f, 0.0f);
            mat.SetTexture("_BrushTex", editContext.brushTexture);
            mat.SetVector("_BrushParams", brushParams);
            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, (int)TerrainPaintUtility.BuiltinPaintMaterialPasses.SmoothHeights);

            TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Smooth Height");
            return true;
        }
    }
}
