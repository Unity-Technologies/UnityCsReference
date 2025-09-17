// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using Unity.Burst;
using Unity.IntegerTime;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using static Unity.Collections.LowLevel.Unsafe.BurstLike;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEngine.Audio
{
    /// <summary>
    /// Factory for instantiating <see cref="Generator"/> to be used internally or from other scripts.
    /// </summary>
    /// <remarks>
    /// <see cref="IGeneratorDefinition"/>s do not own any created <see cref="Generator"/> instances,
    /// nor should they try to store these for scripting. Instead, the user of the <see cref="IGeneratorDefinition"/>
    /// should expose the created <see cref="Generator"/> through their own API.
    ///
    /// <para/>
    /// 
    /// <see cref="IGeneratorDefinition"/>s are generally implemented on a <see cref="MonoBehaviour"/> or a <see cref="ScriptableObject"/>
    /// to bind together asset/scene management and audio generation tools with a uniform interface.
    /// You can also directly instantiate a <see cref="Generator"/> using a <see cref="ControlContext"/> purely in code.
    ///
    /// <para/>
    ///
    /// If you want to serialize a reference to a <see cref="IGeneratorDefinition"/> and have an object picker for
    /// asset / component based factories, use the <see cref="IGeneratorDefinition.Serializable"/> utility to store/load these references.
    /// </remarks>
    /// <seealso cref="AudioSource.generatorDefinition"/>
    /// <seealso cref="AudioSource.generatorHandle"/>"/>
    [UsedByNativeCode]
    public interface IGeneratorDefinition : Generator.ICapabilities
    {
        /// <summary>
        /// A helper struct that allows you to object select and serialize a reference to any <see cref="UnityEngine.Object"/>,
        /// <see cref="MonoBehaviour"/> or <see cref="ScriptableObject"/> that implements <see cref="IGeneratorDefinition"/>.
        /// </summary>
        /// <remarks>
        /// Interface references are not directly serializable in user scripts if they are implemented on a <see cref="UnityEngine.Object"/>,
        /// even if using <see cref="SerializeReference"/>.
        /// This helper struct additionally provides a <see cref="PropertyDrawer"/> giving a UI with an object field properly scoped to
        /// <see cref="IGeneratorDefinition"/> objects.
        /// </remarks>
        [Serializable]
        public struct Serializable
        {
            [SerializeField]
            internal Object Reference;

            /// <summary>
            /// Get and Set the serialized object as a <see cref="IGeneratorDefinition"/>.
            /// </summary>
            public IGeneratorDefinition definition
            {
                get => Reference as IGeneratorDefinition;
                set => Reference = (Object)value;
            }

            /// <summary>
            /// A type-safe helper method that retrieves the internal value.
            /// </summary>
            public T Get<T>()
                where T : Object, IGeneratorDefinition
            {
                return Reference as T;
            }

            /// <summary>
            /// A type-safe helper method that sets the internal value.
            /// </summary>
            public void Set<T>(T value)
                where T : Object, IGeneratorDefinition
            {
                Reference = value;
            }

            /// <summary>
            /// Construct this serializable struct with an initial <see cref="IGeneratorDefinition"/> value.
            /// </summary>
            /// <param name="generatorDefinition">The initial value to set.</param>
            /// <exception cref="InvalidCastException">
            /// Thrown if <paramref name="generatorDefinition"/> is not a <see cref="UnityEngine.Object"/>.
            /// </exception>
            public Serializable(IGeneratorDefinition generatorDefinition) => Reference = (Object)generatorDefinition;
        }

        /// <summary>
        /// Ask this interface to instantiate a runtime <see cref="Generator"/> instance.
        /// </summary>
        /// <param name="context">
        /// The context associated with this <see cref="Generator"/>
        /// </param>
        /// <param name="nestedConfiguration">
        /// If not null, the <see cref="Generator"/> shall be created as a nested generator with such configuration to be used from within another processor.
        /// </param>
        /// <param name="creationParameters">
        /// Initialization parameters passed through.
        /// </param>
        Generator CreateRuntime(ControlContext context, DSPConfiguration? nestedConfiguration, ControlContext.ProcessorCreationParameters creationParameters);
    }

    /// <summary>
    /// A <see cref="Audio.Processor"/> that generates audio data.
    /// </summary>
    /// <remarks>
    /// A <see cref="Generator"/> can be defined through implementing the <see cref="Audio.Generator.IControl{TProcessor}"/> and
    /// <see cref="Audio.Generator.IProcessor"/> interfaces, which defines the control and processing thread behaviour respectively.
    /// <para/>
    /// To use a generator in a scene, for example with an <see cref="AudioSource"/>, it must have an associated
    /// <see cref="IGeneratorDefinition"/> that will instantiate it.
    /// In the Unity Audio system they are used with an <see cref="AudioSource"/> by setting the <see cref="AudioSource.generatorDefinition"/>
    /// property to the associated <see cref="IGeneratorDefinition"/>.
    /// <para/>
    /// It is also possible to create your own generators through code using a <see cref="ControlContext"/>.
    /// Interacting with an instantiated generator depends on the ownership of the <see cref="Generator"/>:
    /// If the generator has been created from an <see cref="AudioSource"/>, you would interact with it
    /// through the handle obtained from <see cref="AudioSource.generatorHandle"/>.
    /// </remarks>
    /// <example>
    /// <code source="../../../../../Tests/EditModeAndPlayModeTests/Audio/Assets/DocCodeExamples/SAP_HowToUseGenerator.cs"/>
    /// </example>
    public unsafe struct Generator
    {
        /// <summary>
        /// Describes the runtime behaviour of the <see cref="Generator"/>.
        /// These reported values are cached in the beginning and assumed to not change.
        /// </summary>
        /// <remarks>
        /// This must be implemented identically on both the asset / component / offline version in <see cref="IGeneratorDefinition"/>
        /// and the runtime instance <see cref="Generator.IProcessor"/>.
        /// This is so that offline tooling and asset management can reason about the content in advance,
        /// and the engine identically so without depending on loading each other.
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
        /// Information on the audio setup of the <see cref="Generator"/> passed back to the
        /// instantiator from <see cref="Audio.Generator.IControl{TProcessor}.Configure"/>.
        /// </summary>
        /// <seealso cref="ControlContext.GetConfiguration(Generator)"/>
        public readonly struct Setup
        {
            /// <summary>
            /// Declare the <see cref="AudioSpeakerMode"/> of this <see cref="Generator"/> and by extension
            /// the number of channels this <see cref="Generator"/> will use.
            /// </summary>
            /// <remarks>
            /// This directly determines the size of the <see cref="ChannelBuffer"/> passed to the <see cref="Process"/> method.
            /// </remarks>
            public readonly AudioSpeakerMode speakerMode;

            /// <summary>
            /// Declare the sampling rate the output of this <see cref="Generator"/> will be played at.
            /// </summary>
            public readonly int sampleRate;

            /// <summary>
            /// Create a new generator setup.
            /// </summary>
            public Setup(AudioSpeakerMode speakerMode, int sampleRate)
            {
                this.speakerMode = speakerMode;
                this.sampleRate = sampleRate;
            }

            /// <summary>
            /// Create a new generator setup, deriving values from <paramref name="fromConfiguration"/>.
            /// </summary>
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

        /// <summary>
        /// The configuration of a specific instance of a <see cref="Generator"/>.
        /// </summary>
        /// <remarks>
        /// This is mainly self-reported from <see cref="ICapabilities"/> and the <see cref="Audio.Generator.IControl{TProcessor}.Configure"/> method.
        /// </remarks>
        public struct Configuration
        {
            internal Setup Setup;
            internal Properties Properties;
            internal DiscreteTime ReportedLength;
            internal bool IsFinite;
            internal bool IsRealtime;
            internal bool HasKnownLength;

            /// <summary>
            /// Information on the audio setup of a <see cref="Generator"/> instance,
            /// required to be provided to the instantiator in a <see cref="Audio.Generator.IControl{TProcessor}.Configure"/> call.
            /// </summary>
            public Setup setup => Setup;
            /// <summary>
            /// Optional or additional metadata about the <see cref="Generator"/> instance.
            /// </summary>
            public Properties properties => Properties;
            /// <summary>
            /// Declares whether this <see cref="Generator"/> instance has a defined length and will eventually end.
            /// </summary>
            /// <seealso cref="ICapabilities.isFinite"/>
            public bool isFinite => IsFinite;
            /// <summary>
            /// Whether this <see cref="Generator"/> instance must be treated as a source rendering in real time.
            /// </summary>
            /// <seealso cref="ICapabilities.isRealtime"/>
            public bool isRealtime => IsRealtime;
            /// <summary>
            /// Declares the length in seconds of this <see cref="Generator"/> instance, if known.
            /// </summary>
            /// <seealso cref="ICapabilities.length"/>
            public DiscreteTime? length => HasKnownLength ? ReportedLength : null;
        }

        /// <summary>
        /// The result returned from a <see cref="Generator.IProcessor.Process"/> call.
        /// </summary>
        /// <remarks>
        /// This primarily contains the amount of frames actually written into the passed-in <see cref="ChannelBuffer"/>.
        /// </remarks>
        /// <seealso cref="Generator.Process"/>
        public ref struct Result
        {
            internal int m_ProcessedFrames;

            /// <summary>
            /// Number of frames processed by the <see cref="Generator"/> in <see cref="Generator.Processor"/>.
            /// </summary>
            public int processedFrames => m_ProcessedFrames;

            /// <summary>
            /// Creates a new <see cref="Generator.Result"/> from a number of frames processed.
            /// </summary>
            public static implicit operator Result(int processedFrames)
            {
                return new Result { m_ProcessedFrames = processedFrames };
            }
        }

        /// <summary>
        /// Additional arguments passed to the <see cref="Generator.Process"/> method.
        /// </summary>
        public ref struct Arguments
        {
            /// <summary>
            /// If <see cref="Generator.IDefinition.IsRealtime"/> is set, this field contains the aggregate playback speed of this source.
            /// </summary>
            internal float Speed;
        }

        /// <summary>
        /// The control interface an implementation of a <see cref="Generator"/> must implement on a struct to be fully formed.
        /// </summary>
        /// <remarks>
        /// The control side of a <see cref="Audio.Processor"/> receives various callbacks from a <see cref="ControlContext"/>
        /// from the logical control thread.
        /// You can annotate this with <see cref="Unity.Burst.BurstCompileAttribute"/> to have it compiled with Burst.
        /// </remarks>
        /// <typeparam name="TProcessor">The tandem processing counterpart.</typeparam>
        /// <seealso cref="Audio.Processor.IControl{TProcessor}"/>
        [JobProducerType(typeof(IGeneratorControlExtensions.JobStruct<,>))]
        public interface IControl<TProcessor> : Processor.IControl<TProcessor>
            where TProcessor : unmanaged, Processor.IProcessor
        {
            /// <summary>
            /// Called to configure the <see cref="Generator"/> before it is used, and when the audio system reconfigures.
            /// A default implementation will set the <paramref name="setup"/> and <paramref name="properties"/> based on the
            /// <paramref name="configuration"/>.
            /// </summary>
            /// <remarks>
            /// </remarks>
            /// <param name="processor">
            /// In case of reconfiguration, the <paramref name="processor"/> is temporarily suspended from processing,
            /// and you can safely modify its properties.
            /// </param>
            /// <param name="context">
            /// The <see cref="ControlContext"/> associated with this call.
            /// </param>
            /// <param name="configuration">
            /// The configuration you're being suggested to use, for optimal performance.
            /// You must initialize <paramref name="setup"/> to either this or a value of your choosing.
            /// </param>
            /// <param name="properties">Additional properties you can set, or leave as default.</param>
            /// <param name="setup">
            /// Out parameter where you must configure the sample rate and <see cref="AudioSpeakerMode"/> this <see cref="Generator"/> must
            /// run at.
            /// The system enforces this to be true for you, and anyone using this <see cref="Generator"/> will handle conversion
            /// to another <see cref="DSPConfiguration"/> if needed.
            /// </param>
            /// <seealso cref="ControlContext.GetConfiguration(Generator)"/>
            /// <seealso cref="Generator.Configuration"/>
            public void Configure(
                ControlContext context,
                ref TProcessor processor,
                in DSPConfiguration configuration,
                out Setup setup,
                ref Properties properties
            );
        }

        /// <summary>
        /// The processing interface an implementation of a <see cref="Generator"/> must implement on a struct to be fully formed.
        /// </summary>
        /// <remarks>
        /// The processing side of a <see cref="Audio.Processor"/> receives various callbacks from a <see cref="ProcessingContext"/>
        /// from the logical processing thread.
        /// You can annotate this with <see cref="Unity.Burst.BurstCompileAttribute"/> to have it compiled with Burst.
        /// </remarks>
        /// <seealso cref="Processor.IProcessor"/>
        [JobProducerType(typeof(IGeneratorProcessorExtensions.JobStruct<>))]
        public interface IProcessor : Audio.Processor.IProcessor, ICapabilities
        {
            /// <summary>
            /// Called when you're asked to produce the next segment of audio into <paramref name="buffer"/>.
            /// </summary>
            /// <param name="context">
            /// The <see cref="ProcessingContext"/> associated with this call.
            /// Use this to process any nested <see cref="Audio.Processor"/>s or query/return data together with <paramref name="pipe"/>.
            /// </param>
            /// <param name="pipe">Cross-thread communications pipe.</param>
            /// <param name="buffer">The buffer your <see cref="Generator"/> will put its processing result into.</param>
            /// <param name="args">Addtional arguments.</param>
            /// <returns>
            /// A <see cref="Result"/> struct indicating amongst other things how many frames were actually written into <paramref name="buffer"/>.
            /// </returns>
            /// <seealso cref="Generator.Process"/>
            public Result Process(in ProcessingContext context, Processor.Pipe pipe, ChannelBuffer buffer, Arguments args);
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

        /// <summary>
        /// Manually process this particular <see cref="Generator"/>.
        /// </summary>
        /// <remarks>
        /// In most use cases, you would not call this directly, but rather have the audio system call it for you.
        /// If you are yourself nesting a <see cref="Generator"/> inside another <see cref="Audio.Processor"/>, you would call this.
        /// </remarks>
        /// <param name="context">
        /// The <see cref="ProcessingContext"/> associated with this call. You either get this from your own callback,
        /// or from <see cref="ControlContext.Manual.BeginMix"/>.
        /// </param>
        /// <param name="args">
        /// Additional arguments passed along, which can be default-initialized.</param>
        /// <param name="buffer">
        /// The buffer the <see cref="Generator"/> will put its processing result into.
        /// </param>
        /// <returns>
        /// A <see cref="Result"/> struct indicating amongst other things how many frames were actually written into <paramref name="buffer"/>.
        /// </returns>
        /// <seealso cref="Generator.IProcessor.Process"/>
        public Result Process(in ProcessingContext context, ChannelBuffer buffer, Arguments args)
        {
            ScriptableProcessorBindings.ValidateCanProcess(Processor.Handle, context);

            fixed (float* writeBuffer = buffer.Buffer)
            {
                fixed (ProcessingContext* pContext = &context)
                {
                    var processArguments = new IGeneratorProcessorExtensions.ProcessArguments
                    {
                        AudioBuffer = writeBuffer,
                        Context = pContext,
                        FrameCount = buffer.frameCount
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
        /// Nested <see cref="Generator"/>s must be manually configured,
        /// and this call is only valid on nested <see cref="Audio.Processor"/>s.
        /// </remarks>
        /// <seealso cref="ControlContext.AllocateGenerator"/>
        /// <seealso cref="Processor.Configure"/>
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
        /// This is only valid on nested <see cref="Audio.Processor"/>s.
        /// You must always update any nested <see cref="Audio.Processor"/>s you have created.
        /// </remarks>
        /// <seealso cref="ControlContext.AllocateGenerator"/>
        /// <seealso cref="Generator.Configure"/>
        public void Update(ControlContext context)
        {
            ScriptableProcessorBindings.PerformRecursiveUpdate(Processor.Handle, context.Header);
        }

        /// <summary>
        /// Convert this <see cref="Generator"/> to its more general <see cref="Audio.Processor"/> representation.
        /// </summary>
        /// <see cref="Audio.Processor"/>s are unowned and can safely handed out to other users.
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
