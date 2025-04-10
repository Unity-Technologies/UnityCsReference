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
    [NativeHeader("Modules/Director/PlayableDirector.h")]
    [NativeHeader("Runtime/Mono/MonoBehaviour.h")]
    [RequiredByNativeCode]
    [HelpURL("https://docs.unity3d.com/ScriptReference/Playables.PlayableDirector.html")]
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

        internal void Play(FrameRate frameRate) => PlayOnFrame(frameRate);

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
        [NativeThrows]
        extern public void Evaluate();
        [NativeThrows]
        extern private void PlayOnFrame(FrameRate frameRate);
        [NativeThrows]
        extern public void Play();
        extern public void Stop();
        extern public void Pause();
        extern public void Resume();
        [NativeThrows]
        extern public void RebuildGraph();
        extern public void ClearReferenceValue(PropertyName id);
        extern public void SetReferenceValue(PropertyName id, UnityEngine.Object value);
        extern public UnityEngine.Object GetReferenceValue(PropertyName id, out bool idValid);
        [NativeMethod("GetBindingFor")]
        extern public Object GetGenericBinding(Object key);
        [NativeMethod("ClearBindingFor")]
        extern public void ClearGenericBinding(Object key);
        [NativeThrows]
        extern public void RebindPlayableGraphOutputs();

        extern internal void ProcessPendingGraphChanges();
        [NativeMethod("HasBinding")]
        extern internal bool HasGenericBinding(Object key);

        extern private PlayState GetPlayState();
        extern private void SetWrapMode(DirectorWrapMode mode);
        extern private DirectorWrapMode GetWrapMode();
        [NativeThrows]
        extern private void EvaluateNextFrame();
        extern private PlayableGraph GetGraphHandle();
        extern private void SetPlayOnAwake(bool on);
        extern private bool GetPlayOnAwake();
        [NativeThrows]
        extern private void Internal_SetGenericBinding(Object key, Object value);
        extern private void SetPlayableAsset(ScriptableObject asset);
        extern private ScriptableObject Internal_GetPlayableAsset();
        //Delegates
        public event Action<PlayableDirector> played;
        public event Action<PlayableDirector> paused;
        public event Action<PlayableDirector> stopped;

        //internal director manager api;
        [NativeHeader("Runtime/Director/Core/DirectorManager.h")]
        [StaticAccessor("GetDirectorManager()", StaticAccessorType.Dot)]
        internal extern static void ResetFrameTiming();

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
