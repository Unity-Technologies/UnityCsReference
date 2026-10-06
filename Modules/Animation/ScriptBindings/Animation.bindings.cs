// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using uei = UnityEngine.Internal;

namespace UnityEngine
{
    // Used by Animation.Play function.
    ///<summary>Options that choose which animations to stop when playing an animation.</summary>
    ///<remarks>Methods use this enum to choose which animations to stop based on the layer or component of the started animation. For example, the <see cref="Animation.Play" /> method uses <c>StopSameLayer</c> to stop animations on the same layer as the started animation. This method uses <c>StopAll</c> to stop animations associated with the same <see cref="Animation" /> component as the started animation.</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/PlayModeExample.cs}]]></code>
    ///</example>
    ///<seealso cref="Animation.CrossFade" />
    ///<seealso cref="Animation.CrossFadeQueued" />
    ///<seealso cref="Animation.Play" />
    ///<seealso cref="Animation.PlayQueued" />
    public enum PlayMode
    {
        // Will stop all animations that were started in the same layer. This is the default when playing animations.
        ///<summary>Stops animations on the same layer as the started animation. This is the default behavior.</summary>
        StopSameLayer = 0,
        // Will stop all animations that were started with this component before playing
        ///<summary>Stops animations played by the same component as the started animation.</summary>
        StopAll = 4,
    }

    // Used by Animation.Play function.
    ///<summary>Used by <see cref="Animation.Play" /> function.</summary>
    public enum QueueMode
    {
        // Will start playing after all other animations have stopped playing
        ///<summary>Will start playing after all other animations have stopped playing.</summary>
        CompleteOthers = 0,
        // Starts playing immediately. This can be used if you just want to quickly create a duplicate animation.
        ///<summary>Starts playing immediately. This can be used if you just want to quickly create a duplicate animation.</summary>
        PlayNow = 2
    }

    // Used by Animation.Play function.
    ///<summary>Used by <see cref="Animation.Play" /> function.</summary>
    public enum AnimationBlendMode
    {
        // Animations will be blended
        ///<summary>Animations will be blended.</summary>
        Blend = 0,
        // Animations will be added
        ///<summary>Animations will be added.</summary>
        Additive = 1
    }

    // considered deprecated
    ///<exclude />
    public enum AnimationPlayMode { Stop = 0, Queue = 1, Mix = 2 }

    // This enum controlls culling of Animation component.
    ///<summary>This enum controlls culling of Animation component.</summary>
    ///<remarks>When culling is enabled, Unity might stop animating if it thinks that the results of the animation won't be visible to the user.
    ///This could save you some performance if you have things animating outside of the viewport, whose animation is only important
    ///when the user can actually see the thing that is being animated. When Animation component is culled it
    ///won't do anything: it won't update animation states, execute events or sample animations.</remarks>
    public enum AnimationCullingType
    {
        // Animation culling is disabled - object is animated even when offscreen.
        ///<summary>Animation culling is disabled - object is animated even when offscreen.</summary>
        AlwaysAnimate = 0,
        // Animation is disabled when renderers are not visible.
        ///<summary>Animation is disabled when renderers are not visible.</summary>
        ///<remarks>This culling method is more suitable when you have renderers attached after import - it will take
        ///renderers (like mesh renderers, particle renderers and so on) attached to this gameObject or children
        ///of this game object.</remarks>
        BasedOnRenderers = 1,

        // Animation is disabled when localBounds are not visible.
        ///<exclude />
        [System.Obsolete("Enum member AnimatorCullingMode.BasedOnClipBounds has been deprecated. Use AnimationCullingType.AlwaysAnimate or AnimationCullingType.BasedOnRenderers instead")]
        BasedOnClipBounds = 2,
        // Animation is disabled when localBounds are not visible.
        ///<exclude />
        [System.Obsolete("Enum member AnimatorCullingMode.BasedOnUserBounds has been deprecated. Use AnimationCullingType.AlwaysAnimate or AnimationCullingType.BasedOnRenderers instead")]
        BasedOnUserBounds = 3
    }

    ///<summary>The update mode of the Animation component.</summary>
    public enum AnimationUpdateMode
    {
        ///<summary>Animation updates in the Update loop.</summary>
        Normal = 0,
        ///<summary>Animation updates in the FixedUpdate loop. Use this mode to evaluate animation independent of frame rate.</summary>
        Fixed = 1
    }

    internal enum AnimationEventSource
    {
        NoSource = 0,
        Legacy = 1,
        Animator = 2,
    }

    // The animation component is used to play back animations.
    ///<summary>The animation component is used to play back animations.</summary>
    ///<remarks>
    ///  <para>You can assign animation clips to the animation component and control playback from your script.
    ///The animation system in Unity is weight-based and supports Animation Blending, Additive animations, Animation Mixing, Layers and full control over all aspects of playback.
    ///
    ///For an overview of animation scripting in Unity please [read this introduction](xref:AnimationOverview).
    ///
    ///<see cref="AnimationState" /> can be used to change the layer of an animation, modify playback speed, and for direct control over blending and mixing.
    ///
    ///Also <see cref="Animation" /> supports enumerators. Looping through all <see cref="T:UnityEngine.AnimationState" /> is performed like this:</para>
    ///  <para />
    ///</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using UnityEngine;
    ///using System.Collections;
    ///
    ///public class ExampleClass : MonoBehaviour
    ///{
    ///    public Animation anim;
    ///
    ///    void Start()
    ///    {
    ///        anim = GetComponent<Animation>();
    ///        foreach (AnimationState state in anim)
    ///        {
    ///            state.speed = 0.5F;
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<seealso href="xref:AnimationOverview">here</seealso>
    [global::UnityEngine.NativeClass("Animation", PersistentTypeId = 111)]
    [NativeHeader("Modules/Animation/Animation.h")]
    public sealed class Animation : Behaviour, IEnumerable
    {
        internal Animation(global::UnityEngine.EntityId id) : base(id) {}
        public Animation() {}
        ///<summary>The default animation.</summary>
        public extern AnimationClip clip { get; set; }
        ///<summary>Should the default animation clip (the <see cref="Animation.clip" /> property) automatically start playing on startup?</summary>
        public extern bool playAutomatically { get; set; }
        ///<summary>How should time beyond the playback range of the clip be treated?</summary>
        public extern WrapMode wrapMode { get; set; }

