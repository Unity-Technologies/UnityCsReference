// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor;

class RunModeDecorator : PlayModeControllerDecorator<RunModeDecorator.DecoratorSettings>
{
    [Serializable]
    internal struct DecoratorSettings
    {
        internal const string k_RunModePropertyName = nameof(RunMode);

        [HideInInspector] public RunModeState RunMode;
    }

    internal RunModeState RunMode => Settings.RunMode;
}
