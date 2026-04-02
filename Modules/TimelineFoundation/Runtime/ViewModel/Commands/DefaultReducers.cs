// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.ViewModel;

namespace Unity.Timeline.Foundation.Commands
{
    static class DefaultReducers
    {
        public static void RegisterAll(ViewModelBase viewModel)
        {
            Player.Reducers.RegisterAll(viewModel);
            ViewData.Reducers.RegisterAll(viewModel);
            Sequence.Reducers.RegisterAll(viewModel);
            Selection.Reducers.RegisterAll(viewModel);
            Time.Reducers.RegisterAll(viewModel);
        }
    }
}
