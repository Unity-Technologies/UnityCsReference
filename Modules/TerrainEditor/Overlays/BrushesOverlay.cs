// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.PackageManager;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.EditorTools;

namespace UnityEditor.TerrainTools
{
    public static class BrushesOverlay
    {

        public static bool IsSelectedObjectTerrain()
        {
            return Selection.activeGameObject && Selection.activeGameObject.GetComponent<Terrain>();
        }

        // returns null if the tool is not a terrain tool
        // this is used in terrain tools package
        public static EditorTool ActiveTerrainTool
        {
            get { return EditorToolManager.GetActiveTool() as TerrainPaintToolWithOverlaysBase; }
        }

        // return false if the tool is not valid
        internal static bool IsToolValid(ref TerrainPaintToolWithOverlaysBase tool)
        {
            if (!tool)
            {
                return false;
            }
            if (!tool.Terrain)
            {
                if (Selection.activeGameObject == null || Selection.activeGameObject.GetComponent<Terrain>() == null) return false; // check when loading and unloading packages
                tool.Terrain = Selection.activeGameObject.GetComponent<Terrain>();
            }
            if (!tool.Terrain)
            {
                Debug.LogError("Tool does NOT have associated terrain");
                return false;
            }

            return true;
        }


        // this static function is for displaying the brush mask
        // can also be used for opacity and size (which is currently displayed through BrushAttributesOverlay.cs)
        internal static void OnGUI(BrushGUIEditFlags flags = BrushGUIEditFlags.All)
        {
            Vector2 scrollPos = new Vector2();
            var tool = EditorToolManager.GetActiveTool() as TerrainPaintToolWithOverlaysBase;
            if (!IsToolValid(ref tool)) return;
            if (TerrainInspector.s_activeTerrainInspectorInstance == null) return; // another check for switching between packages
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            if (tool.HasBrushMask) // todo: i think this check is no longer necessary. delete it?
            {
                // show the brush masks
                if (tool.Terrain != null && tool.Terrain.terrainData != null)
                {
                    int textureRez = tool.Terrain.terrainData.heightmapResolution;
                    IOnInspectorGUI editContext = new OnInspectorGUIContext();
                    editContext.ShowBrushesGUI(10, flags, textureRez);
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }


    // brush masks ----------------
    [Overlay(typeof(SceneView), "Brush Masks")]
    [Icon("TerrainOverlays/BrushSettingIcons/BrushMask.png")]
    internal class BrushMaskOverlay : ToolbarOverlay, ITransientOverlay
    {
        // determines whether the toolbar should be visible or not
        // only visible for tools which are sculpt or materials or details
        public bool visible
        {
            get
            {
                var currTool = TerrainInspector.GetActiveTerrainTool() as ITerrainPaintToolWithOverlays;
                if (currTool == null)
                    return false;
                return currTool.HasBrushMask && BrushesOverlay.IsSelectedObjectTerrain();
            }
        }

        public override VisualElement CreatePanelContent()
        {
            return new BrushMaskToolbar();
        }

        protected internal override Layout supportedLayouts
        {
            get
            {
                var ret = Layout.Panel;
                return ret;
            }
        }
    }

    [EditorToolbarElement("BrushMaskToolbar", typeof(SceneView))]
    internal class BrushMaskToolbar : OverlayToolbar
    {
        VisualElement m_RootElement;

        public BrushMaskToolbar()
        {
            m_RootElement = new VisualElement();

            IMGUIContainer img = new IMGUIContainer();
            img.style.minHeight = 200;
            img.style.minWidth = 300;

            img.onGUIHandler = () =>
            {
                // adjust brush mask width here to force brush mask sizes
                EditorGUIUtility.currentViewWidth = 430;
                BrushesOverlay.OnGUI(BrushGUIEditFlags.SelectAndInspect);
            };
            m_RootElement.Add(img);
            Add(m_RootElement);
        }
    }

}
