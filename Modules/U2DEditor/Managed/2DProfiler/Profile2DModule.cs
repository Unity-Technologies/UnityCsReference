// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEditor.Profiling;
using UnityEditorInternal.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// This namespace contains internal classes for the Unity Editor's Profiler.
/// </summary>
namespace UnityEditor.U2D.Profiling
{

    internal class Profile2DModuleDetailsViewController : ProfilerModuleViewController
    {

        [SerializeField]
        private SpriteAtlasProfilerInfoTreeViewState m_SpriteAtlasProfilerTreeViewState;
        private SpriteProfilerView m_SpriteProfilerView = null;
        private SpriteAtlasProfilerInfoBackend m_SpriteAtlasProfilerBackend = null;
        int[] m_SpriteRendererMarkerIds = new int[U2DProfilerMakers.s_SpriteRendererSampleNames.Length];
        int[] m_SortingGroupMarkerIds = new int[U2DProfilerMakers.s_SortingGroupSampleNames.Length];
        SpriteProfilerView spriteAtlasProfilerInfoView { get { if (null == m_SpriteProfilerView) m_SpriteProfilerView = new SpriteProfilerView(spriteAtlasProfilerTreeviewState); return m_SpriteProfilerView; } }
        SpriteAtlasProfilerInfoTreeViewState spriteAtlasProfilerTreeviewState { get { if (null == m_SpriteAtlasProfilerTreeViewState) m_SpriteAtlasProfilerTreeViewState = new SpriteAtlasProfilerInfoTreeViewState(); return m_SpriteAtlasProfilerTreeViewState; } }
        SpriteAtlasProfilerInfoBackend spriteAtlasProfilerBackend { get { if (null == m_SpriteAtlasProfilerBackend) m_SpriteAtlasProfilerBackend = new SpriteAtlasProfilerInfoBackend(spriteAtlasProfilerTreeviewState); return m_SpriteAtlasProfilerBackend; } }

        public Profile2DModuleDetailsViewController(ProfilerWindow profilerWindow) : base(profilerWindow)
        {

        }

        protected override VisualElement CreateView()
        {
            var view = spriteAtlasProfilerInfoView;
            SubscribeToExternalEvents();
            OnSelectedFrameIndexChanged(ProfilerWindow.selectedFrameIndex);
            return view;
        }

        void OnSelectedFrameIndexChanged(long index)
        {
            if(Event.current != null && Event.current.type == EventType.Layout)
                return;
            if (UnityEngine.Profiling.Profiler.enabled && !spriteAtlasProfilerInfoView.GetSpriteStatisticProfilerView().IsLiveUpdateEnabled())
                return;

            var frameIndex = Convert.ToInt32(index);
            var items = new List<SpriteNode>();
            spriteAtlasProfilerInfoView.Init(null);
            using (var frameData = UnityEditorInternal.ProfilerDriver.GetRawFrameDataView(frameIndex, 0))
            {
                if (frameData == null || !frameData.valid)
                {
                    return;
                }

                // statistics
                var markerId = frameData.GetMarkerId(U2DProfilerMakers.k_SpriteCountMarkerName);
                var spriteCount = frameData.GetCounterValueAsLong(markerId);
                markerId = frameData.GetMarkerId(U2DProfilerMakers.k_SpriteAtlasCountMakerName);
                var spriteAtlasCount = frameData.GetCounterValueAsLong(markerId);
                markerId = frameData.GetMarkerId(U2DProfilerMakers.k_SpritesRenderedMakerName);
                var spritesRendered = frameData.GetCounterValueAsLong(markerId);
                markerId = frameData.GetMarkerId(U2DProfilerMakers.k_SpriteAtlasesRenderedMakerName);
                var spriteAtlasesRendered = frameData.GetCounterValueAsLong(markerId);

                float spriteRendererTime = 0f;
                for(int i = 0; i < U2DProfilerMakers.s_SpriteRendererSampleNames.Length; i++)
                {
                    m_SpriteRendererMarkerIds[i] = frameData.GetMarkerId(U2DProfilerMakers.s_SpriteRendererSampleNames[i]);
                    if (m_SpriteRendererMarkerIds[i] == FrameDataView.invalidMarkerId)
                    {
                        Debug.LogWarning($"{U2DProfilerMakers.s_SpriteRendererSampleNames[i]} marker id is invalid, please make sure the marker name is correct and the marker is properly registered.");
                    }
                }

                float sortingGroupTime = 0f;
                for(int i = 0; i < U2DProfilerMakers.s_SortingGroupSampleNames.Length; i++)
                {
                    m_SortingGroupMarkerIds[i] = frameData.GetMarkerId(U2DProfilerMakers.s_SortingGroupSampleNames[i]);
                    if (m_SortingGroupMarkerIds[i] == FrameDataView.invalidMarkerId)
                    {
                        Debug.LogWarning($"{U2DProfilerMakers.s_SortingGroupSampleNames[i]} marker id is invalid, please make sure the marker name is correct and the marker is properly registered.");
                    }
                }

                int sampleCount = frameData.sampleCount;
                for (int i = 0; i < sampleCount; ++i)
                {
                    var sampleMarkerId = frameData.GetSampleMarkerId(i);
                    float markerTime = frameData.GetSampleTimeMs(i);
                    int j = 0;
                    for (j = 0; j < U2DProfilerMakers.s_SpriteRendererSampleNames.Length; j++)
                    {
                        if (sampleMarkerId == m_SpriteRendererMarkerIds[j])
                        {
                            spriteRendererTime += markerTime;
                            break;
                        }
                    }

                    for (j = 0; j < U2DProfilerMakers.s_SortingGroupSampleNames.Length; j++)
                    {
                        if (sampleMarkerId == m_SortingGroupMarkerIds[j])
                        {
                            sortingGroupTime += markerTime;
                            break;
                        }
                    }
                }

                spriteAtlasProfilerInfoView.GetSpriteStatisticProfilerView().SetStatistic(
                    spriteCount, spriteAtlasCount, spritesRendered, spriteAtlasesRendered, spriteRendererTime, sortingGroupTime);
                var sourceItemsData = Profiler2D.GetSpriteAtlasUsageProfilerInfo(frameData);
                if (0 == sourceItemsData.Length)
                {
                    return;
                }

                var spriteAtlasItems = Profiler2D.GetSpriteAtlasProfilerInfo(frameData);
                var sourceItems = sourceItemsData.ToArray();

                // Map SpriteAtlas Items.
                Dictionary<EntityId, SpriteAtlasProfilerInfo> atlasItems = new Dictionary<EntityId, SpriteAtlasProfilerInfo>();
                foreach (var item in spriteAtlasItems)
                    atlasItems[item.assetEntityId] = item;

                Array.Sort(sourceItems, (x, y) =>
                {
                    int result = x.atlasEntityId.CompareTo(y.atlasEntityId);
                    if (result != 0)
                        return result;
                    result = x.textureEntityId.CompareTo(y.textureEntityId);
                    if (result != 0)
                        return result;
                    return x.spriteEntityId.CompareTo(y.spriteEntityId);
                });

                foreach (var s in sourceItems)
                {
                    var spriteTextureSizeRatio = 0.0f;
                    var spriteAtlasName = "?";
                    var spriteAtlasGuid = "?";
                    if (atlasItems.ContainsKey(s.atlasEntityId))
                    {
                        bool valid = EntityId.ToULong(s.atlasEntityId) != 0;
                        spriteTextureSizeRatio = s.spriteTextureSizeRatio;
                        if (valid)
                        {
                            valid = frameData.GetUnityObjectInfo(s.atlasEntityId, out var spriteAtlasInfo);
                            spriteAtlasName = valid ? spriteAtlasInfo.name : spriteAtlasName;
                            spriteAtlasGuid = valid ? Profiler2D.GetSpriteStringByBlobOffset(frameData, atlasItems[s.atlasEntityId].assetGuidOffset) : "?";
                        }
                    }
                    spriteAtlasName = (spriteAtlasGuid == "?") ? "Sprites not in atlas" : spriteAtlasName;

                    var texValid = frameData.GetUnityObjectInfo(s.textureEntityId, out var textureInfo);
                    var spriteValid = frameData.GetUnityObjectInfo(s.spriteEntityId, out var spriteInfo);
                    var wrapper = new SpriteNode((int)EntityId.ToULong(s.spriteEntityId),
                         spriteAtlasName,
                         spriteAtlasGuid,
                         spriteValid ? spriteInfo.name : "(none)",
                         texValid ? textureInfo.name : "(none)",
                         spriteTextureSizeRatio, s.spriteEntityId, s.atlasEntityId, s.textureEntityId);
                    items.Add(wrapper);
                }
                spriteAtlasProfilerBackend.SetData(items);
                spriteAtlasProfilerInfoView.Init(spriteAtlasProfilerBackend);
            }
        }

