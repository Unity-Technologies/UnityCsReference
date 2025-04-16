// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    internal class AudioFilterGUI
    {
        private EditorGUI.VUMeter.SmoothingData[] dataOut;

        public void DrawAudioFilterGUI(MonoBehaviour behaviour)
        {
            int channelCount = AudioUtil.GetCustomFilterChannelCount(behaviour);

            if (channelCount > 0)
            {
                if (dataOut == null)
                {
                    dataOut = new EditorGUI.VUMeter.SmoothingData[channelCount];
                }

                double ms = (double)AudioUtil.GetCustomFilterProcessTime(behaviour) / 1000000.0; // ms
                float limit = (float)ms / ((float)AudioSettings.outputSampleRate / 1024.0f / (float)channelCount);

                GUILayout.BeginHorizontal();
                GUILayout.Space(13);
                GUILayout.BeginVertical();
                EditorGUILayout.Space();
                for (int c = 0; c < channelCount; ++c)
                {
                    EditorGUILayout.VUMeterHorizontal(AudioUtil.GetCustomFilterMaxOut(behaviour, c), ref dataOut[c], GUILayout.MinWidth(50), GUILayout.Height(5));
                }
                GUILayout.EndVertical();
                Color old = GUI.color;
                GUI.color = new Color(limit, 1.0f - limit, 0.0f, 1.0f);
                GUILayout.Box(string.Format("{0:00.00}ms", ms), GUILayout.MinWidth(40), GUILayout.Height(20));
                GUI.color = old;

                GUILayout.EndHorizontal();
                EditorGUILayout.Space();

                // force repaint
                GUIView.current.Repaint();
            }
        }
    }
}
