// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using Unity.Audio;
using Unity.Burst;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Bindings;
using Unity.Collections.LowLevel.Unsafe;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;

namespace UnityEngine.Audio
{
    /// <summary>
    /// Factory for instantiating <see cref="EffectInstance"/> to be used internally or from other scripts.
    /// </summary>
    /// <remarks>
    /// Implement <see cref="IAudioEffect"/> on a <see cref="MonoBehaviour"/> and add the component to the same GameObject as an
    /// <see cref="AudioSource"/> to process the audio source's signal with the effect. The audio source discovers the
    /// component automatically, calls <see cref="CreateInstance"/>, and owns the returned <see cref="EffectInstance"/>.
    /// Use <see cref="AudioSource.GetEffectInstance{TComponent}"/> to interact with the instance while it is playing.
    ///
    /// <para/>
    ///
    /// <see cref="IAudioEffect"/>s do not own any created <see cref="EffectInstance"/> instances,
    /// and they don't try to store these for scripting.
    ///
    /// <para/>
    ///
    /// If you want to serialize a reference to a <see cref="IAudioEffect"/>, use the <see cref="Serializable"/>
    /// utility to store/load these references.
    /// </remarks>
    /// <seealso cref="AudioSource.GetEffectInstance{TComponent}"/>
    /// <seealso cref="ControlContext.AllocateEffect"/>
    [UsedByNativeCode]
    public interface IAudioEffect : EffectInstance.ICapabilities
    {
        /// <summary>
        /// A helper struct that allows you to serialize a reference to any <see cref="UnityEngine.Object"/>,
        /// <see cref="MonoBehaviour"/> or <see cref="ScriptableObject"/> that implements <see cref="IAudioEffect"/>.
        /// </summary>
        /// <remarks>
        /// Interface references are not directly serializable in user scripts if they are implemented on a
        /// <see cref="UnityEngine.Object"/>, even if using <see cref="SerializeReference"/>.
        /// </remarks>
        [Serializable]
        public struct Serializable
        {
            [SerializeField]
            internal UnityEngine.Object Reference;

            /// <summary>
            /// Get and Set the serialized object as a <see cref="IAudioEffect"/>.
            /// </summary>
            public IAudioEffect definition
            {
                get => Reference as IAudioEffect;
                set => Reference = (UnityEngine.Object)value;
            }

            /// <summary>
            /// A type-safe helper method that retrieves the internal value.
            /// </summary>
            public T Get<T>()
                where T : UnityEngine.Object, IAudioEffect
            {
                return Reference as T;
            }

            /// <summary>
            /// A type-safe helper method that sets the internal value.
            /// </summary>
            public void Set<T>(T value)
                where T : UnityEngine.Object, IAudioEffect
            {
                Reference = value;
            }

            /// <summary>
            /// Construct this serializable struct with an initial <see cref="IAudioEffect"/> value.
            /// </summary>
            /// <param name="effect">The initial value to set.</param>
            /// <exception cref="InvalidCastException">
            /// Thrown if <paramref name="effect"/> is not a <see cref="UnityEngine.Object"/>.
            /// </exception>
            public Serializable(IAudioEffect effect) => Reference = (UnityEngine.Object)effect;
        }

        /// <summary>
        /// Ask this interface to create an <see cref="EffectInstance"/>.
        /// </summary>
        /// <param name="context">The context associated with this <see cref="EffectInstance"/>.</param>
        /// <param name="nestedFormat">
        /// If not null, the <see cref="EffectInstance"/> shall be created as a nested effect with such format to be used from within another processor.
        /// </param>
        /// <param name="creationParameters">Initialization parameters passed through.</param>
        /// <returns>The created <see cref="EffectInstance"/>.</returns>
        EffectInstance CreateInstance(ControlContext context, AudioFormat? nestedFormat, EffectInstance.CreationParameters creationParameters);
    }


