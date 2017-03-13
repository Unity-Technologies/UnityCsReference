// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.Audio;

namespace UnityEditor
{
    internal static class AudioMixerEffectGUI
    {
        const string kAudioSliderFloatFormat = "F2";
        const string kExposedParameterUnicodeChar = " \u2794";

        public static void EffectHeader(string text)
        {
            GUILayout.Label(text, styles.headerStyle);
        }

        public static bool Slider(GUIContent label, ref float value, float displayScale, float displayExponent, string unit, float leftValue, float rightValue, AudioMixerController controller, AudioParameterPath path, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();

            float oldNumberWidth = EditorGUIUtility.fieldWidth;
            string origFormat = EditorGUI.kFloatFieldFormatString;

            bool exposed = controller.ContainsExposedParameter(path.parameter);

            EditorGUIUtility.fieldWidth = 70f;     // do not go over 70 because then sliders will not be shown when inspector has minimal width
            EditorGUI.kFloatFieldFormatString = kAudioSliderFloatFormat;
            EditorGUI.s_UnitString = unit;
            GUIContent content = label;
            if (exposed)
                content = GUIContent.Temp(label.text + kExposedParameterUnicodeChar, label.tooltip);

            float displayValue = value * displayScale;
            displayValue = EditorGUILayout.PowerSlider(content, displayValue, leftValue * displayScale, rightValue * displayScale, displayExponent, options);

            EditorGUI.s_UnitString = null;
            EditorGUI.kFloatFieldFormatString = origFormat;
            EditorGUIUtility.fieldWidth = oldNumberWidth;

            if (Event.current.type == EventType.ContextClick)
            {
                Rect wholeSlider = GUILayoutUtility.topLevel.GetLast();
                if (wholeSlider.Contains(Event.current.mousePosition))
                {
                    Event.current.Use();

                    GenericMenu pm = new GenericMenu();
                    if (!exposed)
                        pm.AddItem(new GUIContent("Expose '" + path.ResolveStringPath(false) + "' to script"), false, ExposePopupCallback, new ExposedParamContext(controller, path));
                    else
                        pm.AddItem(new GUIContent("Unexpose"), false, UnexposePopupCallback, new ExposedParamContext(controller, path));

                    ParameterTransitionType existingType;
                    bool overrideExists = controller.TargetSnapshot.GetTransitionTypeOverride(path.parameter, out existingType);
                    System.Diagnostics.Debug.Assert(!overrideExists || existingType == ParameterTransitionType.Lerp);

                    pm.AddSeparator(string.Empty);
                    pm.AddItem(new GUIContent("Linear Snapshot Transition"), existingType == ParameterTransitionType.Lerp, ParameterTransitionOverrideCallback, new ParameterTransitionOverrideContext(controller, path.parameter, ParameterTransitionType.Lerp));
                    pm.AddItem(new GUIContent("Smoothstep Snapshot Transition"), existingType == ParameterTransitionType.Smoothstep, ParameterTransitionOverrideCallback, new ParameterTransitionOverrideContext(controller, path.parameter, ParameterTransitionType.Smoothstep));
                    pm.AddItem(new GUIContent("Squared Snapshot Transition"), existingType == ParameterTransitionType.Squared, ParameterTransitionOverrideCallback, new ParameterTransitionOverrideContext(controller, path.parameter, ParameterTransitionType.Squared));
                    pm.AddItem(new GUIContent("SquareRoot Snapshot Transition"), existingType == ParameterTransitionType.SquareRoot, ParameterTransitionOverrideCallback, new ParameterTransitionOverrideContext(controller, path.parameter, ParameterTransitionType.SquareRoot));
                    pm.AddItem(new GUIContent("BrickwallStart Snapshot Transition"), existingType == ParameterTransitionType.BrickwallStart, ParameterTransitionOverrideCallback, new ParameterTransitionOverrideContext(controller, path.parameter, ParameterTransitionType.BrickwallStart));
                    pm.AddItem(new GUIContent("BrickwallEnd Snapshot Transition"), existingType == ParameterTransitionType.BrickwallEnd, ParameterTransitionOverrideCallback, new ParameterTransitionOverrideContext(controller, path.parameter, ParameterTransitionType.BrickwallEnd));
                    pm.AddSeparator(string.Empty);

                    pm.ShowAsContext();
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                value = displayValue / displayScale;
                return true;
            }
            return false;
        }

        private class ExposedParamContext
        {
            public ExposedParamContext(AudioMixerController controller, AudioParameterPath path)
            {
                this.controller = controller;
                this.path = path;
            }

            public AudioMixerController controller;
            public AudioParameterPath path;
        }


        public static void ExposePopupCallback(object obj)
        {
            ExposedParamContext context = (ExposedParamContext)obj;
            Undo.RecordObject(context.controller, "Expose Mixer Parameter");
            context.controller.AddExposedParameter(context.path);

            AudioMixerUtility.RepaintAudioMixerAndInspectors();
        }

        public static void UnexposePopupCallback(object obj)
        {
            ExposedParamContext context = (ExposedParamContext)obj;
            Undo.RecordObject(context.controller, "Unexpose Mixer Parameter");
            context.controller.RemoveExposedParameter(context.path.parameter);

            AudioMixerUtility.RepaintAudioMixerAndInspectors();
        }

        private class ParameterTransitionOverrideContext
        {
            public ParameterTransitionOverrideContext(AudioMixerController controller, GUID parameter, ParameterTransitionType type)
            {
                this.controller = controller;
                this.parameter = parameter;
                this.type = type;
            }

            public AudioMixerController     controller;
            public GUID                     parameter;
            public ParameterTransitionType  type;
        }

        private class ParameterTransitionOverrideRemoveContext
        {
            public ParameterTransitionOverrideRemoveContext(AudioMixerController controller, GUID parameter)
            {
                this.controller = controller;
                this.parameter = parameter;
            }

            public AudioMixerController     controller;
            public GUID                     parameter;
        }

        public static void ParameterTransitionOverrideCallback(object obj)
        {
            ParameterTransitionOverrideContext context = (ParameterTransitionOverrideContext)obj;
            Undo.RecordObject(context.controller, "Change Parameter Transition Type");
            if (context.type == ParameterTransitionType.Lerp)
                context.controller.TargetSnapshot.ClearTransitionTypeOverride(context.parameter);
            else
                context.controller.TargetSnapshot.SetTransitionTypeOverride(context.parameter, context.type);
        }

        public static bool PopupButton(GUIContent label, GUIContent buttonContent, GUIStyle style, out Rect buttonRect, params GUILayoutOption[] options)
        {
            if (label != null)
            {
                Rect r = EditorGUILayout.s_LastRect = EditorGUILayout.GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
                int id = EditorGUIUtility.GetControlID("EditorPopup".GetHashCode(), FocusType.Keyboard, r);
                buttonRect = EditorGUI.PrefixLabel(r, id, label);
            }
            else
            {
                Rect r = GUILayoutUtility.GetRect(buttonContent, style, options);
                buttonRect = r;
            }

            return EditorGUI.DropdownButton(buttonRect, buttonContent, FocusType.Passive, style);
        }

        private static AudioMixerDrawUtils.Styles styles { get { return AudioMixerDrawUtils.styles; } }
    }
}
