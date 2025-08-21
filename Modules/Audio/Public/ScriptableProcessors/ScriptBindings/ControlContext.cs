// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Unity.Audio;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IntegerTime;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Audio
{
#pragma warning disable 0169, 0649

    public static unsafe partial class MessageExtensions
    {
        /// <summary>
        /// Return the inner reference sent.
        /// </summary>
        /// <exception cref="InvalidCastException">
        /// Thrown if the inner piece of data doesn't match the type of <typeparamref name="T"/>.
        /// </exception>
        /// <seealso cref="Processor.Message.Get{T}"/>
        public static T Get<T>(in this Processor.Message message)
            where T : class
        {
            if (!message.Is<T>())
                throw new InvalidCastException($"Message does not contain data of type {typeof(T)}");

            return (T)GCHandle.FromIntPtr(message.ManagedHandle).Target;
        }

        /// <summary>
        /// Send a message with a piece of data to be immediately evaluated by the <see cref="IProcessorControl{TProcessor}.OnMessage"/>
        /// </summary>
        /// <remarks>
        /// This is an overload specifically for sending class types to processors.
        /// </remarks>
        /// <returns>
        /// <see cref="Processor.MessageStatus.Handled"/> if this <see cref="Processor"/> acknowledged and processed the message,
        /// <see cref="Processor.MessageStatus.Unhandled"/> if not or ignored.
        /// </returns>
        /// <seealso cref="ControlContext.SendMessage{T}(Processor, ref T)"/>
        public static Processor.MessageStatus SendMessage<T>(this ControlContext context, Processor processor, T message)
            where T : class
        {
            return context.SendManagedMessage(processor, message);
        }
    }

    [NativeHeader("Modules/Audio/Public/ScriptableProcessors/ControlHeader.h"), RequiredByNativeCode]
    unsafe struct ControlHeader
    {
        internal Unity.Audio.Handle Handle;
        // Needs to be a pointer as this structure is natively allocated. This can be optimized later.
        internal IntPtr ManagedTransport;
        // More stuff here, careful.
    }


    /// <summary>
    /// Internal representation of flags controlling how a <see cref="Processor"/> is handled over the course of its lifetime.
    /// </summary>
    /// <seealso cref="ControlContext.ProcessorUpdateSetting"/>
    [System.Flags]
    internal enum ProcessorInitializationFlags : UInt32
    {
        /// <summary>
        /// Invoke <see cref="Processor.IControl{TProcessor}.Update"/> only if data has been returned from
        /// <see cref="Processor.Pipe.SendData"/> since the last update.
        /// </summary>
        UpdateControlIfDataIsAvailable = 1 << 1,
        /// <summary>
        /// Always invoke <see cref="Processor.IControl{TProcessor}.Update"/> on this processor on every update.
        /// </summary>
        UpdateControlAlways = 1 << 2,


        /// <summary>
        /// Invoke <see cref="Processor.IProcessor.Update"/> only if data has been sent from
        /// <see cref="Processor.Pipe.SendData"/> or <see cref="ControlContext.SendData"/> since the last update.
        /// </summary>
        UpdateProcessorIfDataIsAvailable = 1 << 3,
        /// <summary>
        /// Always invoke <see cref="Processor.IProcessor.Update"/> on this processor on every update.
        /// </summary>
        UpdateProcessorAlways = 1 << 4,
    }

    [NativeHeader("Modules/Audio/Public/ScriptableProcessors/ScriptBindings/ScriptableProcessor.bindings.h"), RequiredByNativeCode]
    public unsafe partial struct ControlContext : IAudioScriptingContext
    {
        /// <summary>
        /// A manually-managed <see cref="ControlContext"/> for usage outside of the normal Unity audio system (tests, custom audio system).
        /// </summary>
        /// <remarks>
        /// This allows you to drive normally automatic functionality on scriptable processors contained within this <see cref="ControlContext"/>.
        /// It's currently undefined behaviour to call any APIs from different threads simultaneously, so this can currently only be used for "offline usage".
        /// </remarks>
        public struct Manual : IDisposable
        {
            public ControlContext context => m_Context;
            ControlContext m_Context;

            public ProcessingContext? BeginMix(ulong dspTick)
            {
                ProcessingContext ret;
                if (InternalBeginManualMixFromControlContext(m_Context.m_Header, dspTick, &ret))
                    return ret;

                return null;
            }

            public void EndMix(ChannelBuffer result)
            {
                InternalEndMixManualControlContext(m_Context.m_Header, result.Buffer);
            }

            /// <summary>
            /// Update and submit any queued commands found on <see cref="ControlContext"/>, making them available to the mixing functionality functions.
            /// </summary>
            /// <remarks>
            /// This also performs general maintenance and disposes processors asynchronously.
            /// </remarks>
            public void Update()
            {
                InternalUpdateManualControlContext(m_Context.m_Header);
            }

            /// <summary>
            /// Dispose a custom-created <see cref="ControlContext"/>.
            /// </summary>
            /// <remarks>
            /// This will immediately invalidate any references or objects created from this <see cref="ControlContext"/>.
            /// </remarks>
            public void Dispose()
            {
                m_Context.m_Handle.CheckValidOrThrow();
                InternalDestroyControlContext(m_Context.m_Header);
            }

            internal Manual(in ControlContext context)
            {
                m_Context = context;
            }
        }

        /// <summary>
        /// Settings controlling how a <see cref="Processor"/> is updated over the course of its lifetime.
        /// </summary>
        public enum ProcessorUpdateSetting
        {
            /// <summary>
            /// The default update setting for a <see cref="Processor"/>.
            /// </summary>
            /// <remarks>
            /// This is equivalent to <see cref="UpdateIfDataIsAvailable"/>.
            /// A default-inititalized value of <see cref="ProcessorUpdateSetting"/> will correspond to this as well.
            /// </remarks>
            Default = 0,

            /// <summary>
            /// Never invoke <see cref="Processor.IControl{TProcessor}.Update"/> on this processor nor <see cref="Processor.IProcessor.Update"/>.
            /// </summary>
            NeverUpdate = 1,

            /// <summary>
            /// Invoke <see cref="Processor.IControl{TProcessor}.Update"/> or <see cref="Processor.IProcessor.Update"/>
            /// only if data has been sent or returned from <see cref="Processor.Pipe.SendData"/> since the last update.
            /// </summary>
            UpdateIfDataIsAvailable = 2,
            /// <summary>
            /// Always invoke <see cref="Processor.IControl{TProcessor}.Update"/> or <see cref="Processor.IProcessor.Update"/>
            /// on this <see cref="Processor"> on every update.
            /// </summary>
            UpdateAlways = 3,
        }

        /// <summary>
        /// Additional data and parameters specifying how a <see cref="Processor"/> should be created.
        /// </summary>
        /// <remarks>
        /// These are generally suggested setup from whomever is creating the <see cref="Processor"/>, such as a <see cref="IGeneratorDefinition"/>.
        /// You can change properties to suit your particular needs.
        /// </remarks>
        public struct ProcessorCreationParameters
        {
            /// <summary>
            /// Control under what circumstances <see cref="Processor.IControl{TProcessor}.Update"/> will be called.
            /// </summary>
            public ProcessorUpdateSetting controlUpdateSetting { get; set; }
            /// <summary>
            /// Control under what circumstances <see cref="Processor.IProcessor.Update"/> will be called.
            /// </summary>
            public ProcessorUpdateSetting processorUpdateSetting { get; set; }

            internal readonly ProcessorInitializationFlags BuildInitializationFlags()
            {
                ProcessorInitializationFlags flags = 0;

                if (controlUpdateSetting == ProcessorUpdateSetting.UpdateIfDataIsAvailable)
                    flags |= ProcessorInitializationFlags.UpdateControlIfDataIsAvailable;
                else if (controlUpdateSetting == ProcessorUpdateSetting.UpdateAlways)
                    flags |= ProcessorInitializationFlags.UpdateControlAlways;

                if (processorUpdateSetting == ProcessorUpdateSetting.UpdateIfDataIsAvailable)
                    flags |= ProcessorInitializationFlags.UpdateProcessorIfDataIsAvailable;
                else if (processorUpdateSetting == ProcessorUpdateSetting.UpdateAlways)
                    flags |= ProcessorInitializationFlags.UpdateProcessorAlways;

                return flags;
            }
        }

        ControlHeader* m_Header;
        Unity.Audio.Handle m_Handle;

        internal readonly ControlHeader *Header => m_Header;

        /// <summary>
        /// The default provided <see cref="ControlContext"/>
        /// which is used and updated in tandem with Unity's built-in audio system.
        /// </summary>
        public static ControlContext builtIn => new(InternalGetBuiltInControlHeader());

        internal ControlContext(void* headerThatShouldBeOfResourceType)
        {
            var header = (ControlHeader*)headerThatShouldBeOfResourceType;
            m_Handle = header->Handle;
            m_Handle.CheckValidOrThrow();

            m_Header = header;
        }

        /// <summary>
        /// Allocate a <see cref="Generator"/> with the specified processor and control state.
        /// </summary>
        /// <seealso cref="IGeneratorDefinition.CreateRuntime(ControlContext, DSPConfiguration?, ProcessorCreationParameters)"/>
        /// <param name="nestedConfiguration">
        /// If not null, the returned <see cref="Generator"/> will be treated as nested and use this configuration.
        /// </param>
        /// <param name="creationParameters">
        /// Additional parameters and initialization state for the processor.
        /// This is generally received from <see cref="IGeneratorDefinition.CreateRuntime(ControlContext, DSPConfiguration?, ProcessorCreationParameters)"/>
        /// </param>
        public readonly Generator AllocateGenerator<TProcessor, TControl>(
            in TProcessor processorState,
            in TControl controlState,
            DSPConfiguration? nestedConfiguration = null,
            in ProcessorCreationParameters creationParameters = default
        )
            where TProcessor : unmanaged, Generator.IProcessor
            where TControl : unmanaged, Generator.IControl<TProcessor>
        {
            m_Handle.CheckValidOrThrow();

            var generatorChunk = ProcessorExtensions.CAllocChunk<IGeneratorControlExtensions.JobStruct<TControl, TProcessor>.ControlStorage>();

            var header = &generatorChunk->HeaderAndProcessor.Header;

            header->Processor.ProcessorReflectionData = IGeneratorProcessorExtensions.GetReflectionData<TProcessor>();
            header->Processor.ControlReflectionData = IGeneratorControlExtensions.GetReflectionData<TControl, TProcessor>();

            header->Configuration.IsRealtime = processorState.isRealtime;
            header->Configuration.IsFinite = processorState.isFinite;

            if (processorState.length is DiscreteTime time)
            {
                header->Configuration.ReportedLength = time;
                header->Configuration.HasKnownLength = true;
            }

            generatorChunk->HeaderAndProcessor.UserProcessor = processorState;
            generatorChunk->UserControl = controlState;

            var config = (nestedConfiguration ?? default).audioConfiguration;

            ScriptableGeneratorBindings.InitializeGeneratorHandle(header, m_Header, nestedConfiguration.HasValue ? &config : null, creationParameters.BuildInitializationFlags());

            return new(header);
        }

        public readonly RootOutput AllocateRootOutput<TProcessor, TControl>(in TProcessor processorState, in TControl controlState, in ProcessorCreationParameters creationParameters = default)
            where TProcessor : unmanaged, RootOutput.IProcessor
            where TControl : unmanaged, RootOutput.IControl<TProcessor>
        {
            m_Handle.CheckValidOrThrow();

            var outputChunk = ProcessorExtensions.CAllocChunk<IRootOutputControlExtensions.JobStruct<TControl, TProcessor>.ControlStorage>();

            var header = &outputChunk->HeaderAndProcessor.Header;

            header->ProcessorReflectionData = IRootOutputProcessorExtensions.GetReflectionData<TProcessor>();
            header->ControlReflectionData = IRootOutputControlExtensions.GetReflectionData<TControl, TProcessor>();

            outputChunk->HeaderAndProcessor.UserProcessor = processorState;
            outputChunk->UserControl = controlState;

            IRootOutputProcessorExtensions.InitializeRootOutputHandle(header, m_Header, creationParameters.BuildInitializationFlags());

            return new(header);
        }

        public readonly bool IsGenerator<TGenerator, TControl>(Processor processor)
            where TGenerator : unmanaged, Generator.IProcessor
            where TControl : unmanaged, Generator.IControl<TGenerator>
        {
            m_Handle.CheckValidOrThrow();
            processor.Handle.CheckValidOrThrow();

            return processor.Header->ControlReflectionData == IGeneratorControlExtensions.GetReflectionData<TControl, TGenerator>();
        }

        public readonly bool IsRootOutput<TRootOutput, TControl>(Processor processor)
            where TRootOutput : unmanaged, RootOutput.IProcessor
            where TControl : unmanaged, RootOutput.IControl<TRootOutput>
        {
            m_Handle.CheckValidOrThrow();
            processor.Handle.CheckValidOrThrow();

            return processor.Header->ControlReflectionData == IRootOutputControlExtensions.GetReflectionData<TControl, TRootOutput>();
        }

        public bool Exists(Processor processor)
        {
            m_Handle.CheckValidOrThrow();
            return ScriptableProcessorBindings.CheckProcessorExists(processor.Handle, m_Header);
        }

        /// <summary>
        /// Send a message with a piece of data to be immediately evaluated by the <see cref="IProcessorControl{TProcessor}.OnMessage"/>.
        /// </summary>
        /// <remarks>
        /// The <paramref name="message"/> will be passed to the processor by reference, so the processor can modify it.
        /// </remarks>
        /// <returns>
        /// <see cref="Processor.MessageStatus.Handled"/> if this <see cref="Processor"/> acknowledged and processed the message,
        /// <see cref="Processor.MessageStatus.Unhandled"/> if not or ignored.
        /// </returns>
        public Processor.MessageStatus SendMessage<T>(Processor processor, ref T message)
            where T : unmanaged
        {
            m_Handle.CheckValidOrThrow();

            fixed (T* pT = &message)
            {
                Processor.Message transport = new Processor.Message
                {
                    TypeHash = BurstRuntime.GetHashCode64<T>(),
                    Data = pT,
                    ManagedHandle = default
                };

                return ScriptableProcessorBindings.SendMessageToProcessor(processor.Header, Header, &transport);
            }
        }

        internal Processor.MessageStatus SendManagedMessage<T>(Processor processor, T message)
            where T : class
        {
            m_Handle.CheckValidOrThrow();
            object prior = null;
            GCHandle target;

            if (Header->ManagedTransport != IntPtr.Zero)
            {
                target = GCHandle.FromIntPtr(Header->ManagedTransport);

                prior = target.Target;
                target.Target = message;
            }
            else
            {
                target = GCHandle.Alloc(message, GCHandleType.Normal);
                Header->ManagedTransport = GCHandle.ToIntPtr(target);
            }

            Processor.Message transport = new Processor.Message
            {
                TypeHash = BurstRuntime.GetHashCode64<T>(),
                Data = null,
                ManagedHandle = GCHandle.ToIntPtr(target)
            };

            var ret = ScriptableProcessorBindings.SendMessageToProcessor(processor.Header, Header, &transport);

            target.Target = prior;

            return ret;
        }

        /// <summary>
        /// Send a piece of data to make it available to the processor in the next <see cref="Processor.IProcessor.Update"/>.
        /// </summary>
        public unsafe void SendData<T>(Processor processor, in T data)
            where T : unmanaged
        {
            m_Handle.CheckValidOrThrow();
            processor.Handle.CheckValidOrThrow();

            if (!processor.Header->IsSameControl(Header))
                throw new ArgumentException($"{nameof(Processor)} belongs to a different {nameof(ControlContext)}");

            fixed (T* pData = &data)
            {
                var ret = ScriptableProcessorBindings.AddDataToProcessorHandle(
                    m_Header,
                    processor.Handle,
                    pData,
                    sizeof (T),
                    UnsafeUtility.AlignOf<T>(),
                    BurstRuntime.GetHashCode64<T>()
                );

                // An exception will be thrown if the handle is invalid or disposed (just above).
                // It should never fail otherwise, currently.
                Debug.Assert(ret, "Failed to send data from control context, this is an internal error");
            }
        }

        public Processor.AvailableData GetAvailableData(Processor processor)
        {
            m_Handle.CheckValidOrThrow();
            processor.Handle.CheckValidOrThrow();

            var dataElement = ScriptableProcessorBindings.GetAvailableDataForControl(m_Header, processor.Handle);

            return new Processor.AvailableData(dataElement);
        }

        public void Destroy(Generator generator) => DestroyProcessor(generator.Processor);
        public void Destroy(RootOutput root) => DestroyProcessor(root.Processor);

        public Generator.Configuration GetConfiguration(Generator generator)
        {
            m_Handle.CheckValidOrThrow();

            generator.Processor.Handle.CheckValidOrThrow();

            return ((Generator.GeneratorHeader*)generator.Processor.Header)->Configuration;
        }

        /// <summary>
        /// Forcefully wait for any commands created on <see cref="builtIn"/> or on any derivative
        /// objects to have taken effect.
        /// </summary>
        /// <remarks>
        /// Normally this happens automatically over time. This should only be used in exceptional scenarios
        /// (because it mostly likely stalls for a while) or for deterministic testing.
        /// </remarks>
        public static void WaitForBuiltInQueueFlush()
        {
            InternalWaitForQueueFlush(builtIn.m_Header);
        }

        /// <summary>
        /// Create a new <see cref="ControlContext"/> with additional functionality you can drive manually.
        /// </summary>
        /// <remarks>
        /// This context is completely separate from <see cref="builtIn"/> and shares no data, lifetime or callbacks.
        /// You must manually destroy this instance.
        /// </remarks>
        public static Manual CreateManualControlContext(in DSPConfiguration config)
        {
            var ret = new ControlContext(InternalCreateControlContext());
            ret.m_Handle.CheckValidOrThrow();

            InternalSetConfigurationManualControlContext(ret.m_Header, config.audioConfiguration);

            return new(ret);
        }

        Processor.AvailableData IAudioScriptingContext.GetAvailableData(Handle handle)
        {
            m_Handle.CheckValidOrThrow();

            // Cannot use processor.Handle.CheckValidOrThrow() here, as the handle may be in process of being disposed.
            // This will only be called from within Processor.Communication.GetAvailableData.
            if (!handle.Valid)
                throw new InvalidOperationException("Invalid handle provided to GetAvailableData");

            var dataElement = ScriptableProcessorBindings.GetAvailableDataForControl(m_Header, handle);

            return new Processor.AvailableData(dataElement);
        }

        unsafe bool IAudioScriptingContext.SendData(Handle handle, void* data, int size, int align, long typehash)
        {
            return ScriptableProcessorBindings.AddDataToProcessorHandle(
                m_Header,
                handle,
                data,
                size,
                align,
                typehash
            );
        }

        internal void DestroyProcessor(Processor processor)
        {
            m_Handle.CheckValidOrThrow();

            if (processor.Handle.Equals(default))
                throw new InvalidOperationException("Default / zero-initialized value of processor being destroyed");

            ScriptableProcessorBindings.QueueProcessorDispose(processor.Header, m_Header);
        }

        [RequiredByNativeCode(GenerateProxy = true)]
        internal static unsafe void CleanupHeader(ref ControlHeader header)
        {
            if (header.ManagedTransport != IntPtr.Zero)
            {
                // If the header has a managed transport, we need to free it.
                var gcHandle = GCHandle.FromIntPtr(header.ManagedTransport);
                if (gcHandle.IsAllocated)
                {
                    gcHandle.Free();
                }
            }
        }

        [NativeMethod(Name = "audio::GetBuiltInControlHeader", IsFreeFunction = true)]
        static extern unsafe internal void* InternalGetBuiltInControlHeader();

        [NativeMethod(Name = "audio::WaitForQueueFlush", IsFreeFunction = true)]
        extern static void InternalWaitForQueueFlush(void* header);

        // Void* because bindings complain about mismatched blittable layout (and rightly so, probably).
        [NativeMethod(Name = "audio::CreateControlContext", IsFreeFunction = true)]
        extern static void* InternalCreateControlContext();

        [NativeMethod(Name = "audio::DestroyControlContext", IsFreeFunction = true, ThrowsException = true)]
        extern static void InternalDestroyControlContext(void* header);

        [NativeMethod(Name = "audio::BeginMixManualControlContext ", IsFreeFunction = true, ThrowsException = true)]
        extern static bool InternalBeginManualMixFromControlContext(void* header, ulong dspTick, void* resultContext);

        [NativeMethod(Name = "audio::EndMixManualControlContext", IsFreeFunction = true, ThrowsException = true)]
        extern static void InternalEndMixManualControlContext(void* header, Span<float> data);

        [NativeMethod(Name = "audio::UpdateManualControlContext", IsFreeFunction = true, ThrowsException = true)]
        extern static void InternalUpdateManualControlContext(void* header);

        [NativeMethod(Name = "audio::SetConfigurationManualControlContext", IsFreeFunction = true, ThrowsException = true)]
        extern static void InternalSetConfigurationManualControlContext(void* header, AudioConfiguration config);
    }
}