        void SubscribeToExternalEvents()
        {
            ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;
        }

        void UnsubscribeFromExternalEvents()
        {
            ProfilerWindow.SelectedFrameIndexChanged -= OnSelectedFrameIndexChanged;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            UnsubscribeFromExternalEvents();
            m_SpriteProfilerView = null;
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// A custom Profiler module for 2D profiling data.
    /// </summary>
    [Serializable]
    [ProfilerModuleMetadata("2D", typeof(ProfilerModule.LocalizationResource), IconPath = "Profiler.2D")]
    internal class Profile2DModule : ProfilerModuleBase
    {

        /// <summary>
        /// Gets the default order index for this module.
        /// </summary>
        private protected override int defaultOrderIndex => 15;

        /// <summary>
        /// Gets the legacy preference key for this module.
        /// </summary>
        private protected override string legacyPreferenceKey => "ProfilerChartU2D";

        /// <summary>
        /// An array of counter names for 2D profiling data.
        /// </summary>
        static readonly (string counterName, ProfilerCategory category)[] k_Counters =
        {
            (U2DProfilerMakers.k_SpriteCountMarkerName, ProfilerCategory.Memory),
            (U2DProfilerMakers.k_SpriteAtlasCountMakerName, ProfilerCategory.Memory),
            (U2DProfilerMakers.k_SpritesRenderedMakerName, ProfilerCategory.U2D),
            (U2DProfilerMakers.k_SpriteAtlasesRenderedMakerName, ProfilerCategory.U2D)
        };

        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            // If 2d Tooling package is installed create the tooling ViewController instead.
            return new Profile2DModuleDetailsViewController(ProfilerWindow);
        }

        /// <summary>
        /// Collects the default chart counters for this module.
        /// </summary>
        /// <returns>A list of ProfilerCounterData objects representing the default chart counters.</returns>
        protected override List<ProfilerCounterData> CollectDefaultChartCounters()
        {
            // Create a new list to store the chart counters
            var chartCounters = new List<ProfilerCounterData>(k_Counters.Length);

            // Iterate over each counter name and create a ProfilerCounterData object for it
            foreach (var counterName in k_Counters)
            {
                chartCounters.Add(new ProfilerCounterData()
                {
                    m_Name = counterName.counterName,
                    m_Category = counterName.category.Name,
                });
            }


            // Return the list of chart counters
            return chartCounters;
        }
    }
}
