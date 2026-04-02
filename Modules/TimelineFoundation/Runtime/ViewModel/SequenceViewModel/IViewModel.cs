// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.CSO;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    //view has access to this, for reading, and 'writing' via Dispatch()
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal interface IViewModel
    {
        void Dispatch<TCommand>(TCommand command) where TCommand : ICommand;
        TState GetData<TState>() where TState : struct, IReadOnlyData;
        void ListenTo<TState>(Action<TState> callback) where TState : struct, IReadOnlyData;
        void Detach<TState>(Action<TState> callback) where TState : struct, IReadOnlyData;
    }
}
