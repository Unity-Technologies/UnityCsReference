// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor.Audio;

namespace UnityEditor
{
    class AudioMixerEffectView
    {
        private const float kMinPitch        =  0.01f;
        private const float kMaxPitch        = 10.0f;

        private const int kLabelWidth = 170;
        private const int kTextboxWidth = 70;

        private AudioMixerGroupController m_PrevGroup = null;
        private readonly EffectDragging m_EffectDragging;
        private int m_LastNumChannels = 0;

        private AudioMixerEffectPlugin m_SharedPlugin = new AudioMixerEffectPlugin();
        private Dictionary<string, IAudioEffectPluginGUI> m_CustomEffectGUIs = new Dictionary<string, IAudioEffectPluginGUI>();

        static class Texts
        {
            public static GUIContent editInPlaymode = new GUIContent("Edit in Playmode");
            public static GUIContent pitch = new GUIContent("Pitch");
            public static GUIContent addEffect = new GUIContent("Add Effect");
            public static GUIContent volume = new GUIContent("Volume");
            public static GUIContent sendLevel = new GUIContent("Send level");
            public static GUIContent bus = new GUIContent("Receive");
            public static GUIContent none = new GUIContent("None");
            public static GUIContent wet = new GUIContent("Wet", "Enables/disables wet/dry ratio on this effect. Note that this makes the DSP graph more complex and requires additional CPU and memory, so use it only when necessary.");
            public static string dB = "dB";
            public static string percentage = "%";
            public static string cpuFormatString = " - CPU: {0:#0.00}%";
        }

        public AudioMixerEffectView()
        {
            m_EffectDragging = new EffectDragging();

            Type pluginType = typeof(IAudioEffectPluginGUI);
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                //Debug.Log("Assembly: " + assembly.FullName);
                try
                {
                    var types = assembly.GetTypes();
                    //Debug.Log("Contained types: " + types.ToString());
                    foreach (Type t in types.Where(t => !t.IsAbstract && pluginType.IsAssignableFrom(t)))
                    {
                        //Debug.Log("Instantiating type: " + t.FullName);
                        RegisterCustomGUI(Activator.CreateInstance(t) as IAudioEffectPluginGUI);
                    }
                }
                catch (System.Exception)
                {
                    //Debug.Log("Failed getting types in assembly: " + assembly.FullName);
                }
            }
        }

        public bool RegisterCustomGUI(IAudioEffectPluginGUI gui)
        {
            string name = gui.Name;
            if (m_CustomEffectGUIs.ContainsKey(name))
            {
                var oldGUI = m_CustomEffectGUIs[name];
                Debug.LogError("Attempt to register custom GUI for plugin " + name + " failed as another plugin is already registered under this name.");
                Debug.LogError("Plugin trying to register itself: " + gui.Description + " (Vendor: " + gui.Vendor + ")");
                Debug.LogError("Plugin already registered: " + oldGUI.Description + " (Vendor: " + oldGUI.Vendor + ")");
                return false;
            }

            m_CustomEffectGUIs[name] = gui;
            return true;
        }

