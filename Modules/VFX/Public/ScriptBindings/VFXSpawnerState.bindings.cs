// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.VFX
{
    ///<summary>This enum describes the state of VFXSpawner.</summary>
    public enum VFXSpawnerLoopState
    {
        ///<summary>If the VFXSpawner is in this state then it has finished and is currently awaiting a Play invocation. This is the default and final state.</summary>
        Finished,
        ///<summary>If the VFXSpawner is in this state, then it is waiting before it starts a loop.</summary>
        DelayingBeforeLoop,
        ///<summary>If the VFXSpawner is in this state, then it is currently looping. When in this state, <see cref="VFX.VFXSpawnerState.playing" /> is true.</summary>
        Looping,
        ///<summary>If the VFXSpawner is in this state, then it is waiting after a loop has reached the end.</summary>
        DelayingAfterLoop
    }

    ///<summary>The spawn state of a Spawn system.</summary>
    ///<remarks>This class is useful for debugging a Visual Effect's spawner. For example, you can see if the effect is currently playing, the number of loops the spawner has processed, as well as the current <see cref="VFX.VFXSpawnerLoopState">state</see> of the spawner.
    ///
    ///                To access the state of a Visual Effect's Spawn system, either use <see cref="VFX.VisualEffect.GetSpawnSystemInfo" /> or, in a class that inherits from <see cref="VFX.VFXSpawnerCallbacks" />, override the <see cref="VFX.VFXSpawnerCallbacks.OnUpdate">OnUpdate</see> method.</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using UnityEngine;
    ///using UnityEngine.VFX;
    ///
    ///class ConstantRateEquivalent : VFXSpawnerCallbacks
    ///{
    ///    public class InputProperties
    ///    {
    ///        [Min(0), Tooltip("Sets the number of particles to spawn per second.")]
    ///        public float Rate = 10;
    ///    }
    ///
    ///    static private readonly int rateID = Shader.PropertyToID("Rate");
    ///
    ///    public sealed override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    ///    {
    ///    }
    ///
    ///    public sealed override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    ///    {
    ///        if (state.playing)
    ///        {
    ///            float currentRate = vfxValues.GetFloat(rateID);
    ///            state.spawnCount += currentRate * state.deltaTime;
    ///        }
    ///    }
    ///
    ///    public sealed override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    ///    {
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/VFX/Public/VFXSpawnerState.h")]
    public sealed class VFXSpawnerState : IDisposable
    {
        private IntPtr m_Ptr;
        private bool m_Owner;
        private VFXEventAttribute m_WrapEventAttribute;

        ///<exclude />
        public VFXSpawnerState() : this(Internal_Create(), true)
        {
        }

        internal VFXSpawnerState(IntPtr ptr, bool owner)
        {
            m_Ptr = ptr;
            m_Owner = owner;
        }

        extern static internal IntPtr Internal_Create();

        [RequiredByNativeCode]
        internal static VFXSpawnerState CreateSpawnerStateWrapper()
        {
            var spawnerState = new VFXSpawnerState(IntPtr.Zero, false);
            spawnerState.PrepareWrapper();
            return spawnerState;
        }

        void PrepareWrapper()
        {
            if (m_Owner)
                throw new Exception("VFXSpawnerState : SetWrapValue is reserved to CreateWrapper object");

            if (m_WrapEventAttribute != null)
                throw new Exception("VFXSpawnerState : Unexpected calling twice prepare wrapper");

            m_WrapEventAttribute = VFXEventAttribute.CreateEventAttributeWrapper();
        }

        [RequiredByNativeCode]
        internal void SetWrapValue(IntPtr ptrToSpawnerState, IntPtr ptrToEventAttribute)
        {
            if (m_Owner)
                throw new Exception("VFXSpawnerState : SetWrapValue is reserved to CreateWrapper object");

            if (m_WrapEventAttribute == null)
                throw new Exception("VFXSpawnerState : Missing PrepareWrapper");

            m_Ptr = ptrToSpawnerState;
            m_WrapEventAttribute.SetWrapValue(ptrToEventAttribute);
        }

        internal IntPtr GetPtr()
        {
            return m_Ptr;
        }

        private void Release()
        {
            if (m_Ptr != IntPtr.Zero && m_Owner)
            {
                Internal_Destroy(m_Ptr);
            }
            m_Ptr = IntPtr.Zero;
            m_WrapEventAttribute = null;
        }

#pragma warning disable UAC0045 // Frees the owned native state; Internal_Destroy is thread-safe and guarded by the owner flag.
        ~VFXSpawnerState()
        {
            Release();
        }
#pragma warning restore UAC0045

        ///<exclude />
        public void Dispose()
        {
            Release();
            GC.SuppressFinalize(this);
        }

        [NativeMethod(IsThreadSafe = true)]
        extern static private void Internal_Destroy(IntPtr ptr);

        ///<summary>The current playing state.</summary>
        public bool playing
        {
            get
            {
                return loopState == VFXSpawnerLoopState.Looping;
            }
            set
            {
                loopState = value ? VFXSpawnerLoopState.Looping : VFXSpawnerLoopState.Finished;
            }
        }
        ///<summary>This boolean indicates if a new loop has just started.</summary>
        ///<remarks>
        ///  <see cref="VFX.VFXSpawnerState.totalTime" /> should be equals to zero at this stage.</remarks>
        extern public bool newLoop { get; }
        ///<summary>The current state of VFXSpawnerState.</summary>
        extern public VFXSpawnerLoopState loopState { get; set; }
        ///<summary>The current Spawn count.</summary>
        ///<remarks>The Spawn count is relative to a unique frame. If this custom block is the first one in the stack, the Spawn count is 0.0f.
        ///
        ///The Spawn system accumulates the Spawn count. Remaining integer values are consumed by a particles system.
        ///
        ///For example, the internal constant Spawn rate is implemented this way:</remarks>
        ///<example nocheck="true">
        ///  <code><![CDATA[state.spawnCount += currentRate * state.deltaTime]]></code>
        ///</example>
        extern public float spawnCount { get; set; }
        ///<summary>The current delta time.</summary>
        ///<remarks>You can modify this value. This only affects following blocks.</remarks>
        extern public float deltaTime { get; set; }
        ///<summary>The accumulated delta time since the last Play event.</summary>
        extern public float totalTime { get; set; }
        ///<summary>The current delay time that the VFXSpawner waits for before it starts a loop.</summary>
        extern public float delayBeforeLoop { get; set; }
        ///<summary>The duration of the looping state.</summary>
        ///<remarks>A VFXSpawner considers a negative value as an infinite duration.</remarks>
        extern public float loopDuration { get; set; }
        ///<summary>The current delay time that the VFXSpawner waits for after it finishes a loop.</summary>
        extern public float delayAfterLoop { get; set; }
        ///<summary>The current index of loop.</summary>
        ///<remarks>This value should be less than <see cref="VFX.VFXSpawnerState.loopCount" />. Unity compares this value to <see cref="VFX.VFXSpawnerState.loopCount" /> at the current iteration to decide on the next state.</remarks>
        extern public int loopIndex { get; set; }
        ///<summary>The current loop count.</summary>
        ///<remarks>A VFXSpawner considers a negative value as an infinite count. The VFXSpawner resets this value whenever you invoke Play on the Spawn context.</remarks>
        extern public int loopCount { get; set; }

        extern internal VFXEventAttribute Internal_GetVFXEventAttribute();

        ///<summary>Gets the modifiable current event attribute (RO).</summary>
        public VFXEventAttribute vfxEventAttribute
        {
            get
            {
                if (!m_Owner && m_WrapEventAttribute != null)
                    return m_WrapEventAttribute;

                //Default fallback, it will allocate a new VFXEventAttribute
                return Internal_GetVFXEventAttribute();
            }
        }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(VFXSpawnerState vfxSpawnerState) => vfxSpawnerState.m_Ptr;
        }
    }
}
