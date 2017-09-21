// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// The ParticleSystemUI displays and manages the modules of a particle system.

namespace UnityEditor
{
    internal class ParticleSystemUI
    {
        public ParticleEffectUI m_ParticleEffectUI; // owner
        public ModuleUI[] m_Modules;
        public ParticleSystem[] m_ParticleSystems;
        public SerializedObject m_ParticleSystemSerializedObject;
        public SerializedObject m_RendererSerializedObject;
        private static string[] s_ModuleNames;
        private string m_SupportsCullingText;
        private string m_SupportsCullingTextLabel; // Cached version including bullet points

        // Keep in sync with ParticleSystemEditor.h
        public enum DefaultTypes
        {
            Root,
            SubBirth,
            SubCollision,
            SubDeath,
        };

        protected class Texts
        {
            public GUIContent addModules = new GUIContent("", "Show/Hide Modules");
            public string bulletPoint = "\u2022 ";
        }
        private static Texts s_Texts;

        public bool multiEdit { get { return (m_ParticleSystems != null) && (m_ParticleSystems.Length > 1); } }

        public void Init(ParticleEffectUI owner, ParticleSystem[] systems)
        {
            if (s_ModuleNames == null)
                s_ModuleNames = GetUIModuleNames();
            if (s_Texts == null)
                s_Texts = new Texts();

            m_ParticleEffectUI = owner;
            m_ParticleSystems = systems;
            m_ParticleSystemSerializedObject = new SerializedObject(m_ParticleSystems);
            m_RendererSerializedObject = null;

            m_SupportsCullingText = null;

            m_Modules = CreateUIModules(this, m_ParticleSystemSerializedObject);

            bool anyWithoutRenderers = m_ParticleSystems.FirstOrDefault(o => o.GetComponent<ParticleSystemRenderer>() == null) != null;
            if (!anyWithoutRenderers)
                InitRendererUI();

            UpdateParticleSystemInfoString();
        }

        internal ModuleUI GetParticleSystemRendererModuleUI()
        {
            return m_Modules[m_Modules.Length - 1];
        }

        private void InitRendererUI()
        {
            List<ParticleSystemRenderer> renderers = new List<ParticleSystemRenderer>();
            foreach (ParticleSystem ps in m_ParticleSystems)
            {
                // Ensure we have a renderer
                ParticleSystemRenderer psRenderer = ps.GetComponent<ParticleSystemRenderer>();
                if (psRenderer == null)
                {
                    ps.gameObject.AddComponent<ParticleSystemRenderer>();
                }
                renderers.Add(ps.GetComponent<ParticleSystemRenderer>());
            }

            // Create RendererModuleUI
            if (renderers.Count > 0)
            {
                System.Diagnostics.Debug.Assert(m_Modules[m_Modules.Length - 1] == null); // If hitting this assert we have either not cleaned up the previous renderer or hitting another module

                m_RendererSerializedObject = new SerializedObject(renderers.ToArray());
                m_Modules[m_Modules.Length - 1] = new RendererModuleUI(this, m_RendererSerializedObject, s_ModuleNames[s_ModuleNames.Length - 1]);
            }
        }

        private void ClearRenderers()
        {
            // Remove renderer components
            m_RendererSerializedObject = null;
            foreach (ParticleSystem ps in m_ParticleSystems)
            {
                ParticleSystemRenderer psRenderer = ps.GetComponent<ParticleSystemRenderer>();
                if (psRenderer != null)
                {
                    Undo.DestroyObjectImmediate(psRenderer);
                }
            }
            m_Modules[m_Modules.Length - 1] = null;
        }

        public float GetEmitterDuration()
        {
            InitialModuleUI m = m_Modules[0] as InitialModuleUI;
            if (m != null)
                return m.m_LengthInSec.floatValue;
            return -1.0f;
        }

