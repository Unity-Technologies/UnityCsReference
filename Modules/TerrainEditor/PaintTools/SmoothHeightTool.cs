// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEngine.TerrainTools;
using UnityEditor.ShortcutManagement;

namespace UnityEditor.TerrainTools
{
    internal class SmoothHeightTool : TerrainPaintToolWithOverlays<SmoothHeightTool>
    {
        const string k_ToolName = "Smooth Height";
        public override string OnIcon => "TerrainOverlays/Smooth_On.png";
        public override string OffIcon => "TerrainOverlays/Smooth.png";

        [SerializeField]
        public float m_direction = 0.0f;     // -1 to 1

        class Styles
        {
            public readonly GUIContent description = EditorGUIUtility.TrTextContent("Click to smooth the terrain height.");
            public readonly GUIContent direction = EditorGUIUtility.TrTextContent("Blur Direction", "Blur only up (1.0), only down (-1.0) or both (0.0)");
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

        [FormerlyPrefKeyAs("Terrain/Smooth Height", "f3")]
        [Shortcut("Terrain/Smooth Height", typeof(TerrainToolShortcutContext), KeyCode.F3)]
        static void SelectShortcut(ShortcutArguments args)
        {
            TerrainToolShortcutContext context = (TerrainToolShortcutContext)args.context;
            context.SelectPaintToolWithOverlays<SmoothHeightTool>();
        }

        public override int IconIndex
        {
            get { return (int) SculptIndex.Smooth; }
        }

        public override TerrainCategory Category
        {
            get { return TerrainCategory.Sculpt; }
        }

        public override string GetName()
        {
            return k_ToolName;
        }

        public override string GetDescription()
        {
            return GetStyles().description.text;
        }

        public override bool HasToolSettings => true;
        public override bool HasBrushMask => true;
        public override bool HasBrushAttributes => true;

        public override void OnToolSettingsGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            Styles styles = GetStyles();
            m_direction = EditorGUILayout.Slider(styles.direction, m_direction, -1.0f, 1.0f);
        }

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            OnToolSettingsGUI(terrain, editContext);

            int textureRez = terrain.terrainData.heightmapResolution;
            editContext.ShowBrushesGUI(5, BrushGUIEditFlags.All, textureRez);
        }

        private void ApplyBrushInternal(PaintContext paintContext, float brushStrength, Texture brushTexture, BrushTransform brushXform)
        {
            Material mat = TerrainPaintUtility.GetBuiltinPaintMaterial();

            Vector4 brushParams = new Vector4(brushStrength, 0.0f, 0.0f, 0.0f);
            mat.SetTexture("_BrushTex", brushTexture);
            mat.SetVector("_BrushParams", brushParams);
            Vector4 smoothWeights = new Vector4(
                Mathf.Clamp01(1.0f - Mathf.Abs(m_direction)),   // centered
                Mathf.Clamp01(-m_direction),                    // min
                Mathf.Clamp01(m_direction),                     // max
                0.0f);                                          // unused
            mat.SetVector("_SmoothWeights", smoothWeights);
            TerrainPaintUtility.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);

            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, (int)TerrainBuiltinPaintMaterialPasses.SmoothHeights);
        }

        public override void OnRenderBrushPreview(Terrain terrain, IOnSceneGUI editContext)
        {
            // We're only doing painting operations, early out if it's not a repaint
            if (Event.current.type != EventType.Repaint)
                return;

            if (editContext.hitValidTerrain)
            {
                BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.raycastHit.textureCoord, editContext.brushSize, 0.0f);
                PaintContext ctx = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);
                TerrainPaintUtilityEditor.DrawBrushPreview(ctx, TerrainBrushPreviewMode.SourceRenderTexture, editContext.brushTexture, brushXform, TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial(), 0);
                TerrainPaintUtility.ReleaseContextResources(ctx);
            }
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.uv, editContext.brushSize, 0.0f);
            PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds());
            ApplyBrushInternal(paintContext, editContext.brushStrength, editContext.brushTexture, brushXform);
            TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Smooth Height");
            return true;
        }
    }
}
