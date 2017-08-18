// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditorInternal;
using UnityEditor.Audio;

namespace UnityEditor
{
    internal class AudioMixerChannelStripView
    {
        [Serializable]
        public class State
        {
            public int m_LastClickedInstanceID = 0;
            public Vector2 m_ScrollPos = new Vector2(0, 0);
        }

        public static float kVolumeScaleMouseDrag = 1.0f;
        public static float kEffectScaleMouseDrag = 0.3f; // higher precision for effect slot editing

        private static Color kMoveColorHighlight = new Color(0.3f, 0.6f, 1.0f, 0.4f);
        private static Color kMoveSlotColHiAllowed = new Color(1f, 1f, 1f, 0.7f); //new Color(59 / 255f, 162 / 255f, 216 / 255f, 0.7f);
        private static Color kMoveSlotColLoAllowed = new Color(1f, 1f, 1f, 0.0f);
        private static Color kMoveSlotColBorderAllowed = new Color(1f, 1f, 1f, 1.0f);
        private static Color kMoveSlotColHiDisallowed = new Color(1.0f, 0.0f, 0.0f, 0.7f);
        private static Color kMoveSlotColLoDisallowed = new Color(0.8f, 0.0f, 0.0f, 0.0f);
        private static Color kMoveSlotColBorderDisallowed = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        private static int kRectSelectionHashCode = "RectSelection".GetHashCode();
        private static int kEffectDraggingHashCode = "EffectDragging".GetHashCode();
        private static int kVerticalFaderHash = "VerticalFader".GetHashCode();

        public int m_FocusIndex = -1;
        public int m_IndexCounter = 0;
        public int m_EffectInteractionControlID;
        public int m_RectSelectionControlID;
        public float m_MouseDragStartX = 0.0f;
        public float m_MouseDragStartY = 0.0f;
        public float m_MouseDragStartValue = 0.0f;
        public Vector2 m_RectSelectionStartPos = new Vector2(0, 0);
        public Rect m_RectSelectionRect = new Rect(0, 0, 0, 0);

        State m_State;
        private AudioMixerController m_Controller;
        private MixerGroupControllerCompareByName m_GroupComparer = new MixerGroupControllerCompareByName();
        private bool m_WaitingForDragEvent = false;
        private int m_ChangingWetMixIndex = -1;
        private int m_MovingEffectSrcIndex = -1;
        private int m_MovingEffectDstIndex = -1;
        private Rect m_MovingSrcRect = new Rect(-1, -1, 0, 0);
        private Rect m_MovingDstRect = new Rect(-1, -1, 0, 0);
        private bool m_MovingEffectAllowed = false;
        private AudioMixerGroupController m_MovingSrcGroup = null;
        private AudioMixerGroupController m_MovingDstGroup = null;
        private const float k_MinVULevel = -80f;
        private List<int> m_LastNumChannels = new List<int>();

        private bool m_RequiresRepaint = false;
        public bool requiresRepaint
        {
            get
            {
                if (m_RequiresRepaint)
                {
                    m_RequiresRepaint = false;
                    return true;
                }
                return false;
            }
        }

        const float headerHeight = 22f;
        const float vuHeight = 170f;
        const float dbHeight = 17f;
        const float soloMuteBypassHeight = 30f;
        const float effectHeight = 16f;
        const float spaceBetween = 0;
        const int channelStripSpacing = 4;
        const float channelStripBaseWidth = 90f;
        const float spaceBetweenMainGroupsAndReferenced = 50f;
        readonly Vector2 channelStripsOffset = new Vector2(15, 10);

