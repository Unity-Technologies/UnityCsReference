// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class TerrainLayerSelectionWindow : EditorWindow
    {
        int m_SelectedTerrainLayer = 0;
        TerrainLayer[] m_TerrainLayerList = null;
        Terrain m_Terrain;
        int m_Index;
        bool m_ShouldRefreshList = true;

        // UI
        Vector2 m_ScrollPos;

        public int selectedIndex { get { return m_SelectedTerrainLayer; } }
        static class Styles
        {
            public static GUIStyle gridList = "GridList";
            public static GUIContent terrainLayers = EditorGUIUtility.TrTextContent("Terrain Layers");

            public static GUIContent btnCreateNewLayer = EditorGUIUtility.TrTextContent("Create New Layer");
            public static GUIContent btnAddLayer = EditorGUIUtility.TrTextContent("Add Layer");
            public static GUIContent btnCloseWindow = EditorGUIUtility.TrTextContent("Close Window");
            public static GUIContent errNoLayersFound = EditorGUIUtility.TrTextContent("No terrain layers found. Create or import some");
        }
        void OnEnable()
        {
            if (m_TerrainLayerList == null)
            {
                LoadTerrainLayers();
                UpdateSelection(0);
            }
        }

        static internal void ShowTerrainLayerListEditor(string title, Terrain terrain, int index)
        {
            TerrainLayerSelectionWindow editor = GetWindow<TerrainLayerSelectionWindow>(true, title);
            editor.m_Terrain = terrain;
            editor.m_Index = index;
        }

        private void OnInspectorUpdate()
        {
            ValidateTerrainLayerList();
            Repaint();
        }

        void LoadTerrainLayers()
        {
            ArrayList arr = new ArrayList();

            // Load all the .terrainlayer files
            string[] fileEntries = Directory.GetFiles(Application.dataPath, "*.terrainlayer", SearchOption.AllDirectories);
            string path = Application.dataPath.Remove(Application.dataPath.Length - "Assets".Length);
            foreach (string file in fileEntries)
            {
                string filePath = file.Remove(0, path.Length);
                filePath = filePath.Replace("\\", "/");
                TerrainLayer b = AssetDatabase.LoadMainAssetAtPath(filePath) as TerrainLayer;
                if (b)
                    arr.Add(b);
            }

            m_TerrainLayerList = arr.ToArray(typeof(TerrainLayer)) as TerrainLayer[];
            m_ShouldRefreshList = false;
        }

        void UpdateSelection(int newSelectedTerrainLayer)
        {
            m_SelectedTerrainLayer = newSelectedTerrainLayer;
        }

        TerrainLayer GetActiveTerrainLayer()
        {
            if (m_TerrainLayerList != null && m_TerrainLayerList.Length > 0)
            {
                if (m_TerrainLayerList.Length < m_SelectedTerrainLayer)
                    m_SelectedTerrainLayer = 0;

                return m_TerrainLayerList[m_SelectedTerrainLayer];
            }
            return null;
        }

        void AppendTerrainLayer()
        {
            if (m_Terrain == null || m_Terrain.terrainData == null)
                return;

            TerrainLayer activeTerrainLayer = GetActiveTerrainLayer();
            if (activeTerrainLayer == null)
                return;

            TerrainLayer[] infos = m_Terrain.terrainData.terrainLayers;
            foreach (TerrainLayer info in infos)
            {
                if (info == activeTerrainLayer)
                    return;
            }

            int newIndex = m_Index;
            if (newIndex == -1)
            {
                var newarray = new TerrainLayer[infos.Length + 1];
                System.Array.Copy(infos, 0, newarray, 0, infos.Length);
                newIndex = infos.Length;
                infos = newarray;
            }
            infos[newIndex] = activeTerrainLayer;
            m_Terrain.terrainData.terrainLayers = infos;
            EditorUtility.SetDirty(m_Terrain);
        }

        void ValidateTerrainLayerList()
        {
            if (m_TerrainLayerList == null)
                return;

            foreach (TerrainLayer l in m_TerrainLayerList)
            {
                if (l == null)
                {
                    LoadTerrainLayers();
                    break;
                }
            }

            if (m_ShouldRefreshList)
            {
                if (m_TerrainLayerList.Length != Directory.GetFiles(Application.dataPath, "*.terrainlayer", SearchOption.AllDirectories).Length)
                    LoadTerrainLayers();
            }
        }

        void OnGUI()
        {
            ValidateTerrainLayerList();

            GUILayout.Label(Styles.terrainLayers, EditorStyles.boldLabel);

            bool doubleClick = false;
            if (m_TerrainLayerList != null)
            {
                m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos, false, false);
                int newTerrainLayer = TerrainLayerSelectionGrid(m_SelectedTerrainLayer, m_TerrainLayerList, 64, Styles.gridList, Styles.errNoLayersFound, out doubleClick);
                if (newTerrainLayer != m_SelectedTerrainLayer)
                    UpdateSelection(newTerrainLayer);
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(Styles.btnCreateNewLayer))
            {
                TerrainLayerInspector.CreateNewDefaultTerrainLayer();
                m_ShouldRefreshList = true;
            }
            if (doubleClick || GUILayout.Button(Styles.btnAddLayer))
            {
                AppendTerrainLayer();
            }
            if (GUILayout.Button(Styles.btnCloseWindow))
            {
                Close();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
        }

        static int TerrainLayerSelectionGrid(int selected, TerrainLayer[] terrainLayers, int approxSize, GUIStyle style, GUIContent errMessage, out bool doubleClick)
        {
            GUILayout.BeginVertical("box", GUILayout.MinHeight(approxSize));
            int retval = 0;

            doubleClick = false;

            if (terrainLayers.Length != 0)
            {
                int columns = (int)(EditorGUIUtility.currentViewWidth) / approxSize;
                int rows = (int)Mathf.Ceil((terrainLayers.Length + columns - 1) / columns);
                Rect r = GUILayoutUtility.GetAspectRect((float)columns / (float)rows);
                Event evt = Event.current;
                if (evt.type == EventType.MouseDown && evt.clickCount == 2 && r.Contains(evt.mousePosition))
                {
                    doubleClick = true;
                    evt.Use();
                }

                retval = GUI.SelectionGrid(r, System.Math.Min(selected, terrainLayers.Length - 1), GUIContentFromTerrainLayer(terrainLayers), (int)columns, style);
            }
            else
                GUILayout.Label(errMessage);

            GUILayout.EndVertical();
            return retval;
        }

        static GUIContent[] GUIContentFromTerrainLayer(TerrainLayer[] terrainLayers)
        {
            GUIContent[] retval = new GUIContent[terrainLayers.Length];

            for (int i = 0; i < terrainLayers.Length; i++)
                retval[i] = new GUIContent(terrainLayers[i].diffuseTexture);

            return retval;
        }
    }
} //namespace
