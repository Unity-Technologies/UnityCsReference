// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal class NetworkingOperationsProfilerModule : ProfilerModuleBase
    {
        SplitterState m_NetworkSplit = new SplitterState(new[] { 20f, 80f }, new[] { 100, 100 }, null);

        public override void DrawToolbar(Rect position)
        {
            // This module still needs to be broken apart into Toolbar and View.
        }

        public override void DrawView(Rect position)
        {
            DrawNetworkOperationsPane();
        }

        private String[] msgNames =
        {
            "UserMessage", "ObjectDestroy", "ClientRpc", "ObjectSpawn", "Owner", "Command", "LocalPlayerTransform", "SyncEvent", "SyncVars", "SyncList", "ObjectSpawnScene", "NetworkInfo", "SpawnFinished", "ObjectHide", "CRC", "ClientAuthority"
        };

        private bool[] msgFoldouts = { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true };

        void DrawNetworkOperationsPane()
        {
            SplitterGUILayout.BeginHorizontalSplit(m_NetworkSplit);

            GUILayout.Label(ProfilerDriver.GetOverviewText(ProfilerArea.NetworkOperations, m_ProfilerWindow.GetActiveVisibleFrameIndex()), EditorStyles.wordWrappedLabel);

            m_PaneScroll = GUILayout.BeginScrollView(m_PaneScroll, ProfilerWindow.Styles.background);

            EditorGUILayout.BeginHorizontal(EditorStyles.contentToolbar);
            EditorGUILayout.LabelField("Operation Detail");
            EditorGUILayout.LabelField("Over 5 Ticks");
            EditorGUILayout.LabelField("Over 10 Ticks");
            EditorGUILayout.LabelField("Total");
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel += 1;

            for (short msgId = 0; msgId < msgNames.Length; msgId++)
            {
#pragma warning disable CS0618
                if (!NetworkDetailStats.m_NetworkOperations.ContainsKey(msgId))
#pragma warning restore
                    continue;

                msgFoldouts[msgId] = EditorGUILayout.Foldout(msgFoldouts[msgId], msgNames[msgId] + ":");
                if (msgFoldouts[msgId])
                {
                    EditorGUILayout.BeginVertical();
#pragma warning disable CS0618
                    var detail = NetworkDetailStats.m_NetworkOperations[msgId];
#pragma warning restore

                    EditorGUI.indentLevel += 1;

                    foreach (var entryName in detail.m_Entries.Keys)
                    {
                        int tick = (int)Time.time;
                        var entry = detail.m_Entries[entryName];

                        if (entry.m_IncomingTotal > 0)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("IN:" + entryName);
                            EditorGUILayout.LabelField(entry.m_IncomingSequence.GetFiveTick(tick).ToString());
                            EditorGUILayout.LabelField(entry.m_IncomingSequence.GetTenTick(tick).ToString());
                            EditorGUILayout.LabelField(entry.m_IncomingTotal.ToString());
                            EditorGUILayout.EndHorizontal();
                        }

                        if (entry.m_OutgoingTotal > 0)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("OUT:" + entryName);
                            EditorGUILayout.LabelField(entry.m_OutgoingSequence.GetFiveTick(tick).ToString());
                            EditorGUILayout.LabelField(entry.m_OutgoingSequence.GetTenTick(tick).ToString());
                            EditorGUILayout.LabelField(entry.m_OutgoingTotal.ToString());
                            EditorGUILayout.EndHorizontal();
                        }
                    }

                    EditorGUI.indentLevel -= 1;
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUI.indentLevel -= 1;
            GUILayout.EndScrollView();
            SplitterGUILayout.EndHorizontalSplit();
        }

    }
}
