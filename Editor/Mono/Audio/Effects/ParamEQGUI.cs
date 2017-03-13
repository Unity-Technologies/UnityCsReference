// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;

namespace UnityEditor
{
    internal class ParamEqGUI : IAudioEffectPluginGUI
    {
        public static string kCenterFreqName = "Center freq";
        public static string kOctaveRangeName = "Octave range";
        public static string kFrequencyGainName = "Frequency gain";

        public override string Name
        {
            get { return "ParamEQ"; }
        }

        public override string Description
        {
            get { return "Parametric equalizer"; }
        }

        public override string Vendor
        {
            get { return "Firelight Technologies"; }
        }

        const bool useLogScale = true;

        public static GUIStyle BuildGUIStyleForLabel(Color color, int fontSize, bool wrapText, FontStyle fontstyle, TextAnchor anchor)
        {
            GUIStyle style = new GUIStyle();
            style.focused.background = style.onNormal.background;
            style.focused.textColor = color;
            style.alignment = anchor;
            style.fontSize = fontSize;
            style.fontStyle = fontstyle;
            style.wordWrap = wrapText;
            style.clipping = TextClipping.Overflow;
            style.normal.textColor = color;
            return style;
        }

        public static GUIStyle textStyle10 = BuildGUIStyleForLabel(Color.grey, 10, false, FontStyle.Normal, TextAnchor.MiddleCenter);

        private static void DrawFrequencyTickMarks(Rect r, float samplerate, bool logScale, Color col)
        {
            textStyle10.normal.textColor = col;
            float px = r.x, w = 60.0f;
            for (float normFreq = 0; normFreq < 1.0f; normFreq += 0.01f)
            {
                float f = (float)MapNormalizedFrequency(normFreq, samplerate, logScale, true);
                float x = r.x + normFreq * r.width;
                if (x - px > w)
                {
                    EditorGUI.DrawRect(new Rect(x, r.yMax - 5f, 1f, 5f), col);
                    GUI.Label(new Rect(x, r.yMax - 22f, 1, 15f), (f < 1000.0f) ? string.Format("{0:F0} Hz", f) : string.Format("{0:F0} kHz", f * 0.001f), textStyle10);
                    px = x;
                }
            }
        }

        protected static Color ScaleAlpha(Color col, float blend)
        {
            return new Color(col.r, col.g, col.b, col.a * blend);
        }

        // Maps from normalized frequency to real frequency
        private static double MapNormalizedFrequency(double f, double sr, bool useLogScale, bool forward)
        {
            double maxFreq = 0.5 * sr;
            if (useLogScale)
            {
                const double lowestFreq = 10.0;
                if (forward)
                    return lowestFreq * Math.Pow(maxFreq / lowestFreq, f);
                return Math.Log(f / lowestFreq) / Math.Log(maxFreq / lowestFreq);
            }
            return (forward) ? (f * maxFreq) : (f / maxFreq);
        }

