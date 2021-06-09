// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using System.Text;
using System.Collections.Generic;
using UnityEditor.Profiling;


namespace UnityEditorInternal.Profiling
{
    [Serializable]
    [ProfilerModuleMetadata("Physics", typeof(LocalizationResource), IconPath = "Profiler.Physics")]
    internal class PhysicsProfilerModule : ProfilerModuleBase
    {
        private enum PhysicsProfilerStatsView
        {
            Legacy = 0,
            Current = 1
        }

        const string k_IconName = "Profiler.Physics";
        const int k_DefaultOrderIndex = 6;
        static readonly string k_UnLocalizedName = "Physics";
        static readonly string k_Name = LocalizationDatabase.GetLocalizedString(k_UnLocalizedName);

        internal override ProfilerArea area => ProfilerArea.Physics;

        private protected override int defaultOrderIndex => k_DefaultOrderIndex;
        private protected override string legacyPreferenceKey => "ProfilerChartPhysics";
        static readonly string k_PhysicsCountersCategoryName = ProfilerCategory.Physics.Name;
        static readonly string k_MemoryCountersCategoryName = ProfilerCategory.Memory.Name;

        private PhysicsProfilerStatsView m_ShowStatsView;
        private PhysicsProfilerStatsView m_CachedShowStatsView;

        private static int k_labelWidthTitle = 220;
        private static int k_labelWidthDetail = 120;

        static readonly ProfilerCounterData[] k_DefaultPhysicsAreaCounterNames =
        {
            new ProfilerCounterData()
            {
                m_Name = "Physics Used Memory",
                m_Category = k_MemoryCountersCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Active Dynamic Bodies",
                m_Category = k_PhysicsCountersCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Active Kinematic Bodies",
                m_Category = k_PhysicsCountersCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Dynamic Bodies",
                m_Category = k_PhysicsCountersCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Active Constraints",
                m_Category = k_PhysicsCountersCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Overlaps",
                m_Category = k_PhysicsCountersCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Trigger Overlaps",
                m_Category = k_PhysicsCountersCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Discreet Overlaps",
                m_Category = k_PhysicsCountersCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Continuous Overlaps",
                m_Category = k_PhysicsCountersCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Physics Queries",
                m_Category = k_PhysicsCountersCategoryName,
            }
        };

        static readonly ProfilerCounterData[] k_LegacyPhysicsAreaCounterNames =
        {
            new ProfilerCounterData()
            {
                m_Name = "Active Dynamic",
                m_Category = k_PhysicsCountersCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Active Kinematic",
                m_Category = k_PhysicsCountersCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Static Colliders",
                m_Category = k_PhysicsCountersCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Rigidbody",
                m_Category = k_PhysicsCountersCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Trigger Overlaps",
                m_Category = k_PhysicsCountersCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Active Constraints",
                m_Category = k_PhysicsCountersCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Contacts",
                m_Category = k_PhysicsCountersCategoryName,
            }
        };

        internal override void OnEnable()
        {
            m_ShowStatsView = PhysicsProfilerStatsView.Current;
            ProfilerDriver.profileLoaded += OnLoadProfileData;
            LegacyModuleInitialize();
            base.OnEnable();
        }

        internal override void OnDisable()
        {
            ProfilerDriver.profileLoaded -= OnLoadProfileData;
            base.OnDisable();
        }

        void OnLoadProfileData()
        {
            using (var f = ProfilerDriver.GetRawFrameDataView(ProfilerWindow.GetActiveVisibleFrameIndex(), 0))
            {
                if (f.valid)
                {
                    var physicsQueries = GetCounterValue(f, "Physics Queries");
                    if (physicsQueries != -1)
                    {
                        m_ShowStatsView = PhysicsProfilerStatsView.Current;
                    }
                    else
                    {
                        m_ShowStatsView = PhysicsProfilerStatsView.Legacy;
                    }
                }
            }
        }

        void UpdatePhysicsChart()
        {
            if (m_ShowStatsView == PhysicsProfilerStatsView.Current)
            {
                InternalSetChartCounters(ProfilerCounterDataUtility.ConvertFromLegacyCounterDatas(
                    new List<ProfilerCounterData>(k_DefaultPhysicsAreaCounterNames)));
            }
            else
            {
                m_ShowStatsView = PhysicsProfilerStatsView.Legacy;
                InternalSetChartCounters(ProfilerCounterDataUtility.ConvertFromLegacyCounterDatas(
                    new List<ProfilerCounterData>(k_LegacyPhysicsAreaCounterNames)));
            }
            RebuildChart();
        }

        public override void DrawToolbar(Rect position)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            m_ShowStatsView = (PhysicsProfilerStatsView)EditorGUILayout.EnumPopup(m_ShowStatsView, EditorStyles.toolbarDropDownLeft, GUILayout.Width(70f));