        ///<summary>Stops all playing animations that were started with this Animation.</summary>
        ///<remarks>Stopping an animation also Rewinds it to the Start.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    Animation anim;
        ///
        ///    void Start()
        ///    {
        ///        anim = GetComponent<Animation>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        if (Input.GetButtonDown("Jump") && anim.isPlaying)
        ///        {
        ///            anim.Stop();
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern void Stop();
        ///<summary>Stops an animation named <c>name</c>.</summary>
        ///<remarks>Stopping an animation also Rewinds it to the Start.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    Animation anim;
        ///
        ///    void Start()
        ///    {
        ///        anim = GetComponent<Animation>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        if (Input.GetButtonDown("Jump") && anim.isPlaying)
        ///        {
        ///            anim.Stop("CubeJump");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void Stop(string name) { StopNamed(name); }
        [NativeName("Stop")] private extern void StopNamed(string name);
        ///<summary>Rewinds all animations.</summary>
        ///<remarks>Sets the time of all animations to 0.</remarks>
        ///<seealso cref="AnimationState.time" />
        public extern void Rewind();
        ///<summary>Rewinds the animation named <c>name</c>.</summary>
        ///<remarks>
        ///  <para>Sets the time of the animation named <c>name</c> to 0. If there is no animation named <c>name</c>, nothing happens.</para>
        ///  <para />
        ///</remarks>
        ///<param name="name">The name of the animation to rewind.</param>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/AnimationRewindExample.cs}]]></code>
        ///</example>
        ///<seealso cref="AnimationState.time" />
        public void Rewind(string name) { RewindNamed(name); }
        [NativeName("Rewind")] private extern void RewindNamed(string name);

        ///<summary>Samples animations at the current state.</summary>
        ///<remarks>This is useful when you explicitly want to set up some animation state, and sample it once.</remarks>
        public extern void Sample();
        ///<summary>Is an animation currently being played?</summary>
        public extern bool isPlaying { [NativeName("IsPlaying")] get; }
        ///<summary>Is the animation named <c>name</c> playing?</summary>
        public extern bool IsPlaying(string name);

        ///<summary>Returns the animation state named <c>name</c>.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    public Animation anim;
        ///
        ///    void Start()
        ///    {
        ///        // Get the walk animation state  and set its speed
        ///        anim["walk"].speed = 2.0f;
        ///
        ///        // Get the run animation state and set its weight
        ///        anim["run"].weight = 0.5f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public AnimationState this[string name] { get { return GetState(name); } }

        [uei.ExcludeFromDocs] public bool Play() { return Play(PlayMode.StopSameLayer); }
        ///<summary>Plays an animation without blending.</summary>
        ///<remarks>If no name is supplied then the default animation plays. Use the optional <see cref="PlayMode" /> to choose how this animation affects animations already playing.
        ///
        ///If the specified animation is already playing then other animations will be stopped but the animation will not rewind to the beginning. When the end of the animation is reached it will automatically be stopped and rewound to the start unless the <see cref="PlayMode" /> is set to Loop. If <see cref="Animation.Play" /> is called on an object during a frame update where the object is also [deactivated](xref:DeactivatingGameObjects) then the call will effectively be cancelled. The animation will not start playing when the object is later reactivated. However, if a call on a subsequent frame (while the object is still inactive) then the animation will start playing after reactivation.
        ///
        ///To use <see cref="Animation.Play" /> the animation data must be visible in the Inspector window. This window contains all animations for a <see cref="GameObject" /> in an array. As an example two animations <c>jump</c> and <c>spin</c> are stored in the Animations list. <see cref="Animation.Play" /> can play each of these animations. <see cref="Animation" /> can also combine animations. An (unsupported and undocumented) <see cref="AnimationState" />.layer is used for this. For example leaving <c>jump</c> at layer zero and moving <c>spin</c> to layer 123 will allow them to be played together.
        ///
        ///Animations must be marked as ‘Legacy’ in the Inspector for the animations to be found by this method. This option appears after switching the Inspector Window to ‘Debug’.</remarks>
        ///<returns>If no name is supplied and there is no default animation, then this method returns <c>false</c>. Otherwise, it returns <c>true</c>.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        /// // Animation.Play example. Let the S and J keys start
        /// // a spin or jump animation. Let Space play back spin and
        /// // jump at the same time. Let Z play spin and jump with
        /// // spin doubled in speed.
        /// //
        /// // Spin: rotate the cube 360 degrees in half or one second
        /// // Jump: bounce up to 2 units and down in one second
        /// //
        /// // Note: AnimationState.layer is no longer supported, but still exists.
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    private Animation anim;
        ///
        ///    void Start()
        ///    {
        ///        anim = gameObject.GetComponent<Animation>();
        ///        anim["spin"].layer = 123;
        ///    }
        ///
        ///    // double the spin speed when true
        ///    private bool fastSpin = false;
        ///
        ///    void Update()
        ///    {
        ///        // leave spin or jump to complete before changing
        ///        if (anim.isPlaying)
        ///        {
        ///            return;
        ///        }
        ///
        ///        if (Input.GetKeyDown(KeyCode.S))
        ///        {
        ///            Debug.Log("Spinning");
        ///            anim.Play("spin");
        ///        }
        ///
        ///        if (Input.GetKeyDown(KeyCode.J))
        ///        {
        ///            Debug.Log("Jumping");
        ///            anim.Play("jump");
        ///        }
        ///
        ///        // combine jump and spin
        ///        if (Input.GetKeyDown(KeyCode.Space))
        ///        {
        ///            Debug.Log("Jumping and spinning");
        ///            anim.Play("jump");
        ///            anim.Play("spin");
        ///        }
        ///
        ///        // have spin speed reverted to 1.0 second
        ///        if (fastSpin == true)
        ///        {
        ///            anim["spin"].speed = 1.0f;
        ///            fastSpin = false;
        ///        }
        ///
        ///        if (Input.GetKeyDown(KeyCode.Z))
        ///        {
        ///            Debug.Log("Jumping and spinning in half a second");
        ///            anim.Play("jump");
        ///            anim["spin"].speed = 2.0f;
        ///            anim.Play("spin");
        ///
        ///            // we've used spin at a speed of two, now mark
        ///            // the spin speed to revert back to one
        ///            fastSpin = true;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public bool Play([uei.DefaultValue("PlayMode.StopSameLayer")] PlayMode mode) { return PlayDefaultAnimation(mode); }
        [NativeName("Play")] extern private bool PlayDefaultAnimation(PlayMode mode);