    /// <summary>
    /// A <see cref="ProcessorInstance"/> that processes audio data.
    /// </summary>
    /// <remarks>
    /// An <see cref="EffectInstance"/> reads audio from an input buffer, transforms it, and writes the result to an output buffer.
    /// You can define the EffectInstance through implementing the <see cref="EffectInstance.IControl{TProcessor}"/> and
    /// <see cref="IRealtime"/> interfaces, which defines the control and real-time thread behavior respectively.
    /// <para/>
    /// To use an effect in a scene, implement <see cref="IAudioEffect"/> on a <see cref="MonoBehaviour"/> and add the component
    /// to the same GameObject as an <see cref="AudioSource"/>. The <see cref="AudioSource"/> auto-discovers
    /// <see cref="IAudioEffect"/> components on its GameObject, and instantiates and manages their
    /// <see cref="EffectInstance"/>s for you.
    /// <para/>
    /// It is also possible to create your own effects through code using <see cref="ControlContext.AllocateEffect"/>,
    /// for example to run an effect nested inside another processor.
    /// Interacting with an instantiated effect depends on the ownership of the <see cref="EffectInstance"/>:
    /// If the effect has been created from an <see cref="AudioSource"/>, you would interact with it
    /// through the instance obtained from <see cref="AudioSource.GetEffectInstance{TComponent}"/>.
    /// </remarks>
    /// <example>
    /// The following example implements a gain effect and exposes it as a component you can add
    /// to the GameObject of an <see cref="AudioSource"/>.
    /// <code source="../../../../../Documentation/ManualDocs/com.unity.documentation-examples/Audio/Scripts/ScriptableAudioPipeline/EffectExample.cs" region="example-effect"/>
    /// </example>
    /// <seealso cref="IAudioEffect"/>
    /// <seealso cref="ControlContext.AllocateEffect"/>
    public unsafe struct EffectInstance : IEquatable<EffectInstance>
    {
#pragma warning disable 169
        /// <summary>
        /// Additional arguments passed to the <see cref="IRealtime.Process"/> method.
        /// </summary>
        /// <remarks>
        /// This type currently carries no data. It is reserved so that additional processing arguments can be
        /// introduced later without requiring existing effect implementations to change. Pass a default-initialized
        /// value when calling <see cref="Process"/> manually.
        /// </remarks>
        public ref struct Arguments
        {
            byte m_Reserved;
        }

        /// <summary>
        /// The result returned from an <see cref="IRealtime.Process"/> call.
        /// </summary>
        /// <remarks>
        /// This type currently carries no data. It is reserved so that processing results can be introduced later
        /// without requiring existing effect implementations to change. Return a default-initialized value from
        /// <see cref="IRealtime.Process"/>.
        /// </remarks>
        public ref struct Result
        {
            byte m_Reserved;
        }
#pragma warning restore 169

        /// <summary>
        /// Placeholder for future sidechain wiring and layout information.
        /// </summary>
        /// <remarks>
        /// This type currently carries no data. It's reserved so that sidechain support can be introduced later
        /// without requiring existing effect implementations to change.
        /// </remarks>
        public struct Sidechain
        {
            // Kept non-empty so the type can grow without an ABI-visible size change.
#pragma warning disable 169
            internal byte m_Reserved;
#pragma warning restore 169
        }

        /// <summary>
        /// Per-instance setup an effect reports back from <see cref="IControl{TProcessor}.Configure"/>.
        /// </summary>
        /// <remarks>
        /// Provides a home for configure-time output that depends on the running audio configuration,
        /// for example sidechain layout. Individual fields are placeholders today, so assign a
        /// default-initialized value from <see cref="IControl{TProcessor}.Configure"/>.
        /// </remarks>
        public struct Setup
        {
            /// <summary>
            /// Sidechain layout the effect requests for this instance. Currently unused; reserved for
            /// future extension.
            /// </summary>
            internal Sidechain sidechain { get; set; }
        }

