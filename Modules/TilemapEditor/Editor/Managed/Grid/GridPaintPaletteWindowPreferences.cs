// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class GridPaintPaletteWindowPreferences
    {
        // Used by SettingsProvider to pick up Search Keywords in preferences filter
        public static readonly GUIContent targetEditModeDialogLabel = GridPaintPaletteWindow.TilePaletteProperties.targetEditModeDialogLabel;
        public static readonly GUIContent targetSortingModeLabel = GridPaintingState.GridPaintActiveTargetsPreferences.targetSortingModeLabel;
        public static readonly GUIContent keepEditModeActiveTargetLabel =
            GridPaintingState.GridPaintActiveTargetsPreferences.targetRestoreEditModeSelectionLabel;

        [SettingsProvider]
        internal static SettingsProvider CreateSettingsProvider()
        {
            var settingsProvider = new SettingsProvider("Preferences/2D/Tile Palette", SettingsScope.User, SettingsProvider.GetSearchKeywordsFromGUIContentProperties<GridPaintPaletteWindowPreferences>())
            {
                guiHandler = searchContext =>
                {
                    GridPaintPaletteWindow.PreferencesGUI();
                    GridPaintingState.GridPaintActiveTargetsPreferences.PreferencesGUI();
                }
            };
            return settingsProvider;
        }
    }
}
