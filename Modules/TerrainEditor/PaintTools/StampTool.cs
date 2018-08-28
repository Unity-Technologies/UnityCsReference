// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
    [FilePathAttribute("Library/TerrainTools/Stamp", FilePathAttribute.Location.ProjectFolder)]
    public class StampTool : TerrainPaintTool<StampTool>
    {
        [SerializeField]
        float m_StampHeight = 0.0f;

        public override string GetName()
        {
            return "Stamp Terrain";
        }

        public override string GetDesc()
        {
            return "Left click to stamp the brush onto the terrain.\n\nHold shift and left click to stamp negative.";
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            if (Event.current.type == EventType.MouseDrag)
                return true;

            Material mat = TerrainPaintUtility.GetBuiltinPaintMaterial();

            Rect brushRect = TerrainPaintUtility.CalculateBrushRectInTerrainUnits(terrain, editContext.uv, editContext.brushSize);
            TerrainPaintUtility.PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushRect);

            Vector4 brushParams = new Vector4(editContext.brushStrength * 0.01f, 0.0f, m_StampHeight, 0.0f);

            if (Event.current.shift)
                brushParams.x = -brushParams.x;

            mat.SetTexture("_BrushTex", editContext.brushTexture);
            mat.SetVector("_BrushParams", brushParams);
            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, (int)TerrainPaintUtility.BuiltinPaintMaterialPasses.StampHeight);

            TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Stamp");
            return true;
        }

        public override void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext)
        {
            TerrainPaintUtilityEditor.ShowDefaultPreviewBrush(terrain, editContext.brushTexture, editContext.brushStrength * 0.01f, editContext.brushSize, m_StampHeight);
        }

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            EditorGUI.BeginChangeCheck();
            m_StampHeight = EditorGUILayout.Slider(new GUIContent("Stamp Height", "You can set the Stamp Height property manually or you can shift-click on the terrain to sample the height at the mouse position (rather like the “eyedropper” tool in an image editor)."), m_StampHeight * terrain.terrainData.size.y, 0, terrain.terrainData.size.y) / terrain.terrainData.size.y;
            if (EditorGUI.EndChangeCheck())
                Save(true);

            // show built-in brushes
            base.OnInspectorGUI(terrain, editContext);
        }
    }
}