        /// <summary>
        /// Additional parameters and initialization state for creating an <see cref="EffectInstance"/>.
        /// </summary>
        /// <remarks>
        /// Currently mirrors the fields on <see cref="ProcessorInstance.CreationParameters"/>, but exists as its
        /// own type so effect-only parameters, for example sidechain wiring, can be added later without touching
        /// the shared processor type.
        /// </remarks>
        /// <seealso cref="ControlContext.AllocateEffect"/>
        public struct CreationParameters
        {
            /// <summary>
            /// Control under what circumstances <see cref="ProcessorInstance.IControl{TRealtime}.Update"/> will be called.
            /// </summary>
            public ProcessorInstance.UpdateSetting controlUpdateSetting { get; set; }

            /// <summary>
            /// Control under what circumstances <see cref="ProcessorInstance.IRealtime.Update"/> will be called.
            /// </summary>
            public ProcessorInstance.UpdateSetting realtimeUpdateSetting { get; set; }

            internal readonly ProcessorInstance.InitializationFlags BuildInitializationFlags()
            {
                ProcessorInstance.InitializationFlags flags = 0;

                if (controlUpdateSetting == ProcessorInstance.UpdateSetting.UpdateIfDataIsAvailable)
                    flags |= ProcessorInstance.InitializationFlags.UpdateControlIfDataIsAvailable;
                else if (controlUpdateSetting == ProcessorInstance.UpdateSetting.UpdateAlways)
                    flags |= ProcessorInstance.InitializationFlags.UpdateControlAlways;

                if (realtimeUpdateSetting == ProcessorInstance.UpdateSetting.UpdateIfDataIsAvailable)
                    flags |= ProcessorInstance.InitializationFlags.UpdateProcessorIfDataIsAvailable;
                else if (realtimeUpdateSetting == ProcessorInstance.UpdateSetting.UpdateAlways)
                    flags |= ProcessorInstance.InitializationFlags.UpdateProcessorAlways;

                return flags;
            }
        }

        /// <summary>
        /// Static capabilities an effect declares up front.
        /// </summary>
        /// <remarks>
        /// This interface currently declares no members. When new capabilities are introduced, members will
        /// default to false so existing implementers do not need to change.
        /// </remarks>
        public interface ICapabilities
        {
        }

        /// <summary>
        /// The control interface an implementation of an <see cref="EffectInstance"/> must implement on a struct to be fully formed.
        /// </summary>
        /// <remarks>
        /// The control side of a <see cref="ProcessorInstance"/> receives various callbacks from a <see cref="ControlContext"/>
        /// from the logical control thread.
        /// You can annotate this with <see cref="T:Unity.Burst.BurstCompileAttribute"/> to have it compiled with Burst.
        /// </remarks>
        /// <typeparam name="TProcessor">The tandem processing counterpart.</typeparam>
        /// <seealso cref="ProcessorInstance.IControl{TRealtime}"/>
        [JobProducerType(typeof(IEffectControlExtensions.JobStruct<,>))]
        public interface IControl<TProcessor> : ProcessorInstance.IControl<TProcessor>
            where TProcessor : unmanaged, ProcessorInstance.IRealtime
        {
            /// <summary>
            /// Called to configure the <see cref="EffectInstance"/> before it is used, and when the audio system reconfigures.
            /// </summary>
            /// <remarks>
            /// In case of reconfiguration, the <typeparamref name="TProcessor"/> is temporarily suspended from processing,
            /// and you can safely modify its properties.
            /// </remarks>
            /// <param name="context">The <see cref="ControlContext"/> that owns this <see cref="EffectInstance"/>.</param>
            /// <param name="processor">The real-time part of the effect to configure.</param>
            /// <param name="configuration">The audio configuration the effect will run at (sample rate, channel count, buffer size).</param>
            /// <param name="setup">
            /// Out parameter where the effect reports per-instance setup information, for example sidechain
            /// layout. Currently a placeholder; assign <c>default</c> unless you need to declare
            /// something specific.
            /// </param>
            void Configure(ControlContext context, ref TProcessor processor, in AudioConfiguration configuration, out Setup setup);
        }

