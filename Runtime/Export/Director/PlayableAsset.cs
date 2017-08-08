// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Scripting;

namespace UnityEngine.Playables
{
    public struct PlayableBinding
    {
        public static readonly PlayableBinding[] None = new PlayableBinding[0];
        public static readonly double DefaultDuration = double.PositiveInfinity;

        public string streamName { get; set; }
        public DataStreamType streamType { get; set; }
        public UnityEngine.Object sourceObject { get; set; }
        public System.Type sourceBindingType { get; set; }
    };

    public interface IPlayableAsset
    {
        Playable CreatePlayable(PlayableGraph graph, GameObject owner);
        double duration { get; }
        IEnumerable<PlayableBinding> outputs { get; }
    }

    [System.Serializable]
    [RequiredByNativeCode]
    public abstract class PlayableAsset : ScriptableObject, IPlayableAsset
    {
        public abstract Playable CreatePlayable(PlayableGraph graph, GameObject owner);

        public virtual double duration
        {
            get { return PlayableBinding.DefaultDuration; }
        }

        public virtual IEnumerable<PlayableBinding> outputs
        {
            get { return PlayableBinding.None; }
        }

        // Called by playable director to instantiate a playable asset
        //  Uses an IntPtr because XBoxOne doesn't support marshalling the struct as a return value properly
        //  and UAP doesn't support out parameters
        [RequiredByNativeCode]
        internal static unsafe void Internal_CreatePlayable(PlayableAsset asset, PlayableGraph graph, GameObject go, IntPtr ptr)
        {
            Playable result;
            if (asset == null)
                result = Playable.Null;
            else
                result = asset.CreatePlayable(graph, go);

            Playable* handle = (Playable*)ptr.ToPointer();
            *handle = result;
        }

        // workaround for not being able to Invoke<double> on iOS
        [RequiredByNativeCode]
        internal static void Internal_GetPlayableAssetDuration(PlayableAsset asset, IntPtr ptrToDouble)
        {
            double d = asset.duration;
            unsafe
            {
                double* ptr = (double*)ptrToDouble.ToPointer();
                *ptr = d;
            }
        }
    }
}
