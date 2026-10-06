// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;
namespace UnityEngine
{
    // Stores keyframe based animations.
    ///<summary>Provides an asset that assigns animation curves to animatable properties.</summary>
    ///<remarks>The <see cref="Animation" /> component and the <see cref="Animator" /> component use the <see cref="AnimationClip" /> asset to animate <see cref="GameObject" /> and <see cref="Component" /> properties.
    ///
    ///**Supported Animatable Types**
    ///
    ///When you create an <see cref="AnimationClip" /> asset with an <see cref="Animation" /> component, you must set <c>legacy</c> to true. Use the <see cref="Animation" /> component to animate the following public or serialized properties for a <see cref="GameObject" /> or <see cref="Component" />:
    ///
    ///* float values
    ///* boolean values (casted to float)
    ///* integer values (casted to float)
    ///* discrete integer values (using the attribute <see cref="T:UnityEngine.Animations.DiscreteEvaluationAttribute" /> and reinterpreted to float)
    ///
    ///For <c>legacy</c> clips, use the <see cref="SetCurve" /> method to assign new curves in the Editor and at runtime.
    ///
    ///When you create an <see cref="AnimationClip" /> asset for an <see cref="Animator" /> component, you can animate the following public or serialized properties:
    ///
    ///* float values
    ///* boolean values (cast as float)
    ///* integer values (cast as float)
    ///* discrete integer values (use the <see cref="T:UnityEngine.Animations.DiscreteEvaluationAttribute" /> attribute reinterpreted as a float)
    ///* <see cref="Object" /> reference values (saved as an <see cref="T:UnityEditor.ObjectReferenceKeyframe" />)
    ///
    ///**Curve creation**
    ///
    ///In the Editor, it is recommended that you use the <see cref="M:UnityEditor.AnimationUtility.SetEditorCurve" /> and <see cref="M:UnityEditor.AnimationUtility.SetEditorCurves" /> methods to assign one or many <see cref="AnimationCurve" /> objects for each float property and each boolean, integer, or discrete integer reinterpreted as a float.
    ///
    ///In the Editor and at runtime, you can use the <see cref="SetCurve" /> method to assign an <see cref="AnimationCurve" /> to an animation clip. 
    ///You can also modify an animation clip at runtime but this modification is only recognized by the <see cref="Animation" /> component. The <see cref="Animator" /> component requires that clips are compiled to an optimized representation and this compilation process is not available at runtime. 
    ///
    ///Use the <see cref="M:UnityEditor.AnimationUtility.SetObjectReferenceCurve" /> and <see cref="M:UnityEditor.AnimationUtility.SetObjectReferenceCurves" /> methods to create and assign new <see cref="T:UnityEditor.ObjectReferenceKeyframe" /> arrays of <see cref="Object" /> reference properties. **Note:** This is only supported by the <see cref="Animator" /> component.
    ///
    ///**Curve query**
    ///
    ///Use the <see cref="M:UnityEditor.AnimationUtility.GetEditorCurve" /> method to retrieve an <see cref="AnimationCurve" /> for a float property.
    ///
    ///Use the <see cref="M:UnityEditor.AnimationUtility.GetObjectReferenceCurve" /> method to retrieve an <see cref="T:UnityEditor.ObjectReferenceKeyframe" /> array for an
    ///<see cref="Object" /> reference property. **Note:** This is only supported by the <see cref="Animator" /> component.
    ///
    ///**Animation Events management**
    ///
    ///In the editor, use the <see cref="M:UnityEditor.AnimationUtility.SetAnimationEvents" /> method to set or replace the <see cref="AnimationEvent" /> array for the <see cref="AnimationClip" />.
    ///Use the <see cref="M:UnityEditor.AnimationUtility.GetAnimationEvents" /> method to retrieve the <see cref="AnimationEvent" /> array from the <see cref="AnimationClip" />.</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/LegacyAnimationClipExample.cs}]]></code>
    ///</example>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/Editor/DocumentationExamples/AnimationClipWithAnimationCurvesExample.cs}]]></code>
    ///</example>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/Editor/DocumentationExamples/AnimationClipWithObjectReferenceKeyframesExample.cs}]]></code>
    ///</example>
    ///<seealso cref="Animation" />
    ///<seealso cref="Animator" />
    ///<seealso cref="AnimationCurve" />
    ///<seealso cref="T:UnityEditor.ObjectReferenceKeyframe" />
    ///<seealso cref="AnimationEvent" />
    [NativeHeader("Modules/Animation/ScriptBindings/AnimationClip.bindings.h")]
    [global::UnityEngine.NativeClass("AnimationClip", PersistentTypeId = 74)]
    [NativeHeader("Modules/Animation/AnimationClip.h")]
    public sealed class AnimationClip : Motion
    {
        internal AnimationClip(global::UnityEngine.EntityId id) : base(id) {}
        // Creates a new animation clip
        ///<summary>Creates a new animation clip.</summary>
        public AnimationClip()
        {
            Internal_CreateAnimationClip(this);
        }

