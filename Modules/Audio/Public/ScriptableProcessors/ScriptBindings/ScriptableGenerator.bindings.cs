// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using UnityEngine.Bindings;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using Unity.Jobs.LowLevel.Unsafe;
using static Unity.Collections.LowLevel.Unsafe.BurstLike;
using Unity.Burst;
using System.ComponentModel;
using UnityEngine.Scripting;

namespace UnityEngine.Audio
{
    /// <summary>
    /// Factory for instantiating <see cref="Generator"/> to be used internally or from other scripts.
    /// </summary>
    /// <remarks>
    /// If you want to serialize a reference to a <see cref="Generator.IDefinition"/> and have an object picker for
    /// asset / component based factories, use the <see cref="Serializable"/> utility to store/load these references.
    /// </remarks>
    [UsedByNativeCode]
    public interface IGeneratorDefinition : Generator.ICapabilities
    {
        /// <summary>
        /// Serializable utility for storing object-pickable <see cref="IGeneratorDefinition"/> references on eg.
        /// <see cref="MonoBehaviour"/> or any other host object annotated with <see cref="SerializableAttribute"/>.
        /// </summary>
        [Serializable]
        public struct Serializable
        {
            [SerializeField]
            internal Object Reference;

            public IGeneratorDefinition definition
            {
                get => Reference as IGeneratorDefinition;
                set => Reference = (Object)value;
            }

            public T Get<T>()
                where T : Object, IGeneratorDefinition
            {
                return Reference as T;
            }

            public void Set<T>(T value)
                where T : Object, IGeneratorDefinition
            {
                Reference = value;
            }

            public Serializable(IGeneratorDefinition generatorDefinition) => Reference = (Object)generatorDefinition;
        }

        /// <summary>
        /// Ask this interface to instantiate a runtime <see cref="Generator"/> instance.
        /// </summary>
        /// <param name="context">The context associated with this <see cref="Generator"/></param>
        /// <param name="nestedConfiguration">
        /// If not null, the <see cref="Generator"/> shall be created as a nested generator with such configuration to be used from within another processor.
        /// </param>
        /// <param name="creationParameters">Initialization parameters passed through.</param>
        Generator CreateRuntime(ControlContext context, DSPConfiguration? nestedConfiguration, ControlContext.ProcessorCreationParameters creationParameters);
    }

    public unsafe struct Generator
    {
        /// <summary>
        /// Describes the runtime behaviour of the <see cref="Generator"/>.
        /// These reported values are cached in the beginning and assumed to not change.
        /// </summary>
        /// <remarks>
        /// This must be implemented identically on both the asset / offline version in <see cref="IGeneratorDefinition"/> and the runtime
        /// instance <see cref="Processor.IProcessor"/>.
        /// This is so that offline tooling and asset management can reason about the content, and the engine identically so without depending on loading each other.
        /// </remarks>
        public interface ICapabilities
        {
            /// <summary>
            /// Return true if this <see cref="Generator"/> is finite, meaning it has a defined length and will terminate eventually.
            /// </summary>
            /// <remarks>
            /// <see cref="Generator"/> do not have to know their length ahead of time but for static content and editor tooling it helps.
            /// </remarks>
            /// <seealso cref="length"/>
            public bool isFinite { get; }

            /// <summary>
            /// Declare whether this <see cref="Generator"/> must be treated as a source rendering in real time.
            /// Realtime generators must be processed at the same sampling rate and buffer size as the system they run in.
            /// </summary>
            /// <remarks>
            /// Realtime <see cref="Generator"/>s shall return the same output every time they are processed.
            /// Additionally, the system enforces the buffer size of the passed-in <see cref="ChannelBuffer"/> equals the length of the
            /// <see cref="ProcessingContext.Configuration.dspBufferLength"/>.
            ///
            /// Use cases include hardware devices that cannot be rendered at arbitrary rate, or systematic graphs that render ahead of time.
            /// If you are not sure whether your <see cref="Generator"/> is realtime or not, you should set this to false.
            /// </remarks>
            /// <seealso cref="Processor.IProcessor.Configure(ControlContext, in DSPConfiguration, out Setup, ref Properties)"/>.
            public bool isRealtime { get; }

