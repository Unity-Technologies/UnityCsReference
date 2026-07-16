// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Bindings;

namespace UnityEngine.Audio
{
    /// <summary>
    /// A <see cref="ProcessorInstance"/> with extra callbacks intended to allow scheduling different management and compute work
    /// over the course of a mix frame. Finally, additional audio can be appended to the final audio output.
    /// </summary>
    /// <remarks>
    /// Usage of this is generally very low level, and intended to provide integration points for internal or external audio middleware,
    /// that share the same input/output resources as the host audio system.
    /// Create instances of these using <see cref="ControlContext.AllocateRootOutput"/>.
    /// </remarks>
    public unsafe partial struct RootOutputInstance : IEquatable<RootOutputInstance>
    {
        /// <summary>
        /// The control interface an implementation of a <see cref="RootOutputInstance"/> must implement on a struct to be fully formed.
        /// </summary>
        /// <remarks>
        /// The control side of a <see cref="ProcessorInstance"/> receives various callbacks from a <see cref="ControlContext"/>
        /// from the logical control thread.
        /// You can annotate this with <see cref="Unity.Burst.BurstCompileAttribute"/> to have it compiled with Burst.
        /// </remarks>
        /// <typeparam name="TRealtime">The tandem processing counterpart.</typeparam>
        /// <seealso cref="ProcessorInstance.IControl{TRealtime}"/>
        [JobProducerType(typeof(IRootOutputControlExtensions.JobStruct<,>))]
        public interface IControl<TRealtime> : ProcessorInstance.IControl<TRealtime>
            where TRealtime : unmanaged, ProcessorInstance.IRealtime
        {
            /// <summary>
            /// Called to configure the <see cref="RootOutputInstance"/> before it is used, and when the audio system reconfigures.
            /// </summary>
            /// <returns>
            /// Optionally you can return a non-default <see cref="JobHandle"/> allowing you to do heavier configuration/setup on a worker thread.
            /// </returns>
            /// <param name="realtime">
            /// The realtime instance that will be used in the processing thread.
            /// In case of reconfiguration, the <paramref name="realtime"/> is temporarily suspended from processing,
            /// and you can safely modify its properties.
            /// </param>
            /// <param name="format">
            /// The updated system format. This is the same as <see cref="ControlContext"/> runs with.
            /// </param>
            /// <seealso cref="AudioSettings.Reset"/>
            /// <param name="context">The context this <see cref="RootOutputInstance"/> is being configured from.</param>
            public JobHandle Configure(ControlContext context, ref TRealtime realtime, in AudioFormat format);
        }

        /// <summary>
        /// The processing interface an implementation of a <see cref="RootOutputInstance"/> must implement on a struct to be fully formed.
        /// </summary>
        /// <remarks>
        /// The processing side of a <see cref="ProcessorInstance"/> receives various callbacks from a <see cref="RealtimeContext"/>
        /// from the logical processing thread.
        /// You can annotate this with <see cref="Unity.Burst.BurstCompileAttribute"/> to have it compiled with Burst.
        /// </remarks>
        /// <seealso cref="ProcessorInstance.IRealtime"/>
        [JobProducerType(typeof(IRootOutputProcessorExtensions.JobStruct<>))]
        public interface IRealtime : Audio.ProcessorInstance.IRealtime
        {
            /// <summary>
            /// Perform any tasks necessary before any other resource managed by this <see cref="RootOutputInstance"/> is being used by anything else.
            /// </summary>
            /// <remarks>
            /// For instance, a <see cref="GeneratorInstance"/> hardware input may sample its data here once, and that would then be available afterwards
            /// without changing for this mixing update.
            /// </remarks>
            /// <returns>
            /// Optionally an async dependency that will be fed into every other <see cref="RootOutputInstance.IRealtime"/>
            /// </returns>
            public JobHandle EarlyProcessing(in RealtimeContext context, ProcessorInstance.Pipe pipe);

            /// <summary>
            /// Schedule your main body of work in parallel to everything else.
            /// If you are using jobs, you are required to manually keep track of dependencies and finish them later.
            /// </summary>
            /// <param name="input">
            /// The complete dependency of all other <see cref="EarlyProcessing"/> for all other <see cref="ProcessorInstance"/>s.
            /// If you are using other/foreign scriptable <see cref="ProcessorInstance"/>s, your work must depend on or complete this parameter.
            /// </param>
            public void Process(in RealtimeContext context, ProcessorInstance.Pipe pipe, JobHandle input);

            /// <summary>
            /// Return the main result of your computation to the system in <paramref name="output"/>.
            /// </summary>
            /// <param name="output">
            /// A buffer with the same size as the <see cref="AudioFormat"/> passed into <see cref="RootOutputInstance.IControl{TRealtime}.Configure"/>.
            /// </param>
            /// <remarks>
            /// The contents written to <paramref name="output"/> will be additively added to the main audio output.
            /// </remarks>
            public void EndProcessing(in RealtimeContext context, ProcessorInstance.Pipe pipe, ChannelBuffer output);

