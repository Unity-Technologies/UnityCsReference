// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.Build;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.UIR;

namespace UnityEditor.ShaderVariantAnalyser
{
    abstract class PanelBase : VisualElement
    {
        public abstract bool BindData(DataReader reader, IPanelData data);
        public abstract void Unbind();
        public virtual void SetDisplay(bool visible)
        {
            style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    enum PanelType
    {
        Overview = 0,
        Index,
        Details,
    }

    interface IPanelData
    {
        public string Caption { get; }
        public PanelType Panel { get; }
    }

    partial class ShaderVariantAnalyser : EditorWindow
    {
        const string k_Name = "Shader Variant Analyzer";
        const string k_LastBuildPrefs = "VariantAnalyser_LastBuild";
        const string k_DismissedBuildPrefs = "VariantAnalyser_DismissedBuild";

        DropdownField m_BuildSource;
        ToolbarBreadcrumbs m_Breadcrumbs;
        VisualElement m_NoContentPanel;
        VisualElement m_ContentPanel;
        VisualElement m_NewBuildBanner;
        Label m_NewBuildBannerText;

        OverviewPanel m_Overview;
        IndexPanel m_IndexPanel;
        DetailsPanel m_Details;

        GUID m_LoadedBuild;
        DataReader m_DataReader;
        [SerializeReference] List<IPanelData> m_PanelStack = new();

        static VisualTreeAsset WindowAsset => (VisualTreeAsset)EditorGUIUtility.Load("ShaderVariantAnalyser/VariantAnalyserWindow.uxml");

        static StyleSheet ThemeStylesheet => (StyleSheet)EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? "ShaderVariantAnalyser/ShaderVariantAnalyser_Dark.uss" : "ShaderVariantAnalyser/ShaderVariantAnalyser_Light.uss");

        [MenuItem("internal:Window/Internal/" + k_Name)]
        public static void OpenWindow()
        {
            if (HasOpenInstances<ShaderVariantAnalyser>())
                FocusWindowIfItsOpen<ShaderVariantAnalyser>();
            else
            {
                if (GUID.TryParse(EditorPrefs.GetString(k_LastBuildPrefs), out var build))
                    OpenBuild(build);
                else
                    OpenBuild(new());
            }
        }

        public static void OpenBuild(GUID build)
        {
            OpenBuild_Internal(build)?.BindActiveData();
        }

        public static void OpenBuildShader(GUID build, GUID shader)
        {
            var win = OpenBuild_Internal(build);
            if (!win)
                win = CreateNewWindow();

            win.BindActiveData();

            if (win.m_DataReader == null)
                return;

            var idx = win.m_DataReader.Shaders.FindAssetIndex(shader);
            if (idx >= 0)
                win.OpenDetails(true, idx);
            else
                Debug.LogError($"[ShaderVariantAnalyser] Couldn't find a Shader with GUID {shader}");
        }

        public static void OpenBuildMaterial(GUID build, GUID mat)
        {
            var win = OpenBuild_Internal(build);
            if (!win)
                win = CreateNewWindow();

            win.BindActiveData();

            if (win.m_DataReader == null)
                return;

            var idx = win.m_DataReader.Materials.FindAssetIndex(mat);
            if (idx >= 0)
                win.OpenDetails(false, idx);
            else
                Debug.LogError($"[ShaderVariantAnalyser] Couldn't find a Material with GUID {mat}");
        }

        static ShaderVariantAnalyser OpenBuild_Internal(GUID build)
        {
            var res = CreateNewWindow();

            if (res.m_LoadedBuild == build)
                return null;

            EditorPrefs.SetString(k_LastBuildPrefs, build.ToString());

            if (res.m_DataReader != null)
            {
                res.m_DataReader.Dispose();
                res.m_DataReader = null;
                res.ReturnToPanel(-1);
            }
            res.m_LoadedBuild = build;
            return res;
        }

        static ShaderVariantAnalyser CreateNewWindow()
        {
            return GetWindow<ShaderVariantAnalyser>(k_Name, true);
        }

        bool TryParseBuildTimestamp(string isoUtc, out DateTimeOffset value)
        {
            return DateTimeOffset.TryParseExact(isoUtc, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value);
        }

        string GetBuildName(GUID build)
        {
            BuildReportSummary summary = BuildHistory.GetBuildSummary(build);
            var name = $"{summary.PlatformName} • {summary.BuildTypeName} • {summary.BuildName}";
            if (TryParseBuildTimestamp(summary.BuildStartedAt, out var parsed))
                return name + parsed.ToLocalTime().ToString(" • yyyy'-'MM'-'dd HH:mm");
            return name;
        }

        public void RefreshBuildList()
        {
            if (!GUID.TryParse(EditorPrefs.GetString(k_DismissedBuildPrefs), out var dismissedBuild))
                dismissedBuild = new GUID();

            BuildHistory.Refresh();

            var builds = BuildHistory.GetAllBuilds();
            var buildNames = new List<string>(builds.Length + 1);
            int selected = 0;
            buildNames.Add("No Build");

            for (int i = 0; i < builds.Length; i++)
            {
                buildNames.Add(GetBuildName(builds[i]));
                if (builds[i] == m_LoadedBuild)
                    selected = i + 1;
            }
            m_BuildSource.choices = buildNames;
            m_BuildSource.index = selected;

            if (selected == 1)
            {
                // If we open the build, don't inform us about it anymore
                EditorPrefs.SetString(k_DismissedBuildPrefs, m_LoadedBuild.ToString());
                m_NewBuildBanner.style.display = DisplayStyle.None;
            }
            else if (builds.Length > 0 && dismissedBuild != builds[0])
            {
                m_NewBuildBannerText.text = $"A more recent build is available: {buildNames[1]}";
                m_NewBuildBanner.style.display = DisplayStyle.Flex;
            }
            else
                m_NewBuildBanner.style.display = DisplayStyle.None;
        }

        void BindActiveData()
        {
            RefreshBuildList();

            if (m_LoadedBuild.Empty())
            {
                m_ContentPanel?.style.display = DisplayStyle.None;
                m_NoContentPanel?.style.display = DisplayStyle.Flex;
                return;
            }
            m_ContentPanel?.style.display = DisplayStyle.Flex;
            m_NoContentPanel?.style.display = DisplayStyle.None;

            if (!BuildHistory.TryGetBuildReportDirectory(m_LoadedBuild, out var buildDir))
            {
                Debug.LogWarning($"[ShaderVariantAnalyser] Can't find the directory for build {m_LoadedBuild}");
                OpenBuild(new());
                return;
            }

            if (m_DataReader == null)
                m_DataReader = DataReader.LoadFolder(buildDir);

            if (m_PanelStack.Count == 0)
                OpenOverview();
            else
            {
                m_Breadcrumbs.Clear();
                for (int i = 0; i < m_PanelStack.Count; ++i)
                {
                    if (!GetPanel(m_PanelStack[i].Panel).BindData(m_DataReader, m_PanelStack[i]))
                    {
                        Debug.LogError("[ShaderVariantAnalyser] Couldn't bind data for panel");
                        m_PanelStack.RemoveRange(i, m_PanelStack.Count - i);
                        break;
                    }
                    int idx = i;
                    m_Breadcrumbs.PushItem(m_PanelStack[i].Caption, () => ReturnToPanel(idx));
                }

                DisplayTopPanel(true);
            }
        }

        PanelBase GetPanel(PanelType panel)
        {
            switch (panel)
            {
                case PanelType.Overview:
                    return m_Overview;
                case PanelType.Index:
                    return m_IndexPanel;
                case PanelType.Details:
                    return m_Details;
            }
            return null;
        }

        void RegisterPanel(IPanelData data)
        {
            int idx = m_PanelStack.Count;
            m_Breadcrumbs.PushItem(data.Caption, () => ReturnToPanel(idx));
            m_PanelStack.Add(data);
        }

        void DisplayTopPanel(bool visible)
        {
            if (m_PanelStack.Count > 0)
                GetPanel(m_PanelStack[^1].Panel).SetDisplay(visible);
        }

        void ReturnToPanel(int idx, bool show = true)
        {
            var target = idx + 1;

            if (m_PanelStack.Count <= target)
            {
                DisplayTopPanel(show);
                return;
            }

            DisplayTopPanel(false);

            while (m_PanelStack.Count > target)
                PopPanel();

            if (show)
                DisplayTopPanel(true);
        }

        void PopPanel()
        {
            m_Breadcrumbs.PopItem();
            var data = m_PanelStack[^1];
            GetPanel(data.Panel).Unbind();
            m_PanelStack.RemoveAt(m_PanelStack.Count - 1);
        }

        void OnDisable()
        {
            if (m_DataReader != null)
            {
                m_DataReader.Dispose();
                m_DataReader = null;
            }
        }

        void OpenOverview(bool show = true)
        {
            ReturnToPanel(0, show);

            if (m_PanelStack.Count == 0)
            {
                var data = m_Overview.BindData(m_DataReader);
                if (data == null)
                {
                    Debug.LogError("[ShaderVariantAnalyser] Failed to open Overview panel");
                    return;
                }

                RegisterPanel(data);
                if (show)
                    DisplayTopPanel(true);
            }
        }

        void OpenIndex(bool shaders, bool show = true)
        {
            ReturnToPanel(1, false);

            // Check if already open
            if (m_PanelStack.Count == 2)
            {
                var data = m_PanelStack[1] as IndexPanel.Data;
                if (data != null && data.isShader == shaders)
                {
                    DisplayTopPanel(show);
                    return;
                }
            }

            OpenOverview(false);

            var newData = m_IndexPanel.BindData(m_DataReader, shaders);
            if (newData == null)
            {
                Debug.LogError("[ShaderVariantAnalyser] Failed to open Index panel");
                return;
            }

            RegisterPanel(newData);
            if (show)
                DisplayTopPanel(true);
        }

        void OpenDetails(bool shader, int idx)
        {
            ReturnToPanel(2, false);
            if (m_PanelStack.Count == 3)
            {
                var data = m_PanelStack[2] as DetailsPanel.Data;
                if (data != null && data.isShader == shader && data.idx == idx)
                {
                    DisplayTopPanel(true);
                    return;
                }
            }

            OpenIndex(shader, false);

            var newData = m_Details.BindData(m_DataReader, idx, shader);
            if (newData == null)
            {
                Debug.LogError("[ShaderVariantAnalyser] Failed to open Details panel");
                return;
            }

            RegisterPanel(newData);
            DisplayTopPanel(true);
        }

        void CreateGUI()
        {
            rootVisualElement.styleSheets.Add(ThemeStylesheet);
            WindowAsset.CloneTree(rootVisualElement);

            m_NoContentPanel = rootVisualElement.Q<VisualElement>("no-content-panel");
            m_ContentPanel = rootVisualElement.Q<VisualElement>("content-panel");

            m_NewBuildBanner = rootVisualElement.Q<VisualElement>("newer-build-banner");
            m_NewBuildBannerText = rootVisualElement.Q<Label>("recent-build-label");
            rootVisualElement.Q<Button>("load-new-build").clicked += () =>
            {
                if (BuildHistory.TryGetLatestBuild(out var build))
                    OpenBuild(build);
            };
            rootVisualElement.Q<Button>("ignore-new-build").clicked += () =>
            {
                if (BuildHistory.TryGetLatestBuild(out var build))
                {
                    EditorPrefs.SetString(k_DismissedBuildPrefs, build.ToString());
                    m_NewBuildBanner.style.display = DisplayStyle.None;
                }
            };

            m_BuildSource = rootVisualElement.Q<DropdownField>("source-dropdown");
            m_BuildSource.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                var builds = BuildHistory.GetAllBuilds();
                if (m_BuildSource.index == 0 || m_BuildSource.index > builds.Length)
                    OpenBuild(new GUID());
                else
                    OpenBuild(builds[m_BuildSource.index - 1]);
            });

            m_Breadcrumbs = rootVisualElement.Q<ToolbarBreadcrumbs>("toolbar-breadcrumbs");
            m_Overview = rootVisualElement.Q<OverviewPanel>("overview-panel");
            m_Overview.OnOpenIndexClicked += (shaders) => OpenIndex(shaders);
            m_Overview.OnItemSelected += (shaders, idx) => OpenDetails(shaders, idx);
            m_Overview.SetDisplay(false);

            m_IndexPanel = rootVisualElement.Q<IndexPanel>("index-panel");
            m_IndexPanel.OnItemSelected += (shaders, idx) => OpenDetails(shaders, idx);
            m_IndexPanel.SetDisplay(false);

            m_Details = rootVisualElement.Q<DetailsPanel>("details-panel");
            m_Details.SetDisplay(false);

            BindActiveData();
        }
    }
}
