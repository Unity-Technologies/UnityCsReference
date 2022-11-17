// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEngine.TerrainTools;
using UnityEditor.ShortcutManagement;

namespace UnityEditor.TerrainTools
{
    internal class PaintHolesTool : TerrainPaintToolWithOverlays<PaintHolesTool>
    {
        const string k_ToolName = "Paint Holes";
        public override string OnIcon => "TerrainOverlays/Holes_On.png";
        public override string OffIcon => "TerrainOverlays/Holes.png";
        [Shortcut("Terrain/Paint Holes", typeof(TerrainToolShortcutContext), KeyCode.F8)]
        static void SelectShortcut(ShortcutArguments args)
        {
            TerrainToolShortcutContext context = (TerrainToolShortcutContext)args.context;
            context.SelectPaintToolWithOverlays<PaintHolesTool>();
        }
        public override int IconIndex
        {
            get { return (int) SculptIndex.Holes; }
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
            return "Left click to paint a hole.\n\nHold shift and left click to erase it.";
        }

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext, bool overlays)
        {
            int textureRez = terrain.terrainData.holesResolution;
            editContext.ShowBrushesGUI(5, BrushGUIEditFlags.All, textureRez);
        }

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            OnInspectorGUI(terrain, editContext, false);
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
            Vector2 halfTexelOffset = new Vector2(0.5f / terrain.terrainData.holesResolution, 0.5f / terrain.terrainData.holesResolution);
            BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.uv - halfTexelOffset, editContext.brushSize, 0.0f);
            PaintContext paintContext = TerrainPaintUtility.BeginPaintHoles(terrain, brushXform.GetBrushXYBounds());

            Material mat = TerrainPaintUtility.GetBuiltinPaintMaterial();

            float brushStrength = Event.current.shift ? editContext.brushStrength : -editContext.brushStrength;
            Vector4 brushParams = new Vector4(brushStrength, 0.0f, 0.0f, 0.0f);
            mat.SetTexture("_BrushTex", editContext.brushTexture);
            mat.SetVector("_BrushParams", brushParams);

            TerrainPaintUtility.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);

            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, (int)TerrainBuiltinPaintMaterialPasses.PaintHoles);

            TerrainPaintUtility.EndPaintHoles(paintContext, "Terrain Paint - Paint Holes");
            return true;
        }
    }
}
