// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.Presets
{
    class AddPresetTypeWindow : AdvancedDropdownWindow
    {
        Action<PresetType> m_OnSelection;

        const string k_PresetTypeSearch = "PresetTypeSearchString";
        const int k_MaxWindowHeight = 395 - 80;

        static AdvancedDropdownState s_State = new AdvancedDropdownState();

        protected override bool setInitialSelectionPosition { get; } = false;

        internal static bool Show(Rect rect, Action<PresetType> onSelection, string search = null)
        {
            CloseAllOpenWindows<AddPresetTypeWindow>();
            var window = CreateInstance<AddPresetTypeWindow>();
            window.dataSource = new AddPresetTypeDataSource();
            window.gui = new AddPresetTypeGUI(window.dataSource);
            window.state = s_State;
            window.m_OnSelection = onSelection;
            window.Init(rect);
            window.searchString = search ?? EditorPrefs.GetString(k_PresetTypeSearch, "");
            return true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            showHeader = true;
            selectionChanged += OnItemSelected;
        }

        void OnItemSelected(AdvancedDropdownItem item)
        {
            if (m_OnSelection != null)
            {
                var presetItem = item as PresetTypeDropdownItem;
                if (presetItem != null && presetItem.presetType.IsValid())
                {
                    m_OnSelection(presetItem.presetType);
                }
            }
        }

        protected override void OnDisable()
        {
            EditorPrefs.SetString(k_PresetTypeSearch, searchString);
        }

        protected override Vector2 CalculateWindowSize(Rect buttonRect)
        {
            return new Vector2(buttonRect.width, k_MaxWindowHeight);
        }
    }
}
