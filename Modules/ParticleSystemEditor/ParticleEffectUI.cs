// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor.Overlays;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Rendering;

// The ParticleEffectUI displays one or more ParticleSystemUIs.

namespace UnityEditor
{
    internal interface ParticleEffectUIOwner
    {
        void Repaint();

        Editor customEditor { get; }
    }

    internal class ParticleEffectUI
    {
        public ParticleEffectUIOwner m_Owner;               // Can be InspectorWindow or ParticleSystemWindow
        static ParticleEffectUI s_EffectUi;
        public ParticleSystemUI[] m_Emitters;               // Contains UI for all ParticleSystem children of the root ParticleSystem for this effect
        bool m_EmittersActiveInHierarchy;
        public bool m_SubEmitterSelected;
        ParticleSystemCurveEditor m_ParticleSystemCurveEditor; // The curve editor used by ParticleSystem modules
        List<ParticleSystem> m_SelectedParticleSystems;     // This is the array of selected particle systems and used to find the root ParticleSystem and for the inspector
        TimeHelper m_TimeHelper = new TimeHelper();
        public static ParticleSystem m_MainPlaybackSystem;
        public static bool m_ShowBounds = false;
        public static bool m_ShowOnlySelected = false;
        public static bool m_VerticalLayout;
        const string k_SimulationStateId = "SimulationState";
        enum PlayState { Stopped = 0, Playing = 1, Paused = 2 }

        // ParticleSystemWindow Layout
        static readonly Vector2 k_MinEmitterAreaSize = new Vector2(125f, 100);
        static readonly Vector2 k_MinCurveAreaSize = new Vector2(100, 100);
        float m_EmitterAreaWidth = 230;                                 // Only used in ParticleSystemWindow for horizontal layout
        float m_CurveEditorAreaHeight = 330;                            // Only used in ParticleSystemWindow for vertical layout
        Vector2 m_EmitterAreaScrollPos = Vector2.zero;
        static readonly Color k_DarkSkinDisabledColor = new Color(0.66f, 0.66f, 0.66f, 0.95f);
        static readonly Color k_LightSkinDisabledColor = new Color(0.84f, 0.84f, 0.84f, 0.95f);

        private enum OwnerType { Inspector, ParticleSystemWindow }

        internal class Texts
        {
            public GUIContent previewSpeed = EditorGUIUtility.TrTextContent("Playback Speed", "Playback Speed is also affected by the Time Scale setting in the Time Manager.");
            public GUIContent previewSpeedDisabled = EditorGUIUtility.TrTextContent("Playback Speed", "Playback Speed is locked to 0.0, because the Time Scale in the Time Manager is set to 0.0.");
            public GUIContent previewTime = EditorGUIUtility.TrTextContent("Playback Time", "Playback Time since the Particle System has started");
            public GUIContent particleCount = EditorGUIUtility.TrTextContent("Particles", "Particles count");
            public GUIContent subEmitterParticleCount = EditorGUIUtility.TrTextContent("Sub Emitter Particles");
            public GUIContent particleSpeeds = EditorGUIUtility.TrTextContent("Speed Range", "Start speed minimum and maximum values");
            public GUIContent play = EditorGUIUtility.TrTextContent("Play", "Play Particle System");
            public GUIContent playDisabled = EditorGUIUtility.TrTextContent("Play", "Play is disabled, because the Time Scale in the Time Manager is set to 0.0.");
            public GUIContent stop = EditorGUIUtility.TrTextContent("Stop", "Stop Particle System");
            public GUIContent pause = EditorGUIUtility.TrTextContent("Pause", "Pause Particle System");
            public GUIContent restart = EditorGUIUtility.TrTextContent("Restart", "Restart Particle System");
            public GUIContent addParticleSystem = EditorGUIUtility.TrTextContent("", "Create Particle System");
            public GUIContent showBounds = EditorGUIUtility.TrTextContent("Show Bounds", "Show world space bounding boxes.");
            public GUIContent showOnlySelected = EditorGUIUtility.TrTextContent("Show Only Selected", "Hide all unselected Particle Systems in the current Effect.");
            public GUIContent resimulation = EditorGUIUtility.TrTextContent("Resimulate", "If resimulate is enabled, the Particle System will show changes made to the system immediately (including changes made to the Particle System Transform).");
            public GUIContent previewLayers = EditorGUIUtility.TrTextContent("Simulate Layers", "Automatically preview all looping Particle Systems on the chosen layers, in addition to the selected Game Objects.");
            public string secondsFloatFieldFormatString = "f2";
            public string speedFloatFieldFormatString = "f1";
        }
        private static Texts s_Texts;
        internal static Texts texts
        {
            get
            {
                if (s_Texts == null)
                    s_Texts = new Texts();
                return s_Texts;
            }
        }

        static Event CreateCommandEvent(string commandName)
        {
            return new Event { type = EventType.ExecuteCommand, commandName = "ParticleSystem/" + commandName };
        }

        static Event s_PlayEvent = CreateCommandEvent("Play");
        static Event s_StopEvent = CreateCommandEvent("Stop");
        static Event s_RestartEvent = CreateCommandEvent("Restart");
        static Event s_ResimulationEvent = CreateCommandEvent("Resimulation");
        static Event s_ShowBoundsEvent = CreateCommandEvent("ShowBounds");
        static Event s_ShowOnlySelectedEvent = CreateCommandEvent("ShowOnlySelected");
        static Event s_ForwardBeginEvent = CreateCommandEvent("ForwardBegin");
        static Event s_ForwardEndEvent = CreateCommandEvent("ForwardEnd");
        static Event s_ReverseBeginEvent = CreateCommandEvent("ReverseBegin");
        static Event s_ReverseEndEvent = CreateCommandEvent("ReverseEnd");

        static void DispatchShortcutEvent(Event evt)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                if (sceneView.SendEvent(evt))
                    return;
            }

            var inspectors = Resources.FindObjectsOfTypeAll<ParticleSystemInspector>();
            foreach (var inspector in inspectors)
            {
                if (inspector != null)
                {
                    if (inspector.HandleShortcutEvent(evt))
                        return;
                }
            }