        // [Writable]: native binds this object as the wrapper before AwakeFromLoad, whose editor callback
        // (AnimationUtility.onCurveWasModified) hands the clip to managed code.
        [FreeFunction("AnimationClipBindings::Internal_CreateAnimationClip")]
        extern private static void Internal_CreateAnimationClip([Writable] AnimationClip self);

        // This method was moved here to prevent GameObject or the Core from depending on Animation.
        // Helps in modularizing managed code.
        ///<summary>Samples an animation at a given time for any animated properties.</summary>
        ///<remarks>It is recommended to use the <see cref="Animation" /> interface instead for performance reasons.
        ///This will sample <c>animation</c> at the given <c>time</c>.
        ///Any component properties that are animated in the clip will be replaced with the sampled value.
        ///Most of the time you want to use <see cref="Animation.Play" /> instead. SampleAnimation is useful when you need to jump between frames in an unordered way or
        ///based on some special input.</remarks>
        ///<param name="go">The animated game object.</param>
        ///<param name="time">The time to sample an animation.</param>
        ///<seealso cref="Animation" />
        public void SampleAnimation(GameObject go, float time)
        {
            SampleAnimation(go, this, time, this.wrapMode);
        }

        [NativeHeader("Modules/Animation/AnimationUtility.h")]
        [FreeFunction]
        extern internal static void SampleAnimation([NotNull] GameObject go, [NotNull] AnimationClip clip, float inTime, WrapMode wrapMode);


        // Animation length in seconds (RO)
        ///<summary>Animation length in seconds. (RO)</summary>
        [NativeProperty("Length", false, TargetType.Function)]
        public extern float length { get; }

        [NativeProperty("StartTime", false, TargetType.Function)]
        internal extern float startTime { get; }

        [NativeProperty("StopTime", false, TargetType.Function)]
        internal extern float stopTime { get; }

        ///<summary>Frame rate at which keyframes are sampled. (RO)</summary>
        ///<remarks>This is the frame rate that was used in the
        ///animation program you used to create the animation or model.</remarks>
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
        [NativeProperty("SampleRate", false, TargetType.Function)]

        // Frame rate at which keyframes are sampled (RO)
        public extern float frameRate { get; set; }

