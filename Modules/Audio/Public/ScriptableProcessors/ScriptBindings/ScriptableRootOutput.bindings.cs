// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using Unity.Burst;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Bindings;
using static Unity.Collections.LowLevel.Unsafe.BurstLike;

namespace UnityEngine.Audio
{
    /// <summary>
    /// An audio <see cref="Processor"/> with extra callbacks intended to allow scheduling different management and compute work
    /// over the course of a mixframe. Finally, additional audio can be appended to the final audio output.
    /// </summary>
    /// <remarks>
    /// Usage of this is generally very low level, and intended to provide integration points for internal or external audio middleware,
    /// that share the same input/output resources as the host audio system.
    /// Create instances of these using <see cref="ControlContext.AllocateRootOutput"/>.
    /// </remarks>
    public unsafe struct RootOutput
    {
        /// <summary>
        /// The control interface an implementation of a <see cref="RootOutput"/> must implement on a struct to be fully formed.
        /// </summary>
        /// <remarks>
        /// The control side of a <see cref="Audio.Processor"/> receives various callbacks from a <see cref="ControlContext"/>
        /// from the logical control thread.
        /// You can annotate this with <see cref="Unity.Burst.BurstCompileAttribute"/> to have it compiled with Burst.
        /// </remarks>
        /// <typeparam name="TProcessor">The tandem processing counterpart.</typeparam>
        /// <seealso cref="Audio.Processor.IControl{TProcessor}"/>
        [JobProducerType(typeof(IRootOutputControlExtensions.JobStruct<,>))]
        public interface IControl<TProcessor> : Processor.IControl<TProcessor>
            where TProcessor : unmanaged, Processor.IProcessor
        {
            /// <summary>
            /// Called to configure the <see cref="RootOutput"/> before it is used, and when the audio system reconfigures.
            /// </summary>
            /// <returns>
            /// Optionally you can return a non-default <see cref="JobHandle"/> allowing you to do heavier configuration/setup on a worker thread.
            /// </returns>
            /// <param name="processor">
            /// The processor instance that will be used in the processing thread.
            /// In case of reconfiguration, the <paramref name="processor"/> is temporarily suspended from processing,
            /// and you can safely modify its properties.
            /// </param>
            /// <param name="configuration">
            /// The updated system configuration. This is the same as <see cref="ControlContext"/> runs with.
            /// </param>
            /// <seealso cref="AudioSettings.Reset"/>
            /// <param name="context">The context this <see cref="RootOutput"/> is being configured from.</param>
            public JobHandle Configure(ControlContext context, ref TProcessor processor, in DSPConfiguration configuration);
        }

        /// <summary>
        /// The processing interface an implementation of a <see cref="RootOutput"/> must implement on a struct to be fully formed.
        /// </summary>
        /// <remarks>
        /// The processing side of a <see cref="Audio.Processor"/> receives various callbacks from a <see cref="ProcessingContext"/>
        /// from the logical processing thread.
        /// You can annotate this with <see cref="Unity.Burst.BurstCompileAttribute"/> to have it compiled with Burst.
        /// </remarks>
        /// <seealso cref="Processor.IProcessor"/>
        [JobProducerType(typeof(IRootOutputProcessorExtensions.JobStruct<>))]
        public interface IProcessor : Audio.Processor.IProcessor
        {
            /// <summary>
            /// Perform any tasks necessary before any other resource managed by this <see cref="RootOutput"/> is being used by anything else.
            /// </summary>
            /// <remarks>
            /// For instance, a <see cref="Generator"/> hardware input may sample its data here once, and that would then be available afterwards
            /// without changing for this mixing update.
            /// </remarks>
            /// <returns>
            /// Optionally an async dependency that will be fed into every other <see cref="RootOutput.IProcessor.Process"/>
            /// </returns>
            public JobHandle EarlyProcessing(in ProcessingContext context, Processor.Pipe pipe);

            /// <summary>
            /// Schedule your main body of work in parallel to everything else.
            /// If you are using jobs, you are required to manually keep track of dependencies and finish them later.
            /// </summary>
            /// <param name="input">
            /// The complete dependency of all other <see cref="EarlyProcessing"/> for all other <see cref="Audio.Processor"/>s.
            /// If you are using other/foreign scriptable <see cref="Audio.Processor"/>s, your work must depend on or complete this parameter.
            /// </param>
            public void Process(in ProcessingContext context, Processor.Pipe pipe, JobHandle input);

            /// <summary>
            /// Return the main result of your computation to the system in <paramref name="output"/>.
            /// </summary>
            /// <param name="output">
            /// A buffer with the same size as the <see cref="DSPConfiguration"/> passed into <see cref="RootOutput.IControl{TProcessor}.Configure"/>.
            /// </param>
            /// <remarks>
            /// The contents written to <paramref name="output"/> will be additively added to the main audio output.
            /// </remarks>
            public void EndProcessing(in ProcessingContext context, Processor.Pipe pipe, ChannelBuffer output);

            /// <summary>
            /// Called potentially after a sequence of <see cref="Processor.IProcessor.Update"/>,
            /// when a <see cref="Audio.Processor"/> has been disposed from eg. <see cref="ControlContext.Destroy(RootOutput)"/>.
            /// </summary>
            /// <remarks>
            /// This is a chance to sync any work done or ongoing before leaving the processing thread.
            /// This will always be called after <see cref="EndProcessing"/>.
            /// </remarks>
            public void RemovedFromProcessing();
        }

