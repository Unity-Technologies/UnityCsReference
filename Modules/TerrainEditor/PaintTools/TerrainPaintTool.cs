// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.TerrainTools;

namespace UnityEditor.TerrainTools
{
    [MovedFrom("UnityEditor.Experimental.TerrainAPI")]
    [Flags]
    public enum BrushGUIEditFlags
    {
        None = 0,
        Select = 1,
        Inspect = 2,
        Size = 4,
        Opacity = 8,
        SelectAndInspect = 3,
        All = 15,
    }

    [MovedFrom("UnityEditor.Experimental.TerrainAPI")]
    [Flags]
    public enum RepaintFlags
    {
        UI = 1,
        Scene = 2,
    }

    [MovedFrom("UnityEditor.Experimental.TerrainAPI")]
    public interface IOnPaint
    {
        Texture brushTexture { get; }
        Vector2 uv { get; }
        float brushStrength { get; }
        float brushSize { get; }
        bool hitValidTerrain { get; }
        RaycastHit raycastHit { get;  }

        [Obsolete("IOnPaint.RepaintAllInspectors has been deprecated. Use IOnPaint.Repaint instead")]
        void RepaintAllInspectors();
        void Repaint(RepaintFlags flags = RepaintFlags.UI);
    }

    [MovedFrom("UnityEditor.Experimental.TerrainAPI")]
    public interface IOnSceneGUI
    {
        SceneView sceneView { get; }
        Texture brushTexture { get; }
        float brushStrength { get; }
        float brushSize { get; }
        bool hitValidTerrain { get; }
        RaycastHit raycastHit { get;  }
        int controlId { get; }

        void Repaint(RepaintFlags flags = RepaintFlags.UI);
    }

    [MovedFrom("UnityEditor.Experimental.TerrainAPI")]
    public interface IOnInspectorGUI
    {
        void ShowBrushesGUI(int spacing = 5, BrushGUIEditFlags flags = BrushGUIEditFlags.All, int textureResolutionPerTile = 0);
        void Repaint(RepaintFlags flags = RepaintFlags.UI);
    }