            /// <summary>
            /// Called potentially after a sequence of <see cref="ProcessorInstance.IRealtime.Update"/>,
            /// when a <see cref="ProcessorInstance"/> has been disposed from eg. <see cref="ControlContext.Destroy(RootOutputInstance)"/>.
            /// </summary>
            /// <remarks>
            /// This is a chance to sync any work done or ongoing before leaving the processing thread.
            /// This will always be called after <see cref="EndProcessing"/>.
            /// </remarks>
            public void RemovedFromProcessing();
        }

        /// <summary>
        /// Convert this <see cref="RootOutputInstance"/> to its more general <see cref="ProcessorInstance"/> representation.
        /// </summary>
        /// <see cref="ProcessorInstance"/>s are unowned and can safely handed out to other users.
        public static implicit operator ProcessorInstance(in RootOutputInstance root) => root.m_ProcessorInstance;

        /// <summary>
        /// Checks if this instance equals another.
        /// </summary>
        /// <param name="other">The other instance for comparing.</param>
        /// <returns>True if the given instance is equal to this, otherwise, false.</returns>
        public bool Equals(RootOutputInstance other)
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

            return obj is RootOutputInstance instance && Equals(instance);
        }

        /// <summary>
        /// Checks if two instances are equal.
        /// </summary>
        /// <param name="a">The first instance for comparing.</param>
        /// <param name="b">The second instance for comparing.</param>
        /// <returns>True if the two given instances are equal, otherwise, false.</returns>
        public static bool operator ==(RootOutputInstance a, RootOutputInstance b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Checks if two instances are not equal.
        /// </summary>
        /// <param name="a">The first instance for comparing.</param>
        /// <param name="b">The second instance for comparing.</param>
        /// <returns>True if the two given instances are not equal, otherwise, false.</returns>
        public static bool operator !=(RootOutputInstance a, RootOutputInstance b)
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

        internal RootOutputInstance(ProcessorHeader* header)
            => m_ProcessorInstance = new ProcessorInstance(header->DualThreadHandle, header);

        internal readonly ProcessorInstance m_ProcessorInstance;
    }

    #region job-types

    [EditorBrowsable(EditorBrowsableState.Never)]
    static class IRootOutputControlExtensions
    {
        internal struct JobStruct<TUserControl, TUserProcessor>
            where TUserControl : unmanaged, RootOutputInstance.IControl<TUserProcessor>
            where TUserProcessor : unmanaged, RootOutputInstance.IRealtime
        {
            internal struct ControlStorage
            {
                public IRootOutputProcessorExtensions.JobStruct<TUserProcessor>.Storage HeaderAndProcessor;
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

                        storage.UserControl.Configure(new ControlContext(args->ControlContext), ref storage.HeaderAndProcessor.UserProcessor, new AudioFormat(args->Now)).Complete();
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
            where TUserControl : unmanaged, RootOutputInstance.IControl<TUserProcessor>
            where TUserProcessor : unmanaged, RootOutputInstance.IRealtime
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
            internal RealtimeContext* Context;
            internal JobHandle InOut;
            internal DualThreadHandle Self;

            internal float* AudioBuffer;
            internal int OutputFrameCount;
            internal int OutputChannelCount;
        }

        internal struct JobStruct<TUserProcessor>
            where TUserProcessor : unmanaged, RootOutputInstance.IRealtime
        {
            internal struct Storage
            {
                public ProcessorHeader Header;
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
            where T : unmanaged, RootOutputInstance.IRealtime
        {
            JobStruct<T>.Initialize();
            var reflectionData = JobStruct<T>.jobReflectionData.Data;
            return reflectionData;
        }

        internal static unsafe DualThreadHandle InitializeRootOutputHandle<TRealtime, TControl>(
            ref IRootOutputControlExtensions.JobStruct<TControl, TRealtime>.ControlStorage storage,
            ControlHeader* control,
            ProcessorInstance.InitializationFlags flags
        )
            where TRealtime : unmanaged, RootOutputInstance.IRealtime
            where TControl : unmanaged, RootOutputInstance.IControl<TRealtime>

        {
            fixed (ProcessorHeader* header = &storage.HeaderAndProcessor.Header)
                return InternalInitializeRootOutputHandle(header, sizeof(IRootOutputControlExtensions.JobStruct<TControl, TRealtime>.ControlStorage), control, flags);
        }

        // Intermediate above exists because otherwise bindings layer will throw.
        [NativeMethod(Name = "audio::InitializeRootOutputHandle", IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe DualThreadHandle InternalInitializeRootOutputHandle(/*ScriptingProcessorHeader*/ void* header, int tailSize, /*ControlHeader*/ void* control, ProcessorInstance.InitializationFlags flags);
    }

    #endregion
}
