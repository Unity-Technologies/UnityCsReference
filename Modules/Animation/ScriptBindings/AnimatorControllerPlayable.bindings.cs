// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;
using System.Runtime.InteropServices;

namespace UnityEngine.Animations
{
    ///<summary>An implementation of <see cref="IPlayable" /> that controls an animation <see cref="RuntimeAnimatorController" />.</summary>
    ///<remarks>NOTE: You can use <see cref="PlayableExtensions" /> methods with AnimatorControllerPlayable objects.</remarks>
    [NativeHeader("Modules/Animation/ScriptBindings/AnimatorControllerPlayable.bindings.h")]
    [NativeHeader("Modules/Animation/ScriptBindings/Animator.bindings.h")]
    [NativeHeader("Modules/Animation/Director/AnimatorControllerPlayable.h")]
    [NativeHeader("Modules/Animation/RuntimeAnimatorController.h")]
    [NativeHeader("Modules/Animation/AnimatorInfo.h")]
    [StaticAccessor("AnimatorControllerPlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public partial struct AnimatorControllerPlayable : IPlayable, IEquatable<AnimatorControllerPlayable>
    {
        PlayableHandle m_Handle;

        static readonly AnimatorControllerPlayable m_NullPlayable = new AnimatorControllerPlayable(PlayableHandle.Null);
        ///<summary>Returns an invalid AnimatorControllerPlayable.</summary>
        public static AnimatorControllerPlayable Null { get { return m_NullPlayable; } }

        ///<summary>Creates an <see cref="AnimatorControllerPlayable" /> in the <see cref="T:UnityEngine.Playables.PlayableGraph" />.</summary>
        ///<param name="graph">The <see cref="T:UnityEngine.Playables.PlayableGraph" /> object that will own the AnimatorControllerPlayable.</param>
        ///<param name="controller">The <see cref="RuntimeAnimatorController" /> that will be added in the graph.</param>
        ///<returns>A <see cref="AnimatorControllerPlayable" />.</returns>
        public static AnimatorControllerPlayable Create(PlayableGraph graph, RuntimeAnimatorController controller)
        {
            var handle = CreateHandle(graph, controller);
            return new AnimatorControllerPlayable(handle);
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph, RuntimeAnimatorController controller)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!CreateHandleInternal(graph, controller, ref handle))
                return PlayableHandle.Null;

            return handle;
        }

        internal AnimatorControllerPlayable(PlayableHandle handle)
        {
            m_Handle = PlayableHandle.Null;
            SetHandle(handle);
        }

        ///<exclude />
        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        ///<exclude />
        public void SetHandle(PlayableHandle handle)
        {
            if (m_Handle.IsValid())
                throw new InvalidOperationException("Cannot call IPlayable.SetHandle on an instance that already contains a valid handle.");

            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<AnimatorControllerPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AnimatorControllerPlayable.");
            }

            m_Handle = handle;
        }