        public void OnGUI(AudioMixerGroupController group)
        {
            if (group == null)
                return;

            var controller = group.controller;
            var allGroups = controller.GetAllAudioGroupsSlow();
            var effectMap = new Dictionary<AudioMixerEffectController, AudioMixerGroupController>();
            foreach (var g in allGroups)
                foreach (var e in g.effects)
                    effectMap[e] = g;

            Rect totalRect = EditorGUILayout.BeginVertical();

            if (EditorApplication.isPlaying)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUI.BeginChangeCheck();
                GUILayout.Toggle(AudioSettings.editingInPlaymode, Texts.editInPlaymode, EditorStyles.miniButton, GUILayout.Width(120));
                if (EditorGUI.EndChangeCheck())
                    AudioSettings.editingInPlaymode = !AudioSettings.editingInPlaymode;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            using (new EditorGUI.DisabledScope(!AudioMixerController.EditingTargetSnapshot()))
            {
                if (group != m_PrevGroup)
                {
                    m_PrevGroup = group;
                    controller.m_HighlightEffectIndex = -1;
                    AudioMixerUtility.RepaintAudioMixerAndInspectors();
                }

                // Do Effect modules
                DoInitialModule(group, controller, allGroups);
                for (int effectIndex = 0; effectIndex < group.effects.Length; effectIndex++)
                {
                    DoEffectGUI(effectIndex, group, allGroups, effectMap, ref controller.m_HighlightEffectIndex);
                }

                m_EffectDragging.HandleDragging(totalRect, group, controller);

                GUILayout.Space(10f);

                EditorGUILayout.BeginHorizontal();

                GUILayout.FlexibleSpace();
                if (EditorGUILayout.DropdownButton(Texts.addEffect, FocusType.Passive, GUISkin.current.button))
                {
                    GenericMenu pm = new GenericMenu();
                    Rect buttonRect = GUILayoutUtility.topLevel.GetLast();
                    AudioMixerGroupController[] groupArray = new AudioMixerGroupController[] { group };
                    AudioMixerChannelStripView.AddEffectItemsToMenu(controller, groupArray, group.effects.Length, string.Empty, pm);
                    pm.DropDown(buttonRect);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        public static float DoInitialModule(AudioMixerGroupController group, AudioMixerController controller, List<AudioMixerGroupController> allGroups)
        {
            Rect totalRect = EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

            // Pitch
            float value = group.GetValueForPitch(controller, controller.TargetSnapshot);
            if (AudioMixerEffectGUI.Slider(Texts.pitch, ref value, 100.0f, 1.0f, Texts.percentage, kMinPitch, kMaxPitch, controller, new AudioGroupParameterPath(group, group.GetGUIDForPitch())))
            {
                Undo.RecordObject(controller.TargetSnapshot, "Change Pitch");
                group.SetValueForPitch(controller, controller.TargetSnapshot, value);
            }

            GUILayout.Space(5f);
            EditorGUILayout.EndVertical();

            AudioMixerDrawUtils.DrawSplitter();

            return totalRect.height;
        }

        public void DoEffectGUI(int effectIndex, AudioMixerGroupController group, List<AudioMixerGroupController> allGroups, Dictionary<AudioMixerEffectController, AudioMixerGroupController> effectMap, ref int highlightEffectIndex)
        {
            Event evt = Event.current;

            AudioMixerController controller = group.controller;
            AudioMixerEffectController effect = group.effects[effectIndex];
            MixerParameterDefinition[] paramDefs = MixerEffectDefinitions.GetEffectParameters(effect.effectName);

            // This rect is valid after every layout event
            Rect totalRect = EditorGUILayout.BeginVertical();

            bool hovering = totalRect.Contains(evt.mousePosition);
            EventType evtType = evt.GetTypeForControl(m_EffectDragging.dragControlID);

            if (evtType == EventType.MouseMove && hovering && highlightEffectIndex != effectIndex)
            {
                highlightEffectIndex = effectIndex;
                AudioMixerUtility.RepaintAudioMixerAndInspectors();
            }

            // Header
            const float colorCodeWidth = 6f;
            var gearSize = EditorStyles.iconButton.CalcSize(EditorGUI.GUIContents.titleSettingsIcon);
            Rect headerRect = GUILayoutUtility.GetRect(1, 17f);
            Rect colorCodeRect = new Rect(headerRect.x + 6f, headerRect.y + 5f, colorCodeWidth, colorCodeWidth);
            Rect labelRect = new Rect(headerRect.x + 8f + colorCodeWidth, headerRect.y, headerRect.width - 8f - colorCodeWidth - gearSize.x - 5f, headerRect.height);
            Rect gearRect = new Rect(labelRect.xMax, headerRect.y, gearSize.x, gearSize.y);
            Rect dragRect = new Rect(headerRect.x, headerRect.y, headerRect.width - gearSize.x - 5f, headerRect.height);
            {
                bool showCPU = EditorPrefs.GetBool(AudioMixerGroupEditor.kPrefKeyForShowCpuUsage, false) && EditorUtility.audioProfilingEnabled;

                float val = EditorGUIUtility.isProSkin ? 0.1f : 1.0f;
                Color headerColor =  new Color(val, val, val, 0.2f);
                Color origColor = GUI.color;
                GUI.color = headerColor;
                GUI.DrawTexture(headerRect, EditorGUIUtility.whiteTexture);
                GUI.color = origColor;

                Color effectColorCode = AudioMixerDrawUtils.GetEffectColor(effect);
                EditorGUI.DrawRect(colorCodeRect, effectColorCode);
                GUI.Label(labelRect, showCPU ? effect.effectName + string.Format(Texts.cpuFormatString, effect.GetCPUUsage(controller)) : effect.effectName, EditorStyles.boldLabel);
                if (EditorGUI.DropdownButton(gearRect, EditorGUI.GUIContents.titleSettingsIcon, FocusType.Passive, EditorStyles.iconButton))
                {
                    ShowEffectContextMenu(group, effect, effectIndex, controller, gearRect);
                }

                // Show context menu if right clicking in header rect (for convenience)
                if (evt.type == EventType.ContextClick && headerRect.Contains(evt.mousePosition))
                {
                    ShowEffectContextMenu(group, effect, effectIndex, controller, new Rect(evt.mousePosition.x, headerRect.y, 1, headerRect.height));
                    evt.Use();
                }

                if (evtType == EventType.Repaint)
                    EditorGUIUtility.AddCursorRect(dragRect, MouseCursor.ResizeVertical, m_EffectDragging.dragControlID);
            }

            using (new EditorGUI.DisabledScope(effect.bypass || group.bypassEffects))
            {
                EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

                if (effect.IsAttenuation())
                {
                    EditorGUILayout.BeginVertical();
                    float value = group.GetValueForVolume(controller, controller.TargetSnapshot);
                    if (AudioMixerEffectGUI.Slider(Texts.volume, ref value, 1.0f, 1.0f, Texts.dB, AudioMixerController.kMinVolume, AudioMixerController.GetMaxVolume(), controller, new AudioGroupParameterPath(group, group.GetGUIDForVolume())))
                    {
                        Undo.RecordObject(controller.TargetSnapshot, "Change Volume Fader");
                        group.SetValueForVolume(controller, controller.TargetSnapshot, value);
                        AudioMixerUtility.RepaintAudioMixerAndInspectors();
                    }

                    //FIXME
                    // 1) The VUMeter used is not in the same style that fits the rest of the Audio UI
                    // 2) The layout of the VU meters is hacked together and a lot of magic numbers are used.
                    int numChannels = 0;
                    float[] vuinfo_level = new float[9];
                    float[] vuinfo_peak = new float[9];

                    numChannels = group.controller.GetGroupVUInfo(group.groupID, true, ref vuinfo_level, ref vuinfo_peak);

                    if (evt.type == EventType.Layout)
                    {
                        m_LastNumChannels = numChannels;
                    }
                    else
                    {
                        if (numChannels != m_LastNumChannels)
                            HandleUtility.Repaint();      // Repaint to ensure correct rendered num channels

                        // Ensure same num channels as in layout event to not break IMGUI controlID handling
                        numChannels = m_LastNumChannels;
                    }

                    GUILayout.Space(4f);
                    for (int c = 0; c < numChannels; ++c)
                    {
                        float level = 1 - AudioMixerController.VolumeToScreenMapping(Mathf.Clamp(vuinfo_level[c], AudioMixerController.kMinVolume, AudioMixerController.GetMaxVolume()), 1, true);
                        float peak = 1 - AudioMixerController.VolumeToScreenMapping(Mathf.Clamp(vuinfo_peak[c], AudioMixerController.kMinVolume, AudioMixerController.GetMaxVolume()), 1, true);
                        EditorGUILayout.VUMeterHorizontal(level, peak, GUILayout.Height(10));

                        //This allows the meters to drop to 0 after PlayMode has stopped.
                        if (!EditorApplication.isPlaying && peak > 0.0F)
                            AudioMixerUtility.RepaintAudioMixerAndInspectors();
                    }
                    GUILayout.Space(4f);
                    EditorGUILayout.EndVertical();
                }

                if (effect.IsSend())
                {
                    Rect buttonRect;
                    GUIContent buttonContent = (effect.sendTarget == null) ? Texts.none : GUIContent.Temp(effect.GetSendTargetDisplayString(effectMap));
                    if (AudioMixerEffectGUI.PopupButton(Texts.bus, buttonContent, EditorStyles.popup, out buttonRect))
                        ShowBusPopupMenu(effectIndex, @group, allGroups, effectMap, effect, buttonRect);

                    if (effect.sendTarget != null)
                    {
                        float wetLevel = effect.GetValueForMixLevel(controller, controller.TargetSnapshot);
                        if (AudioMixerEffectGUI.Slider(Texts.sendLevel, ref wetLevel, 1.0f, 1.0f, Texts.dB, AudioMixerController.kMinVolume, AudioMixerController.kMaxEffect, controller, new AudioEffectParameterPath(group, effect, effect.GetGUIDForMixLevel())))
                        {
                            Undo.RecordObject(controller.TargetSnapshot, "Change Send Level");
                            effect.SetValueForMixLevel(controller, controller.TargetSnapshot, wetLevel);
                            AudioMixerUtility.RepaintAudioMixerAndInspectors();
                        }
                    }
                }

                if (MixerEffectDefinitions.EffectCanBeSidechainTarget(effect))
                {
                    bool anyTargetsFound = false;
                    foreach (var g in allGroups)
                    {
                        foreach (var e in g.effects)
                        {
                            if (e.IsSend() && e.sendTarget == effect)
                            {
                                anyTargetsFound = true;
                                break;
                            }
                        }
                        if (anyTargetsFound)
                            break;
                    }
                    if (!anyTargetsFound)
                    {
                        GUILayout.Label(new GUIContent("No Send sources connected.", EditorGUIUtility.warningIcon));
                    }
                }

                // Wet mix
                if (effect.enableWetMix && !effect.IsReceive() && !effect.IsDuckVolume() && !effect.IsAttenuation() && !effect.IsSend())
                {
                    float wetLevel = effect.GetValueForMixLevel(controller, controller.TargetSnapshot);
                    if (AudioMixerEffectGUI.Slider(Texts.wet, ref wetLevel, 1.0f, 1.0f, Texts.dB, AudioMixerController.kMinVolume, AudioMixerController.kMaxEffect, controller, new AudioEffectParameterPath(group, effect, effect.GetGUIDForMixLevel())))
                    {
                        Undo.RecordObject(controller.TargetSnapshot, "Change Mix Level");
                        effect.SetValueForMixLevel(controller, controller.TargetSnapshot, wetLevel);
                        AudioMixerUtility.RepaintAudioMixerAndInspectors();
                    }
                }

                // All other effects
                bool drawDefaultGUI = true;
                if (m_CustomEffectGUIs.ContainsKey(effect.effectName))
                {
                    var customGUI = m_CustomEffectGUIs[effect.effectName];
                    m_SharedPlugin.m_Controller = controller;
                    m_SharedPlugin.m_Effect = effect;
                    m_SharedPlugin.m_ParamDefs = paramDefs;
                    drawDefaultGUI = customGUI.OnGUI(m_SharedPlugin);
                }
                if (drawDefaultGUI)
                {
                    foreach (var p in paramDefs)
                    {
                        float value = effect.GetValueForParameter(controller, controller.TargetSnapshot, p.name);
                        if (AudioMixerEffectGUI.Slider(GUIContent.Temp(p.name, p.description), ref value, p.displayScale, p.displayExponent, p.units, p.minRange, p.maxRange, controller, new AudioEffectParameterPath(group, effect, effect.GetGUIDForParameter(p.name))))
                        {
                            Undo.RecordObject(controller.TargetSnapshot, "Change " + p.name);
                            effect.SetValueForParameter(controller, controller.TargetSnapshot, p.name, value);
                        }
                    }
                    if (paramDefs.Length > 0)
                        GUILayout.Space(6f);
                }
            }

            m_EffectDragging.HandleDragElement(effectIndex, totalRect, dragRect, group, allGroups);

            EditorGUILayout.EndVertical(); // indented effect contents
            EditorGUILayout.EndVertical(); // calc total size

            AudioMixerDrawUtils.DrawSplitter();
        }

        private static void ShowEffectContextMenu(AudioMixerGroupController group, AudioMixerEffectController effect, int effectIndex, AudioMixerController controller, Rect buttonRect)
        {
            GenericMenu menu = new GenericMenu();

            if (!effect.IsReceive())
            {
                if (!effect.IsAttenuation() && !effect.IsSend() && !effect.IsDuckVolume())
                {
                    menu.AddItem(new GUIContent("Allow Wet Mixing (causes higher memory usage)"), effect.enableWetMix, delegate()
                        {
                            Undo.RecordObject(effect, "Enable Wet Mixing");
                            effect.enableWetMix = !effect.enableWetMix;
                        });
                    menu.AddItem(new GUIContent("Bypass"), effect.bypass, delegate()
                        {
                            Undo.RecordObject(effect, "Bypass Effect");
                            effect.bypass = !effect.bypass;

                            controller.UpdateBypass();
                            AudioMixerUtility.RepaintAudioMixerAndInspectors();
                        });
                    menu.AddSeparator("");
                }

                menu.AddItem(new GUIContent("Copy effect settings to all snapshots"), false, delegate()
                    {
                        Undo.RecordObject(controller, "Copy effect settings to all snapshots");
                        if (effect.IsAttenuation())
                            controller.CopyAttenuationToAllSnapshots(group, controller.TargetSnapshot);
                        else
                            controller.CopyEffectSettingsToAllSnapshots(group, effectIndex, controller.TargetSnapshot, effect.IsSend());
                        AudioMixerUtility.RepaintAudioMixerAndInspectors();
                    });

                if (!effect.IsAttenuation() && !effect.IsSend() && !effect.IsDuckVolume() && effect.enableWetMix)
                {
                    menu.AddItem(new GUIContent("Copy effect settings to all snapshots, including wet level"), false, delegate()
                        {
                            Undo.RecordObject(controller, "Copy effect settings to all snapshots, including wet level");
                            controller.CopyEffectSettingsToAllSnapshots(group, effectIndex, controller.TargetSnapshot, true);
                            AudioMixerUtility.RepaintAudioMixerAndInspectors();
                        });
                }

                menu.AddSeparator("");
            }

            AudioMixerGroupController[] groupArray = new AudioMixerGroupController[] { group };
            AudioMixerChannelStripView.AddEffectItemsToMenu(controller, groupArray, effectIndex, "Add effect before/", menu);
            AudioMixerChannelStripView.AddEffectItemsToMenu(controller, groupArray, effectIndex + 1, "Add effect after/", menu);

            if (!effect.IsAttenuation())
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Remove this effect"), false, delegate()
                    {
                        controller.ClearSendConnectionsTo(effect);
                        controller.RemoveEffect(effect, group);
                        AudioMixerUtility.RepaintAudioMixerAndInspectors();
                    });
            }

            menu.DropDown(buttonRect);
        }

        private static void ShowBusPopupMenu(int effectIndex, AudioMixerGroupController group, List<AudioMixerGroupController> allGroups, Dictionary<AudioMixerEffectController, AudioMixerGroupController> effectMap, AudioMixerEffectController effect, Rect buttonRect)
        {
            GenericMenu pm = new GenericMenu();
            pm.AddItem(new GUIContent("None"), false, AudioMixerChannelStripView.ConnectSendPopupCallback, new AudioMixerChannelStripView.ConnectSendContext(effect, null));
            pm.AddSeparator("");
            AudioMixerChannelStripView.AddMenuItemsForReturns(pm, string.Empty, effectIndex, group, allGroups, effectMap, effect, true);

            if (pm.GetItemCount() == 2)
            {
                pm.AddDisabledItem(new GUIContent("No valid Receive targets found"));
            }

            pm.DropDown(buttonRect);
        }

        // -----------------------------------
        // Effect dragging logic and rendering
        // -----------------------------------
        class EffectDragging
        {
            public EffectDragging()
            {
                m_DragControlID = GUIUtility.GetPermanentControlID();
            }

            public bool IsDraggingIndex(int effectIndex)
            {
                return m_MovingSrcIndex == effectIndex && GUIUtility.hotControl == m_DragControlID;
            }

            public int dragControlID
            {
                get { return m_DragControlID; }
            }

            private bool isDragging
            {
                get { return m_MovingSrcIndex != -1 && GUIUtility.hotControl == m_DragControlID; }
            }

            private readonly Color kMoveColorBorderAllowed = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            private readonly Color kMoveColorHiAllowed = new Color(1.0f, 1.0f, 1.0f, 0.3f);
            private readonly Color kMoveColorLoAllowed = new Color(1.0f, 1.0f, 1.0f, 0.0f);
            private readonly Color kMoveColorBorderDisallowed = new Color(0.8f, 0.0f, 0.0f, 1.0f);
            private readonly Color kMoveColorHiDisallowed = new Color(1.0f, 0.0f, 0.0f, 0.3f);
            private readonly Color kMoveColorLoDisallowed = new Color(1.0f, 0.0f, 0.0f, 0.0f);

            private readonly int m_DragControlID = 0;
            private int m_MovingSrcIndex = -1;
            private int m_MovingDstIndex = -1;
            private bool m_MovingEffectAllowed = false;
            private float m_MovingPos = 0;
            private Rect m_MovingRect = new Rect(0, 0, 0, 0);
            private float m_DragHighlightPos = -1.0f;
            private float m_DragHighlightHeight = 2f;

            // Called per effect
            public void HandleDragElement(int effectIndex, Rect effectRect, Rect dragRect, AudioMixerGroupController group, List<AudioMixerGroupController> allGroups)
            {
                Event evt = Event.current;

                switch (evt.GetTypeForControl(m_DragControlID))
                {
                    case EventType.MouseDown:
                        if (evt.button == 0 && dragRect.Contains(evt.mousePosition) && GUIUtility.hotControl == 0)
                        {
                            m_MovingSrcIndex = effectIndex;
                            m_MovingPos = evt.mousePosition.y;
                            m_MovingRect = new Rect(effectRect.x, effectRect.y - m_MovingPos, effectRect.width, effectRect.height);
                            GUIUtility.hotControl = m_DragControlID;
                            EditorGUIUtility.SetWantsMouseJumping(1);
                            evt.Use();
                        }
                        break;

                    case EventType.Repaint:
                        if (effectIndex == m_MovingSrcIndex)
                        {
                            using (new EditorGUI.DisabledScope(true))
                            {
                                AudioMixerDrawUtils.styles.channelStripAreaBackground.Draw(effectRect, false, false, false, false);
                            }
                        }
                        break;
                }

                if (isDragging)
                {
                    float h2 = effectRect.height * 0.5f;
                    float dy = evt.mousePosition.y - effectRect.y - h2;

                    if (Mathf.Abs(dy) <= h2)
                    {
                        int newMovingDstIndex = (dy < 0.0f) ? effectIndex : (effectIndex + 1);
                        if (newMovingDstIndex != m_MovingDstIndex)
                        {
                            m_DragHighlightPos = (dy < 0.0f) ? effectRect.y : (effectRect.y + effectRect.height);
                            m_MovingDstIndex = newMovingDstIndex;
                            m_MovingEffectAllowed = !AudioMixerController.WillMovingEffectCauseFeedback(allGroups, group, m_MovingSrcIndex, group, newMovingDstIndex, null);
                        }
                    }

                    // Do not draw drag highlight line for positions where no change will happen
                    if (m_MovingDstIndex == m_MovingSrcIndex || m_MovingDstIndex == m_MovingSrcIndex + 1)
                    {
                        m_DragHighlightPos = 0;
                    }
                }
            }

            // Called once per OnGUI
            public void HandleDragging(Rect totalRect, AudioMixerGroupController group, AudioMixerController controller)
            {
                // Early out if we are not dragging
                if (!isDragging)
                    return;

                Event evt = Event.current;
                const float kMoveRange = 15;

                switch (evt.GetTypeForControl(m_DragControlID))
                {
                    case EventType.MouseDrag:
                        m_MovingPos = evt.mousePosition.y;
                        evt.Use();
                        break;

                    case EventType.MouseUp:
                        evt.Use();
                        if (m_MovingSrcIndex != -1)
                        {
                            if (m_MovingDstIndex != -1 && m_MovingEffectAllowed)
                            {
                                var effects = group.effects.ToList();
                                if (AudioMixerController.MoveEffect(ref effects, m_MovingSrcIndex, ref effects, m_MovingDstIndex))
                                    group.effects = effects.ToArray();
                            }
                            m_MovingSrcIndex = -1;
                            m_MovingDstIndex = -1;
                            controller.m_HighlightEffectIndex = -1;
                            if (GUIUtility.hotControl == m_DragControlID)
                                GUIUtility.hotControl = 0;
                            EditorGUIUtility.SetWantsMouseJumping(0);
                            AudioMixerUtility.RepaintAudioMixerAndInspectors();
                            EditorGUIUtility.ExitGUI(); // Exit because we changed order of effects
                        }
                        break;

                    case EventType.Repaint:
                        if (m_DragHighlightPos > 0.0f)
                        {
                            float w = totalRect.width;
                            Color moveColorLo = (m_MovingEffectAllowed) ? kMoveColorLoAllowed : kMoveColorLoDisallowed;
                            Color moveColorHi = (m_MovingEffectAllowed) ? kMoveColorHiAllowed : kMoveColorHiDisallowed;
                            Color moveColorBorder = (m_MovingEffectAllowed) ? kMoveColorBorderAllowed : kMoveColorBorderDisallowed;
                            AudioMixerDrawUtils.DrawGradientRect(new Rect(m_MovingRect.x, m_DragHighlightPos - kMoveRange, w, kMoveRange), moveColorLo, moveColorHi);
                            AudioMixerDrawUtils.DrawGradientRect(new Rect(m_MovingRect.x, m_DragHighlightPos, w, kMoveRange), moveColorHi, moveColorLo);
                            AudioMixerDrawUtils.DrawGradientRect(new Rect(m_MovingRect.x, m_DragHighlightPos - m_DragHighlightHeight / 2, w, m_DragHighlightHeight), moveColorBorder, moveColorBorder);
                        }
                        break;
                }
            }
        }
    }
}
