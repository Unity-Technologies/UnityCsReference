// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using System.Collections.Generic;
using UnityEditor.Profiling;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    [ProfilerModuleMetadata("Physics (2D)", typeof(LocalizationResource), IconPath = "Profiler.Physics2D")]
    internal class Physics2DProfilerModule : ProfilerModuleBase
    {
        // Styles used to display the alternating background in the detail view.
        private static class DefaultStyles
        {
            public static GUIStyle backgroundEven = "OL EntryBackEven";
            public static GUIStyle backgroundOdd = "OL EntryBackOdd";
        }

        // The profiler view to use.
        private enum PhysicsProfilerStatsView
        {
            Legacy = 0,
            Current = 1
        }

        private PhysicsProfilerStatsView m_ShowStatsView;
        private PhysicsProfilerStatsView m_CachedShowStatsView;

        // Current Counters to display.
        static readonly ProfilerCounterData[] k_CurrentPhysicsAreaCounterNames =
        {
            new ProfilerCounterData()
            {
                m_Name = "Total Contacts",
                m_Category = k_Physics2DCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Total Shapes",
                m_Category = k_Physics2DCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Total Queries",
                m_Category = k_Physics2DCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Total Callbacks",
                m_Category = k_Physics2DCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Total Joints",
                m_Category = k_Physics2DCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Total Bodies",
                m_Category = k_Physics2DCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Awake Bodies",
                m_Category = k_Physics2DCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Dynamic Bodies",
                m_Category = k_Physics2DCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Continuous Bodies",
                m_Category = k_Physics2DCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Physics Used Memory (2D)",
                m_Category = k_MemoryCategoryName,
            }
        };

        // Legacy Counters to display.
        static readonly ProfilerCounterData[] k_LegacyPhysicsAreaCounterNames =
        {
            new ProfilerCounterData()
            {
                m_Name = "Total Bodies",
                m_Category = k_Physics2DCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Active Bodies",
                m_Category = k_Physics2DCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Sleeping Bodies",
                m_Category = k_Physics2DCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Dynamic Bodies",
                m_Category = k_Physics2DCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Kinematic Bodies",
                m_Category = k_Physics2DCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Static Bodies",
                m_Category = k_Physics2DCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Contacts",
                m_Category = k_Physics2DCategoryName,
            }
        };

        const int k_DefaultOrderIndex = 7;
        private static readonly ushort k_Physics2DCategoryId = ProfilerCategory.Physics2D;
        private static readonly string k_Physics2DCategoryName = ProfilerCategory.Physics2D.Name;
        private static readonly string k_MemoryCategoryName = ProfilerCategory.Memory.Name;
        private static int k_labelWidthTitle = 150;
        private static int k_labelWidthDetail = 160;

        // Profiler module overrides.
        internal override ProfilerArea area => ProfilerArea.Physics2D;
        public override bool usesCounters => false;
        private protected override int defaultOrderIndex => k_DefaultOrderIndex;
        private protected override string legacyPreferenceKey => "ProfilerChartPhysics2D";

        private Dictionary<string, int> m_Markers;

        // Storage for retrieved samples.
        private struct SampleData
        {
            public float timeMs;
            public int count;

            public override string ToString()
            {
                return string.Format("{0:0.00} ms [{1}]", timeMs, count);
            }
        }
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

            m_Markers = null;

            base.OnDisable();
        }

        private void OnLoadProfileData()
        {
            using (var frameData = ProfilerDriver.GetRawFrameDataView(ProfilerWindow.GetActiveVisibleFrameIndex(), 0))
            {
                if (frameData.valid)
                {
                    var physicsQueries = GetCounterValue(frameData, "Total Queries");
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

        private void InitializeMarkers(RawFrameDataView frameData)
        {
            // Fetch all the markers.
            var markerInfo = new List<FrameDataView.MarkerInfo>();
            frameData.GetMarkers(markerInfo);

            // Assign the markers.
            m_Markers = new Dictionary<string, int>(64);
            foreach(var marker in markerInfo)
            {
                if (marker.category == k_Physics2DCategoryId)
                    m_Markers.Add(marker.name, marker.id);
            }
        }

        private long GetPhysicsCounterValue(RawFrameDataView frameData, string markerName)
        {
            // Find the marker.
            if (m_Markers.TryGetValue(markerName, out int markerId))
                return frameData.GetCounterValueAsLong(markerId);

            // Indicate bad value!
            return -1;
        }

        private SampleData GetPhysicsSampleData(RawFrameDataView frameData, string markerName)
        {
            SampleData sampleData = default;

            // Find the marker.
            if (m_Markers.TryGetValue(markerName, out int markerId))
            {
                for (var i = 0; i < frameData.sampleCount; ++i)
                {
                    // Ignore if it's not the marker we want.
                    if (markerId != frameData.GetSampleMarkerId(i))
                        continue;

                    // Accumulate sample data.
                    sampleData.timeMs += frameData.GetSampleTimeMs(i);
                    sampleData.count++;
                }
            }

            return sampleData;
        }

        private void UpdatePhysicsChart()
        {
            if (m_ShowStatsView == PhysicsProfilerStatsView.Current)
            {
                InternalSetChartCounters(ProfilerCounterDataUtility.ConvertFromLegacyCounterDatas(
                    new List<ProfilerCounterData>(k_CurrentPhysicsAreaCounterNames)));
            }
            else
            {
                m_ShowStatsView = PhysicsProfilerStatsView.Legacy;
                InternalSetChartCounters(ProfilerCounterDataUtility.ConvertFromLegacyCounterDatas(
                    new List<ProfilerCounterData>(k_LegacyPhysicsAreaCounterNames)));
            }

            RebuildChart();
        }

        private void DrawAlternateBackground(Rect region, Vector2 scrollPosition, int rowCount)
        {
            // Only draw if repainting.
            if (Event.current.rawType != EventType.Repaint)
                return;

            // Fetch the label height.
            var labelHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Draw the selected rows.
            for (int row = 0; row < rowCount; ++row)
            {
                // Only draw alternate rows.
                if (row % 2 == 1)
                    continue;

                // Calculate and draw the row.
                var rowRect = new Rect(region.x + scrollPosition.x, region.y + ((row-1) * labelHeight), region.width, labelHeight);
                DefaultStyles.backgroundEven.Draw(rowRect, false, false, false, false);
            }
        }

        protected override List<ProfilerCounterData> CollectDefaultChartCounters()
        {
            if (m_ShowStatsView == PhysicsProfilerStatsView.Current)
                return new List<ProfilerCounterData>(k_CurrentPhysicsAreaCounterNames);

            return new List<ProfilerCounterData>(k_LegacyPhysicsAreaCounterNames);
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
            m_PaneScroll = GUILayout.BeginScrollView(m_PaneScroll, ProfilerWindow.Styles.background);

            using (var frameData = ProfilerDriver.GetRawFrameDataView(ProfilerWindow.GetActiveVisibleFrameIndex(), 0))
            {
                if (frameData.valid)
                {
                    // Initialize the profiler markers.
                    InitializeMarkers(frameData);

                    bool newCountersAvailable = GetPhysicsCounterValue(frameData, "Total Queries") != -1;

                    if (m_ShowStatsView == PhysicsProfilerStatsView.Current)
                    {
                        // Determine if the new counters are available by looking for a counter only available there.
                        if (newCountersAvailable)
                        {
                            // Draw an alternate lined background to make following metrics on the same line easier.
                            DrawAlternateBackground(position, m_PaneScroll, 10);

                            long physicsMemoryUsed = GetCounterValue(frameData, "Physics Used Memory (2D)");
                            long totalUsedMemory = GetCounterValue(frameData, "Total Used Memory");
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Physics Used Memory", GUILayout.Width(k_labelWidthTitle));
                            EditorGUILayout.LabelField("| Total: " + GetCounterValueAsBytes(frameData, "Physics Used Memory (2D)"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField(string.Format("| Relative: {0:p2}", (float)physicsMemoryUsed / (float)totalUsedMemory), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Bodies", GUILayout.Width(k_labelWidthTitle));
                            EditorGUILayout.LabelField("| Total: " + GetPhysicsCounterValue(frameData, "Total Bodies"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Awake: " + GetPhysicsCounterValue(frameData, "Awake Bodies"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Asleep: " + GetPhysicsCounterValue(frameData, "Asleep Bodies"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Dynamic: " + GetPhysicsCounterValue(frameData, "Dynamic Bodies"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Kinematic: " + GetPhysicsCounterValue(frameData, "Kinematic Bodies"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Static: " + GetPhysicsCounterValue(frameData, "Static Bodies"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Discrete: " + GetPhysicsCounterValue(frameData, "Discrete Bodies"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Continuous: " + GetPhysicsCounterValue(frameData, "Continuous Bodies"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Shapes", GUILayout.Width(k_labelWidthTitle));
                            EditorGUILayout.LabelField("| Total: " + GetPhysicsCounterValue(frameData, "Total Shapes"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Awake: " + GetPhysicsCounterValue(frameData, "Awake Shapes"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Asleep: " + GetPhysicsCounterValue(frameData, "Asleep Shapes"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Dynamic: " + GetPhysicsCounterValue(frameData, "Dynamic Shapes"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Kinematic: " + GetPhysicsCounterValue(frameData, "Kinematic Shapes"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Static: " + GetPhysicsCounterValue(frameData, "Static Shapes"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Queries", GUILayout.Width(k_labelWidthTitle));
                            EditorGUILayout.LabelField("| Total: " + GetPhysicsCounterValue(frameData, "Total Queries"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Raycast: " + GetPhysicsCounterValue(frameData, "Raycast Queries"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Shapecast: " + GetPhysicsCounterValue(frameData, "Shapecast Queries"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Overlap: " + GetPhysicsCounterValue(frameData, "Overlap Queries"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| IsTouching: " + GetPhysicsCounterValue(frameData, "IsTouching Queries"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| GetContacts: " + GetPhysicsCounterValue(frameData, "GetContacts Queries"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Particle: " + GetPhysicsCounterValue(frameData, "Particle Queries"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Contacts", GUILayout.Width(k_labelWidthTitle));
                            EditorGUILayout.LabelField("| Total: " + GetPhysicsCounterValue(frameData, "Total Contacts"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Added: " + GetPhysicsCounterValue(frameData, "Added Contacts"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Removed: " + GetPhysicsCounterValue(frameData, "Removed Contacts"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Broadphase Updates: " + GetPhysicsCounterValue(frameData, "Broadphase Updates"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Broadphase Pairs: " + GetPhysicsCounterValue(frameData, "Broadphase Pairs"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Callbacks", GUILayout.Width(k_labelWidthTitle));
                            EditorGUILayout.LabelField("| Total: " + GetPhysicsCounterValue(frameData, "Total Callbacks"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Collision Enter: " + GetPhysicsCounterValue(frameData, "Collision Enter"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Collision Stay: " + GetPhysicsCounterValue(frameData, "Collision Stay"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Collision Exit: " + GetPhysicsCounterValue(frameData, "Collision Exit"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Trigger Enter: " + GetPhysicsCounterValue(frameData, "Trigger Enter"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Trigger Stay: " + GetPhysicsCounterValue(frameData, "Trigger Stay"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Trigger Exit: " + GetPhysicsCounterValue(frameData, "Trigger Exit"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Solver", GUILayout.Width(k_labelWidthTitle));
                            EditorGUILayout.LabelField("| World Count: " + GetPhysicsCounterValue(frameData, "Solver World Count"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Simulation Count: " + GetPhysicsCounterValue(frameData, "Solver Simulation Count"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Discrete Islands: " + GetPhysicsCounterValue(frameData, "Solver Discrete Islands"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Continuous Islands: " + GetPhysicsCounterValue(frameData, "Solver Continuous Islands"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Transform Sync", GUILayout.Width(k_labelWidthTitle));
                            EditorGUILayout.LabelField("| Sync Calls: " + GetPhysicsCounterValue(frameData, "Total Transform Sync Calls"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Sync Bodies: " + GetPhysicsCounterValue(frameData, "Transform Sync Bodies"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Sync Colliders: " + GetPhysicsCounterValue(frameData, "Transform Sync Colliders"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Parent Sync Bodies: " + GetPhysicsCounterValue(frameData, "Transform Parent Sync Bodies"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Parent Sync Colliders: " + GetPhysicsCounterValue(frameData, "Transform Parent Sync Colliders"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Joints", GUILayout.Width(k_labelWidthTitle));
                            EditorGUILayout.LabelField("| Total: " + GetPhysicsCounterValue(frameData, "Total Joints"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Timings", GUILayout.Width(k_labelWidthTitle));
                            EditorGUILayout.LabelField("| Sim: " + GetPhysicsSampleData(frameData, "Physics2D.Simulate"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Sync: " + GetPhysicsSampleData(frameData, "Physics2D.SyncTransformChanges"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Step: " + GetPhysicsSampleData(frameData, "Physics2D.Step"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Write: " + GetPhysicsSampleData(frameData, "Physics2D.UpdateTransforms"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.LabelField("| Callbacks: " + GetPhysicsSampleData(frameData, "Physics2D.CompileContactCallbacks"), GUILayout.Width(k_labelWidthDetail));
                            EditorGUILayout.EndHorizontal();
                        }
                        else
                        {
                            EditorGUILayout.LabelField("Current Data not Available.");
                        }
                    }
                    else
                    {
                        // Determine if the old counters are available by looking for a counter only available there.
                        if (!newCountersAvailable)
                        {
                            // Old data compatibility.
                            var activeText = ProfilerDriver.GetOverviewText(ProfilerArea.Physics2D, ProfilerWindow.GetActiveVisibleFrameIndex());
                            var height = EditorStyles.wordWrappedLabel.CalcHeight(GUIContent.Temp(activeText), position.width);
                            EditorGUILayout.SelectableLabel(activeText, EditorStyles.wordWrappedLabel, GUILayout.MinHeight(height));
                        }
                        else
                        {
                            EditorGUILayout.LabelField("Legacy Data not Available.");
                        }
                    }
                }
            }

            GUILayout.EndScrollView();
        }
    }
}
