// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEditor.EditorTools;
using UnityEditor.UIElements;
using UnityEngine.TerrainUtils;
using UnityEngine.UIElements;

namespace UnityEditor.TerrainTools
{
    [CustomEditor(typeof(TerrainPaintToolWithOverlaysBase), editorForChildClasses: true)]
    internal class TerrainToolEditor : Editor, ICreateHorizontalToolbar
    {
        private Vector2 m_ScrollPos;
        private IMGUIContainer m_ImgContainer;
        static Dictionary<Type, (MethodInfo, bool)> m_ToolTypeToGUIFunc = new (); // true for non-obsolete, false for obsolete
        private static bool? m_IsTerrainToolsInstalled; // this value gets cleared on domain reload

        private static bool IsTerrainToolsPackageInstalled()
        {
            if (m_IsTerrainToolsInstalled.HasValue)
            {
                return m_IsTerrainToolsInstalled.Value;
            }

            var upm = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            var terrainPackageInfo = upm.Where(pi => pi.name == "com.unity.terrain-tools").ToArray();

            Debug.Assert(terrainPackageInfo.Length <= 1, "Only one version of terrain-tools package allowed to be installed");

            m_IsTerrainToolsInstalled = terrainPackageInfo.Length != 0; // equals 0 means no package installed, equals 1 means package is installed
            return m_IsTerrainToolsInstalled.Value;
        }

        public OverlayToolbar CreateHorizontalToolbarContent()
        {

            var toolbar = new OverlayToolbar();
            var editor = TerrainInspector.s_activeTerrainInspectorInstance;
            var tool = target as TerrainPaintToolWithOverlaysBase;
            if (!editor || !tool) return toolbar;

            toolbar.Add(new EditorToolbarDropdown($"{tool.GetName()} Settings", () =>
            {
                var m = Event.current.mousePosition;
                PopupWindow.Show(new Rect(m.x, m.y, 10, 20), new TerrainSettingsPopup());
            }));
            return toolbar;
        }

        class TerrainSettingsPopup : PopupWindowContent
        {

            private Vector2 m_ScrollPos;

            public override Vector2 GetWindowSize()
            {
                var editor = TerrainInspector.s_activeTerrainInspectorInstance;
                var tool = EditorToolManager.GetActiveTool() as TerrainPaintToolWithOverlaysBase;
                if (editor && tool)
                {
                    if (!tool.HasToolSettings)
                    {
                        // appropriate size for no tool settings message
                        return new Vector2(230, 30);
                    }

                    // else tool settings
                    // different sizes based on tooltype / package installed perhaps
                    if (tool.Category == TerrainCategory.NeighborTerrains)
                    {
                        return new Vector2(400, 60);
                    }

                    if (tool.Category == TerrainCategory.Materials || tool.Category == TerrainCategory.Foliage || tool.Category == TerrainCategory.CustomBrushes)
                    {
                        return new Vector2(400, 250);
                    }

                    // if package installed
                    if (IsTerrainToolsPackageInstalled())
                    {
                        // stamp and noise foliage and materials should be longer
                        if (tool.GetName() == "Stamp Terrain" || tool.GetName() == "Sculpt/Noise")
                        {
                            return new Vector2(400, 250);
                        }

                        if (tool.GetName() == "Transform/Smudge" || tool.GetName() == "Effects/Sharpen Peaks" || tool.GetName() == "Effects/Contrast")
                        {
                            return new Vector2(400, 80);
                        }

                        if (tool.GetName() == "Transform/Twist" || tool.GetName() == "Transform/Pinch" || tool.GetName() == "Smooth Height")
                        {
                            return new Vector2(400, 100);
                        }

                        // need to do something about erosion too (same size, but a little longer maybe)
                        if (tool.GetName() == "Erosion/Wind" || tool.GetName() == "Erosion/Hydraulic" || tool.GetName() == "Erosion/Thermal")
                        {
                            return new Vector2(400, 160);
                        }

                        // else everything else should be smaller
                        return new Vector2(400, 120);
                    }

                    // else package is NOT installed
                    if (tool.GetName() == "Smooth Height")
                    {
                        return new Vector2(400, 35);
                    }

                    return new Vector2(400, 75); // every other tool without package

                }

                // if code reaches here, tool is null
                return new Vector2(400, 250); // return some default
            }

