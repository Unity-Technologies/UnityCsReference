// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Commands.Sequence;
using Unity.Timeline.Foundation.ViewModel;
using UnityEngine;
using UnityEditor;

namespace Unity.Timeline.Foundation.View.Debugger
{
    class SequenceComponentDrawer : ComponentDrawer<SequenceSourceComponent>
    {
        bool m_ContentsFoldout;
        bool m_DiffFoldout;
        bool m_LookupFoldout;

        DebugLogDrawer m_DebugViewDrawer = new DebugLogDrawer(true);
        List<EventLog> m_Logs = new List<EventLog>();
        SequenceDiff m_LastDiff;

        public override void OnGUI()
        {
            if (component == null)
            {
                EditorGUILayout.LabelField("No player assigned");
                return;
            }

            Sequence sequence = component.readonlyData.sequence;

            if (sequence == null)
            {
                EditorGUILayout.LabelField("No sequence selected");
                return;
            }

            ProcessDiff();

            EditorGUILayout.LabelField($"Timeline: {sequence}");
            EditorGUILayout.Space();

            using (new EditorGUI.IndentLevelScope(1))
            {
                DrawFixedDuration(sequence);

                m_ContentsFoldout = EditorGUILayout.Foldout(m_ContentsFoldout, "Sequence contents", true);
                if (m_ContentsFoldout)
                {
                    using (new EditorGUI.IndentLevelScope(1))
                    {
                        DrawSequence(sequence);
                    }
                }

                m_DiffFoldout = EditorGUILayout.Foldout(m_DiffFoldout, "Sequence changes", true);
                if (m_DiffFoldout)
                {
                    using (new EditorGUI.IndentLevelScope(1))
                    {
                        if (GUILayout.Button("Clear log"))
                            m_Logs.Clear();

                        m_DebugViewDrawer.DrawLog(m_Logs);
                    }
                }

                m_LookupFoldout = EditorGUILayout.Foldout(m_LookupFoldout, "Id Look up tables", true);
                if (m_LookupFoldout)
                {
                    using (new EditorGUI.IndentLevelScope(1))
                    {
                        DrawLookupTables(sequence);
                    }
                }
            }
        }

        void DrawFixedDuration(Sequence sequence)
        {
            using (var changeScope = new EditorGUI.ChangeCheckScope())
            {
                var fixedDuration = EditorGUILayout.DoubleField(new GUIContent("Fixed Duration"), (double)sequence.duration);
                if (changeScope.changed)
                {
                    viewModel.Dispatch(new SetDuration(new DiscreteTime(fixedDuration)));
                }
            }
        }

        void DrawSequence(Sequence sequence)
        {
            using (new EditorGUILayout.VerticalScope())
            {
                foreach (Track track in sequence.children)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (var changeScope = new EditorGUI.ChangeCheckScope())
                        {
                            string newTrackName = EditorGUILayout.TextField("Track -", track.name);
                            if (changeScope.changed)
                            {
                                viewModel.Dispatch(new SetTrackName(track, newTrackName));
                            }
                        }

                        EditorGUILayout.LabelField($" - {track}");

                        if (GUILayout.Button("Remove"))
                            viewModel.Dispatch(new RemoveTrack(track));

                        GUILayout.FlexibleSpace();
                    }

                    EditorGUILayout.LabelField("    -Items");
                    foreach (Item item in track.Items)
                        EditorGUILayout.LabelField($"     {item.ToString()}");
                }
            }
        }

        void ProcessDiff()
        {
            SequenceDiff diff = component.readonlyData.lastDiff;
            if (diff != m_LastDiff && diff.HasChanges())
            {
                ParseDiff(diff);
                m_LastDiff = diff;
            }
        }

        void ParseDiff(SequenceDiff diff)
        {
            foreach (TrackChange trackChange in diff.trackChanges)
            {
                m_Logs.Add(new EventLog($"{trackChange.type} for track :{trackChange.track}", string.Empty));
            }

            foreach (Track added in diff.hierarchyChanges.addedTracks)
            {
                m_Logs.Add(new EventLog($"Track added: {added}", string.Empty));
            }

            foreach (Track removed in diff.hierarchyChanges.removedTracks)
            {
                m_Logs.Add(new EventLog($"Track removed: {removed}", string.Empty));
            }

            foreach (Track reordered in diff.hierarchyChanges.reorderedTracks)
            {
                m_Logs.Add(new EventLog($"Track re-ordered: {reordered}", string.Empty));
            }
        }

        void DrawLookupTables(Sequence sequence)
        {
            var children = sequence.children;

            EditorGUILayout.LabelField($"TracksIdLookup: ");
            using (new EditorGUI.IndentLevelScope(1))
            {
                foreach (var track in children)
                {
                    EditorGUILayout.LabelField($"{track.ID} - {track.name}");
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"ItemsIdLookup: ");
            using (new EditorGUI.IndentLevelScope(1))
            {
                foreach (var track in children)
                {
                    foreach (var item in track.Items)
                    {
                        EditorGUILayout.LabelField($"{item.ID} - {item.type}: {item.name}, TimeRange: {item.start}-{item.end}");
                    }
                }
            }
        }
    }
}
