// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEngine.Playables
{
    public static class PlayableOutputExtensions
    {
        public static bool IsOutputValid<U>(this U output) where U : struct, IPlayableOutput
        {
            return output.GetHandle().IsValid();
        }

        public static Object GetReferenceObject<U>(this U output) where U : struct, IPlayableOutput
        {
            return output.GetHandle().GetReferenceObject();
        }

        public static void SetReferenceObject<U>(this U output, Object value) where U : struct, IPlayableOutput
        {
            output.GetHandle().SetReferenceObject(value);
        }

        public static Object GetUserData<U>(this U output) where U : struct, IPlayableOutput
        {
            return output.GetHandle().GetUserData();
        }

        public static void SetUserData<U>(this U output, Object value) where U : struct, IPlayableOutput
        {
            output.GetHandle().SetUserData(value);
        }

        public static Playable GetSourcePlayable<U>(this U output) where U : struct, IPlayableOutput
        {
            return new Playable(output.GetHandle().GetSourcePlayable());
        }

        public static void SetSourcePlayable<U, V>(this U output, V value)
            where U : struct, IPlayableOutput
            where V : struct, IPlayable
        {
            output.GetHandle().SetSourcePlayable(value.GetHandle());
        }

        public static int GetSourceInputPort<U>(this U output) where U : struct, IPlayableOutput
        {
            return output.GetHandle().GetSourceInputPort();
        }

        public static void SetSourceInputPort<U>(this U output, int value) where U : struct, IPlayableOutput
        {
            output.GetHandle().SetSourceInputPort(value);
        }

        public static float GetWeight<U>(this U output) where U : struct, IPlayableOutput
        {
            return output.GetHandle().GetWeight();
        }

        public static void SetWeight<U>(this U output, float value) where U : struct, IPlayableOutput
        {
            output.GetHandle().SetWeight(value);
        }
    }
}
