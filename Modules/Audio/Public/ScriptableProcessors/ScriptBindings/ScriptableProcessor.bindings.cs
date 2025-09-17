// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using Unity.Audio;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Audio
{
    /// <undoc/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public unsafe interface IAudioScriptingContext // Internal tag interface for dispatching implementations.
    {
        /// <undoc/>
        internal Processor.AvailableData GetAvailableData(Handle handle);
        /// <undoc/>
        internal bool SendData(Handle handle, void* data, int size, int align, long typehash);
    }

    /// <summary>
    /// A temporary context tied to a particular mix cycle, and generally passed along when processing <see cref="Processor"/>s.
    /// </summary>
    /// <remarks>
    /// This also gives access to communicating data together with a <see cref="Processor.Pipe"/>.
    /// </remarks>
    /// <seealso cref="ControlContext.Manual.BeginMix"/>
    public unsafe struct ProcessingContext : IAudioScriptingContext
    {
        /// <summary>
        /// The DSP time at which the mix cycle began.
        /// </summary>
        public readonly UInt64 dspTime => m_DSPClock;

        /// <summary>
        /// True if this context was ever created.
        /// </summary>
        public readonly bool isCreated => Access.IsCreated;

        internal RealtimeAccess Access;
        UInt64 m_DSPClock;

        Processor.AvailableData IAudioScriptingContext.GetAvailableData(Handle handle)
            => new(ScriptableProcessorBindings.GetAvailableDataForRealtime(Access, handle));

        bool IAudioScriptingContext.SendData(Handle handle, void* data, int size, int align, long typehash)
        {
            ScriptableProcessorBindings.ReturnDataFromProcessor(Access, handle, data, size, align, typehash);
            return true;
        }
    }

    /// <summary>
    /// <see cref="Processor"/> is a handle to the common functionality of a scriptable processor.
    /// </summary>
    /// <remarks>
    /// This could be a <see cref="Generator"/> etc., but with limited structural API available.
    /// Use this together with <see cref="ControlContext"/> to query and send commands to the processor.
    /// </remarks>
    public readonly struct Processor
    {
        /// <summary>
        /// A context giving access to data that is currently available for the <see cref="Processor"/> this was passed to.
        /// </summary>
        /// <seealso cref="Processor.IProcessor.Update"/>
        /// <seealso cref="Processor.AvailableData"/>
        /// <seealso cref="Processor.Pipe"/>
        public unsafe struct UpdatedDataContext : IAudioScriptingContext
        {
            internal readonly RealtimeAccess Access;

            /// <undoc/>
            AvailableData IAudioScriptingContext.GetAvailableData(Handle handle)
                // Empty implementation: This is currently only used in a callback where <see cref="Pipe.Head"/> is already set.
                => default;

            /// <undoc/>
            bool IAudioScriptingContext.SendData(Handle handle, void* data, int size, int align, long typehash)
            {
                ScriptableProcessorBindings.ReturnDataFromProcessor(Access, handle, data, size, align, typehash);
                return true;
            }

            internal UpdatedDataContext(in RealtimeAccess access) => Access = access;
        }

        /// <summary>
        /// Base interface for the common processing logic of a <see cref="Processor"/>.
        /// </summary>
        /// <remarks>
        /// All <see cref="Processor"/>s must implement this interface to be fully formed,
        /// though usually each specific <see cref="Processor"/> implements a more specific interface inheriting from this one.
        /// </remarks>
        /// <seealso cref="Generator.IProcessor"/>"/>
        /// <seealso cref="Audio.Processor.IControl{TProcessor}"/>"/>
        public interface IProcessor
        {
            /// <summary>
            /// Implement this function to react to data sent to the <see cref="Processor"/> or communicate something back.
            /// </summary>
            /// <remarks>
            /// By default, this is called when there's data sent to your <see cref="Processor"/>.
            /// This can be changed by setting flags in <see cref="ControlContext.ProcessorCreationParameters.controlUpdateSetting"/>
            /// when initially creating the <see cref="Processor"/>.
            /// </remarks>
            /// <param name="context">The context giving access to available data.</param>
            /// <param name="pipe">Cross-thread communications pipe.</param>
            /// <seealso cref="Audio.Processor.IControl{TProcessor}.Update"/>
            public void Update(UpdatedDataContext context, Pipe pipe);
        }

        /// <summary>
        /// Base interface for common functionality of controlling a <see cref="Processor"/>.
        /// </summary>
        /// <remarks>
        /// Here you can implement any control logic that is required for the processor,
        /// that will run outside of the real time thread.
        ///
        /// <para/>
        /// 
        /// All <see cref="Processor"/>s must implement this interface to be fully formed,
        /// though usually each specific <see cref="Processor"/> implements a more specific interface inheriting from this one.
        /// </remarks>
        /// <seealso cref="Audio.Generator.IControl{TProcessor}"/>
        public interface IControl<TProcessor>
            where TProcessor : unmanaged, IProcessor
        {
            /// <summary>
            /// Called when the generator is destroyed, after having left the processing thread.
            /// </summary>
            /// <remarks>
            /// Here you can clean up any resources that were allocated for the control or realtime thread.
            /// It may take an indeterminate amount of time between <see cref="ControlContext.Destroy"/> is called and this
            /// will be called, and there may be any number of <see cref="Update"/> calls inbetween.
            /// This is because destroying processors is an asynchronous process.
            /// </remarks>
            /// <seealso cref="RootOutput.IProcessor.RemovedFromProcessing"/>
            /// <seealso cref="ControlContext.Destroy"/>
            public void Dispose(ControlContext context, ref TProcessor processor);

            /// <summary>
            /// Called if you have subscribed to continuous updates from the control thread, or if there is
            /// data returned from the <typeparamref name="TProcessor"/> counterpart.
            /// </summary>
            /// <remarks>
            /// This is guaranteed to be invoked before <see cref="Dispose"/> if there's any data that's been sent
            /// from <see cref="Pipe.SendData"/> and suitable flags have been set in
            /// <see cref="ControlContext.ProcessorCreationParameters.controlUpdateSetting"/>.
            /// </remarks>
            /// <seealso cref="ControlContext.Manual.Update"/>
            /// <seealso cref="ControlContext.ProcessorUpdateSetting"/>
            public void Update(ControlContext context, Pipe pipe);

            /// <summary>
            /// Called immediately from <see cref="ControlContext.SendMessage"/> when a message was sent to this <see cref="Processor"/>.
            /// </summary>
            /// <returns>
            /// <see cref="MessageStatus.Handled"/> if this <see cref="Processor"/> acknowledged and processed the message,
            /// <see cref="MessageStatus.Unhandled"/> if not or ignored.
            /// </returns>
            /// <param name="context">Context the processor is in.</param>
            /// <param name="pipe">Cross-thread communications pipe.</param>
            /// <param name="message">
            /// The message someone sent to you through <see cref="ControlContext.SendMessage"/>.
            /// The contents are sent by reference, so you can modify them and the sender will see the changes.
            /// </param>
            public MessageStatus OnMessage(ControlContext context, Pipe pipe, Message message);
        }

        /// <summary>
        /// A bi-directional communication system typically between two logical threads.
        /// </summary>
        /// <remarks>
        /// One thread is referred to as "control" (main thread typically) and one as "realtime" (audio thread typically).
        /// Using <see cref="ControlContext"/> and <see cref="ProcessingContext"/> as keys to the APIs,
        /// you can read data the other thread sent you or send data back.
        /// </remarks>
        public unsafe ref struct Pipe
        {
            internal readonly AvailableData.Element* Head;
            internal readonly Handle DualThreadHandle;

            /// <summary>
            /// Access an enumerator to the currently available data.
            /// </summary>
            /// <param name="context">The context key which provides data access from the respective thread.</param>
            /// <returns>A temporary collection of available data.</returns>
            /// <seealso cref="Processor.AvailableData"/>
            public readonly AvailableData GetAvailableData<TAudioContext>(TAudioContext context)
                where TAudioContext : unmanaged, IAudioScriptingContext
            {
                if (!DualThreadHandle.Valid)
                    throw new InvalidOperationException("DualThreadHandle is not valid, cannot get available data.");

                return Head != null ? new(Head) : context.GetAvailableData(DualThreadHandle);
            }

            /// <summary>
            /// Send data from <see cref="IProcessor"/> to <see cref="IControl{TProcessor}"/> or vice versa.
            /// </summary>
            /// <param name="context">The context key which targets the other logical thread receiver.</param>
            /// <param name="data">The data to be received in the other logical thread.</param>
            /// <returns>
            /// True if this operation is currently possible. This can fail for instance when the <see cref="Processor"/>
            /// is being in process of being disposed through <see cref="ControlContext.DestroyProcessor"/>,
            /// so you must guard against this if you are transferring resources.
            /// </returns>
            public readonly bool SendData<TAudioContext, T>(TAudioContext context, in T data)
                where TAudioContext : unmanaged, IAudioScriptingContext
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

            internal Pipe(Handle dualThreadHandle, AvailableData.Element* head = null)
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
            /// Use <see cref="Processor.Message.Is"/> to figure out if you can call this function with a particular type.
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
        /// A return value from <see cref="Audio.Processor.IControl{TProcessor}.OnMessage"/>
        /// that lets the message sender know if the message was handled or not.
        /// </summary>
        public enum MessageStatus
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
        /// <seealso cref="ControlContext.SendData"/>
        /// <seealso cref="Pipe.SendData"/>
        /// <example>
        /// <code source="../../../../../Tests/EditModeAndPlayModeTests/Audio/Assets/DocCodeExamples/SAP_HowToUseGetAvailableData.cs"/>
        /// </example>
        public unsafe ref struct AvailableData
        {
            /// <summary>
            /// A piece of temporary immutable type-erased data that was sent from an API like
            /// <see cref="ControlContext.SendData"/> or <see cref="Processor.Pipe.SendData"/>, received on likely another thread.
            /// </summary>
            /// <remarks>
            /// You can use <see cref="Processor.AvailableData.Element.TryGetData"/> to test and extract a piece of typed data,
            /// if the current element matches it.
            /// </remarks>
            public unsafe ref struct Element
            {
                /// <summary>
                /// Test and return a piece of typed data if this <see cref="Processor.AvailableData.Element"/> matches the type given.
                /// </summary>
                /// <param name="data">The typed piece of data to be extracted.</param>
                /// <returns>
                /// Whether this <see cref="Processor.AvailableData.Element"/> is of the same type as <typeparamref name="T"/>, and if so,
                /// whether the <paramref name="data"/> parameter was updated.
                /// </returns>
                /// <seealso cref="Processor.AvailableData"/>
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
                Unity.Audio.Handle m_AudioHandle;
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

        readonly internal Unity.Audio.Handle Handle;
        readonly internal unsafe ProcessorHeader* Header;

        internal unsafe Processor(Unity.Audio.Handle handle, ProcessorHeader* header)
        {
            Handle = handle;
            Header = header;
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
        internal Unity.Audio.Handle Self;
    }

    internal unsafe struct UpdateArguments
    {
        internal ControlHeader* ControlContext;
        internal Processor.AvailableData.Element* FirstElement;
        internal Unity.Audio.Handle Self;
    }

    internal unsafe struct ConfigureArguments
    {
        internal AudioConfiguration Now;
        internal ControlHeader* ControlContext;
    }

    internal unsafe struct MessageArguments
    {
        internal ControlHeader* Context;
        internal Processor.Message* MessageData;
        internal Unity.Audio.Handle Self;
        internal Processor.MessageStatus StatusReturn;
    };

    internal unsafe struct ProcessorRealtimeUpdateArguments
    {
        internal readonly RealtimeAccess Access;
        internal readonly Processor.AvailableData.Element* Head;
        internal readonly Unity.Audio.Handle Self;
    }

    #endregion

    enum ProcessorFunction : UInt32
    {
        Process = 1,
        Update = 2,
        OutputProcessEarly = 3,
        OutputProcess = 4,
        OutputProcessEnd = 5,
        OutputRemoved = 6
    };

    enum ControlFunction : UInt32
    {
        Dispose = 0xFF + 1,
        Configure = 0xFF + 2,
        Update = 0xFF + 3,
        Message = 0xFF + 4
    };

    [NativeHeader("Modules/Audio/Public/ScriptableProcessors/ScriptableProcessor.h"), RequiredByNativeCode]
    unsafe struct ProcessorHeader
    {
        void* m_Control;
        internal Unity.Audio.Handle DualThreadHandle;
        internal delegate* unmanaged[Cdecl]<ProcessorHeader*, ProcessorFunction, void*, void> NativeProcessorFunction;
        internal delegate* unmanaged[Cdecl]<ProcessorHeader*, ControlFunction, void*, void> NativeControlFunction;

        internal IntPtr ProcessorReflectionData;
        internal IntPtr ControlReflectionData;

        /// <summary>
        /// Call the C# / Job / native function directly.
        /// This only happens in response from direct C# handle APIs, like nested produce, main-thread commands etc.
        /// </summary>
        /// <remarks>
        /// This does NOT assert validity of the handle.
        /// TODO: Maybe we shouldn't have an implementation of this in C# as well. We could just defer into the engine implementation
        /// at the expense of additional managed/native trampolines or extra virtual function calls.
        /// </remarks>
        public void InvokeProcessor(ProcessorFunction fn, void* args)
        {
            fixed (ProcessorHeader* pThis = &this)
            {
                switch (fn)
                {
                    case ProcessorFunction.Update:
                    case ProcessorFunction.OutputProcessEarly:
                    case ProcessorFunction.OutputProcess:
                    case ProcessorFunction.OutputProcessEnd:
                    case ProcessorFunction.OutputRemoved:
                        throw new NotSupportedException($"Cannot manually invoke {fn}, these are called automatically");

                    default:
                        break;
                }

                NativeProcessorFunction(pThis, fn, args);
            }
        }

        public bool IsSameControl(ControlHeader* other) => m_Control == other;
    }

    [NativeHeader("Modules/Audio/Public/ScriptableProcessors/ScriptBindings/ScriptableProcessor.bindings.h")]
    internal static class ScriptableProcessorBindings
    {
        public static unsafe void QueueProcessorDispose(ProcessorHeader* header, ControlHeader* control)
            => QueueProcessorDisposeInternal(header, control);

        public static unsafe bool AddDataToProcessorHandle(ControlHeader* control, in Unity.Audio.Handle handle, void* data, int size, int align, long typeHash)
        {
            return AddDataToProcessorHandleInternal(control, handle, data, size, align, typeHash);
        }

        public static unsafe Processor.AvailableData.Element* GetAvailableDataForRealtime(in RealtimeAccess access, in Unity.Audio.Handle handle)
        {
            fixed (RealtimeAccess* pAccess = &access)
            {
                return (Processor.AvailableData.Element*)GetRealtimeDataElementListForProcessorInternal(pAccess, handle);
            }
        }

        public static unsafe Processor.AvailableData.Element* GetAvailableDataForControl(ControlHeader* control, in Unity.Audio.Handle handle)
        {
            return (Processor.AvailableData.Element*)GetControlDataElementListForProcessorInternal(control, handle);
        }

        public static unsafe void ReturnDataFromProcessor(in RealtimeAccess access, in Unity.Audio.Handle handle, void* data, int size, int align, long typeHash)
        {
            fixed (RealtimeAccess* pAccess = &access)
            {
                ReturnDataFromProcessorInternal(pAccess, handle, data, size, align, typeHash);
            }
        }

        /// <summary>
        /// Validates the validity of the handle and that you can currently call process/produce etc. with
        /// <paramref name="header"/>.
        /// </summary>
        public static unsafe void ValidateCanProcess(in Unity.Audio.Handle handle, in ProcessingContext ctx)
        {
            fixed (ProcessingContext* pCtx = &ctx)
            {
                ValidateCanProcessInternal(handle, pCtx);
            }
        }

        public static unsafe bool CheckProcessorExists(Unity.Audio.Handle handle, ControlHeader* control)
        {
            return CheckProcessorExistsInternal(handle, control);
        }

        public static unsafe void PerformRecursiveConfigure(Unity.Audio.Handle handle, ControlHeader* control, in AudioConfiguration configuration)
        {
            PerformRecursiveConfigureInternal(handle, control, configuration);
        }

        public static unsafe void PerformRecursiveUpdate(Unity.Audio.Handle handle, ControlHeader* control)
        {
            PerformRecursiveUpdateInternal(handle, control);
        }

        public static unsafe Processor.MessageStatus SendMessageToProcessor(ProcessorHeader* header, ControlHeader* control, Processor.Message* message)
        {
            return SendMessageToProcessorInternal(header, control, message);
        }

        [NativeMethod(Name = "audio::SendMessageToProcessor", IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe Processor.MessageStatus SendMessageToProcessorInternal(/*ProcessorHeader* */ void* header, /*ControlHeader* */ void* control, /* Message* */ void* message);

        [NativeMethod(Name = "audio::PerformRecursiveUpdate", IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void PerformRecursiveUpdateInternal(Unity.Audio.Handle handle, /*ControlHeader* */ void* control);

        [NativeMethod(Name = "audio::PerformRecursiveConfigure", IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void PerformRecursiveConfigureInternal(Unity.Audio.Handle handle, /*ControlHeader* */ void* control, in AudioConfiguration configuration);

        [NativeMethod(Name = "audio::ValidateCanProcess", IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        static extern unsafe void ValidateCanProcessInternal(in Unity.Audio.Handle handle, /* ProcessContext* */ void* processingContext);

        [NativeMethod(Name = "audio::QueueProcessorDispose", IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void QueueProcessorDisposeInternal(/*ProcessorHeader* */ void* header, /*ControlHeader* */ void* control);

        [NativeMethod(Name = "audio::GetRealtimeDataElementListForProcessor", IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe /*DataElement*/ void* GetRealtimeDataElementListForProcessorInternal(/*RealtimeAccess**/ void* access, in Unity.Audio.Handle handle);

        [NativeMethod(Name = "audio::GetControlDataElementListForProcessor", IsFreeFunction = true)]
        static extern unsafe /*DataElement*/ void* GetControlDataElementListForProcessorInternal(/*ControlHeader* */ void* control, in Unity.Audio.Handle handle);

        [NativeMethod(Name = "audio::ReturnDataFromProcessor", IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        static extern unsafe void ReturnDataFromProcessorInternal(/*RealtimeAccess**/ void* access, in Unity.Audio.Handle handle, void* data, int size, int align, long typeHash);

        [NativeMethod(Name = "audio::AddDataToProcessor", IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe bool AddDataToProcessorHandleInternal(/*ControlHeader* */ void* control, in Unity.Audio.Handle handle, void* data, int size, int align, long typeHash);

        [NativeMethod(Name = "audio::CheckProcessorExists", IsFreeFunction = true)]
        static extern unsafe bool CheckProcessorExistsInternal(Unity.Audio.Handle handle, /*ControlHeader* */ void* control);
    }

    static class ProcessorExtensions
    {
        internal static unsafe T* CAllocChunk<T>()
            where T : unmanaged
        {
            var chunk = (T*)UnsafeUtility.MallocTracked(sizeof(T), UnsafeUtility.AlignOf<T>(), Allocator.Persistent, 3);
            *chunk = default;
            return chunk;
        }

        public static unsafe void DispatchGenericControl<TControl, TProcessor>(ref TControl control, ref TProcessor processor, in ProcessorHeader header, void* additionalPtr, ControlFunction function)
            where TControl : unmanaged, Processor.IControl<TProcessor>
            where TProcessor : unmanaged, Processor.IProcessor
        {
            switch (function)
            {
                case ControlFunction.Dispose:
                {
                    var args = (DisposeArguments*)additionalPtr;
                    control.Dispose(new(args->ControlContext), ref processor);

                    fixed (ProcessorHeader* pHeader = &header)
                        UnsafeUtility.FreeTracked(pHeader, Allocator.Persistent);

                    break;
                }
                case ControlFunction.Update:
                {
                    var args = (UpdateArguments*)additionalPtr;
                    control.Update(new (args->ControlContext), new (args->Self));
                    break;
                }

                case ControlFunction.Message:
                {
                    var args = (MessageArguments*)additionalPtr;
                    args->StatusReturn = control.OnMessage(new(args->Context), new(args->Self), *args->MessageData);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static unsafe void DispatchGenericProcessor<T>(ref T processor, in ProcessorHeader header, void* additionalPtr, ProcessorFunction function)
            where T : unmanaged, Processor.IProcessor
        {
            switch (function)
            {
                case ProcessorFunction.Update:
                {
                    // Natively, this is wrapped in a single element pointer struct so just reference it directly through.
                    var args = *(ProcessorRealtimeUpdateArguments**)additionalPtr;
                    processor.Update(new(args->Access), new(args->Self, args->Head));
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
