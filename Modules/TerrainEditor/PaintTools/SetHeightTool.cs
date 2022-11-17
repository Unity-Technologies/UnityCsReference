// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.TerrainTools;
using UnityEditor.ShortcutManagement;

namespace UnityEditor.TerrainTools
{
    [FilePath("Library/TerrainTools/SetHeight", FilePathAttribute.Location.ProjectFolder)]
    internal class SetHeightTool : TerrainPaintToolWithOverlays<SetHeightTool>
    {
        private enum HeightSpace
        {
            World,
            Local
        }

        const string k_ToolName = "Set Height";
        public override string OnIcon => "TerrainOverlays/SetHeight_On.png";
        public override string OffIcon => "TerrainOverlays/SetHeight.png";

        [SerializeField] HeightSpace m_HeightSpace;
        [SerializeField] float m_TargetHeight;

        [FormerlyPrefKeyAs("Terrain/Set Height", "f2")]
        [Shortcut("Terrain/Set Height", typeof(TerrainToolShortcutContext), KeyCode.F2)]
        static void SelectShortcut(ShortcutArguments args)
        {
            TerrainToolShortcutContext context = (TerrainToolShortcutContext)args.context;
            context.SelectPaintToolWithOverlays<SetHeightTool>();
        }

        class Styles
        {
            public readonly GUIContent description = EditorGUIUtility.TrTextContent("Left click to set the height.\n\nHold shift and left click to sample the target height.");
            public readonly GUIContent space = EditorGUIUtility.TrTextContent("Space", "The heightmap space in which the painting operates.");
            public readonly GUIContent height = EditorGUIUtility.TrTextContent("Height", "You can set the Height property manually or you can shift-click on the terrain to sample the height at the mouse position (rather like the 'eyedropper' tool in an image editor).");
            public readonly GUIContent flatten = EditorGUIUtility.TrTextContent("Flatten Tile", "The Flatten button levels the whole terrain to the chosen height.");
            public readonly GUIContent flattenAll = EditorGUIUtility.TrTextContent("Flatten All", "If selected, it will traverse all neighbors and flatten them too");
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
            get { return (int) SculptIndex.SetHeight; }
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

                TerrainPaintUtilityEditor.DrawBrushPreview(paintContext, TerrainBrushPreviewMode.SourceRenderTexture, editContext.brushTexture, brushXform, material, 0);

                // draw result preview
                {
                    ApplyBrushInternal(paintContext, editContext.brushStrength, editContext.brushTexture, brushXform, terrain);

                    // restore old render target
                    RenderTexture.active = paintContext.oldRenderTexture;

                    material.SetTexture("_HeightmapOrig", paintContext.sourceRenderTexture);

                    TerrainPaintUtilityEditor.DrawBrushPreview(paintContext, TerrainBrushPreviewMode.DestinationRenderTexture, editContext.brushTexture, brushXform, material, 1);
                }

                TerrainPaintUtility.ReleaseContextResources(paintContext);
            }
        }

        private void ApplyBrushInternal(PaintContext paintContext, float brushStrength, Texture brushTexture, BrushTransform brushXform, Terrain terrain)
        {
            Material mat = TerrainPaintUtility.GetBuiltinPaintMaterial();

            float brushTargetHeight = Mathf.Clamp01((m_TargetHeight - paintContext.heightWorldSpaceMin) / paintContext.heightWorldSpaceSize);

            Vector4 brushParams = new Vector4(brushStrength * 0.01f, PaintContext.kNormalizedHeightScale * brushTargetHeight, 0.0f, 0.0f);
            mat.SetTexture("_BrushTex", brushTexture);
            mat.SetVector("_BrushParams", brushParams);

            TerrainPaintUtility.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);

            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, (int)TerrainBuiltinPaintMaterialPasses.SetHeights);
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            if (Event.current.shift)
            {
                m_TargetHeight = terrain.terrainData.GetInterpolatedHeight(editContext.uv.x, editContext.uv.y) + terrain.GetPosition().y;
                editContext.Repaint(RepaintFlags.UI);
                return true;
            }

            BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.uv, editContext.brushSize, 0.0f);
            PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds());
            ApplyBrushInternal(paintContext, editContext.brushStrength, editContext.brushTexture, brushXform, terrain);
            TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Set Height");
            return true;
        }

        void Flatten(Terrain terrain)
        {
            Undo.RegisterCompleteObjectUndo(terrain.terrainData, terrain.gameObject.name);

            float terrainHeight = Mathf.Clamp01((m_TargetHeight - terrain.transform.position.y) / terrain.terrainData.size.y);

            int res = terrain.terrainData.heightmapResolution;
            float[,] heights = new float[res, res];
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    heights[y, x] = terrainHeight;
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

        public override void OnToolSettingsGUI(Terrain terrain, IOnInspectorGUI editContext, bool overlays)
        {
            Styles styles = GetStyles();

            EditorGUI.BeginChangeCheck();
            {
                m_HeightSpace = (HeightSpace)EditorGUILayout.EnumPopup(styles.space, m_HeightSpace);
            }
            if (EditorGUI.EndChangeCheck())
            {
                if (m_HeightSpace == HeightSpace.Local)
                    m_TargetHeight = Mathf.Clamp(m_TargetHeight, terrain.GetPosition().y, terrain.terrainData.size.y + terrain.GetPosition().y);
            }

            if (m_HeightSpace == HeightSpace.Local)
                m_TargetHeight = EditorGUILayout.Slider(styles.height, m_TargetHeight - terrain.GetPosition().y, 0, terrain.terrainData.size.y) + terrain.GetPosition().y;
            else
                m_TargetHeight = EditorGUILayout.FloatField(styles.height, m_TargetHeight);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(styles.flatten, GUILayout.ExpandWidth(false)))
                Flatten(terrain);
            if (GUILayout.Button(styles.flattenAll, GUILayout.ExpandWidth(false)))
                FlattenAll();
            GUILayout.EndHorizontal();

        }

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext, bool overlays)
        {
            // show built-in brushes
            int textureRez = terrain.terrainData.heightmapResolution;
            editContext.ShowBrushesGUI(5, BrushGUIEditFlags.All, textureRez);

            OnToolSettingsGUI(terrain, editContext, overlays);
        }

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            OnInspectorGUI(terrain, editContext, false);
        }
    }
}