        public void OnGUI(float width, bool fixedWidth)
        {
            bool isRepaintEvent = Event.current.type == EventType.Repaint;

            // Name of current emitter
            string selectedEmitterName = null;
            if (m_ParticleSystems.Length > 1)
            {
                selectedEmitterName = "Multiple Particle Systems";
            }
            else if (m_ParticleSystems.Length > 0)
            {
                selectedEmitterName = m_ParticleSystems[0].gameObject.name;
            }

            if (fixedWidth)
            {
                EditorGUIUtility.labelWidth = width * 0.4f;
                EditorGUILayout.BeginVertical(GUILayout.Width(width));
            }
            else
            {
                // First make sure labelWidth is at default width, then subtract
                EditorGUIUtility.labelWidth = 0;
                EditorGUIUtility.labelWidth = EditorGUIUtility.labelWidth - 4;

                EditorGUILayout.BeginVertical();
            }

            {
                InitialModuleUI initial = (InitialModuleUI)m_Modules[0];
                for (int i = 0; i < m_Modules.Length; ++i)
                {
                    ModuleUI module = m_Modules[i];
                    if (module == null)
                        continue;

                    bool initialModule = (module == m_Modules[0]);

                    // Skip if not visible (except initial module which should always be visible)
                    if (!module.visibleUI && !initialModule)
                        continue;

                    // Module header size
                    GUIContent headerLabel = new GUIContent();
                    GUIStyle headerStyle;
                    Rect moduleHeaderRect;
                    if (initialModule)
                    {
                        moduleHeaderRect = GUILayoutUtility.GetRect(width, 25);
                        headerStyle = ParticleSystemStyles.Get().emitterHeaderStyle;
                    }
                    else
                    {
                        moduleHeaderRect = GUILayoutUtility.GetRect(width, 15);
                        headerStyle = ParticleSystemStyles.Get().moduleHeaderStyle;
                    }

                    // Module content here to render it below the the header
                    if (module.foldout)
                    {
                        using (new EditorGUI.DisabledScope(!module.enabled))
                        {
                            Rect moduleSize = EditorGUILayout.BeginVertical(ParticleSystemStyles.Get().modulePadding);
                            {
                                moduleSize.y -= 4; // pull background 'up' behind title to fill rounded corners.
                                moduleSize.height += 4;
                                GUI.Label(moduleSize, GUIContent.none, ParticleSystemStyles.Get().moduleBgStyle);
                                module.OnInspectorGUI(initial);
                            }
                            EditorGUILayout.EndVertical();
                        }
                    }

                    // TODO: Get Texture instead of static preview. Render Icon (below titlebar due to rounded corners)
                    if (initialModule)
                    {
                        // Get preview of material or mesh
                        ParticleSystemRenderer renderer = m_ParticleSystems[0].GetComponent<ParticleSystemRenderer>();
                        float iconSize = 21;
                        Rect iconRect = new Rect(moduleHeaderRect.x + 4, moduleHeaderRect.y + 2, iconSize, iconSize);

                        if (isRepaintEvent && renderer != null)
                        {
                            bool iconRendered = false;
                            int instanceID = 0;

                            if (!multiEdit)
                            {
                                if (renderer.renderMode == ParticleSystemRenderMode.Mesh)
                                {
                                    if (renderer.mesh != null)
                                        instanceID = renderer.mesh.GetInstanceID();
                                }
                                else if (renderer.sharedMaterial != null)
                                {
                                    instanceID = renderer.sharedMaterial.GetInstanceID();
                                }

                                // If the asset is dirty we ensure to get a updated one by clearing cache of temporary previews
                                if (EditorUtility.IsDirty(instanceID))
                                    AssetPreview.ClearTemporaryAssetPreviews();

                                if (instanceID != 0)
                                {
                                    Texture2D icon = AssetPreview.GetAssetPreview(instanceID);
                                    if (icon != null)
                                    {
                                        GUI.DrawTexture(iconRect, icon, ScaleMode.StretchToFill, true);
                                        iconRendered = true;
                                    }
                                }
                            }

                            // Fill so we do not see the background when we have no icon (eg multi-edit)
                            if (!iconRendered)
                            {
                                GUI.Label(iconRect, GUIContent.none, ParticleSystemStyles.Get().moduleBgStyle);
                            }
                        }

                        // Select gameObject when clicking on icon
                        if (!multiEdit && EditorGUI.DropdownButton(iconRect, GUIContent.none, FocusType.Passive, GUIStyle.none))
                        {
                            // Toggle selected particle system from selection
                            if (EditorGUI.actionKey)
                            {
                                List<int> newSelection = new List<int>();
                                int instanceID = m_ParticleSystems[0].gameObject.GetInstanceID();
                                newSelection.AddRange(Selection.instanceIDs);
                                if (!newSelection.Contains(instanceID) || newSelection.Count != 1)
                                {
                                    if (newSelection.Contains(instanceID))
                                        newSelection.Remove(instanceID);
                                    else
                                        newSelection.Add(instanceID);
                                }

                                Selection.instanceIDs = newSelection.ToArray();
                            }
                            else
                            {
                                Selection.instanceIDs = new int[0];
                                Selection.activeInstanceID = m_ParticleSystems[0].gameObject.GetInstanceID();
                            }
                        }
                    }

                    // Button logic for enabledness (see below for UI)
                    Rect checkMarkRect = new Rect(moduleHeaderRect.x + 2, moduleHeaderRect.y + 1, 13, 13);
                    if (!initialModule && GUI.Button(checkMarkRect, GUIContent.none, GUIStyle.none))
                        module.enabled = !module.enabled;

                    // Button logic for plus/minus (see below for UI)
                    Rect plusRect = new Rect(moduleHeaderRect.x + moduleHeaderRect.width - 10, moduleHeaderRect.y + moduleHeaderRect.height - 10, 10, 10);
                    Rect plusRectInteract = new Rect(plusRect.x - 4, plusRect.y - 4, plusRect.width + 4, plusRect.height + 4);
                    Rect infoRect = new Rect(plusRect.x - 23, plusRect.y - 8, 20, 20);

                    if (initialModule && EditorGUI.DropdownButton(plusRectInteract, s_Texts.addModules, FocusType.Passive, GUIStyle.none))
                        ShowAddModuleMenu();

                    // Module header (last to become top most renderered)
                    if (!string.IsNullOrEmpty(selectedEmitterName))
                        headerLabel.text = initialModule ? selectedEmitterName : module.displayName;
                    else
                        headerLabel.text = module.displayName;
                    headerLabel.tooltip = module.toolTip;
                    bool newToggleState = GUI.Toggle(moduleHeaderRect, module.foldout, headerLabel, headerStyle);
                    if (newToggleState != module.foldout)
                    {
                        switch (Event.current.button)
                        {
                            case 0:
                                bool newFoldoutState = !module.foldout;
                                if (Event.current.control)
                                {
                                    foreach (var moduleUi in m_Modules)
                                        if (moduleUi != null && moduleUi.visibleUI)
                                            moduleUi.foldout = newFoldoutState;
                                }
                                else
                                {
                                    module.foldout = newFoldoutState;
                                }
                                break;
                            case 1:
                                if (initialModule)
                                    ShowEmitterMenu();
                                else
                                    ShowModuleMenu(i);
                                break;
                        }
                    }

                    // Render checkmark on top (logic: see above)
                    if (!initialModule)
                    {
                        EditorGUI.showMixedValue = module.enabledHasMultipleDifferentValues;
                        GUIStyle style = EditorGUI.showMixedValue ? ParticleSystemStyles.Get().checkmarkMixed : ParticleSystemStyles.Get().checkmark;
                        GUI.Toggle(checkMarkRect, module.enabled, GUIContent.none, style);
                        EditorGUI.showMixedValue = false;
                    }

                    // Render plus/minus on top
                    if (isRepaintEvent && initialModule)
                        GUI.Label(plusRect, GUIContent.none, ParticleSystemStyles.Get().plus);

                    if (initialModule && !string.IsNullOrEmpty(m_SupportsCullingTextLabel))
                    {
                        var supportsCullingText = new GUIContent("", ParticleSystemStyles.Get().warningIcon, m_SupportsCullingTextLabel);
                        GUI.Label(infoRect, supportsCullingText);
                    }

                    GUILayout.Space(1); // dist to next module
                } // foreach module
                GUILayout.Space(-1);
            }
            EditorGUILayout.EndVertical(); // end fixed moduleWidth

            // Apply the property, handle undo
            ApplyProperties();
        }

