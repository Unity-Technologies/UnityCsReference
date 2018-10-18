// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
    [FilePathAttribute("Library/TerrainTools/SetHeight", FilePathAttribute.Location.ProjectFolder)]
    public class SetHeightTool : TerrainPaintTool<SetHeightTool>
    {
        [SerializeField]
        float m_Height;
        [SerializeField]
        bool m_FlattenAll;

        public override string GetName()
        {
            return "Set Height";
        }

        public override string GetDesc()
        {
            return "Left click to set the height.\n\nHold shift and left click to sample the target height.";
        }

        public override void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext)
        {
            // We're only doing painting operations, early out if it's not a repaint
            if (Event.current.type != EventType.Repaint)
                return;

            if (editContext.hitValidTerrain)
            {
                BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.raycastHit.textureCoord, editContext.brushSize, 0.0f);
                PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);

                Material material = TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial();

                TerrainPaintUtilityEditor.DrawBrushPreview(
                    paintContext, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture, editContext.brushTexture, brushXform, material, 0);

                // draw result preview
                {
                    ApplyBrushInternal(paintContext, editContext.brushStrength, editContext.brushTexture, brushXform);

                    // restore old render target
                    RenderTexture.active = paintContext.oldRenderTexture;

                    material.SetTexture("_HeightmapOrig", paintContext.sourceRenderTexture);

                    TerrainPaintUtilityEditor.DrawBrushPreview(
                        paintContext, TerrainPaintUtilityEditor.BrushPreview.DestinationRenderTexture, editContext.brushTexture, brushXform, material, 1);
                }

                TerrainPaintUtility.ReleaseContextResources(paintContext);
            }
        }

        private void ApplyBrushInternal(PaintContext paintContext, float brushStrength, Texture brushTexture, BrushTransform brushXform)
        {
            Material mat = TerrainPaintUtility.GetBuiltinPaintMaterial();

            Vector4 brushParams = new Vector4(brushStrength * 0.01f, 0.5f * m_Height, 0.0f, 0.0f);
            mat.SetTexture("_BrushTex", brushTexture);
            mat.SetVector("_BrushParams", brushParams);

            TerrainPaintUtility.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);

            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, (int)TerrainPaintUtility.BuiltinPaintMaterialPasses.SetHeights);
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            if (Event.current.shift)
            {
                m_Height = terrain.terrainData.GetInterpolatedHeight(editContext.uv.x, editContext.uv.y) / terrain.terrainData.size.y;
                editContext.RepaintAllInspectors();
                return true;
            }

            BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.uv, editContext.brushSize, 0.0f);
            PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds());
            ApplyBrushInternal(paintContext, editContext.brushStrength, editContext.brushTexture, brushXform);
            TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Set Height");
            return true;
        }

        void Flatten(Terrain terrain)
        {
            Undo.RegisterCompleteObjectUndo(terrain.terrainData, terrain.gameObject.name);

            int w = terrain.terrainData.heightmapWidth;
            int h = terrain.terrainData.heightmapHeight;

            float[,] heights = new float[h, w];
            for (int y = 0; y < heights.GetLength(0); y++)
            {
                for (int x = 0; x < heights.GetLength(1); x++)
                {
                    heights[y, x] = m_Height;
                }
            }
            terrain.terrainData.SetHeights(0, 0, heights);
        }

        void FlattenAll()
        {
            foreach (Terrain t in Terrain.activeTerrains)
            {
                Flatten(t);
            }
        }

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            m_FlattenAll = EditorGUILayout.Toggle(new GUIContent("Flatten all", "If selected, it will traverse all neighbors and flatten them too"), m_FlattenAll);

            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal();
            m_Height = EditorGUILayout.Slider(new GUIContent("Height", "You can set the Height property manually or you can shift-click on the terrain to sample the height at the mouse position (rather like the 'eyedropper' tool in an image editor)."), m_Height * terrain.terrainData.size.y, 0, terrain.terrainData.size.y) / terrain.terrainData.size.y;
            if (GUILayout.Button(new GUIContent("Flatten", "The Flatten button levels the whole terrain to the chosen height."), GUILayout.ExpandWidth(false)))
            {
                if (m_FlattenAll)
                    FlattenAll();
                else
                    Flatten(terrain);
            }
            GUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
                Save(true);

            // show built-in brushes
            editContext.ShowBrushesGUI(5);
        }
    }
}