            /// <summary>
            /// If you know your length in advance, you should set it here. Otherwise it's assumed to end at some point in the future,
            /// if <see cref="isFinite"/> is set to true.
            /// </summary>
            /// <remarks>
            /// This value is ignored for <see cref="Generator"/>s that are not finite (ie. <see cref="isFinite"/>).
            /// </remarks>
            public DiscreteTime? length { get; }
        }

        /// <summary>
        /// A required setup of information you need to provide to the instantiator.
        /// </summary>
        public readonly struct Setup
        {
            /// <summary>
            /// Declare the <see cref="AudioSpeakerMode"/> of this <see cref="Generator"/> and by extension
            /// the number of channels this <see cref="Generator"/> will use.
            /// This directly determines the size of the <see cref="ChannelBuffer"/> passed to the <see cref="Process"/> method.
            /// </summary>
            public readonly AudioSpeakerMode speakerMode;

            /// <summary>
            /// Declare the sampling rate the output of this <see cref="Generator"/> will be played at.
            /// </summary>
            public readonly int sampleRate;

            /// <summary>
            /// Creates a new generator setup.
            /// </summary>
            public Setup(AudioSpeakerMode speakerMode, int sampleRate)
            {
                this.speakerMode = speakerMode;
                this.sampleRate = sampleRate;
            }

            public Setup(in DSPConfiguration fromConfiguration)
                : this(fromConfiguration.speakerMode, fromConfiguration.sampleRate)
            {

            }
        }

        /// <summary>
        /// Represents optional or additional metadata about the <see cref="Generator"/>.
        /// </summary>
        public struct Properties
        {
            // Speakermode etc., metadata
            byte m_Reserved;
        }

        public struct Configuration
        {
            internal Setup Setup;
            internal Properties Properties;
            internal DiscreteTime ReportedLength;
            internal bool IsFinite;
            internal bool IsRealtime;
            internal bool HasKnownLength;

            public Setup setup => Setup;
            public Properties properties => Properties;
            public bool isFinite => IsFinite;
            public bool isRealtime => IsRealtime;
            public DiscreteTime? length => HasKnownLength ? ReportedLength : null;
        }

        public ref struct Result
        {
            internal int m_ProcessedFrames;

            public int processedFrames => m_ProcessedFrames;


            public static implicit operator Result(int processedFrames)
            {
                return new Result { m_ProcessedFrames = processedFrames };
            }
        }

        public ref struct Arguments
        {
            /// <summary>
            /// If <see cref="Generator.IDefinition.IsRealtime"/> is set, this field contains the aggregate playback speed of this source.
            /// </summary>
            internal float Speed;
        }

        [JobProducerType(typeof(IGeneratorControlExtensions.JobStruct<,>))]
        public interface IControl<TProcessor> : Processor.IControl<TProcessor>
            where TProcessor : unmanaged, Processor.IProcessor
        {
            /// <summary>
            /// Called to configure the <see cref="Generator"/> before it is used, and when the audio system reconfigures.
            /// The default implementation will set the <paramref name="setup"/> and <paramref name="properties"/> based on the
            /// <paramref name="configuration"/>.
            /// </summary>
            /// <remarks>
            /// In case of reconfiguration, the <typeparamref name="TProcessor"/> is temporarily suspended from processing,
            /// and you can safely modify its properties.
            /// </remarks>
            public void Configure(
                ControlContext context,
                ref TProcessor processor,
                in DSPConfiguration configuration,
                out Setup setup,
                ref Properties properties
            );
        }

