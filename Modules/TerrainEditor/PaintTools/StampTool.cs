// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditor
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

        public override bool Paint(Terrain terrain, Texture brushTexture, Vector2 uv, float brushStrength, int brushSize)
        {
            if (Event.current.type == EventType.MouseDrag)
                return false;

            Material mat = TerrainPaintUtility.GetBuiltinPaintMaterial();

            Rect brushRect = TerrainPaintUtility.CalculateBrushRectInTerrainUnits(terrain, uv, brushSize);
            TerrainPaintUtility.PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushRect);

            Vector4 brushParams = new Vector4(brushStrength * 0.01f, 0.0f, m_StampHeight, 0.0f);

            if (Event.current.shift)
                brushParams.x = -brushParams.x;

            mat.SetTexture("_BrushTex", brushTexture);
            mat.SetVector("_BrushParams", brushParams);
            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, (int)TerrainPaintUtility.BuiltinPaintMaterialPasses.StampHeight);

            TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Stamp");
            return false;
        }

        public override void OnSceneGUI(SceneView sceneView, Terrain terrain, Texture brushTexture, float brushStrength, int brushSizeInTerrainUnits)
        {
            TerrainPaintUtilityEditor.ShowDefaultPreviewBrush(terrain, brushTexture, brushStrength * 0.01f, brushSizeInTerrainUnits, m_StampHeight);
        }

        public override void OnInspectorGUI(Terrain terrain)
        {
            EditorGUI.BeginChangeCheck();
            m_StampHeight = EditorGUILayout.Slider(new GUIContent("Stamp Height", "You can set the Stamp Height property manually or you can shift-click on the terrain to sample the height at the mouse position (rather like the “eyedropper” tool in an image editor)."), m_StampHeight * terrain.terrainData.size.y, 0, terrain.terrainData.size.y) / terrain.terrainData.size.y;
            if (EditorGUI.EndChangeCheck())
                Save(true);
        }
    }
}