        public void OnSceneViewGUI()
        {
            if (m_Modules == null)
                return;

            // Render bounds
            if (ParticleEffectUI.m_ShowBounds)
            {
                foreach (ParticleSystem ps in m_ParticleSystems)
                {
                    if (multiEdit)
                        ShowBounds(ParticleSystemEditorUtils.GetRoot(ps));
                    else
                        ShowBounds(ps);
                }
            }

            UpdateProperties();

            foreach (var module in m_Modules)
            {
                if (module == null || !module.visibleUI || !module.enabled)
                    continue;

                if (module.foldout)
                {
                    module.OnSceneViewGUI();
                }
            }
            // Apply the property, handle undo
            ApplyProperties();
        }

        private void ShowBounds(ParticleSystem ps)
        {
            if (ps.particleCount > 0)
            {
                ParticleSystemRenderer particleSystemRenderer = ps.GetComponent<ParticleSystemRenderer>();

                var oldCol = Handles.color;
                Handles.color = Color.yellow;
                var worldBounds = particleSystemRenderer.bounds;
                Handles.DrawWireCube(worldBounds.center, worldBounds.size);
                Handles.color = oldCol;
            }

            // In multi-edit, children are not stored, so render thir bounds manually
            if (multiEdit)
            {
                ParticleSystem[] children = ps.transform.GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem child in children)
                {
                    if (child != ps)
                    {
                        bool alreadySelected = m_ParticleSystems.FirstOrDefault(o => ParticleSystemEditorUtils.GetRoot(o) == child) != null;
                        if (!alreadySelected)
                            ShowBounds(child);
                    }
                }
            }
        }

