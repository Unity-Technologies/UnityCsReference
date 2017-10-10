// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor
{
    [CustomEditor(typeof(ParticleSystem))]
    [CanEditMultipleObjects]
    internal class ParticleSystemInspector : Editor, ParticleEffectUIOwner
    {
        ParticleEffectUI m_ParticleEffectUI;
        GUIContent m_PreviewTitle = new GUIContent("Particle System Curves");
        GUIContent showWindowText = new GUIContent("Open Editor...");
        GUIContent closeWindowText = new GUIContent("Close Editor");
        GUIContent hideWindowText = new GUIContent("Hide Editor");
        static GUIContent m_PlayBackTitle;


        public static GUIContent playBackTitle
        {
            get
            {
                if (m_PlayBackTitle == null)
                    m_PlayBackTitle = new GUIContent("Particle Effect");
                return m_PlayBackTitle;
            }
        }

        private bool selectedInParticleSystemWindow
        {
            get
            {
                GameObject targetGameObject = (target as ParticleSystem).gameObject;
                GameObject currentGameObject;

                if (ParticleSystemEditorUtils.lockedParticleSystem == null)
                    currentGameObject = Selection.activeGameObject;
                else
                    currentGameObject = ParticleSystemEditorUtils.lockedParticleSystem.gameObject;

                return currentGameObject == targetGameObject;
            }
        }

        public Editor customEditor { get { return this; } }

        public void OnEnable()
        {
            // Get notified when hierarchy- or project window has changes so we can detect if particle systems have been dragged in or out.
            EditorApplication.hierarchyWindowChanged += HierarchyOrProjectWindowWasChanged;
            EditorApplication.projectWindowChanged += HierarchyOrProjectWindowWasChanged;
            SceneView.onSceneGUIDelegate += OnSceneViewGUI;
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        public void OnDisable()
        {
            SceneView.onSceneGUIDelegate -= OnSceneViewGUI;
            EditorApplication.projectWindowChanged -= HierarchyOrProjectWindowWasChanged;
            EditorApplication.hierarchyWindowChanged -= HierarchyOrProjectWindowWasChanged;
            Undo.undoRedoPerformed -= UndoRedoPerformed;

            if (m_ParticleEffectUI != null)
                m_ParticleEffectUI.Clear();
        }

        private void HierarchyOrProjectWindowWasChanged()
        {
            // target can be null if child particle system was deleted when ParticleSystemWindow is open
            if (target != null && ShouldShowInspector())
                Init(true);
        }

        void UndoRedoPerformed()
        {
            Init(true);
            if (m_ParticleEffectUI != null)
                m_ParticleEffectUI.UndoRedoPerformed();
        }

        private void Init(bool forceInit)
        {
            IEnumerable<ParticleSystem> systems = from p in targets.OfType<ParticleSystem>() where (p != null) select p;
            if (systems == null || !systems.Any())
            {
                m_ParticleEffectUI = null;
                return;
            }

            if (m_ParticleEffectUI == null)
            {
                m_ParticleEffectUI = new ParticleEffectUI(this);
                m_ParticleEffectUI.InitializeIfNeeded(systems);
            }
            else if (forceInit)
            {
                m_ParticleEffectUI.InitializeIfNeeded(systems);
            }
        }

        void ShowEdiorButtonGUI()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                if (m_ParticleEffectUI == null || !m_ParticleEffectUI.multiEdit)
                {
                    bool alreadySelected = selectedInParticleSystemWindow;
                    GameObject targetGameObject = (target as ParticleSystem).gameObject;

                    GUIContent text = null;
                    ParticleSystemWindow window = ParticleSystemWindow.GetInstance();
                    if (window)
                        window.customEditor = this; // window can be created by LoadWindowLayout, when Editor starts up, so always make sure the custom editor member is set up (case 930005)

                    if (window && window.IsVisible() && alreadySelected)
                    {
                        if (window.GetNumTabs() > 1)
                            text = hideWindowText;
                        else
                            text = closeWindowText;
                    }
                    else
                    {
                        text = showWindowText;
                    }

                    if (GUILayout.Button(text, EditorStyles.miniButton, GUILayout.Width(110)))
                    {
                        if (window && window.IsVisible() && alreadySelected)
                        {
                            // Hide window (close instead if not possible)
                            if (!window.ShowNextTabIfPossible())
                                window.Close();
                        }
                        else
                        {
                            if (!alreadySelected)
                            {
                                ParticleSystemEditorUtils.lockedParticleSystem = null;
                                Selection.activeGameObject = targetGameObject;
                            }

                            if (window)
                            {
                                if (!alreadySelected)
                                    window.Clear();

                                // Show window
                                window.Focus();
                            }
                            else
                            {
                                // Kill inspector gui first to ensure playback time is cached properly
                                Clear();

                                // Create new window
                                ParticleSystemWindow.CreateWindow();
                                window = ParticleSystemWindow.GetInstance();
                                window.customEditor = this;
                                GUIUtility.ExitGUI();
                            }
                        }
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        public override bool UseDefaultMargins() { return false; }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

            ShowEdiorButtonGUI();

            if (ShouldShowInspector())
            {
                if (m_ParticleEffectUI == null)
                    Init(true);

                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical(EditorStyles.inspectorFullWidthMargins);

                m_ParticleEffectUI.OnGUI();

                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
            }
            else
            {
                Clear();
            }

            EditorGUILayout.EndVertical();
        }

        void Clear()
        {
            if (m_ParticleEffectUI != null)
                m_ParticleEffectUI.Clear();
            m_ParticleEffectUI = null;
        }

        private bool ShouldShowInspector()
        {
            // Only show the inspector GUI if we are not showing the ParticleSystemWindow
            ParticleSystemWindow window = ParticleSystemWindow.GetInstance();
            return !window || !window.IsVisible() || !selectedInParticleSystemWindow;
        }

        public void OnSceneViewGUI(SceneView sceneView)
        {
            if (ShouldShowInspector())
            {
                Init(false); // Here because can be called before inspector GUI so to prevent blinking GUI when selecting ps.
                if (m_ParticleEffectUI != null)
                {
                    m_ParticleEffectUI.OnSceneViewGUI();
                }
            }
        }

        public override bool HasPreviewGUI()
        {
            return ShouldShowInspector();
        }

        public override void DrawPreview(Rect previewArea)
        {
            ObjectPreview.DrawPreview(this, previewArea, new Object[] { targets[0] }); // only draw the first preview - all the selected systems share it
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (m_ParticleEffectUI != null)
                m_ParticleEffectUI.GetParticleSystemCurveEditor().OnGUI(r);
        }

        public override GUIContent GetPreviewTitle()
        {
            return m_PreviewTitle;
        }

        public override void OnPreviewSettings()
        {
        }
    }
} // namespace UnityEditor
