// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(DoubleCurvePresetLibrary))]
    internal class DoubleCurvePresetLibraryEditor : Editor
    {
        private GenericPresetLibraryInspector<DoubleCurvePresetLibrary> m_GenericPresetLibraryInspector;

        public void OnEnable()
        {
            m_GenericPresetLibraryInspector = new GenericPresetLibraryInspector<DoubleCurvePresetLibrary>(target, GetHeader(),  null);
            m_GenericPresetLibraryInspector.presetSize = new Vector2(72, 20);
            m_GenericPresetLibraryInspector.lineSpacing = 5f;
        }

        private string GetHeader()
        {
            return "Particle Curve Preset Library";
        }

        public void OnDestroy()
        {
            if (m_GenericPresetLibraryInspector != null)
                m_GenericPresetLibraryInspector.OnDestroy();
        }

        public override void OnInspectorGUI()
        {
            if (m_GenericPresetLibraryInspector != null)
                m_GenericPresetLibraryInspector.OnInspectorGUI();
        }
    }
} // namespace
