// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(ColorPresetLibrary))]
    internal class ColorPresetLibraryEditor : Editor
    {
        private GenericPresetLibraryInspector<ColorPresetLibrary> m_GenericPresetLibraryInspector;

        public void OnEnable()
        {
            m_GenericPresetLibraryInspector = new GenericPresetLibraryInspector<ColorPresetLibrary>(target, "Color Preset Library", OnEditButtonClicked);
            m_GenericPresetLibraryInspector.useOnePixelOverlappedGrid = true;
            m_GenericPresetLibraryInspector.maxShowNumPresets = 2000; // does not use a preview cache so show many
        }

        public void OnDestroy()
        {
            if (m_GenericPresetLibraryInspector != null)
                m_GenericPresetLibraryInspector.OnDestroy();
        }

        public override void OnInspectorGUI()
        {
            m_GenericPresetLibraryInspector.itemViewMode = PresetLibraryEditorState.GetItemViewMode(ColorPicker.presetsEditorPrefID); // ensure in-sync
            if (m_GenericPresetLibraryInspector != null)
                m_GenericPresetLibraryInspector.OnInspectorGUI();
        }

        private void OnEditButtonClicked(string libraryPath)
        {
            ColorPicker.Show(GUIView.current, Color.white);
            ColorPicker.get.currentPresetLibrary = libraryPath;
        }
    }
} // namespace
