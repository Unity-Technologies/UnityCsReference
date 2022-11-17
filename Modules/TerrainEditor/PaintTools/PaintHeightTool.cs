// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEngine.TerrainTools;
using UnityEditor.ShortcutManagement;

namespace UnityEditor.TerrainTools
{
    internal class PaintHeightTool : TerrainPaintToolWithOverlays<PaintHeightTool>
    {
        internal const string k_ToolName = "Raise or Lower Terrain";
        public override string OnIcon => "TerrainOverlays/PaintHeight_On.png";
        public override string OffIcon => "TerrainOverlays/PaintHeight.png";

        [FormerlyPrefKeyAs("Terrain/Raise Height", "f1")]
        [Shortcut("Terrain/Raise or Lower Terrain", typeof(TerrainToolShortcutContext), KeyCode.F1)]
        static void SelectShortcut(ShortcutArguments args)
        {
            TerrainToolShortcutContext context = (TerrainToolShortcutContext)args.context;
            context.SelectPaintToolWithOverlays<PaintHeightTool>();
        }

        class Styles
        {
            public readonly GUIContent description = EditorGUIUtility.TrTextContent("Left click to raise.\n\nHold shift and left click to lower.");
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
        public override int IconIndex
        {
            get { return (int) SculptIndex.PaintHeight; }
        }

        public override TerrainCategory Category
        {
            get { return TerrainCategory.Sculpt; }
        }
        public override bool HasBrushMask => true;
        public override bool HasBrushAttributes => true;

        public override string GetName()
        {
            return k_ToolName;
        }

        public override string GetDescription()
        {
            return GetStyles().description.text;
        }

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext, bool overlays)
        {
            int textureRez = terrain.terrainData.heightmapResolution;
            editContext.ShowBrushesGUI(5, BrushGUIEditFlags.All, textureRez);
        }

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            OnInspectorGUI(terrain, editContext, false);
        }

        private void ApplyBrushInternal(PaintContext paintContext, float brushStrength, Texture brushTexture, BrushTransform brushXform)
        {
            Material mat = TerrainPaintUtility.GetBuiltinPaintMaterial();

            brushStrength = Event.current.shift ? -brushStrength : brushStrength;
            Vector4 brushParams = new Vector4(0.01f * brushStrength, 0.0f, 0.0f, 0.0f);
            mat.SetTexture("_BrushTex", brushTexture);
            mat.SetVector("_BrushParams", brushParams);

            TerrainPaintUtility.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);

            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, (int)TerrainBuiltinPaintMaterialPasses.RaiseLowerHeight);
        }

        public override void OnRenderBrushPreview(Terrain terrain, IOnSceneGUI editContext)
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
                    paintContext, TerrainBrushPreviewMode.SourceRenderTexture, editContext.brushTexture, brushXform, material, 0);

                // draw result preview
                {
                    ApplyBrushInternal(paintContext, editContext.brushStrength, editContext.brushTexture, brushXform);

                    // restore old render target
                    RenderTexture.active = paintContext.oldRenderTexture;

                    material.SetTexture("_HeightmapOrig", paintContext.sourceRenderTexture);
                    TerrainPaintUtilityEditor.DrawBrushPreview(
                        paintContext, TerrainBrushPreviewMode.DestinationRenderTexture, editContext.brushTexture, brushXform, material, 1);
                }

                TerrainPaintUtility.ReleaseContextResources(paintContext);
            }
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.uv, editContext.brushSize, 0.0f);
            PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds());
            ApplyBrushInternal(paintContext, editContext.brushStrength, editContext.brushTexture, brushXform);
            TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Raise or Lower Height");
            return true;
        }
    }
}
