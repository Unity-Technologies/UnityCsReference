// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;

namespace UnityEngine.Playables
{
    // This must always be in sync with DirectorWrapMode in Runtime/Director/Core/DirectorTypes.h
    public enum DirectorWrapMode
    {
        Hold = 0,
        Loop = 1,
        None = 2
    }

    [NativeHeader("Runtime/Director/Module/PlayableDirector.h")]
    [RequiredByNativeCode]
    public partial class PlayableDirector : Behaviour, IExposedPropertyTable
    {
        public PlayState state
        {
            get { return GetPlayState(); }
        }

        public DirectorWrapMode extrapolationMode
        {
            set { SetWrapMode(value); }
            get { return GetWrapMode(); }
        }

        public PlayableAsset playableAsset
        {
            get { return GetPlayableAssetInternal() as PlayableAsset; }
            set { SetPlayableAssetInternal(value as ScriptableObject); }
        }

        public PlayableGraph playableGraph
        {
            get { return GetGraphHandle(); }
        }

        public bool playOnAwake
        {
            get { return GetPlayOnAwake(); }
            set { SetPlayOnAwake(value); }
        }

        public void DeferredEvaluate()
        {
            EvaluateNextFrame();
        }

        public void Play(PlayableAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException("asset");

            Play(asset, extrapolationMode);
        }

        public void Play(PlayableAsset asset, DirectorWrapMode mode)
        {
            if (asset == null)
                throw new ArgumentNullException("asset");

            playableAsset = asset;
            extrapolationMode = mode;
            Play();
        }

        // Bindings properties.
        extern public DirectorUpdateMode timeUpdateMode { set; get; }
        extern public double time { set; get; }
        extern public double initialTime { set; get; }
        extern public double duration { get; }

        // Bindings methods.
        extern public void Evaluate();
        extern public void Play();
        extern public void Stop();
        extern public void Pause();
        extern public void Resume();
        extern public void RebuildGraph();
        extern public void ClearReferenceValue(PropertyName id);

        extern internal void ProcessPendingGraphChanges();

        extern private PlayState GetPlayState();
        extern private void SetWrapMode(DirectorWrapMode mode);
        extern private DirectorWrapMode GetWrapMode();
        extern private void EvaluateNextFrame();
        extern private PlayableGraph GetGraphHandle();
        extern private void SetPlayOnAwake(bool on);
        extern private bool GetPlayOnAwake();
    }
}
