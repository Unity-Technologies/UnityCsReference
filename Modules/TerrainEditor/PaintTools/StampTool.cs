// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.ShortcutManagement;

namespace UnityEditor.Experimental.TerrainAPI
{
    [FilePathAttribute("Library/TerrainTools/Stamp", FilePathAttribute.Location.ProjectFolder)]
    internal class StampTool : TerrainPaintTool<StampTool>
    {
        const string toolName = "Stamp Terrain";

        [SerializeField]
        float m_StampHeightTerrainSpace = 0.0f;

        [SerializeField]
        float m_MaxBlendAdd = 0.0f;

        [Shortcut("Terrain/Stamp Terrain", typeof(TerrainToolShortcutContext))]
        static void SelectShortcut(ShortcutArguments args)
        {
            TerrainToolShortcutContext context = (TerrainToolShortcutContext)args.context;
            context.SelectPaintTool<StampTool>();
        }

        class Styles
        {
            public readonly GUIContent description = EditorGUIUtility.TrTextContent("Left click to stamp the brush onto the terrain.\n\nHold control and mousewheel to adjust height.\nHold shift to invert the stamp.");
            public readonly GUIContent height = EditorGUIUtility.TrTextContent("Stamp Height", "You can set the Stamp Height manually or you can hold shift and mouse wheel on the terrain to adjust it.");
            public readonly GUIContent down = EditorGUIUtility.TrTextContent("Subtract", "Subtract the stamp from the terrain.");
            public readonly GUIContent maxadd = EditorGUIUtility.TrTextContent("Max <--> Add", "Blend between adding the heights and taking the maximum.");
        }

        private static Styles m_styles;
        private Styles GetStyles()
        {
            if (m_styles == null)
            {
                m_styles = new Styles();
            }
            return m_styles;
        }

        public override string GetName()
        {
            return toolName;
        }

        public override string GetDesc()
        {
            return GetStyles().description.text;
        }

        private void ApplyBrushInternal(PaintContext paintContext, float brushStrength, Texture brushTexture, BrushTransform brushXform, Terrain terrain, bool negate)
        {
            Material mat = TerrainPaintUtility.GetBuiltinPaintMaterial();

            float height = m_StampHeightTerrainSpace / terrain.terrainData.size.y;
            if (negate)
            {
                height = -height;
            }
            Vector4 brushParams = new Vector4(brushStrength, 0.0f, height, m_MaxBlendAdd);
            mat.SetTexture("_BrushTex", brushTexture);
            mat.SetVector("_BrushParams", brushParams);

            TerrainPaintUtility.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);

            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, (int)TerrainPaintUtility.BuiltinPaintMaterialPasses.StampHeight);
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            // ignore mouse drags
            if (Event.current.type == EventType.MouseDrag)
                return true;

            BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.uv, editContext.brushSize, 0.0f);
            PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds());
            ApplyBrushInternal(paintContext, editContext.brushStrength, editContext.brushTexture, brushXform, terrain, Event.current.shift);
            TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Stamp");
            return true;
        }

        public override void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext)
        {
            Event evt = Event.current;
            if (evt.control && (evt.type == EventType.ScrollWheel))
            {
                const float k_mouseWheelToHeightRatio = -0.0004f;
                // we use distance to modify the scroll speed, so that when a user is up close to the brush, they get fine adjustment, and when the user is far from the brush, it adjusts quickly
                m_StampHeightTerrainSpace += Event.current.delta.y * k_mouseWheelToHeightRatio * editContext.raycastHit.distance;
                evt.Use();
                editContext.Repaint();
            }

            // We're only doing painting operations, early out if it's not a repaint
            if (evt.type != EventType.Repaint)
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
                    ApplyBrushInternal(paintContext, editContext.brushStrength, editContext.brushTexture, brushXform, terrain, evt.shift);

                    // restore old render target
                    RenderTexture.active = paintContext.oldRenderTexture;

                    material.SetTexture("_HeightmapOrig", paintContext.sourceRenderTexture);

                    TerrainPaintUtilityEditor.DrawBrushPreview(
                        paintContext, TerrainPaintUtilityEditor.BrushPreview.DestinationRenderTexture, editContext.brushTexture, brushXform, material, 1);
                }

                TerrainPaintUtility.ReleaseContextResources(paintContext);
            }
        }

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            Styles styles = GetStyles();
            EditorGUI.BeginChangeCheck();
            {
                EditorGUI.BeginChangeCheck();
                float height = Mathf.Abs(m_StampHeightTerrainSpace);
                bool stampDown = (m_StampHeightTerrainSpace < 0.0f);
                height = EditorGUILayout.PowerSlider(styles.height, height, 0, terrain.terrainData.size.y, 2.0f);
                stampDown = EditorGUILayout.Toggle(styles.down, stampDown);
                if (EditorGUI.EndChangeCheck())
                {
                    m_StampHeightTerrainSpace = (stampDown ? -height : height);
                }
            }
            m_MaxBlendAdd = EditorGUILayout.Slider(styles.maxadd, m_MaxBlendAdd, 0.0f, 1.0f);
            if (EditorGUI.EndChangeCheck())
            {
                Save(true);
            }

            // show built-in brushes
            editContext.ShowBrushesGUI(5);
            base.OnInspectorGUI(terrain, editContext);
        }
    }
}
