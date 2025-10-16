// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

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

        uint m_CurrentIndex = UIRenderDevice.k_MaxQueuedFrameCount - 1;

        Stack<CommandList> m_CommandListPool = new();

        CommandList m_DefaultCommandList = new CommandList(IntPtr.Zero, IntPtr.Zero);
        List<CommandList>[] m_CommandListsArray; // Each index represents a frame
        List<CommandList> m_CurrentFrameCommandLists; // For the current frame

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
            ++m_CurrentIndex;
            if (m_CurrentIndex ==  UIRenderDevice.k_MaxQueuedFrameCount)
                m_CurrentIndex = 0;

            // Get the command lists of the current frame
            m_CurrentFrameCommandLists = m_CommandListsArray[m_CurrentIndex];

            // Release the contents
            UIRenderer lastRenderer = null; 
            for (int i = 0 ; i < m_CurrentFrameCommandLists.Count ; ++i)
            {
                CommandList cmdList = m_CurrentFrameCommandLists[i];

                // Reset the data stored on the UIDocument.
                var rootUIDocumentElement = cmdList.m_Owner as UIDocumentRootElement;
                UIRenderer renderer = rootUIDocumentElement.uiRenderer;

                Debug.Assert(renderer == cmdList.m_Renderer);

                // A UIRenderer might contain multiple command lists (e.g. 1 per material). We make this check
                // to avoid reseting the same UIRenderer many times.
                if (lastRenderer != renderer)
                {
                    if (renderer != null) // It could have been destroyed since the last frame
                        renderer.ResetDrawCallData();
                    lastRenderer = renderer;
                }

                cmdList.Reset();
                m_CommandListPool.Push(cmdList);
            }
            m_CurrentFrameCommandLists.Clear();
        }

        public void BeginSerialize(TextureSlotCount textureSlotCount)
        {
            m_TextureSlotCount = textureSlotCount;

            m_DefaultCommandList.Init(null, null, 0);
        }

        public void EndSerialize()
        {
            // Assign the command lists to the UIRenderer components.
            // Note that the device may be null at this point (e.g., EvaluateChain may had to
            // dispose of the RenderChain when evaluating an immediate element that closed a window).
            for (int i = 0; i < m_CurrentFrameCommandLists.Count; ++i)
            {
                var cmdList = m_CurrentFrameCommandLists[i];
                var renderer = cmdList.m_Renderer;
                if (renderer != null)
                {
                    renderer.commandLists = m_CommandListsArray;
                    bool forceSingleTexture = (cmdList.flags & CommandFlags.ForceSingleTextureSlot) != 0;
                    uint forceRenderType = (uint)(cmdList.flags & CommandFlags.ForceRenderTypeBits) >> (int)CommandFlags.ForceRenderTypeBitOffset;
                    renderer.AddDrawCallData((int)m_CurrentIndex, i, cmdList.m_Material, forceSingleTexture ? 1 : (uint)m_TextureSlotCount, forceRenderType);
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

        public void ResetCommandListUIRenderer()
        {
            // This method is called from UIRenderDevice.Dispose() to ensure that all UIRenderer
            // instances have been properly reset before we dispose of the CommandListManager.
            // Since the UIRenderer can be reused by another CommandListManager instance, we must
            // reset the DrawCallData associated with the current CommandListManager instance.
            if (m_CommandListsArray != null)
            {
                UIRenderer lastRenderer = null;
                for (int i = 0; i < m_CommandListsArray.Length; ++i)
                {
                    List<CommandList> commandLists = m_CommandListsArray[i];
                    for (int j = 0; j < commandLists.Count; ++j)
                    {
                        UIRenderer renderer = commandLists[j].m_Renderer;
                        // A UIRenderer might contain multiple command lists (e.g. 1 per material). We make this check
                        // to avoid reseting the same UIRenderer many times.
                        if (lastRenderer != renderer)
                        {
                            if (renderer != null)
                            {
                                renderer.ResetDrawCallData();
                            }
                            lastRenderer = renderer;
                        }
                    }
                }
            }
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
                        commandLists[j].Dispose();
                    }
                    commandLists.Clear();
                }
                m_CommandListsArray = null;
            }
            else DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern
    }
}
