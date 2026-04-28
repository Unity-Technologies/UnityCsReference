// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Audio;

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

        ProcessorInstance.AvailableData ProcessorInstance.IContext.GetAvailableData(Handle handle)
            => new(ScriptableProcessorBindings.GetAvailableDataForRealtime(Access, handle));

        bool ProcessorInstance.IContext.SendData(Handle handle, void* data, int size, int align, long typehash)
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
        /// <param name="context">
        /// The <see cref="RealtimeContext"/> associated with this call. You either get this from your own callback,
        /// or from <see cref="ControlContext.Manual.BeginMix"/>.
        /// </param>
        /// <param name="args">
        /// Additional arguments passed along, which can be default-initialized.</param>
        /// <param name="buffer">
        /// The buffer the <see cref="GeneratorInstance"/> will put its processing result into.
        /// </param>
        /// <returns>
        /// A <see cref="Result"/> struct indicating amongst other things how many frames were actually written into <paramref name="buffer"/>.
        /// </returns>
        /// <seealso cref="System.Diagnostics.Process"/>
        public readonly GeneratorInstance.Result Process(GeneratorInstance generatorInstance, ChannelBuffer buffer, GeneratorInstance.Arguments args)
        {
            ScriptableProcessorBindings.ValidateCanProcess(generatorInstance.m_ProcessorInstance.Handle, this);

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

                    generatorInstance.m_ProcessorInstance.Header->InvokeProcessor(ProcessorFunction.Process, &processArguments);

                    return processArguments.Result;
                }
            }
        }
    }
}
