// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define DEBUG_PLOT_GAIN

using UnityEngine;

namespace UnityEditor
{
    internal class DuckVolumeGUI : IAudioEffectPluginGUI
    {
        public static string kThresholdName = "Threshold";
        public static string kRatioName = "Ratio";
        public static string kMakeupGainName = "Make-up Gain";
        public static string kAttackTimeName = "Attack Time";
        public static string kReleaseTimeName = "Release Time";
        public static string kKneeName = "Knee";

        public override string Name
        {
            get { return "Duck Volume"; }
        }

        public override string Description
        {
            get { return "Volume Ducking"; }
        }

        public override string Vendor
        {
            get { return "Unity Technologies"; }
        }

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

        public static GUIStyle textStyle10 = BuildGUIStyleForLabel(Color.grey, 10, false, FontStyle.Normal, TextAnchor.MiddleLeft);

        public static void DrawText(float x, float y, string text)
        {
            GUI.Label(new Rect(x, y - 5, 200, 10), new GUIContent(text, ""), textStyle10);
        }

        public static void DrawLine(float x1, float y1, float x2, float y2, Color col)
        {
            Handles.color = col;
            Handles.DrawLine(new Vector3(x1, y1, 0), new Vector3(x2, y2, 0));
        }

        public enum DragType
        {
            None,
            ThresholdAndKnee,
            Ratio,
            MakeupGain,
        };

        private static DragType dragtype = DragType.None;

        protected static Color ScaleAlpha(Color col, float blend)
        {
            return new Color(col.r, col.g, col.b, col.a * blend);
        }

        protected static void DrawVU(Rect r, float level, float blend, bool topdown)
        {
            level = 1.0f - level;
            const float border = 1;
            var r2 = new Rect(r.x + border, r.y + border + (topdown ? 0.0f : level * r.height), r.width - 2 * border, r.y - 2 * border + (topdown ? level * r.height : r.height - level * r.height));
            AudioMixerDrawUtils.DrawRect(r, new Color(0.1f, 0.1f, 0.1f));
            AudioMixerDrawUtils.DrawRect(r2, new Color(0.6f, 0.2f, 0.2f));
        }

        // Input: normalized x value [0;1]. Returns: normalized y value [-1;-1]
        static float EvaluateDuckingVolume(float x, float ratio, float threshold, float makeupGain, float knee, float dbRange, float dbMin)
        {
            float duckGradient = 1.0f / ratio;
            float duckThreshold = threshold;
            float duckMakeupGain = makeupGain;
            float duckKnee = knee;
            float duckKneeC1 = (knee > 0.0f) ? ((duckGradient - 1.0f) / (4.0f * knee)) : 0.0f;
            float duckKneeC2 = duckThreshold - knee;

            float level = x * dbRange + dbMin;
            float gain = level;
            float t = level - duckThreshold;
            if (t > -duckKnee && t < duckKnee)
            {
                t += duckKnee;
                gain = t * (duckKneeC1 * t + 1.0f) + duckKneeC2;
            }
            else if (t > 0.0f)
                gain = duckThreshold + duckGradient * t;
            return (2.0f * (gain + duckMakeupGain - dbMin) / dbRange) - 1.0f;
        }