    internal interface ITerrainPaintTool
    {
        string GetName();
        string GetDescription();
        void OnEnable();
        void OnDisable();
        void OnEnterToolMode();
        void OnExitToolMode();
        void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext);
        void OnRenderBrushPreview(Terrain terrain, IOnSceneGUI editContext);
        void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext);
        bool OnPaint(Terrain terrain, IOnPaint editContext);
    }

    [MovedFrom("UnityEditor.Experimental.TerrainAPI")]
    public abstract class TerrainPaintTool<T> : ScriptableSingleton<T>, ITerrainPaintTool where T : TerrainPaintTool<T>
    {
        public abstract string GetName();
        public abstract string GetDescription();
        public virtual void OnEnable() {}
        public virtual void OnDisable() {}
        public virtual void OnEnterToolMode() {}
        public virtual void OnExitToolMode() {}
        public virtual void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext) {}
        public virtual void OnRenderBrushPreview(Terrain terrain, IOnSceneGUI editContext) {}
        public virtual void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext) {}
        public virtual bool OnPaint(Terrain terrain, IOnPaint editContext) { return false; }
    }

    internal interface ITerrainPaintToolWithOverlays : ITerrainPaintTool
    {
        string OnIcon { get; }

        string OffIcon { get; }

        int IconIndex { get { return 0;} }

        TerrainCategory Category { get { return TerrainCategory.CustomBrushes; } }

        bool HasBrushMask => false;

        bool HasBrushAttributes => false;

        bool HasToolSettings => false;

        // function for drawing GUI for specific tool
        void OnToolSettingsGUI(Terrain terrain, IOnInspectorGUI editContext, bool overlays);

        void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext, bool overlays);
    }

    public enum TerrainCategory
    {
        Sculpt,
        Materials,
        Foliage,
        NeighborTerrains,
        CustomBrushes,
    }

    // the indices are incremented by 10 so that customTools/terrainTools can have values that slip in between them
    public enum SculptIndex
    {
        PaintHeight = 100,
        SetHeight = 101,
        Holes = 103,
        Stamp = 201,
        Smooth = 305,
    }

    public enum MaterialIndex
    {
        PaintTexture = 10,
    }

    public enum FoliageIndex
    {
        PaintDetails = 10,
        PaintTrees = 20,
    }

    public enum NeighborTerrainsIndex
    {
        CreateTerrain = 10,
    }

    public abstract class TerrainPaintToolWithOverlays<T> : TerrainPaintToolWithOverlaysBase where T : TerrainPaintToolWithOverlays<T>
    {
    }

    // TerrainPaintTool is the old paint tool type which inherits from ScriptableSingleton
    // TerrainPaintToolWithOverlays is a new type of paint tool which inherits from EditorTools and is used in Overlays
    // there are additional variables that come with this new type, such as TerrainCategory and HasToolSettings
    public abstract class TerrainPaintToolWithOverlaysBase : EditorTools.EditorTool, ITerrainPaintToolWithOverlays
    {
        static OnSceneGUIContext s_OnSceneGUIContext = new OnSceneGUIContext(null, new RaycastHit(), null, 0.0f, 0.0f, 0);
        static OnPaintContext s_OnPaintContext = new OnPaintContext(new RaycastHit(), null, Vector2.zero, 0.0f, 0.0f);

        static Terrain s_LastActiveTerrain;

        public virtual string OnIcon => "TerrainOverlays/CustomBrushes_On.png";
        public virtual string OffIcon => "TerrainOverlays/CustomBrushes.png";

        public virtual int IconIndex
        {
            get { return 0; }
        }

        public virtual TerrainCategory Category
        {
            get { return TerrainCategory.CustomBrushes; }
        }
        // the categories that exist so far
        // Sculpt
        // Materials
        // Foliage
        // NeighborTerrains
        // CustomBrushes

        public virtual bool HasToolSettings => false;
        public virtual bool HasBrushMask => false;
        public virtual bool HasBrushAttributes => false;

        // function for drawing GUI for specific tool
        public virtual void OnToolSettingsGUI(Terrain terrain, IOnInspectorGUI editContext, bool overlays) { }
        public Terrain Terrain { get; set; }
        public abstract string GetName();
        public abstract string GetDescription();
        public virtual void OnEnable() {}
        public virtual void OnDisable() {}
        public virtual void OnEnterToolMode() { }
        public virtual void OnExitToolMode() { }
        public virtual void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext) { }
        public virtual void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext, bool overlays) { }
        public virtual void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext) { }
        public virtual void OnRenderBrushPreview(Terrain terrain, IOnSceneGUI editContext) { }
        public virtual bool OnPaint(Terrain terrain, IOnPaint editContext) { return false; }

        public override void OnActivated()
        {
            OnEnterToolMode();
        }

        public override void OnWillBeDeactivated()
        {
            PaintContext.ApplyDelayedActions();
            OnExitToolMode();
        }

        public override void OnToolGUI(EditorWindow window)
        {
            Terrain = null;
            // currently this is always true but assert in case it changes in the future
            var sceneView = (SceneView)window;
            Debug.Assert(sceneView != null, $"TerrainTool::OnToolGUI - SceneView window is null. This is being called from a different type of window: {window.GetType()}");

            var editor = TerrainInspector.s_activeTerrainInspectorInstance;
            // this can happen if the user selects the terrain tool from the tool history without
            // having a terrain object selected
            if(editor == null)
            {
                return;
            }

            // this happens for one frame after terrain is deselected or when the
            // user is using box selection in the sceneview.
            // target might also be null if the user has selected the terrain tool
            // from the editor tool history and doesn't have a terrain selected
            if (target == null)
            {
                return;
            }

            // there's a chance target could be a GameObject or a Terrain depending on
            // what we do with Selection.activeObject. Might be GameObject at first but
            // then we set activeObject to Terrain sometimes during painting.
            if(target.GetType() == typeof(GameObject))
            {
                Terrain = ((GameObject)target).GetComponent<Terrain>();
            }
            else if(target.GetType() == typeof(Terrain))
            {
                Terrain = (Terrain)target;
            }

            Event e = Event.current;

            TerrainInspector.RaycastAllTerrains(out var hitTerrain, out var raycastHit);

            Texture brushTexture = this.GetType() == typeof(PaintTreesTool) ? editor.brushList.GetCircleBrush().texture : editor.brushList.GetActiveBrush().texture;
            Vector2 uv = raycastHit.textureCoord;

            bool hitValidTerrain = (hitTerrain != null && hitTerrain.terrainData != null);
            if (hitValidTerrain)
            {
                editor.HotkeyApply(raycastHit.distance);
            }

            int id = GUIUtility.GetControlID(TerrainInspector.s_TerrainEditorHash, FocusType.Passive);
            // last active terrain is used in case user is in the middle of a painting operation but the mouse is no longer hovering over a terrain tile,
            // in which case hitTerrain will be null. we want to permit the continuation of the current painting op in this situation

            // if m_terrain is null then try to get the activeGameobject if it is a terrain
            if (!Terrain && Selection.activeGameObject && Selection.activeGameObject.GetComponent<Terrain>())
            {
                Terrain = Selection.activeGameObject.GetComponent<Terrain>();
            }

            // set the last hit terrain
            Terrain lastActiveTerrain = hitValidTerrain ? hitTerrain : s_LastActiveTerrain;
            if (lastActiveTerrain)
            {
                OnSceneGUI(lastActiveTerrain, s_OnSceneGUIContext.Set(sceneView, hitValidTerrain, raycastHit, brushTexture, editor.brushStrength, editor.brushSize, id));

                if (e.type == EventType.Repaint)
                {
                    var mousePos = Event.current.mousePosition;
                    var cameraRect = sceneView.cameraViewport;
                    cameraRect.y = 0;
                    var isMouseInSceneView = cameraRect.Contains(mousePos);
                    if (EditorGUIUtility.hotControl == id || (isMouseInSceneView && EditorGUIUtility.hotControl == 0))
                    {
                        OnRenderBrushPreview(lastActiveTerrain, s_OnSceneGUIContext);
                    }
                }
            }

            var eventType = e.GetTypeForControl(id);
            if (!hitValidTerrain)
            {
                // if we release the mouse button outside the terrain we still need to update the terrains
                if (eventType == EventType.MouseUp)
                    PaintContext.ApplyDelayedActions();

                return;
            }

            s_LastActiveTerrain = hitTerrain;

            // user might start painting on one Terrain but end painting on another, and that Terrain needs to be the new selection
            bool changeSelection = false;
            switch (eventType)
            {
                case EventType.Layout:
                    HandleUtility.AddDefaultControl(id);
                    break;

                case EventType.MouseMove:
                    if (hitTerrain)
                    {
                        HandleUtility.Repaint();
                    }
                    break;

                case EventType.MouseDown:
                case EventType.MouseDrag:
                    {
                        if(EditorGUIUtility.hotControl != id)
                        {
                            if(EditorGUIUtility.hotControl != 0 || eventType == EventType.MouseDrag)
                            {
                                return;
                            }
                        }

                        // If user is ALT-dragging, we want to return to main routine
                        if (e.alt)
                            return;

                        // allow painting with LMB only
                        if (e.button != 0)
                        {
                            return;
                        }

                        HandleUtility.AddDefaultControl(id);

                        if (HandleUtility.nearestControl != id)
                        {
                            return;
                        }

                        if (e.type == EventType.MouseDown)
                        {
                            EditorGUIUtility.hotControl = id;
                        }

                        if (OnPaint(hitTerrain, s_OnPaintContext.Set(hitValidTerrain, raycastHit, brushTexture, uv, editor.brushStrength, editor.brushSize)))
                        {
                            hitTerrain.editorRenderFlags = TerrainRenderFlags.Heightmap;
                        }

                        if (Terrain != hitTerrain && e.type == EventType.MouseDown)
                        {
                            changeSelection = true;
                        }

                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    {
                        if (GUIUtility.hotControl != id) return;
                        GUIUtility.hotControl = 0;
                        if (Terrain != hitTerrain) changeSelection = true;
                        PaintContext.ApplyDelayedActions();
                        e.Use();
                    }
                    break;
            }

            if (changeSelection) Selection.activeObject = hitTerrain;
        }
    }

    internal class OnPaintContext : IOnPaint
    {
        internal Texture m_BrushTexture = null;
        internal Vector2 m_UV = Vector2.zero;
        internal float m_BrushStrength = 0.0f;
        internal float m_BrushSize = 0;
        internal bool m_HitValidTerrain = false;
        internal RaycastHit m_RaycastHit;

        public OnPaintContext(RaycastHit raycastHit, Texture brushTexture, Vector2 uv, float brushStrength, float brushSize)
        {
            Set(false, raycastHit, brushTexture, uv, brushStrength, brushSize);
        }

        public OnPaintContext Set(bool hitValidTerrain, RaycastHit raycastHit, Texture brushTexture, Vector2 uv, float brushStrength, float brushSize)
        {
            m_BrushTexture = brushTexture;
            m_UV = uv;
            m_BrushStrength = brushStrength;
            m_BrushSize = brushSize;
            m_HitValidTerrain = hitValidTerrain;
            m_RaycastHit = raycastHit;
            return this;
        }

        public Texture brushTexture { get { return m_BrushTexture; } }
        public Vector2 uv { get { return m_UV; } }
        public float brushStrength { get { return m_BrushStrength; } }
        public float brushSize { get { return m_BrushSize; } }
        public bool hitValidTerrain { get { return m_HitValidTerrain; } }
        public RaycastHit raycastHit { get { return m_RaycastHit; } }

        public void RepaintAllInspectors()
        {
            Repaint(RepaintFlags.UI);
        }

        public void Repaint(RepaintFlags flags)
        {
            if ((flags & RepaintFlags.UI) != 0)
            {
                InspectorWindow.RepaintAllInspectors();
            }

            if ((flags & RepaintFlags.Scene) != 0)
            {
                EditorApplication.SetSceneRepaintDirty();
            }
        }
    }

    internal class OnSceneGUIContext : IOnSceneGUI
    {
        internal SceneView m_SceneView = null;
        internal Texture m_BrushTexture = null;
        internal float m_BrushStrength = 0.0f;
        internal float m_BrushSize = 0;
        internal bool m_HitValidTerrain = false;
        internal RaycastHit m_RaycastHit;
        internal int m_ControlId;

        public OnSceneGUIContext(SceneView sceneView, RaycastHit raycastHit, Texture brushTexture, float brushStrength, float brushSize, int controlId)
        {
            Set(sceneView, false, raycastHit, brushTexture, brushStrength, brushSize, controlId);
        }

        public OnSceneGUIContext Set(SceneView sceneView, bool hitValidTerrain, RaycastHit raycastHit, Texture brushTexture, float brushStrength, float brushSize, int controlId)
        {
            m_SceneView = sceneView;
            m_BrushTexture = brushTexture;
            m_BrushStrength = brushStrength;
            m_BrushSize = brushSize;
            m_HitValidTerrain = hitValidTerrain;
            m_RaycastHit = raycastHit;
            m_ControlId = controlId;
            return this;
        }

        public SceneView sceneView { get { return m_SceneView; } }
        public Texture brushTexture { get { return m_BrushTexture; } }
        public float brushStrength { get { return m_BrushStrength; } }
        public float brushSize { get { return m_BrushSize; } }
        public bool hitValidTerrain { get { return m_HitValidTerrain; } }
        public RaycastHit raycastHit { get { return m_RaycastHit; } }
        public int controlId { get { return m_ControlId; } }

        public void Repaint(RepaintFlags flags)
        {
            if ((flags & RepaintFlags.UI) != 0)
            {
                InspectorWindow.RepaintAllInspectors();
            }

            if ((flags & RepaintFlags.Scene) != 0)
            {
                EditorApplication.SetSceneRepaintDirty();
            }
        }
    }

    public class OnInspectorGUIContext : IOnInspectorGUI
    {
        public OnInspectorGUIContext() {}

        public void ShowBrushesGUI(int spacing = 5, BrushGUIEditFlags flags = BrushGUIEditFlags.All, int textureResolutionPerTile = 0)
        {
            if (flags == BrushGUIEditFlags.None || TerrainInspector.s_activeTerrainInspectorInstance == null) // null check for adding and removing package
            {
                return;
            }

            TerrainInspector.s_activeTerrainInspectorInstance.ShowBrushes(
                spacing,
                (flags & BrushGUIEditFlags.Select) != 0,
                (flags & BrushGUIEditFlags.Inspect) != 0,
                (flags & BrushGUIEditFlags.Size) != 0,
                (flags & BrushGUIEditFlags.Opacity) != 0,
                textureResolutionPerTile);
        }

        public void Repaint(RepaintFlags flags)
        {
            if ((flags & RepaintFlags.UI) != 0)
            {
                InspectorWindow.RepaintAllInspectors();
            }

            if ((flags & RepaintFlags.Scene) != 0)
            {
                EditorApplication.SetSceneRepaintDirty();
            }
        }
    }
}
