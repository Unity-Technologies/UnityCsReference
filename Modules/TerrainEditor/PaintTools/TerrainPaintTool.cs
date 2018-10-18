// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    public interface IOnPaint
    {
        Texture brushTexture { get; }
        Vector2 uv { get; }
        float brushStrength { get; }
        float brushSize { get; }

        void RepaintAllInspectors();
    }
    public interface IOnSceneGUI
    {
        SceneView sceneView { get; }
        Texture brushTexture { get; }
        float brushStrength { get; }
        float brushSize { get; }
        bool hitValidTerrain { get; }
        RaycastHit raycastHit { get;  }
    }

    public interface IOnInspectorGUI
    {
        void ShowBrushesGUI(int spacing);
    }

    internal interface ITerrainPaintTool
    {
        string GetName();
        string GetDesc();
        void OnEnable();
        void OnDisable();
        void OnEnterToolMode();
        void OnExitToolMode();
        void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext);
        void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext);
        bool OnPaint(Terrain terrain, IOnPaint editContext);
    }

    public abstract class TerrainPaintTool<T> : ScriptableSingleton<T>, ITerrainPaintTool where T : TerrainPaintTool<T>
    {
        public abstract string GetName();
        public abstract string GetDesc();
        public virtual void OnEnable() {}
        public virtual void OnDisable() {}
        public virtual void OnEnterToolMode() {}
        public virtual void OnExitToolMode() {}
        public virtual void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext) {}
        public virtual void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext) {}
        public virtual bool OnPaint(Terrain terrain, IOnPaint editContext) { return false; }
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

        public void RepaintAllInspectors() { InspectorWindow.RepaintAllInspectors(); }
    }

    internal class OnSceneGUIContext : IOnSceneGUI
    {
        internal SceneView m_SceneView = null;
        internal Texture m_BrushTexture = null;
        internal float m_BrushStrength = 0.0f;
        internal float m_BrushSize = 0;
        internal bool m_HitValidTerrain = false;
        internal RaycastHit m_RaycastHit;

        public OnSceneGUIContext(SceneView sceneView, RaycastHit raycastHit, Texture brushTexture, float brushStrength, float brushSize)
        {
            Set(sceneView, false, raycastHit, brushTexture, brushStrength, brushSize);
        }

        public OnSceneGUIContext Set(SceneView sceneView, bool hitValidTerrain, RaycastHit raycastHit, Texture brushTexture, float brushStrength, float brushSize)
        {
            m_SceneView = sceneView;
            m_BrushTexture = brushTexture;
            m_BrushStrength = brushStrength;
            m_BrushSize = brushSize;
            m_HitValidTerrain = hitValidTerrain;
            m_RaycastHit = raycastHit;
            return this;
        }

        public SceneView sceneView { get { return m_SceneView; } }
        public Texture brushTexture { get { return m_BrushTexture; } }
        public float brushStrength { get { return m_BrushStrength; } }
        public float brushSize { get { return m_BrushSize; } }
        public bool hitValidTerrain { get { return m_HitValidTerrain; } }
        public RaycastHit raycastHit { get { return m_RaycastHit; } }
    }

    internal class OnInspectorGUIContext : IOnInspectorGUI
    {
        public OnInspectorGUIContext() {}
        public void ShowBrushesGUI(int spacing) { TerrainInspector.s_activeTerrainInspectorInstance.ShowBrushes(spacing); }
    }
}