        /// <summary>
        /// Keep closely in sync with C++ counterpart.
        /// </summary>
        [NativeHeader("Modules/Audio/Public/ScriptableProcessors/ScriptBindings/EffectHandle.h"), RequiredByNativeCode]
        internal struct EffectHeader
        {
            internal ProcessorHeader Processor;
            // Must be kept in sync with audio::EffectHeader in EffectHandle.h
            unsafe void* m_DSP;
            //internal Configuration Configuration;
        }

        /// <summary>
        /// The processing interface an implementation of an <see cref="EffectInstance"/> must implement on a struct to be fully formed.
        /// </summary>
        /// <remarks>
        /// The processing side of a <see cref="ProcessorInstance"/> receives various callbacks from a <see cref="RealtimeContext"/>
        /// from the logical processing thread.
        /// You can annotate this with <see cref="T:Unity.Burst.BurstCompileAttribute"/> to have it compiled with Burst.
        /// </remarks>
        /// <seealso cref="ProcessorInstance.IRealtime"/>
        [JobProducerType(typeof(IEffectProcessorExtensions.JobStruct<>))]
        public interface IRealtime : Audio.ProcessorInstance.IRealtime
        {
            /// <summary>
            /// Called when you're asked to process the audio in <paramref name="inputBuffer"/> into <paramref name="outputBuffer"/>.
            /// </summary>
            /// <remarks>
            /// Unlike a generator, an effect both consumes audio (from <paramref name="inputBuffer"/>) and produces audio
            /// (into <paramref name="outputBuffer"/>). The two buffers are guaranteed to have matching
            /// <see cref="ChannelBuffer.channelCount"/> and <see cref="ChannelBuffer.frameCount"/>.
            /// <para/>
            /// Write every output sample: the output buffer starts undefined, and can reference the same memory as
            /// the input buffer, so read each input sample before you overwrite the corresponding output sample.
            /// </remarks>
            /// <param name="context">
            /// The <see cref="RealtimeContext"/> associated with this call.
            /// Use this to process any nested <see cref="ProcessorInstance"/>s or query/return data.
            /// </param>
            /// <param name="inputBuffer">The buffer containing the incoming audio your effect will read from.</param>
            /// <param name="outputBuffer">The buffer your effect will write its processing result into.</param>
            /// <param name="args">Additional arguments for processing.</param>
            /// <returns>A <see cref="Result"/> struct. It currently carries no data, so return a default-initialized value.</returns>
            /// <seealso cref="RealtimeContext"/>
            Result Process(in RealtimeContext context, ChannelBuffer inputBuffer, ChannelBuffer outputBuffer, Arguments args);
        }

