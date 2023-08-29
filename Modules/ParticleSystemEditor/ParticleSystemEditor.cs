// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Presets;
using UnityEditor.ShortcutManagement;

namespace UnityEditor
{
    [CustomEditor(typeof(ParticleSystem))]
    [CanEditMultipleObjects]
    internal class ParticleSystemInspector : Editor, ParticleEffectUIOwner, ICoupledEditor
    {
        ParticleEffectUI m_ParticleEffectUI;
        GUIContent m_PreviewTitle = EditorGUIUtility.TrTextContent("Particle System Curves");
        GUIContent showWindowText = EditorGUIUtility.TrTextContent("Open Editor...");
        GUIContent closeWindowText = EditorGUIUtility.TrTextContent("Close Editor");
        GUIContent hideWindowText = EditorGUIUtility.TrTextContent("Hide Editor");
        GUIContent selectSubEmitterOwner = EditorGUIUtility.TrTextContent("Select Sub-Emitter Owner");

        static GUIContent m_PlayBackTitle;

        public class ShortcutContext : IShortcutToolContext
        {
            public bool active { get; set; }
        }

        ShortcutContext m_ShortcutContext = new ShortcutContext { active = true };

        public static GUIContent playBackTitle
        {
            get
            {
                if (m_PlayBackTitle == null)
                    m_PlayBackTitle = EditorGUIUtility.TrTextContent("Particle Effect");
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
            EditorApplication.hierarchyChanged += HierarchyOrProjectWindowWasChanged;
            EditorApplication.projectChanged += HierarchyOrProjectWindowWasChanged;
            SceneView.duringSceneGui += OnSceneViewGUI;
            Undo.undoRedoEvent += UndoRedoPerformed;
            PrefabUtility.prefabInstanceUpdated += PrefabInstanceUpdated;
            PrefabUtility.prefabInstanceReverted += PrefabInstanceReverted;
            ShortcutIntegration.instance.contextManager.RegisterToolContext(m_ShortcutContext);
        }

        public void OnDisable()
        {
            EditorApplication.projectChanged -= HierarchyOrProjectWindowWasChanged;
            EditorApplication.hierarchyChanged -= HierarchyOrProjectWindowWasChanged;
            SceneView.duringSceneGui -= OnSceneViewGUI;
            Undo.undoRedoEvent -= UndoRedoPerformed;
            PrefabUtility.prefabInstanceUpdated -= PrefabInstanceUpdated;
            PrefabUtility.prefabInstanceReverted -= PrefabInstanceReverted;
            ShortcutIntegration.instance.contextManager.DeregisterToolContext(m_ShortcutContext);

            if (m_ParticleEffectUI != null)
            {
                m_ParticleEffectUI.Clear();
                m_ParticleEffectUI.ClearSelectedSystems();
            }
        }

        private void HierarchyOrProjectWindowWasChanged()
        {
            // target can be null if child particle system was deleted when ParticleSystemWindow is open
            if (target != null && ShouldShowInspector())
                Init(true);
        }

        internal bool HandleShortcutEvent(Event evt)
        {
            if (m_ParticleEffectUI != null)
                return m_ParticleEffectUI.HandleShortcutEvent(evt);
            return false;
        }

        void UndoRedoPerformed(in UndoRedoInfo info)
        {
            Init(true);
            if (m_ParticleEffectUI != null)
                m_ParticleEffectUI.UndoRedoPerformed(info);
        }

        private void PrefabInstanceUpdated(GameObject instance)
        {
            Init(forceInit : false);
            if (m_ParticleEffectUI != null)
                m_ParticleEffectUI.PrefabInstanceUpdated(instance);
        }

        private void PrefabInstanceReverted(GameObject instance)
        {
            Init(forceInit: false);
            if (m_ParticleEffectUI != null)
                m_ParticleEffectUI.PrefabInstanceReverted(instance);
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

        void ShowEditorButtonGUI()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (m_ParticleEffectUI != null)
                    m_ParticleEffectUI.m_SubEmitterSelected = false;

                if (m_ParticleEffectUI == null || !m_ParticleEffectUI.multiEdit)
                {
                    bool alreadySelected = selectedInParticleSystemWindow;
                    GameObject targetGameObject = (target as ParticleSystem).gameObject;

                    // Show a button to select the sub-emitter owner, if this system is a sub-emitter
                    List<ParticleSystem> owners = new List<ParticleSystem>();
                    var parent = targetGameObject.transform.parent;
                    while (parent != null)
                    {
                        var ps = parent.GetComponent<ParticleSystem>();
                        if (ps != null)
                        {
                            var subEmitters = ps.subEmitters;
                            if (subEmitters.enabled)
                            {
                                for (int i = 0; i < subEmitters.subEmittersCount; i++)
                                {
                                    var subEmitter = subEmitters.GetSubEmitterSystem(i);
                                    if (subEmitter != null && subEmitter.gameObject == targetGameObject)
                                    {
                                        owners.Add(ps);
                                        break;
                                    }
                                }
                            }
                        }

                        parent = parent.parent;
                    }
                    if (owners.Count > 0)
                    {
                        if (m_ParticleEffectUI != null)
                            m_ParticleEffectUI.m_SubEmitterSelected = true;

                        if (owners.Count == 1)
                        {
                            if (GUILayout.Button(GUIContent.Temp(selectSubEmitterOwner.text, owners[0].name), EditorStyles.miniButton, GUILayout.Width(160)))
                                Selection.activeGameObject = owners[0].gameObject;
                        }
                        else
                        {
                            if (EditorGUILayout.DropdownButton(selectSubEmitterOwner, FocusType.Passive, EditorStyles.miniButton, GUILayout.Width(160)))
                            {
                                GenericMenu menu = new GenericMenu();

                                foreach (var owner in owners)
                                    menu.AddItem(new GUIContent(owner.name), false, OnOwnerSelected, owner);
                                menu.AddSeparator("");
                                menu.AddItem(new GUIContent("Select All"), false, OnOwnersSelected, owners);

                                Rect buttonRect = GUILayoutUtility.topLevel.GetLast();
                                menu.DropDown(buttonRect);
                            }
                        }
                    }

                    // When editing a preset the GameObject will have the NotEditable flag.
                    // We do not support the ParticleSystemWindow for Presets for two reasons:
                    // - When selected the Preset editor creates a temporary GameObject which it then uses to edit the properties.
                    // The ParticleSystemWindow also uses the Selection system which triggers a selection change, the Preset
                    // editor cleans up the temp object and the ParticleSystemWindow is now unable to edit the system.
                    // - A preset will only contain a single system, so there is no benefit to using the window. (case 1198545)
                    if (Preset.IsEditorTargetAPreset(target))
                        return;

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
        }

        void OnOwnerSelected(object owner)
        {
            Selection.activeGameObject = ((ParticleSystem)owner).gameObject;
        }

        void OnOwnersSelected(object owners)
        {
            Selection.objects = ((List<ParticleSystem>)owners).Select(o => o.gameObject).ToArray();
        }

        public override bool UseDefaultMargins() { return false; }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

            ShowEditorButtonGUI();

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
            {
                m_ParticleEffectUI.Clear();
                m_ParticleEffectUI.ClearSelectedSystems();
            }
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

        SerializedObject ICoupledEditor.coupledComponent
        {
            get
            {
                Init(false);
                return m_ParticleEffectUI.m_Emitters[0].m_RendererSerializedObject;
            }
        }
    }
} // namespace UnityEditor
