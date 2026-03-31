// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Audio;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IntegerTime;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Audio
{
#pragma warning disable 0169, 0649

    /// <summary>
    /// Utilities and additional compiler overloads for particular message payloads.
    /// </summary>
    public static unsafe partial class MessageExtensions
    {
        /// <summary>
        /// Return the inner reference sent.
        /// </summary>
        /// <remarks>
        /// This is an overload to specifically extract managed classes sent through messages.
        /// </remarks>
        /// <param name="message">The message to evaluate.</param>
        /// <exception cref="InvalidCastException">
        /// Thrown if the inner piece of data doesn't match the type of <typeparamref name="T"/>.
        /// </exception>
        /// <seealso cref="ProcessorInstance.Message.Get"/>
        public static T Get<T>(in this ProcessorInstance.Message message)
            where T : class
        {
            if (!message.Is<T>())
                throw new InvalidCastException($"Message does not contain data of type {typeof(T)}");

            return (T)GCHandle.FromIntPtr(message.ManagedHandle).Target;
        }

        /// <summary>
        /// Send a message with a piece of data to be immediately evaluated by the <see cref="ProcessorInstance.IControl{TRealtime}.OnMessage"/>
        /// </summary>
        /// <remarks>
        /// This is an overload specifically for sending class types to processors.
        /// </remarks>
        /// <returns>
        /// <see cref="ProcessorInstance.Response.Handled"/> if this <see cref="ProcessorInstance"/> acknowledged and processed the message,
        /// <see cref="ProcessorInstance.Response.Unhandled"/> if not or ignored.
        /// </returns>
        /// <seealso cref="ControlContext.SendMessage"/>
        public static ProcessorInstance.Response SendMessage<T>(this ControlContext context, ProcessorInstance processorInstance, T message)
            where T : class
        {
            return context.SendManagedMessage(processorInstance, message);
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
    /// Control data communication, scripting-, creation-, destruction- and data query of <see cref="ProcessorInstance"/>s in an audio system.
    /// </summary>
    /// <remarks>
    /// A <see cref="ControlContext"/> effectively represents an instance of an audio system, and is responsible for synchronizing efficient and
    /// deterministic data communication between a controlling thread and a realtime audio rendering thread.
    /// Use <see cref="ControlContext.builtIn"/> for scripting the shared audio system Unity uses,
    /// or use <see cref="ControlContext.CreateManualControlContext"/> for exercising your own auxillary system.
    /// </remarks>
    [NativeHeader("Modules/Audio/Public/ScriptableProcessors/ScriptBindings/ScriptableProcessor.bindings.h"), RequiredByNativeCode]
    public unsafe partial struct ControlContext : ProcessorInstance.IContext
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
            /// <summary>
            /// The context being manually driven.
            /// </summary>
            public ControlContext context => m_Context;

            ControlContext m_Context;

            /// <summary>
            /// Begin a mix.
            /// </summary>
            /// <remarks>
            /// "Mixes" in Unity span a period of time where you can issue processing calls.
            /// They must be ended in pairs with a call to <see cref="Manual.EndMix"/>.
            /// </remarks>
            /// <param name="dspTick"></param>
            /// <returns>
            /// A nullable <see cref="RealtimeContext"/>.
            /// This might hold no value in cases where audio is temporarily bypassed on the device, or similar.
            /// You should only <see cref="Manual.EndMix"/> if there's a non-null return from this function.
            /// </returns>
            public RealtimeContext? BeginMix(ulong dspTick)
            {
                RealtimeContext ret;
                if (InternalBeginManualMixFromControlContext(m_Context.m_Header, dspTick, &ret))
                    return ret;

                return null;
            }

            /// <summary>
            /// End a previously begun mix, additionally rendering any <see cref="RootOutputInstance"/>s into <paramref name="result"/>.
            /// </summary>
            /// <param name="result">
            /// A buffer with a size that matches the <see cref="AudioFormat"/> of the <see cref="ControlContext"/>,
            /// usually specified on creation through <see cref="ControlContext.CreateManualControlContext"/>.
            /// </param>
            public void EndMix(ChannelBuffer result)
            {
                InternalEndMixManualControlContext(m_Context.m_Header, result.Buffer);
            }

            /// <summary>
            /// Update and submit any queued commands found on <see cref="ControlContext"/>,
            /// making them available to the mixing functionality functions.
            /// </summary>
            /// <remarks>
            /// This also performs general maintenance and disposes <see cref="ProcessorInstance"/>s asynchronously.
            /// This must currently not be called while a mix is ongoing (<see cref="ControlContext.Manual"/> can currently only be used from one thread).
            /// This is called automatically on <see cref="ControlContext.builtIn"/> as a part of the normal Unity audio system update.
            /// </remarks>
            /// <seealso cref="PlayerLoop.PostLateUpdate.UpdateAudio"/>
            public void Update()
            {
                InternalUpdateManualControlContext(m_Context.m_Header);
            }

            /// <summary>
            /// Triggers a system-wide reconfiguration with the specified audio format.
            /// </summary>
            /// <remarks>
            /// This suspends realtime processing temporarily while calling Configure on all processors.
            /// During this time, <see cref="ControlContext.IsSystemWideReconfiguring"/> will return true,
            /// allowing nested generators to be safely reconfigured.
            /// </remarks>
            /// <param name="format">The new audio format to apply to the system.</param>
            internal void SetConfiguration(in AudioFormat format)
            {
                InternalSetConfigurationManualControlContext(m_Context.m_Header, format.audioConfiguration);
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

        ControlHeader* m_Header;
        internal Unity.Audio.Handle m_Handle;

        internal readonly ControlHeader *Header => m_Header;

        /// <summary>
        /// The default provided <see cref="ControlContext"/>
        /// which is used and updated in tandem with Unity's built-in audio system.
        /// </summary>
        /// <remarks>
        /// Referencing this instance guarantees that any changes and communication happen in atomic lockstep
        /// with the built in audio system, for deterministic audio results.
        /// </remarks>
        public static ControlContext builtIn => new(InternalGetBuiltInControlHeader());

        internal ControlContext(void* headerThatShouldBeOfResourceType)
        {
            var header = (ControlHeader*)headerThatShouldBeOfResourceType;
            m_Handle = header->Handle;
            m_Handle.CheckValidOrThrow();

            m_Header = header;
        }

        /// <summary>
        /// Allocate a <see cref="GeneratorInstance"/> with the specified realtime and control state.
        /// </summary>
        /// <remarks>
        /// This can generally be used to render the output of a <see cref="IAudioGenerator"/>, potentially from within another <see cref="GeneratorInstance"/>.
        /// </remarks>
        /// <seealso cref="IAudioGenerator.CreateInstance"/>
        /// <param name="controlState">The initial state available from the control thread.</param>
        /// <param name="realtimeState">The initial state available from the control thread.</param>
        /// <param name="nestedFormat">
        /// If not null, the returned <see cref="GeneratorInstance"/> will be treated as nested and use this format.
        /// </param>
        /// <param name="creationParameters">
        /// Additional parameters and initialization state for the processor.
        /// This is generally received from <see cref="IAudioGenerator.CreateInstance"/>
        /// </param>
        /// <returns>
        /// A <see cref="GeneratorInstance"/> you own and control, that must later be destroyed with <see cref="ControlContext.Destroy(GeneratorInstance)"/>.
        /// </returns>
        public readonly GeneratorInstance AllocateGenerator<TRealtime, TControl>(
            in TRealtime realtimeState,
            in TControl controlState,
            AudioFormat? nestedFormat = null,
            in ProcessorInstance.CreationParameters creationParameters = default
        )
            where TRealtime : unmanaged, GeneratorInstance.IRealtime
            where TControl : unmanaged, GeneratorInstance.IControl<TRealtime>
        {
            m_Handle.CheckValidOrThrow();

            var generatorChunk = ProcessorExtensions.CAllocChunk<IGeneratorControlExtensions.JobStruct<TControl, TRealtime>.ControlStorage>();

            var header = &generatorChunk->HeaderAndProcessor.Header;

            header->Processor.ProcessorReflectionData = IGeneratorProcessorExtensions.GetReflectionData<TRealtime>();
            header->Processor.ControlReflectionData = IGeneratorControlExtensions.GetReflectionData<TControl, TRealtime>();

            header->Configuration.IsRealtime = realtimeState.isRealtime;
            header->Configuration.IsFinite = realtimeState.isFinite;

            if (realtimeState.length is DiscreteTime time)
            {
                header->Configuration.ReportedLength = time;
                header->Configuration.HasKnownLength = true;
            }

            generatorChunk->HeaderAndProcessor.UserProcessor = realtimeState;
            generatorChunk->UserControl = controlState;

            var config = (nestedFormat ?? default).audioConfiguration;

            ScriptableGeneratorBindings.InitializeGeneratorHandle(header, m_Header, nestedFormat.HasValue ? &config : null, creationParameters.BuildInitializationFlags());

            return new(header);
        }

        /// <summary>
        /// Allocate a <see cref="RootOutputInstance"/> with the specified processor and control state.
        /// </summary>
        /// <remarks>
        /// <see cref="RootOutputInstance"/>s have low-level acces to the threading model of the audio system and the final audio output,
        /// and are generally used to integrate larger internal or external audio systems.
        /// </remarks>
        /// <param name="controlState">The initial state available from the control thread.</param>
        /// <param name="realtimeState">The initial state available from the control thread.</param>
        /// <param name="creationParameters">
        /// Additional parameters and initialization state for the processor.
        /// </param>
        /// <returns>
        /// A <see cref="RootOutputInstance"/> you own and control, that must later be destroyed with <see cref="ControlContext.Destroy(RootOutputInstance)"/>.
        /// </returns>
        public readonly RootOutputInstance AllocateRootOutput<TRealtime, TControl>(in TRealtime realtimeState, in TControl controlState, in ProcessorInstance.CreationParameters creationParameters = default)
            where TRealtime : unmanaged, RootOutputInstance.IRealtime
            where TControl : unmanaged, RootOutputInstance.IControl<TRealtime>
        {
            m_Handle.CheckValidOrThrow();

            var outputChunk = ProcessorExtensions.CAllocChunk<IRootOutputControlExtensions.JobStruct<TControl, TRealtime>.ControlStorage>();

            var header = &outputChunk->HeaderAndProcessor.Header;

            header->ProcessorReflectionData = IRootOutputProcessorExtensions.GetReflectionData<TRealtime>();
            header->ControlReflectionData = IRootOutputControlExtensions.GetReflectionData<TControl, TRealtime>();

            outputChunk->HeaderAndProcessor.UserProcessor = realtimeState;
            outputChunk->UserControl = controlState;

            IRootOutputProcessorExtensions.InitializeRootOutputHandle(header, m_Header, creationParameters.BuildInitializationFlags());

            return new(header);
        }

        /// <summary>
        /// Test whether <paramref name="processorInstance"/> is a <see cref="GeneratorInstance"/> built from a
        /// <typeparamref name="TRealtime"/> and <typeparamref name="TControl"/>.
        /// </summary>
        public readonly bool IsGenerator<TRealtime, TControl>(ProcessorInstance processorInstance)
            where TRealtime : unmanaged, GeneratorInstance.IRealtime
            where TControl : unmanaged, GeneratorInstance.IControl<TRealtime>
        {
            m_Handle.CheckValidOrThrow();
            processorInstance.Handle.CheckValidOrThrow();

            return processorInstance.Header->ControlReflectionData == IGeneratorControlExtensions.GetReflectionData<TControl, TRealtime>();
        }

        /// <summary>
        /// Test whether <paramref name="processorInstance"/> is a <see cref="RootOutputInstance"/> built from a
        /// <typeparamref name="TRealtime"/> and <typeparamref name="TControl"/>.
        /// </summary>
        public readonly bool IsRootOutput<TRealtime, TControl>(ProcessorInstance processorInstance)
            where TRealtime : unmanaged, RootOutputInstance.IRealtime
            where TControl : unmanaged, RootOutputInstance.IControl<TRealtime>
        {
            m_Handle.CheckValidOrThrow();
            processorInstance.Handle.CheckValidOrThrow();

            return processorInstance.Header->ControlReflectionData == IRootOutputControlExtensions.GetReflectionData<TControl, TRealtime>();
        }

        /// <summary>
        /// Test whether <paramref name="processorInstance"/> is valid and belongs to this <see cref="ControlContext"/>.
        /// </summary>
        public bool Exists(ProcessorInstance processorInstance)
        {
            m_Handle.CheckValidOrThrow();
            return ScriptableProcessorBindings.CheckProcessorExists(processorInstance.Handle, m_Header);
        }

        /// <summary>
        /// Send a message with a piece of data to be immediately evaluated by the <see cref="ProcessorInstance.IControl{TRealtime}.OnMessage"/>.
        /// </summary>
        /// <remarks>
        /// The <paramref name="message"/> will be passed to the <paramref name="processorInstance"/> by reference, so the <paramref name="processorInstance"/> can modify it.
        /// </remarks>
        /// <returns>
        /// <see cref="ProcessorInstance.Response.Handled"/> if <paramref name="processorInstance"/> acknowledged and processed the message,
        /// <see cref="ProcessorInstance.Response.Unhandled"/> if not or ignored.
        /// </returns>
        public ProcessorInstance.Response SendMessage<T>(ProcessorInstance processorInstance, ref T message)
            where T : unmanaged
        {
            m_Handle.CheckValidOrThrow();
            processorInstance.Handle.CheckValidOrThrow();

            fixed (T* pT = &message)
            {
                ProcessorInstance.Message transport = new ProcessorInstance.Message
                {
                    TypeHash = BurstRuntime.GetHashCode64<T>(),
                    Data = pT,
                    ManagedHandle = default
                };

                return ScriptableProcessorBindings.SendMessageToProcessor(processorInstance.Header, Header, &transport);
            }
        }

        internal ProcessorInstance.Response SendManagedMessage<T>(ProcessorInstance processorInstance, T message)
            where T : class
        {
            m_Handle.CheckValidOrThrow();
            processorInstance.Handle.CheckValidOrThrow();

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

            ProcessorInstance.Message transport = new ProcessorInstance.Message
            {
                TypeHash = BurstRuntime.GetHashCode64<T>(),
                Data = null,
                ManagedHandle = GCHandle.ToIntPtr(target)
            };

            var ret = ScriptableProcessorBindings.SendMessageToProcessor(processorInstance.Header, Header, &transport);

            target.Target = prior;

            return ret;
        }

        /// <summary>
        /// Destroy a <see cref="GeneratorInstance"/> previously allocated with <see cref="ControlContext.AllocateGenerator"/>.
        /// </summary>
        public void Destroy(GeneratorInstance generatorInstance) => DestroyProcessor(generatorInstance.m_ProcessorInstance);

        /// <summary>
        /// Destroy a <see cref="RootOutputInstance"/> previously allocated with <see cref="ControlContext.AllocateRootOutput"/>.
        /// </summary>
        public void Destroy(RootOutputInstance rootOutputInstance) => DestroyProcessor(rootOutputInstance.m_ProcessorInstance);

        /// <summary>
        /// Get the declared configuration <paramref name="generatorInstance"/> runs in.
        /// </summary>
        /// <seealso cref="GeneratorInstance.IControl{TRealtime}.Configure"/>
        public GeneratorInstance.Configuration GetConfiguration(GeneratorInstance generatorInstance)
        {
            m_Handle.CheckValidOrThrow();

            generatorInstance.m_ProcessorInstance.Handle.CheckValidOrThrow();

            return ((GeneratorInstance.GeneratorHeader*)generatorInstance.m_ProcessorInstance.Header)->Configuration;
        }

        /// <summary>
        /// Manually reconfigure this <see cref="GeneratorInstance"/> with the given <paramref name="format"/>.
        /// </summary>
        /// <remarks>
        /// This method can only be called on nested <see cref="GeneratorInstance"/>s (created by your code, not root processors)
        /// and only during system-wide reconfiguration when <see cref="IsSystemWideReconfiguring"/> is true.
        /// <para/>
        /// During system-wide reconfiguration (e.g., when the sample rate changes), the realtime audio thread is suspended,
        /// making it safe to reconfigure nested processors. Calling this method at any other time will throw an exception.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if called when <see cref="IsSystemWideReconfiguring"/> is false.
        /// </exception>
        /// <seealso cref="ControlContext.AllocateGenerator"/>
        /// <seealso cref="IsSystemWideReconfiguring"/>
        /// <seealso cref="Exists"/>
        public void Configure(GeneratorInstance generatorInstance, in AudioFormat format)
        {
            ScriptableProcessorBindings.PerformRecursiveConfigure(
                generatorInstance.m_ProcessorInstance.Handle,
                Header,
                format.audioConfiguration
            );
        }

        /// <summary>
        /// Returns true if the audio system is currently performing a system-wide reconfiguration.
        /// </summary>
        /// <remarks>
        /// This property is true when the system is reconfiguring all processors due to a change
        /// in audio settings (e.g., sample rate change). During system-wide reconfiguration, the
        /// realtime audio thread is suspended, making it safe to call <see cref="Configure"/> on
        /// nested <see cref="GeneratorInstance"/>s.
        /// <para/>
        /// During initial processor creation (outside of system-wide reconfiguration), this will be false,
        /// so calling <see cref="Configure"/> on nested processors will fail. Use lazy initialization
        /// patterns instead (check <see cref="Exists"/> first, create if needed, only call
        /// <see cref="Configure"/> when this property is true and the nested processor already exists).
        /// </remarks>
        /// <seealso cref="Configure"/>
        public bool IsSystemWideReconfiguring => ScriptableProcessorBindings.IsSystemWideReconfiguring(Header);

        /// <summary>
        /// Manually update this <see cref="GeneratorInstance"/>.
        /// </summary>
        /// <remarks>
        /// This is only valid on nested <see cref="ProcessorInstance"/>s.
        /// You must always update any nested <see cref="ProcessorInstance"/>s you have created.
        /// </remarks>
        /// <seealso cref="ControlContext.AllocateGenerator"/>
        /// <seealso cref="ControlContext.Configure"/>
        public void Update(GeneratorInstance generatorInstance)
        {
            ScriptableProcessorBindings.PerformRecursiveUpdate(generatorInstance.m_ProcessorInstance.Handle, Header);
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
        /// <param name="format">
        /// The initial <see cref="AudioFormat"/> the returned <see cref="ControlContext.Manual"/> will run in.
        /// </param>
        /// <remarks>
        /// This context is completely separate from <see cref="builtIn"/> and shares no data, lifetime or callbacks.
        /// It's an error to mix and match <see cref="ProcessorInstance"/>s from different <see cref="ControlContext"/>s.
        /// You must manually destroy this instance.
        /// </remarks>
        /// <returns>
        /// A <see cref="ControlContext.Manual"/> you own and control, that must later be disposed.
        /// </returns>
        public static Manual CreateManualControlContext(in AudioFormat format)
        {
            var ret = new ControlContext(InternalCreateControlContext());
            ret.m_Handle.CheckValidOrThrow();

            InternalSetConfigurationManualControlContext(ret.m_Header, format.audioConfiguration);

            return new(ret);
        }

        ProcessorInstance.AvailableData ProcessorInstance.IContext.GetAvailableData(Handle handle)
        {
            m_Handle.CheckValidOrThrow();

            // Cannot use processor.Handle.CheckValidOrThrow() here, as the handle may be in process of being disposed.
            // This will only be called from within Processor.Communication.GetAvailableData.
            if (!handle.Valid)
                throw new InvalidOperationException("Invalid handle provided to GetAvailableData");

            var dataElement = ScriptableProcessorBindings.GetAvailableDataForControl(m_Header, handle);

            return new ProcessorInstance.AvailableData(dataElement);
        }

        unsafe bool ProcessorInstance.IContext.SendData(Handle handle, void* data, int size, int align, long typehash)
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

        internal void DestroyProcessor(ProcessorInstance processorInstance)
        {
            m_Handle.CheckValidOrThrow();

            if (processorInstance.Handle.Equals(default))
                throw new InvalidOperationException("Default / zero-initialized value of processor being destroyed");

            ScriptableProcessorBindings.QueueProcessorDispose(processorInstance.Header, m_Header);
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
