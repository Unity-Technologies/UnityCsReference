// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.PackageManager.UI.Internal;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Scripting;
using UnityEditor.AssetPackage;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;

namespace UnityEditor
{
    internal class PackageExport : EditorWindow
    {
        internal sealed class DependencyContainer
        {
            private readonly Dictionary<Type, object> m_RegisteredDependencies = new();

            public DependencyContainer()
            {
                Reset();
            }

            public void Reset()
            {
                Register(new UtilityAdapter(), typeof(IUtilityAdapter));
                Register(new UnityConnectAdapter(), typeof(IUnityConnectAdapter));
                Register(new AssetDatabaseAdapter(), typeof(IAssetDatabaseAdapter));
                Register(new EditorUtilityAdapter(), typeof(IEditorUtilityAdapter));
                Register(new EditorWindowAdapter(), typeof(IEditorWindowAdapter));
                Register(new PackageManagerAdapter(), typeof(IPackageManagerAdapter));
            }

            public T Register<T>(T service, Type type) where T : class
            {
                if (service == null)
                    return null;
                m_RegisteredDependencies[type] = service;
                return service;
            }

            public T Resolve<T>() where T : class
            {
                return m_RegisteredDependencies.TryGetValue(typeof(T), out var result) ? result as T : null;
            }
        }


        [SerializeField] private ExportPackageItem[] m_ExportPackageItems;
        [SerializeField] private bool m_IncludeDependencies = true;
        [SerializeField] private bool m_IncludeScripts = true;
        [SerializeField] private TreeViewState m_TreeViewState;
        [SerializeField] private string[] m_ProjectBrowserSelection;
        [SerializeField] internal int m_SelectedOrgIndex = -1;
        [SerializeField] private OrganizationInfo[] m_OrganizationInfos = Array.Empty<OrganizationInfo>();
        [NonSerialized] private PackageExportTreeView m_Tree;
        [NonSerialized] private bool m_DidScheduleUpdate = false;

        [NonSerialized] private readonly IUtilityAdapter m_UtilityAdapter;
        [NonSerialized] private readonly IEditorUtilityAdapter m_EditorUtilityAdapter;

        public static readonly DependencyContainer dependencyContainer = new();

        public ExportPackageItem[] items => m_ExportPackageItems;

        public bool isOrgSelected => m_SelectedOrgIndex > -1;
        
        internal static class Styles
        {
            public static GUIStyle title = "LargeBoldLabel";
            public static GUIStyle bottomBarBg = "ProjectBrowserBottomBarBg";
            public static GUIStyle topBarBg = "OT TopBar";
            public static GUIStyle loadingTextStyle = "CenteredLabel";
            public static GUIContent authoringOrg = EditorGUIUtility.TrTextContent("Authoring organization:", "The organization that will be used to sign the exported package.");
            public static GUIContent allText = EditorGUIUtility.TrTextContent("All");
            public static GUIContent noneText = EditorGUIUtility.TrTextContent("None");
            public static GUIContent includeDependenciesText = EditorGUIUtility.TrTextContent("Include dependencies", "Include all dependencies required for the selected items in the export list.");
            public static GUIContent includeScriptsText = EditorGUIUtility.TrTextContent("Include all scripts", "Include all project scripts in the export list to avoid potential compilation errors.");
            public static GUIContent header = EditorGUIUtility.TrTextContent("Items to Export");
        }

        public PackageExport()
        {
            // Initial pos and minsize
            position = new Rect(100, 100, 400, 300);
            minSize = new Vector2(350, 350);

            m_UtilityAdapter = dependencyContainer.Resolve<IUtilityAdapter>();
            m_EditorUtilityAdapter = dependencyContainer.Resolve<IEditorUtilityAdapter>();
        }

        internal void SetOrganizationInfos(OrganizationInfo[] organizationInfos)
        {
            m_OrganizationInfos = organizationInfos;
            if (m_OrganizationInfos.Length == 1)
                m_SelectedOrgIndex = 0;
            else
                m_SelectedOrgIndex = -1;
        }

