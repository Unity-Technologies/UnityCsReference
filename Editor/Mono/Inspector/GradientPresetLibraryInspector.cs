// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(GradientPresetLibrary))]
    internal class GradientPresetLibraryEditor : Editor
    {
        private GenericPresetLibraryInspector<GradientPresetLibrary> m_GenericPresetLibraryInspector;

        public void OnEnable()
        {
            m_GenericPresetLibraryInspector = new GenericPresetLibraryInspector<GradientPresetLibrary>(target, "Gradient Preset Library", OnEditButtonClicked);
            m_GenericPresetLibraryInspector.presetSize = new Vector2(72, 16);
            m_GenericPresetLibraryInspector.lineSpacing = 4f;
        }

        public void OnDestroy()
        {
            if (m_GenericPresetLibraryInspector != null)
                m_GenericPresetLibraryInspector.OnDestroy();
        }

        public override void OnInspectorGUI()
        {
            m_GenericPresetLibraryInspector.itemViewMode = PresetLibraryEditorState.GetItemViewMode("Gradient"); // ensure in-sync
            if (m_GenericPresetLibraryInspector != null)
                m_GenericPresetLibraryInspector.OnInspectorGUI();
        }

        private void OnEditButtonClicked(string libraryPath)
        {
            GradientPicker.Show(new Gradient(), true);
            GradientPicker.instance.currentPresetLibrary = libraryPath;
        }
    }
} // namespace