        static bool CurveDisplay(IAudioEffectPlugin plugin, Rect r0, ref float threshold, ref float ratio, ref float makeupGain, ref float attackTime, ref float releaseTime, ref float knee, float sidechainLevel, float outputLevel, float blend)
        {
            Event evt = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            Rect r = AudioCurveRendering.BeginCurveFrame(r0);

            const float thresholdActiveWidth = 10f;
            float vuWidth = 10f;

            float minThreshold, maxThreshold, defThreshold; plugin.GetFloatParameterInfo(kThresholdName, out minThreshold, out maxThreshold, out defThreshold);
            float minRatio, maxRatio, defRatio; plugin.GetFloatParameterInfo(kRatioName, out minRatio, out maxRatio, out defRatio);
            float minMakeupGain, maxMakeupGain, defMakeupGain; plugin.GetFloatParameterInfo(kMakeupGainName, out minMakeupGain, out maxMakeupGain, out defMakeupGain);
            float minKnee, maxKnee, defKnee; plugin.GetFloatParameterInfo(kKneeName, out minKnee, out maxKnee, out defKnee);

            float dbRange = 100.0f, dbMin = -80.0f;
            float thresholdPosX = r.width * (threshold - dbMin) / dbRange;

            bool modifiedValue = false;
            switch (evt.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    if (r.Contains(Event.current.mousePosition) && evt.button == 0)
                    {
                        dragtype = DragType.None;
                        GUIUtility.hotControl = controlID;
                        EditorGUIUtility.SetWantsMouseJumping(1);
                        evt.Use();

                        // Ensure visible state change on mousedown to make it clear that interaction is possible
                        if ((Mathf.Abs(r.x + thresholdPosX - evt.mousePosition.x) >= thresholdActiveWidth))
                            dragtype = (evt.mousePosition.x < r.x + thresholdPosX) ? DragType.MakeupGain : DragType.Ratio;
                        else
                            dragtype = DragType.ThresholdAndKnee;
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID && evt.button == 0)
                    {
                        dragtype = DragType.None;
                        GUIUtility.hotControl = 0;
                        EditorGUIUtility.SetWantsMouseJumping(0);
                        evt.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        float dragAcceleration = evt.alt ? .25f : 1f;
                        if (dragtype == DragType.ThresholdAndKnee)
                        {
                            bool dragKnee = Mathf.Abs(evt.delta.x) < Mathf.Abs(evt.delta.y);
                            if (dragKnee)
                                knee = Mathf.Clamp(knee + evt.delta.y * 0.5f * dragAcceleration, minKnee, maxKnee);
                            else
                                threshold = Mathf.Clamp(threshold + evt.delta.x * 0.1f * dragAcceleration, minThreshold, maxThreshold);
                        }
                        else if (dragtype == DragType.Ratio)
                            ratio = Mathf.Clamp(ratio + evt.delta.y * (ratio > 1.0f ? 0.05f : 0.003f) * dragAcceleration, minRatio, maxRatio);
                        else if (dragtype == DragType.MakeupGain)
                            makeupGain = Mathf.Clamp(makeupGain - evt.delta.y * 0.5f * dragAcceleration, minMakeupGain, maxMakeupGain);
                        else
                        {
                            Debug.LogError("Drag: Unhandled enum");
                        }

                        modifiedValue = true;
                        evt.Use();
                    }
                    break;
            }

            if (evt.type == EventType.Repaint)
            {
                // Curve
                HandleUtility.ApplyWireMaterial();

                //float sidechainPosX = r.width * (sidechainLevel - dbMin) / dbRange;
                float thresholdPosY = r.height * (1.0f - ((threshold - dbMin + makeupGain) / dbRange));
                Color thresholdColor = new Color(0.7f, 0.7f, 0.7f);
                Color sidechainColor = Color.black;


                float duckGradient = 1.0f / ratio;
                float duckThreshold = threshold;
                float duckSidechainLevel = sidechainLevel;
                float duckMakeupGain = makeupGain;
                float duckKnee = knee;
                float duckKneeC1 = (knee > 0.0f) ? ((duckGradient - 1.0f) / (4.0f * knee)) : 0.0f;
                float duckKneeC2 = duckThreshold - knee;

                // Main filled curve
                AudioCurveRendering.DrawFilledCurve(
                    r,
                    delegate(float x, out Color col)
                    {
                        float level = x * dbRange + dbMin;
                        float gain = level;
                        float t = level - duckThreshold;
                        col = ScaleAlpha(duckSidechainLevel > level ? AudioCurveRendering.kAudioOrange : Color.grey, blend);
                        if (t > -duckKnee && t < duckKnee)
                        {
                            t += duckKnee;
                            gain = t * (duckKneeC1 * t + 1.0f) + duckKneeC2;

                            if (dragtype == DragType.ThresholdAndKnee)
                            {
                                const float mult = 1.2f;
                                col = new Color(col.r * mult, col.g * mult, col.b * mult);
                            }
                        }
                        else if (t > 0.0f)
                            gain = duckThreshold + duckGradient * t;
                        return (2.0f * (gain + duckMakeupGain - dbMin) / dbRange) - 1.0f;
                    }
                    );

                // Curve shown when modifying MakeupGain
                if (dragtype == DragType.MakeupGain)
                {
                    AudioCurveRendering.DrawCurve(
                        r,
                        delegate(float x)
                        {
                            float level = x * dbRange + dbMin;
                            float gain = level;
                            float t = level - duckThreshold;
                            if (t > -duckKnee && t < duckKnee)
                            {
                                t += duckKnee;
                                gain = t * (duckKneeC1 * t + 1.0f) + duckKneeC2;
                            }
                            else if (t > 0.0f)
                                gain = duckThreshold + duckGradient * t;
                            return (2.0f * (gain + duckMakeupGain - dbMin) / dbRange) - 1.0f;
                        },
                        Color.white
                        );
                }


                // Threshold text and line
                textStyle10.normal.textColor = ScaleAlpha(thresholdColor, blend);
                EditorGUI.DrawRect(new Rect(r.x + thresholdPosX, r.y, 1, r.height), textStyle10.normal.textColor);
                DrawText(r.x + thresholdPosX + 4, r.y + 6, string.Format("Threshold: {0:F1} dB", threshold));

                // Sidechain text and line
                textStyle10.normal.textColor = ScaleAlpha(sidechainColor, blend);
                DrawText(r.x + 4, r.y + r.height - 10, sidechainLevel < -80 ? "Input: None" : string.Format("Input: {0:F1} dB", sidechainLevel));

                if (dragtype == DragType.Ratio)
                {
                    float aspect = (float)r.height / (float)r.width;
                    Handles.DrawAAPolyLine(2.0f,
                        new Color[] { Color.black, Color.black },
                        new Vector3[]
                    {
                        new Vector3(r.x + thresholdPosX + r.width, r.y + thresholdPosY - aspect * r.width, 0.0f),
                        new Vector3(r.x + thresholdPosX - r.width, r.y + thresholdPosY + aspect * r.width, 0.0f)
                    });
                    Handles.DrawAAPolyLine(3.0f,
                        new Color[] { Color.white, Color.white },
                        new Vector3[]
                    {
                        new Vector3(r.x + thresholdPosX + r.width, r.y + thresholdPosY - aspect * duckGradient * r.width, 0.0f),
                        new Vector3(r.x + thresholdPosX - r.width, r.y + thresholdPosY + aspect * duckGradient * r.width, 0.0f)
                    });
                }
                else if (dragtype == DragType.ThresholdAndKnee)
                {
                    // Knee min and max lines
                    float normalizedKnee1 = (threshold - knee - dbMin) / dbRange;
                    float normalizedKnee2 = (threshold + knee - dbMin) / dbRange;
                    float y1 = EvaluateDuckingVolume(normalizedKnee1, ratio, threshold, makeupGain, knee, dbRange, dbMin);
                    float y2 = EvaluateDuckingVolume(normalizedKnee2, ratio, threshold, makeupGain, knee, dbRange, dbMin);
                    float knee1PosY = r.yMax - (y1 + 1f) * 0.5f * r.height;
                    float knee2PosY = r.yMax - (y2 + 1f) * 0.5f * r.height;
                    EditorGUI.DrawRect(new Rect(r.x + normalizedKnee1 * r.width, knee1PosY, 1, r.height - knee1PosY), new Color(0, 0, 0, 0.5f));
                    EditorGUI.DrawRect(new Rect(r.x + normalizedKnee2 * r.width - 1, knee2PosY, 1, r.height - knee2PosY), new Color(0, 0, 0, 0.5f));

                    // Enhanced threshold
                    EditorGUI.DrawRect(new Rect(r.x + thresholdPosX - 1, r.y, 3, r.height), Color.white);
                }

                outputLevel = (Mathf.Clamp(outputLevel - makeupGain, dbMin, dbMin + dbRange) - dbMin) / dbRange;
                if (EditorApplication.isPlaying)
                {
                    const int margin = 2;
                    Rect vuRect = new Rect(r.x + r.width - vuWidth + margin, r.y + margin, vuWidth - 2 * margin, r.height - 2 * margin);
                    DrawVU(vuRect, outputLevel, blend, true);
                }
            }

            AudioCurveRendering.EndCurveFrame();

            return modifiedValue;
        }

