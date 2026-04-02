// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using Unity.Timeline.Foundation.Commands.ViewData;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.ViewModel;
using UnityEngine;
using UnityEditor;

namespace Unity.Timeline.Foundation.View.Debugger
{
    class ViewComponentDrawer : ComponentDrawer<ViewComponent>
    {
        public override void OnGUI()
        {
            if (component == null)
            {
                EditorGUILayout.LabelField("No player assigned");
                return;
            }

            ViewData data = component.readonlyData;

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                var start = (DiscreteTime)EditorGUILayout.FloatField("Display start time", (float)data.displayStartTime);
                var end = (DiscreteTime)EditorGUILayout.FloatField("Display end time", (float)data.displayEndTime);
                float vert = EditorGUILayout.FloatField("Vertical scroll offset", data.verticalScrollOffset);
                float headerWidth = EditorGUILayout.FloatField("HeaderWidth", data.headerWidth);

                if (scope.changed)
                {
                    var viewData = new ViewData(new TimeRange(start, end), vert, headerWidth);
                    viewModel.Dispatch(new UpdateViewData(viewData));
                }
            }
        }
    }
}
