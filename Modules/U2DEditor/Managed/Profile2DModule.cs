// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditor.U2D;
using UnityEditor.Profiling;
using UnityEditorInternal.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
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
        private SpriteAtlasProfilerInfoView m_SpriteAtlasProfilerView = null;
        private SpriteAtlasProfilerInfoBackend m_SpriteAtlasProfilerBackend = null;

        SpriteAtlasProfilerInfoView spriteAtlasProfilerInfoView { get { if (null == m_SpriteAtlasProfilerView) m_SpriteAtlasProfilerView = new SpriteAtlasProfilerInfoView(spriteAtlasProfilerTreeviewState); return m_SpriteAtlasProfilerView; } }
        SpriteAtlasProfilerInfoTreeViewState spriteAtlasProfilerTreeviewState { get { if (null == m_SpriteAtlasProfilerTreeViewState) m_SpriteAtlasProfilerTreeViewState = new SpriteAtlasProfilerInfoTreeViewState(); return m_SpriteAtlasProfilerTreeViewState; } }
        SpriteAtlasProfilerInfoBackend spriteAtlasProfilerBackend { get { if (null == m_SpriteAtlasProfilerBackend) m_SpriteAtlasProfilerBackend = new SpriteAtlasProfilerInfoBackend(spriteAtlasProfilerTreeviewState); return m_SpriteAtlasProfilerBackend; } }

        public Profile2DModuleDetailsViewController(ProfilerWindow profilerWindow) : base(profilerWindow)
        {

        }

        protected override VisualElement CreateView()
        {
            var view = spriteAtlasProfilerInfoView.CreateGUI();
            SubscribeToExternalEvents();
            OnSelectedFrameIndexChanged(ProfilerWindow.selectedFrameIndex);
            return view;
        }

        void OnSelectedFrameIndexChanged(long index)
        {
            var frameIndex = Convert.ToInt32(index);
            var items = new List<SpriteAtlasProfilerInfoWrapper>();
            using (var frameData = UnityEditorInternal.ProfilerDriver.GetRawFrameDataView(frameIndex, 0))
            {
                if (frameData == null || !frameData.valid)
                {
                    return;
                }

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
                        bool valid = s.atlasEntityId.GetRawData() != 0;
                        spriteTextureSizeRatio = s.spriteTextureSizeRatio;
                        if (valid)
                        {
                            valid = frameData.GetUnityObjectInfo(s.atlasEntityId, out var spriteAtlasInfo);
                            spriteAtlasName = valid ? spriteAtlasInfo.name : spriteAtlasName;
                            spriteAtlasGuid = valid ? Profiler2D.GetSpriteStringByBlobOffset(frameData, atlasItems[s.atlasEntityId].assetGuidOffset) : "?";
                        }
                    }
                    spriteAtlasName = (spriteAtlasGuid == "?") ? "Textures" : spriteAtlasName;

                    var texValid = frameData.GetUnityObjectInfo(s.textureEntityId, out var textureInfo);
                    var spriteValid = frameData.GetUnityObjectInfo(s.spriteEntityId, out var spriteInfo);
                    var wrapper = new SpriteAtlasProfilerInfoWrapper((int)s.spriteEntityId.GetRawData(),
                         spriteAtlasName,
                         spriteAtlasGuid,
                         spriteValid ? spriteInfo.name : "(none)",
                         texValid ? textureInfo.name : "(none)",
                         spriteTextureSizeRatio);
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
            m_SpriteAtlasProfilerView = null;
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
            ("Sprite Count", ProfilerCategory.Memory),
            ("SpriteAtlas Count", ProfilerCategory.Memory),
            ("Sprites Rendered", ProfilerCategory.U2D),
            ("SpriteAtlases used in Rendering", ProfilerCategory.U2D)
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
