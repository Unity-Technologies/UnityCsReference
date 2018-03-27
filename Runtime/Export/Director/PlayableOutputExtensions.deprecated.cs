// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEngine.Playables
{
    public static partial class PlayableOutputExtensions
    {
        [Obsolete("Method GetSourceInputPort has been renamed to GetSourceOutputPort (UnityUpgradable) -> GetSourceOutputPort<U>(*)", false)]
        public static int GetSourceInputPort<U>(this U output) where U : struct, IPlayableOutput
        {
            return output.GetHandle().GetSourceOutputPort();
        }

        [Obsolete("Method SetSourceInputPort has been renamed to SetSourceOutputPort (UnityUpgradable) -> SetSourceOutputPort<U>(*)", false)]
        public static void SetSourceInputPort<U>(this U output, int value) where U : struct, IPlayableOutput
        {
            output.GetHandle().SetSourceOutputPort(value);
        }
    }
}
