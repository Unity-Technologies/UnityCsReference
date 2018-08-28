// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
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

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            editContext.ShowBrushesGUI(5);
        }

        public override void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext)
        {
            TerrainPaintUtilityEditor.ShowDefaultPreviewBrush(terrain, editContext.brushTexture, editContext.brushStrength * 0.01f, editContext.brushSize, editContext.brushStrength * 0.01f);
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            float brushStrength = Event.current.shift ? -editContext.brushStrength : editContext.brushStrength;

            Material mat = TerrainPaintUtility.GetBuiltinPaintMaterial();
            Rect brushRect = TerrainPaintUtility.CalculateBrushRectInTerrainUnits(terrain, editContext.uv, editContext.brushSize);
            TerrainPaintUtility.PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushRect);

            // apply brush
            Vector4 brushParams = new Vector4(brushStrength * 0.01f, 0.0f, 0.0f, 0.0f);
            mat.SetTexture("_BrushTex", editContext.brushTexture);
            mat.SetVector("_BrushParams", brushParams);
            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, (int)TerrainPaintUtility.BuiltinPaintMaterialPasses.RaiseLowerHeight);

            TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Raise or Lower Height");
            return true;
        }
    }
}