        /// <summary>
        /// Manually process this particular <see cref="EffectInstance"/>.
        /// </summary>
        /// <remarks>
        /// In most use cases, you would not call this directly, but rather have the audio system call it for you.
        /// If you nest an <see cref="EffectInstance"/> inside another <see cref="ProcessorInstance"/>,
        /// call this from the owning processor's real-time part, within a mix cycle.
        /// </remarks>
        /// <param name="context">The <see cref="RealtimeContext"/> of the current mix cycle.</param>
        /// <param name="inputBuffer">The buffer holding the audio for the effect to process.</param>
        /// <param name="outputBuffer">The buffer the effect will put its processing result into.</param>
        /// <param name="effectArgs">Additional arguments passed along, which can be default-initialized.</param>
        /// <returns>A <see cref="Result"/> struct. It currently carries no data.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="inputBuffer"/> and <paramref name="outputBuffer"/> don't have matching channel and frame counts.
        /// </exception>
        public Result Process(in RealtimeContext context, ChannelBuffer inputBuffer, ChannelBuffer outputBuffer, /* TODO: pass to native runtime — DCME-1518 */ Arguments effectArgs)
        {
            if (inputBuffer.channelCount != outputBuffer.channelCount || inputBuffer.frameCount != outputBuffer.frameCount)
                throw new ArgumentException($"Input and output buffers must have matching dimensions. Input: {inputBuffer.channelCount}ch x {inputBuffer.frameCount} frames, Output: {outputBuffer.channelCount}ch x {outputBuffer.frameCount} frames");

            ScriptableProcessorBindings.ValidateCanProcess(m_Processor.Handle, context);

            fixed (float* writeBuffer = outputBuffer.Buffer)
            {
                fixed (float* readBuffer = inputBuffer.Buffer)
                {
                    fixed (RealtimeContext* pContext = &context)
                    {
                        var processArguments = new IEffectProcessorExtensions.ProcessArguments
                        {
                            InputBuffer = readBuffer,
                            OutputBuffer = writeBuffer,
                            Context = pContext,
                            Self = m_Processor.Handle,
                            FrameCount = inputBuffer.frameCount,
                            ChannelCount = inputBuffer.channelCount
                        };

                        ScriptableProcessorBindings.InvokeRealtimeEffect(context.Access, processArguments);
                    }
                }
            }

            return default; // TODO: populate real Result — DCME-1517
        }

        /// <summary>
        /// Manually reconfigure this <see cref="EffectInstance"/> with the given <see cref="AudioConfiguration"/>.
        /// </summary>
        /// <remarks>
        /// This method can only be called on nested <see cref="EffectInstance"/>s (created by your code, not root processors)
        /// and only during system-wide reconfiguration when <see cref="ControlContext.IsSystemWideReconfiguring"/> is true.
        /// <para/>
        /// During system-wide reconfiguration (for example, when the sample rate changes), the realtime audio thread is suspended,
        /// making it safe to reconfigure nested processors. Calling this method at any other time will throw an exception.
        /// </remarks>
        /// <param name="context">The <see cref="ControlContext"/> that owns this <see cref="EffectInstance"/>.</param>
        /// <param name="configuration">The new <see cref="AudioConfiguration"/> the effect should run at.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <paramref name="context"/> is default or has been disposed, or if called when
        /// <see cref="ControlContext.IsSystemWideReconfiguring"/> is false.
        /// </exception>
        /// <seealso cref="ControlContext.AllocateEffect"/>
        /// <seealso cref="ControlContext.IsSystemWideReconfiguring"/>
        public void Configure(ControlContext context, in AudioConfiguration configuration)
        {
            context.m_Handle.CheckValidOrThrow();

            ScriptableProcessorBindings.PerformRecursiveConfigure(
                m_Processor.Handle,
                context.Header,
                configuration
            );
        }

        /// <summary>
        /// Convert this <see cref="EffectInstance"/> to its more general <see cref="ProcessorInstance"/> representation.
        /// </summary>
        /// <remarks><see cref="ProcessorInstance"/>s are unowned and can safely be handed out to other users.</remarks>
        /// <param name="effect">The instance to convert.</param>
        /// <returns>The underlying <see cref="ProcessorInstance"/>.</returns>
        public static implicit operator ProcessorInstance(in EffectInstance effect) => effect.m_Processor;

        /// <summary>
        /// Checks if this instance equals another.
        /// </summary>
        /// <param name="other">The other instance for comparing.</param>
        /// <returns>True if the given instance is equal to this, otherwise, false.</returns>
        public bool Equals(EffectInstance other)
        {
            return m_Processor.Equals(other.m_Processor);
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

            return obj is EffectInstance instance && Equals(instance);
        }

        /// <summary>
        /// Checks if two instances are equal.
        /// </summary>
        /// <param name="a">The first instance for comparing.</param>
        /// <param name="b">The second instance for comparing.</param>
        /// <returns>True if the two given instances are equal, otherwise, false.</returns>
        public static bool operator ==(EffectInstance a, EffectInstance b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Checks if two instances are not equal.
        /// </summary>
        /// <param name="a">The first instance for comparing.</param>
        /// <param name="b">The second instance for comparing.</param>
        /// <returns>True if the two given instances are not equal, otherwise, false.</returns>
        public static bool operator !=(EffectInstance a, EffectInstance b)
        {
            return !a.Equals(b);
        }

        /// <summary>
        /// Retrieves a hash code based on this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return m_Processor.GetHashCode();
        }