        ///<exclude />
        public static implicit operator Playable(AnimatorControllerPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        ///<exclude />
        public static explicit operator AnimatorControllerPlayable(Playable playable)
        {
            return new AnimatorControllerPlayable(playable.GetHandle());
        }

        ///<exclude />
        public bool Equals(AnimatorControllerPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }

        // Gets the value of a float parameter
        ///<summary>Returns the value of the given float parameter.</summary>
        ///<remarks>If the float parameter you specify doesn't exist, the float returns as 0.</remarks>
        ///<param name="name">The parameter name.</param>
        ///<returns>The value of the parameter.</returns>
        public float GetFloat(string name)
        {
            return GetFloatString(ref m_Handle, name);
        }

        // Gets the value of a float parameter
        ///<summary>Returns the value of the given float parameter.</summary>
        ///<remarks>If the float parameter you specify doesn't exist, the float returns as 0.</remarks>
        ///<param name="id">The parameter ID.</param>
        ///<returns>The value of the parameter.</returns>
        public float GetFloat(int id)
        {
            return GetFloatID(ref m_Handle, id);
        }

        // Sets the value of a float parameter
        ///<summary>Send float values to the AnimatorController to affect transitions.</summary>
        ///<remarks>Use SetFloat in a script to send float values to the AnimatorController in order to activate transitions. In the AnimatorController, define what values affect how certain animations transition. This is useful in various situations, especially in animation cycles such as movement animations where you might require the character to walk or run depending on the button pressure applied.</remarks>
        ///<param name="name">The parameter name.</param>
        ///<param name="value">The new parameter value.</param>
        public void SetFloat(string name, float value)
        {
            SetFloatString(ref m_Handle, name, value);
        }

        ///<summary>Send float values to the AnimatorController to affect transitions.</summary>
        ///<remarks>Use SetFloat in a script to send float values to the AnimatorController in order to activate transitions. In the AnimatorController, define what values affect how certain animations transition. This is useful in various situations, especially in animation cycles such as movement animations where you might require the character to walk or run depending on the button pressure applied.</remarks>
        ///<param name="name">The parameter name.</param>
        ///<param name="value">The new parameter value.</param>
        ///<param name="dampTime">The damper total time.</param>
        ///<param name="deltaTime">The delta time to give to the damper.</param>
        public void SetFloat(string name, float value, float dampTime, float deltaTime)
        {
            SetFloatStringDamp(ref m_Handle, name, value, dampTime, deltaTime);
        }

        // Sets the value of a float parameter
        ///<summary>Send float values to the AnimatorController to affect transitions.</summary>
        ///<remarks>Use SetFloat in a script to send float values to the AnimatorController in order to activate transitions. In the AnimatorController, define what values affect how certain animations transition. This is useful in various situations, especially in animation cycles such as movement animations where you might require the character to walk or run depending on the button pressure applied.</remarks>
        ///<param name="id">The parameter ID.</param>
        ///<param name="value">The new parameter value.</param>
        public void SetFloat(int id, float value)
        {
            SetFloatID(ref m_Handle, id, value);
        }

        ///<summary>Send float values to the AnimatorController to affect transitions.</summary>
        ///<remarks>Use SetFloat in a script to send float values to the AnimatorController in order to activate transitions. In the AnimatorController, define what values affect how certain animations transition. This is useful in various situations, especially in animation cycles such as movement animations where you might require the character to walk or run depending on the button pressure applied.</remarks>
        ///<param name="id">The parameter ID.</param>
        ///<param name="value">The new parameter value.</param>
        ///<param name="dampTime">The damper total time.</param>
        ///<param name="deltaTime">The delta time to give to the damper.</param>
        public void SetFloat(int id, float value, float dampTime, float deltaTime)
        {
            SetFloatIDDamp(ref m_Handle, id, value, dampTime, deltaTime);
        }

        // Gets the value of a bool parameter
        ///<summary>Returns the value of the given boolean parameter.</summary>
        ///<remarks>Return the current state of a bool parameter within the Animator Controller. Use the parameter’s name or ID to search for the appropriate one.</remarks>
        ///<param name="name">The parameter name.</param>
        ///<returns>The value of the parameter.</returns>
        public bool GetBool(string name)
        {
            return GetBoolString(ref m_Handle, name);
        }

        // Gets the value of a bool parameter
        ///<summary>Returns the value of the given boolean parameter.</summary>
        ///<remarks>Return the current state of a bool parameter within the Animator Controller. Use the parameter’s name or ID to search for the appropriate one.</remarks>
        ///<param name="id">The parameter ID.</param>
        ///<returns>The value of the parameter.</returns>
        public bool GetBool(int id)
        {
            return GetBoolID(ref m_Handle, id);
        }

        // Sets the value of a bool parameter
        ///<summary>Sets the value of the given boolean parameter.</summary>
        ///<remarks>Use AnimatorControllerPlayable.SetBool to pass Boolean values to an [Animator Controller](xref:class-AnimatorController)
        ///via script.
        ///
        ///Use this to trigger transitions between states. For example, triggering a death animation by setting an “alive” boolean to false. See documentation on [Animation](xref:AnimatorControllerCreation) for more information on setting up Animators.
        ///
        ///Note: You can identify the parameter by name or by ID number, but the name or ID number must be the same as the parameter you want to change in the AnimatorController.</remarks>
        ///<param name="name">The parameter name.</param>
        ///<param name="value">The new parameter value.</param>
        public void SetBool(string name, bool value)
        {
            SetBoolString(ref m_Handle, name, value);
        }

        // Sets the value of a bool parameter
        ///<summary>Sets the value of the given boolean parameter.</summary>
        ///<remarks>Use AnimatorControllerPlayable.SetBool to pass Boolean values to an [Animator Controller](xref:class-AnimatorController)
        ///via script.
        ///
        ///Use this to trigger transitions between states. For example, triggering a death animation by setting an “alive” boolean to false. See documentation on [Animation](xref:AnimatorControllerCreation) for more information on setting up Animators.
        ///
        ///Note: You can identify the parameter by name or by ID number, but the name or ID number must be the same as the parameter you want to change in the AnimatorController.</remarks>
        ///<param name="id">The parameter ID.</param>
        ///<param name="value">The new parameter value.</param>
        public void SetBool(int id, bool value)
        {
            SetBoolID(ref m_Handle, id, value);
        }

        // Gets the value of an integer parameter
        ///<summary>Returns the value of the given integer parameter.</summary>
        ///<param name="name">The parameter name.</param>
        ///<returns>The value of the parameter.</returns>
        public int GetInteger(string name)
        {
            return GetIntegerString(ref m_Handle, name);
        }

        // Gets the value of an integer parameter
        ///<summary>Returns the value of the given integer parameter.</summary>
        ///<param name="id">The parameter ID.</param>
        ///<returns>The value of the parameter.</returns>
        public int GetInteger(int id)
        {
            return GetIntegerID(ref m_Handle, id);
        }

        // Sets the value of an integer parameter
        ///<summary>Sets the value of the given integer parameter.</summary>
        ///<remarks>Use this as a way to trigger transitions between Animator states. One way of using Integers instead of Floats or Booleans is to use it for something that has multiple states, for example directions (turn left, turn right etc.). Each direction could correspond to a number instead of having multiple Booleans that have to be reset each time.
        ///
        ///See documentation on [Animation](xref:Animator) for more information on setting up Animators.
        ///
        ///Note: You can identify the parameter by name or by ID number, but the name or ID number must be the same as the parameter you want to change in the AnimatorController.</remarks>
        ///<param name="name">The parameter name.</param>
        ///<param name="value">The new parameter value.</param>
        public void SetInteger(string name, int value)
        {
            SetIntegerString(ref m_Handle, name, value);
        }

        // Sets the value of an integer parameter
        ///<summary>Sets the value of the given integer parameter.</summary>
        ///<remarks>Use this as a way to trigger transitions between Animator states. One way of using Integers instead of Floats or Booleans is to use it for something that has multiple states, for example directions (turn left, turn right etc.). Each direction could correspond to a number instead of having multiple Booleans that have to be reset each time.
        ///
        ///See documentation on [Animation](xref:Animator) for more information on setting up Animators.
        ///
        ///Note: You can identify the parameter by name or by ID number, but the name or ID number must be the same as the parameter you want to change in the AnimatorController.</remarks>
        ///<param name="id">The parameter ID.</param>
        ///<param name="value">The new parameter value.</param>
        public void SetInteger(int id, int value)
        {
            SetIntegerID(ref m_Handle, id, value);
        }

        // Sets the trigger parameter on
        ///<summary>Sets the value of the given trigger parameter.</summary>
        ///<remarks>This method allows you to set (i.e. activate) an animation trigger, to cause a change in flow in the state machine of an animator controller. The [Animation Parameters](xref:AnimationParameters) page describes the purpose of the Animator Controller Parameters window.  <c>Trigger</c> is one of the 4 selectable options. Selecting this adds a <c>Trigger</c> to the list of chosen parameters.  Once this is added to the selected list it can be named.  Unlike <c>bool</c>s which have the same <c>true/false</c> option, <c>Trigger</c>s have a <c>true</c> option which automatically returns back to <c>false</c>.  A typical example might be to have a Jump option.  If this option is entered during run-time the character will jump.  At the end of the Jump the previous motion (perhaps a walk or run state) will be returned to.</remarks>
        ///<param name="name">The parameter name.</param>
        public void SetTrigger(string name)
        {
            SetTriggerString(ref m_Handle, name);
        }

        // Sets the trigger parameter at on
        ///<summary>Sets the value of the given trigger parameter.</summary>
        ///<remarks>This method allows you to set (i.e. activate) an animation trigger, to cause a change in flow in the state machine of an animator controller. The [Animation Parameters](xref:AnimationParameters) page describes the purpose of the Animator Controller Parameters window.  <c>Trigger</c> is one of the 4 selectable options. Selecting this adds a <c>Trigger</c> to the list of chosen parameters.  Once this is added to the selected list it can be named.  Unlike <c>bool</c>s which have the same <c>true/false</c> option, <c>Trigger</c>s have a <c>true</c> option which automatically returns back to <c>false</c>.  A typical example might be to have a Jump option.  If this option is entered during run-time the character will jump.  At the end of the Jump the previous motion (perhaps a walk or run state) will be returned to.</remarks>
        ///<param name="id">The parameter ID.</param>
        public void SetTrigger(int id)
        {
            SetTriggerID(ref m_Handle, id);
        }

        // Resets the trigger parameter at off
        ///<summary>Resets the value of the given trigger parameter.</summary>
        ///<remarks>Use this to reset a Trigger [parameter](xref:AnimationParameters) in an Animator Controller that could still be active. Make sure to create a parameter in the Animator Controller with the same name. See <see cref="Animator.SetTrigger" /> for more information about how to set a Trigger.</remarks>
        ///<param name="name">The parameter name.</param>
        public void ResetTrigger(string name)
        {
            ResetTriggerString(ref m_Handle, name);
        }

        // Resets the trigger parameter at off
        ///<summary>Resets the value of the given trigger parameter.</summary>
        ///<remarks>Use this to reset a Trigger [parameter](xref:AnimationParameters) in an Animator Controller that could still be active. Make sure to create a parameter in the Animator Controller with the same name. See <see cref="Animator.SetTrigger" /> for more information about how to set a Trigger.</remarks>
        ///<param name="id">The parameter ID.</param>
        public void ResetTrigger(int id)
        {
            ResetTriggerID(ref m_Handle, id);
        }

        // Returns true if a parameter is controlled by an additional curve on an animation
        ///<summary>Returns true if the parameter is controlled by a curve, false otherwise.</summary>
        ///<param name="name">The parameter name.</param>
        ///<returns>True if the parameter is controlled by a curve, false otherwise.</returns>
        public bool IsParameterControlledByCurve(string name)
        {
            return IsParameterControlledByCurveString(ref m_Handle, name);
        }

        // Returns true if a parameter is controlled by an additional curve on an animation
        ///<summary>Returns true if the parameter is controlled by a curve, false otherwise.</summary>
        ///<param name="id">The parameter ID.</param>
        ///<returns>True if the parameter is controlled by a curve, false otherwise.</returns>
        public bool IsParameterControlledByCurve(int id)
        {
            return IsParameterControlledByCurveID(ref m_Handle, id);
        }

        // The AnimatorController layer count
        ///<exclude />
        public int GetLayerCount()
        {
            return GetLayerCountInternal(ref m_Handle);
        }

        ///<summary>Returns the layer name.</summary>
        ///<param name="layerIndex">The layer index.</param>
        ///<returns>The layer name.</returns>
        public string GetLayerName(int layerIndex)
        {
            return GetLayerNameInternal(ref m_Handle, layerIndex);
        }

        ///<summary>Returns the index of the layer with the given name.</summary>
        ///<param name="layerName">The layer name.</param>
        ///<returns>The layer index.</returns>
        public int GetLayerIndex(string layerName)
        {
            return GetLayerIndexInternal(ref m_Handle, layerName);
        }

        ///<summary>Returns the weight of the layer at the specified index.</summary>
        ///<param name="layerIndex">The layer index.</param>
        ///<returns>The layer weight.</returns>
        public float GetLayerWeight(int layerIndex)
        {
            return GetLayerWeightInternal(ref m_Handle, layerIndex);
        }

        ///<summary>Sets the weight of the layer at the given index.</summary>
        ///<param name="layerIndex">The layer index.</param>
        ///<param name="weight">The new layer weight.</param>
        public void SetLayerWeight(int layerIndex, float weight)
        {
            SetLayerWeightInternal(ref m_Handle, layerIndex, weight);
        }

        ///<summary>Returns an <see cref="AnimatorStateInfo" /> with the information on the current state.</summary>
        ///<remarks>Fetches the data from the current state in the AnimatorController. Use this to get details from the state, including accessing the state’s speed, length, name and other variables. For gathering information from the clips that the states hold, see <see cref="AnimatorControllerPlayable.GetCurrentAnimatorClipInfo" />.</remarks>
        ///<param name="layerIndex">The layer index.</param>
        ///<returns>An <see cref="AnimatorStateInfo" /> with the information on the current state.</returns>
        public AnimatorStateInfo GetCurrentAnimatorStateInfo(int layerIndex)
        {
            return GetCurrentAnimatorStateInfoInternal(ref m_Handle, layerIndex);
        }

        ///<summary>Returns an <see cref="AnimatorStateInfo" /> with the information on the next state.</summary>
        ///<param name="layerIndex">The layer index.</param>
        ///<returns>An <see cref="AnimatorStateInfo" /> with the information on the next state.</returns>
        public AnimatorStateInfo GetNextAnimatorStateInfo(int layerIndex)
        {
            return GetNextAnimatorStateInfoInternal(ref m_Handle, layerIndex);
        }

        // Gets the Transition information on a specified AnimatorController layer
        ///<summary>Returns an <see cref="AnimatorTransitionInfo" /> with the informations on the current transition.</summary>
        ///<param name="layerIndex">The layer's index.</param>
        ///<returns>An <see cref="AnimatorTransitionInfo" /> with the informations on the current transition.</returns>
        public AnimatorTransitionInfo GetAnimatorTransitionInfo(int layerIndex)
        {
            return GetAnimatorTransitionInfoInternal(ref m_Handle, layerIndex);
        }

        ///<summary>Returns an array of all the <see cref="AnimatorClipInfo" /> in the current state of the given layer.</summary>
        ///<param name="layerIndex">The layer index.</param>
        ///<returns>An array of all the <see cref="AnimatorClipInfo" /> in the current state.</returns>
        public AnimatorClipInfo[] GetCurrentAnimatorClipInfo(int layerIndex)
        {
            return GetCurrentAnimatorClipInfoInternal(ref m_Handle, layerIndex);
        }

        // Gets the list of AnimatorClipInfo currently played by the current state
        public void GetCurrentAnimatorClipInfo(int layerIndex, List<AnimatorClipInfo> clips)
        {
            if (clips == null) throw new ArgumentNullException("clips");

            GetAnimatorClipInfoInternal(ref m_Handle, layerIndex, true, clips);
        }

        // Gets the list of AnimatorClipInfo currently played by the next state
        public void GetNextAnimatorClipInfo(int layerIndex, List<AnimatorClipInfo> clips)
        {
            if (clips == null) throw new ArgumentNullException("clips");

            GetAnimatorClipInfoInternal(ref m_Handle, layerIndex, false, clips);
        }

        [NativeMethod(ThrowsException = true)]
        extern private static void GetAnimatorClipInfoInternal(ref PlayableHandle handle, int layerIndex, bool isCurrent, [Out,NotNull] List<AnimatorClipInfo> clips);

        // Gets the number of AnimatorClipInfo currently played by the current state
        ///<summary>Returns the number of <see cref="AnimatorClipInfo" /> in the current state.</summary>
        ///<param name="layerIndex">The layer index.</param>
        ///<returns>The number of <see cref="AnimatorClipInfo" /> in the current state.</returns>
        public int GetCurrentAnimatorClipInfoCount(int layerIndex)
        {
            return GetAnimatorClipInfoCountInternal(ref m_Handle, layerIndex, true);
        }

        // Gets the number of AnimatorClipInfo currently played by the next state
        ///<summary>Returns the number of <see cref="AnimatorClipInfo" /> in the next state.</summary>
        ///<param name="layerIndex">The layer index.</param>
        ///<returns>The number of <see cref="AnimatorClipInfo" /> in the next state.</returns>
        public int GetNextAnimatorClipInfoCount(int layerIndex)
        {
            return GetAnimatorClipInfoCountInternal(ref m_Handle, layerIndex, false);
        }

        ///<summary>Returns an array of all the <see cref="AnimatorClipInfo" /> in the next state of the given layer.</summary>
        ///<param name="layerIndex">The layer index.</param>
        ///<returns>An array of all the <see cref="AnimatorClipInfo" /> in the next state.</returns>
        public AnimatorClipInfo[] GetNextAnimatorClipInfo(int layerIndex)
        {
            return GetNextAnimatorClipInfoInternal(ref m_Handle, layerIndex);
        }

        ///<summary>Returns true if there is a transition on the given layer, false otherwise.</summary>
        ///<param name="layerIndex">The layer index.</param>
        ///<returns>True if there is a transition on the given layer, false otherwise.</returns>
        public bool IsInTransition(int layerIndex)
        {
            return IsInTransitionInternal(ref m_Handle, layerIndex);
        }

        ///<exclude />
        public int GetParameterCount()
        {
            return GetParameterCountInternal(ref m_Handle);
        }

        ///<summary>See <see cref="P:UnityEditor.Animations.AnimatorController.parameters" />.</summary>
        public AnimatorControllerParameter GetParameter(int index)
        {
            var parameter = GetParameterInternal(ref m_Handle, index);
            if ((int)parameter.m_Type == AnimatorControllerParameterTypeConstants.InvalidType)
                throw new IndexOutOfRangeException("Invalid parameter index.");
            return parameter;
        }

        internal int GetParameterLayout(Span<int> hashes, Span<AnimatorControllerParameterType> types)
        {
            return GetParameterLayoutInternal(ref m_Handle, hashes, MemoryMarshal.Cast<AnimatorControllerParameterType, int>(types));
        }

        internal int GetParameterValues(Span<AnimatorParameterValue> values)
        {
            return GetParameterValuesInternal(ref m_Handle, MemoryMarshal.Cast<AnimatorParameterValue, int>(values));
        }

        /// <remarks>When the layout contains duplicate hashes, the order of <paramref name="hashes"/> determines which entry is written. Hashes in layout order address every entry.</remarks>
        internal void SetParameterValues(ReadOnlySpan<int> hashes, ReadOnlySpan<AnimatorParameterValue> values)
        {
            if (hashes.Length != values.Length)
                throw new ArgumentException("hashes and values must have the same length.");
            SetParameterValuesInternal(ref m_Handle, hashes, MemoryMarshal.Cast<AnimatorParameterValue, int>(values));
        }

        public void CrossFadeInFixedTime(string stateName, float transitionDuration)
        {
            CrossFadeInFixedTimeInternal(ref m_Handle, StringToHash(stateName), transitionDuration, -1, 0.0f);
        }

        public void CrossFadeInFixedTime(string stateName, float transitionDuration, int layer)
        {
            CrossFadeInFixedTimeInternal(ref m_Handle, StringToHash(stateName), transitionDuration, layer, 0.0f);
        }

        ///<summary>Creates a crossfade from the current state to any other state using times in seconds.</summary>
        ///<remarks>When you specify a state name, or the string used to generate a hash, it should include the name of the parent layer. For example, if you have a <c>Run</c> state in the <c>Base Layer</c>, the name is <c>Base Layer.Run</c>.</remarks>
        ///<param name="stateName">The name of the state.</param>
        ///<param name="transitionDuration">The duration of the transition (in seconds).</param>
        ///<param name="layer">The layer where the crossfade occurs.</param>
        ///<param name="fixedTime">The time of the state (in seconds).</param>
        ///<seealso cref="AnimatorControllerPlayable.CrossFade" />
        public void CrossFadeInFixedTime(string stateName, float transitionDuration, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("0.0f")] float fixedTime)
        {
            CrossFadeInFixedTimeInternal(ref m_Handle, StringToHash(stateName), transitionDuration, layer, fixedTime);
        }

