// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.Model;
using Unity.Timeline.Foundation.ViewModel;

namespace Unity.Timeline.Foundation.Commands.Player
{
    static class Reducers
    {
        public static void RegisterAll(ViewModelBase viewModel)
        {
            viewModel.RegisterCommandHandler<PlayerComponent, Play>(PlayPlayerReducer);
            viewModel.RegisterCommandHandler<PlayerComponent, Pause>(PausePlayerReducer);
            viewModel.RegisterCommandHandler<PlayerComponent, Stop>(StopPlayerReducer);
            viewModel.RegisterCommandHandler<TimeComponent, PlayerComponent, Evaluate>(EvaluatePlayerReducer);
        }

        static void PlayPlayerReducer(PlayerComponent player, Play action)
        {
            if (player == null) return;

            using (player.UpdateScope())
            {
                player.playerModel.Play();
            }
        }

        static void PausePlayerReducer(PlayerComponent player, Pause action)
        {
            if (player == null) return;

            using (player.UpdateScope())
            {
                player.playerModel.Pause();
            }
        }

        static void StopPlayerReducer(PlayerComponent player, Stop action)
        {
            if (player == null) return;

            using (player.UpdateScope())
            {
                player.playerModel.Stop();
            }
        }

        public static void EvaluatePlayerReducer(TimeComponent timeComponent, PlayerComponent player, Evaluate action)
        {
            if (player == null) return;

            TimeSourceData timeSourceData = new TimeSourceData(timeComponent.readonlyData.localToGlobalTimeTransform,
                action.time);
            using (player.UpdateScope())
            {
                player.playerModel.Evaluate(timeSourceData);
            }
        }
    }
}
