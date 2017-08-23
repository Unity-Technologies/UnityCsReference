// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections;
using System;
using System.Text;
using UnityEngine.Scripting;

namespace UnityEditor
{
    internal class GameViewGUI
    {
        private static string FormatNumber(long num)
        {
            if (num < 1000)
                return num.ToString();
            if (num < 1000000)
                return (num * 0.001).ToString("f1") + "k";
            return (num * 0.000001).ToString("f1") + "M";
        }

        private static int m_FrameCounter = 0;
        private static float m_ClientTimeAccumulator = 0.0f;
        private static float m_RenderTimeAccumulator = 0.0f;
        private static float m_MaxTimeAccumulator = 0.0f;
        private static float m_ClientFrameTime = 0.0f;
        private static float m_RenderFrameTime = 0.0f;
        private static float m_MaxFrameTime = 0.0f;

        // Cache bold style of scene skin
        private static GUIStyle s_SectionHeaderStyle;
        private static GUIStyle sectionHeaderStyle
        {
            get
            {
                if (s_SectionHeaderStyle == null)
                    s_SectionHeaderStyle = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene).GetStyle("BoldLabel");
                return s_SectionHeaderStyle;
            }
        }

        // Create label style from scene skin; add rich text support
        private static GUIStyle s_LabelStyle;
        private static GUIStyle labelStyle
        {
            get
            {
                if (s_LabelStyle == null)
                {
                    s_LabelStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene).label);
                    s_LabelStyle.richText = true;
                }
                return s_LabelStyle;
            }
        }

        // Time per frame varies wildly, so we average a bit and display that.
        private static void UpdateFrameTime()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            float frameTime = UnityStats.frameTime;
            float renderTime = UnityStats.renderTime;
            m_ClientTimeAccumulator += frameTime;
            m_RenderTimeAccumulator += renderTime;
            m_MaxTimeAccumulator += Mathf.Max(frameTime, renderTime);
            ++m_FrameCounter;

            bool needsTime = m_ClientFrameTime == 0.0f && m_RenderFrameTime == 0.0f;
            bool resetTime = m_FrameCounter > 30 || m_ClientTimeAccumulator > 0.3f || m_RenderTimeAccumulator > 0.3f;

            if (needsTime || resetTime)
            {
                m_ClientFrameTime = m_ClientTimeAccumulator / m_FrameCounter;
                m_RenderFrameTime = m_RenderTimeAccumulator / m_FrameCounter;
                m_MaxFrameTime = m_MaxTimeAccumulator / m_FrameCounter;
            }
            if (resetTime)
            {
                m_ClientTimeAccumulator = 0.0f;
                m_RenderTimeAccumulator = 0.0f;
                m_MaxTimeAccumulator = 0.0f;
                m_FrameCounter = 0;
            }
        }

        private static string FormatDb(float vol)
        {
            if (vol == 0.0f)
                return "-\u221E dB";
            return string.Format("{0:F1} dB", 20.0f * Mathf.Log10(vol));
        }

        [RequiredByNativeCode]
        public static void GameViewStatsGUI()
        {
            GUI.skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);

            GUI.color = new Color(1, 1, 1, .75f);
            float w = 300, h = 204;

            // Increase windows size to make room for network data
            int connectionCount = Network.connections.Length;
            if (connectionCount != 0)
                h += 220;

            GUILayout.BeginArea(new Rect(GUIView.current.position.width - w - 10, 27, w, h), "Statistics", GUI.skin.window);

            // Audio stats
            GUILayout.Label("Audio:", sectionHeaderStyle);
            StringBuilder audioStats = new StringBuilder(400);
            float audioLevel = UnityStats.audioLevel;
            audioStats.Append("  Level: " + FormatDb(audioLevel) + (EditorUtility.audioMasterMute ? " (MUTED)\n" : "\n"));
            audioStats.Append(String.Format("  Clipping: {0:F1}%", 100.0f * UnityStats.audioClippingAmount));
            GUILayout.Label(audioStats.ToString());

            GUI.Label(new Rect(170, 40, 120, 20), String.Format("DSP load: {0:F1}%", 100.0f * UnityStats.audioDSPLoad));
            GUI.Label(new Rect(170, 53, 120, 20), String.Format("Stream load: {0:F1}%", 100.0f * UnityStats.audioStreamLoad));

            // Graphics stats
            GUILayout.Label("Graphics:", sectionHeaderStyle);

            // Time stats
            UpdateFrameTime();

            string timeStats = System.String.Format("{0:F1} FPS ({1:F1}ms)",
                    1.0f / Mathf.Max(m_MaxFrameTime, 1.0e-5f), m_MaxFrameTime * 1000.0f);
            GUI.Label(new Rect(170, 75, 120, 20), timeStats);

            int screenBytes = UnityStats.screenBytes;
            int batchesSavedByDynamicBatching = UnityStats.dynamicBatchedDrawCalls - UnityStats.dynamicBatches;
            int batchesSavedByStaticBatching = UnityStats.staticBatchedDrawCalls - UnityStats.staticBatches;
            int batchesSavedByInstancing = UnityStats.instancedBatchedDrawCalls - UnityStats.instancedBatches;

            StringBuilder gfxStats = new StringBuilder(400);
            if (m_ClientFrameTime > m_RenderFrameTime)
                gfxStats.Append(String.Format("  CPU: main <b>{0:F1}</b>ms  render thread {1:F1}ms\n", m_ClientFrameTime * 1000.0f, m_RenderFrameTime * 1000.0f));
            else
                gfxStats.Append(String.Format("  CPU: main {0:F1}ms  render thread <b>{1:F1}</b>ms\n", m_ClientFrameTime * 1000.0f, m_RenderFrameTime * 1000.0f));
            gfxStats.Append(String.Format("  Batches: <b>{0}</b> \tSaved by batching: {1}\n", UnityStats.batches, batchesSavedByDynamicBatching + batchesSavedByStaticBatching + batchesSavedByInstancing));
            gfxStats.Append(String.Format("  Tris: {0} \tVerts: {1} \n", FormatNumber(UnityStats.trianglesLong), FormatNumber(UnityStats.verticesLong)));
            gfxStats.Append(String.Format("  Screen: {0} - {1}\n", UnityStats.screenRes, EditorUtility.FormatBytes(screenBytes)));
            gfxStats.Append(String.Format("  SetPass calls: {0} \tShadow casters: {1} \n", UnityStats.setPassCalls, UnityStats.shadowCasters));
            gfxStats.Append(String.Format("  Visible skinned meshes: {0}  Animations: {1}", UnityStats.visibleSkinnedMeshes, UnityStats.visibleAnimations));
            GUILayout.Label(gfxStats.ToString(), labelStyle);

            // Networking stats
            if (connectionCount != 0)
            {
                GUILayout.Label("Network:", sectionHeaderStyle);
                GUILayout.BeginHorizontal();
                for (int i = 0; i < connectionCount; i++)
                {
                    GUILayout.Label(UnityStats.GetNetworkStats(i));
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("Network: (no players connected)", sectionHeaderStyle);
            }
            GUILayout.EndArea();
        }
    }
}
