// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityEditor
{
    internal interface ITerrainPaintTool
    {
        void OnEnable();
        void OnDisable();
        void OnSceneGUI(SceneView sceneView, Terrain terrain, Texture brushTexture, float brushStrength, int brushSizeInTerrainUnits);
        void OnInspectorGUI(Terrain terrain);
        string GetName();
        string GetDesc();
        bool Paint(Terrain terrain, Texture brushTexture, Vector2 uv, float brushStrength, int brushSizeInTerrainUnits);
        bool ShouldShowBrushes();
        bool DoesPaint();
    }

    public abstract class TerrainPaintTool<T> : ScriptableSingleton<T>, ITerrainPaintTool where T : TerrainPaintTool<T>
    {
        public virtual void OnEnable() {}
        public virtual void OnDisable() {}
        public virtual void OnSceneGUI(SceneView sceneView, Terrain terrain, Texture brushTexture, float brushStrength, int brushSizeInTerrainUnits) {}
        public virtual void OnInspectorGUI(Terrain terrain) {}
        public abstract string GetName();
        public abstract string GetDesc();
        public virtual bool Paint(Terrain terrain, Texture brushTexture, Vector2 uv, float brushStrength, int brushSizeInTerrainUnits) { return false; }
        public virtual bool ShouldShowBrushes() { return true;  }
        public virtual bool DoesPaint() { return true; }
    }
}
