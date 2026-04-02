// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Commands.Player;
using Unity.Timeline.Foundation.ViewModel;
using UnityEngine;
using UnityEditor;

namespace Unity.Timeline.Foundation.View.Debugger
{
    class PlayerComponentDrawer : ComponentDrawer<PlayerComponent>
    {
        public override void OnGUI()
        {
            if (component == null)
            {
                EditorGUILayout.LabelField("No player assigned");
                return;
            }

            PlayerData playerData = component.readonlyData;

            EditorGUILayout.LabelField($"Player: {component}");
            EditorGUILayout.LabelField($"Is playing: {playerData.isPlaying}");
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Play"))
                    OnPlay();
                if (GUILayout.Button("Pause"))
                    OnPause();
                if (GUILayout.Button("Stop"))
                    OnStop();
                float newTime = EditorGUILayout.FloatField("time:", 0f);
                if (GUILayout.Button("Evaluate"))
                    Evaluate(new DiscreteTime(Mathf.Max(0f, newTime)));
            }
        }

        void OnPlay()
        {
            viewModel.Dispatch(new Play());
        }

        void OnPause()
        {
            viewModel.Dispatch(new Pause());
        }

        void OnStop()
        {
            viewModel.Dispatch(new Stop());
        }

        void Evaluate(DiscreteTime time)
        {
            viewModel.Dispatch(new Evaluate(time));
        }
    }
}
