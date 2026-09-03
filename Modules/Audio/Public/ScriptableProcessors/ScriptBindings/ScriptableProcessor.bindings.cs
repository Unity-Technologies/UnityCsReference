// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Unity.Audio;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Audio
{
    /// <summary>
    /// A compact 32-bit handle for referencing DualThreadManager processors.
    /// </summary>
    /// <remarks>
    /// Bit layout:
    /// - [15:0]  Index   (16 bits) - processor array index
    /// - [27:16] Version (12 bits) - stale handle detection
    /// - [31:28] DTM ID  (4 bits)  - debug check for correct DTM
    ///
    /// This handle fits in a void* pointer, enabling storage in contexts like FMOD userdata.
    /// Unlike Unity.Audio.Handle, validation requires calling into the DualThreadManager.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/Audio/Public/DualThreadManager.h")]
    internal struct DualThreadHandle : IEquatable<DualThreadHandle>
    {
        internal uint Bits;

        /// <summary>
        /// Returns true if this handle was created (not the default/null sentinel).
        /// Does NOT validate that the referenced processor still exists.
        /// </summary>
        public readonly bool WasCreated => Bits != 0;

        public readonly ushort GetIndex() => (ushort)Bits;
        public readonly ushort GetVersion() => (ushort)((Bits >> 16) & 0xFFF);
        public readonly byte GetDTMId() => (byte)(Bits >> 28);

        public readonly bool Equals(DualThreadHandle other) => Bits == other.Bits;
        public readonly override bool Equals(object obj) => obj is DualThreadHandle other && Equals(other);
        public readonly override int GetHashCode() => (int)Bits;

        public static bool operator ==(DualThreadHandle left, DualThreadHandle right) => left.Bits == right.Bits;
        public static bool operator !=(DualThreadHandle left, DualThreadHandle right) => left.Bits != right.Bits;
    }

    /// <summary>
    /// <see cref="ProcessorInstance"/> is a handle to the common functionality of a scriptable processor.
    /// </summary>
    /// <remarks>
    /// This could be a <see cref="GeneratorInstance"/> etc., but with limited structural API available.
    /// Use this together with <see cref="ControlContext"/> to query and send commands to the processor.
    /// </remarks>
    public readonly partial struct ProcessorInstance : IEquatable<ProcessorInstance>
    {
        /// <undoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public unsafe interface IContext // Internal tag interface for dispatching implementations.
        {
            /// <undoc/>
            internal AvailableData GetAvailableData(DualThreadHandle handle);

            /// <undoc/>
            internal bool SendData(DualThreadHandle handle, void* data, int size, int align, long typehash);
        }

        /// <summary>
        /// Settings controlling how a <see cref="ProcessorInstance"/> is updated over the course of its lifetime.
        /// </summary>
        /// <seealso cref="ProcessorInstance.CreationParameters"/>
        /// <seealso cref="ProcessorInstance.IControl{TRealtime}.Update"/>
        /// <seealso cref="ProcessorInstance.IRealtime.Update"/>
        public enum UpdateSetting
        {
            /// <summary>
            /// The default update setting for a <see cref="ProcessorInstance"/>.
            /// </summary>
            /// <remarks>
            /// This is equivalent to <see cref="UpdateIfDataIsAvailable"/>.
            /// A default-inititalized value of <see cref="UpdateSetting"/> will correspond to this as well.
            /// </remarks>
            Default = 0,

            /// <summary>
            /// Never invoke <see cref="ProcessorInstance.IControl{TRealtime}.Update"/> on this processor nor <see cref="ProcessorInstance.IRealtime.Update"/>.
            /// </summary>
            NeverUpdate = 1,

            /// <summary>
            /// Invoke <see cref="ProcessorInstance.IControl{TRealtime}.Update"/> or <see cref="ProcessorInstance.IRealtime.Update"/>
            /// only if data has been sent or returned from <see cref="ProcessorInstance.Pipe.SendData"/> since the last update.
            /// </summary>
            UpdateIfDataIsAvailable = 2,
            /// <summary>
            /// Always invoke <see cref="ProcessorInstance.IControl{TRealtime}.Update"/> or <see cref="ProcessorInstance.IRealtime.Update"/>
            /// on this <see cref="ProcessorInstance"/> on every update.
            /// </summary>
            UpdateAlways = 3,
        }

        /// <summary>
        /// Additional data and parameters specifying how a <see cref="ProcessorInstance"/> should be created.
        /// </summary>
        /// <remarks>
        /// These are generally suggested setup from whomever is creating the <see cref="ProcessorInstance"/>, such as a <see cref="IAudioGenerator"/>.
        /// You can change properties to suit your particular needs.
        /// </remarks>
        [Obsolete("Use the per-processor CreationParameters types (GeneratorInstance.CreationParameters, RootOutputInstance.CreationParameters) instead; this shared type will be removed.")]
        public partial struct CreationParameters
        {
            /// <summary>
            /// Control under what circumstances <see cref="ProcessorInstance.IControl{TRealtime}.Update"/> will be called.
            /// </summary>
            public UpdateSetting controlUpdateSetting { get; set; }

            /// <summary>
            /// Control under what circumstances <see cref="ProcessorInstance.IRealtime.Update"/> will be called.
            /// </summary>
            public UpdateSetting realtimeUpdateSetting { get; set; }

            internal readonly InitializationFlags BuildInitializationFlags()
            {
                InitializationFlags flags = 0;

                if (controlUpdateSetting == UpdateSetting.UpdateIfDataIsAvailable)
                    flags |= InitializationFlags.UpdateControlIfDataIsAvailable;
                else if (controlUpdateSetting == UpdateSetting.UpdateAlways)
                    flags |= InitializationFlags.UpdateControlAlways;

                if (realtimeUpdateSetting == UpdateSetting.UpdateIfDataIsAvailable)
                    flags |= InitializationFlags.UpdateProcessorIfDataIsAvailable;
                else if (realtimeUpdateSetting == UpdateSetting.UpdateAlways)
                    flags |= InitializationFlags.UpdateProcessorAlways;

                return flags;
            }
        }

        /// <summary>
        /// Internal representation of flags controlling how a <see cref="ProcessorInstance"/> is handled over the course of its lifetime.
        /// </summary>
        /// <seealso cref="ProcessorInstance.UpdateSetting"/>
        [System.Flags]
        internal enum InitializationFlags : UInt32
        {
            /// <summary>
            /// Invoke <see cref="ProcessorInstance.IControl{TRealtime}.Update"/> only if data has been returned from
            /// <see cref="ProcessorInstance.Pipe.SendData"/> since the last update.
            /// </summary>
            UpdateControlIfDataIsAvailable = 1 << 1,
            /// <summary>
            /// Always invoke <see cref="ProcessorInstance.IControl{TRealtime}.Update"/> on this realtime on every update.
            /// </summary>
            UpdateControlAlways = 1 << 2,


            /// <summary>
            /// Invoke <see cref="ProcessorInstance.IRealtime.Update"/> only if data has been sent from
            /// <see cref="ProcessorInstance.Pipe.SendData"/> or <c>ControlContext.SendData</c> since the last update.
            /// </summary>
            UpdateProcessorIfDataIsAvailable = 1 << 3,
            /// <summary>
            /// Always invoke <see cref="ProcessorInstance.IRealtime.Update"/> on this realtime on every update.
            /// </summary>
            UpdateProcessorAlways = 1 << 4,
        }

        /// <summary>
        /// A context giving access to data that is currently available for the <see cref="ProcessorInstance"/> this was passed to.
        /// </summary>
        /// <seealso cref="ProcessorInstance.IRealtime"/>
        /// <seealso cref="ProcessorInstance.AvailableData"/>
        /// <seealso cref="ProcessorInstance.Pipe"/>
        public unsafe struct UpdatedDataContext : IContext
        {
            internal readonly RealtimeAccess Access;

            /// <undoc/>
            AvailableData IContext.GetAvailableData(DualThreadHandle handle)
                // Empty implementation: This is currently only used in a callback where <see cref="Pipe.Head"/> is already set.
                => default;

            /// <undoc/>
            bool IContext.SendData(DualThreadHandle handle, void* data, int size, int align, long typehash)
            {
                ScriptableProcessorBindings.ReturnDataFromProcessor(Access, handle, data, size, align, typehash);
                return true;
            }

            internal UpdatedDataContext(in RealtimeAccess access) => Access = access;
        }

        /// <summary>
        /// Base interface for the common processing logic of a <see cref="ProcessorInstance"/>.
        /// </summary>
        /// <remarks>
        /// All <see cref="ProcessorInstance"/>s must implement this interface to be fully formed,
        /// though usually each specific <see cref="ProcessorInstance"/> implements a more specific interface inheriting from this one.
        /// </remarks>
        /// <seealso cref="GeneratorInstance.IRealtime"/>"/>
        /// <seealso cref="ProcessorInstance.IControl{TRealtime}"/>"/>
        public interface IRealtime
        {
            /// <summary>
            /// Implement this function to react to data sent to the <see cref="ProcessorInstance"/> or communicate something back.
            /// </summary>
            /// <remarks>
            /// By default, this is called when there's data sent to your <see cref="ProcessorInstance"/>.
            /// This can be changed by setting flagsCreationParametersationParameters.controlUpdateSetting"/>
            /// when initially creating the <see cref="ProcessorInstance"/>.
            /// </remarks>
            /// <param name="context">The context giving access to available data.</param>
            /// <param name="pipe">Cross-thread communications pipe.</param>
            /// <seealso cref="ProcessorInstance.IControl{TRealtime}.Update"/>
            public void Update(UpdatedDataContext context, Pipe pipe);

            /// <summary>
            /// Called immediately from <see cref="RealtimeContext.SendMessage"/> when a message was sent to this
            /// <see cref="ProcessorInstance"/> on the real-time side.
            /// </summary>
            /// <remarks>
            /// This is the real-time counterpart of <see cref="ProcessorInstance.IControl{TRealtime}.OnMessage"/>.
            /// It runs synchronously on the real-time thread, within the originating <see cref="RealtimeContext"/>.
            /// <para/>
            /// This has a default implementation returning <see cref="Response.Unhandled"/>, so existing
            /// <see cref="ProcessorInstance.IRealtime"/> implementations remain source-compatible; override it to
            /// handle real-time messages. Since overriding default methods isn't positively constrained by the compiler, you may find it helpful to implement such interface methods <a href="https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/interfaces/explicit-interface-implementation">explicitly</a>.
            /// </remarks>
            /// <returns>
            /// <see cref="Response.Handled"/> if this <see cref="ProcessorInstance"/> acknowledged and processed the message,
            /// <see cref="Response.Unhandled"/> if not or ignored.
            /// </returns>
            /// <param name="context">
            /// A <see cref="RealtimeMessageContext"/> giving cross-thread data communication via
            /// <paramref name="pipe"/> (but no nested processing).
            /// </param>
            /// <param name="pipe">Cross-thread communications pipe.</param>
            /// <param name="message">
            /// The message someone sent to you through <see cref="RealtimeContext.SendMessage"/>.
            /// The contents are sent by reference, so you can modify them and the sender will see the changes.
            /// </param>
            /// <seealso cref="RealtimeContext.SendMessage"/>
            // Default interface method on purpose: keeps this interface source-compatible for existing
            // implementors, and Burst handles the DIM call in the generic realtime dispatch via
            // monomorphization (no boxing). Keep it a DIM rather than a required member.
            public Response OnMessage(RealtimeMessageContext context, Pipe pipe, Message message) => Response.Unhandled;
        }

        /// <summary>
        /// Base interface for common functionality of controlling a <see cref="ProcessorInstance"/>.
        /// </summary>
        /// <remarks>
        /// Here you can implement any control logic that is required for the realtime,
        /// that will run outside of the real-time thread.
        ///
        /// <para/>
        ///
        /// All <see cref="ProcessorInstance"/>s must implement this interface to be fully formed,
        /// though usually each specific <see cref="ProcessorInstance"/> implements a more specific interface inheriting from this one.
        /// </remarks>
        /// <seealso cref="GeneratorInstance.IControl{TRealtime}"/>
        public interface IControl<TRealtime>
            where TRealtime : unmanaged, IRealtime
        {
            /// <summary>
            /// Called when the generator is destroyed, after having left the processing thread.
            /// </summary>
            /// <remarks>
            /// Here you can clean up any resources that were allocated for the control or realtime thread.
            /// It may take an indeterminate amount of time between <see cref="ControlContext.Destroy"/> is called and this
            /// will be called, and there may be any number of <see cref="Update"/> calls inbetween.
            /// This is because destroying realtimes is an asynchronous process.
            /// </remarks>
            /// <seealso cref="RootOutputInstance.IRealtime.RemovedFromProcessing"/>
            /// <seealso cref="ControlContext.Destroy"/>
            public void Dispose(ControlContext context, ref TRealtime realtime);

            /// <summary>
            /// Called if you have subscribed to continuous updates from the control thread, or if there is
            /// data returned from the <typeparamref name="TRealtime"/> counterpart.
            /// </summary>
            /// <remarks>
            /// This is guaranteed to be invoked before <see cref="Dispose"/> if there's any data that's been sent
            /// from <see cref="Pipe.SendData"/> and suitable flags have been set in
            /// <see cref="ProcessorInstance.CreationParameters.controlUpdateSetting"/>.
            /// </remarks>
            /// <seealso cref="ControlContext.Manual.Update"/>
            /// <seealso cref="ProcessorInstance.UpdateSetting"/>
            public void Update(ControlContext context, Pipe pipe);

            /// <summary>
            /// Called immediately from <see cref="ControlContext.SendMessage"/> when a message was sent to this <see cref="ProcessorInstance"/>.
            /// </summary>
            /// <returns>
            /// <see cref="Response.Handled"/> if this <see cref="ProcessorInstance"/> acknowledged and processed the message,
            /// <see cref="Response.Unhandled"/> if not or ignored.
            /// </returns>
            /// <param name="context">Context the processor is in.</param>
            /// <param name="pipe">Cross-thread communications pipe.</param>
            /// <param name="message">
            /// The message someone sent to you through <see cref="ControlContext.SendMessage"/>.
            /// The contents are sent by reference, so you can modify them and the sender will see the changes.
            /// </param>
            public Response OnMessage(ControlContext context, Pipe pipe, Message message);
        }

        /// <summary>
        /// A bi-directional communication system typically between two logical threads.
        /// </summary>
        /// <remarks>
        /// One thread is referred to as "control" (main thread typically) and one as "realtime" (audio thread typically).
        /// Using <see cref="ControlContext"/> and <see cref="RealtimeContext"/> as keys to the APIs,
        /// you can read data the other thread sent you or send data back.
        /// </remarks>
        public unsafe ref struct Pipe
        {
            internal readonly AvailableData.Element* Head;
            internal readonly DualThreadHandle DualThreadHandle;

            /// <summary>
            /// Access an enumerator to the currently available data.
            /// </summary>
            /// <param name="context">The context key which provides data access from the respective thread.</param>
            /// <returns>A temporary collection of available data.</returns>
            /// <seealso cref="ProcessorInstance.AvailableData"/>
            public readonly AvailableData GetAvailableData<TAudioContext>(TAudioContext context)
                where TAudioContext : unmanaged, IContext
            {
                if (!DualThreadHandle.WasCreated)
                    throw new InvalidOperationException("DualThreadHandle is not valid, cannot get available data.");

                return Head != null ? new(Head) : context.GetAvailableData(DualThreadHandle);
            }

            /// <summary>
            /// Send data from <see cref="IRealtime"/> to <see cref="IControl{TRealtime}"/> or vice versa.
            /// </summary>
            /// <param name="context">The context key which targets the other logical thread receiver.</param>
            /// <param name="data">The data to be received in the other logical thread.</param>
            /// <returns>
            /// True if this operation is currently possible. This can fail for instance when the <see cref="ProcessorInstance"/>
            /// is being in process of being disposed through <see cref="ControlContext.DestroyProcessor"/>,
            /// so you must guard against this if you are transferring resources.
            /// </returns>
            public readonly bool SendData<TAudioContext, T>(TAudioContext context, in T data)
                where TAudioContext : unmanaged, IContext
                where T : unmanaged
            {
                // The context will do validation.

                fixed (T* pData = &data)
                {
                    return context.SendData(
                        DualThreadHandle,
                        pData,
                        sizeof(T),
                        UnsafeUtility.AlignOf<T>(),
                        BurstRuntime.GetHashCode64<T>()
                    );
                }
            }

            internal Pipe(DualThreadHandle dualThreadHandle, AvailableData.Element* head = null)
            {
                Head = head;
                DualThreadHandle = dualThreadHandle;
            }
        }

        /// <summary>
        /// A by-reference wrapper around something sent from <see cref="ControlContext.SendMessage"/>.
        /// </summary>
        /// <remarks>
        /// Since this has no defined type, you will need to test with <see cref="Message.Is"/> to discern between different
        /// message types being delivered to you.
        /// </remarks>
        public unsafe ref struct Message
        {
            /// <summary>
            /// Test whether this <see cref="Message"/> is of <typeparamref name="T"/>.
            /// </summary>
            public readonly bool Is<T>()
            {
                return TypeHash == BurstRuntime.GetHashCode64<T>();
            }

            /// <summary>
            /// Return a reference to the inner piece of data. You can modify this and the effects will be visible to the
            /// original message sender.
            /// </summary>
            /// <remarks>
            /// Use <see cref="ProcessorInstance.Message.Is"/> to figure out if you can call this function with a particular type.
            /// </remarks>
            /// <exception cref="InvalidCastException">
            /// Thrown if the inner piece of data doesn't match the type of <typeparamref name="T"/>.
            /// </exception>
            public readonly ref T Get<T>()
                where T : unmanaged
            {
                if (!Is<T>())
                    throw new InvalidCastException($"Message does not contain data of type {typeof(T)}");

                return ref *(T*)Data;
            }

            internal long TypeHash;
            internal void* Data;
            internal IntPtr ManagedHandle;
        }

        /// <summary>
        /// A return value from <see cref="ProcessorInstance.IControl{TRealtime}.OnMessage"/>
        /// that lets the message sender know if the message was handled or not.
        /// </summary>
        public enum Response
        {
            /// <summary>
            /// Indicates the message wasn't handled.
            /// </summary>
            Unhandled,
            /// <summary>
            /// Indicates the message was recognized and handled.
            /// </summary>
            Handled
        }

#pragma warning disable 0169, 0649
        /// <summary>
        /// A temporary collection of data available for enumeration.
        /// </summary>
        /// <remarks>
        /// You can enumerate the <see cref="Element"/>s within using a __foreach__ loop over this collection.
        /// </remarks>
        /// <seealso cref="Pipe.SendData"/>
        /// <example>
        /// <code source="../../../../../Tests/EditModeAndPlayModeTests/Audio/Assets/DocCodeExamples/SAP_HowToUseGetAvailableData.cs"/>
        /// </example>
        public unsafe ref struct AvailableData
        {
            /// <summary>
            /// A piece of temporary immutable type-erased data that was sent from <see cref="ProcessorInstance.Pipe.SendData"/>, received on likely another thread.
            /// </summary>
            /// <remarks>
            /// You can use <see cref="ProcessorInstance.AvailableData.Element.TryGetData"/> to test and extract a piece of typed data,
            /// if the current element matches it.
            /// </remarks>
            public unsafe ref struct Element
            {
                /// <summary>
                /// Test and return a piece of typed data if this <see cref="ProcessorInstance.AvailableData.Element"/> matches the type given.
                /// </summary>
                /// <param name="data">The typed piece of data to be extracted.</param>
                /// <returns>
                /// Whether this <see cref="ProcessorInstance.AvailableData.Element"/> is of the same type as <typeparamref name="T"/>, and if so,
                /// whether the <paramref name="data"/> parameter was updated.
                /// </returns>
                /// <seealso cref="ProcessorInstance.AvailableData"/>
                public bool TryGetData<T>(out T data) where T : unmanaged
                {
                    var attemptedTypeHash = BurstRuntime.GetHashCode64<T>();

                    if (attemptedTypeHash == TypeHash)
                    {
                        data = *(T*)m_Data;
                        return true;
                    }

                    data = default;
                    return false;
                }

                internal readonly Element* Next() { return m_NextElement; }

                internal long TypeHash;
                void* m_Data;
                int m_Size;
                int m_Align;
                DualThreadHandle m_Handle;
                Element* m_NextElement;
            }

            /// <undoc/>
            public Element Current => *m_CurrentElement;

            /// <undoc/>
            public AvailableData GetEnumerator()
            {
                return this;
            }

            /// <undoc/>
            public bool MoveNext()
            {
                if (m_MoveNextCalled)
                {
                    if (m_CurrentElement == null)
                        return false;

                    m_CurrentElement = m_CurrentElement->Next();
                }
                else
                {
                    m_MoveNextCalled = true;
                }

                return m_CurrentElement != null;
            }

            internal AvailableData(Element* element)
            {
                m_CurrentElement = element;
                m_MoveNextCalled = false;
            }

            Element* m_CurrentElement;
            bool m_MoveNextCalled;
        }

        internal readonly DualThreadHandle Handle;

        /// <summary>
        /// Checks if this instance equals another.
        /// </summary>
        /// <param name="other">The other instance for comparing.</param>
        /// <returns>True if the given instance is equal to this, otherwise, false.</returns>
        public bool Equals(ProcessorInstance other)
        {
            return Handle == other.Handle;
        }

        /// <summary>
        /// Checks if this instance equals a given object.
        /// </summary>
        /// <param name="obj">The object for comparing.</param>
        /// <returns>True if the given object is equal to this instance, otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            return obj is ProcessorInstance instance && Equals(instance);
        }

        /// <summary>
        /// Checks if two instances are equal.
        /// </summary>
        /// <param name="a">The first instance for comparing.</param>
        /// <param name="b">The second instance for comparing.</param>
        /// <returns>True if the two given instances are equal, otherwise, false.</returns>
        public static bool operator ==(ProcessorInstance a, ProcessorInstance b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Checks if two instances are not equal.
        /// </summary>
        /// <param name="a">The first instance for comparing.</param>
        /// <param name="b">The second instance for comparing.</param>
        /// <returns>True if the two given instances are not equal, otherwise, false.</returns>
        public static bool operator !=(ProcessorInstance a, ProcessorInstance b)
        {
            return !a.Equals(b);
        }

        /// <summary>
        /// Retrieves a hash code based on this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return Handle.GetHashCode();
        }

        internal ProcessorInstance(DualThreadHandle handle)
        {
            Handle = handle;
        }
    }

    /// <summary>
    /// A built-in message asking a processor to seek. Send it through <see cref="ControlContext.SendMessage{T}(ProcessorInstance, ref T)"/>
    /// to a generator instance, for example an <c>AudioClip</c> instantiated as a generator (backed by a SampleProvider),
    /// including within nested generator graphs.
    /// </summary>
    public struct SeekMessage
    {
        // Native can't compute the managed type hash, so make it known here. This covers seeks originating
        // in managed: constructing one runs this first, so native recognizes the hash we send with it.
        // Seeks originating in native are covered by BuiltInMessageTypeHashes, which native calls before
        // its first send. Both store the same value, so it doesn't matter which runs first.
        static SeekMessage()
        {
            ScriptableProcessorBindings.RegisterSeekMessageTypeHash(BurstRuntime.GetHashCode64<SeekMessage>());
        }

        // Layout must match the native audio::SeekMessage. A negative m_When encodes an immediate seek,
        // which `when` surfaces as null so the sentinel never reaches users.
        Unity.IntegerTime.DiscreteTime m_Destination;
        Unity.IntegerTime.DiscreteTime m_When;

        /// <summary>The clip position to seek to.</summary>
        public Unity.IntegerTime.DiscreteTime destination => m_Destination;

        /// <summary>
        /// The clip position at which the seek fires: it is applied when playback reaches this position
        /// (which itself jumps on each seek). Null means immediate, so the seek applies at the next block.
        /// Seeks may be scheduled in any order and are evaluated in send order. A seek whose position
        /// playback has already passed, including one the cursor jumped past via an earlier seek, is dropped.
        /// </summary>
        public Unity.IntegerTime.DiscreteTime? when => m_When.Value >= 0 ? m_When : null;

        /// <summary>Creates a seek request. Omit <paramref name="when"/> (or pass null) for an immediate seek.</summary>
        /// <param name="destination">The clip position to seek to.</param>
        /// <param name="when">The clip position at which the seek fires, or null to seek immediately.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="destination"/> or <paramref name="when"/> is negative.
        /// </exception>
        public SeekMessage(Unity.IntegerTime.DiscreteTime destination, Unity.IntegerTime.DiscreteTime? when = null)
        {
            if (destination.Value < 0)
                throw new ArgumentOutOfRangeException(nameof(destination), "The seek destination cannot be negative.");

            // when.Value is the DiscreteTime; its Value is the raw tick count.
            if (when.HasValue && when.Value.Value < 0)
                throw new ArgumentOutOfRangeException(nameof(when), "The seek time cannot be negative. Omit it to seek immediately.");

            m_Destination = destination;
            m_When = when ?? Unity.IntegerTime.DiscreteTime.FromTicks(-1);
        }
    }

    [NativeHeader("Modules/Audio/Public/ScriptableProcessors/ScriptBindings/ScriptableProcessor.bindings.h")]
    internal static class BuiltInMessageTypeHashes
    {
        // Native can build and send built-in messages on its own (through GeneratorHandle::SendSeekMessage)
        // without managed ever constructing a SeekMessage, so a type's static constructor is too late to
        // register its hash; native calls this before its first such send instead. No native sender exists
        // today - AudioSource::SetSamplePosition no longer seeks generator instances - but the registration
        // keeps that entry point safe. Idempotent: the hashes are compile-time constants, so repeat calls
        // store the same values.
        [RequiredByNativeCode(GenerateProxy = true)]
        internal static void RegisterAll()
        {
            ScriptableProcessorBindings.RegisterSeekMessageTypeHash(BurstRuntime.GetHashCode64<SeekMessage>());
        }
    }

    #region context-structs

    /// <summary>
    /// Fast access of shared memory between native DualThreadManager::Realtime and C#
    /// </summary>
    /// <remarks>
    /// Any usage must be covered by tests to ensure native and scripting have the same layout.
    /// </remarks>
    unsafe readonly struct RealtimeAccess
    {
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

        internal readonly bool IsCreated => m_Realtime != null;

        [NativeDisableUnsafePtrRestriction]
        readonly void* m_Realtime;
        readonly int m_Frame, m_DTM;

#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
    }

    internal unsafe struct DisposeArguments
    {
        internal ControlHeader* ControlContext;
        internal DualThreadHandle Self;
    }

    internal unsafe struct ControlUpdateArguments
    {
        internal ControlHeader* ControlContext;
        internal ProcessorInstance.AvailableData.Element* FirstElement;
        internal DualThreadHandle Self;
    }

    internal unsafe struct ConfigureArguments
    {
        internal AudioConfiguration Now;
        internal ControlHeader* ControlContext;
    }

    internal unsafe struct ControlMessageArguments
    {
        internal ControlHeader* Context;
        internal ProcessorInstance.Message* MessageData;
        internal DualThreadHandle Self;
        internal ProcessorInstance.Response StatusReturn;
    };

    internal unsafe struct RealtimeUpdateArguments
    {
        internal readonly RealtimeAccess Access;
        internal readonly ProcessorInstance.AvailableData.Element* Head;
        internal readonly DualThreadHandle Self;
    }

    internal unsafe struct RealtimeMessageArguments
    {
        internal RealtimeAccess Access;
        internal ProcessorInstance.Message* MessageData;
        internal DualThreadHandle Self;
        internal ProcessorInstance.Response StatusReturn;
    }

    #endregion

    enum ProcessorFunction : UInt32
    {
        Process = 1,
        Update = 2,
        OutputProcessEarly = 3,
        OutputProcess = 4,
        OutputProcessEnd = 5,
        OutputRemoved = 6,
        Message = 7
    };

    enum ControlFunction : UInt32
    {
        Dispose = 0xFF + 1,
        Configure = 0xFF + 2,
        Update = 0xFF + 3,
        Message = 0xFF + 4
    };

    // Layout mirror of the native audio::ProcessorHeader. C# never stores this; it is resolved transiently from a
    // handle (see ControlContext.GetProcessorHeader) only to read ControlReflectionData / Configuration. Keep the
    // field order in lockstep with the native struct.
    [NativeHeader("Modules/Audio/Public/ScriptableProcessors/ScriptableProcessor.h"), RequiredByNativeCode]
    unsafe struct ProcessorHeader
    {
        internal delegate* unmanaged[Cdecl]<ProcessorHeader*, ProcessorFunction, void*, void> NativeProcessorFunction;
        internal delegate* unmanaged[Cdecl]<ProcessorHeader*, ControlFunction, void*, void> NativeControlFunction;

        internal IntPtr ProcessorReflectionData;
        internal IntPtr ControlReflectionData;
    }

    [NativeHeader("Modules/Audio/Public/ScriptableProcessors/ScriptBindings/ScriptableProcessor.bindings.h")]
    internal static class ScriptableProcessorBindings
    {
        public static unsafe void QueueProcessorDispose(DualThreadHandle handle, ControlHeader* control)
            => QueueProcessorDisposeInternal(handle, control);

        public static unsafe bool AddDataToProcessorHandle(ControlHeader* control, DualThreadHandle handle, void* data, int size, int align, long typeHash)
        {
            return AddDataToProcessorHandleInternal(control, handle, data, size, align, typeHash);
        }

        public static unsafe ProcessorInstance.AvailableData.Element* GetAvailableDataForRealtime(in RealtimeAccess access, DualThreadHandle handle)
        {
            fixed (RealtimeAccess* pAccess = &access)
            {
                return (ProcessorInstance.AvailableData.Element*)GetRealtimeDataElementListForProcessorInternal(pAccess, handle);
            }
        }

        public static unsafe ProcessorInstance.AvailableData.Element* GetAvailableDataForControl(ControlHeader* control, DualThreadHandle handle)
        {
            return (ProcessorInstance.AvailableData.Element*)GetControlDataElementListForProcessorInternal(control, handle);
        }

        public static unsafe void ReturnDataFromProcessor(in RealtimeAccess access, DualThreadHandle handle, void* data, int size, int align, long typeHash)
        {
            fixed (RealtimeAccess* pAccess = &access)
            {
                ReturnDataFromProcessorInternal(pAccess, handle, data, size, align, typeHash);
            }
        }

        public static unsafe void InvokeRealtimeGenerate(in RealtimeAccess access, in IGeneratorProcessorExtensions.ProcessArguments args)
        {
            fixed (RealtimeAccess* pAccess = &access)
            {
                fixed (IGeneratorProcessorExtensions.ProcessArguments* pArgs = &args)
                    InvokeRealtimeGenerateInternal(pAccess, pArgs);
            }
        }

        public static unsafe void InvokeRealtimeMessage(ref RealtimeMessageArguments args)
        {
            fixed (RealtimeMessageArguments* pArgs = &args)
                InvokeRealtimeMessageInternal(pArgs);
        }

        /// <summary>
        /// Validates the validity of the handle and that you can currently call process/produce etc. with
        /// <paramref name="handle"/>.
        /// </summary>
        public static unsafe void ValidateCanProcess(DualThreadHandle handle, in RealtimeContext ctx)
        {
            fixed (RealtimeContext* pCtx = &ctx)
            {
                ValidateCanProcessInternal(handle, pCtx);
            }
        }

        public static unsafe bool CheckProcessorExists(DualThreadHandle handle, ControlHeader* control)
        {
            return CheckProcessorExistsInternal(handle, control);
        }

        public static unsafe void PerformRecursiveConfigure(DualThreadHandle handle, ControlHeader* control, in AudioConfiguration configuration)
        {
            PerformRecursiveConfigureInternal(handle, control, configuration);
        }

        public static unsafe void PerformRecursiveUpdate(DualThreadHandle handle, ControlHeader* control)
        {
            PerformRecursiveUpdateInternal(handle, control);
        }

        public static unsafe bool IsSystemWideReconfiguring(ControlHeader* control)
        {
            return IsSystemWideReconfiguringInternal(control);
        }

        public static unsafe ProcessorInstance.Response SendMessageToProcessor(DualThreadHandle handle, ControlHeader* control, ProcessorInstance.Message* message)
        {
            return SendMessageToProcessorInternal(handle, control, message);
        }

        public static unsafe ProcessorHeader* GetProcessorHeaderFromHandle(ControlHeader* control, DualThreadHandle handle)
        {
            return (ProcessorHeader*)GetProcessorHeaderFromHandleInternal(control, handle);
        }
        [NativeMethod(Name = "audio::GetProcessorHeaderFromHandle", IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe /*ProcessorHeader*/ void* GetProcessorHeaderFromHandleInternal(/*ControlHeader*/ void* control, DualThreadHandle handle);
        [NativeMethod(Name = "audio::SendMessageToProcessor", IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe ProcessorInstance.Response SendMessageToProcessorInternal(DualThreadHandle handle, /*ControlHeader* */ void* control, /* Message* */ void* message);

        public static void RegisterSeekMessageTypeHash(long typeHash) => RegisterSeekMessageTypeHashInternal(typeHash);
        [NativeMethod(Name = "audio::RegisterSeekMessageTypeHash", IsFreeFunction = true)]
        static extern void RegisterSeekMessageTypeHashInternal(long typeHash);

        [NativeMethod(Name = "audio::PerformRecursiveUpdate", IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void PerformRecursiveUpdateInternal(DualThreadHandle handle, /*ControlHeader* */ void* control);

        [NativeMethod(Name = "audio::IsSystemWideReconfiguring", IsFreeFunction = true)]
        static extern unsafe bool IsSystemWideReconfiguringInternal(/*ControlHeader* */ void* control);

        [NativeMethod(Name = "audio::PerformRecursiveConfigure", IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void PerformRecursiveConfigureInternal(DualThreadHandle handle, /*ControlHeader* */ void* control, in AudioConfiguration configuration);

        [NativeMethod(Name = "audio::ValidateCanProcess", IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        static extern unsafe void ValidateCanProcessInternal(DualThreadHandle handle, /* ProcessContext* */ void* processingContext);

        [NativeMethod(Name = "audio::QueueProcessorDispose", IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void QueueProcessorDisposeInternal(DualThreadHandle handle, /*ControlHeader* */ void* control);

        [NativeMethod(Name = "audio::GetRealtimeDataElementListForProcessor", IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        static extern unsafe /*DataElement*/ void* GetRealtimeDataElementListForProcessorInternal(/*RealtimeAccess**/ void* access, DualThreadHandle handle);

        [NativeMethod(Name = "audio::GetControlDataElementListForProcessor", IsFreeFunction = true)]
        static extern unsafe /*DataElement*/ void* GetControlDataElementListForProcessorInternal(/*ControlHeader* */ void* control, DualThreadHandle handle);

        [NativeMethod(Name = "audio::ReturnDataFromProcessor", IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        static extern unsafe void ReturnDataFromProcessorInternal(/*RealtimeAccess**/ void* access, DualThreadHandle handle, void* data, int size, int align, long typeHash);

        [NativeMethod(Name = "audio::InvokeRealtimeGenerate", IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        static extern unsafe void InvokeRealtimeGenerateInternal(/*RealtimeAccess**/ void* access, /* GeneratorProduceDataArguments**/ void* args);

        [NativeMethod(Name = "audio::InvokeRealtimeMessage", IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        static extern unsafe void InvokeRealtimeMessageInternal(/* ProcessorRealtimeMessageArguments**/ void* args);

        [NativeMethod(Name = "audio::AddDataToProcessor", IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe bool AddDataToProcessorHandleInternal(/*ControlHeader* */ void* control, DualThreadHandle handle, void* data, int size, int align, long typeHash);

        [NativeMethod(Name = "audio::CheckProcessorExists", IsFreeFunction = true)]
        static extern unsafe bool CheckProcessorExistsInternal(DualThreadHandle handle, /*ControlHeader* */ void* control);

        // This method is only used for testing what happens when exceptions are thrown from script bindings.
        [NativeMethod(Name = "audio::ThrowScriptingExceptionForTest", IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        internal static extern void ThrowScriptingExceptionForTest();
    }

    static class ProcessorExtensions
    {
        public static unsafe void DispatchGenericControl<TControl, TRealtime>(ref TControl control, ref TRealtime realtime, in ProcessorHeader header, void* additionalPtr, ControlFunction function)
            where TControl : unmanaged, ProcessorInstance.IControl<TRealtime>
            where TRealtime : unmanaged, ProcessorInstance.IRealtime
        {
            switch (function)
            {
                case ControlFunction.Dispose:
                {
                    var args = (DisposeArguments*)additionalPtr;
                    control.Dispose(new(args->ControlContext), ref realtime);
                    // No header free here: the header lives inside the bridge's native-owned slab, which
                    // the DTM frees with UNITY_FREE once this dispatch returns up through the destructor.
                    break;
                }
                case ControlFunction.Update:
                {
                    var args = (ControlUpdateArguments*)additionalPtr;
                    control.Update(new (args->ControlContext), new (args->Self));
                    break;
                }

                case ControlFunction.Message:
                {
                    var args = (ControlMessageArguments*)additionalPtr;
                    args->StatusReturn = control.OnMessage(new(args->Context), new(args->Self), *args->MessageData);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static unsafe void DispatchGenericProcessor<T>(ref T processor, in ProcessorHeader header, void* additionalPtr, ProcessorFunction function)
            where T : unmanaged, ProcessorInstance.IRealtime
        {
            switch (function)
            {
                case ProcessorFunction.Update:
                {
                    // Natively, this is wrapped in a single element pointer struct so just reference it directly through.
                    var args = *(RealtimeUpdateArguments**)additionalPtr;
                    processor.Update(new(args->Access), new(args->Self, args->Head));
                    break;
                }
                case ProcessorFunction.Message:
                {
                    // audio::InvokeRealtimeMessage passes the managed args straight through, so additionalPtr is a
                    // single pointer to them (unlike Update, which native wraps in a pointer-to-pointer).
                    var args = (RealtimeMessageArguments*)additionalPtr;
                    args->StatusReturn = processor.OnMessage(new(args->Access), new(args->Self), *args->MessageData);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
