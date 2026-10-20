// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Burst;
using Debug = Unity.Scripting.LowLevel.Debug;

namespace Unity.Entities
{
    [GenerateBurstMonoInterop("ECBInterop")]
    [BurstCompile]
    internal partial struct ECBInterop
    {
        [BurstMonoInteropMethod]
        [BurstDiscard]
        internal static unsafe void _RemoveSharedComponentReferences(EntityDataAccess* mgr, int* sharedIndex, int count)
        {
            try
            {
                for (var i = 0; i < count; i++)
                {
                    var sharedComponentIndex = sharedIndex[i];
                    mgr->EntityComponentStore->RemoveSharedComponentReference_Unmanaged(sharedComponentIndex);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [BurstMonoInteropMethod]
        internal static unsafe void _ProcessChainChunk(void* walker, int processorType,
            ECBPlaybackState* playbackState, int currentChain, int nextChain)
        {
            if (processorType == (int)EntityCommandBuffer.ECBProcessorType.PlaybackProcessor)
            {
                var playbackWalker = (EntityCommandBuffer.EcbWalker<EntityCommandBuffer.PlaybackProcessor>*) walker;
                playbackWalker->ProcessChain(playbackState, currentChain, nextChain);
            }
            else if (processorType == (int)EntityCommandBuffer.ECBProcessorType.DebugViewProcessor)
            {
                var debugViewWalker = (EntityCommandBuffer.EcbWalker<EntityCommandBuffer.DebugViewProcessor>*) walker;
                debugViewWalker->ProcessChain(playbackState, currentChain, nextChain);
            }
            else if (processorType == (int) EntityCommandBuffer.ECBProcessorType.PlaybackWithTraceProcessor)
            {
                var playbackWithTraceWalker = (EntityCommandBuffer.EcbWalker<EntityCommandBuffer.PlaybackWithTraceProcessor>*) walker;
                playbackWithTraceWalker->ProcessChain(playbackState, currentChain, nextChain);
            }
            else if (processorType == (int) EntityCommandBuffer.ECBProcessorType.PrePlaybackValidationProcessor)
            {
                var prePlaybackValidationWalker = (EntityCommandBuffer.EcbWalker<EntityCommandBuffer.PrePlaybackValidationProcessor>*) walker;
                prePlaybackValidationWalker->ProcessChain(playbackState, currentChain, nextChain);
            }
        }


    }
}
