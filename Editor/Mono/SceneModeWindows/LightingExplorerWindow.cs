// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Light Explorer", icon = "Lighting")]
    internal class LightingExplorerWindow : EditorWindow
    {
        static class Styles
        {
            public static readonly GUIContent[] TabTypes =
            {
                EditorGUIUtility.TextContent("Lights"),
                EditorGUIUtility.TextContent("Reflection Probes"),
                EditorGUIUtility.TextContent("Light Probes"),
                EditorGUIUtility.TextContent("Static Emissives")
            };
        }

        List<LightingExplorerWindowTab> m_TableTabs;

        float m_ToolbarPadding = -1;
        TabType m_SelectedTab = TabType.Lights;

        enum TabType
        {
            Lights,
            Reflections,
            LightProbes,
            Emissives,
            Count
        }

        [MenuItem("Window/Lighting/Light Explorer", false, 2099)]
        static void CreateLightingExplorerWindow()
        {
            LightingExplorerWindow window = EditorWindow.GetWindow<LightingExplorerWindow>();
            window.minSize = new Vector2(500, 250);
            window.Show();
        }

        private float toolbarPadding
        {
            get
            {
                if (m_ToolbarPadding == -1)
                {
                    var iconsSize = EditorStyles.iconButton.CalcSize(EditorGUI.GUIContents.helpIcon);
                    m_ToolbarPadding = (iconsSize.x * 2) + (EditorGUI.kControlVerticalSpacing * 3);
                }
                return m_ToolbarPadding;
            }
        }

        void OnEnable()
        {
            titleContent = GetLocalizedTitleContent();

            if (m_TableTabs == null || m_TableTabs.Count != (int)TabType.Count)
            {
                m_TableTabs = new List<LightingExplorerWindowTab>
                {
                    new LightingExplorerWindowTab(new SerializedPropertyTable("LightTable",   () => {
                            return UnityEngine.Object.FindObjectsOfType<Light>();
                        }, LightTableColumns.CreateLightColumns)),
                    new LightingExplorerWindowTab(new SerializedPropertyTable("ReflectionTable", () => {
                            return UnityEngine.Object.FindObjectsOfType<ReflectionProbe>();
                        }, LightTableColumns.CreateReflectionColumns)),
                    new LightingExplorerWindowTab(new SerializedPropertyTable("LightProbeTable", () => {
                            return UnityEngine.Object.FindObjectsOfType<LightProbeGroup>();
                        }, LightTableColumns.CreateLightProbeColumns)),
                    new LightingExplorerWindowTab(new SerializedPropertyTable("EmissiveMaterialTable",
                            StaticEmissivesGatherDelegate(), LightTableColumns.CreateEmissivesColumns))
                };
            }

            for (int i = 0; i < m_TableTabs.Count; i++)
            {
                m_TableTabs[i].OnEnable();
            }

            EditorApplication.searchChanged += Repaint;
            Repaint();
        }

        void OnDisable()
        {
            if (m_TableTabs != null)
            {
                for (int i = 0; i < m_TableTabs.Count; i++)
                {
                    m_TableTabs[i].OnDisable();
                }
            }

            EditorApplication.searchChanged -= Repaint;
        }

        void OnInspectorUpdate()
        {
            if (m_TableTabs != null && (int)m_SelectedTab >= 0 && (int)m_SelectedTab < m_TableTabs.Count)
            {
                m_TableTabs[(int)m_SelectedTab].OnInspectorUpdate();
            }
        }

        void OnSelectionChange()
        {
            if (m_TableTabs != null)
            {
                for (int i = 0; i < m_TableTabs.Count; i++)
                {
                    if (i == (m_TableTabs.Count - 1)) // last tab containing materials
                    {
                        int[] selectedIds = Object.FindObjectsOfType<MeshRenderer>().Where((MeshRenderer mr) => {
                                return Selection.instanceIDs.Contains(mr.gameObject.GetInstanceID());
                            }).SelectMany(meshRenderer => meshRenderer.sharedMaterials).Where((Material m) => {
                                return m != null && (m.globalIlluminationFlags & MaterialGlobalIlluminationFlags.AnyEmissive) != 0;
                            }).Select(m => m.GetInstanceID()).Union(Selection.instanceIDs).Distinct().ToArray();

                        m_TableTabs[i].OnSelectionChange(selectedIds);
                    }
                    else
                        m_TableTabs[i].OnSelectionChange();
                }
            }

            Repaint();
        }

        void OnHierarchyChange()
        {
            if (m_TableTabs != null)
            {
                for (int i = 0; i < m_TableTabs.Count; i++)
                {
                    m_TableTabs[i].OnHierarchyChange();
                }
            }
        }

        void OnGUI()
        {
            EditorGUIUtility.labelWidth = 130;

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            m_SelectedTab = (TabType)GUILayout.Toolbar((int)m_SelectedTab, Styles.TabTypes, "LargeButton", GUI.ToolbarButtonSize.FitToContents);
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (m_TableTabs != null && (int)m_SelectedTab >= 0 && (int)m_SelectedTab < m_TableTabs.Count)
                m_TableTabs[(int)m_SelectedTab].OnGUI();
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        private SerializedPropertyDataStore.GatherDelegate StaticEmissivesGatherDelegate()
        {
            return () => {
                    return Object.FindObjectsOfType<MeshRenderer>().Where((MeshRenderer mr) => {
                        return (GameObjectUtility.AreStaticEditorFlagsSet(mr.gameObject, StaticEditorFlags.LightmapStatic));
                    }).SelectMany(meshRenderer => meshRenderer.sharedMaterials).Where((Material m) => {
                        return m != null && ((m.globalIlluminationFlags & MaterialGlobalIlluminationFlags.AnyEmissive) != 0) && m.HasProperty("_EmissionColor");
                    }).Distinct().ToArray();
                };
        }
    }
}