        static bool ParamEqualizerCurveEditor(IAudioEffectPlugin plugin, Rect r, ref float centerFreq, ref float bandwidth, ref float gain, float blend)
        {
            Event evt = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            r = AudioCurveRendering.BeginCurveFrame(r);

            float minCenterFreq, maxCenterFreq, defCenterFreq; plugin.GetFloatParameterInfo(kCenterFreqName, out minCenterFreq, out maxCenterFreq, out defCenterFreq);
            float minOctaveRange, maxOctaveRange, defOctaveRange; plugin.GetFloatParameterInfo(kOctaveRangeName, out minOctaveRange, out maxOctaveRange, out defOctaveRange);
            float minGain, maxGain, defGain; plugin.GetFloatParameterInfo(kFrequencyGainName, out minGain, out maxGain, out defGain);

            bool modifiedValue = false;
            switch (evt.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    if (r.Contains(Event.current.mousePosition) && evt.button == 0)
                    {
                        GUIUtility.hotControl = controlID;
                        EditorGUIUtility.SetWantsMouseJumping(1);
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID && evt.button == 0)
                    {
                        GUIUtility.hotControl = 0;
                        EditorGUIUtility.SetWantsMouseJumping(0);
                        evt.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        float dragAcceleration = Event.current.alt ? .25f : 1f;
                        centerFreq = Mathf.Clamp((float)MapNormalizedFrequency(MapNormalizedFrequency(centerFreq, plugin.GetSampleRate(), useLogScale, false) + evt.delta.x / r.width, plugin.GetSampleRate(), useLogScale, true), minCenterFreq, maxCenterFreq);
                        if (Event.current.shift)
                            bandwidth = Mathf.Clamp(bandwidth - evt.delta.y * 0.02f * dragAcceleration, minOctaveRange, maxOctaveRange);
                        else
                            gain = Mathf.Clamp(gain - evt.delta.y * 0.01f * dragAcceleration, minGain, maxGain);
                        modifiedValue = true;
                        evt.Use();
                    }
                    break;
            }

            if (Event.current.type == EventType.Repaint)
            {
                // Mark CenterFreq with a vertical line
                float c = (float)MapNormalizedFrequency(centerFreq, plugin.GetSampleRate(), useLogScale, false);
                EditorGUI.DrawRect(new Rect(c * r.width + r.x, r.y, 1f, r.height), GUIUtility.hotControl == controlID ? new Color(0.6f, 0.6f, 0.6f) : new Color(0.4f, 0.4f, 0.4f));

                // Curve
                HandleUtility.ApplyWireMaterial();
                double kPI = 3.1415926;
                double wm = -2.0f * kPI / plugin.GetSampleRate();
                double w0 = 2.0 * kPI * centerFreq / plugin.GetSampleRate();
                double Q = 1.0 / bandwidth;
                double A = gain;
                double alpha = Math.Sin(w0) / (2.0 * Q);
                double b0 =  1.0 + alpha * A;
                double b1 = -2.0 * Math.Cos(w0);
                double b2 =  1.0 - alpha * A;
                double a0 =  1.0 + alpha / A;
                double a1 = -2.0 * Math.Cos(w0);
                double a2 =  1.0 - alpha / A;
                AudioCurveRendering.DrawCurve(
                    r,
                    delegate(float x)
                    {
                        double f = MapNormalizedFrequency((double)x, plugin.GetSampleRate(), useLogScale, true);
                        ComplexD w = ComplexD.Exp(wm * f);
                        ComplexD n = w * (w * b2 + b1) + b0;
                        ComplexD d = w * (w * a2 + a1) + a0;
                        ComplexD h = n / d;
                        double mag = Math.Log10(h.Mag2());
                        return (float)(0.5 * mag); // 20 dB range
                    },
                    ScaleAlpha(AudioCurveRendering.kAudioOrange, blend)
                    );
            }

            DrawFrequencyTickMarks(r, plugin.GetSampleRate(), useLogScale, new Color(1.0f, 1.0f, 1.0f, 0.3f * blend));

            AudioCurveRendering.EndCurveFrame();

            return modifiedValue;
        }

        public override bool OnGUI(IAudioEffectPlugin plugin)
        {
            float blend = plugin.IsPluginEditableAndEnabled() ? 1.0f : 0.5f;

            float centerFreq, octaveRange, frequencyGain;
            plugin.GetFloatParameter(kCenterFreqName, out centerFreq);
            plugin.GetFloatParameter(kOctaveRangeName, out octaveRange);
            plugin.GetFloatParameter(kFrequencyGainName, out frequencyGain);

            GUILayout.Space(5f);
            Rect r = GUILayoutUtility.GetRect(200, 100, GUILayout.ExpandWidth(true));
            if (ParamEqualizerCurveEditor(plugin, r, ref centerFreq, ref octaveRange, ref frequencyGain, blend))
            {
                plugin.SetFloatParameter(kCenterFreqName, centerFreq);
                plugin.SetFloatParameter(kOctaveRangeName, octaveRange);
                plugin.SetFloatParameter(kFrequencyGainName, frequencyGain);
            }

            return true;
        }
    }
}