            public override void OnGUI(Rect rect)
            {
                var editor = TerrainInspector.s_activeTerrainInspectorInstance;
                var tool = EditorToolManager.GetActiveTool() as TerrainPaintToolWithOverlaysBase;
                if (editor && tool)
                {
                    m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
                    TerrainToolEditor.OnGUI(tool);
                    EditorGUILayout.EndScrollView();
                }
            }
        }

        public void CreateIMGContainer()
        {
            m_ImgContainer = new IMGUIContainer();
            m_ImgContainer.style.minWidth = 300;
            m_ImgContainer.style.maxWidth = 400;
            m_ImgContainer.style.maxHeight = 600;

            m_ImgContainer.onGUIHandler = () =>
            {
                // adjust brush mask width here to force brush mask sizes
                EditorGUIUtility.currentViewWidth = 430;
                OnGUI();
                EditorGUILayout.Space(); // spacer for bottom offset
            };
        }


        public override VisualElement CreateInspectorGUI()
        {
            CreateIMGContainer();
            return m_ImgContainer;
        }

        public static void OnGUI(TerrainPaintToolWithOverlaysBase tool)
        {
            if (!tool)
            {
                return;
            }

            if (!tool.Terrain && Selection.activeGameObject)
            {
                tool.Terrain = Selection.activeGameObject.GetComponent<Terrain>();
            }

            if (!tool.Terrain)
            {
                return;
            }

            if (tool.HasToolSettings)
            {
                Type type = tool.GetType();

                if (!m_ToolTypeToGUIFunc.ContainsKey(type))
                {
                    // store the tool type to function ONCE
                    MethodInfo funcWithoutBoolean = type.GetMethod("OnToolSettingsGUI", new [] {typeof(Terrain), typeof(IOnInspectorGUI)}); // not obsolete
                    MethodInfo funcWithBoolean = type.GetMethod("OnToolSettingsGUI", new [] {typeof(Terrain), typeof(IOnInspectorGUI), typeof(bool)}); // obsolete
                    if (funcWithoutBoolean == null || funcWithBoolean == null) return;

                    if (funcWithoutBoolean.DeclaringType != funcWithoutBoolean.GetBaseDefinition().DeclaringType)
                    {
                        // non obsolete method is overriden
                        m_ToolTypeToGUIFunc[type] = (funcWithoutBoolean, true);
                    } else if (funcWithBoolean.DeclaringType != funcWithBoolean.GetBaseDefinition().DeclaringType)
                    {
                        // obsolete method is overriden
                        m_ToolTypeToGUIFunc[type] = (funcWithBoolean, false);
                    }
                    else
                    {
                        return; // shouldnt get here
                    }
                }

                // invoke obsolete or non obsolete method depending on value of Item2 (bool)
                m_ToolTypeToGUIFunc[type].Item1
                    .Invoke(tool,
                        m_ToolTypeToGUIFunc[type].Item2
                            ? new object[] {tool.Terrain, new OnInspectorGUIContext()}
                            : new object[] {tool.Terrain, new OnInspectorGUIContext(), false});
            }
            else
            {
                GUILayout.Label("This tool has no extra tool settings!");
            }
        }

        public void OnGUI()
        {
            var editor = TerrainInspector.s_activeTerrainInspectorInstance;
            var tool = target as TerrainPaintToolWithOverlaysBase;
            if (!editor || !tool) return;
            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
            OnGUI(tool); // helper function for GUI
            EditorGUILayout.EndScrollView();
        }
    }
}