        internal EffectInstance(DualThreadHandle handle)
            => m_Processor = new ProcessorInstance(handle);

        internal readonly ProcessorInstance m_Processor;
    }

    #region job-types

    [EditorBrowsable(EditorBrowsableState.Never)]
    static class IEffectControlExtensions
    {
        internal struct JobStruct<TUserControl, TUserProcessor>
            where TUserControl : unmanaged, EffectInstance.IControl<TUserProcessor>
            where TUserProcessor : unmanaged, EffectInstance.IRealtime
        {
            internal struct ControlStorage
            {
                public IEffectProcessorExtensions.JobStruct<TUserProcessor>.Storage HeaderAndProcessor;
                public TUserControl UserControl;
            }

            internal static readonly SharedStatic<IntPtr> jobReflectionData = SharedStatic<IntPtr>.GetOrCreate<JobStruct<TUserControl, TUserProcessor>>();

            [BurstDiscard]
            internal static unsafe void Initialize()
            {
                if (jobReflectionData.Data == IntPtr.Zero)
                    jobReflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(ControlStorage), typeof(TUserControl), (ExecuteJobFunction)Execute);
            }

            internal delegate void ExecuteJobFunction(ref ControlStorage storage, IntPtr additionalPtr, IntPtr additionalPtr2, ref JobRanges ranges, int jobIndex);

            public static unsafe void Execute(ref ControlStorage storage, IntPtr additionalPtr, IntPtr additionalPtr2, ref JobRanges ranges, int jobIndex)
            {
                var function = (ControlFunction)additionalPtr2;

                switch (function)
                {
                    case ControlFunction.Configure:
                    {
                        var args = (ConfigureArguments*)additionalPtr;

                        // Setup is currently discarded on the native side; the field exists so the
                        // interface can grow (e.g. sidechain layout) without breaking implementers.
                        storage.UserControl.Configure(new ControlContext(args->ControlContext), ref storage.HeaderAndProcessor.UserProcessor, args->Now, out _);
                        break;
                    }
                    default:
                    {
                        ProcessorExtensions.DispatchGenericControl(
                            ref storage.UserControl,
                            ref storage.HeaderAndProcessor.UserProcessor,
                            storage.HeaderAndProcessor.Header.Processor,
                            (void*)additionalPtr,
                            function
                        );
                        break;
                    }
                }
            }
        }