        static internal void ShowExportPackageWindow(OrganizationInfo[] organizationInfos)
        {
            var packageExportWindow = dependencyContainer.Resolve<IEditorWindowAdapter>().GetPackageExportWindow();
            packageExportWindow.SetOrganizationInfos(organizationInfos);
            packageExportWindow.RefreshAssetList();
        }

        // Called from menu
        [RequiredByNativeCode]
        static internal void ShowExportPackage()
        {
            var packageManagerAdapter = dependencyContainer.Resolve<IPackageManagerAdapter>();
            var unityConnectAdapter = dependencyContainer.Resolve<IUnityConnectAdapter>();

            if (packageManagerAdapter.HasBypassPackageTrustEntitlement || !unityConnectAdapter.isUserLoggedIn)
                ShowExportPackageWindow(Array.Empty<OrganizationInfo>());
            else
                unityConnectAdapter.ParseOrganizationInfos(ShowExportPackageWindow);
        }

        internal static IEnumerable<ExportPackageItem> GetAssetItemsForExport(ICollection<string> guids, bool includeDependencies, bool includeScripts)
        {
            var assetDatabaseAdapter = dependencyContainer.Resolve<IAssetDatabaseAdapter>();
            var utilityAdapter = dependencyContainer.Resolve<IUtilityAdapter>();

            // if nothing is selected, export all
            if (0 == guids.Count)
            {
                string[] temp = Array.Empty<string>(); // <--- I dont get this API
                guids = new HashSet<string>(assetDatabaseAdapter.CollectAllChildren(AssetDatabase.assetFolderGUID, temp));
            }

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var guidsArray = includeScripts ? guids.Union(UnityEditorInternal.InternalEditorUtility.GetAllScriptGUIDs()).ToArray() : guids.ToArray();
#pragma warning restore UA2001
            var assets = utilityAdapter.BuildExportPackageItemsListWithPackageManagerWarning(guidsArray, includeDependencies, true);

            // If the user exports the root Assets folder, we need to remove it from the list
            // explicitly, as it doesn't make sense
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            assets = assets.Where(val => val.assetPath != "Assets").ToArray();
#pragma warning restore UA2001

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
        
        bool HasEmptyAssetList()
        {
            return m_ExportPackageItems != null && m_ExportPackageItems.Length == 0;
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
            else if (HasEmptyAssetList())
            {
                RenderEmptyAssetList();
                return;
            }

            using (new EditorGUI.DisabledScope(!HasValidAssetList()))
            {
                RenderTopArea();
                RenderTopButtonsArea();
            }

            RenderTreeViewArea(!HasValidAssetList());

            using (new EditorGUI.DisabledScope(!HasValidAssetList()))
            {
                RenderBottomArea();
            }
        }

        private void RenderEmptyAssetList()
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
        }

        private void RenderTopArea()
        {
            var totalTopHeight = 53f;
            var topRect = GUILayoutUtility.GetRect(position.width, totalTopHeight);

            // Background
            GUI.Label(topRect, GUIContent.none, Styles.topBarBg);

            // Header
            var headerRect = new Rect(topRect.x + 5f, topRect.y + 5f, topRect.width, 20f);
            GUI.Label(headerRect, Styles.header, Styles.title);

            // Organization name label + dropdown
            if (m_OrganizationInfos.Length > 0)
                RenderDropdownArea(topRect, headerRect);
        }

