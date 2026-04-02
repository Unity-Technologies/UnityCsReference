// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.ViewModel;
using UnityEditor;

namespace Unity.Timeline.Foundation.View.Debugger
{
    class SelectionComponentDrawer : ComponentDrawer<SelectionComponent>
    {
        public override void OnGUI()
        {
            SelectionData selectionInfo = component.readonlyData;

            DrawSelectionContainer("Selected", selectionInfo.selection);
            DrawSelectionContainer("Newly selected", selectionInfo.newlySelected);
            DrawSelectionContainer("Newly deselected", selectionInfo.newlyDeselected);
        }

        static void DrawSelectionContainer(string label, SelectionContainer selection)
        {
            EditorGUILayout.LabelField(label);
            using var scope = new EditorGUI.IndentLevelScope();

            if (selection.Count() <= 0)
            {
                EditorGUILayout.LabelField("Empty");
            }
            else
            {
                DrawSelection("Tracks", selection.tracks);
                DrawSelection("Clips", selection.clips);
                DrawSelection("Markers", selection.markers);
                DrawSelection("Transitions", selection.transitions);
            }
        }

        static void DrawSelection(string name, IReadOnlyCollection<UniqueID> selection)
        {
            if (selection.Count <= 0) return;

            EditorGUILayout.LabelField(name);
            using var scope = new EditorGUI.IndentLevelScope();
            foreach (UniqueID id in selection)
                EditorGUILayout.LabelField(id.ToString());
        }
    }
}