        // For background ui
        static Texture2D m_GridTexture;
        private const float kGridTileWidth = 12.0f;
        private static readonly Color kGridColorDark = new Color(0f, 0f, 0f, 0.18f);
        private static readonly Color kGridColorLight = new Color(0f, 0f, 0f, 0.10f);
        private static Color gridColor
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                    return kGridColorDark;
                else
                    return kGridColorLight;
            }
        }

        private AudioMixerDrawUtils.Styles styles
        {
            get { return AudioMixerDrawUtils.styles; }
        }

        public AudioMixerChannelStripView(AudioMixerChannelStripView.State state)
        {
            m_State = state;
        }

        static Texture2D CreateTilableGridTexture(int width, int height, Color backgroundColor, Color lineColor)
        {
            Color[] pixels = new Color[width * height];

            // background
            for (int i = 0; i < height * width; i++)
                pixels[i] = backgroundColor;

            // right edge
            for (int i = 0; i < height; i++)
                pixels[i * width + (width - 1)] = lineColor;

            // bottom edge
            for (int i = 0; i < width; i++)
                pixels[(height - 1) * width + i] = lineColor;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        Texture2D gridTextureTilable
        {
            get
            {
                if (m_GridTexture == null)
                    m_GridTexture = CreateTilableGridTexture((int)kGridTileWidth, (int)kGridTileWidth, new Color(0, 0, 0, 0), gridColor);
                return m_GridTexture;
            }
        }

        void DrawAreaBackground(Rect rect)
        {
            if (Event.current.type == EventType.Repaint)
            {
                // Draw background
                Color prevColor = GUI.color;
                GUI.color = new Color(1, 1, 1, EditorGUIUtility.isProSkin ? 0.6f : 0.2f);
                AudioMixerDrawUtils.styles.channelStripAreaBackground.Draw(rect, false, false, false, false);
                GUI.color = prevColor;
                // Draw grid
                //GUI.DrawTextureWithTexCoords (rect, gridTextureTilable, new Rect(0, 0, rect.width / gridTextureTilable.width, rect.height / gridTextureTilable.height), true);
            }
        }

        private void SetFocus() { m_FocusIndex = m_IndexCounter; }
        private void ClearFocus() { m_FocusIndex = -1; }
        private bool HasFocus() { return m_FocusIndex == m_IndexCounter; }
        private bool IsFocusActive() { return m_FocusIndex != -1; }

        public class EffectContext
        {
            public EffectContext(AudioMixerController controller, AudioMixerGroupController[] groups, int index, string name)
            {
                this.controller = controller;
                this.groups = groups;
                this.index = index;
                this.name = name;
            }

            public AudioMixerController controller;
            public AudioMixerGroupController[] groups;
            public int index;
            public string name;
        }

        public static void InsertEffectPopupCallback(object obj)
        {
            EffectContext context = (EffectContext)obj;

            foreach (AudioMixerGroupController group in context.groups)
            {
                Undo.RecordObject(group, "Add effect");
                AudioMixerEffectController newEffect = new AudioMixerEffectController(context.name);

                int index = (context.index == -1 || context.index > group.effects.Length) ? group.effects.Length : context.index;
                group.InsertEffect(newEffect, index);
                AssetDatabase.AddObjectToAsset(newEffect, context.controller);
                newEffect.PreallocateGUIDs();
            }

            AudioMixerUtility.RepaintAudioMixerAndInspectors();
        }

        public void RemoveEffectPopupCallback(object obj)
        {
            EffectContext context = (EffectContext)obj;

            foreach (AudioMixerGroupController group in context.groups)
            {
                if (context.index >= group.effects.Length)
                    continue;
                AudioMixerEffectController effect = group.effects[context.index];
                context.controller.ClearSendConnectionsTo(effect);
                context.controller.RemoveEffect(effect, group);
            }

            AudioMixerUtility.RepaintAudioMixerAndInspectors();
        }

        public class ConnectSendContext
        {
            public ConnectSendContext(AudioMixerEffectController sendEffect, AudioMixerEffectController targetEffect)
            {
                this.sendEffect = sendEffect;
                this.targetEffect = targetEffect;
            }

            public AudioMixerEffectController sendEffect;
            public AudioMixerEffectController targetEffect;
        }

        public static void ConnectSendPopupCallback(object obj)
        {
            ConnectSendContext context = (ConnectSendContext)obj;
            Undo.RecordObject(context.sendEffect, "Change Send Target");
            context.sendEffect.sendTarget = context.targetEffect;
        }

        private bool ClipRect(Rect r, Rect clipRect, ref Rect overlap)
        {
            overlap.x = Mathf.Max(r.x, clipRect.x);
            overlap.y = Mathf.Max(r.y, clipRect.y);
            overlap.width = Mathf.Min(r.x + r.width, clipRect.x + clipRect.width) - overlap.x;
            overlap.height = Mathf.Min(r.y + r.height, clipRect.y + clipRect.height) - overlap.y;
            return overlap.width > 0.0f && overlap.height > 0.0f;
        }

        public float VerticalFader(Rect r, float value, int direction, float dragScale, bool drawScaleValues, bool drawMarkerValue, string tooltip, float maxValue, GUIStyle style)
        {
            Event evt = Event.current;
            int handleHeight = (int)style.fixedHeight;
            int faderScreenRange = (int)r.height - handleHeight;
            float valueScreenPos = AudioMixerController.VolumeToScreenMapping(Mathf.Clamp(value, AudioMixerController.kMinVolume, maxValue), faderScreenRange, true);
            Rect handleRect = new Rect(r.x, r.y + (int)valueScreenPos, r.width, handleHeight);

            int controlID = GUIUtility.GetControlID(kVerticalFaderHash, FocusType.Passive);

            switch (evt.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    if (r.Contains(evt.mousePosition) && GUIUtility.hotControl == 0)
                    {
                        m_MouseDragStartY = evt.mousePosition.y;
                        m_MouseDragStartValue = valueScreenPos;
                        GUIUtility.hotControl = controlID;
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        valueScreenPos = Mathf.Clamp(m_MouseDragStartValue + dragScale * (evt.mousePosition.y - m_MouseDragStartY), 0.0f, faderScreenRange);
                        value = Mathf.Clamp(AudioMixerController.VolumeToScreenMapping(valueScreenPos, faderScreenRange, false), AudioMixerController.kMinVolume, maxValue);
                        evt.Use();
                    }
                    break;

                case EventType.Repaint:

                    // Ticks and Numbers
                    if (drawScaleValues)
                    {
                        float tickStartY = r.y + handleHeight / 2f;
                        float level = maxValue;
                        using (new EditorGUI.DisabledScope(true))
                        {
                            while (level >= AudioMixerController.kMinVolume)
                            {
                                float y = AudioMixerController.VolumeToScreenMapping(level, faderScreenRange, true);

                                if (level / 10 % 2 == 0)
                                    GUI.Label(new Rect(r.x, tickStartY + y - 7, r.width, 14), GUIContent.Temp(Mathf.RoundToInt(level).ToString()), styles.vuValue);
                                EditorGUI.DrawRect(new Rect(r.x, tickStartY + y - 1, 5f, 1), new Color(0, 0, 0, 0.5f));
                                level -= 10.0f;
                            }
                        }
                    }

                    // Drag handle
                    if (drawMarkerValue)
                        style.Draw(handleRect, GUIContent.Temp(Mathf.RoundToInt(value).ToString()), false, false, false, false);
                    else
                        style.Draw(handleRect, false, false, false, false);
                    AudioMixerDrawUtils.AddTooltipOverlay(handleRect, tooltip);
                    break;
            }

            return value;
        }

        private static Color hfaderCol1 = new Color(0.2f, 0.2f, 0.2f, 1.0f);
        private static Color hfaderCol2 = new Color(0.4f, 0.4f, 0.4f, 1.0f);

        public float HorizontalFader(Rect r, float value, float minValue, float maxValue, int direction, float dragScale)
        {
            m_IndexCounter++;
            Rect r2 = new Rect(r);
            float handleWidth = r.width * 0.2f, faderScreenRange = r2.width - handleWidth;
            AudioMixerDrawUtils.DrawGradientRect(r2, hfaderCol1, hfaderCol2);
            Event evt = Event.current;
            if (evt.type == EventType.MouseDown && r2.Contains(evt.mousePosition))
            {
                m_MouseDragStartX = evt.mousePosition.x;
                m_MouseDragStartValue = value;
                SetFocus();
            }
            if (HasFocus())
            {
                if (evt.type == EventType.MouseDrag)
                {
                    value = m_MouseDragStartValue + (dragScale * (maxValue - minValue) * (evt.mousePosition.x - m_MouseDragStartX) / faderScreenRange);
                    Event.current.Use();
                }
                else if (evt.type == EventType.MouseUp)
                {
                    ClearFocus();
                    Event.current.Use();
                }
            }
            value = Mathf.Clamp(value, minValue, maxValue);
            r2.x = r.x;
            r2.width = r.width;
            r2.x = r.x + faderScreenRange * ((value - minValue) / (maxValue - minValue));
            r2.width = handleWidth;
            AudioMixerDrawUtils.DrawGradientRect(r2, hfaderCol2, hfaderCol1);
            return value;
        }

        public GUIStyle sharedGuiStyle = new GUIStyle();

        public GUIStyle GetEffectBarStyle(AudioMixerEffectController effect)
        {
            if (effect.IsSend() || effect.IsReceive() || effect.IsDuckVolume())
                return styles.sendReturnBar;
            if (effect.IsAttenuation())
                return styles.attenuationBar;

            return styles.effectBar;
        }

        class PatchSlot
        {
            public AudioMixerGroupController group;
            public float x, y;
        };

        GUIContent bypassButtonContent = new GUIContent("", "Toggle bypass on this effect");
        void EffectSlot(Rect effectRect, AudioMixerSnapshotController snapshot, AudioMixerEffectController effect, int effectIndex, ref int highlightEffectIndex, ChannelStripParams p, ref Dictionary<AudioMixerEffectController, PatchSlot> patchslots)
        {
            if (effect == null)
                return;

            Rect r = effectRect;

            Event evt = Event.current;
            if (evt.type == EventType.Repaint && patchslots != null && (effect.IsSend() || MixerEffectDefinitions.EffectCanBeSidechainTarget(effect)))
            {
                var c = new PatchSlot();
                c.group = p.group;
                c.x = r.xMax - (r.yMax - r.yMin) * 0.5f;
                c.y = (r.yMin + r.yMax) * 0.5f;
                patchslots[effect] = c;
            }

            // Bypass button
            const float bypassWidth = 10.0f;
            bool isBypassable = !effect.DisallowsBypass();
            Rect bypassRect = r; bypassRect.width = bypassWidth;
            r.xMin += bypassWidth;
            if (isBypassable)
            {
                // Handle mouse input is first, rendering is further down (to render on top).
                if (GUI.Button(bypassRect, bypassButtonContent, GUIStyle.none))
                {
                    Undo.RecordObject(effect, "Bypass Effect");
                    effect.bypass = !effect.bypass;
                    m_Controller.UpdateBypass();
                    InspectorWindow.RepaintAllInspectors();
                }
            }

            // Effect
            m_IndexCounter++;

            // Repaint
            // We are using the following GUIStyle backgrounds for:
            //  Normal: Disabled by bypass or group bypass
            //  Hover: Enabled, not draggable
            //  Active: Enabled, background for draggable bar
            //  Focused: Enabled, foreground for draggable bar

            float level = (effect != null) ? Mathf.Clamp(effect.GetValueForMixLevel(m_Controller, snapshot), AudioMixerController.kMinVolume, AudioMixerController.kMaxEffect) : AudioMixerController.kMinVolume;
            bool showLevel = (effect != null) && ((effect.IsSend() && effect.sendTarget != null) || effect.enableWetMix);
            if (evt.type == EventType.Repaint)
            {
                GUIStyle style = GetEffectBarStyle(effect);

                float drawLevel = (showLevel) ? ((level - AudioMixerController.kMinVolume) / (AudioMixerController.kMaxEffect - AudioMixerController.kMinVolume)) : 1.0f;
                bool enabled = (!p.group.bypassEffects && (effect == null || !effect.bypass)) || (effect != null && effect.DisallowsBypass());
                Color col1 = (effect != null) ? AudioMixerDrawUtils.GetEffectColor(effect) : new Color(0.0f, 0.0f, 0.0f, 0.5f);
                if (!enabled)
                    col1 = new Color(col1.r * 0.5f, col1.g * 0.5f, col1.b * 0.5f);

                if (enabled)
                {
                    if (drawLevel < 1.0f)
                    {
                        // Bar forground (we need special for small widths because style rendering with borders have a min width to work)
                        float forgroundWidth = r.width * drawLevel;
                        const float minimumWidthForBorderSetup = 4f;
                        if (forgroundWidth < minimumWidthForBorderSetup)
                        {
                            // Forground less than minimumWidthForBorderSetup (Always show marker to indicate the slot is draggable)
                            const float markerWidth = 2f;
                            forgroundWidth = Mathf.Max(forgroundWidth, markerWidth);
                            float frac = 1f - forgroundWidth / minimumWidthForBorderSetup;

                            Color orgColor = GUI.color;
                            if (!GUI.enabled)
                                GUI.color = new Color(1, 1, 1, 0.5f);
                            GUI.DrawTextureWithTexCoords(new Rect(r.x, r.y, forgroundWidth, r.height), style.focused.background, new Rect(frac, 0f, 1f - frac, 1f));
                            GUI.color = orgColor;
                        }
                        else
                        {
                            // Forground larger than minimumWidthForBorderSetup (uses border setup)
                            style.Draw(new Rect(r.x, r.y, forgroundWidth, r.height), false, false, false, true);
                        }

                        // Bar background
                        GUI.DrawTexture(new Rect(r.x + forgroundWidth, r.y, r.width - forgroundWidth, r.height), style.onFocused.background, ScaleMode.StretchToFill);
                    }
                    else
                    {
                        //GUI.DrawTexture(r, showLevel ? style.focused.background : style.hover.background, ScaleMode.StretchToFill);
                        style.Draw(r, !showLevel, false, false, showLevel);
                    }

                    // Cursor icon needs to proper work with dragging: TODO use hotcontrol
                    //if (showLevel)
                    //  EditorGUIUtility.AddCursorRect(r, MouseCursor.SlideArrow);
                }
                else
                {
                    // Disabled
                    style.Draw(r, false, false, false, false);
                }

                // Bypass toggle
                if (isBypassable)
                    styles.circularToggle.Draw(new Rect(bypassRect.x + 2, bypassRect.y + 5f, bypassRect.width - 2, bypassRect.width - 2f), false, false, !effect.bypass, false);

                if (effect.IsSend() && effect.sendTarget != null)
                {
                    bypassRect.y -= 1;
                    GUI.Label(bypassRect, styles.sendString, EditorStyles.miniLabel);
                }

                using (new EditorGUI.DisabledScope(!enabled))
                {
                    string name = GetEffectSlotName(effect, showLevel, snapshot, p);
                    string tooltip = GetEffectSlotTooltip(effect, r, p);
                    GUI.Label(new Rect(r.x, r.y, r.width - bypassWidth, r.height), GUIContent.Temp(name, tooltip), styles.effectName);
                }
            }
            else
            {
                EffectSlotDragging(effectRect, snapshot, effect, showLevel, level, effectIndex, ref highlightEffectIndex, p);
            }
        }

        string GetEffectSlotName(AudioMixerEffectController effect, bool showLevel, AudioMixerSnapshotController snapshot, ChannelStripParams p)
        {
            if (m_ChangingWetMixIndex == m_IndexCounter && showLevel)
            {
                return string.Format("{0:F1} dB", effect.GetValueForMixLevel(m_Controller, snapshot));
            }

            if (effect.IsSend() && effect.sendTarget != null)
            {
                return effect.GetSendTargetDisplayString(p.effectMap);
            }

            return effect.effectName;
        }

        string GetEffectSlotTooltip(AudioMixerEffectController effect, Rect effectRect, ChannelStripParams p)
        {
            // Only fetch a tooltip if the cursor is inside the rect
            if (!effectRect.Contains(Event.current.mousePosition))
                return string.Empty;

            if (effect.IsSend())
            {
                if (effect.sendTarget != null)
                {
                    string sendTarget = effect.GetSendTargetDisplayString(p.effectMap);
                    return "Send to: " + sendTarget; // We add the tooltip here because we rarely
                }
                else
                {
                    return styles.emptySendSlotGUIContent.tooltip;
                }
            }
            if (effect.IsReceive())
            {
                return styles.returnSlotGUIContent.tooltip;
            }
            if (effect.IsDuckVolume())
            {
                return styles.duckVolumeSlotGUIContent.tooltip;
            }
            if (effect.IsAttenuation())
            {
                return styles.attenuationSlotGUIContent.tooltip;
            }

            // Tooltip for all other effects
            return styles.effectSlotGUIContent.tooltip;
        }

        private void EffectSlotDragging(Rect r, AudioMixerSnapshotController snapshot, AudioMixerEffectController effect, bool showLevel, float level, int effectIndex, ref int highlightEffectIndex, ChannelStripParams p)
        {
            Event evt = Event.current;

            switch (evt.GetTypeForControl(m_EffectInteractionControlID))
            {
                case EventType.MouseDown:
                    if (r.Contains(evt.mousePosition) && evt.button == 0 && GUIUtility.hotControl == 0)
                    {
                        GUIUtility.hotControl = m_EffectInteractionControlID;
                        m_MouseDragStartX = evt.mousePosition.x;
                        m_MouseDragStartValue = level;
                        highlightEffectIndex = effectIndex;
                        m_MovingEffectSrcIndex = -1;
                        m_MovingEffectDstIndex = -1;
                        m_WaitingForDragEvent = true;
                        m_MovingSrcRect = r;
                        m_MovingDstRect = r;
                        m_MovingSrcGroup = p.group;
                        m_MovingDstGroup = p.group;
                        m_MovingEffectAllowed = true;
                        SetFocus();
                        Event.current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                        InspectorWindow.RepaintAllInspectors();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == m_EffectInteractionControlID && evt.button == 0 && p.stripRect.Contains(evt.mousePosition))
                    {
                        if (m_MovingEffectDstIndex != -1 && m_MovingEffectAllowed)
                        {
                            if (IsDuplicateKeyPressed() && CanDuplicateDraggedEffect())
                            {
                                // Duplicate effect
                                AudioMixerEffectController sourceEffect = m_MovingSrcGroup.effects[m_MovingEffectSrcIndex];
                                AudioMixerEffectController copiedEffect = m_MovingSrcGroup.controller.CopyEffect(sourceEffect);
                                var targetEffects = m_MovingDstGroup.effects.ToList();
                                if (AudioMixerController.InsertEffect(copiedEffect, ref targetEffects, m_MovingEffectDstIndex))
                                {
                                    m_MovingDstGroup.effects = targetEffects.ToArray();
                                }
                            }
                            else
                            {
                                // Move effect
                                if (m_MovingSrcGroup == m_MovingDstGroup)
                                {
                                    var effects = m_MovingSrcGroup.effects.ToList();
                                    if (AudioMixerController.MoveEffect(ref effects, m_MovingEffectSrcIndex, ref effects, m_MovingEffectDstIndex))
                                    {
                                        m_MovingSrcGroup.effects = effects.ToArray();
                                    }
                                }
                                else if (!m_MovingSrcGroup.effects[m_MovingEffectSrcIndex].IsAttenuation())
                                {
                                    var sourceEffects = m_MovingSrcGroup.effects.ToList();
                                    var targetEffects = m_MovingDstGroup.effects.ToList();
                                    if (AudioMixerController.MoveEffect(ref sourceEffects, m_MovingEffectSrcIndex, ref targetEffects, m_MovingEffectDstIndex))
                                    {
                                        m_MovingSrcGroup.effects = sourceEffects.ToArray();
                                        m_MovingDstGroup.effects = targetEffects.ToArray();
                                    }
                                }
                            }
                        }

                        ClearEffectDragging(ref highlightEffectIndex);
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                        GUIUtility.ExitGUI();  // We changed order of effects so stop iterating effects
                    }
                    break;

                case EventType.MouseDrag:

                    if (GUIUtility.hotControl == m_EffectInteractionControlID)
                    {
                        // Detect direction of drag to decide if we want to adjust value or move effect
                        if (HasFocus() && m_WaitingForDragEvent)
                        {
                            m_ChangingWetMixIndex = -1;
                            if (effectIndex < p.group.effects.Length)
                            {
                                if (Mathf.Abs(evt.delta.y) > Mathf.Abs(evt.delta.x))
                                {
                                    // Move effect
                                    m_MovingEffectSrcIndex = effectIndex;
                                    ClearFocus();
                                }
                                else
                                {
                                    // Change wet mix value
                                    m_ChangingWetMixIndex = m_IndexCounter;
                                }
                            }
                            m_WaitingForDragEvent = false;
                        }

                        // Moving effect so detect effect insertion on this strip
                        if (IsMovingEffect() && p.stripRect.Contains(evt.mousePosition))
                        {
                            float h2 = r.height * 0.5f;
                            float min = effectIndex == 0 ? -h2 : 0;
                            float max = effectIndex == p.group.effects.Length - 1 ? r.height + h2 : r.height;
                            float dy = evt.mousePosition.y - r.y;
                            if (dy >= min && dy <= max && effectIndex < p.group.effects.Length)
                            {
                                int newMovingEffectDstIndex = (dy < h2) ? effectIndex : (effectIndex + 1);
                                if (newMovingEffectDstIndex != m_MovingEffectDstIndex || m_MovingDstGroup != p.group)
                                {
                                    m_MovingDstRect.x = r.x;
                                    m_MovingDstRect.width = r.width;
                                    m_MovingDstRect.y = ((dy < h2) ? r.y : (r.y + r.height)) - 1;
                                    m_MovingEffectDstIndex = newMovingEffectDstIndex;
                                    m_MovingDstGroup = p.group;
                                    m_MovingEffectAllowed =
                                        !(m_MovingSrcGroup.effects[m_MovingEffectSrcIndex].IsAttenuation() && m_MovingSrcGroup != m_MovingDstGroup) &&
                                        !AudioMixerController.WillMovingEffectCauseFeedback(p.allGroups, m_MovingSrcGroup, m_MovingEffectSrcIndex, m_MovingDstGroup, newMovingEffectDstIndex, null) &&
                                        (!IsDuplicateKeyPressed() || CanDuplicateDraggedEffect());
                                }
                                evt.Use(); // We use when having valid pos so because if drag event is not used after all strips we clear drag destination to remove insert hightlight for invalid positions
                            }
                        }

                        // Changing wetmix level
                        if (IsAdjustingWetMix() && HasFocus())
                        {
                            if (showLevel)
                            {
                                m_WaitingForDragEvent = false;

                                float tmp = kEffectScaleMouseDrag * HandleUtility.niceMouseDelta + level;
                                float deltaLevel = Mathf.Clamp(tmp, AudioMixerController.kMinVolume, AudioMixerController.kMaxEffect) - level;
                                if (deltaLevel != 0.0f)
                                {
                                    Undo.RecordObject(m_Controller.TargetSnapshot, "Change effect level");
                                    if (effect.IsSend() && m_Controller.CachedSelection.Count > 1 && m_Controller.CachedSelection.Contains(p.group))
                                    {
                                        List<AudioMixerEffectController> changeEffects = new List<AudioMixerEffectController>();
                                        foreach (var g in m_Controller.CachedSelection)
                                            foreach (var e in g.effects)
                                                if (e.effectName == effect.effectName && e.sendTarget == effect.sendTarget)
                                                    changeEffects.Add(e);
                                        foreach (var e in changeEffects)
                                            if (!e.IsSend() || e.sendTarget != null)
                                                e.SetValueForMixLevel(
                                                    m_Controller,
                                                    snapshot,
                                                    Mathf.Clamp(e.GetValueForMixLevel(m_Controller, snapshot) + deltaLevel, AudioMixerController.kMinVolume, AudioMixerController.kMaxEffect));
                                    }
                                    else
                                    {
                                        if (!effect.IsSend() || effect.sendTarget != null)
                                            effect.SetValueForMixLevel(
                                                m_Controller,
                                                snapshot,
                                                Mathf.Clamp(level + deltaLevel, AudioMixerController.kMinVolume, AudioMixerController.kMaxEffect));
                                    }
                                    InspectorWindow.RepaintAllInspectors();
                                }
                                evt.Use();
                            }
                        }
                    }
                    break;
            }
        }

        void ClearEffectDragging(ref int highlightEffectIndex)
        {
            if (GUIUtility.hotControl == m_EffectInteractionControlID)
                GUIUtility.hotControl = 0;
            m_MovingEffectSrcIndex = -1;
            m_MovingEffectDstIndex = -1;
            m_MovingSrcRect = new Rect(-1, -1, 0, 0);
            m_MovingDstRect = new Rect(-1, -1, 0, 0);
            m_MovingSrcGroup = null;
            m_MovingDstGroup = null;
            m_ChangingWetMixIndex = -1;
            highlightEffectIndex = -1;
            ClearFocus();
            InspectorWindow.RepaintAllInspectors();
        }

        void UnhandledEffectDraggingEvents(ref int highlightEffectIndex)
        {
            Event evt = Event.current;

            switch (evt.GetTypeForControl(m_EffectInteractionControlID))
            {
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == m_EffectInteractionControlID && evt.button == 0)
                    {
                        ClearEffectDragging(ref highlightEffectIndex);
                        evt.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == m_EffectInteractionControlID)
                    {
                        // Clear dst group when dragging outside the strips
                        m_MovingEffectDstIndex = -1;
                        m_MovingDstRect = new Rect(-1, -1, 0, 0);
                        m_MovingDstGroup = null;
                        evt.Use();
                    }
                    break;
                case EventType.Repaint:

                    if (IsMovingEffect())
                    {
                        // Mark the effect being dragged
                        if (evt.type == EventType.Repaint)
                        {
                            EditorGUI.DrawRect(m_MovingSrcRect, kMoveColorHighlight);

                            // Set + cursor when duplicating dragged effect
                            var cursor = (IsDuplicateKeyPressed() && m_MovingEffectAllowed) ? MouseCursor.ArrowPlus : MouseCursor.ResizeVertical;
                            EditorGUIUtility.AddCursorRect(new Rect(evt.mousePosition.x - 10, evt.mousePosition.y - 10, 20, 20), cursor, m_EffectInteractionControlID);
                        }
                    }

                    if (m_MovingEffectDstIndex != -1 && m_MovingDstRect.y >= 0.0f)// && m_MovingEffectDstIndex != m_MovingEffectSrcIndex && m_MovingEffectDstIndex != m_MovingEffectSrcIndex +1)
                    {
                        float kMoveRange = 2;
                        Color moveSlotColLo = (m_MovingEffectAllowed) ? kMoveSlotColLoAllowed : kMoveSlotColLoDisallowed;
                        Color moveSlotColHi = (m_MovingEffectAllowed) ? kMoveSlotColHiAllowed : kMoveSlotColHiDisallowed;
                        Color moveSlotColBorder = (m_MovingEffectAllowed) ? kMoveSlotColBorderAllowed : kMoveSlotColBorderDisallowed;
                        AudioMixerDrawUtils.DrawGradientRect(new Rect(m_MovingDstRect.x, m_MovingDstRect.y - kMoveRange, m_MovingDstRect.width, kMoveRange), moveSlotColLo, moveSlotColHi);
                        AudioMixerDrawUtils.DrawGradientRect(new Rect(m_MovingDstRect.x, m_MovingDstRect.y, m_MovingDstRect.width, kMoveRange), moveSlotColHi, moveSlotColLo);
                        AudioMixerDrawUtils.DrawGradientRect(new Rect(m_MovingDstRect.x, m_MovingDstRect.y - 1, m_MovingDstRect.width, 1), moveSlotColBorder, moveSlotColBorder);
                    }

                    break;
            }
        }

        bool IsDuplicateKeyPressed()
        {
            return Event.current.alt;
        }

        bool CanDuplicateDraggedEffect()
        {
            if (IsMovingEffect() && m_MovingSrcGroup != null)
                return !m_MovingSrcGroup.effects[m_MovingEffectSrcIndex].IsAttenuation();
            return false;
        }

        private class BusConnection
        {
            public BusConnection(float srcX, float srcY, AudioMixerEffectController targetEffect, float mixLevel, Color col, bool isSend, bool isSelected)
            {
                this.srcX = srcX;
                this.srcY = srcY;
                this.targetEffect = targetEffect;
                this.mixLevel = mixLevel;
                this.color = col;
                this.isSend = isSend;
                this.isSelected = isSelected;
            }

            public AudioMixerEffectController targetEffect;
            public float srcX;
            public float srcY;
            public float mixLevel;
            public Color color;
            public bool isSend;
            public bool isSelected;
        }

        private bool DoSoloButton(Rect r, AudioMixerGroupController group, List<AudioMixerGroupController> allGroups, List<AudioMixerGroupController> selection)
        {
            Event evt = Event.current;

            // Right click toggle
            if (evt.type == EventType.MouseUp && evt.button == 1 && r.Contains(evt.mousePosition) && allGroups.Any(g => g.solo))
            {
                Undo.RecordObject(group, "Change solo state");
                foreach (var g in allGroups)
                    g.solo = false;
                evt.Use();
                return true;
            }

            bool newState = GUI.Toggle(r, group.solo, styles.soloGUIContent, AudioMixerDrawUtils.styles.soloToggle);
            if (newState != group.solo)
            {
                Undo.RecordObject(group, "Change solo state");
                group.solo = !group.solo;
                foreach (var g in selection)
                    g.solo = group.solo;
                return true;
            }


            return false;
        }

        private bool DoMuteButton(Rect r, AudioMixerGroupController group, List<AudioMixerGroupController> allGroups, bool anySoloActive, List<AudioMixerGroupController> selection)
        {
            Event evt = Event.current;
            // Right click toggle
            if (evt.type == EventType.MouseUp && evt.button == 1 && r.Contains(evt.mousePosition) && allGroups.Any(g => g.mute))
            {
                Undo.RecordObject(group, "Change mute state");
                if (allGroups.Any(g => g.solo))
                    return false;
                foreach (var g in allGroups)
                    g.mute = false;
                evt.Use();
                return true;
            }
            Color orgColor = GUI.color;
            bool dimColor = anySoloActive && group.mute;
            if (dimColor)
                GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 0.5f);
            bool newState = GUI.Toggle(r, group.mute, styles.muteGUIContent, AudioMixerDrawUtils.styles.muteToggle);
            if (dimColor)
                GUI.color = orgColor;

            if (newState != group.mute)
            {
                Undo.RecordObject(group, "Change mute state");
                group.mute = !group.mute;
                foreach (var g in selection)
                    g.mute = group.mute;
                return true;
            }

            return false;
        }

        private bool DoBypassEffectsButton(Rect r, AudioMixerGroupController group, List<AudioMixerGroupController> allGroups, List<AudioMixerGroupController> selection)
        {
            Event evt = Event.current;
            // Right click toggle
            if (evt.type == EventType.MouseUp && evt.button == 1 && r.Contains(evt.mousePosition) && allGroups.Any(g => g.bypassEffects))
            {
                Undo.RecordObject(group, "Change bypass effects state");
                foreach (var g in allGroups)
                    g.bypassEffects = false;
                evt.Use();
                return true;
            }

            bool newState = GUI.Toggle(r, group.bypassEffects, styles.bypassGUIContent, AudioMixerDrawUtils.styles.bypassToggle);
            if (newState != group.bypassEffects)
            {
                Undo.RecordObject(group, "Change bypass effects state");
                group.bypassEffects = !group.bypassEffects;
                foreach (var g in selection)
                    g.bypassEffects = group.bypassEffects;
                return true;
            }

            return false;
        }

        private static bool RectOverlaps(Rect r1, Rect r2)
        {
            Rect overlap = new Rect();
            overlap.x = Mathf.Max(r1.x, r2.x);
            overlap.y = Mathf.Max(r1.y, r2.y);
            overlap.width = Mathf.Min(r1.x + r1.width, r2.x + r2.width) - overlap.x;
            overlap.height = Mathf.Min(r1.y + r1.height, r2.y + r2.height) - overlap.y;
            return overlap.width > 0.0f && overlap.height > 0.0f;
        }

        bool IsRectSelectionActive()
        {
            return GUIUtility.hotControl == m_RectSelectionControlID;
        }

        // Handles multiselection (using shift + ctrl/cmd)
        void GroupClicked(AudioMixerGroupController clickedGroup, ChannelStripParams p, bool clickedControlInGroup)
        {
            // Get ids from items
            List<int> allIDs = new List<int>();
            foreach (var group in p.shownGroups)
                allIDs.Add(group.GetInstanceID());

            List<int> selectedIDs = new List<int>();
            foreach (var group in m_Controller.CachedSelection)
                selectedIDs.Add(group.GetInstanceID());

            int lastClickedID = m_State.m_LastClickedInstanceID;
            bool allowMultiselection = true;
            bool keepMultiSelection = Event.current.shift || clickedControlInGroup;
            bool useShiftAsActionKey = false;

            List<int> newSelection = InternalEditorUtility.GetNewSelection(clickedGroup.GetInstanceID(), allIDs, selectedIDs, lastClickedID, keepMultiSelection, useShiftAsActionKey, allowMultiselection);
            List<AudioMixerGroupController> groups = (from x in p.allGroups where newSelection.Contains(x.GetInstanceID()) select x).ToList();

            Selection.objects = groups.ToArray();
            m_Controller.OnUnitySelectionChanged();

            InspectorWindow.RepaintAllInspectors();
        }

        private void DoAttenuationFader(Rect r, AudioMixerGroupController group, List<AudioMixerGroupController> selection, GUIStyle style)
        {
            float volume = Mathf.Clamp(group.GetValueForVolume(m_Controller, m_Controller.TargetSnapshot), AudioMixerController.kMinVolume, AudioMixerController.GetMaxVolume());
            float newVolume = VerticalFader(r, volume, 1, kVolumeScaleMouseDrag, true, true, styles.attenuationFader.tooltip, AudioMixerController.GetMaxVolume(), style);
            if (volume != newVolume)
            {
                float deltaVolume = newVolume - volume;
                Undo.RecordObject(m_Controller.TargetSnapshot, "Change volume fader");
                foreach (var g in selection)
                {
                    float vol = Mathf.Clamp(g.GetValueForVolume(m_Controller, m_Controller.TargetSnapshot), AudioMixerController.kMinVolume, AudioMixerController.GetMaxVolume());
                    g.SetValueForVolume(
                        m_Controller,
                        m_Controller.TargetSnapshot,
                        Mathf.Clamp(vol + deltaVolume, AudioMixerController.kMinVolume, AudioMixerController.GetMaxVolume()));
                }
                InspectorWindow.RepaintAllInspectors();
            }
        }

        static internal void AddEffectItemsToMenu(AudioMixerController controller, AudioMixerGroupController[] groups, int insertIndex, string prefix, GenericMenu pm)
        {
            string[] effectNames = MixerEffectDefinitions.GetEffectList();
            for (int t = 0; t < effectNames.Length; t++)
            {
                if (effectNames[t] != "Attenuation")
                    pm.AddItem(new GUIContent(prefix + AudioMixerController.FixNameForPopupMenu(effectNames[t])),
                        false,
                        InsertEffectPopupCallback,
                        new EffectContext(controller, groups, insertIndex, effectNames[t]));
            }
        }

        private void DoEffectSlotInsertEffectPopup(Rect buttonRect, AudioMixerGroupController group, List<AudioMixerGroupController> allGroups,
            int effectSlotIndex, ref Dictionary<AudioMixerEffectController, AudioMixerGroupController> effectMap)
        {
            GenericMenu pm = new GenericMenu();

            AudioMixerGroupController[] groups = new AudioMixerGroupController[] { group };

            if (effectSlotIndex < group.effects.Length)
            {
                var effect = group.effects[effectSlotIndex];
                if (!effect.IsAttenuation() && !effect.IsSend() && !effect.IsReceive() && !effect.IsDuckVolume())
                {
                    pm.AddItem(new GUIContent("Allow Wet Mixing (causes higher memory usage)"), effect.enableWetMix, delegate { effect.enableWetMix = !effect.enableWetMix; });
                    pm.AddItem(new GUIContent("Bypass"), effect.bypass, delegate { effect.bypass = !effect.bypass; m_Controller.UpdateBypass(); InspectorWindow.RepaintAllInspectors(); });
                    pm.AddSeparator("");
                }

                AddEffectItemsToMenu(group.controller, groups, effectSlotIndex, "Add effect before/", pm);
                AddEffectItemsToMenu(group.controller, groups, effectSlotIndex + 1, "Add effect after/", pm);
            }
            else
            {
                // Add effect at the end of the list
                AddEffectItemsToMenu(group.controller, groups, effectSlotIndex, "", pm);
            }

            if (effectSlotIndex < group.effects.Length)
            {
                var effect = group.effects[effectSlotIndex];
                if (!effect.IsAttenuation())
                {
                    pm.AddSeparator("");
                    pm.AddItem(new GUIContent("Remove"), false, RemoveEffectPopupCallback, new EffectContext(m_Controller, groups, effectSlotIndex, ""));
                    bool insertedSeparator = false;
                    if (effect.IsSend())
                    {
                        if (effect.sendTarget != null)
                        {
                            if (!insertedSeparator)
                            {
                                insertedSeparator = true;
                                pm.AddSeparator("");
                            }
                            pm.AddItem(new GUIContent("Disconnect from '" + effect.GetSendTargetDisplayString(effectMap) + "'") , false, ConnectSendPopupCallback, new ConnectSendContext(effect, null));
                        }

                        if (!insertedSeparator)
                            AddSeperatorIfAnyReturns(pm, allGroups);
                        AddMenuItemsForReturns(pm, "Connect to ", effectSlotIndex, group, allGroups, effectMap, effect, false);
                    }
                }
            }
            pm.DropDown(buttonRect);
            Event.current.Use();
        }

        void AddSeperatorIfAnyReturns(GenericMenu pm, List<AudioMixerGroupController> allGroups)
        {
            foreach (var g in allGroups)
            {
                foreach (var ge in g.effects)
                {
                    if (ge.IsReceive() || ge.IsDuckVolume())
                    {
                        pm.AddSeparator("");
                        return;
                    }
                }
            }
        }

        public static void AddMenuItemsForReturns(GenericMenu pm, string prefix, int effectIndex, AudioMixerGroupController group, List<AudioMixerGroupController> allGroups,
            Dictionary<AudioMixerEffectController, AudioMixerGroupController> effectMap, AudioMixerEffectController effect, bool showCurrent)
        {
            foreach (var g in allGroups)
            {
                foreach (var ge in g.effects)
                {
                    if (MixerEffectDefinitions.EffectCanBeSidechainTarget(ge))
                    {
                        var identifiedLoop = new List<AudioMixerController.ConnectionNode>();

                        //TODO: In the future, consider allowing feedback loops, as the send code now allows for this, though give a warning to the user
                        // that they may be doing something stupid
                        if (!AudioMixerController.WillChangeOfEffectTargetCauseFeedback(allGroups, group, effectIndex, ge, identifiedLoop))
                        {
                            if (showCurrent || effect.sendTarget != ge)
                                pm.AddItem(new GUIContent(prefix + "'" + ge.GetDisplayString(effectMap) + "'"), effect.sendTarget == ge, ConnectSendPopupCallback, new ConnectSendContext(effect, ge));
                        }
                        else
                        {
                            string baseString = "A connection to '" + AudioMixerController.FixNameForPopupMenu(ge.GetDisplayString(effectMap)) + "' would result in a feedback loop/";
                            pm.AddDisabledItem(new GUIContent(baseString + "Loop: "));
                            int loopIndex = 1;
                            foreach (var s in identifiedLoop)
                            {
                                pm.AddDisabledItem(new GUIContent(baseString + loopIndex + ": " + s.GetDisplayString() + "->"));
                                loopIndex++;
                            }
                            pm.AddDisabledItem(new GUIContent(baseString + loopIndex + ": ..."));
                        }
                    }
                }
            }
        }

        public void VUMeter(AudioMixerGroupController group, Rect r, float level, float peak)
        {
            EditorGUI.VUMeter.VerticalMeter(r, level, peak,  EditorGUI.VUMeter.verticalVUTexture, Color.grey);
        }

        private GUIContent headerGUIContent = new GUIContent();

        class ChannelStripParams
        {
            public int index;
            public Rect stripRect;
            public Rect visibleRect;
            public bool visible;
            public AudioMixerGroupController group;
            public int maxEffects;
            public bool drawingBuses;
            public bool anySoloActive;
            public List<BusConnection> busConnections = new List<BusConnection>();
            public List<AudioMixerGroupController> rectSelectionGroups = new List<AudioMixerGroupController>();
            public List<AudioMixerGroupController> allGroups;
            public List<AudioMixerGroupController> shownGroups;

            public int numChannels = 0;
            public float[] vuinfo_level = new float[9];
            public float[] vuinfo_peak = new float[9];

            //public int highlightEffectIndex;
            public Dictionary<AudioMixerEffectController, AudioMixerGroupController> effectMap;

            const float kAddEffectButtonHeight = 16f;

            public List<Rect> bgRects;
            public readonly int kHeaderIndex = 0;
            public readonly int kVUMeterFaderIndex = 1;
            public readonly int kTotalVULevelIndex = 2;
            public readonly int kSoloMuteBypassIndex = 3;
            public readonly int kEffectStartIndex = 4;

            public void Init(AudioMixerController controller, Rect channelStripRect, int maxNumEffects)
            {
                numChannels = controller.GetGroupVUInfo(group.groupID, false, ref vuinfo_level, ref vuinfo_peak);
                //numChannels = 8; // debugging

                maxEffects = maxNumEffects;
                bgRects = GetBackgroundRects(channelStripRect, group, maxEffects);
                stripRect = channelStripRect;


                stripRect.yMax = bgRects[bgRects.Count - 1].yMax; // Ensure full height
            }

            List<Rect> GetBackgroundRects(Rect r, AudioMixerGroupController group, int maxNumGroups)
            {
                List<float> heights = new List<float>();
                heights.AddRange(Enumerable.Repeat(0f, kEffectStartIndex));
                heights[kHeaderIndex] = headerHeight;
                heights[kVUMeterFaderIndex] = vuHeight;
                heights[kTotalVULevelIndex] = dbHeight;
                heights[kSoloMuteBypassIndex] = soloMuteBypassHeight;
                int maxNumEffectSlots = maxNumGroups; // maxNumGroups includes an empty slot used as button for adding new effect
                for (int i = 0; i < maxNumEffectSlots; ++i)
                    heights.Add(effectHeight);
                heights.Add(10f);

                List<Rect> rects = new List<Rect>();
                float curY = r.y;
                foreach (int height in heights)
                {
                    if (rects.Count > 0)
                        curY += spaceBetween;

                    rects.Add(new Rect(r.x, curY, r.width, height));
                    curY += height;
                }

                curY += 10f; // space between last effect and add button

                rects.Add(new Rect(r.x, curY, r.width, kAddEffectButtonHeight));

                return rects;
            }
        }

        void DrawBackgrounds(ChannelStripParams p, bool selected)
        {
            if (Event.current.type == EventType.Repaint)
            {
                // Bg
                styles.channelStripBg.Draw(p.stripRect, false, false, selected, false);

                // Line over effects (todo: move to texture)
                float lightColor = 119f / 255f;
                float darkColor = 58f / 255f;
                Color lineColor = EditorGUIUtility.isProSkin ? new Color(darkColor, darkColor, darkColor) : new Color(lightColor, lightColor, lightColor);
                Rect lineRect = p.bgRects[p.kEffectStartIndex];
                lineRect.y -= 1;
                lineRect.height = 1;
                EditorGUI.DrawRect(lineRect, lineColor);

                // Optimize by rendering vu numbers and lines in one texture
                //Rect vuNumbersRect = p.bgRects[1];
                //vuNumbersRect.x = p.stripRect.xMax - 90f;
                //vuNumbersRect.x = vuNumbersRect.xMax - AudioMixerDrawUtils.styles.channelStripVUMeterBg.normal.background.width; // right align
                //AudioMixerDrawUtils.styles.channelStripVUMeterBg.Draw(vuNumbersRect, false, false, false, false); // 0 -80 dB numbers overlay
            }

            // User color
            Rect colorRect = p.bgRects[p.kVUMeterFaderIndex];
            colorRect.height = EditorGUIUtility.isProSkin ? 1 : 2;
            colorRect.y -= colorRect.height;

            int colorIndex = p.group.userColorIndex;
            if (colorIndex != 0)
                EditorGUI.DrawRect(colorRect, AudioMixerColorCodes.GetColor(colorIndex));
        }

        void OpenGroupContextMenu(AudioMixerGroupController[] groups)
        {
            GenericMenu pm = new GenericMenu();

            AddEffectItemsToMenu(groups[0].controller, groups, 0, "Add effect at top/", pm);
            AddEffectItemsToMenu(groups[0].controller, groups, -1, "Add effect at bottom/", pm);

            pm.AddSeparator(string.Empty);
            AudioMixerColorCodes.AddColorItemsToGenericMenu(pm, groups);
            pm.AddSeparator(string.Empty);

            pm.ShowAsContext();
        }

        private void DrawChannelStrip(ChannelStripParams p, ref int highlightEffectIndex, ref Dictionary<AudioMixerEffectController, PatchSlot> patchslots, bool showBusConnectionsOfSelection)
        {
            Event evt = Event.current;

            bool mouseDownInsideStrip = evt.type == EventType.MouseDown && p.stripRect.Contains(evt.mousePosition);

            // Selection rendering state
            bool selected = m_Controller.CachedSelection.Contains(p.group);
            if (IsRectSelectionActive())
            {
                if (RectOverlaps(p.stripRect, m_RectSelectionRect))
                {
                    p.rectSelectionGroups.Add(p.group);
                    selected = true;
                }
            }

            // Draw all background rects
            DrawBackgrounds(p, selected);

            // Header area
            headerGUIContent.text = headerGUIContent.tooltip = p.group.GetDisplayString();
            GUI.Label(p.bgRects[p.kHeaderIndex], headerGUIContent, AudioMixerDrawUtils.styles.channelStripHeaderStyle);

            // VU Meter and attenuation area
            Rect innerRect = new RectOffset(-6, 0, 0, -4).Add(p.bgRects[p.kVUMeterFaderIndex]);

            float spaceBetweenColumns = 1f;
            float column1Width = 54f;
            float column2Width = innerRect.width - column1Width - spaceBetweenColumns;
            Rect column1Rect = new Rect(innerRect.x, innerRect.y, column1Width, innerRect.height);
            Rect column2Rect = new Rect(column1Rect.xMax + spaceBetweenColumns, innerRect.y, column2Width, innerRect.height);

            float faderWidth = 29f;
            Rect faderRect = new Rect(column2Rect.x, column2Rect.y, faderWidth, column2Rect.height);
            Rect tripleButtonRect = p.bgRects[p.kSoloMuteBypassIndex];
            GUIStyle attenuationMarkerStyle = AudioMixerDrawUtils.styles.channelStripAttenuationMarkerSquare;
            using (new EditorGUI.DisabledScope(!AudioMixerController.EditingTargetSnapshot()))
            {
                DoVUMeters(column1Rect, attenuationMarkerStyle.fixedHeight, p);
                DoAttenuationFader(faderRect, p.group, m_Controller.CachedSelection, attenuationMarkerStyle);
                DoTotaldB(p);
                DoEffectList(p, selected, ref highlightEffectIndex, ref patchslots, showBusConnectionsOfSelection);
            }

            // We want to be able to set mute, solo and bypass even when EditingTargetSnapshot is false for testing during gameplay
            DoSoloMuteBypassButtons(tripleButtonRect, p.group, p.allGroups, m_Controller.CachedSelection, p.anySoloActive);

            // Handle group click after all controls so we can detect if a control was clicked by checking if the event was used here
            if (mouseDownInsideStrip && evt.button == 0)
                GroupClicked(p.group, p, evt.type == EventType.Used);

            // Check context click after all UI controls (only show if we did not interact with any controls)
            if (evt.type == EventType.ContextClick && p.stripRect.Contains(evt.mousePosition))
            {
                evt.Use();

                //If its part of an existing selection, then pass all, otherwise pretend its a new selection
                if (selected)
                    OpenGroupContextMenu(m_Controller.CachedSelection.ToArray());
                else
                    OpenGroupContextMenu(new AudioMixerGroupController[] { p.group });
            }
        }

        void DoTotaldB(ChannelStripParams p)
        {
            // Right align db but shown centered (prevents 'db' from jumping around)
            float textWidth = 50f;
            styles.totalVULevel.padding.right = (int)((p.stripRect.width - textWidth) * 0.5f);
            float vu_level = Mathf.Max(p.vuinfo_level[8], k_MinVULevel);
            Rect rect = p.bgRects[p.kTotalVULevelIndex];
            GUI.Label(rect, string.Format("{0:F1} dB", vu_level), styles.totalVULevel);
        }

        GUIContent addText = new GUIContent("Add..");
        void DoEffectList(ChannelStripParams p, bool selected, ref int highlightEffectIndex, ref Dictionary<AudioMixerEffectController, PatchSlot> patchslots, bool showBusConnectionsOfSelection)
        {
            Event evt = Event.current;
            for (int i = 0; i < p.maxEffects; i++)
            {
                Rect effectRect = p.bgRects[p.kEffectStartIndex + i];
                if (i < p.group.effects.Length)
                {
                    AudioMixerEffectController effect = p.group.effects[i];
                    if (p.visible)
                    {
                        // Use right click event first
                        if (evt.type == EventType.ContextClick && effectRect.Contains(Event.current.mousePosition))
                        {
                            ClearFocus();
                            DoEffectSlotInsertEffectPopup(effectRect, p.group, p.allGroups, i, ref p.effectMap);
                            evt.Use();
                        }

                        // Then do effect button
                        EffectSlot(effectRect, m_Controller.TargetSnapshot, effect, i, ref highlightEffectIndex, p, ref patchslots);
                    }
                }
            }

            // Empty slot below effects
            if (p.visible)
            {
                Rect effectRect = p.bgRects[p.bgRects.Count - 1];
                if (evt.type == EventType.Repaint)
                {
                    GUI.DrawTextureWithTexCoords(new Rect(effectRect.x, effectRect.y, effectRect.width, effectRect.height - 1), styles.effectBar.hover.background, new Rect(0, 0.5f, 0.1f, 0.1f));
                    GUI.Label(effectRect, addText, styles.effectName);
                }
                if (evt.type == EventType.MouseDown && effectRect.Contains(Event.current.mousePosition))
                {
                    ClearFocus();
                    int insertEffectAtIndex = p.group.effects.Length;
                    DoEffectSlotInsertEffectPopup(effectRect, p.group, p.allGroups, insertEffectAtIndex, ref p.effectMap);
                    evt.Use();
                }
            }
        }

        float DoVUMeters(Rect vuRect, float attenuationMarkerHeight, ChannelStripParams p)
        {
            float spaceBetweenMeters = 1f;

            // When the audiomixer has been rebuild we might have some frames where numChannels is not initialized.
            // To prevent flickering we use last valid num channels used at current index
            int numChannels = p.numChannels;
            if (numChannels == 0)
            {
                if (p.index >= 0 && p.index < m_LastNumChannels.Count)
                    numChannels = m_LastNumChannels[p.index];
            }
            else
            {
                // Cache valid numChannels for current index
                while (p.index >= m_LastNumChannels.Count)
                    m_LastNumChannels.Add(0);
                m_LastNumChannels[p.index] = numChannels;
            }

            // Adjust rect based on how many channels we have
            if (numChannels <= 2)
            {
                const float twoChannelWidth = 25f;
                vuRect.x = vuRect.xMax - twoChannelWidth; // right align
                vuRect.width = twoChannelWidth;
            }

            // This can happen when simulating player builds, i.e. not calculating VU-info for most of the channels.
            if (numChannels == 0)
                return vuRect.x;

            float vuOffsetY = Mathf.Floor(attenuationMarkerHeight / 2);
            vuRect.y += vuOffsetY;
            vuRect.height -= 2 * vuOffsetY;
            float vuWidth = Mathf.Round((vuRect.width - numChannels * spaceBetweenMeters) / numChannels);
            Rect vuSubRect = new Rect(vuRect.xMax - vuWidth, vuRect.y, vuWidth, vuRect.height);
            // Draw from right to left to ensure perfect right side pixel position (we are rounding widths above)
            for (int i = numChannels - 1; i >= 0; i--)
            {
                if (i != numChannels - 1)
                    vuSubRect.x -= vuSubRect.width + spaceBetweenMeters;

                float warpedLevel = 1f - AudioMixerController.VolumeToScreenMapping(Mathf.Clamp(p.vuinfo_level[i], AudioMixerController.kMinVolume, AudioMixerController.GetMaxVolume()), 1f, true);
                float warpedPeak =  1f - AudioMixerController.VolumeToScreenMapping(Mathf.Clamp(p.vuinfo_peak[i], AudioMixerController.kMinVolume, AudioMixerController.GetMaxVolume()), 1f, true);
                VUMeter(p.group, vuSubRect, warpedLevel, warpedPeak);
            }
            AudioMixerDrawUtils.AddTooltipOverlay(vuRect, styles.vuMeterGUIContent.tooltip);

            return vuSubRect.x;
        }

        void DoSoloMuteBypassButtons(Rect rect, AudioMixerGroupController group, List<AudioMixerGroupController> allGroups, List<AudioMixerGroupController> selection, bool anySoloActive)
        {
            float buttonSize = 21f;
            float spaceBetween = 2f;
            float startX = rect.x + (rect.width - (buttonSize * 3 + spaceBetween * 2)) * 0.5f;
            Rect buttonRect = new Rect(startX, rect.y, buttonSize, buttonSize);
            bool needMixerUpdate = false;
            needMixerUpdate |= DoSoloButton(buttonRect, group, allGroups, selection);
            buttonRect.x += buttonSize + spaceBetween;
            needMixerUpdate |= DoMuteButton(buttonRect, group, allGroups, anySoloActive, selection);
            if (needMixerUpdate)
                m_Controller.UpdateMuteSolo();
            buttonRect.x += buttonSize + spaceBetween;
            if (DoBypassEffectsButton(buttonRect, group, allGroups, selection))
                m_Controller.UpdateBypass();
        }

        public void OnMixerControllerChanged(AudioMixerController controller)
        {
            m_Controller = controller;
        }

        [System.NonSerialized]
        private int FrameCounter = 0;

        [System.NonSerialized]
        private GUIStyle developerInfoStyle = AudioMixerDrawUtils.BuildGUIStyleForLabel(new Color(1.0f, 0.0f, 0.0f, 0.5f), 20, false, FontStyle.Bold, TextAnchor.MiddleLeft);

        public void ShowDeveloperOverlays(Rect rect, Event evt, bool show)
        {
            if (show && Unsupported.IsDeveloperBuild() && evt.type == EventType.Repaint)
            {
                AudioMixerDrawUtils.ReadOnlyLabel(new Rect(rect.x + 5, rect.y + 5, rect.width - 10, 20), "Current snapshot: " + m_Controller.TargetSnapshot.name, developerInfoStyle);
                AudioMixerDrawUtils.ReadOnlyLabel(new Rect(rect.x + 5, rect.y + 25, rect.width - 10, 20), "Frame count: " + FrameCounter++, developerInfoStyle);
            }
        }

        public static float Lerp(float x1, float x2, float t)
        {
            return x1 + (x2 - x1) * t;
        }

        public static void GetCableVertex(float x1, float y1, float x2, float y2, float x3, float y3, float t, out float x, out float y)
        {
            x = Lerp(Lerp(x1, x2, t), Lerp(x2, x3, t), t);
            y = Lerp(Lerp(y1, y2, t), Lerp(y2, y3, t), t);
        }

        [System.NonSerialized]
        private Vector3[] cablepoints = new Vector3[20];

        public void OnGUI(Rect rect, bool showReferencedBuses, bool showBusConnections, bool showBusConnectionsOfSelection, List<AudioMixerGroupController> allGroups, Dictionary<AudioMixerEffectController, AudioMixerGroupController> effectMap, bool sortGroupsAlphabetically, bool showDeveloperOverlays, AudioMixerGroupController scrollToItem)
        {
            if (m_Controller == null)
            {
                DrawAreaBackground(rect);
                return;
            }

            if (Event.current.type == EventType.Layout)
                return;

            m_RectSelectionControlID = GUIUtility.GetControlID(kRectSelectionHashCode, FocusType.Passive);
            m_EffectInteractionControlID = GUIUtility.GetControlID(kEffectDraggingHashCode, FocusType.Passive);

            m_IndexCounter = 0;

            Event evt = Event.current;
            var sortedGroups = m_Controller.GetCurrentViewGroupList().ToList();
            if (sortGroupsAlphabetically)
                sortedGroups.Sort(m_GroupComparer);

            Rect baseChannelStripRect = new Rect(channelStripsOffset.x, channelStripsOffset.y, channelStripBaseWidth, 300);
            if (scrollToItem != null)
            {
                int index = sortedGroups.IndexOf(scrollToItem);
                if (index >= 0)
                {
                    float x = (baseChannelStripRect.width + channelStripSpacing) * index - m_State.m_ScrollPos.x;
                    if (x < -20 || x > rect.width)
                        m_State.m_ScrollPos.x += x;
                }
            }

            var unsortedBuses = new List<AudioMixerGroupController>();
            foreach (var c in sortedGroups)
            {
                foreach (var e in c.effects)
                {
                    if (e.sendTarget != null)
                    {
                        var targetGroup = effectMap[e.sendTarget];
                        if (!unsortedBuses.Contains(targetGroup) && !sortedGroups.Contains(targetGroup))
                            unsortedBuses.Add(targetGroup);
                    }
                }
            }
            List<AudioMixerGroupController> buses = unsortedBuses.ToList();
            buses.Sort(m_GroupComparer);

            // Show referenced buses after all sorted groups
            int numMainGroups = sortedGroups.Count;
            if (showReferencedBuses && buses.Count > 0)
                sortedGroups.AddRange(buses);

            int maxEffects = 1;
            foreach (var c in sortedGroups)
                maxEffects = Mathf.Max(maxEffects, c.effects.Length);

            bool isShowingReferencedGroups = sortedGroups.Count != numMainGroups;
            Rect contentRect = GetContentRect(sortedGroups, isShowingReferencedGroups, maxEffects);
            m_State.m_ScrollPos = GUI.BeginScrollView(rect, m_State.m_ScrollPos, contentRect);
            DrawAreaBackground(new Rect(0, 0, 10000, 10000));

            // Setup shared AudioGroup data
            var channelStripParams = new ChannelStripParams
            {
                effectMap = effectMap,
                allGroups = allGroups,
                shownGroups = sortedGroups,
                anySoloActive = allGroups.Any(g => g.solo),
                visibleRect = new Rect(m_State.m_ScrollPos.x, m_State.m_ScrollPos.y, rect.width, rect.height)
            };

            var patchslots = showBusConnections ? new Dictionary<AudioMixerEffectController, PatchSlot>() : null;
            for (int i = 0; i < sortedGroups.Count; ++i)
            {
                var group = sortedGroups[i];
                // Specific parameter data for current AudioGroup
                channelStripParams.index = i;
                channelStripParams.group = group;
                channelStripParams.drawingBuses = false;
                channelStripParams.visible = RectOverlaps(channelStripParams.visibleRect, baseChannelStripRect);
                channelStripParams.Init(m_Controller, baseChannelStripRect, maxEffects);

                DrawChannelStrip(channelStripParams, ref m_Controller.m_HighlightEffectIndex, ref patchslots, showBusConnectionsOfSelection);

                // If we click inside the the strip rect we use the event to ensure the mousedown is not caught by rectangle selection
                if (evt.type == EventType.MouseDown && evt.button == 0 && channelStripParams.stripRect.Contains(evt.mousePosition))
                    evt.Use();
                // If we are dragging effects we reset destination index when dragging outside effects
                if (IsMovingEffect() && evt.type == EventType.MouseDrag && channelStripParams.stripRect.Contains(evt.mousePosition) && GUIUtility.hotControl == m_EffectInteractionControlID)
                {
                    m_MovingEffectDstIndex = -1;
                    evt.Use();
                }
                baseChannelStripRect.x += channelStripParams.stripRect.width + channelStripSpacing;

                // Add extra space to referenced groups
                if (showReferencedBuses && i == numMainGroups - 1 && sortedGroups.Count > numMainGroups)
                {
                    baseChannelStripRect.x += 50f;

                    using (new EditorGUI.DisabledScope(true))
                    {
                        GUI.Label(new Rect(baseChannelStripRect.x, channelStripParams.stripRect.yMax, 130, 22), styles.referencedGroups, styles.channelStripHeaderStyle);
                    }
                }
            }
            UnhandledEffectDraggingEvents(ref m_Controller.m_HighlightEffectIndex);

            if (evt.type == EventType.Repaint && patchslots != null)
            {
                foreach (var c in patchslots)
                {
                    var slot = c.Value;
                    bool active = !showBusConnectionsOfSelection || m_Controller.CachedSelection.Contains(slot.group);
                    if (active)
                        styles.circularToggle.Draw(new Rect(slot.x - 3, slot.y - 3, 6, 6), false, false, active, false);
                }
                float moveamp = Mathf.Exp(-0.03f * Time.time * Time.time) * 0.1f;
                var trCol1 = new Color(0.0f, 0.0f, 0.0f, AudioMixerController.EditingTargetSnapshot() ? 0.1f : 0.05f);
                var trCol2 = new Color(0.0f, 0.0f, 0.0f, AudioMixerController.EditingTargetSnapshot() ? 1.0f : 0.5f);
                foreach (var c in patchslots)
                {
                    var sourceEffect = c.Key;
                    var targetEffect = sourceEffect.sendTarget;
                    if (targetEffect == null)
                        continue;
                    var sourceSlot = c.Value;
                    if (!patchslots.ContainsKey(targetEffect))
                        continue;
                    var targetSlot = patchslots[targetEffect];
                    int color = sourceEffect.GetInstanceID() ^ targetEffect.GetInstanceID();
                    float phase = (color & 63) * 0.1f;
                    float mx1 = Mathf.Abs(targetSlot.x - sourceSlot.x) * Mathf.Sin(Time.time * 5.0f + phase) * moveamp + (sourceSlot.x + targetSlot.x) * 0.5f;
                    float my1 = Mathf.Abs(targetSlot.y - sourceSlot.y) * Mathf.Cos(Time.time * 5.0f + phase) * moveamp + Math.Max(sourceSlot.y, targetSlot.y) + Mathf.Max(Mathf.Min(0.5f * Math.Abs(targetSlot.y - sourceSlot.y), 50.0f), 50.0f);
                    for (int i = 0; i < cablepoints.Length; i++)
                        GetCableVertex(
                            sourceSlot.x, sourceSlot.y,
                            mx1, my1,
                            targetSlot.x, targetSlot.y,
                            (float)i / (float)(cablepoints.Length - 1),
                            out cablepoints[i].x, out cablepoints[i].y);
                    bool skip = showBusConnectionsOfSelection && !m_Controller.CachedSelection.Contains(sourceSlot.group) && !m_Controller.CachedSelection.Contains(targetSlot.group);
                    Handles.color = skip ? trCol1 : trCol2;
                    Handles.DrawAAPolyLine(7.0f, cablepoints.Length, cablepoints);
                    if (skip)
                        continue;
                    color ^= (color >> 6) ^ (color >> 12) ^ (color >> 18);
                    Handles.color =
                        new Color(
                            (color       & 3) * 0.15f + 0.55f,
                            ((color >> 2) & 3) * 0.15f + 0.55f,
                            ((color >> 4) & 3) * 0.15f + 0.55f,
                            AudioMixerController.EditingTargetSnapshot() ? 1.0f : 0.5f);
                    Handles.DrawAAPolyLine(4.0f, cablepoints.Length, cablepoints);
                    Handles.color = new Color(1.0f, 1.0f, 1.0f, AudioMixerController.EditingTargetSnapshot() ? 0.5f : 0.25f);
                    Handles.DrawAAPolyLine(3.0f, cablepoints.Length, cablepoints);
                }
            }

            RectSelection(channelStripParams);

            GUI.EndScrollView(true);
            AudioMixerDrawUtils.DrawScrollDropShadow(rect, m_State.m_ScrollPos.y, contentRect.height);
            WarningOverlay(allGroups, rect, contentRect);
            ShowDeveloperOverlays(rect, evt, showDeveloperOverlays);

            if (!EditorApplication.isPlaying && !m_Controller.isSuspended)
                m_RequiresRepaint = true;
        }

        bool IsMovingEffect()
        {
            return m_MovingEffectSrcIndex != -1;
        }

        bool IsAdjustingWetMix()
        {
            return m_ChangingWetMixIndex != -1;
        }

        void RectSelection(ChannelStripParams channelStripParams)
        {
            Event evt = Event.current;

            if (evt.type == EventType.MouseDown && evt.button == 0 && GUIUtility.hotControl == 0)
            {
                if (!evt.shift)
                {
                    Selection.objects = new UnityEngine.Object[0];
                    m_Controller.OnUnitySelectionChanged();
                }
                GUIUtility.hotControl = m_RectSelectionControlID;

                m_RectSelectionStartPos = evt.mousePosition;
                m_RectSelectionRect = new Rect(m_RectSelectionStartPos.x, m_RectSelectionStartPos.y, 0, 0);
                Event.current.Use();
                InspectorWindow.RepaintAllInspectors();
            }

            if (evt.type == EventType.MouseDrag)
            {
                if (IsRectSelectionActive())
                {
                    m_RectSelectionRect.x = Mathf.Min(m_RectSelectionStartPos.x, evt.mousePosition.x);
                    m_RectSelectionRect.y = Mathf.Min(m_RectSelectionStartPos.y, evt.mousePosition.y);
                    m_RectSelectionRect.width = Mathf.Max(m_RectSelectionStartPos.x, evt.mousePosition.x) - m_RectSelectionRect.x;
                    m_RectSelectionRect.height = Mathf.Max(m_RectSelectionStartPos.y, evt.mousePosition.y) - m_RectSelectionRect.y;
                    Event.current.Use();
                }

                if (m_MovingSrcRect.y >= 0.0f)
                    Event.current.Use();
            }

            if (IsRectSelectionActive() && evt.GetTypeForControl(m_RectSelectionControlID) == EventType.MouseUp)
            {
                var newChannelStripSelection = (evt.shift) ? m_Controller.CachedSelection : new List<AudioMixerGroupController>();
                foreach (var g in channelStripParams.rectSelectionGroups)
                    if (!newChannelStripSelection.Contains(g))
                        newChannelStripSelection.Add(g);
                Selection.objects = newChannelStripSelection.ToArray();
                m_Controller.OnUnitySelectionChanged();

                GUIUtility.hotControl = 0;
                Event.current.Use();
                //InspectorWindow.RepaintAllInspectors();
            }

            if (evt.type == EventType.Repaint)
            {
                if (IsRectSelectionActive())
                {
                    Color rectSelCol = new Color(1, 1, 1, 0.3f);
                    AudioMixerDrawUtils.DrawGradientRectHorizontal(m_RectSelectionRect, rectSelCol, rectSelCol);
                }
            }
        }

        void WarningOverlay(List<AudioMixerGroupController> allGroups, Rect rect, Rect contentRect)
        {
            int numSoloed = 0, numMuted = 0, numBypassEffects = 0;
            foreach (var g in allGroups)
            {
                if (g.solo)
                    numSoloed++;
                if (g.mute)
                    numMuted++;
                if (g.bypassEffects)
                    numBypassEffects += g.effects.Length - 1; // one of the effects is "Attenuation"
                else
                    numBypassEffects += g.effects.Count(e => e.bypass);
            }

            Event evt = Event.current;

            // Warning overlay
            if (evt.type == EventType.Repaint && numSoloed > 0 || numMuted > 0 || numBypassEffects > 0)
            {
                string warningStr = "Warning: " + (
                        (numSoloed > 0) ? (numSoloed + (numSoloed > 1 ? " groups" : " group") + " currently soloed") :
                        (numMuted > 0) ? (numMuted + (numMuted > 1 ? " groups" : " group") + " currently muted") :
                        (numBypassEffects + (numBypassEffects > 1 ? " effects" : " effect") + " currently bypassed")
                        );

                //bool verticalScrollbar = contentRect.height > rect.height;
                bool horizontalScrollbar = contentRect.width > rect.width;
                const float margin = 5f;
                float textwidth = styles.warningOverlay.CalcSize(GUIContent.Temp(warningStr)).x;
                Rect warningRect =
                    new Rect(rect.x + margin + Mathf.Max((rect.width - 2 * margin - textwidth) * 0.5f, 0f),
                        rect.yMax - styles.warningOverlay.fixedHeight - margin - (horizontalScrollbar ? 17f : 0f),
                        textwidth,
                        styles.warningOverlay.fixedHeight);

                GUI.Label(warningRect, GUIContent.Temp(warningStr), styles.warningOverlay);
            }
        }

        Rect GetContentRect(List<AudioMixerGroupController> sortedGroups, bool isShowingReferencedGroups, int maxEffects)
        {
            float fixedHeight = headerHeight + vuHeight + dbHeight + soloMuteBypassHeight;
            float maxHeight = channelStripsOffset.y + fixedHeight + (maxEffects * effectHeight) + 10 + effectHeight + 10;

            float maxWidth = channelStripsOffset.x * 2 + (channelStripBaseWidth + channelStripSpacing) * sortedGroups.Count + (isShowingReferencedGroups ? spaceBetweenMainGroupsAndReferenced : 0f);
            return new Rect(0, 0, maxWidth, maxHeight);
        }
    }
}