        [JobProducerType(typeof(IGeneratorProcessorExtensions.JobStruct<>))]
        public interface IProcessor : Audio.Processor.IProcessor, ICapabilities
        {
            public Result Process(in ProcessingContext context, Processor.Pipe pipe, ChannelBuffer buf, Arguments args);
        }

        /// <summary>
        /// Keep closely in sync with C++ counterpart.
        /// </summary>
        [NativeHeader("Modules/Audio/Public/ScriptableProcessors/ScriptBindings/GeneratorHandle.h"), RequiredByNativeCode]
        internal struct GeneratorHeader
        {
            internal ProcessorHeader Processor;
            internal Configuration Configuration;
        }

        /// <inheritdoc/>
        public Result Process(in ProcessingContext context, ChannelBuffer buf, Arguments args)
        {
            ScriptableProcessorBindings.ValidateCanProcess(Processor.Handle, context);

            fixed (float* writeBuffer = buf.Buffer)
            {
                fixed (ProcessingContext* pContext = &context)
                {
                    var processArguments = new IGeneratorProcessorExtensions.ProcessArguments
                    {
                        AudioBuffer = writeBuffer,
                        Context = pContext,
                        FrameCount = buf.frameCount
                    };

                    Processor.Header->InvokeProcessor(ProcessorFunction.Process, &processArguments);

                    return processArguments.Result;
                }
            }

        }

        /// <summary>
        /// Manually configure this <see cref="Generator"/> with the given <paramref name="configuration"/>.
        /// </summary>
        /// <remarks>
        /// This is only valid on nested processors.
        /// </remarks>
        public void Configure(ControlContext context, in DSPConfiguration configuration)
        {
            ScriptableProcessorBindings.PerformRecursiveConfigure(
                Processor.Handle,
                context.Header,
                configuration.audioConfiguration
            );
        }

        /// <summary>
        /// Manually update this <see cref="Generator"/>.
        /// </summary>
        /// <remarks>
        /// This is only valid on nested processors.
        /// </remarks>
        public void Update(ControlContext context)
        {
            ScriptableProcessorBindings.PerformRecursiveUpdate(Processor.Handle, context.Header);
        }

        public static implicit operator Processor(in Generator generator) => generator.Processor;

        internal Generator(GeneratorHeader* header)
            => Processor = new Processor(header->Processor.DualThreadHandle, &header->Processor);

        internal readonly Processor Processor;
    }

    #region job-types

