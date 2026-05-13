// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.GraphView
{
    // Serializable information about what is selected in the graph view:
    // just a list of selected element viewDataKeys.
    [Serializable]
    public class GraphSelection
    {
        public List<string> elements = new();
        public bool isEmpty => elements.Count == 0;

        // Deserialize selection info from serialized json.
        // Returns null if selection is empty.
        public static GraphSelection FromJson(string json)
        {
            var res = new GraphSelection();
            EditorJsonUtility.FromJsonOverwrite(json, res);
            return res.isEmpty ? null : res;
        }

        // Apply selection to the graph view and (optional) blackboard.
        public void ApplyToGraphView(GraphView graphView, Blackboard blackboard)
        {
            graphView.ClearSelectionNoUndoRecord();
            foreach (var id in elements)
            {
                var e = graphView.GetElementByGuid(id);
                if (e == null)
                {
                    if (blackboard != null)
                        e = blackboard.Query<BlackboardField>().Where(f => f.viewDataKey == id).First();
                    if (e == null)
                        continue;
                }
                graphView.AddToSelectionNoUndoRecord(e);
            }
        }
    }
}
