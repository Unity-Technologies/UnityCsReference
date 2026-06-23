// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UIElements;

namespace Unity.Hierarchy.Editor
{
    [InitializeOnLoad]
    static partial class HierarchyAnalytics
    {
        const string k_VendorKey = "unity.hierarchy";

        [Serializable]
        class ImplementationPayload : IAnalytic.IData
        {
            [SerializeField] public bool v2Enabled;
        }

        [Serializable]
        class SortPayload : IAnalytic.IData
        {
            [SerializeField] public bool alphanumericalSortEnabled;
        }

        [Serializable]
        class HierarchyCountPayload : IAnalytic.IData
        {
            [SerializeField] public int count;
        }

        [Serializable]
        class PreferencePayload : IAnalytic.IData
        {
            [SerializeField] public bool queryBuilderEnabled;
            [SerializeField] public bool alternatingRowColorsEnabled;
            [SerializeField] public string gameObjectIconsMode;
            [SerializeField] public bool renameNewObjects;
            [SerializeField] public string[] columnsTracked;
        }

        [AnalyticInfo(eventName: "hierarchyImplementationChanged", vendorKey: k_VendorKey)]
        class HierarchyImplementationChanged : IAnalytic
        {
            ImplementationPayload m_Payload;

            public HierarchyImplementationChanged(ImplementationPayload payload) => m_Payload = payload;

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = m_Payload;
                return data != null;
            }
        }

        [AnalyticInfo(eventName: "hierarchyV1SortChanged", vendorKey: k_VendorKey)]
        class HierarchyV1SortChanged : IAnalytic
        {
            SortPayload m_Payload;

            public HierarchyV1SortChanged(SortPayload payload) => m_Payload = payload;

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = m_Payload;
                return data != null;
            }
        }

        [AnalyticInfo(eventName: "hierarchyPreferencesChanged", vendorKey: k_VendorKey)]
        class HierarchyPreferencesChanged : IAnalytic
        {
            PreferencePayload m_Payload;

            public HierarchyPreferencesChanged(PreferencePayload payload) => m_Payload = payload;

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = m_Payload;
                return data != null;
            }
        }

        [AnalyticInfo(eventName: "hierarchyCountChanged", vendorKey: k_VendorKey)]
        class HierarchyCountChanged : IAnalytic
        {
            HierarchyCountPayload m_Payload;

            public HierarchyCountChanged(HierarchyCountPayload payload) => m_Payload = payload;

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = m_Payload;
                return data != null;
            }
        }

        [AutoStaticsCleanupOnCodeReload]
        static int s_WindowCount = 0;

        static HierarchyAnalytics()
        {
            // Make sure events aren't sent during assembly reload to not spam send lifecycle related events
            AssemblyReloadEvents.afterAssemblyReload += Initialize;
            AssemblyReloadEvents.beforeAssemblyReload += () => s_ShouldSendEvents = false;

            EditorSettings.useLegacyHierarchyChanged += SendImplementationChanged;

            HierarchyPreferences.UseQueryBuilder.valueChanged += RequestPreferenceChangeEvent;
            HierarchyPreferences.AlternatingRowBackground.valueChanged += RequestPreferenceChangeEvent;
            HierarchyPreferences.GameObjectIconModeChanged += RequestPreferenceChangeEvent;
            HierarchyPreferences.RenameNewObjects.valueChanged += RequestPreferenceChangeEvent;

            HierarchyPreferences.AllowAlphaNumericHierarchy.valueChanged += SendHierarchyV1SortChanged;
        }

        public static void AddWindow(HierarchyWindow window)
        {
            ++s_WindowCount;
            var columns = window.View.ListViewLayoutConfiguration.header.columns;
            columns.columnChanged += OnColumnChanged;
            SendWindowCountChanged();
        }

        public static void RemoveWindow(HierarchyWindow window)
        {
            --s_WindowCount;
            SendWindowCountChanged();
        }

        static void OnColumnChanged(Column column, ColumnDataType type)
        {
            if (type == ColumnDataType.Visibility)
                RequestPreferenceChangeEvent();
        }

        const string k_bootEventSentKey = "hierarchy.analytics.boot-events-sent";
        [AutoStaticsCleanupOnCodeReload]
        static bool s_ShouldSendEvents = false;
        [AutoStaticsCleanupOnCodeReload]
        static bool s_ColumnsChangedRequested = false;

        // Doesn't use ToString to ensure that any changes to the enum name doesn't change what the analytics receive
        static string GetIconModeString(HierarchyPreferences.IconMode mode)
        {
            switch (mode)
            {
                case HierarchyPreferences.IconMode.ComponentsAndGizmos: return "Components And Gizmos";
                case HierarchyPreferences.IconMode.ComponentsOnly: return "Components Only";
                case HierarchyPreferences.IconMode.GameObjectOnly: return "GameObject Only";
                default: return "";
            }
        }

        static void Initialize()
        {
            s_ShouldSendEvents = true;
            if (!SessionState.GetBool(k_bootEventSentKey, false))
            {
                SessionState.SetBool(k_bootEventSentKey, true);

                // Delay by 1 frame to ensure we don't have ordering problems on boot
                EditorApplication.delayCall += () =>
                {
                    // We send all our analytics data when project is first opened
                    SendImplementationChanged();
                    SendHierarchyV1SortChanged();
                    SendWindowCountChanged();
                    SendPreferenceChanged();
                };
            }
        }

        static void SendImplementationChanged()
        {
            SendAnalytic(new HierarchyImplementationChanged(new ImplementationPayload { v2Enabled = !EditorSettings.useLegacyHierarchy}));
        }

        static void SendHierarchyV1SortChanged()
        {
            SendAnalytic(new HierarchyV1SortChanged(new SortPayload{ alphanumericalSortEnabled = HierarchyPreferences.AllowAlphaNumericHierarchy }));
        }

        // Batched in case we get multiple change in a single frame
        static void RequestPreferenceChangeEvent()
        {
            if (!s_ColumnsChangedRequested)
            {
                s_ColumnsChangedRequested = true;
                EditorApplication.delayCall += () =>
                {
                    s_ColumnsChangedRequested = false;
                    SendPreferenceChanged();
                };
            }
        }

        static void SendWindowCountChanged()
        {
            SendAnalytic(new HierarchyCountChanged(new HierarchyCountPayload { count = s_WindowCount }));
        }

        static void SendPreferenceChanged()
        {
            List<string> columnsTracked = new List<string>();
            foreach (var window in EditorWindow.activeEditorWindows)
            {
                if (window is HierarchyWindow hierarchy && hierarchy.View != null)
                {
                    foreach (var columnState in hierarchy.View.GetState(HierarchyViewState.Content.Columns).Columns)
                    {
                        if (columnState.Visible && !columnsTracked.Contains(columnState.ColumnId))
                        {
                            columnsTracked.Add(columnState.ColumnId);
                        }
                    }
                }
            }

            SendAnalytic(new HierarchyPreferencesChanged(new PreferencePayload()
            {
                queryBuilderEnabled = HierarchyPreferences.UseQueryBuilder,
                alternatingRowColorsEnabled = HierarchyPreferences.AlternatingRowBackground,
                gameObjectIconsMode = GetIconModeString(HierarchyPreferences.GameObjectIconMode),
                renameNewObjects = HierarchyPreferences.RenameNewObjects,
                columnsTracked = columnsTracked.ToArray(),
            }));
        }

        static void SendAnalytic(IAnalytic analytic)
        {
            if (s_ShouldSendEvents)
            {
                EditorAnalytics.SendAnalytic(analytic);
            }
        }
    }
}