        private void RenderDropdownArea(Rect topRect, Rect headerRect)
        {
            // Dropdown label
            var labelSize = EditorStyles.label.CalcSize(Styles.authoringOrg);
            var yPosition = headerRect.yMax + 4f;
            var labelRect = new Rect(
                headerRect.x,
                yPosition,
                labelSize.x,
                EditorGUIUtility.singleLineHeight
            );
            GUI.Label(labelRect, Styles.authoringOrg);

            // Make dropdown fill remaining width
            var dropdownX = labelRect.xMax + 5f;
            var dropdownWidth = Mathf.Max(
                0f,
                topRect.x + topRect.width - 5f - dropdownX
            );

            var displayText = isOrgSelected
                ? m_OrganizationInfos[m_SelectedOrgIndex].name
                : L10n.Tr("Choose organization");

            var dropdownRect = new Rect(
                dropdownX,
                yPosition,
                dropdownWidth,
                EditorGUIUtility.singleLineHeight
            );

            var popupStyle = new GUIStyle(EditorStyles.popup)
            {
                clipping = TextClipping.Ellipsis,
                fixedHeight = EditorGUIUtility.singleLineHeight
            };

            if (EditorGUI.DropdownButton(dropdownRect, new GUIContent(displayText), FocusType.Passive, popupStyle))
            {
                var menu = new GenericMenu();
                for (int i = 0; i < m_OrganizationInfos.Length; i++)
                {
                    var capturedIndex = i; // avoid modified closure
                    menu.AddItem(
                        new GUIContent(m_OrganizationInfos[capturedIndex].name),
                        m_SelectedOrgIndex == capturedIndex,
                        () => m_SelectedOrgIndex = capturedIndex
                    );
                }

                menu.DropDown(dropdownRect);
            }
        }

        private void RenderTopButtonsArea()
        {
            // Background
            GUILayout.BeginVertical();
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);

            GUI.enabled = m_Tree != null ? !m_Tree.isAllItemsEnabled : true;
            if (GUILayout.Button(Styles.allText, GUILayout.Width(50)))
            {
                m_Tree.SetAllEnabled(PackageExportTreeView.EnabledState.All);
                SendAnalyticsEvent("selectAll");
            }
            GUI.enabled = true;

            GUI.enabled = m_Tree != null ? m_Tree.isAnyItemEnabled : true;
            if (GUILayout.Button(Styles.noneText, GUILayout.Width(50)))
            {
                m_Tree.SetAllEnabled(PackageExportTreeView.EnabledState.None);
                SendAnalyticsEvent("selectNone");
            }
            GUI.enabled = true;

            GUILayout.Space(10);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            GUILayout.EndVertical();
        }

        private void RenderBottomArea()
        {
            // Background
            GUILayout.BeginVertical(Styles.bottomBarBg);
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);

            EditorGUI.BeginChangeCheck();
            var includeDependenciesNewValue = GUILayout.Toggle(m_IncludeDependencies, Styles.includeDependenciesText);
            if (m_IncludeDependencies != includeDependenciesNewValue)
            {
                m_IncludeDependencies = includeDependenciesNewValue;
                SendAnalyticsEvent("toggleIncludeDependencies");
            }

            GUILayout.Space(5);
            var includeScriptsNewValue = GUILayout.Toggle(m_IncludeScripts, Styles.includeScriptsText);
            if (m_IncludeScripts != includeScriptsNewValue)
            {
                m_IncludeScripts = includeScriptsNewValue;
                SendAnalyticsEvent("toggleIncludeScripts");
            }

            if (EditorGUI.EndChangeCheck())
            {
                RefreshAssetList();
            }

            GUILayout.FlexibleSpace();

            var isOrgIdChosen = isOrgSelected || m_OrganizationInfos.Length == 0;
            GUI.enabled = m_Tree?.isAnyItemEnabled == true && isOrgIdChosen;
            if (GUILayout.Button(EditorGUIUtility.TrTextContent("Export...")))
            {
                string invalidChars = m_EditorUtilityAdapter.GetInvalidFilenameChars();
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var selectedItemWithInvalidChar = m_ExportPackageItems.FirstOrDefault(item => Path.GetFileNameWithoutExtension(item.assetPath).IndexOfAny(invalidChars.ToCharArray()) != -1 && item.enabledStatus > 0);
#pragma warning restore UA2001
                if (selectedItemWithInvalidChar != null && !m_EditorUtilityAdapter.DisplayDialog(L10n.Tr("Cross platform incompatibility"), L10n.Tr($"The asset “{Path.GetFileNameWithoutExtension(selectedItemWithInvalidChar.assetPath)}” contains one or more characters that are not compatible across platforms: {invalidChars}"), L10n.Tr("I understand"), L10n.Tr("Cancel")))
                {
                    GUIUtility.ExitGUI();
                    SendAnalyticsEvent("exportErrorInvalidCharInAssetName");
                    return;
                }

                Export();
                GUIUtility.ExitGUI();
            }
            GUI.enabled = true;

