// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Timeline.Foundation.Commands.Manipulations;
using Unity.Timeline.Foundation.ViewModel;
using UnityEditor;
namespace Unity.Timeline.Foundation.View.Debugger
{
    class ManipulationComponentDrawer : ComponentDrawer<ManipulationComponent>
    {
        public override void OnGUI()
        {
            ManipulationData data = component.readonlyData;

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                ManipulationState state = (ManipulationState)EditorGUILayout.EnumPopup("Current Manipulation", data.currentManipulation);
                if (scope.changed)
                {
                    viewModel.Dispatch(new SetCurrentManipulation(state));
                }
            }
        }
    }
}
