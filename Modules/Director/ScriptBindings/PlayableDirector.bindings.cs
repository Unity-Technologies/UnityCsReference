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
    ///<summary>Controls playback of <see cref="Playable" /> objects.</summary>
    ///<remarks>The PlayableDirector is primarily used by the &lt;a href="https://docs.unity3d.com/Manual/com.unity.timeline.html"&gt;Timeline Package&lt;/a&gt; to handle bindings to scene objects and control playback of the <see cref="PlayableGraph" />.
    ///
    ///The PlayableDirector object builds a <see cref="PlayableGraph" /> from a <see cref="PlayableAsset" />. Once the graph is built, use the PlayableDirector to manage playback of the graph's <see cref="Playable" />. To control playback of the <see cref="PlayableGraph" />, use:
    ///
    ///- <see cref="PlayableDirector.Play" />
    ///- <see cref="PlayableDirector.Stop" />
    ///- <see cref="PlayableDirector.Pause" />
    ///- <see cref="PlayableDirector.Resume" />
    ///
    ///To be notified of playback state changes, subscribe to:
    ///
    ///- <see cref="PlayableDirector.played" />
    ///- <see cref="PlayableDirector.paused" />
    ///- <see cref="PlayableDirector.stopped" />
    ///
    ///To handle references between assets and scene objects, PlayableDirector implements <see cref="IExposedPropertyTable" />. To set or get bindings, use:
    ///
    ///- <see cref="PlayableDirector.SetGenericBinding" />
    ///- <see cref="PlayableDirector.GetGenericBinding" />
    ///
    ///Multiple PlayableDirectors can reference the same <see cref="PlayableAsset" />. When this occurs, each PlayableDirector creates its own independent <see cref="PlayableGraph" /> with its own scene bindings.
    ///
    ///
    ///The following example demonstrates how to use the Playable Director to bind scene objects to assets and how to control the Playable Graph.</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Modules/Core/Tests/UTFTests/PlayableGraph/DocumentationExamples/PlayableDirectorExample.cs}]]></code>
    ///</example>
    [NativeHeader("Modules/Director/PlayableDirector.h")]
    [NativeHeader("Runtime/Mono/MonoBehaviour.h")]
    [global::UnityEngine.NativeClass("PlayableDirector", PersistentTypeId = 320)]
    [RequiredByNativeCode]
    public partial class PlayableDirector : Behaviour, IExposedPropertyTable
    {
        internal PlayableDirector(global::UnityEngine.EntityId id) : base(id) {}
        ///<exclude />
        public PlayableDirector() {}
        ///<summary>The current playing state of the component. (RO)</summary>
        public PlayState state
        {
            get { return GetPlayState(); }
        }

        ///<summary>Controls how the time is incremented when it goes beyond the duration of the playable.</summary>
        public DirectorWrapMode extrapolationMode
        {
            set { SetWrapMode(value); }
            get { return GetWrapMode(); }
        }

        ///<summary>The <see cref="PlayableAsset" /> that is used to instantiate a playable for playback.</summary>
        public PlayableAsset playableAsset
        {
            get { return Internal_GetPlayableAsset() as PlayableAsset; }
            set { SetPlayableAsset(value as ScriptableObject); }
        }

        ///<summary>The <see cref="PlayableGraph" /> created by the <see cref="PlayableDirector" />.</summary>
        public PlayableGraph playableGraph
        {
            get { return GetGraphHandle(); }
        }

        ///<summary>Whether the playable asset will start playing back as soon as the component awakes.</summary>
        public bool playOnAwake
        {
            get { return GetPlayOnAwake(); }
            set { SetPlayOnAwake(value); }
        }

        ///<summary>Schedules the <see cref="PlayableDirector" /> to perform <see cref="PlayableGraph.Evaluate" /> on the <see cref="PlayableGraph" /> associated with the <see cref="PlayableDirector.playableAsset" /> on the next update.</summary>
        ///<remarks>This only has an effect in the Editor when not in Play Mode.</remarks>
        public void DeferredEvaluate()
        {
            EvaluateNextFrame();
        }

        internal void Play(FrameRate frameRate) => PlayOnFrame(frameRate);

        ///<summary>Instatiates a <see cref="Playable" /> using the provided <see cref="PlayableAsset" /> and starts playback.</summary>
        ///<param name="asset">An asset to instantiate a playable from.</param>
        public void Play(PlayableAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException("asset");

            Play(asset, extrapolationMode);
        }

        ///<summary>Instatiates a <see cref="Playable" /> using the provided <see cref="PlayableAsset" /> and starts playback.</summary>
        ///<param name="asset">An asset to instantiate a playable from.</param>
        ///<param name="mode">What to do when the time passes the duration of the playable.</param>
        public void Play(PlayableAsset asset, DirectorWrapMode mode)
        {
            if (asset == null)
                throw new ArgumentNullException("asset");

            playableAsset = asset;
            extrapolationMode = mode;
            Play();
        }

        ///<summary>Sets the binding of a reference object from a <see cref="PlayableBinding" />.</summary>
        ///<remarks>Use this method to associate assets to objects loaded in the scene. For example, use SetGenericBinding to bind a Timeline track to a GameObject or Component.
        ///
        ///If the value is set to null, the source object is bound to null and the dependency between the PlayableDirector and the source object is kept.</remarks>
        ///<param name="key">The source object in the <see cref="PlayableBinding" />.</param>
        ///<param name="value">The object to bind to the key.</param>
        public void SetGenericBinding(Object key, Object value)
        {
            Internal_SetGenericBinding(key, value);
        }

        // Bindings properties.
        ///<summary>Controls how time is incremented when playing back.</summary>
        ///<seealso cref="DirectorUpdateMode" />
        extern public DirectorUpdateMode timeUpdateMode { set; get; }
        ///<summary>The component's current time. This value is incremented according to the <see cref="PlayableDirector.timeUpdateMode" /> when it is playing. You can also change this value manually.</summary>
        extern public double time { set; get; }
        ///<summary>The time at which the Playable should start when first played.</summary>
        extern public double initialTime { set; get; }
        ///<summary>The duration of the currently connected <see cref="Playable" /> in seconds.</summary>
        extern public double duration { get; }

        // Bindings methods.
        ///<summary>Performs <see cref="PlayableGraph.Evaluate" /> on the <see cref="PlayableGraph" /> associated with the <see cref="PlayableDirector.playableAsset" /> at <see cref="PlayableDirector.time" />.</summary>
        ///<remarks>See also <see cref="PlayableGraph.Evaluate" />.</remarks>
        [NativeMethod(ThrowsException = true)]
        extern public void Evaluate();
        [NativeMethod(ThrowsException = true)]
        extern private void PlayOnFrame(FrameRate frameRate);
        ///<summary>Instatiates a <see cref="Playable" /> using the provided <see cref="PlayableAsset" /> and starts playback.</summary>
        [NativeMethod(ThrowsException = true)]
        extern public void Play();
        ///<summary>Stops playback of the current <see cref="Playable" /> and destroys the corresponding graph.</summary>
        extern public void Stop();
        ///<summary>Pauses playback of the currently running playable.</summary>
        ///<remarks>This method internally invokes <see cref="PlayableGraph.Stop" /> to stop the playable graph time from being automatically updated.</remarks>
        extern public void Pause();
        ///<summary>Resume playing a paused playable.</summary>
        extern public void Resume();
        ///<summary>Discards the existing <see cref="PlayableGraph" /> and creates a new instance.</summary>
        ///<remarks>When the PlayableDirector starts playback, it creates a PlayableGraph from the assigned PlayableAsset. Use this method when the assigned PlayableAsset has changed and it is necessary to show the changes during playback.
        ///
        ///RebuildGraph attempts to maintain the current playback state. For example, if the PlayableDirector has not started playback, RebuildGraph constructs a new PlayableGraph and does not start playback. If the PlayableDirector is playing an existing graph, RebuildGraph stops playback, destroys the graph, creates a new instance of the graph, and resumes playback.</remarks>
        [NativeMethod(ThrowsException = true)]
        extern public void RebuildGraph();
        ///<summary>Clears an exposed reference value.</summary>
        ///<param name="id">Identifier of the <see cref="ExposedReference{T}" />.</param>
        extern public void ClearReferenceValue(PropertyName id);
        ///<summary>Sets an <see cref="ExposedReference{T}" /> value.</summary>
        ///<param name="id">Identifier of the <see cref="ExposedReference{T}" />.</param>
        ///<param name="value">The object to bind to set the reference value to.</param>
        extern public void SetReferenceValue(PropertyName id, UnityEngine.Object value);
        ///<summary>Retrieves an <see cref="ExposedReference{T}" /> binding.</summary>
        ///<param name="id">Identifier of the <see cref="ExposedReference{T}" />.</param>
        ///<param name="idValid">Whether the reference was found.</param>
        extern public UnityEngine.Object GetReferenceValue(PropertyName id, out bool idValid);
        ///<summary>Returns a binding to a reference object.</summary>
        ///<remarks>In Timeline this is the track to bind an object  to. This typically corresponds to the <see cref="PlayableBinding.sourceObject" /> in the PlayableAsset.</remarks>
        ///<param name="key">The object that acts as a key.</param>
        [NativeMethod("GetBindingFor")]
        extern public Object GetGenericBinding(Object key);
        ///<summary>Clears the binding of a reference object.</summary>
        ///<remarks>Use this method to clear the binding and remove the dependency to the source object. For example, use this method to clear the binding for a Timeline track that is no longer used.</remarks>
        ///<param name="key">The source object in the <see cref="PlayableBinding" />.</param>
        [NativeMethod("ClearBindingFor")]
        extern public void ClearGenericBinding(Object key);
        ///<summary>Rebinds each <see cref="PlayableOutput" /> of the <see cref="PlayableGraph" />.</summary>
        ///<remarks>This method updates the <see cref="PlayableOutput" /> binding information. Use when you add or delete a component. Use this method to discover new <see cref="Animator" />, <see cref="AudioSource" />, and <see cref="INotificationReceiver" /> objects without rebuilding the graph.</remarks>
        [NativeMethod(ThrowsException = true)]
        extern public void RebindPlayableGraphOutputs();

        extern internal void ProcessPendingGraphChanges();
        [NativeMethod("HasBinding")]
        extern internal bool HasGenericBinding(Object key);

        extern private PlayState GetPlayState();
        extern private void SetWrapMode(DirectorWrapMode mode);
        extern private DirectorWrapMode GetWrapMode();
        [NativeMethod(ThrowsException = true)]
        extern private void EvaluateNextFrame();
        extern private PlayableGraph GetGraphHandle();
        extern private void SetPlayOnAwake(bool on);
        extern private bool GetPlayOnAwake();
        [NativeMethod(ThrowsException = true)]
        extern private void Internal_SetGenericBinding(Object key, Object value);
        extern private void SetPlayableAsset(ScriptableObject asset);
        extern private ScriptableObject Internal_GetPlayableAsset();
        //Delegates
        ///<summary>Event that is raised when a PlayableDirector component has begun playing.</summary>
        ///<remarks>
        ///  <para>Add an event handler, to this event, to receive a notification when a PlayableDirector begins playing. The handler also receives the PlayableDirector that is playing.
        ///
        ///When using <see cref="PlayableBehaviour" />, this event is raised after <see cref="PlayableBehaviour.OnPlayableCreate" /> and before <see cref="PlayableBehaviour.OnGraphStart" /> and <see cref="PlayableBehaviour.OnBehaviourPlay" />.
        ///
        ///This event will not be raised if the PlayableDirector is automatically played with <see cref="PlayableDirector.playOnAwake" />.</para>
        ///  <para />
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Playables;
        ///
        ///public class PlayableDirectorCallbackExample : MonoBehaviour
        ///{
        ///    public PlayableDirector director;
        ///
        ///    void OnEnable()
        ///    {
        ///        director.played += OnPlayableDirectorPlayed;
        ///    }
        ///
        ///    void OnPlayableDirectorPlayed(PlayableDirector aDirector)
        ///    {
        ///        if (director == aDirector)
        ///            Debug.Log("PlayableDirector named " + aDirector.name + " is now playing.");
        ///    }
        ///
        ///    void OnDisable()
        ///    {
        ///        director.played -= OnPlayableDirectorPlayed;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="PlayableDirector.paused" />
        ///<seealso cref="PlayableDirector.stopped" />
        public event Action<PlayableDirector> played;
        ///<summary>Event that is raised when a PlayableDirector component has paused.</summary>
        ///<remarks>
        ///  <para>Add an event handler, to this event, to receive a notification when a PlayableDirector is paused. The handler also receives the PlayableDirector that is paused.
        ///
        ///When using <see cref="PlayableBehaviour" />, this event is raised before <see cref="PlayableBehaviour.OnBehaviourPause" />.</para>
        ///  <para />
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Playables;
        ///
        ///public class PlayableDirectorCallbackExample : MonoBehaviour
        ///{
        ///    public PlayableDirector director;
        ///    void OnEnable()
        ///    {
        ///        director.paused += OnPlayableDirectorPaused;
        ///    }
        ///
        ///    void OnPlayableDirectorPaused(PlayableDirector aDirector)
        ///    {
        ///        if (director == aDirector)
        ///            Debug.Log("PlayableDirector named " + aDirector.name + " is now paused.");
        ///    }
        ///
        ///    void OnDisable()
        ///    {
        ///        director.paused -= OnPlayableDirectorPaused;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="PlayableDirector.played" />
        ///<seealso cref="PlayableDirector.stopped" />
        public event Action<PlayableDirector> paused;
        ///<summary>Event that is raised when a PlayableDirector component has stopped.</summary>
        ///<remarks>
        ///  <para>Add an event handler, to this event, to receive a notification when a PlayableDirector is stopped. The event handler also receives the PlayableDirector that is stopped.
        ///
        ///When using <see cref="PlayableBehaviour" />, this event is raised before <see cref="PlayableBehaviour.OnBehaviourPause" /> and <see cref="PlayableBehaviour.OnGraphStop" />.</para>
        ///  <para />
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Playables;
        ///
        ///public class PlayableDirectorCallbackExample : MonoBehaviour
        ///{
        ///    public PlayableDirector director;
        ///
        ///    void OnEnable()
        ///    {
        ///        director.stopped += OnPlayableDirectorStopped;
        ///    }
        ///
        ///    void OnPlayableDirectorStopped(PlayableDirector aDirector)
        ///    {
        ///        if (director == aDirector)
        ///            Debug.Log("PlayableDirector named " + aDirector.name + " is now stopped.");
        ///    }
        ///
        ///    void OnDisable()
        ///    {
        ///        director.stopped -= OnPlayableDirectorStopped;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="PlayableDirector.played" />
        ///<seealso cref="PlayableDirector.paused" />
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
