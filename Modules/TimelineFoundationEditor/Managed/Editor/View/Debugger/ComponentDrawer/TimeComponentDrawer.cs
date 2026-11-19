// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using Unity.Timeline.Foundation.Commands.Time;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.ViewModel;
using UnityEditor;

namespace Unity.Timeline.Foundation.View.Debugger
{
    class TimeComponentDrawer : ComponentDrawer<TimeComponent>
    {
        public override void OnGUI()
        {
            if (component == null)
            {
                EditorGUILayout.LabelField("No Time component assigned");
                return;
            }

            TimeData data = component.readonlyData;
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                var time = (DiscreteTime)EditorGUILayout.FloatField("Display Time", (float)data.displayTime);
                if (scope.changed)
                    viewModel.Dispatch(new SetDisplayTime(time));
            }

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.Separator();
                EditorGUILayout.LabelField($"Local To Global Time Transform:");
                var offset = (DiscreteTime)EditorGUILayout.FloatField("Offset", (float)data.localToGlobalTimeTransform.offset);
                var multiplier = (double)EditorGUILayout.FloatField("Multiplier", (float)data.localToGlobalTimeTransform.multiplier);
                if (scope.changed)
                {
                    component.localToGlobalTimeTransform = new TimeTransform(new DiscreteTime(offset), multiplier);
                    component.MarkAsDirty();
                }
            }

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.Separator();
                bool isGlobalTime = EditorGUILayout.Toggle("Use Global Time", component.displayIsGlobalTime);
                EditorGUILayout.LabelField($"Local To Display Time Transform:");
                EditorGUILayout.LabelField($"Offset: {data.localToGlobalTimeTransform.offset}, Multiplier {(float)data.localToGlobalTimeTransform.multiplier}");
                if (scope.changed)
                {
                    component.displayIsGlobalTime = isGlobalTime;
                    component.MarkAsDirty();
                }
            }

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Local To Display Time Transform:");
            EditorGUILayout.LabelField($"Offset: {data.localToDisplayTimeTransform.offset}");
            EditorGUILayout.LabelField($"Multiplier: {data.localToDisplayTimeTransform.multiplier}");
        }
    }
}
