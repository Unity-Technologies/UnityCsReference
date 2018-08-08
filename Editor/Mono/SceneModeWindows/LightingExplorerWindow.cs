// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityEditor
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class LightingExplorerExtensionAttribute : System.Attribute
    {
        internal System.Type renderPipelineType;

        public LightingExplorerExtensionAttribute(System.Type renderPipeline)
        {
            renderPipelineType = renderPipeline;
        }
    }

    public interface ILightingExplorerExtension
    {
        LightingExplorerTab[] GetContentTabs();

        void OnEnable();
        void OnDisable();
    }

    [EditorWindowTitle(title = "Light Explorer", icon = "Lighting")]
    internal class LightingExplorerWindow : EditorWindow
    {
        LightingExplorerTab[] m_TableTabs;
        GUIContent[] m_TabTitles;

        float m_ToolbarPadding = -1;
        int m_SelectedTab = 0;

        System.Type m_CurrentSRPType = null;
        ILightingExplorerExtension m_CurrentLightingExplorerExtension = null;
        static ILightingExplorerExtension s_DefaultLightingExplorerExtension = null;

        [MenuItem("Window/Rendering/Light Explorer", false, 2)]
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

            UpdateTabs();

            for (int i = 0; i < m_TableTabs.Length; i++)
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
                for (int i = 0; i < m_TableTabs.Length; i++)
                {
                    m_TableTabs[i].OnDisable();
                }
            }

            if (m_CurrentLightingExplorerExtension != null)
            {
                m_CurrentLightingExplorerExtension.OnDisable();
            }

            EditorApplication.searchChanged -= Repaint;
        }

        void OnInspectorUpdate()
        {
            if (m_TableTabs != null && (int)m_SelectedTab >= 0 && (int)m_SelectedTab < m_TableTabs.Length)
            {
                m_TableTabs[(int)m_SelectedTab].OnInspectorUpdate();
            }
        }

        void OnSelectionChange()
        {
            if (m_TableTabs != null)
            {
                for (int i = 0; i < m_TableTabs.Length; i++)
                {
                    if (i == (m_TableTabs.Length - 1)) // last tab containing materials
                    {
                        int[] selectedIds = UnityEngine.Object.FindObjectsOfType<MeshRenderer>().Where((MeshRenderer mr) => {
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
                for (int i = 0; i < m_TableTabs.Length; i++)
                {
                    m_TableTabs[i].OnHierarchyChange();
                }
            }
        }

        void OnGUI()
        {
            UpdateTabs();

            EditorGUIUtility.labelWidth = 130;

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            if (m_TabTitles != null)
                m_SelectedTab = GUILayout.Toolbar(m_SelectedTab, m_TabTitles, "LargeButton", GUI.ToolbarButtonSize.FitToContents);
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (m_TableTabs != null && (int)m_SelectedTab >= 0 && (int)m_SelectedTab < m_TableTabs.Length)
                m_TableTabs[(int)m_SelectedTab].OnGUI();
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        private System.Type GetSRPType()
        {
            System.Type SRPType = null;

            if (GraphicsSettings.renderPipelineAsset != null)
            {
                SRPType = GraphicsSettings.renderPipelineAsset.GetType();
            }

            return SRPType;
        }

        private void UpdateTabs()
        {
            var SRPType = GetSRPType();

            if (m_CurrentLightingExplorerExtension == null || m_CurrentSRPType != SRPType)
            {
                m_CurrentSRPType = SRPType;

                if (m_CurrentLightingExplorerExtension != null)
                {
                    m_CurrentLightingExplorerExtension.OnDisable();
                }

                m_CurrentLightingExplorerExtension = GetLightExplorerExtension(SRPType);
                m_CurrentLightingExplorerExtension.OnEnable();

                m_SelectedTab = 0;

                if (m_CurrentLightingExplorerExtension.GetContentTabs() == null || m_CurrentLightingExplorerExtension.GetContentTabs().Length == 0)
                {
                    throw new ArgumentException("There must be atleast 1 content tab defined for the Lighting Explorer.");
                }

                m_TableTabs =  m_CurrentLightingExplorerExtension.GetContentTabs();
                m_TabTitles = m_TableTabs != null ? m_TableTabs.Select(item => item.title).ToArray() : null;
            }
        }

        private ILightingExplorerExtension GetDefaultLightingExplorerExtension()
        {
            if (s_DefaultLightingExplorerExtension == null)
            {
                s_DefaultLightingExplorerExtension = new DefaultLightingExplorerExtension();
            }
            return s_DefaultLightingExplorerExtension;
        }

        private ILightingExplorerExtension GetLightExplorerExtension(System.Type currentSRPType)
        {
            if (currentSRPType == null)
                return GetDefaultLightingExplorerExtension();

            var extensionTypes = EditorAssemblies.GetAllTypesWithInterface<ILightingExplorerExtension>();

            foreach (System.Type extensionType in extensionTypes)
            {
                LightingExplorerExtensionAttribute attribute = System.Attribute.GetCustomAttribute(extensionType, typeof(LightingExplorerExtensionAttribute)) as LightingExplorerExtensionAttribute;
                if (attribute != null && attribute.renderPipelineType == currentSRPType)
                {
                    ILightingExplorerExtension extension = (ILightingExplorerExtension)System.Activator.CreateInstance(extensionType);
                    return extension;
                }
            }

            // no light explorer extension found for current srp, return the default one
            return GetDefaultLightingExplorerExtension();
        }
    }
}
