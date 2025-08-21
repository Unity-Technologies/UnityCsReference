// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class LayoutCheckboxListView : VisualElement
    {
        // View layouts and controls for this Visual Element
        private readonly string UXML = $"{UXMLPaths.UIRoot}/WindowLayoutPopout.uxml";
        private readonly Dictionary<LayoutFlags, Toggle> m_Toggles;

        // Maps view names to Layout flags that this CheckboxListView element expects.
        // (This should be defined in the UXML that this view control represents)
        private static String GetViewNameForFlag(LayoutFlags flag) =>
            flag switch
            {
                LayoutFlags.InspectorWindow => "Inspector",
                LayoutFlags.GameView => "Game",
                LayoutFlags.SceneHierarchyWindow => "Hierarchy",
                LayoutFlags.ConsoleWindow => "Console",
                LayoutFlags.SceneView => "Scene",
                LayoutFlags.EntitiesPlayModeToolsWindow => "PlaymodeTools",
                LayoutFlags.EntitiesHierarchyWindow => "EntitiesHierarchy",
                LayoutFlags.ProfilerWindow => "Profiler",
                _ => throw new NotSupportedException($"Unsupported Layout Flags View: {flag}")
            };

        // Applied Button Callback for returning selected Layouts
        internal event Action<LayoutFlags> OnLayoutFlagsButtonApplied;

        internal LayoutCheckboxListView()
        {
            m_Toggles = new Dictionary<LayoutFlags, Toggle>();

            // Initialize our layout views
            Clear();
            RemoveFromHierarchy();
            (EditorGUIUtility.LoadRequired(UXML) as VisualTreeAsset).CloneTree(this);

            // Bind all toggles per layout flag to this view
            var values = Enum.GetValues(typeof(LayoutFlags));
            foreach (LayoutFlags flag in values)
            {
                // Ignore the None layout flag
                if (flag == LayoutFlags.None)
                {
                    continue;
                }

                // Grab the corresponding view for this flag and bind
                string viewName = GetViewNameForFlag(flag);
                Toggle toggle = this.Q<Toggle>(viewName);
                toggle.RegisterCallback<ClickEvent>(OnToggleButtonClicked);
                m_Toggles.Add(flag, toggle);
            }

            // Bind click events to the apply button
            var applyButton = this.Q<Button>("Apply");
            applyButton.RegisterCallback<ClickEvent>(OnApplyButtonClicked);
        }

        internal Vector2 GetLayoutSize()
        {
            // Return the size of this layout control.
            var size = layout.size;
            if (!float.IsNaN(size.x) && !float.IsNaN(size.y))
            {
                return size;
            }

            // Generate a default size if layout hasn't yet been calculated.
            Vector2 toggleDimensions = new Vector2(128, 20);
            var height = (m_Toggles.Count + 1) * toggleDimensions.y;
            var width = toggleDimensions.x;
            return new Vector2(width, height);
        }

        // Update our layout checkbox toggles with the given current flags.
        internal void RefreshLayout(LayoutFlags currLayoutFlags)
        {
            foreach (KeyValuePair<LayoutFlags, Toggle> togglePair in m_Toggles)
            {
                // Iterate through each layout and their corresponding toggles
                Toggle currToggle = togglePair.Value;
                LayoutFlags toggleFlag = togglePair.Key;

                // If the project does not support this Layout Flag disable and hide it.
                // For example: Entities
                if (!LayoutFlagsUtil.IsLayoutSupported(toggleFlag))
                {
                    currToggle.SetEnabled(false);
                    currToggle.value = false;
                    currToggle.visible = false;
                    currToggle.style.display = DisplayStyle.None;
                    continue;
                }

                // Else, if in Edit mode, certain Panels are forced disabled, address those.
                if (!EditorApplication.isPlaying && LayoutFlagsUtil.ShouldDisableDuringEditMode(toggleFlag))
                {
                    currToggle.SetEnabled(false);

                    // The Visual Intention here is to also uncheck disabled options
                    // (even if they were previously checked)
                    currToggle.value = false;

                    // Display disabled tooltip for Users
                    currToggle.tooltip = "Currently disabled in Editor Mode, enter Play Mode to enable";
                    continue;
                }

                // Else, for the rest of the flags, show as per normal
                currToggle.SetEnabled(true);
                currToggle.value = currLayoutFlags.HasFlag(toggleFlag);
                currToggle.tooltip = "";
            }
        }

        private void OnApplyButtonClicked(ClickEvent e)
        {
            // Create the next layout flags combo based on toggled checkboxes
            LayoutFlags nextFlags = LayoutFlags.None;
            foreach (KeyValuePair<LayoutFlags, Toggle> togglePair in m_Toggles)
            {
                LayoutFlags flag = togglePair.Key;
                bool isChecked = togglePair.Value.value;
                LayoutFlagsUtil.SetFlag(ref nextFlags, flag, isChecked);
            }

            // Notify callbacks on newly selected flags
            if (OnLayoutFlagsButtonApplied != null)
            {
                OnLayoutFlagsButtonApplied(nextFlags);
            }
        }

        private void OnToggleButtonClicked(ClickEvent e)
        {
            // Determines if there is at least one selected toggle button
            bool hasSelected = false;
            foreach (KeyValuePair<LayoutFlags, Toggle> togglePair in m_Toggles)
            {
                if (togglePair.Value.value)
                {
                    hasSelected = true;
                }
            }

            // If there's at least one toggle there's nothing to do - return.
            if (hasSelected)
            {
                return;
            }

            // If none are selected, prevent the last toggle from being deselected
            var toggle = (Toggle)e.target;
            toggle.SetValueWithoutNotify(true);
        }
    }
}
