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
        float brushRotation { get; }
        float brushSize { get; }

        void RepaintAllInspectors();
    }
    public interface IOnSceneGUI
    {
        SceneView sceneView { get; }
        Texture brushTexture { get; }
        float brushStrength { get; }
        float brushRotation { get; }
        float brushSize { get; }
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
        internal float m_BrushRotation = 0.0f;

        public OnPaintContext(Texture brushTexture, Vector2 uv, float brushStrength, float brushRotation, float brushSize)
        {
            Set(brushTexture, uv, brushStrength, brushRotation, brushSize);
        }

        public OnPaintContext Set(Texture brushTexture, Vector2 uv, float brushStrength, float brushRotation, float brushSize)
        {
            m_BrushTexture = brushTexture;
            m_UV = uv;
            m_BrushStrength = brushStrength;
            m_BrushSize = brushSize;
            m_BrushRotation = brushRotation;
            return this;
        }

        public Texture brushTexture { get { return m_BrushTexture; } }
        public Vector2 uv { get { return m_UV; } }
        public float brushStrength { get { return m_BrushStrength; } }
        public float brushRotation { get { return m_BrushRotation; } }
        public float brushSize { get { return m_BrushSize; } }

        public void RepaintAllInspectors() { InspectorWindow.RepaintAllInspectors(); }
    }

    internal class OnSceneGUIContext : IOnSceneGUI
    {
        internal SceneView m_SceneView = null;
        internal Texture m_BrushTexture = null;
        internal float m_BrushStrength = 0.0f;
        internal float m_BrushSize = 0;
        internal float m_BrushRotation = 0.0f;

        public OnSceneGUIContext(SceneView sceneView, Texture brushTexture, float brushStrength, float brushRotation, float brushSize)
        {
            Set(sceneView, brushTexture, brushStrength, brushSize, brushRotation);
        }

        public OnSceneGUIContext Set(SceneView sceneView, Texture brushTexture, float brushStrength, float brushRotation, float brushSize)
        {
            m_SceneView = sceneView;
            m_BrushTexture = brushTexture;
            m_BrushStrength = brushStrength;
            m_BrushSize = brushSize;
            m_BrushRotation = brushRotation;
            return this;
        }

        public SceneView sceneView { get { return m_SceneView; } }
        public Texture brushTexture { get { return m_BrushTexture; } }
        public float brushStrength { get { return m_BrushStrength; } }
        public float brushRotation { get { return m_BrushRotation; } }
        public float brushSize { get { return m_BrushSize; } }
    }

    internal class OnInspectorGUIContext : IOnInspectorGUI
    {
        public OnInspectorGUIContext() {}
        public void ShowBrushesGUI(int spacing) { TerrainInspector.s_activeTerrainInspectorInstance.ShowBrushes(spacing); }
    }
}