            GUILayout.Space(10);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            GUILayout.EndVertical();
        }

        private void RenderTreeViewArea(bool showLoadingScreen)
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
            var fileName = m_EditorUtilityAdapter.SaveFilePanel("Export package ...", "", "", "unitypackage");
            if (fileName != "")
            {
                // build guid list
                List<string> guids = new List<string>();

                foreach (ExportPackageItem ai in m_ExportPackageItems)
                {
                    if (ai.enabledStatus > 0)
                        guids.Add(ai.guid);
                }

                SendAnalyticsEvent(ExportPackage(guids.ToArray(), fileName) ? "exportSuccess" : "exportFailed");

                Close();
                GUIUtility.ExitGUI();
            }
            else
            {
                SendAnalyticsEvent("exportCancelledAtFileSelection");
            }
        }

        internal bool ExportPackage (string[] guids, string fileName)
        {
            if (m_OrganizationInfos.Length == 0)
                m_UtilityAdapter.ExportPackage(guids, fileName, null);
            else
            {
                if (!isOrgSelected)
                {
                    Debug.Log(L10n.Tr("Package export failed because of an incorrect organization name. Please try again."));
                    return false;
                }
                m_UtilityAdapter.ExportPackage(guids, fileName, m_OrganizationInfos[m_SelectedOrgIndex].foreignKey);
            }

            return true;
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

            m_ProjectBrowserSelection ??= Selection.assetGUIDsDeepSelection;
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_ExportPackageItems = GetAssetItemsForExport(m_ProjectBrowserSelection, m_IncludeDependencies, m_IncludeScripts).ToArray();
#pragma warning restore UA2001

            // GUI is reconstructed in OnGUI (when needed)
            m_Tree = null;
            m_TreeViewState = null;

            Repaint();
        }

        private void SendAnalyticsEvent(string action)
        {
            var numSelectedAssets = 0;
            var numTotalAssets = 0;
            foreach (var i in m_ExportPackageItems)
            {
                if (i.isFolder)
                    continue;
                numTotalAssets++;
                if (i.enabledStatus > 0)
                    numSelectedAssets++;
            }
            AssetExportWindowAnalytics.SendEvent(action, numSelectedAssets, numTotalAssets, m_IncludeDependencies);
        }

        [AnalyticInfo(eventName: k_EventName, vendorKey: k_VendorKey)]
        internal class AssetExportWindowAnalytics : IAnalytic
        {
            private const string k_EventName = "assetExportWindow";
            private const string k_VendorKey = "unity.package-manager-ui";

            [Serializable]
            private class Data : IAnalytic.IData
            {
                public string action;
                public int num_selected_assets;
                public int num_total_assets;
                public bool include_dependencies;
            }

            private Data m_Data;
            private AssetExportWindowAnalytics(string action, int numSelectedAsset, int numTotalAssets, bool includeDependencies)
            {
                m_Data = new Data
                {
                    action = action,
                    num_selected_assets = numSelectedAsset,
                    num_total_assets = numTotalAssets,
                    include_dependencies = includeDependencies,
                };
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = m_Data;
                return data != null;
            }

            public static void SendEvent(string action, int numSelectedAsset, int numTotalAssets, bool includeDependencies)
            {
                EditorAnalytics.SendAnalytic(new AssetExportWindowAnalytics(action, numSelectedAsset, numTotalAssets, includeDependencies));
            }
        }
    }
}