        ///<summary>Assigns the curve to animate a specific property.</summary>
        ///<remarks>
        ///  <para>If <c>curve</c> is null the curve will be removed. If a curve already exists
        ///for that property, it will be replaced.
        ///
        ///**Note:** <c>SetCurve</c> will only work at runtime for legacy animation clips. For non-legacy AnimationClips
        ///it is an editor-only function.
        ///
        ///        The following script example shows how a <c>GameObject</c> position can be animated using an
        ///        animation clip.  An animated curve is set onto the <see cref="AnimationClip" /> using <c>SetCurve()</c>.
        ///        This example moves the x offset from 1.0 down to 0.0.
        ///
        ///The <see cref="SetCurve" /> API can be used to animate a large variety of
        ///        parameters.  Some typical components such as <see cref="Transform" /> and <see cref="Material" /> have
        ///        easy to access variables. For example the <see cref="Transform" /> has variables such as
        ///        <see cref="Transform.localPosition" />.  The x, y, and z values of the <c>localPosition</c> can be
        ///        animated using the <see cref="AnimationClip" /> API.  View the <see cref="Transform" /> documentation to
        ///        see the variables and how they can be animated.
        ///
        ///        The <see cref="Material" /> class also links
        ///        to variables that can be animated.  These come from the shader that is used for
        ///        rendering.  Using the "Edit Shader..." option from the material drop down shows
        ///        all the parameters that can be animated. The material parameters are animated through
        ///        the Renderer class that references them. All animatable material parameters start with the <c>material</c> prefix followed
        ///        by the property name starting with an underscore. For example, color (<c>material._Color</c>) and scale
        ///        (<c>material._BumpScale</c>) can be animated.
        ///
        ///        The example script below shows how a GameObject can be animated in two ways at the
        ///        same time. In this example, the position of the GameObject is animated, and the
        ///        Material color is also changed over time.</para>
        ///  <para>Property names can be located by setting Asset Serialization to
        ///            Force Text mode in the Editor settings.  Use <c>Edit-&gt;Project Settings-&gt;Editor</c>
        ///            to enable this mode. The text files that are then written
        ///            by the editor will include the names of the properties.  For example, the yaml
        ///            file written for a Scene object will include the Camera settings.  Looking at this
        ///            yaml file will show:
        ///
        ///<c>m_BackGroundColor: {r: .192156866, g: .301960796, b: .474509805, a: .0196078438}</c><c>m_NormalizedViewPortRect:</c><c>  serializedVersion: 2</c><c>  x: 0</c><c>  y: 0</c><c>  width: 1</c><c>  height: 1</c><c>near clip plane: .300000012</c><c>far clip plane: 1000</c><c>field of view: 60</c><c>orthographic: 0</c><c>orthographic size: 5</c><c>m_Depth: -1</c>
        ///
        ///            This shows that the name for the FOV parameter is "field of view".  If you wanted to
        ///            create an animation clip to animate the camera field of view, you would pass "field of view"
        ///            as the propertyName.
        ///
        ///            Another example is the access of <c>Light</c> settings. The <c>scene.unity</c> file
        ///            (assuming a Scene called <c>scene</c>) will have a string for the light color.
        ///            Script can access the light color by accessing <c>m_Color</c>.  The Scene will need
        ///            to have a light for this example to work.
        ///
        ///</para>
        ///</remarks>
        ///<param name="relativePath">Path to the game object this curve applies to. The <c>relativePath</c>
        ///        is formatted similar to a pathname, e.g. "root/spine/leftArm".  If <c>relativePath</c>
        ///        is empty it refers to the game object the animation clip is attached to.</param>
        ///<param name="type">The class type of the component that is animated.</param>
        ///<param name="propertyName">The name or path to the property being animated.</param>
        ///<param name="curve">The animation curve.</param>
        ///<example>
        ///  <code><![CDATA[
        /// // This script example shows how SetCurve() can be used
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    // Animate the position and color of the GameObject
        ///    public void Start()
        ///    {
        ///        Animation anim = GetComponent<Animation>();
        ///        AnimationCurve curve;
        ///
        ///        // create a new AnimationClip
        ///        AnimationClip clip = new AnimationClip();
        ///        clip.legacy = true;
        ///
        ///        // create a curve to move the GameObject and assign to the clip
        ///        Keyframe[] keys;
        ///        keys = new Keyframe[3];
        ///        keys[0] = new Keyframe(0.0f, 0.0f);
        ///        keys[1] = new Keyframe(1.0f, 1.5f);
        ///        keys[2] = new Keyframe(2.0f, 0.0f);
        ///        curve = new AnimationCurve(keys);
        ///        clip.SetCurve("", typeof(Transform), "localPosition.x", curve);
        ///
        ///        // update the clip to a change the red color
        ///        curve = AnimationCurve.Linear(0.0f, 1.0f, 2.0f, 0.0f);
        ///        clip.SetCurve("", typeof(Renderer), "material._Color.r", curve);
        ///
        ///        // now animate the GameObject
        ///        anim.AddClip(clip, clip.name);
        ///        anim.Play(clip.name);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="ClearCurves" />
        ///<seealso cref="AnimationCurve" />
        [FreeFunction("AnimationClipBindings::Internal_SetCurve", HasExplicitThis = true)]
        public extern void SetCurve([NotNull] string relativePath, [NotNull] Type type, [NotNull] string propertyName, AnimationCurve curve);

        //*undocumented*
        ///<summary>Realigns quaternion keys to ensure shortest interpolation paths.</summary>
        ///<remarks>This function is called in order to ensure better interpolation of quaternions.
        ///          It should be called after animation curves are set.</remarks>
        public extern void EnsureQuaternionContinuity();

        // Clears all curves from the clip.
        ///<summary>Clears all curves from the clip.</summary>
        ///<seealso cref="SetCurve" />
        ///<seealso cref="AnimationCurve" />
        public extern void ClearCurves();

        // Sets the default wrap mode used in the animation state.
        ///<summary>Sets the default wrap mode used in the animation state.</summary>
        ///<remarks>Only used with Legacy AnimationClip.</remarks>
        [NativeProperty("WrapMode", false, TargetType.Function)]
        public extern WrapMode wrapMode { get; set; }

        // AABB of this Animation Clip in local space of Animation component that it is attached too.
        ///<summary>AABB of this Animation Clip in local space of Animation component that it is attached too.</summary>
        ///<remarks>It is precomputed on import for imported models/animations based on the meshes that this animation clip affects.
        ///This bounding box is specific to the mesh(es) that this clip is attached to during import, i.e. this means
        ///that it is calculated based on the file that is part of and on the "Model" file if you're using
        ///Model@Animation notation.</remarks>
        [NativeProperty("Bounds", false, TargetType.Function)]
        public extern Bounds localBounds { get; set; }