        internal static IntPtr GetReflectionData<TUserControl, TUserProcessor>()
            where TUserControl : unmanaged, EffectInstance.IControl<TUserProcessor>
            where TUserProcessor : unmanaged, EffectInstance.IRealtime
        {
            JobStruct<TUserControl, TUserProcessor>.Initialize();
            var reflectionData = JobStruct<TUserControl, TUserProcessor>.jobReflectionData.Data;
            return reflectionData;
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    static class IEffectProcessorExtensions
    {
        internal unsafe struct ProcessArguments
        {
            internal RealtimeContext* Context;
            internal float* InputBuffer;
            internal float* OutputBuffer;
            internal DualThreadHandle Self;
            internal int FrameCount;
            internal int ChannelCount;
        }

        internal struct JobStruct<TUserProcessor>
            where TUserProcessor : unmanaged, EffectInstance.IRealtime
        {
            internal struct Storage
            {
                public EffectInstance.EffectHeader Header;
                public TUserProcessor UserProcessor;
            }

            internal static readonly SharedStatic<IntPtr> jobReflectionData = SharedStatic<IntPtr>.GetOrCreate<JobStruct<TUserProcessor>>();

            [BurstDiscard]
            internal static unsafe void Initialize()
            {
                if (jobReflectionData.Data == IntPtr.Zero)
                    jobReflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(Storage), typeof(TUserProcessor), (ExecuteJobFunction)Execute);
            }

            internal delegate void ExecuteJobFunction(ref Storage storage, IntPtr additionalPtr, IntPtr additionalPtr2, ref JobRanges ranges, int jobIndex);

            public static unsafe void Execute(ref Storage storage, IntPtr additionalPtr, IntPtr additionalPtr2, ref JobRanges ranges, int jobIndex)
            {
                var function = (ProcessorFunction)additionalPtr2;

                switch (function)
                {
                    case ProcessorFunction.EffectProcess:
                    {
                        var args = (ProcessArguments*)additionalPtr;
                        var inputBuffer = new Span<float>(args->InputBuffer, args->ChannelCount * args->FrameCount);
                        var outputBuffer = new Span<float>(args->OutputBuffer, args->ChannelCount * args->FrameCount);
                        var inputChannelBuffer = new ChannelBuffer(inputBuffer, args->ChannelCount);
                        var outputChannelBuffer = new ChannelBuffer(outputBuffer, args->ChannelCount);

                        storage.UserProcessor.Process(*args->Context, inputChannelBuffer, outputChannelBuffer, default);
                        break;
                    }
                    default:
                    {
                        ProcessorExtensions.DispatchGenericProcessor(
                            ref storage.UserProcessor,
                            storage.Header.Processor,
                            (void*)additionalPtr,
                            function
                        );
                        break;
                    }
                }
            }
        }

        internal static IntPtr GetReflectionData<TUserProcessor>()
            where TUserProcessor : unmanaged, EffectInstance.IRealtime
        {
            JobStruct<TUserProcessor>.Initialize();
            var reflectionData = JobStruct<TUserProcessor>.jobReflectionData.Data;
            return reflectionData;
        }
    }

    #endregion

    [NativeHeader("Modules/Audio/Public/ScriptableProcessors/ScriptBindings/ScriptableProcessor.bindings.h")]
    internal static class ScriptableEffectBindings
    {
        [RequiredByNativeCode(GenerateProxy = true)]
        internal static unsafe void InstantiateEffectFromObject(UnityEngine.Object effectDefinitionObject, ref ControlHeader control, out EffectInstance runtimeHandle)
        {
            if (effectDefinitionObject is IAudioEffect definition)
            {
                fixed (ControlHeader* pResources = &control)
                {
                    runtimeHandle = definition.CreateInstance(new ControlContext(pResources), null, default);

                }
            }
            else
            {
                runtimeHandle = default;
                Debug.LogError($"Trying to use object '{effectDefinitionObject}' as an {nameof(IAudioEffect)} but it wasn't possible");
            }
        }

        internal static unsafe DualThreadHandle InitializeEffectHandle<TProcessor, TControl>(
            ref IEffectControlExtensions.JobStruct<TControl, TProcessor>.ControlStorage storage,
            ControlHeader* control,
            AudioConfiguration* nestedConfiguration,
            ProcessorInstance.InitializationFlags flags
        )
            where TProcessor : unmanaged, EffectInstance.IRealtime
            where TControl : unmanaged, EffectInstance.IControl<TProcessor>
        {
            fixed (EffectInstance.EffectHeader* headerPtr = &storage.HeaderAndProcessor.Header)
                return InternalInitializeEffectHandle(headerPtr, sizeof(IEffectControlExtensions.JobStruct<TControl, TProcessor>.ControlStorage), control, nestedConfiguration, flags);
        }

        [NativeMethod(Name = "audio::InitializeEffectHandle", IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe DualThreadHandle InternalInitializeEffectHandle(/*EffectHeader*/ void* header, int tailSize, /*ControlHeader*/ void* control, AudioConfiguration* nestedConfiguration, ProcessorInstance.InitializationFlags flags);

    }
}
