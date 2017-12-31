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

    [NativeHeader("Modules/Director/PlayableDirector.h")]
    [NativeHeader("Runtime/Mono/MonoBehaviour.h")]
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
            get { return Internal_GetPlayableAsset() as PlayableAsset; }
            set { SetPlayableAsset(value as ScriptableObject); }
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

        public void SetGenericBinding(Object key, Object value)
        {
            Internal_SetGenericBinding(key, value);
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
        extern public void SetReferenceValue(PropertyName id, UnityEngine.Object value);
        extern public UnityEngine.Object GetReferenceValue(PropertyName id, out bool idValid);
        [NativeMethod("GetBindingFor")]
        extern public Object GetGenericBinding(Object key);

        extern internal void ProcessPendingGraphChanges();
        [NativeMethod("HasBinding")]
        extern internal bool HasGenericBinding(Object key);

        extern private PlayState GetPlayState();
        extern private void SetWrapMode(DirectorWrapMode mode);
        extern private DirectorWrapMode GetWrapMode();
        extern private void EvaluateNextFrame();
        extern private PlayableGraph GetGraphHandle();
        extern private void SetPlayOnAwake(bool on);
        extern private bool GetPlayOnAwake();
        extern private void Internal_SetGenericBinding(Object key, Object value);
        extern private void SetPlayableAsset(ScriptableObject asset);
        extern private ScriptableObject Internal_GetPlayableAsset();
        //Delegates
        public event Action<PlayableDirector> played;
        public event Action<PlayableDirector> paused;
        public event Action<PlayableDirector> stopped;

        [RequiredByNativeCode]
        void SendOnPlayableDirectorPlay()
        {
            if (played != null)
                played(this);
        }

        [RequiredByNativeCode]
        void SendOnPlayableDirectorPause()
        {
            if (paused != null)
                paused(this);
        }

        [RequiredByNativeCode]
        void SendOnPlayableDirectorStop()
        {
            if (stopped != null)
                stopped(this);
        }
    }
}
