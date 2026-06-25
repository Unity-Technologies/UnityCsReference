// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IntegerTime;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEngine.Audio
{
    /// <summary>
    /// Factory for instantiating <see cref="GeneratorInstance"/> to be used internally or from other scripts.
    /// </summary>
    /// <remarks>
    /// <see cref="IAudioGenerator"/>s do not own any created <see cref="GeneratorInstance"/> instances,
    /// nor should they try to store these for scripting. Instead, the user of the <see cref="IAudioGenerator"/>
    /// should expose the created <see cref="GeneratorInstance"/> through their own API.
    ///
    /// <para/>
    ///
    /// <see cref="IAudioGenerator"/>s are generally implemented on a <see cref="MonoBehaviour"/> or a <see cref="ScriptableObject"/>
    /// to bind together asset/scene management and audio generation tools with a uniform interface.
    /// You can also directly instantiate a <see cref="GeneratorInstance"/> using a <see cref="ControlContext"/> purely in code.
    ///
    /// <para/>
    ///
    /// If you want to serialize a reference to a <see cref="IAudioGenerator"/> and have an object picker for
    /// asset / component based factories, use the <see cref="IAudioGenerator.Serializable"/> utility to store/load these references.
    /// </remarks>
    /// <seealso cref="AudioSource.generator"/>
    /// <seealso cref="AudioSource.generatorInstance"/>"/>
    [UsedByNativeCode]
    public interface IAudioGenerator : GeneratorInstance.ICapabilities
    {
        /// <summary>
        /// A helper struct that allows you to object select and serialize a reference to any <see cref="UnityEngine.Object"/>,
        /// <see cref="MonoBehaviour"/> or <see cref="ScriptableObject"/> that implements <see cref="IAudioGenerator"/>.
        /// </summary>
        /// <remarks>
        /// Interface references are not directly serializable in user scripts if they are implemented on a <see cref="UnityEngine.Object"/>,
        /// even if using <see cref="SerializeReference"/>.
        /// This helper struct additionally provides a <see cref="PropertyDrawer"/> giving a UI with an object field properly scoped to
        /// <see cref="IAudioGenerator"/> objects.
        /// </remarks>
        [Serializable]
        public struct Serializable
        {
            [SerializeField]
            internal Object Reference;

            /// <summary>
            /// Get and Set the serialized object as a <see cref="IAudioGenerator"/>.
            /// </summary>
            public IAudioGenerator definition
            {
                get => Reference as IAudioGenerator;
                set => Reference = (Object)value;
            }

            /// <summary>
            /// A type-safe helper method that retrieves the internal value.
            /// </summary>
            public T Get<T>()
                where T : Object, IAudioGenerator
            {
                return Reference as T;
            }

            /// <summary>
            /// A type-safe helper method that sets the internal value.
            /// </summary>
            public void Set<T>(T value)
                where T : Object, IAudioGenerator
            {
                Reference = value;
            }

            /// <summary>
            /// Construct this serializable struct with an initial <see cref="IAudioGenerator"/> value.
            /// </summary>
            /// <param name="audioGenerator">The initial value to set.</param>
            /// <exception cref="InvalidCastException">
            /// Thrown if <paramref name="audioGenerator"/> is not a <see cref="UnityEngine.Object"/>.
            /// </exception>
            public Serializable(IAudioGenerator audioGenerator) => Reference = (Object)audioGenerator;
        }

        /// <summary>
        /// Ask this interface to create a <see cref="GeneratorInstance"/>.
        /// </summary>
        /// <param name="context">
        /// The context associated with this <see cref="GeneratorInstance"/>
        /// </param>
        /// <param name="nestedFormat">
        /// If not null, the <see cref="GeneratorInstance"/> shall be created as a nested generator with such format to be used from within another processor.
        /// </param>
        /// <param name="creationParameters">
        /// Initialization parameters passed through.
        /// </param>
        GeneratorInstance CreateInstance(ControlContext context, AudioFormat? nestedFormat, ProcessorInstance.CreationParameters creationParameters);
    }

    /// <summary>
    /// A <see cref="ProcessorInstance"/> that generates audio data.
    /// </summary>
    /// <remarks>
    /// A <see cref="GeneratorInstance"/> can be defined through implementing the <see cref="GeneratorInstance.IControl{TRealtime}"/> and
    /// <see cref="IRealtime"/> interfaces, which defines the control and real-time thread behaviour respectively.
    /// <para/>
    /// To use a generator in a scene, for example with an <see cref="AudioSource"/>, it must have an associated
    /// <see cref="IAudioGenerator"/> that will instantiate it.
    /// In the Unity Audio system they are used with an <see cref="AudioSource"/> by setting the <see cref="AudioSource.generator"/>
    /// property to the associated <see cref="IAudioGenerator"/>.
    /// <para/>
    /// It is also possible to create your own generators through code using a <see cref="ControlContext"/>.
    /// Interacting with an instantiated generator depends on the ownership of the <see cref="GeneratorInstance"/>:
    /// If the generator has been created from an <see cref="AudioSource"/>, you would interact with it
    /// through the instance obtained from <see cref="AudioSource.generatorInstance"/>.
    /// </remarks>
    /// <example>
    /// <code source="../../../../../Tests/EditModeAndPlayModeTests/Audio/Assets/DocCodeExamples/SAP_HowToUseGenerator.cs"/>
    /// </example>
    public unsafe partial struct GeneratorInstance : IEquatable<GeneratorInstance>
    {
        /// <summary>
        /// Describes the runtime behaviour of the <see cref="GeneratorInstance"/>.
        /// These reported values are cached in the beginning and assumed to not change.
        /// </summary>
        /// <remarks>
        /// This must be implemented identically on both the asset / component / offline version in <see cref="IAudioGenerator"/>
        /// and the runtime instance <see cref="IRealtime"/>.
        /// This is so that offline tooling and asset management can reason about the content in advance,
        /// and the engine identically so without depending on loading each other.
        /// </remarks>
        public interface ICapabilities
        {
            /// <summary>
            /// Return true if this <see cref="GeneratorInstance"/> is finite, meaning it has a defined length and will terminate eventually.
            /// </summary>
            /// <remarks>
            /// <see cref="GeneratorInstance"/> do not have to know their length ahead of time but for static content and editor tooling it helps.
            /// </remarks>
            /// <seealso cref="length"/>
            public bool isFinite { get; }

            /// <summary>
            /// Declare whether this <see cref="GeneratorInstance"/> must be treated as a source rendering in real time.
            /// Realtime generators must be processed at the same sampling rate and buffer size as the system they run in.
            /// </summary>
            /// <remarks>
            /// Realtime <see cref="GeneratorInstance"/>s shall return the same output every time they are processed.
            /// Additionally, the system enforces the buffer size of the passed-in <see cref="ChannelBuffer"/> equals the length of the
            /// <see cref="AudioFormat.bufferSize"/> that the <see cref="ControlContext"/> runs in.
            ///
            /// Use cases include hardware devices that cannot be rendered at arbitrary rate, or systematic graphs that render ahead of time.
            /// If you are not sure whether your <see cref="GeneratorInstance"/> is realtime or not, you should set this to false.
            /// </remarks>
            /// <seealso cref="GeneratorInstance.IControl{TRealtime}.Configure"/>.
            public bool isRealtime { get; }

            /// <summary>
            /// If you know your length in advance, you should set it here. Otherwise it's assumed to end at some point in the future,
            /// if <see cref="isFinite"/> is set to true.
            /// </summary>
            /// <remarks>
            /// This value is ignored for <see cref="GeneratorInstance"/>s that are not finite (ie. <see cref="isFinite"/>).
            /// </remarks>
            public DiscreteTime? length { get; }
        }

        /// <summary>
        /// Information on the audio setup of the <see cref="GeneratorInstance"/> passed back to the
        /// instantiator from <see cref="GeneratorInstance.IControl{TRealtime}.Configure"/>.
        /// </summary>
        /// <seealso cref="ControlContext.GetConfiguration(GeneratorInstance)"/>
        public readonly struct Setup
        {
            /// <summary>
            /// Declare the <see cref="AudioSpeakerMode"/> of this <see cref="GeneratorInstance"/> and by extension
            /// the number of channels this <see cref="GeneratorInstance"/> will use.
            /// </summary>
            /// <remarks>
            /// This directly determines the size of the <see cref="ChannelBuffer"/> passed to the <see cref="Process"/> method.
            /// </remarks>
            public readonly AudioSpeakerMode speakerMode;

            /// <summary>
            /// Declare the sample rate the output of this <see cref="GeneratorInstance"/> will be played at.
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
            /// Create a new generator setup, deriving values from <paramref name="fromFormat"/>.
            /// </summary>
            public Setup(in AudioFormat fromFormat)
                : this(fromFormat.speakerMode, fromFormat.sampleRate)
            {

            }
        }

        /// <summary>
        /// Represents optional or additional metadata about the <see cref="GeneratorInstance"/>.
        /// </summary>
        public struct Properties
        {
            // Speakermode etc., metadata
            byte m_Reserved;
        }

        /// <summary>
        /// The configuration of a specific instance of a <see cref="GeneratorInstance"/>.
        /// </summary>
        /// <remarks>
        /// This is mainly self-reported from <see cref="ICapabilities"/> and the <see cref="GeneratorInstance.IControl{TRealtime}.Configure"/> method.
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
            /// Information on the audio setup of a <see cref="GeneratorInstance"/> instance,
            /// required to be provided to the instantiator in a <see cref="GeneratorInstance.IControl{TRealtime}.Configure"/> call.
            /// </summary>
            public Setup setup => Setup;
            /// <summary>
            /// Optional or additional metadata about the <see cref="GeneratorInstance"/> instance.
            /// </summary>
            public Properties properties => Properties;
            /// <summary>
            /// Declares whether this <see cref="GeneratorInstance"/> instance has a defined length and will eventually end.
            /// </summary>
            /// <seealso cref="ICapabilities.isFinite"/>
            public bool isFinite => IsFinite;
            /// <summary>
            /// Whether this <see cref="GeneratorInstance"/> instance must be treated as a source rendering in real time.
            /// </summary>
            /// <seealso cref="ICapabilities.isRealtime"/>
            public bool isRealtime => IsRealtime;
            /// <summary>
            /// Declares the length in seconds of this <see cref="GeneratorInstance"/> instance, if known.
            /// </summary>
            /// <seealso cref="ICapabilities.length"/>
            public DiscreteTime? length => HasKnownLength ? ReportedLength : null;

            static internal Int64 FramesAndSampleRateToDiscreteTimeTicks(Int64 lengthFrames, UInt32 sampleRate)
            {
                switch (sampleRate)
                {
                    case 8000: return lengthFrames * DiscreteTime.Tick8Khz;
                    case 16000: return lengthFrames * DiscreteTime.Tick16Khz;
                    case 22050: return lengthFrames * DiscreteTime.Tick22Khz;
                    case 44100: return lengthFrames * DiscreteTime.Tick44Khz;
                    case 48000: return lengthFrames * DiscreteTime.Tick48Khz;
                    case 88200: return lengthFrames * DiscreteTime.Tick88Khz;
                    case 96000: return lengthFrames * DiscreteTime.Tick96Khz;
                    case 192000: return lengthFrames * DiscreteTime.Tick192Khz;
                    default:
                        // For non-standard sample rates, we can still calculate the ticks by using the ratio of ticks per second to the sample rate.
                        return (lengthFrames * DiscreteTime.TicksPerSecond) / sampleRate;
                }
            }
        }

        /// <summary>
        /// The result returned from a <see cref="IRealtime.Process"/> call.
        /// </summary>
        /// <remarks>
        /// This primarily contains the amount of frames actually written into the passed-in <see cref="ChannelBuffer"/>,
        /// and the status of the generator (eg. whether it has finished or not).
        /// </remarks>
        /// <seealso cref="RealtimeContext.Process"/>
        public ref struct Result
        {
            internal enum Status : Int32
            {
                Success = 0,
                Finished,
                Loading,    // Clip still loading async, outputting silence
                LoadFailed  // Clip failed to load (permanent)
            }

            internal int m_ProcessedFrames;
            internal Status m_Status;

            /// <summary>
            /// Number of frames processed by the <see cref="GeneratorInstance"/> in <see cref="RealtimeContext.Process"/>.
            /// </summary>
            public int processedFrames => m_ProcessedFrames;

            /// <summary>
            /// If this is true, the <see cref="GeneratorInstance"/> has finished generating audio and won't produce any more frames.
            /// </summary>
            /// <remarks>
            /// There may still be a range of valid <see cref="Result.processedFrames"/> values when this is true,
            /// for example if the <see cref="GeneratorInstance"/> finishes in the middle of a buffer.
            /// </remarks>
            public bool isFinished() => m_Status == Status.Finished;

            /// <summary>
            /// If this is true, the <see cref="GeneratorInstance"/> is still loading its backing audio clip asynchronously.
            /// </summary>
            /// <remarks>
            /// When loading, the generator outputs silence. Once loading completes, it will start producing audio
            /// or report <see cref="didLoadFail"/> if the load failed.
            /// </remarks>
            public bool isLoading() => m_Status == Status.Loading;

            /// <summary>
            /// If this is true, the backing audio clip failed to load and the generator cannot produce audio.
            /// </summary>
            public bool didLoadFail() => m_Status == Status.LoadFailed;

            /// <summary>
            /// Creates a new <see cref="GeneratorInstance.Result"/> from a number of frames processed.
            /// </summary>
            /// <seealso cref="Result.Finished(int)"/>
            public static implicit operator Result(int processedFrames)
            {
                return new Result { m_ProcessedFrames = processedFrames };
            }

            /// <summary>
            /// Creates a new <see cref="GeneratorInstance.Result"/> that indicates the generator has finished from this point on,
            /// having delivered <see cref="Result.processedFrames"/> additionally.
            /// </summary>
            /// <param name="processedFrames">
            /// This can just be 0 if the generator finishes before processing any frames.
            /// </param>
            public static Result Finished(int processedFrames)
            {
                return new Result { m_ProcessedFrames = processedFrames, m_Status = Status.Finished };
            }
        }

        /// <summary>
        /// Additional arguments passed to the <see cref="RealtimeContext.Process"/> method.
        /// </summary>
        public ref struct Arguments
        {
            /// <summary>
            /// If <see cref="GeneratorInstance.IDefinition.IsRealtime"/> is set, this field contains the aggregate playback speed of this source.
            /// </summary>
            internal float Speed;
        }

        /// <summary>
        /// The control interface an implementation of a <see cref="GeneratorInstance"/> must implement on a struct to be fully formed.
        /// </summary>
        /// <remarks>
        /// The control side of a <see cref="ProcessorInstance"/> receives various callbacks from a <see cref="ControlContext"/>
        /// from the logical control thread.
        /// You can annotate this with <see cref="Unity.Burst.BurstCompileAttribute"/> to have it compiled with Burst.
        /// </remarks>
        /// <typeparam name="TRealtime">The tandem processing counterpart.</typeparam>
        /// <seealso cref="ProcessorInstance.IControl{TRealtime}"/>
        [JobProducerType(typeof(IGeneratorControlExtensions.JobStruct<,>))]
        public interface IControl<TRealtime> : ProcessorInstance.IControl<TRealtime>
            where TRealtime : unmanaged, ProcessorInstance.IRealtime
        {
            /// <summary>
            /// Called to configure the <see cref="GeneratorInstance"/> before it is used, and when the audio system reconfigures.
            /// A default implementation will set the <paramref name="setup"/> and <paramref name="properties"/> based on the
            /// <paramref name="format"/>.
            /// </summary>
            /// <remarks>
            /// </remarks>
            /// <param name="realtime">
            /// In case of reconfiguration, the <paramref name="realtime"/> is temporarily suspended from processing,
            /// and you can safely modify its properties.
            /// </param>
            /// <param name="context">
            /// The <see cref="ControlContext"/> associated with this call.
            /// </param>
            /// <param name="format">
            /// The format you're being suggested to use, for optimal performance.
            /// You must initialize <paramref name="setup"/> to either this or a value of your choosing.
            /// </param>
            /// <param name="properties">Additional properties you can set, or leave as default.</param>
            /// <param name="setup">
            /// Out parameter where you must configure the sample rate and <see cref="AudioSpeakerMode"/> this <see cref="GeneratorInstance"/> must
            /// run at.
            /// The system enforces this to be true for you, and anyone using this <see cref="GeneratorInstance"/> will handle conversion
            /// to another <see cref="AudioFormat"/> if needed.
            /// </param>
            /// <seealso cref="ControlContext.GetConfiguration(GeneratorInstance)"/>
            /// <seealso cref="GeneratorInstance.Configuration"/>
            public void Configure(
                ControlContext context,
                ref TRealtime realtime,
                in AudioFormat format,
                out Setup setup,
                ref Properties properties
            );
        }

        /// <summary>
        /// The processing interface an implementation of a <see cref="GeneratorInstance"/> must implement on a struct to be fully formed.
        /// </summary>
        /// <remarks>
        /// The processing side of a <see cref="ProcessorInstance"/> receives various callbacks from a <see cref="RealtimeContext"/>
        /// from the logical processing thread.
        /// You can annotate this with <see cref="Unity.Burst.BurstCompileAttribute"/> to have it compiled with Burst.
        /// </remarks>
        /// <seealso cref="ProcessorInstance.IRealtime"/>
        [JobProducerType(typeof(IGeneratorProcessorExtensions.JobStruct<>))]
        public interface IRealtime : Audio.ProcessorInstance.IRealtime, ICapabilities
        {
            /// <summary>
            /// Called when you're asked to produce the next segment of audio into <paramref name="buffer"/>.
            /// </summary>
            /// <param name="context">
            /// The <see cref="RealtimeContext"/> associated with this call.
            /// Use this to process any nested <see cref="ProcessorInstance"/>s or query/return data together with <paramref name="pipe"/>.
            /// </param>
            /// <param name="pipe">Cross-thread communications pipe.</param>
            /// <param name="buffer">The buffer your <see cref="GeneratorInstance"/> will put its processing result into.</param>
            /// <param name="args">Addtional arguments.</param>
            /// <returns>
            /// A <see cref="Result"/> struct indicating amongst other things how many frames were actually written into <paramref name="buffer"/>.
            /// </returns>
            /// <seealso cref="RealtimeContext.Process"/>
            public Result Process(in RealtimeContext context, ProcessorInstance.Pipe pipe, ChannelBuffer buffer, Arguments args);
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
        /// Convert this <see cref="GeneratorInstance"/> to its more general <see cref="ProcessorInstance"/> representation.
        /// </summary>
        /// <see cref="ProcessorInstance"/>s are unowned and can safely handed out to other users.
        public static implicit operator ProcessorInstance(in GeneratorInstance generatorInstance) => generatorInstance.m_ProcessorInstance;

        /// <summary>
        /// Checks if this instance equals another.
        /// </summary>
        /// <param name="other">The other instance for comparing.</param>
        /// <returns>True if the given instance is equal to this, otherwise, false.</returns>
        public bool Equals(GeneratorInstance other)
        {
            return m_ProcessorInstance.Equals(other.m_ProcessorInstance);
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

            return obj is GeneratorInstance instance && Equals(instance);
        }

        /// <summary>
        /// Checks if two instances are equal.
        /// </summary>
        /// <param name="a">The first instance for comparing.</param>
        /// <param name="b">The second instance for comparing.</param>
        /// <returns>True if the two given instances are equal, otherwise, false.</returns>
        public static bool operator ==(GeneratorInstance a, GeneratorInstance b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Checks if two instances are not equal.
        /// </summary>
        /// <param name="a">The first instance for comparing.</param>
        /// <param name="b">The second instance for comparing.</param>
        /// <returns>True if the two given instances are not equal, otherwise, false.</returns>
        public static bool operator !=(GeneratorInstance a, GeneratorInstance b)
        {
            return !a.Equals(b);
        }

        /// <summary>
        /// Retrieves a hash code based on this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return m_ProcessorInstance.GetHashCode();
        }

        internal GeneratorInstance(GeneratorHeader* header)
            => m_ProcessorInstance = new ProcessorInstance(header->Processor.DualThreadHandle, &header->Processor);

        internal readonly ProcessorInstance m_ProcessorInstance;
    }

    #region job-types

    [EditorBrowsable(EditorBrowsableState.Never)]
    static class IGeneratorControlExtensions
    {
        internal struct JobStruct<TUserControl, TUserProcessor>
            where TUserControl : unmanaged, GeneratorInstance.IControl<TUserProcessor>
            where TUserProcessor : unmanaged, GeneratorInstance.IRealtime
        {
            internal struct ControlStorage
            {
                public IGeneratorProcessorExtensions.JobStruct<TUserProcessor>.Storage HeaderAndProcessor;
                public TUserControl UserControl;
            }

            internal static readonly BurstLike.SharedStatic<IntPtr> jobReflectionData = BurstLike.SharedStatic<IntPtr>.GetOrCreate<JobStruct<TUserControl, TUserProcessor>>();

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
                            new AudioFormat(args->Now),
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
            where TUserGenerator : unmanaged, GeneratorInstance.IRealtime
            where TUserControl : unmanaged, GeneratorInstance.IControl<TUserGenerator>
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
            internal RealtimeContext* Context;
            internal float* AudioBuffer;
            internal DualThreadHandle Self;
            /// <summary>
            /// The total size of <see cref="AudioBuffer"/> is <see cref="FrameCount"/> times the amount of channels this generator has declared (<see cref="GeneratorInstance.Configuration.setup.channelCount"/>).
            /// </summary>
            internal int FrameCount;
            internal GeneratorInstance.Arguments GeneratorArguments;
            internal GeneratorInstance.Result Result;
        }

        internal struct JobStruct<TUserProcessor>
            where TUserProcessor : unmanaged, GeneratorInstance.IRealtime
        {
            internal struct Storage
            {
                public GeneratorInstance.GeneratorHeader Header;
                public TUserProcessor UserProcessor;
            }

            internal static readonly BurstLike.SharedStatic<IntPtr> jobReflectionData = BurstLike.SharedStatic<IntPtr>.GetOrCreate<JobStruct<TUserProcessor>>();

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
            where TUserProcessor : unmanaged, GeneratorInstance.IRealtime
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
        internal static unsafe void InstantiateGeneratorFromObject(Object generatorObjectDefinition, ref ControlHeader control, out GeneratorInstance runtimeHandle)
        {
            if (generatorObjectDefinition is IAudioGenerator definition)
            {
                fixed (ControlHeader* pResources = &control)
                {
                    var context = new ControlContext(pResources);
                    runtimeHandle = definition.CreateInstance(context, null,default);

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
                Debug.LogError($"Trying to play object {generatorObjectDefinition}, but it doesn't implement {nameof(IAudioGenerator)}");
            }
        }

        internal static unsafe DualThreadHandle InitializeGeneratorHandle<TRealtime, TControl>(
            ref IGeneratorControlExtensions.JobStruct<TControl, TRealtime>.ControlStorage storage,
            ControlHeader* control,
            AudioConfiguration* nestedConfiguration,
            ProcessorInstance.InitializationFlags flags
        )
            where TRealtime : unmanaged, GeneratorInstance.IRealtime
            where TControl : unmanaged, GeneratorInstance.IControl<TRealtime>
        {
            fixed (GeneratorInstance.GeneratorHeader* headerPtr = &storage.HeaderAndProcessor.Header)
                return InternalInitializeGeneratorHandle(headerPtr, sizeof(IGeneratorControlExtensions.JobStruct<TControl, TRealtime>.ControlStorage), control, nestedConfiguration, flags);
        }

        [NativeMethod(Name = "audio::InitializeGeneratorHandle", IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe DualThreadHandle InternalInitializeGeneratorHandle(/*Generator.GeneratorHeader* */void* header, int tailSize, /*ControlHeader*/ void* control, AudioConfiguration* nestedConfiguration, ProcessorInstance.InitializationFlags flags);
    }
}
