// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
    [FilePathAttribute("Library/TerrainTools/PaintTexture", FilePathAttribute.Location.ProjectFolder)]
    public class PaintTextureTool : TerrainPaintTool<PaintTextureTool>
    {
        MaterialEditor m_TemplateMaterialEditor = null;
        [SerializeField]
        bool m_ShowMaterialEditor = false;

        [SerializeField]
        TerrainLayer m_SelectedTerrainLayer = null;
        TerrainLayerInspector m_SelectedTerrainLayerInspector = null;
        [SerializeField]
        bool m_ShowLayerEditor = false;

        public override string GetName()
        {
            return "Paint Texture";
        }

        public override string GetDesc()
        {
            return "Paints the selected material layer onto the terrain texture";
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

            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, (int)TerrainPaintUtility.BuiltinPaintMaterialPasses.PaintTexture);

            TerrainPaintUtility.EndPaintTexture(paintContext, "Terrain Paint - Texture");
            return true;
        }

        public override void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext)
        {
            // We're only doing painting operations, early out if it's not a repaint
            if (Event.current.type != EventType.Repaint)
                return;

            if (editContext.hitValidTerrain)
            {
                BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.raycastHit.textureCoord, editContext.brushSize, 0.0f);
                PaintContext ctx = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);
                TerrainPaintUtilityEditor.DrawBrushPreview(ctx, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture, editContext.brushTexture, brushXform, TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial(), 0);
                TerrainPaintUtility.ReleaseContextResources(ctx);
            }
        }

        private void DrawFoldoutEditor(Editor editor, int controlId, ref bool visible)
        {
            Rect titleRect = Editor.DrawHeaderGUI(editor, editor.target.name);
            int id = GUIUtility.GetControlID(controlId, FocusType.Passive);

            Rect renderRect = EditorGUI.GetInspectorTitleBarObjectFoldoutRenderRect(titleRect);
            renderRect.y = titleRect.yMax - 17f; // align with bottom
            bool newVisible = EditorGUI.DoObjectFoldout(visible, titleRect, renderRect, editor.targets, id);
            // Toggle visibility
            if (newVisible != visible)
            {
                UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(editor.target, newVisible);
                visible = newVisible;
                Save(true);
            }

            if (newVisible)
            {
                editor.OnInspectorGUI();
                EditorGUILayout.Space();
            }
        }

        private const int kTemplateMaterialEditorControl = 67890;
        private const int kSelectedTerrainLayerEditorControl = 67891;

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            GUILayout.Label("Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();
            if (m_TemplateMaterialEditor != null && m_TemplateMaterialEditor.target != terrain.materialTemplate)
            {
                UnityEngine.Object.DestroyImmediate(m_TemplateMaterialEditor);
                m_TemplateMaterialEditor = null;
            }
            if (m_TemplateMaterialEditor == null && terrain.materialTemplate != null)
            {
                m_TemplateMaterialEditor = Editor.CreateEditor(terrain.materialTemplate) as MaterialEditor;
                m_TemplateMaterialEditor.firstInspectedEditor = true;
            }

            if (m_TemplateMaterialEditor != null)
            {
                DrawFoldoutEditor(m_TemplateMaterialEditor, kTemplateMaterialEditorControl, ref m_ShowMaterialEditor);
                EditorGUILayout.Space();
            }

            int layerIndex = TerrainPaintUtility.FindTerrainLayerIndex(terrain, m_SelectedTerrainLayer);
            layerIndex = TerrainLayerUtility.ShowTerrainLayersSelectionHelper(terrain, layerIndex);
            EditorGUILayout.Space();

            if (EditorGUI.EndChangeCheck())
            {
                if (layerIndex != -1)
                    m_SelectedTerrainLayer = terrain.terrainData.terrainLayers[layerIndex];
                else
                    m_SelectedTerrainLayer = null;

                if (m_SelectedTerrainLayerInspector != null)
                {
                    UnityEngine.Object.DestroyImmediate(m_SelectedTerrainLayerInspector);
                    m_SelectedTerrainLayerInspector = null;
                }
                if (m_SelectedTerrainLayer != null)
                    m_SelectedTerrainLayerInspector = Editor.CreateEditor(m_SelectedTerrainLayer) as TerrainLayerInspector;

                Save(true);
            }

            if (m_SelectedTerrainLayerInspector != null)
            {
                var terrainLayerCustomUI = m_TemplateMaterialEditor?.m_CustomShaderGUI as ITerrainLayerCustomUI;
                if (terrainLayerCustomUI != null)
                    m_SelectedTerrainLayerInspector.SetCustomUI(terrainLayerCustomUI, terrain);

                DrawFoldoutEditor(m_SelectedTerrainLayerInspector, kSelectedTerrainLayerEditorControl, ref m_ShowLayerEditor);
                EditorGUILayout.Space();
            }
            editContext.ShowBrushesGUI(5);
        }
    }
}