            if (m_CachedShowStatsView != m_ShowStatsView)
            {
                m_CachedShowStatsView = m_ShowStatsView;
                UpdatePhysicsChart();
            }
            GUILayout.Space(5f);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        public override void DrawDetailsView(Rect position)
        {
            string activeText = string.Empty;
            using (var f = ProfilerDriver.GetRawFrameDataView(ProfilerWindow.GetActiveVisibleFrameIndex(), 0))
            {
                if (f.valid)
                {
                    if (m_ShowStatsView == PhysicsProfilerStatsView.Current)
                    {
                        var stringBuilder = new StringBuilder(1024);
                        stringBuilder.Append("Physics Used Memory: " + GetCounterValueAsBytes(f, "Physics Used Memory"));

                        stringBuilder.Append("\n\nDynamic Bodies: " + GetCounterValue(f, "Dynamic Bodies"));
                        stringBuilder.Append("\nArticulation Bodies: " + GetCounterValue(f, "Articulation Bodies"));

                        stringBuilder.Append("\n\nActive Dynamic Bodies: " + GetCounterValue(f, "Active Dynamic Bodies"));
                        stringBuilder.Append("\nActive Kinematic Bodies: " + GetCounterValue(f, "Active Kinematic Bodies"));
                        stringBuilder.Append("\nActive Constraints: " + GetCounterValue(f, "Active Constraints"));

                        stringBuilder.Append("\n\nStatic Colliders: " + GetCounterValue(f, "Static Colliders"));
                        stringBuilder.Append($"\nColliders Synced: {GetCounterValue(f, "Colliders Synced")}");
                        stringBuilder.Append($"\nRigidbodies Synced: {GetCounterValue(f, "Rigidbodies Synced")}");

                        stringBuilder.Append("\n\nPhysics Queries: " + GetCounterValue(f, "Physics Queries"));

                        activeText = stringBuilder.ToString();
                    }
                    else
                    {
                        // Old data compatibility.
                        activeText = ProfilerDriver.GetOverviewText(ProfilerArea.Physics, ProfilerWindow.GetActiveVisibleFrameIndex());
                    }
                }
            }

            float height = EditorStyles.wordWrappedLabel.CalcHeight(GUIContent.Temp(activeText), position.width);
            m_PaneScroll = GUILayout.BeginScrollView(m_PaneScroll, ProfilerWindow.Styles.background);
            EditorGUILayout.SelectableLabel(activeText, EditorStyles.wordWrappedLabel, GUILayout.MinHeight(height));
            if (m_ShowStatsView == PhysicsProfilerStatsView.Current && activeText != string.Empty)
            {
                DrawHorizontalDetails();
            }
            GUILayout.EndScrollView();
        }

        void DrawHorizontalDetails()
        {
            using (var f = ProfilerDriver.GetRawFrameDataView(ProfilerWindow.GetActiveVisibleFrameIndex(), 0))
            {
                if (f.valid)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Total Overlaps: " + GetCounterValue(f, "Overlaps"), GUILayout.Width(k_labelWidthTitle));
                    EditorGUILayout.LabelField("| Discreet: " + GetCounterValue(f, "Discreet Overlaps"), GUILayout.Width(k_labelWidthDetail));
                    EditorGUILayout.LabelField("Continuous: " + GetCounterValue(f, "Continuous Overlaps"), GUILayout.Width(k_labelWidthDetail));
                    EditorGUILayout.LabelField("Trigger: " + GetCounterValue(f, "Trigger Overlaps"), GUILayout.Width(k_labelWidthDetail));
                    EditorGUILayout.LabelField("Modified: " + GetCounterValue(f, "Modified Overlaps"), GUILayout.Width(k_labelWidthDetail));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Broadphase Adds/Removes: " + GetCounterValue(f, "Broadphase Adds/Removes"), GUILayout.Width(k_labelWidthTitle));
                    EditorGUILayout.LabelField("| Adds: " + GetCounterValue(f, "Broadphase Adds"), GUILayout.Width(k_labelWidthDetail));
                    EditorGUILayout.LabelField("Removes: " + GetCounterValue(f, "Broadphase Removes"), GUILayout.Width(k_labelWidthDetail));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Narrowphase Touches: " + GetCounterValue(f, "Narrowphase Touches"), GUILayout.Width(k_labelWidthTitle));
                    EditorGUILayout.LabelField("| New: " + GetCounterValue(f, "Narrowphase New Touches"), GUILayout.Width(k_labelWidthDetail));
                    EditorGUILayout.LabelField("Lost: " + GetCounterValue(f, "Narrowphase Lost Touches"), GUILayout.Width(k_labelWidthDetail));
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        protected override List<ProfilerCounterData> CollectDefaultChartCounters()
        {
            if (m_ShowStatsView != PhysicsProfilerStatsView.Legacy)
                return new List<ProfilerCounterData>(k_DefaultPhysicsAreaCounterNames);

            return new List<ProfilerCounterData>(k_LegacyPhysicsAreaCounterNames);
        }
    }
}