        public void ApplyProperties()
        {
            bool hasModifiedProperties = m_ParticleSystemSerializedObject.hasModifiedProperties;

            // Check the system was not destroyed such as by an Undo operation.
            if (m_ParticleSystemSerializedObject.targetObject != null)
                m_ParticleSystemSerializedObject.ApplyModifiedProperties();
            if (hasModifiedProperties)
            {
                // Resimulate
                foreach (ParticleSystem ps in m_ParticleSystems)
                {
                    ParticleSystem root = ParticleSystemEditorUtils.GetRoot(ps);
                    if (!ParticleEffectUI.IsStopped(root) && ParticleSystemEditorUtils.editorResimulation)
                        ParticleSystemEditorUtils.PerformCompleteResimulation();
                }

                // Refresh procedural supported string
                UpdateParticleSystemInfoString();
            }
            if (m_RendererSerializedObject != null && m_RendererSerializedObject.targetObject != null)
                m_RendererSerializedObject.ApplyModifiedProperties();
        }

        void UpdateParticleSystemInfoString()
        {
            string supportsCullingText = "";
            foreach (var module in m_Modules)
            {
                if (module == null || !module.visibleUI || !module.enabled)
                    continue;

                module.UpdateCullingSupportedString(ref supportsCullingText);
            }

            if (supportsCullingText != string.Empty)
            {
                if (supportsCullingText != m_SupportsCullingText || m_SupportsCullingTextLabel == null)
                {
                    m_SupportsCullingText = supportsCullingText;
                    m_SupportsCullingTextLabel = "Automatic culling is disabled because: " + supportsCullingText.Replace("\n", "\n" + s_Texts.bulletPoint);
                }
            }
            else
            {
                m_SupportsCullingText = null;
                m_SupportsCullingTextLabel = null;
            }
        }

        public void UpdateProperties()
        {
            // Check the system was not destroyed such as by an Undo operation.
            if (m_ParticleSystemSerializedObject.targetObject != null)
                m_ParticleSystemSerializedObject.UpdateIfRequiredOrScript();
            if (m_RendererSerializedObject != null && m_RendererSerializedObject.targetObject != null)
                m_RendererSerializedObject.UpdateIfRequiredOrScript();
        }

        void ResetModules()
        {
            // Reset all
            foreach (var module in m_Modules)
                if (module != null)
                {
                    module.enabled = false;
                    if (!ParticleEffectUI.GetAllModulesVisible())
                        module.visibleUI = false;
                }

            // Default setup has a renderer
            if (m_Modules[m_Modules.Length - 1] == null)
                InitRendererUI();

            // Default setup has shape, emission and renderer
            int[] defaultEnabledModulesIndicies = { 1, 2, m_Modules.Length - 1 };
            for (int i = 0; i < defaultEnabledModulesIndicies.Length; ++i)
            {
                int moduleIndex = defaultEnabledModulesIndicies[i];
                if (m_Modules[moduleIndex] != null)
                {
                    m_Modules[moduleIndex].enabled = true;
                    m_Modules[moduleIndex].visibleUI = true;
                }
            }
        }

        void ShowAddModuleMenu()
        {
            // Now create the menu, add items and show it
            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < s_ModuleNames.Length; ++i)
            {
                if (m_Modules[i] == null || !m_Modules[i].visibleUI)
                    menu.AddItem(new GUIContent(s_ModuleNames[i]), false, AddModuleCallback, i);
                else
                    menu.AddDisabledItem(new GUIContent(s_ModuleNames[i]));
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Show All Modules"), ParticleEffectUI.GetAllModulesVisible(), AddModuleCallback, 10000);
            menu.ShowAsContext();
            Event.current.Use();
        }

        void AddModuleCallback(object obj)
        {
            int index = (int)obj;
            if (index >= 0 && index < m_Modules.Length)
            {
                if (index == m_Modules.Length - 1)
                {
                    InitRendererUI();
                }
                else
                {
                    m_Modules[index].enabled = true;
                    m_Modules[index].foldout = true;
                }
            }
            else
            {
                m_ParticleEffectUI.SetAllModulesVisible(!ParticleEffectUI.GetAllModulesVisible());
            }
            ApplyProperties();
        }

        void ModuleMenuCallback(object obj)
        {
            int moduleIndex = (int)obj;
            bool isRendererModule = (moduleIndex == m_Modules.Length - 1);
            if (isRendererModule)
            {
                ClearRenderers();
            }
            else
            {
                if (!ParticleEffectUI.GetAllModulesVisible())
                    m_Modules[moduleIndex].visibleUI = false;

                m_Modules[moduleIndex].enabled = false;
            }
        }

