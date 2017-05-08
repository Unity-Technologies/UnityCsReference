// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor
{
    internal class PackageExport : EditorWindow
    {
        [SerializeField] private ExportPackageItem[]    m_ExportPackageItems;
        [SerializeField] private bool                   m_IncludeDependencies = true;
        [SerializeField] private TreeViewState          m_TreeViewState;
        [NonSerialized]  private PackageExportTreeView  m_Tree;
        [NonSerialized]  private bool                   m_DidScheduleUpdate = false;

        public ExportPackageItem[] items { get { return m_ExportPackageItems; } }

        internal static class Styles
        {
            public static GUIStyle title                     = new GUIStyle(EditorStyles.largeLabel);
            public static GUIStyle bottomBarBg               = "ProjectBrowserBottomBarBg";
            public static GUIStyle topBarBg                  = new GUIStyle("ProjectBrowserHeaderBgTop");
            public static GUIStyle loadingTextStyle          = new GUIStyle(EditorStyles.label);
            public static GUIContent allText                 = EditorGUIUtility.TextContent("All");
            public static GUIContent noneText                = EditorGUIUtility.TextContent("None");
            public static GUIContent includeDependenciesText = EditorGUIUtility.TextContent("Include dependencies");
            public static GUIContent header                  = new GUIContent("Items to Export");

            static Styles()
            {
                topBarBg.fixedHeight = 0;
                topBarBg.border.top = topBarBg.border.bottom = 2;

                title.fontStyle = FontStyle.Bold;
                title.alignment = TextAnchor.MiddleLeft;

                loadingTextStyle.alignment = TextAnchor.MiddleCenter;
            }
        }

        public PackageExport()
        {
            // Initial pos and minsize
            position = new Rect(100, 100, 400, 300);
            minSize = new Vector2(350, 350);
        }

        // Called from from menu
        static internal void ShowExportPackage()
        {
            GetWindow<PackageExport>(true, "Exporting package");
        }

        internal static IEnumerable<ExportPackageItem> GetAssetItemsForExport(ICollection<string> guids, bool includeDependencies)
        {
            // if nothing is selected, export all
            if (0 == guids.Count)
            {
                string[] temp = new string[0]; // <--- I dont get this API
                guids = new HashSet<string>(AssetDatabase.CollectAllChildren(AssetDatabase.assetFolderGUID, temp));
            }

            ExportPackageItem[] assets = PackageUtility.BuildExportPackageItemsList(guids.ToArray(), includeDependencies);

            // If any scripts are included, add all scripts with dependencies
            if (includeDependencies && assets.Any(asset => UnityEditorInternal.InternalEditorUtility.IsScriptOrAssembly(asset.assetPath)))
            {
                assets = PackageUtility.BuildExportPackageItemsList(
                        guids.Union(UnityEditorInternal.InternalEditorUtility.GetAllScriptGUIDs()).ToArray(), includeDependencies);
            }

            // If the user exports the root Assets folder, we need to remove it from the list
            // explicitly, as it doesnt make sense
            assets = assets.Where(val => val.assetPath != "Assets").ToArray();

            return assets;
        }

        void RefreshAssetList()
        {
            m_ExportPackageItems = null;
        }

        bool HasValidAssetList()
        {
            return m_ExportPackageItems != null;
        }

        bool CheckAssetExportList()
        {
            if (m_ExportPackageItems.Length == 0)
            {
                GUILayout.Space(20f);
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("Nothing to export!", EditorStyles.boldLabel);
                GUILayout.Label("No assets to export were found in your project.", "WordWrappedLabel");
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("OK"))
                {
                    Close();
                    GUIUtility.ExitGUI();
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                return true;
            }

            return false;
        }

        public void OnDestroy()
        {
            UnscheduleBuildAssetList();
        }

        public void OnGUI()
        {
            if (!HasValidAssetList())
            {
                ScheduleBuildAssetList();
            }
            else if (CheckAssetExportList())
            {
                return;
            }

            using (new EditorGUI.DisabledScope(!HasValidAssetList()))
            {
                TopArea();
            }

            TreeViewArea(!HasValidAssetList());

            using (new EditorGUI.DisabledScope(!HasValidAssetList()))
            {
                BottomArea();
            }
        }

        void TopArea()
        {
            float totalTopHeight = 53f;
            Rect r = GUILayoutUtility.GetRect(position.width, totalTopHeight);

            // Background
            GUI.Label(r, GUIContent.none, Styles.topBarBg);

            // Header
            Rect titleRect = new Rect(r.x + 5f, r.yMin, r.width, r.height);
            GUI.Label(titleRect, Styles.header, Styles.title);
        }

        void BottomArea()
        {
            // Background
            GUILayout.BeginVertical(Styles.bottomBarBg);

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            if (GUILayout.Button(Styles.allText, GUILayout.Width(50)))
            {
                m_Tree.SetAllEnabled(PackageExportTreeView.EnabledState.All);
            }

            if (GUILayout.Button(Styles.noneText, GUILayout.Width(50)))
            {
                m_Tree.SetAllEnabled(PackageExportTreeView.EnabledState.None);
            }

            GUILayout.Space(10);

            EditorGUI.BeginChangeCheck();
            m_IncludeDependencies = GUILayout.Toggle(m_IncludeDependencies, Styles.includeDependenciesText);
            if (EditorGUI.EndChangeCheck())
            {
                RefreshAssetList();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(EditorGUIUtility.TextContent("Export...")))
            {
                Export();
                GUIUtility.ExitGUI();
            }
            GUILayout.Space(10);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            GUILayout.EndVertical();
        }

        private void TreeViewArea(bool showLoadingScreen)
        {
            Rect treeAreaRect = GUILayoutUtility.GetRect(1, 9999, 1, 99999);

            if (showLoadingScreen)
            {
                GUI.Label(treeAreaRect, "Loading...", Styles.loadingTextStyle);
                return;
            }

            if (m_ExportPackageItems != null && m_ExportPackageItems.Length > 0)
            {
                if (m_TreeViewState == null)
                    m_TreeViewState = new TreeViewState();

                if (m_Tree == null)
                    m_Tree = new PackageExportTreeView(this, m_TreeViewState, new Rect());

                m_Tree.OnGUI(treeAreaRect);
            }
        }

        private void Export()
        {
            string fileName = EditorUtility.SaveFilePanel("Export package ...", "", "", "unitypackage");
            if (fileName != "")
            {
                // build guid list
                List<string> guids = new List<string>();

                foreach (ExportPackageItem ai in m_ExportPackageItems)
                {
                    if (ai.enabledStatus > 0)
                        guids.Add(ai.guid);
                }

                PackageUtility.ExportPackage(guids.ToArray(), fileName);

                Close();
                GUIUtility.ExitGUI();
            }
        }

        private void ScheduleBuildAssetList()
        {
            if (!m_DidScheduleUpdate)
            {
                EditorApplication.update += BuildAssetList;
                m_DidScheduleUpdate = true;
            }
        }

        private void UnscheduleBuildAssetList()
        {
            if (m_DidScheduleUpdate)
            {
                m_DidScheduleUpdate = false;
                EditorApplication.update -= BuildAssetList;
            }
        }

        private void BuildAssetList()
        {
            UnscheduleBuildAssetList();

            m_ExportPackageItems = GetAssetItemsForExport(Selection.assetGUIDsDeepSelection, m_IncludeDependencies).ToArray();

            // GUI is reconstructed in OnGUI (when needed)
            m_Tree = null;
            m_TreeViewState = null;

            Repaint();
        }
    }
}
