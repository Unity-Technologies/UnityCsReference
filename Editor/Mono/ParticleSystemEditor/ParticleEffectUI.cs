// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
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
        public ParticleSystemUI[] m_Emitters;               // Contains UI for all ParticleSystem children of the root ParticleSystem for this effect
        bool m_EmittersActiveInHierarchy;
        ParticleSystemCurveEditor m_ParticleSystemCurveEditor; // The curve editor used by ParticleSystem modules
        List<ParticleSystem> m_SelectedParticleSystems;     // This is the array of selected particle systems and used to find the root ParticleSystem and for the inspector
        bool m_ShowOnlySelectedMode;
        TimeHelper m_TimeHelper = new TimeHelper();
        public static bool m_ShowWireframe = false;
        public static bool m_ShowBounds = false;
        public static bool m_VerticalLayout;
        const string k_SimulationStateId = "SimulationState";
        const string k_ShowSelectedId = "ShowSelected";
        enum PlayState { Stopped = 0, Playing = 1, Paused = 2 };

        // ParticleSystemWindow Layout
        static readonly Vector2 k_MinEmitterAreaSize = new Vector2(125f, 100);
        static readonly Vector2 k_MinCurveAreaSize = new Vector2(100, 100);
        float m_EmitterAreaWidth = 230;                                 // Only used in ParticleSystemWindow for horizontal layout
        float m_CurveEditorAreaHeight = 330;                            // Only used in ParticleSystemWindow for vertical layout
        Vector2 m_EmitterAreaScrollPos = Vector2.zero;
        static readonly Color k_DarkSkinDisabledColor = new Color(0.66f, 0.66f, 0.66f, 0.95f);
        static readonly Color k_LightSkinDisabledColor = new Color(0.84f, 0.84f, 0.84f, 0.95f);

        private enum OwnerType { Inspector, ParticleSystemWindow };

        internal class Texts
        {
            public GUIContent previewSpeed = new GUIContent("Playback Speed");
            public GUIContent previewTime = new GUIContent("Playback Time");
            public GUIContent particleCount = new GUIContent("Particles");
            public GUIContent subEmitterParticleCount = new GUIContent("Sub Emitter Particles");
            public GUIContent particleSpeeds = new GUIContent("Speed Range");
            public GUIContent play = new GUIContent("Simulate");
            public GUIContent stop = new GUIContent("Stop");
            public GUIContent pause = new GUIContent("Pause");
            public GUIContent addParticleSystem = new GUIContent("", "Create Particle System");
            public GUIContent wireframe = new GUIContent("Selection", "Show particles with the selection outline/wireframe, based on the selection options in the Gizmos menu");
            public GUIContent bounds = new GUIContent("Bounds", "Show world space bounding boxes");
            public GUIContent resimulation = new GUIContent("Resimulate", "If resimulate is enabled the particle system will show changes made to the system immediately (including changes made to the particle system transform)");
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

        static PrefKey kPlay = new PrefKey("ParticleSystem/Play", ",");
        static PrefKey kStop = new PrefKey("ParticleSystem/Stop", ".");
        static PrefKey kForward = new PrefKey("ParticleSystem/Forward", "m");
        static PrefKey kReverse = new PrefKey("ParticleSystem/Reverse", "n");


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
            return !EditorApplication.isPlaying && active;
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
            bool multiEdit = (allSystems.Count() > 1);

            bool initializeRequired = false;
            ParticleSystem mainSystem = null;

            foreach (ParticleSystem shuriken in allSystems)
            {
                ParticleSystem[] shurikens;
                if (!multiEdit)
                {
                    ParticleSystem root = ParticleSystemEditorUtils.GetRoot(shuriken);
                    if (root == null)
                        continue;

                    shurikens = GetParticleSystems(root);

                    // Check if we need to re-initialize?
                    if (m_SelectedParticleSystems != null && m_SelectedParticleSystems.Count > 0)
                    {
                        if (root == ParticleSystemEditorUtils.GetRoot(m_SelectedParticleSystems[0]))
                        {
                            if (m_ParticleSystemCurveEditor != null && m_Emitters != null && shurikens.Length == m_Emitters.Length && shuriken.gameObject.activeInHierarchy == m_EmittersActiveInHierarchy)
                            {
                                m_SelectedParticleSystems = new List<ParticleSystem>();
                                m_SelectedParticleSystems.Add(shuriken);
                                continue;
                            }
                        }
                    }

                    mainSystem = root;
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
                if (anyAdded == false)
                {
                    m_SelectedParticleSystems = new List<ParticleSystem>();
                    anyAdded = true;
                }
                m_SelectedParticleSystems.Add(shuriken);

                // Single edit emitter setup
                if (!multiEdit)
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
                if (multiEdit)
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

                ParticleSystemEditorUtils.PerformCompleteResimulation();

                m_EmitterAreaWidth = EditorPrefs.GetFloat("ParticleSystemEmitterAreaWidth", k_MinEmitterAreaSize.x);
                m_CurveEditorAreaHeight = EditorPrefs.GetFloat("ParticleSystemCurveEditorAreaHeight", k_MinCurveAreaSize.y);

                // For now only allow ShowOnlySelectedMode for ParticleSystemWindow
                m_ShowOnlySelectedMode = (m_Owner is ParticleSystemWindow) ? SessionState.GetBool(k_ShowSelectedId + mainSystem.GetInstanceID(), false) : false;

                m_EmitterAreaScrollPos.x = SessionState.GetFloat("CurrentEmitterAreaScroll", 0.0f);

                if (ShouldManagePlaybackState(mainSystem))
                {
                    // Restore lastPlayBackTime if available in session cache
                    Vector3 simulationState = SessionState.GetVector3(k_SimulationStateId + mainSystem.GetInstanceID(), Vector3.zero);
                    if (mainSystem.GetInstanceID() == (int)simulationState.x)
                    {
                        float lastPlayBackTime = simulationState.z;
                        if (lastPlayBackTime > 0f)
                            ParticleSystemEditorUtils.editorPlaybackTime = lastPlayBackTime;
                    }

                    // Always play when initializing a particle system
                    Play();
                }
            }

            return initializeRequired;
        }

        internal void UndoRedoPerformed()
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
                            moduleUI.UndoRedoPerformed();
                    }
                }
            }

            m_Owner.Repaint();
        }

        public void Clear()
        {
            ParticleSystem root = ParticleSystemEditorUtils.GetRoot(m_SelectedParticleSystems[0]); // root can have been deleted
            if (ShouldManagePlaybackState(root))
            {
                // Store simulation state of current effect as Vector3 (rootInstanceID, isPlaying, playBackTime)
                if (root != null)
                {
                    PlayState playState;
                    if (IsPlaying())
                        playState = PlayState.Playing;
                    else if (IsPaused())
                        playState = PlayState.Paused;
                    else
                        playState = PlayState.Stopped;
                    int rootInstanceId = root.GetInstanceID();
                    SessionState.SetVector3(k_SimulationStateId + rootInstanceId, new Vector3(rootInstanceId, (int)playState, ParticleSystemEditorUtils.editorPlaybackTime));
                }

                // Stop the ParticleSystem here (prevents it being frozen on screen)
                //Stop();
            }

            m_ParticleSystemCurveEditor.OnDisable();
            Tools.s_Hidden = false; // The collisionmodule might have hidden the tools

            if (root != null)
                SessionState.SetBool(k_ShowSelectedId + root.GetInstanceID(), m_ShowOnlySelectedMode);
            SetShowOnlySelectedMode(false);

            GameView.RepaintAll();
            SceneView.RepaintAll();
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
                nextName = "Particle System " + i;
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
            return "Particle System";
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
                    ps.SetupDefaultType((int)defaultType);

                SessionState.SetFloat("CurrentEmitterAreaScroll", m_EmitterAreaScrollPos.x);

                // Assign default material
                ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
                Material particleMat = null;
                if (GraphicsSettings.renderPipelineAsset != null)
                    particleMat = GraphicsSettings.renderPipelineAsset.GetDefaultParticleMaterial();

                if (particleMat == null)
                    particleMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
                renderer.materials = new[] {particleMat, null};

                Undo.RegisterCreatedObjectUndo(go, "Create ParticleSystem");
                return go;
            }
            return null;
        }

        public ParticleSystemCurveEditor GetParticleSystemCurveEditor()
        {
            return m_ParticleSystemCurveEditor;
        }

        private void SceneViewGUICallback(Object target, SceneView sceneView)
        {
            PlayStopGUI();
        }

        public void OnSceneViewGUI()
        {
            ParticleSystem root = ParticleSystemEditorUtils.GetRoot(m_SelectedParticleSystems[0]);
            if (root && root.gameObject.activeInHierarchy)
                SceneViewOverlay.Window(ParticleSystemInspector.playBackTitle, SceneViewGUICallback, (int)SceneViewOverlay.Ordering.ParticleEffect, SceneViewOverlay.WindowDisplayOption.OneWindowPerTitle);

            foreach (ParticleSystemUI e in m_Emitters)
                e.OnSceneViewGUI();
        }

        int m_IsDraggingTimeHotControlID = -1;

        internal void PlayBackTimeGUI()
        {
            EventType oldEventType = Event.current.type;
            int oldHotControl = GUIUtility.hotControl;
            string oldFormat = EditorGUI.kFloatFieldFormatString;

            EditorGUIUtility.labelWidth = 110.0f;

            EditorGUI.kFloatFieldFormatString = s_Texts.secondsFloatFieldFormatString;
            ParticleSystemEditorUtils.editorSimulationSpeed = Mathf.Clamp(EditorGUILayout.FloatField(s_Texts.previewSpeed, ParticleSystemEditorUtils.editorSimulationSpeed /*, ParticleSystemStyles.Get().numberField*/), 0f, 10f);
            EditorGUI.kFloatFieldFormatString = oldFormat;


            EditorGUI.BeginChangeCheck();
            EditorGUI.kFloatFieldFormatString = s_Texts.secondsFloatFieldFormatString;
            float editorPlaybackTime = EditorGUILayout.FloatField(s_Texts.previewTime, ParticleSystemEditorUtils.editorPlaybackTime);
            EditorGUI.kFloatFieldFormatString = oldFormat;
            if (EditorGUI.EndChangeCheck())
            {
                if (oldEventType == EventType.MouseDrag)
                {
                    ParticleSystemEditorUtils.editorIsScrubbing = true;
                    float previewSpeed = ParticleSystemEditorUtils.editorSimulationSpeed;
                    float oldEditorPlaybackTime = ParticleSystemEditorUtils.editorPlaybackTime;
                    float timeDiff = editorPlaybackTime - oldEditorPlaybackTime;
                    editorPlaybackTime = oldEditorPlaybackTime + timeDiff * (0.05F * previewSpeed);
                }

                editorPlaybackTime = Mathf.Max(editorPlaybackTime, 0.0F);
                ParticleSystemEditorUtils.editorPlaybackTime = editorPlaybackTime;

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
                ParticleSystemEditorUtils.editorIsScrubbing = true;
            }

            // Detect stop dragging
            if (m_IsDraggingTimeHotControlID != -1 && GUIUtility.hotControl != m_IsDraggingTimeHotControlID)
            {
                m_IsDraggingTimeHotControlID = -1;
                ParticleSystemEditorUtils.editorIsScrubbing = false;
            }

            int particleCount = 0;
            float fastestParticle = 0.0f;
            float slowestParticle = Mathf.Infinity;
            foreach (ParticleSystem ps in m_SelectedParticleSystems)
            {
                ps.CalculateEffectUIData(ref particleCount, ref fastestParticle, ref slowestParticle);
            }
            EditorGUILayout.LabelField(s_Texts.particleCount, GUIContent.Temp(particleCount.ToString()));

            bool hasSubEmitters = false;
            int subEmitterParticles = 0;
            foreach (ParticleSystem ps in m_SelectedParticleSystems)
            {
                int subEmitterParticlesCurrent = 0;
                if (ps.CalculateEffectUISubEmitterData(ref subEmitterParticlesCurrent, ref fastestParticle, ref slowestParticle))
                {
                    hasSubEmitters = true;
                    subEmitterParticles += subEmitterParticlesCurrent;
                }
            }
            if (hasSubEmitters)
                EditorGUILayout.LabelField(s_Texts.subEmitterParticleCount, GUIContent.Temp(subEmitterParticles.ToString()));

            if (fastestParticle >= slowestParticle)
                EditorGUILayout.LabelField(s_Texts.particleSpeeds, GUIContent.Temp(slowestParticle.ToString(s_Texts.speedFloatFieldFormatString) + " - " + fastestParticle.ToString(s_Texts.speedFloatFieldFormatString)));
            else
                EditorGUILayout.LabelField(s_Texts.particleSpeeds, GUIContent.Temp("0.0 - 0.0"));

            EditorGUIUtility.labelWidth = 0.0f;
        }

        private void HandleKeyboardShortcuts()
        {
            Event evt = Event.current;

            if (evt.type == EventType.KeyDown)
            {
                int changeTime = 0;
                if (evt.keyCode == ((Event)kPlay).keyCode)
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
                        if (!ParticleSystemEditorUtils.editorIsPlaying)
                            Play();
                        else
                            Pause();
                    }
                    evt.Use();
                }
                else if (evt.keyCode == ((Event)kStop).keyCode)
                {
                    Stop();
                    evt.Use();
                }
                else if (evt.keyCode == ((Event)kReverse).keyCode)
                {
                    changeTime = -1;
                }
                else if (evt.keyCode == ((Event)kForward).keyCode)
                {
                    changeTime = 1;
                }

                if (changeTime != 0)
                {
                    ParticleSystemEditorUtils.editorIsScrubbing = true;
                    float previewSpeed = ParticleSystemEditorUtils.editorSimulationSpeed;
                    float timeDiff = (evt.shift ? 3f : 1f) * m_TimeHelper.deltaTime * (changeTime > 0 ? 3f : -3f);
                    ParticleSystemEditorUtils.editorPlaybackTime = Mathf.Max(0f, ParticleSystemEditorUtils.editorPlaybackTime + timeDiff * (previewSpeed));

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
                    evt.Use();
                }
            }

            if (evt.type == EventType.KeyUp)
                if (evt.keyCode == ((Event)kReverse).keyCode || evt.keyCode == ((Event)kForward).keyCode)
                    ParticleSystemEditorUtils.editorIsScrubbing = false;
        }

        internal static bool IsStopped(ParticleSystem root)
        {
            return (!ParticleSystemEditorUtils.editorIsPlaying && !ParticleSystemEditorUtils.editorIsPaused) && !ParticleSystemEditorUtils.editorIsScrubbing;
        }

        internal bool IsPaused()
        {
            return !IsPlaying() && !IsStopped(ParticleSystemEditorUtils.GetRoot(m_SelectedParticleSystems[0]));
        }

        internal bool IsPlaying()
        {
            return ParticleSystemEditorUtils.editorIsPlaying;
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
                ParticleSystemEditorUtils.editorIsScrubbing = false;
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
                ParticleSystemEditorUtils.editorIsScrubbing = true;
                m_Owner.Repaint();
            }
        }

        internal void Stop()
        {
            ParticleSystemEditorUtils.editorIsScrubbing = false;
            ParticleSystemEditorUtils.editorPlaybackTime = 0.0F;
            ParticleSystemEditorUtils.StopEffect();
            m_Owner.Repaint();
        }

        internal void PlayStopGUI()
        {
            if (s_Texts == null)
                s_Texts = new Texts();

            Event evt = Event.current;
            if (evt.type == EventType.layout)
                m_TimeHelper.Update();

            if (!EditorApplication.isPlaying)
            {
                // Edit Mode: Play/Stop buttons
                GUILayout.BeginHorizontal(GUILayout.Width(200.0f));
                {
                    bool isPlaying = ParticleSystemEditorUtils.editorIsPlaying && !ParticleSystemEditorUtils.editorIsPaused;
                    if (GUILayout.Button(isPlaying ? s_Texts.pause : s_Texts.play, "ButtonLeft"))
                    {
                        if (isPlaying)
                            Pause();
                        else
                            Play();
                    }

                    if (GUILayout.Button(s_Texts.stop, "ButtonRight"))
                    {
                        Stop();
                    }
                }
                GUILayout.EndHorizontal();

                // Playback time
                PlayBackTimeGUI();
            }
            else
            {
                // Play mode: we only handle play/stop (due to problems with determining if a system with subemitters is playing we cannot pause)
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(s_Texts.play))
                    {
                        Stop();
                        Play();
                    }
                    if (GUILayout.Button(s_Texts.stop))
                    {
                        Stop();
                    }
                }
                GUILayout.EndHorizontal();
            }

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

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            ParticleSystemEditorUtils.editorResimulation = GUILayout.Toggle(ParticleSystemEditorUtils.editorResimulation, s_Texts.resimulation, EditorStyles.toggle);
            ParticleEffectUI.m_ShowWireframe = GUILayout.Toggle(ParticleEffectUI.m_ShowWireframe, ParticleEffectUI.texts.wireframe, EditorStyles.toggle);
            ParticleEffectUI.m_ShowBounds = GUILayout.Toggle(ParticleEffectUI.m_ShowBounds, ParticleEffectUI.texts.bounds, EditorStyles.toggle);
            GUILayout.EndHorizontal();

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
                    bool isRepaintEvent = Event.current.type == EventType.repaint;
                    bool isShowOnlySelected = IsShowOnlySelectedMode();
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

                    GUILayout.Space(5);
                    if (GUILayout.Button(s_Texts.addParticleSystem, "OL Plus", GUILayout.Width(20)))
                    {
                        // Store state of inspector before creating new particle system that will reload the inspector (new selected object)
                        //SessionState.SetFloat("CurrentEmitterAreaScroll", m_EmitterAreaScrollPos.x);
                        CreateParticleSystem(ParticleSystemEditorUtils.GetRoot(m_SelectedParticleSystems[0]), SubModuleUI.SubEmitterType.None);
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

        Rect ResizeHandling(bool verticalLayout)
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
            return dragRect;
        }

        void ClampWindowContentSizes()
        {
            EventType type = Event.current.type;
            if (type != EventType.layout)
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

        internal bool IsShowOnlySelectedMode()
        {
            return m_ShowOnlySelectedMode;
        }

        internal void SetShowOnlySelectedMode(bool enable)
        {
            m_ShowOnlySelectedMode = enable;
        }
    }
} // namespace UnityEditor
