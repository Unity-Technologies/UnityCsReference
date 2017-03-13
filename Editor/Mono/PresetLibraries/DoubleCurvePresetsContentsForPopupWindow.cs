// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    class DoubleCurvePresetsContentsForPopupWindow : PopupWindowContent
    {
        PresetLibraryEditor<DoubleCurvePresetLibrary> m_CurveLibraryEditor;
        PresetLibraryEditorState m_CurveLibraryEditorState;
        DoubleCurve m_DoubleCurve;
        bool m_WantsToClose = false;

        public DoubleCurve doubleCurveToSave
        {
            get {return m_DoubleCurve; }
            set {m_DoubleCurve = value; }
        }

        System.Action<DoubleCurve> m_PresetSelectedCallback;

        public DoubleCurvePresetsContentsForPopupWindow(DoubleCurve doubleCurveToSave, System.Action<DoubleCurve> presetSelectedCallback)
        {
            m_DoubleCurve = doubleCurveToSave;
            m_PresetSelectedCallback = presetSelectedCallback;
        }

        public override void OnClose()
        {
            m_CurveLibraryEditorState.TransferEditorPrefsState(false);
        }

        public PresetLibraryEditor<DoubleCurvePresetLibrary> GetPresetLibraryEditor()
        {
            return m_CurveLibraryEditor;
        }

        bool IsSingleCurve(DoubleCurve doubleCurve)
        {
            return doubleCurve.minCurve == null || doubleCurve.minCurve.length == 0;
        }

        string GetEditorPrefBaseName()
        {
            return PresetLibraryLocations.GetParticleCurveLibraryExtension(m_DoubleCurve.IsSingleCurve(), m_DoubleCurve.signedRange);
        }

        public void InitIfNeeded()
        {
            if (m_CurveLibraryEditorState == null)
            {
                m_CurveLibraryEditorState = new PresetLibraryEditorState(GetEditorPrefBaseName());
                m_CurveLibraryEditorState.TransferEditorPrefsState(true);
            }

            if (m_CurveLibraryEditor == null)
            {
                var extension = PresetLibraryLocations.GetParticleCurveLibraryExtension(m_DoubleCurve.IsSingleCurve(), m_DoubleCurve.signedRange);
                var saveLoadHelper = new ScriptableObjectSaveLoadHelper<DoubleCurvePresetLibrary>(extension, SaveType.Text);
                m_CurveLibraryEditor = new PresetLibraryEditor<DoubleCurvePresetLibrary>(saveLoadHelper, m_CurveLibraryEditorState, ItemClickedCallback);
                m_CurveLibraryEditor.addDefaultPresets += AddDefaultPresetsToLibrary;
                m_CurveLibraryEditor.presetsWasReordered = PresetsWasReordered;
                m_CurveLibraryEditor.previewAspect = 4f;
                m_CurveLibraryEditor.minMaxPreviewHeight = new Vector2(24f, 24f);
                m_CurveLibraryEditor.showHeader = true;
            }
        }

        void PresetsWasReordered()
        {
            InspectorWindow.RepaintAllInspectors();
        }

        public override void OnGUI(Rect rect)
        {
            InitIfNeeded();

            m_CurveLibraryEditor.OnGUI(rect, m_DoubleCurve);

            if (m_WantsToClose)
                editorWindow.Close();
        }

        void ItemClickedCallback(int clickCount, object presetObject)
        {
            DoubleCurve doubleCurve = presetObject as DoubleCurve;
            if (doubleCurve == null)
                Debug.LogError("Incorrect object passed " + presetObject);

            m_PresetSelectedCallback(doubleCurve);
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(240, 330);
        }

        void AddDefaultPresetsToLibrary(PresetLibrary presetLibrary)
        {
            DoubleCurvePresetLibrary doubleCurveDefaultLib = presetLibrary as DoubleCurvePresetLibrary;
            if (doubleCurveDefaultLib == null)
            {
                Debug.Log("Incorrect preset library, should be a DoubleCurvePresetLibrary but was a " + presetLibrary.GetType());
                return;
            }

            bool signedRange = m_DoubleCurve.signedRange;
            List<DoubleCurve> defaults = new List<DoubleCurve>();
            if (IsSingleCurve(m_DoubleCurve))
            {
                defaults = GetUnsignedSingleCurveDefaults(signedRange);
            }
            else
            {
                if (signedRange)
                {
                    defaults = GetSignedDoubleCurveDefaults();
                }
                else
                {
                    defaults = GetUnsignedDoubleCurveDefaults();
                }
            }

            foreach (DoubleCurve preset in defaults)
            {
                doubleCurveDefaultLib.Add(preset, "");
            }
        }

        static List<DoubleCurve> GetUnsignedSingleCurveDefaults(bool signedRange)
        {
            List<DoubleCurve> defaults = new List<DoubleCurve>();
            defaults.Add(new DoubleCurve(null, new AnimationCurve(CurveEditorWindow.GetConstantKeys(1f)), signedRange));
            defaults.Add(new DoubleCurve(null, new AnimationCurve(CurveEditorWindow.GetLinearKeys()), signedRange));
            defaults.Add(new DoubleCurve(null, new AnimationCurve(CurveEditorWindow.GetLinearMirrorKeys()), signedRange));
            defaults.Add(new DoubleCurve(null, new AnimationCurve(CurveEditorWindow.GetEaseInKeys()), signedRange));
            defaults.Add(new DoubleCurve(null, new AnimationCurve(CurveEditorWindow.GetEaseInMirrorKeys()), signedRange));
            defaults.Add(new DoubleCurve(null, new AnimationCurve(CurveEditorWindow.GetEaseOutKeys()), signedRange));
            defaults.Add(new DoubleCurve(null, new AnimationCurve(CurveEditorWindow.GetEaseOutMirrorKeys()), signedRange));
            defaults.Add(new DoubleCurve(null, new AnimationCurve(CurveEditorWindow.GetEaseInOutKeys()), signedRange));
            defaults.Add(new DoubleCurve(null, new AnimationCurve(CurveEditorWindow.GetEaseInOutMirrorKeys()), signedRange));
            return defaults;
        }

        static List<DoubleCurve> GetUnsignedDoubleCurveDefaults()
        {
            List<DoubleCurve> defaults = new List<DoubleCurve>();
            defaults.Add(new DoubleCurve(new AnimationCurve(CurveEditorWindow.GetConstantKeys(0f)), new AnimationCurve(CurveEditorWindow.GetConstantKeys(1f)), false));
            return defaults;
        }

        static List<DoubleCurve> GetSignedDoubleCurveDefaults()
        {
            List<DoubleCurve> defaults = new List<DoubleCurve>();
            defaults.Add(new DoubleCurve(new AnimationCurve(CurveEditorWindow.GetConstantKeys(-1f)), new AnimationCurve(CurveEditorWindow.GetConstantKeys(1f)), true));
            return defaults;
        }
    }
}