        public override bool OnGUI(IAudioEffectPlugin plugin)
        {
            float blend = plugin.IsPluginEditableAndEnabled() ? 1.0f : 0.5f;

            float threshold, ratio, makeupGain, attackTime, releaseTime, knee;
            plugin.GetFloatParameter(kThresholdName, out threshold);
            plugin.GetFloatParameter(kRatioName, out ratio);
            plugin.GetFloatParameter(kMakeupGainName, out makeupGain);
            plugin.GetFloatParameter(kAttackTimeName, out attackTime);
            plugin.GetFloatParameter(kReleaseTimeName, out releaseTime);
            plugin.GetFloatParameter(kKneeName, out knee);

            float[] metering; plugin.GetFloatBuffer("Metering", out metering, 2);

            GUILayout.Space(5f);
            Rect r = GUILayoutUtility.GetRect(200, 160, GUILayout.ExpandWidth(true));
            if (CurveDisplay(plugin, r, ref threshold, ref ratio, ref makeupGain, ref attackTime, ref releaseTime, ref knee, metering[0], metering[1], blend))
            {
                plugin.SetFloatParameter(kThresholdName, threshold);
                plugin.SetFloatParameter(kRatioName, ratio);
                plugin.SetFloatParameter(kMakeupGainName, makeupGain);
                plugin.SetFloatParameter(kAttackTimeName, attackTime);
                plugin.SetFloatParameter(kReleaseTimeName, releaseTime);
                plugin.SetFloatParameter(kKneeName, knee);
            }

            return true;
        }
    }
}
