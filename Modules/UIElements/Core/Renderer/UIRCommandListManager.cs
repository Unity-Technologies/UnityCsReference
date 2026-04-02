// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.UIElements.UIR
{
    class CommandListManager : IDisposable
    {
        public static class Testing
        {
            public static List<CommandList> GetCurrentFrameCommandLists(CommandListManager instance) => instance.m_CurrentFrameCommandLists;
        }

        readonly IntPtr m_VertexDecl;
        readonly IntPtr m_DefaultStencilState;

        uint m_SafeFrameIndex = UIRenderDevice.k_MaxQueuedFrameCount - 1;

        Stack<CommandList> m_CommandListPool = new();

        CommandList m_DefaultCommandList = new CommandList(IntPtr.Zero, IntPtr.Zero);
        List<CommandList>[] m_CommandListsArray; // Each index represents a frame
        List<CommandList> m_CurrentFrameCommandLists; // For the current frame

        List<UIRenderer> m_UIRenderersWithDrawCallData = new();
        List<PanelRenderer> m_PanelRenderersWithDrawCallData = new();

        TextureSlotCount m_TextureSlotCount;

        public CommandListManager(IntPtr vertexDecl, IntPtr defaultStencilState)
        {
            m_VertexDecl = vertexDecl;
            m_DefaultStencilState = defaultStencilState;

            m_CommandListsArray = new List<CommandList>[UIRenderDevice.k_MaxQueuedFrameCount];
            for (int i = 0; i < UIRenderDevice.k_MaxQueuedFrameCount; ++i)
                m_CommandListsArray[i] = new List<CommandList>();
        }

        public CommandList defaultCommandList => m_DefaultCommandList;

        public CommandList GetOrCreateCommandList(VisualElement owner, Material material, CommandFlags commandFlags)
        {
            // Reuse command lists whenever possible
            CommandList cmdList;
            if (m_CommandListPool.Count > 0)
                cmdList = m_CommandListPool.Pop();
            else
                cmdList = new CommandList(m_VertexDecl, m_DefaultStencilState);

            cmdList.Init(owner, material, commandFlags);
            m_CurrentFrameCommandLists.Add(cmdList);

            return cmdList;
        }

        public void AdvanceFrame()
        {
            // Update frame index
            ++m_SafeFrameIndex;
            if (m_SafeFrameIndex ==  UIRenderDevice.k_MaxQueuedFrameCount)
                m_SafeFrameIndex = 0;

            // Get the command lists of the current frame
            m_CurrentFrameCommandLists = m_CommandListsArray[m_SafeFrameIndex];

            // Release the contents
            for (int i = 0 ; i < m_CurrentFrameCommandLists.Count ; ++i)
            {
                CommandList cmdList = m_CurrentFrameCommandLists[i];
                cmdList.Reset();
                m_CommandListPool.Push(cmdList);
            }

            m_CurrentFrameCommandLists.Clear();

            // Advance frame on all pooled command lists (including those just returned to the pool)
            foreach (var cmdList in m_CommandListPool)
                cmdList.ActiveDrawRanges.AdvanceFrame();

            // Advance frame on the default command list as well
            m_DefaultCommandList.ActiveDrawRanges.AdvanceFrame();

            ResetUIRendererDrawCallData();
        }

        public void BeginSerialize(TextureSlotCount textureSlotCount)
        {
            m_TextureSlotCount = textureSlotCount;

            m_DefaultCommandList.Init(null, null, 0);
        }

        List<SerializedCommand> m_SerializedCommands = new();

        public unsafe void EndSerialize()
        {
            // Assign the command lists to the UIRenderer/PanelRenderer components.
            // Note that the device may be null at this point (e.g., EvaluateChain may had to
            // dispose of the RenderChain when evaluating an immediate element that closed a window).
            for (int i = 0; i < m_CurrentFrameCommandLists.Count; ++i)
            {
                var cmdList = m_CurrentFrameCommandLists[i];
                var uiRenderer = cmdList.m_UIRenderer;
                if (uiRenderer != null)
                {
                    uiRenderer.commandLists = m_CommandListsArray;
                    bool forceSingleTexture = (cmdList.flags & CommandFlags.ForceSingleTextureSlot) != 0;
                    uint forceRenderType = (uint)(cmdList.flags & CommandFlags.ForceRenderTypeBits) >> (int)CommandFlags.ForceRenderTypeBitOffset;

                    var cmdListState = new CommandListState()
                    {
                        vertexDeclPtr = m_VertexDecl,
                        drawRangesPtr = new IntPtr(cmdList.ActiveDrawRanges.GetBuffer().GetUnsafePtr()),
                        constantPropsPtr = MaterialPropertyBlock.BindingsMarshaller.ConvertToNative(cmdList.constantProps),
                        stencilStatePtr = cmdList.stencilState
                    };

                    var commands = cmdList.Commands;
                    uiRenderer.AddDrawCallData(
                        (int)m_SafeFrameIndex,
                        cmdList.m_Material,
                        forceSingleTexture ? 1 : (uint)m_TextureSlotCount,
                        forceRenderType,
                        new IntPtr(commands.GetBuffer().GetUnsafePtr()),
                        commands.Count,
                        cmdListState);

                    if (m_UIRenderersWithDrawCallData.Count == 0 || m_UIRenderersWithDrawCallData[m_UIRenderersWithDrawCallData.Count - 1] != uiRenderer)
                        m_UIRenderersWithDrawCallData.Add(uiRenderer);
                }

                var panelRenderer = cmdList.m_PanelRenderer;
                if (panelRenderer != null)
                {
                    panelRenderer.commandLists = m_CommandListsArray;
                    bool forceSingleTexture = (cmdList.flags & CommandFlags.ForceSingleTextureSlot) != 0;
                    uint forceRenderType = (uint)(cmdList.flags & CommandFlags.ForceRenderTypeBits) >> (int)CommandFlags.ForceRenderTypeBitOffset;

                    var cmdListState = new CommandListState()
                    {
                        vertexDeclPtr = m_VertexDecl,
                        drawRangesPtr = new IntPtr(cmdList.ActiveDrawRanges.GetBuffer().GetUnsafePtr()),
                        constantPropsPtr = MaterialPropertyBlock.BindingsMarshaller.ConvertToNative(cmdList.constantProps),
                        stencilStatePtr = cmdList.stencilState
                    };

                    var commands = cmdList.Commands;
                    panelRenderer.AddDrawCallData(
                        (int)m_SafeFrameIndex,
                        cmdList.m_Material,
                        forceSingleTexture ? 1 : (uint)m_TextureSlotCount,
                        forceRenderType,
                        new IntPtr(commands.GetBuffer().GetUnsafePtr()),
                        commands.Count,
                        cmdListState);

                    if (m_PanelRenderersWithDrawCallData.Count == 0 || m_PanelRenderersWithDrawCallData[m_PanelRenderersWithDrawCallData.Count - 1] != panelRenderer)
                        m_PanelRenderersWithDrawCallData.Add(panelRenderer);
                }
            }

            // The default command list is never actually executed, but we use it to catch any commands that could
            // be emitted by the root VisualElement. This could happen if, for example, a user applies a style that
            // affects the root in some way.
            m_DefaultCommandList.Reset();
        }

        #region Dispose Pattern

        protected bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
        }

        public void ResetUIRendererDrawCallData()
        {
            foreach (var renderer in m_UIRenderersWithDrawCallData)
            {
                // The renderer may become (fake-)null if the managed part has been destroyed,
                // so it's important to do the null test.
                if (renderer != null)
                    renderer.ResetDrawCallData((int)m_SafeFrameIndex);
            }
            m_UIRenderersWithDrawCallData.Clear();

            foreach (var renderer in m_PanelRenderersWithDrawCallData)
            {
                // The renderer may become (fake-)null if the managed part has been destroyed,
                // so it's important to do the null test.
                if (renderer != null)
                    renderer.ResetDrawCallData((int)m_SafeFrameIndex);
            }
            m_PanelRenderersWithDrawCallData.Clear();
        }

        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                m_DefaultCommandList.Dispose();
                m_DefaultCommandList = null;

                for (int i = 0; i < m_CommandListsArray.Length; ++i)
                {
                    List<CommandList> commandLists = m_CommandListsArray[i];
                    for (int j = 0; j < commandLists.Count; ++j)
                    {
                        var cmdList = commandLists[j];
                        if (cmdList.m_UIRenderer != null)
                        {
                            // It's safe to reset all draw call data here because
                            // the Dispose() is done after the render thread has finished processing.
                            cmdList.m_UIRenderer.ResetAllDrawCallData();
                        }
                        if (cmdList.m_PanelRenderer != null)
                        {
                            // It's safe to reset all draw call data here because
                            // the Dispose() is done after the render thread has finished processing.
                            cmdList.m_PanelRenderer.ResetAllDrawCallData();
                        }

                        cmdList.Dispose();
                    }
                    commandLists.Clear();
                }
                m_CommandListsArray = null;

                while(m_CommandListPool.Count > 0)
                {
                    var cmdList = m_CommandListPool.Pop();
                    cmdList.Dispose();
                }
                m_CommandListPool = null;
            }
            else DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern
    }
}