            var windows = Resources.FindObjectsOfTypeAll<ParticleSystemWindow>();
            foreach (var window in windows)
            {
                if (window != null)
                {
                    if (window.HandleShortcutEvent(evt))
                        return;
                }
            }
        }

        [FormerlyPrefKeyAs("ParticleSystem/Play", ",")]
        [Shortcut("ParticleSystem/Play", typeof(ParticleSystemInspector.ShortcutContext), KeyCode.Comma)]
        static void PlayPauseShortcut(ShortcutArguments args)
        {
            DispatchShortcutEvent(s_PlayEvent);
        }

        [FormerlyPrefKeyAs("ParticleSystem/Stop", ".")]
        [Shortcut("ParticleSystem/Stop", typeof(ParticleSystemInspector.ShortcutContext), KeyCode.Period)]
        static void StopShortcut(ShortcutArguments args)
        {
            DispatchShortcutEvent(s_StopEvent);
        }

        [Shortcut("ParticleSystem/Restart", typeof(ParticleSystemInspector.ShortcutContext), KeyCode.Slash)]
        static void RestartShortcut(ShortcutArguments args)
        {
            DispatchShortcutEvent(s_RestartEvent);
        }

        [Shortcut("ParticleSystem/Resimulation", typeof(ParticleSystemInspector.ShortcutContext))]
        static void ResimulationShortcut(ShortcutArguments args)
        {
            DispatchShortcutEvent(s_ResimulationEvent);
        }

        [Shortcut("ParticleSystem/ShowBounds", typeof(ParticleSystemInspector.ShortcutContext))]
        static void ShowBoundsShortcut(ShortcutArguments args)
        {
            DispatchShortcutEvent(s_ShowBoundsEvent);
        }

        [Shortcut("ParticleSystem/ShowOnlySelected", typeof(ParticleSystemInspector.ShortcutContext))]
        static void ShowOnlySelectedShortcut(ShortcutArguments args)
        {
            DispatchShortcutEvent(s_ShowOnlySelectedEvent);
        }

        [FormerlyPrefKeyAs("ParticleSystem/Forward", "m")]
        [ClutchShortcut("ParticleSystem/Forward", typeof(ParticleSystemInspector.ShortcutContext), KeyCode.M)]
        static void ForwardShortcut(ShortcutArguments args)
        {
            DispatchShortcutEvent(args.stage == ShortcutStage.Begin ? s_ForwardBeginEvent : s_ForwardEndEvent);
        }

        [FormerlyPrefKeyAs("ParticleSystem/Reverse", "n")]
        [ClutchShortcut("ParticleSystem/Reverse", typeof(ParticleSystemInspector.ShortcutContext), KeyCode.N)]
        static void ReverseShortcut(ShortcutArguments args)
        {
            DispatchShortcutEvent(args.stage == ShortcutStage.Begin ? s_ReverseBeginEvent : s_ReverseEndEvent);
        }

        public ParticleEffectUI(ParticleEffectUIOwner owner)
        {
            m_Owner = owner;
            System.Diagnostics.Debug.Assert(m_Owner is ParticleSystemInspector || m_Owner is ParticleSystemWindow);
        }

        public bool multiEdit { get { return (m_SelectedParticleSystems != null) && (m_SelectedParticleSystems.Count > 1); } }

        private bool ShouldManagePlaybackState(ParticleSystem root)
        {
            bool active = false;
            if (root != null)
                active = root.gameObject.activeInHierarchy;
            return active && !Application.IsPlaying(root.gameObject);
        }

        static Color GetDisabledColor()
        {
            return (!EditorGUIUtility.isProSkin) ? k_LightSkinDisabledColor : k_DarkSkinDisabledColor;
        }

        // Returns a list with 'root' and all its direct children.
        static internal ParticleSystem[] GetParticleSystems(ParticleSystem root)
        {
            List<ParticleSystem> particleSystems = new List<ParticleSystem>();
            particleSystems.Add(root);
            GetDirectParticleSystemChildrenRecursive(root.transform, particleSystems);
            return particleSystems.ToArray();
        }

        // Adds only active Particle Systems
        static private void GetDirectParticleSystemChildrenRecursive(Transform transform, List<ParticleSystem> particleSystems)
        {
            foreach (Transform childTransform in transform)
            {
                ParticleSystem ps = childTransform.gameObject.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    // Note: we do not check for if the gameobject is active (we want inactive particle systems as well due prefabs)
                    particleSystems.Add(ps);
                    GetDirectParticleSystemChildrenRecursive(childTransform, particleSystems);
                }
            }
        }

        // Should be called often to ensure we catch if selected Particle System is dragged in/out of root hierarchy
        public bool InitializeIfNeeded(IEnumerable<ParticleSystem> systems)
        {
            bool anyAdded = false;

            ParticleSystem[] allSystems = systems.ToArray();
            bool usingMultiEdit = (allSystems.Length > 1);

            bool initializeRequired = false;
            ParticleSystem mainSystem = null;

            foreach (ParticleSystem shuriken in allSystems)
            {
                ParticleSystem[] shurikens;
                if (!usingMultiEdit)
                {
                    ParticleSystem root = ParticleSystemEditorUtils.GetRoot(shuriken);
                    if (root == null)
                        continue;

                    shurikens = GetParticleSystems(root);
                    mainSystem = root;

                    // Check if we need to re-initialize?
                    if (m_SelectedParticleSystems != null && m_SelectedParticleSystems.Count > 0)
                    {
                        if (root == ParticleSystemEditorUtils.GetRoot(m_SelectedParticleSystems[0]))
                        {
                            if (m_ParticleSystemCurveEditor != null && m_Emitters != null && shurikens.Length == m_Emitters.Length && shuriken.gameObject.activeInHierarchy == m_EmittersActiveInHierarchy)
                            {
                                m_SelectedParticleSystems = new List<ParticleSystem>();
                                m_SelectedParticleSystems.Add(shuriken);

                                if (m_ShowOnlySelected)
                                    SetShowOnlySelectedMode(m_ShowOnlySelected); // always refresh

                                continue;
                            }
                        }
                    }
                }
                else
                {
                    // in multi-edit mode, we explicitly choose the systems to edit, so don't automatically add child systems or search for the root
                    shurikens = new ParticleSystem[] { shuriken };
                    mainSystem = shuriken;
                }

                // Cleanup before initializing
                if (m_ParticleSystemCurveEditor != null)
                    Clear();

                // Now initialize
                initializeRequired = true;
                if (!anyAdded)
                {
                    m_SelectedParticleSystems = new List<ParticleSystem>();
                    anyAdded = true;
                }
                m_SelectedParticleSystems.Add(shuriken);

                // Single edit emitter setup
                if (!usingMultiEdit)
                {
                    // Init CurveEditor before modules (they may add curves during construction)
                    m_ParticleSystemCurveEditor = new ParticleSystemCurveEditor();
                    m_ParticleSystemCurveEditor.Init();

                    int numEmitters = shurikens.Length;
                    if (numEmitters > 0)
                    {
                        m_Emitters = new ParticleSystemUI[numEmitters];

                        for (int i = 0; i < numEmitters; ++i)
                        {
                            m_Emitters[i] = new ParticleSystemUI();
                            m_Emitters[i].Init(this, new ParticleSystem[] { shurikens[i] });
                        }

                        m_EmittersActiveInHierarchy = shuriken.gameObject.activeInHierarchy;
                    }
                }
            }

            if (initializeRequired)
            {
                // Multi-edit emitter setup
                if (usingMultiEdit)
                {
                    // Init CurveEditor before modules (they may add curves during construction)
                    m_ParticleSystemCurveEditor = new ParticleSystemCurveEditor();
                    m_ParticleSystemCurveEditor.Init();

                    int numEmitters = m_SelectedParticleSystems.Count;
                    if (numEmitters > 0)
                    {
                        m_Emitters = new ParticleSystemUI[1];
                        m_Emitters[0] = new ParticleSystemUI();
                        m_Emitters[0].Init(this, m_SelectedParticleSystems.ToArray());
                        m_EmittersActiveInHierarchy = m_SelectedParticleSystems[0].gameObject.activeInHierarchy;
                    }
                }

                // Allow modules to validate their state (the user can have moved emitters around in the hierarchy)
                foreach (ParticleSystemUI e in m_Emitters)
                    foreach (ModuleUI m in e.m_Modules)
                        if (m != null)
                            m.Validate();

                // Sync to state
                if (GetAllModulesVisible())
                    SetAllModulesVisible(true);

                m_EmitterAreaWidth = EditorPrefs.GetFloat("ParticleSystemEmitterAreaWidth", k_MinEmitterAreaSize.x);
                m_CurveEditorAreaHeight = EditorPrefs.GetFloat("ParticleSystemCurveEditorAreaHeight", k_MinCurveAreaSize.y);

                SetShowOnlySelectedMode(m_ShowOnlySelected);

                m_EmitterAreaScrollPos.x = SessionState.GetFloat("CurrentEmitterAreaScroll", 0.0f);

                if (ShouldManagePlaybackState(mainSystem))
                {
                    TryRestorePlayState(mainSystem);

                    // Play when selecting a new particle effect
                    if (m_MainPlaybackSystem != mainSystem)
                        Play();
                }
            }

            m_MainPlaybackSystem = mainSystem;

            return initializeRequired;
        }

        void SavePlayState(ParticleSystem particleSystem)
        {
            if (particleSystem == null)
                return;

            // Store simulation state of current effect as Vector3 (rootInstanceID, PlayState, playBackTime) in Session cache
            int rootInstanceId = particleSystem.GetInstanceID();
            Vector3 state = new Vector3(rootInstanceId, (int)GetCurrentPlayState(), ParticleSystemEditorUtils.playbackTime);
            SessionState.SetVector3(k_SimulationStateId + rootInstanceId, state);
        }

        void TryRestorePlayState(ParticleSystem particleSystem)
        {
            if (particleSystem == null)
                return;

            Vector3 simulationState = SessionState.GetVector3(k_SimulationStateId + particleSystem.GetInstanceID(), Vector3.zero);
            if (particleSystem.GetInstanceID() == (int)simulationState.x)
            {
                float lastPlayBackTime = simulationState.z;
                if (lastPlayBackTime > 0f)
                {
                    if (m_MainPlaybackSystem != particleSystem)
                        ParticleSystemEditorUtils.PerformCompleteResimulation();
                    ParticleSystemEditorUtils.playbackTime = lastPlayBackTime;
                }

                PlayState playState = (PlayState)simulationState.y;
                switch (playState)
                {
                    case PlayState.Stopped:
                        Stop();
                        break;
                    case PlayState.Playing:
                        Play();
                        break;
                    case PlayState.Paused:
                        Pause();
                        break;
                }
            }
        }

        PlayState GetCurrentPlayState()
        {
            PlayState playState;
            if (IsPlaying())
                playState = PlayState.Playing;
            else if (IsPaused())
                playState = PlayState.Paused;
            else
                playState = PlayState.Stopped;
            return playState;
        }

        internal void UndoRedoPerformed(in UndoRedoInfo info)
        {
            Refresh();
            foreach (ParticleSystemUI e in m_Emitters)
            {
                foreach (ModuleUI moduleUI in e.m_Modules)
                {
                    if (moduleUI != null)
                    {
                        moduleUI.CheckVisibilityState();

                        if (moduleUI.foldout)
                            moduleUI.UndoRedoPerformed(info);
                    }
                }
            }

            // Undo will deactivate the ParticleSystem and therefore stop it, here we resume the playback state for the user (UUM-28514)
            RestorePlayBackStateForCurrentSelectedParticleSystem();

            m_Owner.Repaint();
        }

        internal void PrefabInstanceUpdated(GameObject instance)
        {
            // Any update to prefab instances will have stopped the play back here we resume the playback state for the user (UUM-28514)
            RestorePlayBackStateForCurrentSelectedParticleSystem();
        }

        internal void PrefabInstanceReverted(GameObject instance)
        {
            // Any update to prefab instances will have stopped the play back here we resume the playback state for the user (UUM-28514)
            RestorePlayBackStateForCurrentSelectedParticleSystem();
        }

        void RestorePlayBackStateForCurrentSelectedParticleSystem()
        {
            if (m_SelectedParticleSystems == null || m_SelectedParticleSystems.Count == 0)
                return;

            ParticleSystem root = ParticleSystemEditorUtils.GetRoot(m_SelectedParticleSystems[0]);
            if (ShouldManagePlaybackState(root))
            {
                TryRestorePlayState(root);
            }
        }

        public void Clear()
        {
            ParticleSystem root = ParticleSystemEditorUtils.GetRoot(m_SelectedParticleSystems[0]); // root can have been deleted
            if (ShouldManagePlaybackState(root))
            {
                SavePlayState(root);

                // Stop the ParticleSystem here (prevents it being frozen on screen)
                //Stop();
            }

            m_ParticleSystemCurveEditor.OnDisable();
            Tools.s_Hidden = false; // The collisionmodule might have hidden the tools

            if (s_EffectUi == this)
                s_EffectUi = null;

            SetShowOnlySelectedMode(false);


            PlayModeView.RepaintAll();
            SceneView.RepaintAll();
        }

        public void ClearSelectedSystems()
        {
            // We cant clear the selected systems inside of Clear as this method is also used during InitializeIfNeeded when
            // the list is created and calling Clear each time would corrupt the list when multi editing. (case 1254599)
            m_SelectedParticleSystems?.Clear();
        }

        static public Vector2 GetMinSize()
        {
            return k_MinEmitterAreaSize + k_MinCurveAreaSize;
        }

        public void Refresh()
        {
            UpdateProperties();
            m_ParticleSystemCurveEditor.Refresh();
        }

        public string GetNextParticleSystemName()
        {
            string nextName = "";
            for (int i = 2; i < 50; ++i)
            {
                nextName = L10n.Tr("Particle System ") + i;
                bool found = false;
                foreach (ParticleSystemUI e in m_Emitters)
                {
                    if (e.m_ParticleSystems.FirstOrDefault(o => o.name == nextName) != null)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    return nextName;
            }
            return L10n.Tr("Particle System");
        }

        public bool IsParticleSystemUIVisible(ParticleSystemUI psUI)
        {
            OwnerType ownerType = m_Owner is ParticleSystemInspector ? OwnerType.Inspector : OwnerType.ParticleSystemWindow;
            if (ownerType == OwnerType.ParticleSystemWindow)
                return true;

            // ownerType == OwnerType.Inspector
            foreach (ParticleSystem ps in psUI.m_ParticleSystems)
            {
                if (m_SelectedParticleSystems.FirstOrDefault(o => o == ps) != null)
                    return true;
            }

            return false;
        }

        public void PlayOnAwakeChanged(bool newPlayOnAwake)
        {
            foreach (ParticleSystemUI psUI in m_Emitters)
            {
                InitialModuleUI initialModule = psUI.m_Modules[0] as InitialModuleUI;
                System.Diagnostics.Debug.Assert(initialModule != null);
                initialModule.m_PlayOnAwake.boolValue = newPlayOnAwake;
                psUI.ApplyProperties();
            }
        }

        public GameObject CreateParticleSystem(ParticleSystem parentOfNewParticleSystem, SubModuleUI.SubEmitterType defaultType)
        {
            string name = GetNextParticleSystemName();
            GameObject go = new GameObject(name, typeof(ParticleSystem));
            if (go)
            {
                if (parentOfNewParticleSystem)
                    go.transform.parent = parentOfNewParticleSystem.transform;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;

                // Setup particle system based on type
                ParticleSystem ps = go.GetComponent<ParticleSystem>();
                if (defaultType != SubModuleUI.SubEmitterType.None)
                    ps.SetupDefaultType((ParticleSystemSubEmitterType)defaultType);

                SessionState.SetFloat("CurrentEmitterAreaScroll", m_EmitterAreaScrollPos.x);

                // Assign default material
                ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
                Material particleMat = GraphicsSettings.GetDefaultMaterial(DefaultMaterialType.Particle);

                if (particleMat == null)
                    particleMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
                renderer.material = particleMat;

                Undo.RegisterCreatedObjectUndo(go, "Create ParticleSystem");
                return go;
            }
            return null;
        }

        public ParticleSystemCurveEditor GetParticleSystemCurveEditor()
        {
            return m_ParticleSystemCurveEditor;
        }

        public void OnSceneViewGUI()
        {
            ParticleSystem root = ParticleSystemEditorUtils.GetRoot(m_SelectedParticleSystems[0]);
            if (root && root.gameObject.activeInHierarchy)
                s_EffectUi = this;
            else
                s_EffectUi = null;

            foreach (ParticleSystemUI e in m_Emitters)
                e.OnSceneViewGUI();
        }

        int m_IsDraggingTimeHotControlID = -1;

        internal void PlayBackInfoGUI(bool isPlayMode)
        {
            EventType oldEventType = Event.current.type;
            int oldHotControl = GUIUtility.hotControl;
            string oldFormat = EditorGUI.kFloatFieldFormatString;

            EditorGUIUtility.labelWidth = 110.0f;

            if (!isPlayMode)
            {
                EditorGUI.kFloatFieldFormatString = s_Texts.secondsFloatFieldFormatString;
                if (Time.timeScale == 0.0f)
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.FloatField(s_Texts.previewSpeedDisabled, 0.0f);
                    }
                }
                else
                {
                    ParticleSystemEditorUtils.simulationSpeed = Mathf.Clamp(EditorGUILayout.FloatField(s_Texts.previewSpeed, ParticleSystemEditorUtils.simulationSpeed), 0f, 10f);
                }
                EditorGUI.kFloatFieldFormatString = oldFormat;

                EditorGUI.BeginChangeCheck();
                EditorGUI.kFloatFieldFormatString = s_Texts.secondsFloatFieldFormatString;
                float playbackTime = EditorGUILayout.FloatField(s_Texts.previewTime, ParticleSystemEditorUtils.playbackTime);
                EditorGUI.kFloatFieldFormatString = oldFormat;
                if (EditorGUI.EndChangeCheck())
                {
                    if (oldEventType == EventType.MouseDrag)
                    {
                        ParticleSystemEditorUtils.playbackIsScrubbing = true;
                        float previewSpeed = ParticleSystemEditorUtils.simulationSpeed;
                        float oldplaybackTime = ParticleSystemEditorUtils.playbackTime;
                        float timeDiff = playbackTime - oldplaybackTime;
                        playbackTime = oldplaybackTime + timeDiff * (0.05F * previewSpeed);
                    }

                    playbackTime = Mathf.Max(playbackTime, 0.0F);
                    ParticleSystemEditorUtils.playbackTime = playbackTime;

                    foreach (ParticleSystem ps in m_SelectedParticleSystems)
                    {
                        ParticleSystem root = ParticleSystemEditorUtils.GetRoot(ps);
                        if (root.isStopped)
                        {
                            root.Play();
                            root.Pause();
                        }
                    }

                    ParticleSystemEditorUtils.PerformCompleteResimulation();
                }

                // Detect start dragging
                if (oldEventType == EventType.MouseDown && GUIUtility.hotControl != oldHotControl)
                {
                    m_IsDraggingTimeHotControlID = GUIUtility.hotControl;
                    ParticleSystemEditorUtils.playbackIsScrubbing = true;
                }

                // Detect stop dragging
                if (m_IsDraggingTimeHotControlID != -1 && GUIUtility.hotControl != m_IsDraggingTimeHotControlID)
                {
                    m_IsDraggingTimeHotControlID = -1;
                    ParticleSystemEditorUtils.playbackIsScrubbing = false;
                }
            }

            int particleCount = 0;
            float fastestParticle = 0.0f;
            float slowestParticle = Mathf.Infinity;
            foreach (ParticleSystem ps in m_SelectedParticleSystems)
            {
                if (ps != null)
                    ps.CalculateEffectUIData(ref particleCount, ref fastestParticle, ref slowestParticle);
            }
            EditorGUILayout.LabelField(s_Texts.particleCount, GUIContent.Temp(particleCount.ToString()));

            bool hasSubEmitters = false;
            int subEmitterParticles = 0;
            foreach (ParticleSystem ps in m_SelectedParticleSystems)
            {
                int subEmitterParticlesCurrent = 0;
                if (ps != null && ps.CalculateEffectUISubEmitterData(ref subEmitterParticlesCurrent, ref fastestParticle, ref slowestParticle))
                {
                    hasSubEmitters = true;
                    subEmitterParticles += subEmitterParticlesCurrent;
                }
            }
            if (hasSubEmitters)
                EditorGUILayout.LabelField(s_Texts.subEmitterParticleCount, GUIContent.Temp(subEmitterParticles.ToString()));

            if (fastestParticle >= slowestParticle)
                EditorGUILayout.LabelField(s_Texts.particleSpeeds, GUIContent.Temp(slowestParticle.ToString(s_Texts.speedFloatFieldFormatString, CultureInfo.InvariantCulture.NumberFormat) + " - " + fastestParticle.ToString(s_Texts.speedFloatFieldFormatString, CultureInfo.InvariantCulture.NumberFormat)));
            else
                EditorGUILayout.LabelField(s_Texts.particleSpeeds, GUIContent.Temp("0.0 - 0.0"));

            if (!isPlayMode)
            {
                ParticleSystemEditorUtils.previewLayers = EditorGUILayout.LayerMaskField(ParticleSystemEditorUtils.previewLayers, s_Texts.previewLayers);
                ParticleSystemEditorUtils.resimulation = GUILayout.Toggle(ParticleSystemEditorUtils.resimulation, s_Texts.resimulation, EditorStyles.toggle);
            }

            m_ShowBounds = GUILayout.Toggle(m_ShowBounds, texts.showBounds, EditorStyles.toggle);

            EditorGUI.BeginChangeCheck();
            m_ShowOnlySelected = GUILayout.Toggle(m_ShowOnlySelected, texts.showOnlySelected, EditorStyles.toggle);
            if (EditorGUI.EndChangeCheck())
                SetShowOnlySelectedMode(m_ShowOnlySelected);

            EditorGUIUtility.labelWidth = 0.0f;
        }

        bool m_ScrubForward;
        bool m_ScrubReverse;
        float m_ScrubNextUpdate;

        void HandleScrubbing()
        {
            if ((!m_ScrubForward && !m_ScrubReverse) || Time.realtimeSinceStartup < m_ScrubNextUpdate)
                return;

            var evt = Event.current;

            var changeTime = 0;
            if (m_ScrubForward)
                changeTime++;
            if (m_ScrubReverse)
                changeTime--;

            ParticleSystemEditorUtils.playbackIsScrubbing = true;
            float previewSpeed = ParticleSystemEditorUtils.simulationSpeed;
            float timeDiff = (evt.shift ? 3f : 1f) * m_TimeHelper.deltaTime * (changeTime * 3f);
            ParticleSystemEditorUtils.playbackTime = Mathf.Max(0f, ParticleSystemEditorUtils.playbackTime + timeDiff * (previewSpeed));

            foreach (ParticleSystem ps in m_SelectedParticleSystems)
            {
                ParticleSystem root = ParticleSystemEditorUtils.GetRoot(ps);
                if (root.isStopped)
                {
                    root.Play();
                    root.Pause();
                }
            }

            ParticleSystemEditorUtils.PerformCompleteResimulation();

            // Mimic previous behavior that relied on key repeat
            m_ScrubNextUpdate = Time.realtimeSinceStartup + 1f / 15f;
        }

        internal bool HandleShortcutEvent(Event evt)
        {
            if (evt.commandName == s_PlayEvent.commandName)
            {
                if (EditorApplication.isPlaying)
                {
                    // If world is playing Pause is not handled, just restart instead
                    Stop();
                    Play();
                }
                else
                {
                    // In Edit mode we have full play/pause functionality
                    if (!ParticleSystemEditorUtils.playbackIsPlaying)
                        Play();
                    else
                        Pause();
                }

                return true;
            }
            else if (evt.commandName == s_StopEvent.commandName)
            {
                Stop();
                return true;
            }
            else if (evt.commandName == s_RestartEvent.commandName)
            {
                Stop();
                Play();
                return true;
            }
            else if (evt.commandName == s_ResimulationEvent.commandName)
            {
                ParticleSystemEditorUtils.resimulation = !ParticleSystemEditorUtils.resimulation;
                return true;
            }
            else if (evt.commandName == s_ShowBoundsEvent.commandName)
            {
                m_ShowBounds = !m_ShowBounds;
                return true;
            }
            else if (evt.commandName == s_ShowOnlySelectedEvent.commandName)
            {
                m_ShowOnlySelected = !m_ShowOnlySelected;
                return true;
            }
            else if (evt.commandName == s_ForwardBeginEvent.commandName)
            {
                m_ScrubForward = true;
                return true;
            }
            else if (evt.commandName == s_ForwardEndEvent.commandName)
            {
                m_ScrubForward = false;
                if (!m_ScrubReverse)
                    ParticleSystemEditorUtils.playbackIsScrubbing = false;
                return true;
            }
            else if (evt.commandName == s_ReverseBeginEvent.commandName)
            {
                m_ScrubReverse = true;
                return true;
            }
            else if (evt.commandName == s_ReverseEndEvent.commandName)
            {
                m_ScrubReverse = false;
                if (!m_ScrubForward)
                    ParticleSystemEditorUtils.playbackIsScrubbing = false;
                return true;
            }

            return false;
        }

        private void HandleKeyboardShortcuts()
        {
            var evt = Event.current;

            if (evt.type == EventType.ExecuteCommand)
            {
                if (HandleShortcutEvent(evt))
                    evt.Use();
            }

            HandleScrubbing();
        }

        internal static bool IsStopped(ParticleSystem root)
        {
            return (!ParticleSystemEditorUtils.playbackIsPlaying && !ParticleSystemEditorUtils.playbackIsPaused) && !ParticleSystemEditorUtils.playbackIsScrubbing;
        }

        internal bool IsPaused()
        {
            return !IsPlaying() && !IsStopped(ParticleSystemEditorUtils.GetRoot(m_SelectedParticleSystems[0]));
        }

        internal bool IsPlaying()
        {
            return ParticleSystemEditorUtils.playbackIsPlaying;
        }

        internal void Play()
        {
            bool anyPlayed = false;

            foreach (ParticleSystem ps in m_SelectedParticleSystems)
            {
                ParticleSystem root = ParticleSystemEditorUtils.GetRoot(ps);
                if (root)
                {
                    root.Play();
                    anyPlayed = true;
                }
            }

            if (anyPlayed)
            {
                ParticleSystemEditorUtils.playbackIsScrubbing = false;
                m_Owner.Repaint();
            }
        }

        internal void Pause()
        {
            bool anyPaused = false;

            foreach (ParticleSystem ps in m_SelectedParticleSystems)
            {
                ParticleSystem root = ParticleSystemEditorUtils.GetRoot(ps);
                if (root)
                {
                    root.Pause();
                    anyPaused = true;
                }
            }

            if (anyPaused)
            {
                ParticleSystemEditorUtils.playbackIsScrubbing = true;
                m_Owner.Repaint();
            }
        }

        internal void Stop()
        {
            ParticleSystemEditorUtils.playbackIsScrubbing = false;
            ParticleSystemEditorUtils.playbackTime = 0.0F;
            ParticleSystemEffectUtils.StopEffect();
            m_Owner.Repaint();
        }

        internal void PlayStopGUI()
        {
            if (s_Texts == null)
                s_Texts = new Texts();

            Event evt = Event.current;
            if (evt.type == EventType.Layout)
                m_TimeHelper.Update();

            bool disablePlayButton = (Time.timeScale == 0.0f);
            GUIContent playText = disablePlayButton ? s_Texts.playDisabled : s_Texts.play;

            if (!EditorApplication.isPlaying)
            {
                // Edit Mode: Play/Stop buttons
                GUILayout.BeginHorizontal(GUILayout.Width(220.0f));
                {
                    using (new EditorGUI.DisabledScope(disablePlayButton))
                    {
                        bool isPlaying = ParticleSystemEditorUtils.playbackIsPlaying && !ParticleSystemEditorUtils.playbackIsPaused && !disablePlayButton;
                        if (GUILayout.Button(isPlaying ? s_Texts.pause : playText, "ButtonLeft"))
                        {
                            if (isPlaying)
                                Pause();
                            else
                                Play();
                        }
                    }

                    if (GUILayout.Button(s_Texts.restart, "ButtonMid"))
                    {
                        Stop();
                        Play();
                    }

                    if (GUILayout.Button(s_Texts.stop, "ButtonRight"))
                    {
                        Stop();
                    }
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                // Play mode: we only handle play/stop (due to problems with determining if a system with subemitters is playing we cannot pause)
                GUILayout.BeginHorizontal();
                {
                    using (new EditorGUI.DisabledScope(disablePlayButton))
                    {
                        if (GUILayout.Button(playText))
                        {
                            Stop();
                            Play();
                        }
                    }
                    if (GUILayout.Button(s_Texts.stop))
                    {
                        Stop();
                    }
                }
                GUILayout.EndHorizontal();
            }

            // Playback info
            PlayBackInfoGUI(EditorApplication.isPlaying);

            // Handle shortcut keys last so we do not activate them if inputfield has used the event
            HandleKeyboardShortcuts();
        }

        private void InspectorParticleSystemGUI()
        {
            GUILayout.BeginVertical(ParticleSystemStyles.Get().effectBgStyle);
            {
                ParticleSystem selectedSystem = (m_SelectedParticleSystems.Count > 0) ? m_SelectedParticleSystems[0] : null;
                if (selectedSystem != null)
                {
                    ParticleSystemUI psUI = m_Emitters.FirstOrDefault(o => o.m_ParticleSystems[0] == selectedSystem);
                    if (psUI != null)
                    {
                        float width = GUIClip.visibleRect.width - 18; // -10 is effect_bg padding, -8 is inspector padding
                        psUI.OnGUI(width, false);
                    }
                }
            }

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();

            HandleKeyboardShortcuts();
        }

        private void DrawSelectionMarker(Rect rect)
        {
            rect.x += 1; rect.y += 1; rect.width -= 2; rect.height -= 2;
            ParticleSystemStyles.Get().selectionMarker.Draw(rect, GUIContent.none, false, true, true, false);
        }

        private List<ParticleSystemUI> GetSelectedParticleSystemUIs()
        {
            List<ParticleSystemUI> result = new List<ParticleSystemUI>();
            int[] selectedInstanceIDs = Selection.instanceIDs;
            foreach (ParticleSystemUI psUI in m_Emitters)
            {
                if (selectedInstanceIDs.Contains(psUI.m_ParticleSystems[0].gameObject.GetInstanceID()))
                    result.Add(psUI);
            }
            return result;
        }

        private void MultiParticleSystemGUI(bool verticalLayout)
        {
            // Background
            GUILayout.BeginVertical(ParticleSystemStyles.Get().effectBgStyle);
            m_EmitterAreaScrollPos = EditorGUILayout.BeginScrollView(m_EmitterAreaScrollPos);
            {
                Rect emitterAreaRect = EditorGUILayout.BeginVertical();
                {
                    // Click-Drag with Alt pressed in entire area
                    m_EmitterAreaScrollPos -= EditorGUI.MouseDeltaReader(emitterAreaRect, Event.current.alt);
                    // Top padding
                    GUILayout.Space(3);

                    GUILayout.BeginHorizontal();
                    // Left padding
                    GUILayout.Space(3); // added because cannot use padding due to clippling

                    // Draw Emitters
                    Color orgColor = GUI.color;
                    bool isRepaintEvent = Event.current.type == EventType.Repaint;
                    bool isShowOnlySelected = m_ShowOnlySelected;
                    List<ParticleSystemUI> selectedSystems = GetSelectedParticleSystemUIs();

                    for (int i = 0; i < m_Emitters.Length; ++i)
                    {
                        if (i != 0)
                            GUILayout.Space(ModuleUI.k_SpaceBetweenModules);

                        bool isSelected = selectedSystems.Contains(m_Emitters[i]);

                        ModuleUI rendererModuleUI = m_Emitters[i].GetParticleSystemRendererModuleUI();
                        if (isRepaintEvent && rendererModuleUI != null && !rendererModuleUI.enabled)
                            GUI.color = GetDisabledColor();

                        if (isRepaintEvent && isShowOnlySelected && !isSelected)
                            GUI.color = GetDisabledColor();

                        Rect psRect = EditorGUILayout.BeginVertical();
                        {
                            if (isRepaintEvent && isSelected && m_Emitters.Length > 1)
                                DrawSelectionMarker(psRect);

                            m_Emitters[i].OnGUI(ModuleUI.k_CompactFixedModuleWidth, true);
                        }
                        EditorGUILayout.EndVertical();

                        GUI.color = orgColor;
                    }

                    // Do not show the Add button when editing a prefab asset (case 1287185)
                    if (!PrefabUtility.IsPartOfPrefabAsset(m_SelectedParticleSystems[0]))
                    {
                        GUILayout.Space(5);
                        if (GUILayout.Button(s_Texts.addParticleSystem, "OL Plus", GUILayout.Width(20)))
                        {
                            // Store state of inspector before creating new particle system that will reload the inspector (new selected object)
                            //SessionState.SetFloat("CurrentEmitterAreaScroll", m_EmitterAreaScrollPos.x);
                            CreateParticleSystem(ParticleSystemEditorUtils.GetRoot(m_SelectedParticleSystems[0]), SubModuleUI.SubEmitterType.None);
                        }
                    }

                    GUILayout.FlexibleSpace(); // prevent centering
                    GUILayout.EndHorizontal();
                    GUILayout.Space(4);

                    // Click-Drag in background (does not require Alt pressed)
                    m_EmitterAreaScrollPos -= EditorGUI.MouseDeltaReader(emitterAreaRect, true);

                    GUILayout.FlexibleSpace();  // Makes the emitter area background extend to bottom
                }
                EditorGUILayout.EndVertical();  // EmitterAreaRect
            }
            EditorGUILayout.EndScrollView();

            GUILayout.EndVertical();    // Background

            //GUILayout.FlexibleSpace();    // Makes the emitter area background align to bottom of highest emitter

            // Handle shortcut keys last so we do not activate them if inputfield has used the event
            HandleKeyboardShortcuts();
        }

        private void WindowCurveEditorGUI(bool verticalLayout)
        {
            Rect rect;
            if (verticalLayout)
            {
                rect = GUILayoutUtility.GetRect(13, m_CurveEditorAreaHeight, GUILayout.MinHeight(m_CurveEditorAreaHeight));
            }
            else
            {
                EditorWindow win = (EditorWindow)m_Owner;
                System.Diagnostics.Debug.Assert(win != null);
                rect = GUILayoutUtility.GetRect(win.position.width - m_EmitterAreaWidth, win.position.height - 17);
            }

            // Get mouse down event before curve editor
            ResizeHandling(verticalLayout);

            m_ParticleSystemCurveEditor.OnGUI(rect);
        }

        void ResizeHandling(bool verticalLayout)
        {
            Rect dragRect;
            const float dragWidth = 5f;
            if (verticalLayout)
            {
                dragRect = GUILayoutUtility.GetLastRect();
                dragRect.y += -dragWidth;
                dragRect.height = dragWidth;

                // For horizontal layout we add a vertical size controller to adjust emitter area width
                float deltaY = EditorGUI.MouseDeltaReader(dragRect, true).y;
                if (deltaY != 0f)
                {
                    m_CurveEditorAreaHeight -= deltaY;
                    ClampWindowContentSizes();
                    EditorPrefs.SetFloat("ParticleSystemCurveEditorAreaHeight", m_CurveEditorAreaHeight);
                }

                if (Event.current.type == EventType.Repaint)
                    EditorGUIUtility.AddCursorRect(dragRect, MouseCursor.SplitResizeUpDown);
            }
            else
            {
                // For horizontal layout we add a vertical size controller to adjust emitter area width
                dragRect = new Rect(m_EmitterAreaWidth - dragWidth, 0, dragWidth, GUIClip.visibleRect.height);
                float deltaX = EditorGUI.MouseDeltaReader(dragRect, true).x;
                if (deltaX != 0f)
                {
                    m_EmitterAreaWidth += deltaX;
                    ClampWindowContentSizes();
                    EditorPrefs.SetFloat("ParticleSystemEmitterAreaWidth", m_EmitterAreaWidth);
                }

                if (Event.current.type == EventType.Repaint)
                    EditorGUIUtility.AddCursorRect(dragRect, MouseCursor.SplitResizeLeftRight);
            }
        }

        void ClampWindowContentSizes()
        {
            EventType type = Event.current.type;
            if (type != EventType.Layout)
            {
                float width = GUIClip.visibleRect.width;
                float height = GUIClip.visibleRect.height;
                bool verticalLayout = m_VerticalLayout;


                if (verticalLayout)
                    m_CurveEditorAreaHeight = Mathf.Clamp(m_CurveEditorAreaHeight, k_MinCurveAreaSize.y, height - k_MinEmitterAreaSize.y);
                else
                    m_EmitterAreaWidth = Mathf.Clamp(m_EmitterAreaWidth, k_MinEmitterAreaSize.x, width - k_MinCurveAreaSize.x);
            }
        }

        public void OnGUI()
        {
            // Init (if needed)
            if (s_Texts == null)
                s_Texts = new Texts();

            if (m_Emitters == null)
            {
                return;
            }

            // Cache play state so we can resume play after undo'ing and updating prefab instances (UUM-28514)
            if (Event.current.type ==  EventType.Repaint)
            {
                ParticleSystem root = ParticleSystemEditorUtils.GetRoot(m_SelectedParticleSystems[0]);
                if (ShouldManagePlaybackState(root))
                {
                    SavePlayState(root);
                }
            }

            // Grab the latest data from the object
            UpdateProperties();

            OwnerType ownerType = m_Owner is ParticleSystemInspector ? OwnerType.Inspector : OwnerType.ParticleSystemWindow;
            switch (ownerType)
            {
                case OwnerType.ParticleSystemWindow:
                {
                    ClampWindowContentSizes();
                    bool verticalLayout = m_VerticalLayout; // GUIClip.visibleRect.width < GUIClip.visibleRect.height;

                    if (verticalLayout)
                    {
                        MultiParticleSystemGUI(verticalLayout);
                        WindowCurveEditorGUI(verticalLayout);
                    }
                    else
                    {
                        GUILayout.BeginHorizontal();
                        MultiParticleSystemGUI(verticalLayout);
                        WindowCurveEditorGUI(verticalLayout);
                        GUILayout.EndHorizontal();
                    }
                }
                break;
                case OwnerType.Inspector:
                    // The inspector window already has added a vertical scrollview so no need to do it here
                    InspectorParticleSystemGUI();
                    break;
                default:
                    Debug.LogError("Unhandled enum");
                    break;
            }
            // Apply the property, handle undo
            ApplyModifiedProperties();
        }

        void ApplyModifiedProperties()
        {
            // Apply the properties, handles undo
            for (int i = 0; i < m_Emitters.Length; ++i)
                m_Emitters[i].ApplyProperties();
        }

        internal void UpdateProperties()
        {
            for (int i = 0; i < m_Emitters.Length; ++i)
                m_Emitters[i].UpdateProperties();
        }

        static internal bool GetAllModulesVisible()
        {
            return EditorPrefs.GetBool("ParticleSystemShowAllModules", true);
        }

        internal void SetAllModulesVisible(bool showAll)
        {
            EditorPrefs.SetBool("ParticleSystemShowAllModules", showAll);
            foreach (var particleSystemUI in m_Emitters)
            {
                for (int i = 0; i < particleSystemUI.m_Modules.Length; ++i)
                {
                    ModuleUI moduleUi = particleSystemUI.m_Modules[i];
                    if (moduleUi != null)
                    {
                        if (showAll)
                        {
                            if (!moduleUi.visibleUI)
                                moduleUi.visibleUI = true;
                        }
                        else
                        {
                            bool allowHiding = true;
                            if (moduleUi as RendererModuleUI != null)
                            {
                                if (particleSystemUI.m_ParticleSystems.FirstOrDefault(o => o.GetComponent<ParticleSystemRenderer>() == null) == null)
                                    allowHiding = false;
                            }

                            if (allowHiding && !moduleUi.enabled)
                                moduleUi.visibleUI = false;
                        }
                    }
                }
            }
        }

        internal void SetShowOnlySelectedMode(bool enabled)
        {
            int[] selectedInstanceIDs = Selection.instanceIDs;

            foreach (ParticleSystemUI psUI in m_Emitters)
            {
                foreach (ParticleSystem selected in psUI.m_ParticleSystems)
                {
                    if (selected != null)
                    {
                        ParticleSystem root = ParticleSystemEditorUtils.GetRoot(selected);
                        if (root == null)
                            continue;

                        ParticleSystem[] allSystems = GetParticleSystems(root);
                        foreach (ParticleSystem ps in allSystems)
                        {
                            ParticleSystemRenderer psRenderer = ps.GetComponent<ParticleSystemRenderer>();
                            if (psRenderer != null)
                            {
                                if (enabled)
                                    psRenderer.editorEnabled = selectedInstanceIDs.Contains(psRenderer.gameObject.GetInstanceID());
                                else
                                    psRenderer.editorEnabled = true;
                            }
                        }
                    }
                }
            }
        }

        [Overlay(typeof(SceneView), k_OverlayId, k_DisplayName, defaultDisplay = false)]
        class SceneViewParticleOverlay : TransientSceneViewOverlay
        {
            const string k_OverlayId = "Scene View/Particles";
            const string k_DisplayName = "Particles";

            public override bool visible
            {
                get { return s_EffectUi != null; }
            }

            public override void OnGUI()
            {
                if (s_EffectUi == null)
                    return;
                s_EffectUi.PlayStopGUI();
            }
        }
    }
}