        /// <summary>
        /// Convert this <see cref="RootOutput"/> to its more general <see cref="Audio.Processor"/> representation.
        /// </summary>
        /// <see cref="Audio.Processor"/>s are unowned and can safely handed out to other users.
        public static implicit operator Processor(in RootOutput root) => root.Processor;

        internal RootOutput(ProcessorHeader* header)
            => Processor = new Processor(header->DualThreadHandle, header);

        internal readonly Processor Processor;
    }

    #region job-types

    [EditorBrowsable(EditorBrowsableState.Never)]
    static class IRootOutputControlExtensions
    {
        internal struct JobStruct<TUserControl, TUserProcessor>
            where TUserControl : unmanaged, RootOutput.IControl<TUserProcessor>
            where TUserProcessor : unmanaged, RootOutput.IProcessor
        {
            internal struct ControlStorage
            {
                public IRootOutputProcessorExtensions.JobStruct<TUserProcessor>.Storage HeaderAndProcessor;
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

                        storage.UserControl.Configure(new ControlContext(args->ControlContext), ref storage.HeaderAndProcessor.UserProcessor, new DSPConfiguration(args->Now)).Complete();
                        break;
                    }
                    default:
                    {
                        ProcessorExtensions.DispatchGenericControl(
                            ref storage.UserControl,
                            ref storage.HeaderAndProcessor.UserProcessor,
                            storage.HeaderAndProcessor.Header,
                            (void*)additionalPtr,
                            function
                        );
                        break;
                    }
                }
            }
        }

        internal static IntPtr GetReflectionData<TUserControl, TUserProcessor>()
            where TUserControl : unmanaged, RootOutput.IControl<TUserProcessor>
            where TUserProcessor : unmanaged, RootOutput.IProcessor
        {
            JobStruct<TUserControl, TUserProcessor>.Initialize();
            var reflectionData = JobStruct<TUserControl, TUserProcessor>.jobReflectionData.Data;
            return reflectionData;
        }
    }

    [NativeHeader("Modules/Audio/Public/ScriptableProcessors/ScriptBindings/ScriptableProcessor.bindings.h")]
    static class IRootOutputProcessorExtensions
    {
        internal unsafe struct ProcessPhaseUpdateArguments
        {
            internal ProcessingContext* Context;
            internal JobHandle InOut;
            internal Unity.Audio.Handle Self;

            internal float* AudioBuffer;
            internal int OutputFrameCount;
            internal int OutputChannelCount;
        }

        internal struct JobStruct<TUserProcessor>
            where TUserProcessor : unmanaged, RootOutput.IProcessor
        {
            internal struct Storage
            {
                public ProcessorHeader Header;
                public TUserProcessor UserProcessor;
            }

            internal static readonly SharedStatic<IntPtr> jobReflectionData = SharedStatic<IntPtr>.GetOrCreate<JobStruct<TUserProcessor>>();

            [BurstDiscard]
            internal static unsafe void Initialize()
            {
                if (jobReflectionData.Data == IntPtr.Zero)
                    jobReflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(Storage), (ExecuteJobFunction)Execute);
            }

            internal delegate void ExecuteJobFunction(ref Storage storage, IntPtr additionalPtr, IntPtr additionalPtr2, ref JobRanges ranges, int jobIndex);

            public static unsafe void Execute(ref Storage storage, IntPtr additionalPtr, IntPtr processorFunction, ref JobRanges ranges, int jobIndex)
            {
                var function = (ProcessorFunction)processorFunction;

                switch (function)
                {
                    case ProcessorFunction.OutputProcessEarly:
                    {
                        var args = (ProcessPhaseUpdateArguments*)additionalPtr;
                        args->InOut = storage.UserProcessor.EarlyProcessing(*args->Context, new (args->Self));
                        break;
                    }
                    case ProcessorFunction.OutputProcess:
                    {
                        var args = (ProcessPhaseUpdateArguments*)additionalPtr;
                        storage.UserProcessor.Process(*args->Context, new(args->Self), args->InOut);
                        break;
                    }
                    case ProcessorFunction.OutputProcessEnd:
                    {
                        var args = (ProcessPhaseUpdateArguments*)additionalPtr;
                        var buffer = new Span<float>(args->AudioBuffer, args->OutputChannelCount * args->OutputFrameCount);
                        var channelBuffer = new ChannelBuffer(buffer, args->OutputChannelCount);

                        storage.UserProcessor.EndProcessing(*args->Context, new(args->Self), channelBuffer);
                        break;
                    }
                    case ProcessorFunction.OutputRemoved:
                    {
                        storage.UserProcessor.RemovedFromProcessing();
                        break;
                    }
                    default:
                    {
                        ProcessorExtensions.DispatchGenericProcessor(
                            ref storage.UserProcessor,
                            storage.Header,
                            (void*)additionalPtr,
                            function
                        );
                        break;
                    }
                }
            }
        }

        internal static IntPtr GetReflectionData<T>()
            where T : unmanaged, RootOutput.IProcessor
        {
            JobStruct<T>.Initialize();
            var reflectionData = JobStruct<T>.jobReflectionData.Data;
            return reflectionData;
        }

        internal static unsafe void InitializeRootOutputHandle(ProcessorHeader* header, ControlHeader* control, ProcessorInitializationFlags flags)
            => InternalInitializeRootOutputHandle(header, control, flags);

        // Intermediate above exists because otherwise bindings layer will throw.
        [NativeMethod(Name = "audio::InitializeRootOutputHandle", IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void InternalInitializeRootOutputHandle(/*ScriptingProcessorHeader*/ void* header, /*ControlHeader*/ void* control, ProcessorInitializationFlags flags);
    }

    #endregion
}
