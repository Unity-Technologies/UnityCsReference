// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(CurvePresetLibrary))]
    internal class CurvePresetLibraryEditor : Editor
    {
        private GenericPresetLibraryInspector<CurvePresetLibrary> m_GenericPresetLibraryInspector;
        private CurveLibraryType m_CurveLibraryType;

        public void OnEnable()
        {
            string filePath = AssetDatabase.GetAssetPath(target.GetInstanceID());
            m_CurveLibraryType = GetCurveLibraryTypeFromExtension(Path.GetExtension(filePath).TrimStart('.'));
            m_GenericPresetLibraryInspector = new GenericPresetLibraryInspector<CurvePresetLibrary>(target, GetHeader(), OnEditButtonClicked);
            m_GenericPresetLibraryInspector.presetSize = new Vector2(72, 20);
            m_GenericPresetLibraryInspector.lineSpacing = 5f;
        }

        public void OnDestroy()
        {
            if (m_GenericPresetLibraryInspector != null)
                m_GenericPresetLibraryInspector.OnDestroy();
        }

        public override void OnInspectorGUI()
        {
            string editorPrefPrefix = CurvePresetsContentsForPopupWindow.GetBasePrefText(m_CurveLibraryType);
            m_GenericPresetLibraryInspector.itemViewMode = PresetLibraryEditorState.GetItemViewMode(editorPrefPrefix); // ensure in-sync
            if (m_GenericPresetLibraryInspector != null)
                m_GenericPresetLibraryInspector.OnInspectorGUI();
        }

        private void OnEditButtonClicked(string libraryPath)
        {
            Rect ranges = GetCurveRanges();
            CurveEditorSettings settings = new CurveEditorSettings();
            if (ranges.width > 0 && ranges.height > 0 && ranges.width != Mathf.Infinity && ranges.height != Mathf.Infinity)
            {
                settings.hRangeMin = ranges.xMin;
                settings.hRangeMax = ranges.xMax;
                settings.vRangeMin = ranges.yMin;
                settings.vRangeMax = ranges.yMax;
            }

            CurveEditorWindow.curve = new AnimationCurve();
            CurveEditorWindow.color = new Color(0, 0.8f, 0f);
            CurveEditorWindow.instance.Show(GUIView.current, settings);

            CurveEditorWindow.instance.currentPresetLibrary = libraryPath;
        }

        private string GetHeader()
        {
            switch (m_CurveLibraryType)
            {
                case CurveLibraryType.NormalizedZeroToOne:
                    return "Curve Preset Library (Normalized 0 - 1)";
                case CurveLibraryType.Unbounded:
                    return "Curve Preset Library";
                default:
                    return "Curve Preset Library ?";
            }
        }

        private Rect GetCurveRanges()
        {
            switch (m_CurveLibraryType)
            {
                case CurveLibraryType.NormalizedZeroToOne:
                    return new Rect(0, 0, 1, 1);
                case CurveLibraryType.Unbounded:
                    return new Rect();
                default:
                    return new Rect();
            }
        }

        CurveLibraryType GetCurveLibraryTypeFromExtension(string extension)
        {
            string curveNormalized = PresetLibraryLocations.GetCurveLibraryExtension(true);
            string curve = PresetLibraryLocations.GetCurveLibraryExtension(false);
            if (extension.Equals(curveNormalized, StringComparison.OrdinalIgnoreCase))
                return CurveLibraryType.NormalizedZeroToOne;

            if (extension.Equals(curve, StringComparison.OrdinalIgnoreCase))
                return CurveLibraryType.Unbounded;

            Debug.LogError("Extension not recognized!");
            return CurveLibraryType.NormalizedZeroToOne;
        }
    }
} // namespace