        [uei.ExcludeFromDocs] public bool Play(string animation) { return Play(animation, PlayMode.StopSameLayer); }
        ///<summary>Plays an animation without blending.</summary>
        ///<remarks>If no name is supplied then the default animation plays. Use the optional <see cref="PlayMode" /> to choose how this animation affects animations already playing.
        ///
        ///If the specified animation is already playing then other animations will be stopped but the animation will not rewind to the beginning. When the end of the animation is reached it will automatically be stopped and rewound to the start unless the <see cref="PlayMode" /> is set to Loop. If <see cref="Animation.Play" /> is called on an object during a frame update where the object is also [deactivated](xref:DeactivatingGameObjects) then the call will effectively be cancelled. The animation will not start playing when the object is later reactivated. However, if a call on a subsequent frame (while the object is still inactive) then the animation will start playing after reactivation.
        ///
        ///To use <see cref="Animation.Play" /> the animation data must be visible in the Inspector window. This window contains all animations for a <see cref="GameObject" /> in an array. As an example two animations <c>jump</c> and <c>spin</c> are stored in the Animations list. <see cref="Animation.Play" /> can play each of these animations. <see cref="Animation" /> can also combine animations. An (unsupported and undocumented) <see cref="AnimationState" />.layer is used for this. For example leaving <c>jump</c> at layer zero and moving <c>spin</c> to layer 123 will allow them to be played together.
        ///
        ///Animations must be marked as ‘Legacy’ in the Inspector for the animations to be found by this method. This option appears after switching the Inspector Window to ‘Debug’.</remarks>
        ///<returns>If no name is supplied and there is no default animation, then this method returns <c>false</c>. Otherwise, it returns <c>true</c>.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        /// // Animation.Play example. Let the S and J keys start
        /// // a spin or jump animation. Let Space play back spin and
        /// // jump at the same time. Let Z play spin and jump with
        /// // spin doubled in speed.
        /// //
        /// // Spin: rotate the cube 360 degrees in half or one second
        /// // Jump: bounce up to 2 units and down in one second
        /// //
        /// // Note: AnimationState.layer is no longer supported, but still exists.
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    private Animation anim;
        ///
        ///    void Start()
        ///    {
        ///        anim = gameObject.GetComponent<Animation>();
        ///        anim["spin"].layer = 123;
        ///    }
        ///
        ///    // double the spin speed when true
        ///    private bool fastSpin = false;
        ///
        ///    void Update()
        ///    {
        ///        // leave spin or jump to complete before changing
        ///        if (anim.isPlaying)
        ///        {
        ///            return;
        ///        }
        ///
        ///        if (Input.GetKeyDown(KeyCode.S))
        ///        {
        ///            Debug.Log("Spinning");
        ///            anim.Play("spin");
        ///        }
        ///
        ///        if (Input.GetKeyDown(KeyCode.J))
        ///        {
        ///            Debug.Log("Jumping");
        ///            anim.Play("jump");
        ///        }
        ///
        ///        // combine jump and spin
        ///        if (Input.GetKeyDown(KeyCode.Space))
        ///        {
        ///            Debug.Log("Jumping and spinning");
        ///            anim.Play("jump");
        ///            anim.Play("spin");
        ///        }
        ///
        ///        // have spin speed reverted to 1.0 second
        ///        if (fastSpin == true)
        ///        {
        ///            anim["spin"].speed = 1.0f;
        ///            fastSpin = false;
        ///        }
        ///
        ///        if (Input.GetKeyDown(KeyCode.Z))
        ///        {
        ///            Debug.Log("Jumping and spinning in half a second");
        ///            anim.Play("jump");
        ///            anim["spin"].speed = 2.0f;
        ///            anim.Play("spin");
        ///
        ///            // we've used spin at a speed of two, now mark
        ///            // the spin speed to revert back to one
        ///            fastSpin = true;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public bool Play(string animation, [uei.DefaultValue("PlayMode.StopSameLayer")] PlayMode mode);

        [uei.ExcludeFromDocs] public void CrossFade(string animation) { CrossFade(animation, 0.3f); }
        [uei.ExcludeFromDocs] public void CrossFade(string animation, float fadeLength) { CrossFade(animation, fadeLength, PlayMode.StopSameLayer); }
        ///<summary>Fades in the animation with the name <c>animation</c> over a period of time defined by <c>fadeLength</c>.</summary>
        ///<remarks>
        ///  <para>If the mode is set to <see cref="PlayMode.StopSameLayer" />, animations on the same layer as <c>animation</c> are faded out while <c>animation</c> is faded in.
        ///if the mode is set to <see cref="PlayMode.StopAll" />, all animations are faded out while <c>animation</c> is faded in.
        ///
        ///If the animation is not set to be looping, it will be stopped and rewound after playing.
        ///
        ///The following example demonstrates how to switch between two animations using the CrossFade method.</para>
        ///  <para />
        ///</remarks>
        ///<param name="animation">The name of the animation in the <c>Animation</c> component to crossfade to.</param>
        ///<param name="fadeLength">The duration of the crossfade in seconds. Negative values are clamped to 0 seconds.</param>
        ///<param name="mode">The layer behavior of the crossfade. This controls whether the crossfade is with animations on all layers or animations on the same later as <c>animation</c>.</param>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/AnimationCrossFadeExample.cs}]]></code>
        ///</example>
        ///<seealso cref="Animation.Play" />
        ///<seealso cref="PlayMode" />
        extern public void CrossFade(string animation, [uei.DefaultValue("0.3F")] float fadeLength, [uei.DefaultValue("PlayMode.StopSameLayer")] PlayMode mode);

        [uei.ExcludeFromDocs] public void Blend(string animation) { Blend(animation, 1.0f); }
        [uei.ExcludeFromDocs] public void Blend(string animation, float targetWeight) { Blend(animation, targetWeight, 0.3f); }
        ///<summary>Blends the animation named <c>animation</c> towards <c>targetWeight</c> over the next <c>time</c> seconds.</summary>
        ///<remarks>Playback of other animations will not be affected.</remarks>
        extern public void Blend(string animation, [uei.DefaultValue("1.0F")] float targetWeight, [uei.DefaultValue("0.3F")] float fadeLength);

