// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Profile.Internal
{
    /// <summary>
    /// Internal implementation of <see cref="AdvancedDropdownWindow"/> for the
    /// Build profile window. Expects an <see cref="IAddSettingsDataProvider"/> when
    /// generating the set of available settings that can be added to the current
    /// build profile.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
    internal class AddSettingsDropdownWindow : AdvancedDropdownWindow
    {
        const string k_ComponentSearchString = "BuildProfileComponentSearchString";

        static AdvancedDropdownState s_State = new AdvancedDropdownState();

        Action<int> m_OnSelection;

        protected override bool setInitialSelectionPosition { get; } = false;

        /// <summary>
        /// Display the AddSettings dropdown window.
        /// </summary>
        /// <param name="onSelection">Selection callback receiving selected item key.</param>
        /// <param name="dataProvider">Creates list describing </param>
        /// <returns></returns>
        public static bool Show(
            Rect rect,
            Action<int> onSelection,
            IAddSettingsDataProvider dataProvider)
        {
            CloseAllOpenWindows<AddSettingsDropdownWindow>();
            var window = CreateInstance<AddSettingsDropdownWindow>();
            window.dataSource = new AddSettingsDropdownDataSource(dataProvider);
            window.gui = new AdvancedDropdownGUI(window.dataSource);
            window.state = s_State;
            window.m_OnSelection = onSelection;
            window.Init(rect);
            return window;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            showHeader = true;
            selectionChanged += OnItemSelected;
        }

        void OnItemSelected(AdvancedDropdownItem item)
        {
            var settingsItem = item as AddSettingsDropdownDataSource.AddSettingsDropdownItem;
            if (item == null)
                return;

            m_OnSelection?.Invoke(settingsItem.Key);
        }
    }
}