    [EditorBrowsable(EditorBrowsableState.Never)]
    static class IGeneratorControlExtensions
    {
        internal struct JobStruct<TUserControl, TUserProcessor>
            where TUserControl : unmanaged, Generator.IControl<TUserProcessor>
            where TUserProcessor : unmanaged, Generator.IProcessor
        {
            internal struct ControlStorage
            {
                public IGeneratorProcessorExtensions.JobStruct<TUserProcessor>.Storage HeaderAndProcessor;
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

                        storage.UserControl.Configure(
                            new ControlContext(args->ControlContext),
                            ref storage.HeaderAndProcessor.UserProcessor,
                            new DSPConfiguration(args->Now),
                            out storage.HeaderAndProcessor.Header.Configuration.Setup,
                            ref storage.HeaderAndProcessor.Header.Configuration.Properties
                        );

                        if (storage.HeaderAndProcessor.Header.Configuration.IsRealtime && storage.HeaderAndProcessor.Header.Configuration.Setup.sampleRate != args->Now.sampleRate)
                        {
                            Debug.LogError("Realtime generators must obey system sampling rate");
                        }
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

        internal static IntPtr GetReflectionData<TUserControl, TUserGenerator>()
            where TUserGenerator : unmanaged, Generator.IProcessor
            where TUserControl : unmanaged, Generator.IControl<TUserGenerator>
        {
            JobStruct<TUserControl, TUserGenerator>.Initialize();
            var reflectionData = JobStruct<TUserControl, TUserGenerator>.jobReflectionData.Data;
            return reflectionData;
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    static class IGeneratorProcessorExtensions
    {
        internal unsafe ref struct ProcessArguments
        {
            internal ProcessingContext* Context;
            internal float* AudioBuffer;
            internal Unity.Audio.Handle Self;
            /// <summary>
            /// The total size of <see cref="AudioBuffer"/> is <see cref="FrameCount"/> times the amount of channels this generator has declared (<see cref="Generator.Configuration.setup.channelCount"/>).
            /// </summary>
            internal int FrameCount;
            internal Generator.Arguments GeneratorArguments;
            internal Generator.Result Result;
        }

        internal struct JobStruct<TUserProcessor>
            where TUserProcessor : unmanaged, Generator.IProcessor
        {
            internal struct Storage
            {
                public Generator.GeneratorHeader Header;
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
                    case ProcessorFunction.Process:
                    {
                        var args = (ProcessArguments*)additionalPtr;

                        var buffer = new Span<float>(args->AudioBuffer, storage.Header.Configuration.Setup.speakerMode.ChannelCount() * args->FrameCount);
                        var channelBuffer = new ChannelBuffer(buffer, storage.Header.Configuration.Setup.speakerMode.ChannelCount());

                        args->Result = storage.UserProcessor.Process(*args->Context, new(args->Self), channelBuffer, args->GeneratorArguments);
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
            where TUserProcessor : unmanaged, Generator.IProcessor
        {
            JobStruct<TUserProcessor>.Initialize();
            var reflectionData = JobStruct<TUserProcessor>.jobReflectionData.Data;
            return reflectionData;
        }
    }

    #endregion

#pragma warning restore 0169, 0649

    [NativeHeader("Modules/Audio/Public/ScriptableProcessors/ScriptBindings/ScriptableProcessor.bindings.h")]
    internal static class ScriptableGeneratorBindings
    {
        [RequiredByNativeCode(GenerateProxy = true)]
        internal static unsafe void InstantiateGeneratorFromObject(Object generatorObjectDefinition, ref ControlHeader control, out Generator runtimeHandle)
        {
            if (generatorObjectDefinition is IGeneratorDefinition definition)
            {
                fixed (ControlHeader* pResources = &control)
                {
                    var context = new ControlContext(pResources);
                    runtimeHandle = definition.CreateRuntime(context, null,default);

                    if (context.Exists(runtimeHandle))
                    {
                        var decl = context.GetConfiguration(runtimeHandle);

                        if (definition.isFinite != decl.IsFinite)
                        {
                            Debug.LogError($"Generator {generatorObjectDefinition} has inconsistent isFinite declaration: {definition.isFinite} vs {decl.IsFinite}");
                        }

                        if (definition.isRealtime != decl.isRealtime)
                        {
                            Debug.LogError($"Generator {generatorObjectDefinition} has inconsistent isRealtime declaration: {definition.isRealtime} vs {decl.isRealtime}");
                        }

                        if (definition.length != decl.length)
                        {
                            Debug.LogError($"Generator {generatorObjectDefinition} has inconsistent length declaration: {definition.length} vs {decl.length}");
                        }
                    }
                }
            }
            else
            {
                runtimeHandle = default;
                Debug.LogError($"Trying to play object {generatorObjectDefinition}, but it doesn't implement {nameof(IGeneratorDefinition)}");
            }
        }

        internal static unsafe void InitializeGeneratorHandle(Generator.GeneratorHeader* header, ControlHeader* control, AudioConfiguration* nestedConfiguration, ProcessorInitializationFlags flags)
            => InternalInitializeGeneratorHandle(header, control, nestedConfiguration, flags);

        [NativeMethod(Name = "audio::InitializeGeneratorHandle", IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void InternalInitializeGeneratorHandle(/*Generator.GeneratorHeader* */void* header, /*ControlHeader*/ void* control, AudioConfiguration* nestedConfiguration, ProcessorInitializationFlags flags);
    }
}