        void ShowModuleMenu(int moduleIndex)
        {
            // Now create the menu, add items and show it
            GenericMenu menu = new GenericMenu();

            if (!ParticleEffectUI.GetAllModulesVisible())
                menu.AddItem(new GUIContent("Remove"), false, ModuleMenuCallback, moduleIndex);
            else
                menu.AddDisabledItem(new GUIContent("Remove")); // Do not allow remove module when always show modules is enabled
            menu.ShowAsContext();
            Event.current.Use();
        }

        void EmitterMenuCallback(object obj)
        {
            int userData = (int)obj;
            switch (userData)
            {
                case 0:
                    m_ParticleEffectUI.CreateParticleSystem(m_ParticleSystems[0], SubModuleUI.SubEmitterType.None);
                    break;

                case 1:
                    ResetModules();
                    break;

                case 2:
                    EditorGUIUtility.PingObject(m_ParticleSystems[0]);
                    break;

                default:
                    System.Diagnostics.Debug.Assert("Enum not handled!".Length == 0);
                    break;
            }
        }

        void ShowEmitterMenu()
        {
            // Now create the menu, add items and show it
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Show Location"), false, EmitterMenuCallback, 2);
            menu.AddSeparator("");
            if (m_ParticleSystems[0].gameObject.activeInHierarchy)
                menu.AddItem(new GUIContent("Create Particle System"), false, EmitterMenuCallback, 0);
            else
                menu.AddDisabledItem(new GUIContent("Create new Particle System"));

            menu.AddItem(new GUIContent("Reset"), false, EmitterMenuCallback, 1);
            menu.ShowAsContext();
            Event.current.Use();
        }

        private static ModuleUI[] CreateUIModules(ParticleSystemUI e, SerializedObject so)
        {
            int index = 0;
            // Order should match GetUIModuleNames
            return new ModuleUI[] {
                new InitialModuleUI(e, so, s_ModuleNames[index++]),
                new EmissionModuleUI(e, so, s_ModuleNames[index++]),
                new ShapeModuleUI(e, so, s_ModuleNames[index++]),
                new VelocityModuleUI(e, so, s_ModuleNames[index++]),
                new ClampVelocityModuleUI(e, so, s_ModuleNames[index++]),
                new InheritVelocityModuleUI(e, so, s_ModuleNames[index++]),
                new ForceModuleUI(e, so, s_ModuleNames[index++]),
                new ColorModuleUI(e, so, s_ModuleNames[index++]),
                new ColorByVelocityModuleUI(e, so, s_ModuleNames[index++]),
                new SizeModuleUI(e, so, s_ModuleNames[index++]),
                new SizeByVelocityModuleUI(e, so, s_ModuleNames[index++]),
                new RotationModuleUI(e, so, s_ModuleNames[index++]),
                new RotationByVelocityModuleUI(e, so, s_ModuleNames[index++]),
                new ExternalForcesModuleUI(e, so, s_ModuleNames[index++]),
                new NoiseModuleUI(e, so, s_ModuleNames[index++]),
                new CollisionModuleUI(e, so, s_ModuleNames[index++]),
                new TriggerModuleUI(e, so, s_ModuleNames[index++]),
                new SubModuleUI(e, so, s_ModuleNames[index++]),
                new UVModuleUI(e, so, s_ModuleNames[index++]),
                new LightsModuleUI(e, so, s_ModuleNames[index++]),
                new TrailModuleUI(e, so, s_ModuleNames[index++]),
                new CustomDataModuleUI(e, so, s_ModuleNames[index++]),
                null, // RendererModule is created separately in InitRendererUI (it can be added/removed)
            };
        }

        // Names used when adding modules from a drop list
        public static string[] GetUIModuleNames()
        {
            // Order should match GetUIModules
            return new string[] {
                "",
                "Emission",
                "Shape",
                "Velocity over Lifetime",
                "Limit Velocity over Lifetime",
                "Inherit Velocity",
                "Force over Lifetime",
                "Color over Lifetime",
                "Color by Speed",
                "Size over Lifetime",
                "Size by Speed",
                "Rotation over Lifetime",
                "Rotation by Speed",
                "External Forces",
                "Noise",
                "Collision",
                "Triggers",
                "Sub Emitters",
                "Texture Sheet Animation",
                "Lights",
                "Trails",
                "Custom Data",
                "Renderer"
            };
        }
    } // class ParticleSystemUI
} // namespace UnityEditor