        [uei.ExcludeFromDocs] public AnimationState CrossFadeQueued(string animation) { return CrossFadeQueued(animation, 0.3F); }
        [uei.ExcludeFromDocs] public AnimationState CrossFadeQueued(string animation, float fadeLength) { return CrossFadeQueued(animation, fadeLength, QueueMode.CompleteOthers); }
        [uei.ExcludeFromDocs] public AnimationState CrossFadeQueued(string animation, float fadeLength, QueueMode queue) { return CrossFadeQueued(animation, fadeLength, queue, PlayMode.StopSameLayer); }
        ///<summary>Cross fades an animation after previous animations has finished playing.</summary>
        ///<remarks>For example you might play a specific sequence of animations after each other.
        ///
        ///The animation duplicates itself before playing thus you can fade between the same animation.
        ///This can be used to overlay two same animations. For example you might have a sword swing animation.
        ///The player slashes two times quickly after each other.
        ///You could rewind the animation and play from the beginning but then you will get a jump in the animation.
        ///
        ///The following <see cref="QueueMode">queue modes</see> are available: 
        ///
        ///If <c>queue</c> is <see cref="QueueMode.CompleteOthers" /> this animation will only start once all other animations have stopped playing. 
        ///
        ///If <c>queue</c> is <see cref="QueueMode.PlayNow" /> this animation will start playing immediately on a duplicated animation state.
        ///
        ///After the animation has finished playing it will automatically clean itself up. Using the duplicated animation state after it has finished will result in an exception.</remarks>
        [FreeFunction("AnimationBindings::CrossFadeQueuedImpl", HasExplicitThis = true)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern public AnimationState CrossFadeQueued(string animation, [uei.DefaultValue("0.3F")] float fadeLength, [uei.DefaultValue("QueueMode.CompleteOthers")] QueueMode queue, [uei.DefaultValue("PlayMode.StopSameLayer")] PlayMode mode);

        [uei.ExcludeFromDocs] public AnimationState PlayQueued(string animation) { return PlayQueued(animation, QueueMode.CompleteOthers); }
        [uei.ExcludeFromDocs] public AnimationState PlayQueued(string animation, QueueMode queue) { return PlayQueued(animation, queue, PlayMode.StopSameLayer); }
        ///<summary>Plays an animation after previous animations has finished playing.</summary>
        ///<remarks>For example you might play a specific sequence of animations after each other.
        ///
        ///The animation state duplicates itself before playing thus you can fade between the same animation.
        ///This can be used to overlay two same animations. For example you might have a sword swing animation.
        ///The player slashes two times quickly after each other.
        ///You could rewind the animation and play from the beginning but then you will get a jump in the animation.
        ///
        ///The following <see cref="QueueMode">queue modes</see> are available: 
        ///
        ///If <c>queue</c> is <see cref="QueueMode.CompleteOthers" /> this animation will only start once all other animations have stopped playing. 
        ///
        ///If <c>queue</c> is <see cref="QueueMode.PlayNow" /> this animation will start playing immediately on a duplicated animation state.
        ///
        ///After the animation has finished playing it will automatically clean itself up. Using the duplicated animation state after it has finished will result in an exception.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Animation anim = GetComponent<Animation>();
        ///
        ///        //Queues each of these animations to be played one after the other
        ///        anim.PlayQueued("CubeBob", QueueMode.CompleteOthers);
        ///        anim.PlayQueued("CubeFlip", QueueMode.CompleteOthers);
        ///        anim.PlayQueued("CubeShuffle", QueueMode.CompleteOthers);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [FreeFunction("AnimationBindings::PlayQueuedImpl", HasExplicitThis = true)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern public AnimationState PlayQueued(string animation, [uei.DefaultValue("QueueMode.CompleteOthers")] QueueMode queue, [uei.DefaultValue("PlayMode.StopSameLayer")] PlayMode mode);

        ///<summary>Adds a <c>clip</c> to the animation with name <c>newName</c>.</summary>
        public void AddClip(AnimationClip clip, string newName) { AddClip(clip, newName, Int32.MinValue, Int32.MaxValue); }
        [uei.ExcludeFromDocs] public void AddClip(AnimationClip clip, string newName, int firstFrame, int lastFrame) { AddClip(clip, newName, firstFrame, lastFrame, false); }
        ///<summary>Adds <c>clip</c> to the only play between <c>firstFrame</c> and <c>lastFrame</c>. The new clip will also be added to the animation with name <c>newName</c>.</summary>
        ///<remarks>If a clip with that name already exists it will be replaced with the new clip.</remarks>
        ///<param name="addLoopFrame">Should an extra frame be inserted at the end that matches the first frame? Turn this on if you are making a looping animation.</param>
        extern public void AddClip([NotNull] AnimationClip clip, string newName, int firstFrame, int lastFrame, [uei.DefaultValue("false")] bool addLoopFrame);

        ///<summary>Remove clip from the animation list.</summary>
        ///<remarks>This willl remove the clip and any animation states based on it.</remarks>
        extern public void RemoveClip([NotNull] AnimationClip clip);

        ///<summary>Remove clip from the animation list.</summary>
        ///<remarks>This willl remove the animation state that match the name.</remarks>
        public void RemoveClip(string clipName) { RemoveClipNamed(clipName); }
        [NativeName("RemoveClip")] extern private void RemoveClipNamed(string clipName);

        ///<summary>Get the number of clips currently assigned to this animation.</summary>
        extern public int GetClipCount();

        [System.Obsolete("use PlayMode instead of AnimationPlayMode.")]
        public bool Play(AnimationPlayMode mode) { return PlayDefaultAnimation((PlayMode)mode); }
        [System.Obsolete("use PlayMode instead of AnimationPlayMode.")]
        public bool Play(string animation, AnimationPlayMode mode) { return Play(animation, (PlayMode)mode); }

        ///<exclude />
        extern public void SyncLayer(int layer);

        ///<exclude />
        public IEnumerator GetEnumerator() { return new Animation.Enumerator(this); }

        private sealed partial class Enumerator : IEnumerator
        {
            Animation m_Outer;
            int m_CurrentIndex = -1;

            internal Enumerator(Animation outer) { m_Outer = outer; }
            public object Current
            {
                get { return m_Outer.GetStateAtIndex(m_CurrentIndex); }
            }
            public bool MoveNext()
            {
                int childCount = m_Outer.GetStateCount();
                m_CurrentIndex++;
                return m_CurrentIndex < childCount;
            }

            public void Reset() { m_CurrentIndex = -1; }
        }

        [FreeFunction("AnimationBindings::GetState", HasExplicitThis = true)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern internal AnimationState GetState(string name);

        [FreeFunction("AnimationBindings::GetStateAtIndex", HasExplicitThis = true, ThrowsException = true)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern internal AnimationState GetStateAtIndex(int index);

        [NativeName("GetAnimationStateCount")] extern internal int GetStateCount();

        ///<exclude />
        public AnimationClip GetClip(string name)
        {
            AnimationState state = GetState(name);
            if (state)
                return state.clip;
            else
                return null;
        }

        ///<summary>When enabled, the physics system uses animated transforms from GameObjects with kinematic Rigidbody components to influence other GameObjects.</summary>
        ///<remarks>For example, enable animatePhysics to apply velocity and friction from an animated platform to GameObjects on the platform.
        ///For velocity and friction to be applied, the platform GameObject must have a kinematic Rigidbody. To make a Rigidbody kinematic, enable the Is Kinematic property in the Rigidbody component.</remarks>
        extern public bool animatePhysics { get; set; }

        ///<summary>Specifies the update mode of the <see cref="Animation" />.</summary>
        extern public AnimationUpdateMode updateMode { get; set; }

        ///<summary>When turned on, Unity might stop animating if it thinks that the results of the animation won't be visible to the user.</summary>
        ///<remarks>This could save you some performance if you have things animating outside of the viewport, whose animation is only important
        ///when the user can actually see the thing that is being animated.</remarks>
        [System.Obsolete("Use cullingType instead")]
        public extern bool animateOnlyIfVisible
        {
            [FreeFunction("AnimationBindings::GetAnimateOnlyIfVisible", HasExplicitThis = true)]
            get;
            [FreeFunction("AnimationBindings::SetAnimateOnlyIfVisible", HasExplicitThis = true)]
            set;
        }

        ///<summary>Controls culling of this Animation component.</summary>
        ///<seealso cref="AnimationCullingType" />
        extern public AnimationCullingType cullingType { get; set; }
        ///<summary>AABB of this Animation animation component in local space.</summary>
        ///<remarks>By default it is computed based on animation states (i.e. attached animation clips), unless user overrides it by setting value to localBounds.</remarks>
        extern public Bounds localBounds { [NativeName("GetLocalAABB")] get; [NativeName("SetLocalAABB")] set; }
    }

    ///<summary>The AnimationState gives full control over animation blending.</summary>
    ///<remarks>In most cases the <see cref="Animation" /> interface is sufficient and easier to use.
    ///Use the AnimationState if you need full control over the animation blending any playback process.
    ///
    ///The AnimationState interface allows you to modify speed, weight, time and layers while any animation is playing.
    ///You can also setup animation mixing and wrapMode.
    ///
    ///The Animation.</remarks>
    [NativeHeader("Modules/Animation/AnimationState.h")]
    [UsedByNativeCode]
    public sealed class AnimationState : TrackedReference
    {
        ///<summary>Enables / disables the animation.</summary>
        ///<remarks>For the animation to take any effect the weight also needs to be set to a value higher than zero.
        ///If the animation is disabled, time will be paused until the animation is enabled again.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    public Animation anim;
        ///
        ///    void Start()
        ///    {
        ///        // Enable the walk cycle
        ///        anim["Walk"].enabled = true;
        ///        anim["Walk"].weight = 1.0f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public bool enabled { get; set; }
        ///<summary>The weight of animation.</summary>
        ///<remarks>This calculates the blend weights for one curve.
        ///
        ///Weights are distributed so that the top layer gets everything.
        ///If it doesn't use the full weight then the next layer gets to distribute the remaining
        ///weights and so on. Once all weights are used by the top layers,
        ///no weights will be available for lower layers anymore
        ///Unity uses fair weighting, which means if a lower layer wants 80% and 50% have already been used up, the layer will NOT use up all weights.
        ///instead it will take up 80% of the 50%.
        ///
        ///**Example:**
        ///a upper body which is affected by wave, walk and idle
        ///a lower body which is affected by only walk and idle.
        ///
        ///- Blend weights can change per animated value because of mixing.
        ///Even without mixing, sometimes a curve is just not defined. Still you want the blend weights to add up to 1.
        ///Most of the time weights are similar between curves.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    public Animation anim;
        ///
        ///    void Start()
        ///    {
        ///        // Set the blend weight of the walk animation to 0.5
        ///        anim["Walk"].weight = 0.5f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float weight { get; set; }
        ///<summary>Wrapping mode of the animation.</summary>
        ///<remarks>By default wrapMode is initialized to the value set in the [Animation component's](xref:class-Animation) wrap mode.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    public Animation anim;
        ///
        ///    void Start()
        ///    {
        ///        // Set the wrap mode of the walk animation to loop
        ///        anim["Walk"].wrapMode = WrapMode.Loop;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public WrapMode wrapMode { get; set; }
        ///<summary>The current time of the animation.</summary>
        ///<remarks>If the time is larger than length it will be wrapped according to wrapMode.
        ///The value can be larger than the animations length.
        ///In this case playback mode will remap the time before sampling.
        ///This value usually goes from 0 to infinity.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    public Animation anim;
        ///
        ///    void Start()
        ///    {
        ///        // Rewind the walk animation
        ///        anim["Walk"].time = 0.0f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float time { get; set; }
        ///<summary>Normalized time of the State.</summary>
        ///<remarks>The normalized time is a progression ratio. The integer part is the number of times the State has looped.
        ///The fractional part is a percentage (0-1) that represents the progress of the current loop.
        ///For example, a normalized time of 0.5 means that the State has not looped (0) and is halfway (50% or .5) through the first loop.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    public Animation anim;
        ///
        ///    void Start()
        ///    {
        ///        // Fast forward to the middle of the animation
        ///        anim["Walk"].normalizedTime = 0.5f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float normalizedTime { get; set; }
        ///<summary>The playback speed of the animation. 1 is normal playback speed.</summary>
        ///<remarks>A negative playback speed will play the animation backwards.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    public Animation anim;
        ///
        ///    void Start()
        ///    {
        ///        // Walk at normal speed
        ///        anim["Walk"].speed = 1.0f;
        ///
        ///        // Walk at double speed
        ///        anim["Walk"].speed = 2.0f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AnimationState.time" />
        ///<seealso cref="AnimationState.wrapMode" />
        ///<seealso cref="WrapMode" />
        extern public float speed { get; set; }
        ///<summary>The normalized playback speed.</summary>
        ///<remarks>This is most commonly used to synchronize playback speed when blending between two animations.
        ///In most cases it is easier and better to use [Animation Layer syncing](xref:AnimationLayers) instead.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Animation anim;
        ///
        ///    void Start()
        ///    {
        ///        anim = GetComponent<Animation>();
        ///        anim["Run"].normalizedSpeed = anim["Walk"].normalizedSpeed;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float normalizedSpeed { get; set; }
        ///<summary>The length of the animation clip in seconds.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    public Animation anim;
        ///
        ///    void Start()
        ///    {
        ///        // Print the length of the walk animation in seconds
        ///        print(anim["Walk"].length);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float length { get; }
        ///<exclude />
        extern public int layer { get; set; }
        ///<summary>The clip that is being played by this animation state.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    public Animation anim;
        ///
        ///    void Update()
        ///    {
        ///        // Prints the frame rate of the animation clip to the console
        ///        print(anim["walk"].clip.frameRate);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public AnimationClip clip { get; }
        ///<summary>The name of the animation.</summary>
        extern public string name { get; set; }
        ///<summary>Which blend mode should be used?</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    public Animation anim;
        ///
        ///    void Start()
        ///    {
        ///        // Set the leanLeft animation to blend additively
        ///        anim["leanLeft"].blendMode = AnimationBlendMode.Additive;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public AnimationBlendMode blendMode { get; set; }

        [uei.ExcludeFromDocs] public void AddMixingTransform(Transform mix) { AddMixingTransform(mix, true); }
        ///<summary>Adds a transform which should be animated. This allows you to reduce the number of animations you have to create.</summary>
        ///<remarks>
        ///  <para>For example you might have a handwaving animation.
        ///You might want to play the hand waving animation on a idle character or on a walking character.
        ///Either you have to create 2 hand waving animations one for idle, one for walking.
        ///By using mixing the hand waving animation will have full control of the shoulder. But the lower body will not be affected by it, and continue playing the idle or walk animation.
        ///Thus you only need one hand waving animation.
        ///
        ///If <c>recursive</c> is true all children of the <c>mix</c> transform will also be animated.
        ///If you don't call AddMixingTransform, all animation curves will be used.</para>
        ///  <para>Another example using a path:</para>
        ///</remarks>
        ///<param name="mix">The transform to animate.</param>
        ///<param name="recursive">Whether to also animate all children of the specified transform.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    public Animation anim;
        ///    public Transform shoulder;
        ///
        ///    void Start()
        ///    {
        ///        // Add mixing transform
        ///        anim["wave_hand"].AddMixingTransform(shoulder);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    public Animation anim;
        ///
        ///    void Start()
        ///    {
        ///        // Adds a mixing transform using a path instead
        ///        Transform mixTransform = transform.Find("root/upper_body/left_shoulder");
        ///
        ///        // Add mixing transform
        ///        anim["wave_hand"].AddMixingTransform(mixTransform);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public void AddMixingTransform([NotNull] Transform mix, [uei.DefaultValue("true")] bool recursive);

        ///<summary>Removes a transform which should be animated.</summary>
        ///<remarks>You can only pass transforms that have been added through <see cref="AddMixingTransform" /> function. If transform has been
        ///added as <c>recursive</c>, then it will be removed as <c>recursive</c>. Once you remove all mixing transforms added to
        ///animation state all curves become animated again.</remarks>
        ///<seealso cref="AddMixingTransform" />
        extern public void RemoveMixingTransform([NotNull] Transform mix);

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(AnimationState animationState) => animationState.m_Ptr;
        }
    }

    ///<summary>AnimationEventInfo is a read-only, non-allocating struct that provides information about an animation event when it fires.</summary>
    ///<remarks>
    ///  <para>AnimationEventInfo is a ref struct that serves as a read-only view of animation event data when an event is fired during animation playback. Unlike <see cref="AnimationEvent" />, AnimationEventInfo is designed for reporting event information in callbacks. It cannot be used for authoring or be stored in fields.
    ///
    ///An animation event callback can accept either an AnimationEvent or an AnimationEventInfo parameter. Use AnimationEventInfo to avoid allocating the AnimationEvent object itself when an event fires.
    ///
    ///The parameter can access floats, ints, strings, and object references configured on the animation event.
    ///
    ///Note: The AnimationEventInfo struct itself doesn't allocate but accessing string properties (<see cref="functionName" /> and <see cref="stringParameter" />) causes string allocation when retrieving from native code. Accessing non-string properties (such as <c>time</c>, <c>floatParameter</c>, <c>intParameter</c>, <c>objectReferenceParameter</c>, <c>isFiredByLegacy</c>, <c>isFiredByAnimator</c>, <c>animatorStateInfo</c>, and <c>animatorClipInfo</c>) causes no allocations.</para>
    ///  <para>A more detailed example shows how to access animator state information:</para>
    ///</remarks>
    ///<example>
    ///  <code><![CDATA[
    /// // AnimationEventInfo example
    /// // Non-allocating event callback that reads event parameters
    ///
    ///using UnityEngine;
    ///
    ///public class Example : MonoBehaviour
    ///{
    ///    // This callback receives AnimationEventInfo instead of AnimationEvent
    ///    // which avoids allocations when the event fires
    ///    public void OnAnimationEvent(AnimationEventInfo eventInfo)
    ///    {
    ///        Debug.Log($"Event '{eventInfo.functionName}' fired at time {eventInfo.time}");
    ///        Debug.Log($"Parameters: int={eventInfo.intParameter}, float={eventInfo.floatParameter}");
    ///        Debug.Log($"String parameter: {eventInfo.stringParameter}");
    ///
    ///        if (eventInfo.objectReferenceParameter != null)
    ///        {
    ///            Debug.Log($"Object reference: {eventInfo.objectReferenceParameter.name}");
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<example>
    ///  <code><![CDATA[
    /// // Accessing animator state information from AnimationEventInfo
    ///using UnityEngine;
    ///
    ///public class Example : MonoBehaviour
    ///{
    ///    public void OnAnimatorEvent(AnimationEventInfo eventInfo)
    ///    {
    ///        // Check the event source
    ///        if (eventInfo.isFiredByAnimator)
    ///        {
    ///            // Access animator state information
    ///            AnimatorStateInfo stateInfo = eventInfo.animatorStateInfo;
    ///            Debug.Log($"State: {stateInfo.fullPathHash}, Time: {stateInfo.normalizedTime}");
    ///
    ///            // Access clip information
    ///            AnimatorClipInfo clipInfo = eventInfo.animatorClipInfo;
    ///            if (clipInfo.clip != null)
    ///            {
    ///                Debug.Log($"Clip: {clipInfo.clip.name}, Weight: {clipInfo.weight}");
    ///            }
    ///        }
    ///        else if (eventInfo.isFiredByLegacy)
    ///        {
    ///            // Access legacy animation state
    ///            AnimationState animState = eventInfo.animationState;
    ///            if (animState != null)
    ///            {
    ///                Debug.Log($"Animation: {animState.name}, Time: {animState.time}");
    ///            }
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public ref struct AnimationEventInfo
    {
        private IntPtr m_EventPtr;
        private float m_Time;
        private float m_FloatParameter;
        private int m_IntParameter;

        private int m_MessageOptions;
        private AnimationEventSource m_Source;
        private AnimatorStateInfo m_AnimatorStateInfo;
        private AnimatorClipInfo m_AnimatorClipInfo;
        
        ///<summary>String parameter that is stored in the event (Read Only).</summary>
        ///<remarks>Returns the string parameter value that was configured on the animation event in the Animation window or via script when authoring the animation clip.
        ///Note: Accessing this property allocates memory as the string is retrieved from native code.</remarks>
        public string stringParameter => GetStringParameterInternal(m_EventPtr);
        ///<summary>Float parameter that is stored in the event (Read Only).</summary>
        ///<remarks>Returns the float parameter value that was configured on the animation event in the Animation window or via script when authoring the animation clip.</remarks>
        public float floatParameter => m_FloatParameter;
        ///<summary>Integer parameter that is stored in the event (Read Only).</summary>
        ///<remarks>Returns the integer parameter value that was configured on the animation event in the Animation window or via script when authoring the animation clip.</remarks>
        public int intParameter => m_IntParameter;
        ///<summary>Object reference parameter that is stored in the event (Read Only).</summary>
        ///<remarks>Returns the Unity Object reference that was configured on the animation event. This can be any type derived from <see cref="UnityEngine.Object" />, such as GameObjects, Components, ScriptableObjects, or Assets.
        ///Returns null if no object reference was set on the event.</remarks>
        public Object objectReferenceParameter => GetObjectReferenceParameterInternal(m_EventPtr);
        ///<summary>The name of the called function (Read Only).</summary>
        ///<remarks>Returns the name of the callback function invoked by this animation event.
        ///This corresponds to the function name that was configured on the <see cref="AnimationEvent" /> in the Animation window.
        ///Note: Accessing this property allocates memory as the string is retrieved from native code.</remarks>
        public string functionName => GetFunctionNameParameter(m_EventPtr);
        ///<summary>The time at which the event is being fired (Read Only).</summary>
        ///<remarks>Returns the time value in seconds within the animation clip.
        ///For example, if <c>time</c> is 1.5f for an animation clip with a length of 2 seconds, the event fires 1.5 seconds after the animation starts. If the animation clip loops, the event fires at the same time in each iteration.</remarks>
        public float time => m_Time;

        ///<summary>Returns true if this animation event was fired by an Animation component (Read Only).</summary>
        ///<remarks>Use this property to determine if the event was fired by a legacy <see cref="Animation" /> component. When true, the <see cref="animationState" /> property is set. Otherwise, <see cref="animationState" /> is null.</remarks>
        public bool isFiredByLegacy => m_Source == AnimationEventSource.Legacy;
        ///<summary>Returns true if this animation event was fired by an Animator component (Read Only).</summary>
        ///<remarks>Use this property to determine if the event was fired by an <see cref="Animator" /> component. When true, the <see cref="animatorStateInfo" /> and <see cref="animatorClipInfo" /> properties are set. Otherwise, these properties will contain default values.</remarks>
        public bool isFiredByAnimator => m_Source == AnimationEventSource.Animator;

        ///<summary>The animation state that fired this event (Read Only).</summary>
        ///<remarks>Returns null when the method is called outside of an animation event callback.
        ///Note: This member is only set when the event is fired from an Animation component (legacy).</remarks>
        ///<seealso cref="AnimationState" />
        ///<seealso cref="isFiredByLegacy" />
        public AnimationState animationState
        {
            get
            {
                if (!isFiredByLegacy)
                    Debug.LogError("AnimationEvent was not fired by Animation component, you shouldn't use AnimationEvent.animationState");


                return GetStateSenderInternal(m_EventPtr);
            }
        }

        [FreeFunction("AnimationBindings::GetEventStringParameter")]
        extern static string GetStringParameterInternal(IntPtr eventPtr);


        [FreeFunction("AnimationBindings::GetEventFunctionName")]
        extern static string GetFunctionNameParameter(IntPtr eventPtr);


        [FreeFunction("AnimationBindings::GetEventObjectReferenceParameter")]
        extern static Object GetObjectReferenceParameterInternal(IntPtr eventPtr);

        [FreeFunction("AnimationBindings::GetEventAnimationState")]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern static AnimationState GetStateSenderInternal(IntPtr eventPtr);

        ///<summary>The animator state info related to this event (Read Only).</summary>
        ///<remarks>This property contains information about the animator state that triggered the event, including normalized time, length, and state hashes.
        ///Note: The data in this member is only set when the event is fired from an Animator Component. When fired by an Animation component it contains default values.</remarks>
        ///<seealso cref="AnimatorStateInfo" />
        ///<seealso cref="isFiredByAnimator" />
        public AnimatorStateInfo animatorStateInfo
        {
            get
            {
                if (!isFiredByAnimator)
                    Debug.LogError("AnimationEvent was not fired by Animator component, you shouldn't use AnimationEvent.animatorStateInfo");
                return m_AnimatorStateInfo;
            }
        }

        ///<summary>The animator clip info related to this event (Read Only).</summary>
        ///<remarks>This property contains information about the animation clip that triggered the event, including the clip reference and its weight.
        ///Note: The data in this member is only set when the event is fired from an Animator Component. When fired by an Animation component it contains default values.</remarks>
        ///<seealso cref="AnimatorClipInfo" />
        ///<seealso cref="isFiredByAnimator" />
        public AnimatorClipInfo animatorClipInfo
        {
            get
            {
                if (!isFiredByAnimator)
                    Debug.LogError("AnimationEvent was not fired by Animator component, you shouldn't use AnimationEvent.animatorClipInfo");
                return m_AnimatorClipInfo;
            }
        }

      
    }

    ///<summary>AnimationEvent lets you call a script function similar to SendMessage as part of playing back an animation.</summary>
    ///<remarks>
    ///  <para>Animation events support functions that take zero or one parameter.
    ///The parameter can be a float, an int, a string, an object reference, or an AnimationEvent.</para>
    ///  <para>A more detailed example below shows a more complex
    ///      way of creating an animation.  In this script example the <c>Animator</c>
    ///      component is accessed and a <c>Clip</c> from it obtained.  (This clip was
    ///      set up in the Animation window.)  The clip lasts for 2 seconds.  An
    ///      <c>AnimationEvent</c> is created, and has parameters set.  The parameters include
    ///      the function <c>PrintEvent()</c> which will handle the event. The event is then
    ///      added to the clip.  This all happens in <c>Start()</c>.  Once the game has launched
    ///      the event is called after 1.3s and then repeats every 2s.</para>
    ///</remarks>
    ///<example>
    ///  <code><![CDATA[
    /// // Animation Event example
    /// // Small example that can be called on each specified frame.
    /// // The code is executed once per animation loop.
    ///
    ///using UnityEngine;
    ///using System.Collections;
    ///
    ///public class Example : MonoBehaviour
    ///{
    ///    public void PrintEvent()
    ///    {
    ///        Debug.Log("PrintEvent");
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<example>
    ///  <code><![CDATA[
    /// // Add an Animation Event to a GameObject that has an Animator
    ///using UnityEngine;
    ///using System.Collections;
    ///
    ///public class Example : MonoBehaviour
    ///{
    ///    public void Start()
    ///    {
    ///        // existing components on the GameObject
    ///        AnimationClip clip;
    ///        Animator anim;
    ///
    ///        // new event created
    ///        AnimationEvent evt;
    ///        evt = new AnimationEvent();
    ///
    ///        // put some parameters on the AnimationEvent
    ///        //  - call the function called PrintEvent()
    ///        //  - the animation on this object lasts 2 seconds
    ///        //    and the new animation created here is
    ///        //    set up to happen 1.3s into the animation
    ///        evt.intParameter = 12345;
    ///        evt.time = 1.3f;
    ///        evt.functionName = "PrintEvent";
    ///
    ///        // get the animation clip and add the AnimationEvent
    ///        anim = GetComponent<Animator>();
    ///        clip = anim.runtimeAnimatorController.animationClips[0];
    ///        clip.AddEvent(evt);
    ///    }
    ///
    ///    // the function to be called as an event
    ///    public void PrintEvent(int i)
    ///    {
    ///        print("PrintEvent: " + i + " called at: " + Time.time);
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [System.Serializable]
    [RequiredByNativeCode]
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/Animation/AnimationEvent.h")]
    public sealed class AnimationEvent
    {
        [NativeName("time")]
        internal float m_Time;
        [NativeName("functionName")]
        internal string m_FunctionName;
        [NativeName("stringParameter")]
        internal string m_StringParameter;
        [NativeName("objectReferenceParameter")]
        internal Object m_ObjectReferenceParameter;
        [NativeName("floatParameter")]
        internal float m_FloatParameter;
        [NativeName("intParameter")]
        internal int m_IntParameter;

        [NativeName("messageOptions")]
        internal int m_MessageOptions;
        [NativeName("source")]
        internal AnimationEventSource m_Source;
        [NativeName("stateSender")]
        [UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        internal AnimationState m_StateSender;
        [NativeName("animatorStateInfo")]
        internal AnimatorStateInfo m_AnimatorStateInfo;
        [NativeName("animatorClipInfo")]
        internal AnimatorClipInfo m_AnimatorClipInfo;

        ///<summary>Creates a new animation event.</summary>
        public AnimationEvent()
        {
            m_Time = 0.0f;
            m_FunctionName = "";
            m_StringParameter = "";
            m_ObjectReferenceParameter = null;
            m_FloatParameter = 0.0f;
            m_IntParameter = 0;
            m_MessageOptions = 0;
            m_Source = AnimationEventSource.NoSource;
            m_StateSender = null;
        }

        [System.Obsolete("Use stringParameter instead")]
        public string data { get { return m_StringParameter; } set { m_StringParameter = value; } }

        ///<summary>String parameter that is stored in the event and will be sent to the function.</summary>
        public string stringParameter { get { return m_StringParameter; } set { m_StringParameter = value; } }
        ///<summary>Float parameter that is stored in the event and will be sent to the function.</summary>
        public float floatParameter { get { return m_FloatParameter; } set { m_FloatParameter = value; } }
        ///<summary>Int parameter that is stored in the event and will be sent to the function.</summary>
        public int intParameter { get { return m_IntParameter; } set { m_IntParameter = value; } }
        ///<summary>Object reference parameter that is stored in the event and will be sent to the function.</summary>
        public Object objectReferenceParameter { get { return m_ObjectReferenceParameter; } set { m_ObjectReferenceParameter = value; } }
        ///<summary>The name of the function that will be called.</summary>
        ///<remarks>This is the same as calling gameObject.SendMessage(animationEvent.functionName, animationEvent).
        ///        The function called can have zero or a single argument.  See the script example at
        ///        <see cref="AnimationEvent" /> to see how <c>functionName</c> can be used.</remarks>
        public string functionName { get { return m_FunctionName; } set { m_FunctionName = value; } }
        ///<summary>The time at which the event will be fired off.</summary>
        ///<remarks>The <see cref="AnimationEvent" /> obtains the clip length from its attached clip.
        ///        The time property determines when the event is processed. For example, if the clip length is
        ///        2s and <c>time</c> is set to 1.5f, then the function is called 1.5s after the animation
        ///        starts, and then every 2s. The example on the <see cref="AnimationEvent" /> page shows how to use the
        ///        <c>time</c> property.</remarks>
        public float time { get { return m_Time; } set { m_Time = value; } }
        ///<summary>Function call options.</summary>
        ///<remarks>If options are set to <see cref="SendMessageOptions.RequireReceiver" /> (default), an error is printed when the message is not picked up by any component.</remarks>
        public SendMessageOptions messageOptions { get { return (SendMessageOptions)m_MessageOptions; } set { m_MessageOptions = (int)value; } }

        ///<summary>Returns true if this Animation event has been fired by an Animation component.</summary>
        public bool isFiredByLegacy { get { return m_Source == AnimationEventSource.Legacy; } }
        ///<summary>Returns true if this Animation event has been fired by an Animator component.</summary>
        public bool isFiredByAnimator { get { return m_Source == AnimationEventSource.Animator; } }

        ///<summary>The animation state that fired this event (RO).</summary>
        ///<remarks>Returns null when the method is called outside of an animation event callback.
        ///Note: This member will only be set when called from an Animation component(legacy).</remarks>
        ///<seealso cref="AnimationState" />
        public AnimationState animationState
        {
            get
            {
                if (!isFiredByLegacy)
                    Debug.LogError("AnimationEvent was not fired by Animation component, you shouldn't use AnimationEvent.animationState");
                return m_StateSender;
            }
        }

        ///<summary>The animator state info related to this event (RO).</summary>
        ///<seealso cref="AnimatorStateInfo" />
        public AnimatorStateInfo animatorStateInfo
        {
            get
            {
                if (!isFiredByAnimator)
                    Debug.LogError("AnimationEvent was not fired by Animator component, you shouldn't use AnimationEvent.animatorStateInfo");
                return m_AnimatorStateInfo;
            }
        }

        ///<summary>The animator clip info related to this event (RO).</summary>
        ///<seealso cref="AnimatorClipInfo" />
        public AnimatorClipInfo animatorClipInfo
        {
            get
            {
                if (!isFiredByAnimator)
                    Debug.LogError("AnimationEvent was not fired by Animator component, you shouldn't use AnimationEvent.animatorClipInfo");
                return m_AnimatorClipInfo;
            }
        }

        internal int GetHash()
        {
            unchecked
            {
                int hash = 0;
                hash = functionName.GetHashCode();
                hash = 33 * hash + time.GetHashCode();
                return hash;
            }
        }

        [RequiredByNativeCode]
        internal static AnimationEvent CreateAnimationEvent(
            float time,
            string functionName,
            string stringParameter,
            Object objectReferenceParameter,
            float floatParameter,
            int intParameter,
            int messageOptions,
            AnimationEventSource source,
            AnimationState stateSender,
            AnimatorStateInfo animatorStateInfo,
            AnimatorClipInfo animatorClipInfo)
        {
            return new AnimationEvent
            {
                m_Time = time,
                m_FunctionName = functionName,
                m_StringParameter = stringParameter,
                m_ObjectReferenceParameter = objectReferenceParameter,
                m_FloatParameter = floatParameter,
                m_IntParameter = intParameter,
                m_MessageOptions = messageOptions,
                m_Source = source,
                m_StateSender = stateSender,
                m_AnimatorStateInfo = animatorStateInfo,
                m_AnimatorClipInfo = animatorClipInfo
            };
        }
    }
}
