// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Unity.Multiplayer.PlayMode.Editor
{
    // View and Layout definitions as required by internal Unity.Windowlayouts.
    // The exception to the rule - property keys prefixed with "mppm_"
    [Serializable]
    internal class DynamicLayout
    {
        [Serializable]
        internal class DynamicView
        {
            [JsonProperty("class_name", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public string ClassName { get; internal set; }

            [JsonProperty("horizontal", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public bool Horizontal { get; internal set; }

            [JsonProperty("vertical", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public bool Vertical { get; internal set; }

            [JsonProperty("tabs", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public bool Tabs { get; internal set; }

            [JsonProperty("size", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public float Size { get; internal set; }

            [JsonProperty("children", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public List<DynamicView> Children { get; internal set; }

            [JsonProperty("mppm_id", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public string Id { get; internal set; }

            [JsonProperty("mppm_panel", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public string Panel { get; internal set; }
        }

        [JsonProperty("restore_saved_layout", Required = Required.Always)]
        public bool RestoreSavedLayout { get; internal set; }

        [JsonProperty("top_view", Required = Required.Always)]
        public DynamicView TopView { get; internal set; }

        [JsonProperty("center_view", Required = Required.Always)]
        public DynamicView CenterView { get; internal set; }

        internal static string Serialize(ParsingSystemDelegates parsing, DynamicLayout layout)
        {
            return parsing.SerializeObjectFunc(layout);
        }

        internal static bool TryDeserialize(ParsingSystemDelegates parsing, string data, out DynamicLayout layout)
        {
            try
            {
                layout = (DynamicLayout)parsing.DeserializeObjectFunc(data, typeof(DynamicLayout));
            }
            catch (JsonException e) when (e is JsonSerializationException or JsonReaderException)
            {
                MppmLog.Warning($"Dynamic layout De-serialization failure: {e.Message}");
                layout = null;
            }

            return layout != null;
        }

        // Trims this dynamic layout and removes the views that were toggled off in the provided
        // layout flags (if they still exist)
        internal bool TrimDynamicLayout(LayoutFlags flags)
        {
            return TrimDisabledPanelsInLayoutFlags(flags, CenterView);
        }

        // Perform DFS, iterate to the nodes of the trees (layout panel controls)  and trim them off
        // if found disabled in the given layout flags
        private bool TrimDisabledPanelsInLayoutFlags(LayoutFlags layoutFlags, DynamicView view)
        {
            // A leaf view is considered a panel - determine if we should remove it.
            if (view.Children == null || view.Children.Count == 0)
            {
                return ShouldRemoveView(view, layoutFlags);
            }

            // Continue iterating through the tree
            var children = view.Children.ToArray();
            foreach (DynamicView child in children)
            {
                if (TrimDisabledPanelsInLayoutFlags(layoutFlags, child))
                {
                    view.Children.Remove(child);
                }
            }

            return view.Children.Count == 0;
        }

        // Determine if a view panel is disabled in the provided layoutFlags
        private bool ShouldRemoveView(DynamicView view, LayoutFlags layoutFlags)
        {
            // If there's no defined panel, it's misconfigured - remove it.
            if (view.Panel == null)
            {
                return true;
            }

            // Attempt to grab the corresponding flag for the given Panel
            // and strip the view out if we don't recognize it.
            LayoutFlags viewFlag = LayoutFlagsUtil.GetFlagForQualifiedName(view.Panel);
            if (viewFlag == LayoutFlags.None)
            {
                MppmLog.Warning($"Parsed unknown Panel in DynamicLayout {layoutFlags}");
                return true;
            }

            // Else populate the ClassName field required by native (Trunk)
            // to inflate the corresponding views.
            string[] words = view.Panel.Split('.');
            view.ClassName = words[^1];

            // Finally return if the layout flags have enabled this view.
            return !layoutFlags.HasFlag(viewFlag);
        }
    }
}
