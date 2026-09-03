// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Audio;
using Unity.Burst;

namespace UnityEngine.Audio
{
    /// <summary>
    /// A temporary context tied to a particular mix cycle, and generally passed along when processing <see cref="ProcessorInstance"/>s.
    /// </summary>
    /// <remarks>
    /// This also gives access to communicating data together with a <see cref="ProcessorInstance.Pipe"/>.
    /// </remarks>
    /// <seealso cref="ControlContext.Manual.BeginMix"/>
    public unsafe struct RealtimeContext : ProcessorInstance.IContext
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
        internal UInt64 m_DSPClock;

        ProcessorInstance.AvailableData ProcessorInstance.IContext.GetAvailableData(DualThreadHandle handle)
            => new(ScriptableProcessorBindings.GetAvailableDataForRealtime(Access, handle));

        bool ProcessorInstance.IContext.SendData(DualThreadHandle handle, void* data, int size, int align, long typehash)
        {
            ScriptableProcessorBindings.ReturnDataFromProcessor(Access, handle, data, size, align, typehash);
            return true;
        }

        /// <summary>
        /// Manually process this particular <see cref="GeneratorInstance"/>.
        /// </summary>
        /// <remarks>
        /// In most use cases, you would not call this directly, but rather have the audio system call it for you.
        /// If you are yourself nesting a <see cref="GeneratorInstance"/> inside another <see cref="ProcessorInstance"/>, you would call this.
        /// </remarks>
        /// <param name="generatorInstance">
        /// The <see cref="GeneratorInstance"/> to process.
        /// </param>
        /// <param name="args">
        /// Additional arguments passed along, which can be default-initialized.</param>
        /// <param name="buffer">
        /// The buffer the <see cref="GeneratorInstance"/> will put its processing result into.
        /// </param>
        /// <returns>
        /// A <see cref="GeneratorInstance.Result"/> struct indicating amongst other things how many frames were actually written into <paramref name="buffer"/>.
        /// </returns>
        /// <seealso cref="System.Diagnostics.Process"/>
        public readonly GeneratorInstance.Result Process(GeneratorInstance generatorInstance, ChannelBuffer buffer, GeneratorInstance.Arguments args)
        {
            fixed (float* writeBuffer = buffer.Buffer)
            {
                fixed (RealtimeContext* pContext = &this)
                {
                    var processArguments = new IGeneratorProcessorExtensions.ProcessArguments
                    {
                        AudioBuffer = writeBuffer,
                        Context = pContext,
                        FrameCount = buffer.frameCount,
                        Self = generatorInstance.m_ProcessorInstance.Handle,
                        GeneratorArguments = args
                    };

                    ScriptableProcessorBindings.InvokeRealtimeGenerate(Access, processArguments);

                    return processArguments.Result;
                }
            }
        }

        /// <summary>
        /// Send a message with a piece of data to be immediately evaluated by the <see cref="ProcessorInstance.IRealtime.OnMessage"/>
        /// on the real-time side.
        /// </summary>
        /// <remarks>
        /// This is the real-time counterpart of <see cref="ControlContext.SendMessage"/>: it dispatches synchronously to the
        /// <paramref name="processorInstance"/>'s <see cref="ProcessorInstance.IRealtime"/> implementation from within this
        /// <see cref="RealtimeContext"/>.
        /// The <paramref name="message"/> is passed by reference, so the <paramref name="processorInstance"/> can modify it.
        /// </remarks>
        /// <returns>
        /// <see cref="ProcessorInstance.Response.Handled"/> if <paramref name="processorInstance"/> acknowledged and processed the message,
        /// <see cref="ProcessorInstance.Response.Unhandled"/> if not or ignored.
        /// </returns>
        public readonly ProcessorInstance.Response SendMessage<T>(ProcessorInstance processorInstance, ref T message)
            where T : unmanaged
        {
            ScriptableProcessorBindings.ValidateCanProcess(processorInstance.Handle, this);

            fixed (T* pT = &message)
            {
                ProcessorInstance.Message transport = new ProcessorInstance.Message
                {
                    TypeHash = BurstRuntime.GetHashCode64<T>(),
                    Data = pT,
                    ManagedHandle = default
                };

                RealtimeMessageArguments args = new RealtimeMessageArguments
                {
                    Access = Access,
                    MessageData = &transport,
                    Self = processorInstance.Handle
                };

                ScriptableProcessorBindings.InvokeRealtimeMessage(ref args);

                return args.StatusReturn;
            }
        }
    }

    /// <summary>
    /// The context passed to <see cref="ProcessorInstance.IRealtime.OnMessage"/> when a message is delivered on the
    /// real-time side via <see cref="RealtimeContext.SendMessage"/>.
    /// </summary>
    /// <remarks>
    /// Like <see cref="RealtimeContext"/>, this exposes cross-thread data communication together with a
    /// <see cref="ProcessorInstance.Pipe"/>. Unlike <see cref="RealtimeContext"/>, it
    /// deliberately cannot drive nested processing (there is no <c>Process</c> method): handling a message is a
    /// communication operation, not a rendering one.
    /// </remarks>
    /// <seealso cref="RealtimeContext.SendMessage"/>
    /// <seealso cref="ProcessorInstance.IRealtime.OnMessage"/>
    public unsafe struct RealtimeMessageContext : ProcessorInstance.IContext
    {
        /// <summary>
        /// True if this context was ever created.
        /// </summary>
        public readonly bool isCreated => Access.IsCreated;

        internal RealtimeAccess Access;

        ProcessorInstance.AvailableData ProcessorInstance.IContext.GetAvailableData(DualThreadHandle handle)
            => new(ScriptableProcessorBindings.GetAvailableDataForRealtime(Access, handle));

        bool ProcessorInstance.IContext.SendData(DualThreadHandle handle, void* data, int size, int align, long typehash)
        {
            ScriptableProcessorBindings.ReturnDataFromProcessor(Access, handle, data, size, align, typehash);
            return true;
        }

        internal RealtimeMessageContext(in RealtimeAccess access)
        {
            Access = access;
        }
    }
}
