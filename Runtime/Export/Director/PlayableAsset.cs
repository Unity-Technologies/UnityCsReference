// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;

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
        PlayableHandle CreatePlayable(PlayableGraph graph, GameObject owner);
        double duration { get; }
    }

    public abstract partial class PlayableAsset : ScriptableObject, IPlayableAsset
    {
        public abstract PlayableHandle CreatePlayable(PlayableGraph graph, GameObject owner);

        public virtual double duration
        {
            get { return PlayableBinding.DefaultDuration; }
        }

        // workaround for not being able to Invoke<double> on iOS
        internal void InternalGetDuration(IntPtr ptrToDouble)
        {
            double d = duration;
            unsafe
            {
                double* ptr = (double*)ptrToDouble.ToPointer();
                *ptr = d;
            }
        }
    }
}