        ///<summary>Set to true if the AnimationClip will be used with the Legacy Animation component ( instead of the Animator ).</summary>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/LegacyAnimationClipExample.cs}]]></code>
        ///</example>
        extern public new bool legacy
        {
            [NativeMethod("IsLegacy")]
            get;
            [NativeMethod("SetLegacy")]
            set;
        }

        ///<summary>Returns true if the animation contains curve that drives a humanoid rig.</summary>
        extern public bool humanMotion
        {
            [NativeMethod("IsHumanMotion")]
            get;
        }

        ///<summary>Returns true if the animation clip has no curves and no events.</summary>
        extern public bool empty
        {
            [NativeMethod("IsEmpty")]
            get;
        }

        ///<summary>Returns true if the Animation has animation on the root transform.</summary>
        extern public bool hasGenericRootTransform
        {
            [NativeMethod("HasGenericRootTransform")]
            get;
        }

        ///<summary>Returns true if the AnimationClip has editor curves for its root motion.</summary>
        extern public bool hasMotionFloatCurves
        {
            [NativeMethod("HasMotionFloatCurves")]
            get;
        }

        ///<summary>Returns true if the AnimationClip has root motion curves.</summary>
        extern public bool hasMotionCurves
        {
            [NativeMethod("HasMotionCurves")]
            get;
        }

        ///<summary>Returns true if the AnimationClip has root Curves.</summary>
        extern public bool hasRootCurves
        {
            [NativeMethod("HasRootCurves")]
            get;
        }

        internal extern bool hasRootMotion
        {
            [FreeFunction(Name = "AnimationClipBindings::Internal_GetHasRootMotion", HasExplicitThis = true)]
            get;
        }

        ///<summary>Adds an animation event to the clip.</summary>
        ///<remarks>Note that events added with AddEvent persist until play mode
        ///is exited or the is player quit. If you want to add an event to a clip persistently,
        ///use <see cref="M:UnityEditor.AnimationUtility.SetAnimationEvents" /> from the Unity editor.</remarks>
        ///<param name="evt">AnimationEvent to add.</param>
        ///<seealso cref="AnimationEvent" />
        ///<seealso cref="T:UnityEditor.AnimationUtility" />
        public void AddEvent(AnimationEvent evt)
        {
            if (evt == null)
                throw new ArgumentNullException("evt");
            AddEventInternal(evt);
        }

        [FreeFunction(Name = "AnimationClipBindings::AddEventInternal", HasExplicitThis = true)]
        extern private void AddEventInternal([NotNull] AnimationEvent evt);

        // Retrieves all animation events associated with the animation clip
        ///<summary>Animation Events for this animation clip.</summary>
        ///<remarks>Modified events will only persist until play mode
        ///is exited or the player is quit. If you want to add an event to a clip persistently,
        ///use <see cref="M:UnityEditor.AnimationUtility.SetAnimationEvents" /> from the Unity editor.</remarks>
        ///<seealso cref="T:UnityEditor.AnimationUtility" />
        public AnimationEvent[] events
        {
            get => GetEventsInternal();
            set => SetEventsInternal(value);
        }
        [FreeFunction(Name = "AnimationClipBindings::SetEventsInternal", HasExplicitThis = true)]
        extern private void SetEventsInternal(AnimationEvent[] events);
        [FreeFunction(Name = "AnimationClipBindings::GetEventsInternal", HasExplicitThis = true)]
        extern private AnimationEvent[] GetEventsInternal();
    }

    unsafe class GCHandlePool
    {
        GCHandle[] m_handles;
        int m_current;

        public GCHandlePool()
        {
            m_handles = new GCHandle[128];
        }

        public GCHandle Alloc()
        {
            if (m_current > 0)
            {
                return m_handles[--m_current];
            }

            return GCHandle.Alloc(null);
        }

        public GCHandle Alloc(object o)
        {
            if (m_current > 0)
            {
                var handle = m_handles[--m_current];

                handle.Target = o;

                return handle;
            }

            return GCHandle.Alloc(o);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntPtr AllocHandleIfNotNull(object o)
        {
            if (o == null)
                return IntPtr.Zero;

            return (IntPtr)Alloc(o);
        }

        public void Free(GCHandle h)
        {
            if (m_current == m_handles.Length)
            {
                var newLength = m_handles.Length * 2;
                var newHandles = new GCHandle[newLength];
                Array.Copy(m_handles, newHandles, m_handles.Length);

                m_handles = newHandles;
            }

            h.Target = null;

            m_handles[m_current++] = h;
        }
    }
}