        public void CrossFadeInFixedTime(int stateNameHash, float transitionDuration)
        {
            CrossFadeInFixedTimeInternal(ref m_Handle, stateNameHash, transitionDuration, -1, 0.0f);
        }

        public void CrossFadeInFixedTime(int stateNameHash, float transitionDuration, int layer)
        {
            CrossFadeInFixedTimeInternal(ref m_Handle, stateNameHash, transitionDuration, layer, 0.0f);
        }

        ///<summary>Creates a crossfade from the current state to any other state using times in seconds.</summary>
        ///<remarks>When you specify a state name, or the string used to generate a hash, it should include the name of the parent layer. For example, if you have a <c>Run</c> state in the <c>Base Layer</c>, the name is <c>Base Layer.Run</c>.</remarks>
        ///<param name="stateNameHash">The hash name of the state.</param>
        ///<param name="transitionDuration">The duration of the transition (in seconds).</param>
        ///<param name="layer">The layer where the crossfade occurs.</param>
        ///<param name="fixedTime">The time of the state (in seconds).</param>
        ///<seealso cref="AnimatorControllerPlayable.CrossFade" />
        public void CrossFadeInFixedTime(int stateNameHash, float transitionDuration, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("0.0f")] float fixedTime)
        {
            CrossFadeInFixedTimeInternal(ref m_Handle, stateNameHash, transitionDuration, layer, fixedTime);
        }

        public void CrossFade(string stateName, float transitionDuration)
        {
            CrossFadeInternal(ref m_Handle, StringToHash(stateName), transitionDuration, -1, float.NegativeInfinity);
        }

        public void CrossFade(string stateName, float transitionDuration, int layer)
        {
            CrossFadeInternal(ref m_Handle, StringToHash(stateName), transitionDuration, layer, float.NegativeInfinity);
        }

        ///<summary>Creates a crossfade from the current state to any other state using normalized times.</summary>
        ///<remarks>When you specify a state name, or the string used to generate a hash, it should include the name of the parent layer. For example, if you have a <c>Run</c> state in the <c>Base Layer</c>, the name is <c>Base Layer.Run</c>.</remarks>
        ///<param name="stateName">The name of the state.</param>
        ///<param name="transitionDuration">The duration of the transition (normalized).</param>
        ///<param name="layer">The layer where the crossfade occurs.</param>
        ///<param name="normalizedTime">The time of the state (normalized).</param>
        ///<seealso cref="AnimatorControllerPlayable.CrossFadeInFixedTime" />
        public void CrossFade(string stateName, float transitionDuration, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("float.NegativeInfinity")] float normalizedTime)
        {
            CrossFadeInternal(ref m_Handle, StringToHash(stateName), transitionDuration, layer, normalizedTime);
        }

        public void CrossFade(int stateNameHash, float transitionDuration)
        {
            CrossFadeInternal(ref m_Handle, stateNameHash, transitionDuration, -1, float.NegativeInfinity);
        }

        public void CrossFade(int stateNameHash, float transitionDuration, int layer)
        {
            CrossFadeInternal(ref m_Handle, stateNameHash, transitionDuration, layer, float.NegativeInfinity);
        }

        ///<summary>Creates a crossfade from the current state to any other state using normalized times.</summary>
        ///<remarks>When you specify a state name, or the string used to generate a hash, it should include the name of the parent layer. For example, if you have a <c>Run</c> state in the <c>Base Layer</c>, the name is <c>Base Layer.Run</c>.</remarks>
        ///<param name="stateNameHash">The hash name of the state.</param>
        ///<param name="transitionDuration">The duration of the transition (normalized).</param>
        ///<param name="layer">The layer where the crossfade occurs.</param>
        ///<param name="normalizedTime">The time of the state (normalized).</param>
        ///<seealso cref="AnimatorControllerPlayable.CrossFadeInFixedTime" />
        public void CrossFade(int stateNameHash, float transitionDuration, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("float.NegativeInfinity")] float normalizedTime)
        {
            CrossFadeInternal(ref m_Handle, stateNameHash, transitionDuration, layer, normalizedTime);
        }

        public void PlayInFixedTime(string stateName)
        {
            PlayInFixedTimeInternal(ref m_Handle, StringToHash(stateName), -1, float.NegativeInfinity);
        }

        public void PlayInFixedTime(string stateName, int layer)
        {
            PlayInFixedTimeInternal(ref m_Handle, StringToHash(stateName), layer, float.NegativeInfinity);
        }

        ///<summary>Plays a state.</summary>
        ///<remarks>When you specify a state name, or the string used to generate a hash, it should include the name of the parent layer. For example, if you have a <c>Run</c> state in the <c>Base Layer</c>, the name is <c>Base Layer.Run</c>.</remarks>
        ///<param name="stateName">The state name.</param>
        ///<param name="layer">The layer index. If layer is -1, it plays the first state with the given state name or hash.</param>
        ///<param name="fixedTime">The time offset (in seconds).</param>
        ///<seealso cref="Animator.StringToHash" />
        public void PlayInFixedTime(string stateName, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("float.NegativeInfinity")] float fixedTime)
        {
            PlayInFixedTimeInternal(ref m_Handle, StringToHash(stateName), layer, fixedTime);
        }

        public void PlayInFixedTime(int stateNameHash)
        {
            PlayInFixedTimeInternal(ref m_Handle, stateNameHash, -1, float.NegativeInfinity);
        }

        public void PlayInFixedTime(int stateNameHash, int layer)
        {
            PlayInFixedTimeInternal(ref m_Handle, stateNameHash, layer, float.NegativeInfinity);
        }

        ///<summary>Plays a state.</summary>
        ///<remarks>When you specify a state name, or the string used to generate a hash, it should include the name of the parent layer. For example, if you have a <c>Run</c> state in the <c>Base Layer</c>, the name is <c>Base Layer.Run</c>.</remarks>
        ///<param name="stateNameHash">The state hash name. If stateNameHash is 0, it changes the current state time.</param>
        ///<param name="layer">The layer index. If layer is -1, it plays the first state with the given state name or hash.</param>
        ///<param name="fixedTime">The time offset (in seconds).</param>
        ///<seealso cref="Animator.StringToHash" />
        public void PlayInFixedTime(int stateNameHash, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("float.NegativeInfinity")] float fixedTime)
        {
            PlayInFixedTimeInternal(ref m_Handle, stateNameHash, layer, fixedTime);
        }

        public void Play(string stateName)
        {
            PlayInternal(ref m_Handle, StringToHash(stateName), -1, float.NegativeInfinity);
        }

        public void Play(string stateName, int layer)
        {
            PlayInternal(ref m_Handle, StringToHash(stateName), layer, float.NegativeInfinity);
        }

        ///<summary>Plays a state.</summary>
        ///<remarks>When you specify a state name, or the string used to generate a hash, it should include the name of the parent layer. For example, if you have a <c>Run</c> state in the <c>Base Layer</c>, the name is <c>Base Layer.Run</c>.
        ///The <c>normalizedTime</c> parameter varies between 0 and 1.  If this parameter is left at zero then <see cref="Play" /> will operate as expected.  A different starting point can be given.  An example could be <c>normalizedTime</c> set to 0.5, which means the animation starts half way through.  If the transition from one state switches to another it may or may not be blended.  If the transition starts at 0.75 it will be blended with the other state.  If no transition is set up then <see cref="Play" /> will continue to 1.0 with no changes.</remarks>
        ///<param name="stateName">The state name.</param>
        ///<param name="layer">The layer index. If layer is -1, it plays the first state with the given state name or hash.</param>
        ///<param name="normalizedTime">The time offset between zero and one.</param>
        public void Play(string stateName, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("float.NegativeInfinity")] float normalizedTime)
        {
            PlayInternal(ref m_Handle, StringToHash(stateName), layer, normalizedTime);
        }

        public void Play(int stateNameHash)
        {
            PlayInternal(ref m_Handle, stateNameHash, -1, float.NegativeInfinity);
        }

        public void Play(int stateNameHash, int layer)
        {
            PlayInternal(ref m_Handle, stateNameHash, layer, float.NegativeInfinity);
        }

        ///<summary>Plays a state.</summary>
        ///<remarks>When you specify a state name, or the string used to generate a hash, it should include the name of the parent layer. For example, if you have a <c>Run</c> state in the <c>Base Layer</c>, the name is <c>Base Layer.Run</c>.
        ///The <c>normalizedTime</c> parameter varies between 0 and 1.  If this parameter is left at zero then <see cref="Play" /> will operate as expected.  A different starting point can be given.  An example could be <c>normalizedTime</c> set to 0.5, which means the animation starts half way through.  If the transition from one state switches to another it may or may not be blended.  If the transition starts at 0.75 it will be blended with the other state.  If no transition is set up then <see cref="Play" /> will continue to 1.0 with no changes.</remarks>
        ///<param name="stateNameHash">The state hash name. If stateNameHash is 0, it changes the current state time.</param>
        ///<param name="layer">The layer index. If layer is -1, it plays the first state with the given state name or hash.</param>
        ///<param name="normalizedTime">The time offset between zero and one.</param>
        public void Play(int stateNameHash, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("float.NegativeInfinity")] float normalizedTime)
        {
            PlayInternal(ref m_Handle, stateNameHash, layer, normalizedTime);
        }

        ///<summary>Resets the AnimatorController to its default state.</summary>
        ///<remarks>Use this method to reset the layers in the <see cref="T:UnityEditor.Animations.AnimatorController" /> to their default state.</remarks>
        ///<param name="resetParameters">Set to true to also reset the controller parameters to their default values. When set to false, only the controller state is reset.</param>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/AnimatorControllerPlayableResetExample.cs}]]></code>
        ///</example>
        public void ResetControllerState([UnityEngine.Internal.DefaultValue("true")] bool resetParameters = true)
        {
            ResetControllerStateInternal(ref m_Handle, resetParameters);
        }

        ///<summary>Returns true if the state exists in this layer, false otherwise.</summary>
        ///<param name="layerIndex">The layer index.</param>
        ///<param name="stateID">The state ID.</param>
        ///<returns>True if the state exists in this layer, false otherwise.</returns>
        public bool HasState(int layerIndex, int stateID)
        {
            return HasStateInternal(ref m_Handle, layerIndex, stateID);
        }

        internal string ResolveHash(int hash)
        {
            return ResolveHashInternal(ref m_Handle, hash);
        }

        [NativeMethod(ThrowsException = true)]
        extern private static bool CreateHandleInternal(PlayableGraph graph, RuntimeAnimatorController controller, ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static RuntimeAnimatorController GetAnimatorControllerInternal(ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static int GetLayerCountInternal(ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static string GetLayerNameInternal(ref PlayableHandle handle, int layerIndex);

        [NativeMethod(ThrowsException = true)]
        extern private static int GetLayerIndexInternal(ref PlayableHandle handle, string layerName);

        [NativeMethod(ThrowsException = true)]
        extern private static float GetLayerWeightInternal(ref PlayableHandle handle, int layerIndex);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetLayerWeightInternal(ref PlayableHandle handle,  int layerIndex, float weight);

        [NativeMethod(ThrowsException = true)]
        extern private static AnimatorStateInfo GetCurrentAnimatorStateInfoInternal(ref PlayableHandle handle, int layerIndex);

        [NativeMethod(ThrowsException = true)]
        extern private static AnimatorStateInfo GetNextAnimatorStateInfoInternal(ref PlayableHandle handle, int layerIndex);

        [NativeMethod(ThrowsException = true)]
        extern private static AnimatorTransitionInfo GetAnimatorTransitionInfoInternal(ref PlayableHandle handle, int layerIndex);

        [NativeMethod(ThrowsException = true)]
        extern private static AnimatorClipInfo[] GetCurrentAnimatorClipInfoInternal(ref PlayableHandle handle, int layerIndex);

        [NativeMethod(ThrowsException = true)]
        extern private static int GetAnimatorClipInfoCountInternal(ref PlayableHandle handle, int layerIndex, bool current);

        [NativeMethod(ThrowsException = true)]
        extern private static AnimatorClipInfo[] GetNextAnimatorClipInfoInternal(ref PlayableHandle handle, int layerIndex);

        [NativeMethod(ThrowsException = true)]
        extern private static string ResolveHashInternal(ref PlayableHandle handle, int hash);

        [NativeMethod(ThrowsException = true)]
        extern private static bool IsInTransitionInternal(ref PlayableHandle handle, int layerIndex);
        [NativeMethod(ThrowsException = true)]
        extern private static AnimatorControllerParameter GetParameterInternal(ref PlayableHandle handle, int index);

        [NativeMethod(ThrowsException = true)]
        extern private static int GetParameterCountInternal(ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static int GetParameterLayoutInternal(ref PlayableHandle handle, Span<int> hashes, Span<int> types);

        [NativeMethod(ThrowsException = true)]
        extern private static int GetParameterValuesInternal(ref PlayableHandle handle, Span<int> values);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetParameterValuesInternal(ref PlayableHandle handle, ReadOnlySpan<int> hashes, ReadOnlySpan<int> values);

        [NativeMethod(IsThreadSafe = true)]
        extern private static int StringToHash(string name);

        [NativeMethod(ThrowsException = true)]
        extern private static void CrossFadeInFixedTimeInternal(ref PlayableHandle handle, int stateNameHash, float transitionDuration, int layer, float fixedTime);


        [NativeMethod(ThrowsException = true)]
        extern private static void CrossFadeInternal(ref PlayableHandle handle, int stateNameHash, float transitionDuration, int layer, float normalizedTime);

        [NativeMethod(ThrowsException = true)]
        extern private static void PlayInFixedTimeInternal(ref PlayableHandle handle, int stateNameHash, int layer, float fixedTime);

        [NativeMethod(ThrowsException = true)]
        extern private static void PlayInternal(ref PlayableHandle handle, int stateNameHash, int layer, float normalizedTime);

        [NativeMethod(ThrowsException = true)]
        extern private static void ResetControllerStateInternal(ref PlayableHandle handle, bool resetParameters);

        [NativeMethod(ThrowsException = true)]
        extern private static bool HasStateInternal(ref PlayableHandle handle, int layerIndex, int stateID);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetFloatString(ref PlayableHandle handle, string name, float value);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetFloatID(ref PlayableHandle handle, int id, float value);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetFloatStringDamp(ref PlayableHandle handle, string name, float value, float dampTime, float deltaTime);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetFloatIDDamp(ref PlayableHandle handle, int id, float value, float dampTime, float deltaTime);

        [NativeMethod(ThrowsException = true)]
        extern private static float GetFloatString(ref PlayableHandle handle, string name);

        [NativeMethod(ThrowsException = true)]
        extern private static float GetFloatID(ref PlayableHandle handle, int id);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetBoolString(ref PlayableHandle handle, string name, bool value);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetBoolID(ref PlayableHandle handle, int id, bool value);

        [NativeMethod(ThrowsException = true)]
        extern private static bool GetBoolString(ref PlayableHandle handle, string name);

        [NativeMethod(ThrowsException = true)]
        extern private static bool GetBoolID(ref PlayableHandle handle, int id);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetIntegerString(ref PlayableHandle handle, string name, int value);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetIntegerID(ref PlayableHandle handle, int id, int value);

        [NativeMethod(ThrowsException = true)]
        extern private static int GetIntegerString(ref PlayableHandle handle, string name);

        [NativeMethod(ThrowsException = true)]
        extern private static int GetIntegerID(ref PlayableHandle handle, int id);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetTriggerString(ref PlayableHandle handle, string name);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetTriggerID(ref PlayableHandle handle, int id);

        [NativeMethod(ThrowsException = true)]
        extern private static void ResetTriggerString(ref PlayableHandle handle, string name);

        [NativeMethod(ThrowsException = true)]
        extern private static void ResetTriggerID(ref PlayableHandle handle, int id);

        [NativeMethod(ThrowsException = true)]
        extern private static bool IsParameterControlledByCurveString(ref PlayableHandle handle, string name);

        [NativeMethod(ThrowsException = true)]
        extern private static bool IsParameterControlledByCurveID(ref PlayableHandle handle, int id);
    }
}
