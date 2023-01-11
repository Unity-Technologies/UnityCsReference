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
    internal class TerrainToolEditor : Editor
    {
        private Vector2 m_ScrollPos;
        private IMGUIContainer m_ImgContainer;
        static Dictionary<Type, (MethodInfo, bool)> m_ToolTypeToGUIFunc = new (); // true for non-obsolete, false for obsolete


        public override VisualElement CreateInspectorGUI()
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
            return m_ImgContainer;
        }

        public void OnGUI()
        {
            var editor = TerrainInspector.s_activeTerrainInspectorInstance;
            if (!editor) return;

            var tool = target as TerrainPaintToolWithOverlaysBase;
            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            if (!tool)
            {
                Debug.LogError("Tool is NULL");
                return;
            }

            if (!tool.Terrain && Selection.activeGameObject)
            {
                tool.Terrain = Selection.activeGameObject.GetComponent<Terrain>();
            }

            if (!tool.Terrain)
            {
                Debug.LogError("Tool does NOT have associated terrain");
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

            EditorGUILayout.EndScrollView();
        }
    }
}
