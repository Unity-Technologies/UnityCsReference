// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Assertions;

namespace UnityEngine.UIElements.UIR
{
    static class CommandManipulator
    {
        public static void ReplaceHeadCommands(RenderTreeManager renderTreeManager, RenderData renderData, EntryProcessor processor)
        {
            // Retain our command insertion points if possible, to avoid paying the cost of finding them again
            bool foundInsertionBounds = false;
            RenderChainCommand prev = null;
            RenderChainCommand next = null;
            if (renderData.firstHeadCommand != null)
            {
                prev = renderData.firstHeadCommand.prev;
                next = renderData.lastHeadCommand.next;
                RemoveChain(renderData.renderTree, renderData.firstHeadCommand, renderData.lastHeadCommand);
                foundInsertionBounds = true;
            }

            // Do we have anything to insert?
            if (processor.firstHeadCommand != null)
            {
                if (!foundInsertionBounds)
                    FindHeadCommandInsertionPoint(renderData, out prev, out next);

                if (prev != null)
                {
                    processor.firstHeadCommand.prev = prev;
                    prev.next = processor.firstHeadCommand;
                }

                if (next != null)
                {
                    processor.lastHeadCommand.next = next;
                    next.prev = processor.lastHeadCommand;
                }

                renderData.renderTree.OnRenderCommandAdded(processor.firstHeadCommand);
            }

            renderData.firstHeadCommand = processor.firstHeadCommand;
            renderData.lastHeadCommand = processor.lastHeadCommand;
        }

        public static void ReplaceTailCommands(RenderTreeManager renderTreeManager, RenderData renderData, EntryProcessor processor)
        {
            // Retain our command insertion points if possible, to avoid paying the cost of finding them again
            bool foundInsertionBounds = false;
            RenderChainCommand prev = null;
            RenderChainCommand next = null;
            if (renderData.firstTailCommand != null)
            {
                prev = renderData.firstTailCommand.prev;
                next = renderData.lastTailCommand.next;
                RemoveChain(renderData.renderTree, renderData.firstTailCommand, renderData.lastTailCommand);
                foundInsertionBounds = true;
            }

            // Do we have anything to insert?
            if (processor.firstTailCommand != null)
            {
                if (!foundInsertionBounds)
                    FindTailCommandInsertionPoint(renderData, out prev, out next);

                if (prev != null)
                {
                    processor.firstTailCommand.prev = prev;
                    prev.next = processor.firstTailCommand;
                }

                if (next != null)
                {
                    processor.lastTailCommand.next = next;
                    next.prev = processor.lastTailCommand;
                }

                renderData.renderTree.OnRenderCommandAdded(processor.firstTailCommand);
            }

            renderData.firstTailCommand = processor.firstTailCommand;
            renderData.lastTailCommand = processor.lastTailCommand;
        }

        /// <summary>This method searches for a previous command by iterating backwards on a render tree</summary>
        /// <remarks>
        /// Search order from the head:
        /// 1. Head Commands
        /// 2. Previous Sibling
        /// 3. Parent
        ///
        /// Search order from the tail:
        /// 1. Tail Commands
        /// 2. Last Child
        /// 3. Head Commands
        /// 4. Previous Sibling
        /// 5. Parent
        /// </remarks>
        /// <param name="candidate">The RenderData on which we start the iteration</param>
        /// <param name="searchFromHead">When true, we start on the head commands of the candidate and reverse from there.
        /// Otherwise we start on the tail commands of the candidate.</param>
        static RenderChainCommand FindPrevCommand(RenderData candidate, bool searchFromHead)
        {
            // Iterate
            while (true)
            {
                if (!searchFromHead)
                {
                    if (candidate.lastTailCommand != null)
                        return candidate.lastTailCommand;

                    if (candidate.lastChild != null)
                    {
                        candidate = candidate.lastChild;
                        continue;
                    }
                }

                searchFromHead = false;

                if (candidate.lastHeadCommand != null)
                    return candidate.lastHeadCommand;

                if (candidate.prevSibling != null)
                {
                    candidate = candidate.prevSibling;
                    continue;
                }

                if (candidate.parent == null)
                    return null;

                candidate = candidate.parent;
                searchFromHead = true;
            }
        }

        static void FindHeadCommandInsertionPoint(RenderData renderData, out RenderChainCommand prev, out RenderChainCommand next)
        {
            Debug.Assert(renderData.firstHeadCommand == null); // This must only be called when the element previously had no head command.
            prev = FindPrevCommand(renderData, true);
            if (prev == null)
                next = renderData.renderTree.firstCommand;
            else
                next = prev.next;
        }

        static void FindTailCommandInsertionPoint(RenderData renderData, out RenderChainCommand prev, out RenderChainCommand next)
        {
            Debug.Assert(renderData.firstTailCommand == null); // This must only be called when the element previously had no tail command.
            prev = FindPrevCommand(renderData, false);
            Debug.Assert(prev != null); // We MUST have found something because we don't have tails without heads.
            next = prev.next;
        }

        static void RemoveChain(RenderTree renderTree, RenderChainCommand first, RenderChainCommand last)
        {
            Debug.Assert(first != null);
            Debug.Assert(last != null);

            renderTree.OnRenderCommandsRemoved(first, last);

            // Fix the rest of the chain
            if (first.prev != null)
                first.prev.next = last.next;
            if (last.next != null)
                last.next.prev = first.prev;

            // Free the inner chain
            RenderChainCommand current = first;
            RenderChainCommand prev;
            do
            {
                RenderChainCommand next = current.next;
                renderTree.renderTreeManager.FreeCommand(current);
                prev = current;
                current = next;
            } while (prev != last);
        }

        public static void ResetCommands(RenderTreeManager renderTreeManager, RenderData renderData)
        {
            if (renderData.firstHeadCommand != null)
                renderData.renderTree.OnRenderCommandsRemoved(renderData.firstHeadCommand, renderData.lastHeadCommand);

            var prev = renderData.firstHeadCommand != null ? renderData.firstHeadCommand.prev : null;
            var next = renderData.lastHeadCommand != null ? renderData.lastHeadCommand.next : null;
            Debug.Assert(prev == null || prev.owner != renderData);
            Debug.Assert(next == null || next == renderData.firstTailCommand || next.owner != renderData);
            if (prev != null) prev.next = next;
            if (next != null) next.prev = prev;

            if (renderData.firstHeadCommand != null)
            {
                var c = renderData.firstHeadCommand;
                while (c != renderData.lastHeadCommand)
                {
                    var nextC = c.next;
                    renderTreeManager.FreeCommand(c);
                    c = nextC;
                }
                renderTreeManager.FreeCommand(c); // Last head command
            }
            renderData.firstHeadCommand = renderData.lastHeadCommand = null;

            prev = renderData.firstTailCommand != null ? renderData.firstTailCommand.prev : null;
            next = renderData.lastTailCommand != null ? renderData.lastTailCommand.next : null;
            Debug.Assert(prev == null || prev.owner != renderData);
            Debug.Assert(next == null || next.owner != renderData);
            if (prev != null) prev.next = next;
            if (next != null) next.prev = prev;

            if (renderData.firstTailCommand != null)
            {
                renderData.renderTree.OnRenderCommandsRemoved(renderData.firstTailCommand, renderData.lastTailCommand);

                var c = renderData.firstTailCommand;
                while (c != renderData.lastTailCommand)
                {
                    var nextC = c.next;
                    renderTreeManager.FreeCommand(c);
                    c = nextC;
                }
                renderTreeManager.FreeCommand(c); // Last tail command
            }
            renderData.firstTailCommand = renderData.lastTailCommand = null;
        }

        static void InjectCommandInBetween(RenderTreeManager renderTreeManager, RenderChainCommand cmd, RenderChainCommand prev, RenderChainCommand next)
        {
            if (prev != null)
            {
                cmd.prev = prev;
                prev.next = cmd;
            }
            if (next != null)
            {
                cmd.next = next;
                next.prev = cmd;
            }
            var renderData = cmd.owner;

            if (!cmd.isTail)
            {
                if (renderData.firstHeadCommand == null || renderData.firstHeadCommand == next)
                    renderData.firstHeadCommand = cmd;

                if (renderData.lastHeadCommand == null || renderData.lastHeadCommand == prev)
                    renderData.lastHeadCommand = cmd;
            }
            else
            {
                if (renderData.firstTailCommand == null || renderData.firstTailCommand == next)
                    renderData.firstTailCommand = cmd;

                if (renderData.lastTailCommand == null || renderData.lastTailCommand == prev)
                    renderData.lastTailCommand = cmd;
            }

            renderData.renderTree.OnRenderCommandAdded(cmd);
        }

        public static void DisableElementRendering(RenderTreeManager renderTreeManager, VisualElement ve, bool renderingDisabled)
        {
            var renderData = ve.renderData; // Can be the render tree "quad" in the case of render to texture
                                                 // The nested render data will skip rendering entirely.
            if (renderData == null)
                return;

            if (renderingDisabled) // TODO: We whould skip rendering of the RenderTree. The quad should be skipped through Begin/EndDisable.
            {
                if (renderData.firstHeadCommand == null || renderData.firstHeadCommand.type != CommandType.BeginDisable)
                {
                    var cmd = renderTreeManager.AllocCommand();
                    cmd.type = CommandType.BeginDisable;
                    cmd.owner = renderData;

                    if (renderData.firstHeadCommand == null)
                    {
                        FindHeadCommandInsertionPoint(renderData, out var cmdPrev, out var cmdNext);
                        InjectCommandInBetween(renderTreeManager, cmd, cmdPrev, cmdNext);
                    }
                    else
                    {
                        // Need intermediate variable to pass by reference as it is modified
                        var prev = renderData.firstHeadCommand.prev;
                        var next = renderData.firstHeadCommand;
                        var lastHeadCommand = renderData.lastHeadCommand; // InjectCommandInBetween assumes we are adding the last command, witch is not the case now. Backup the value to restore after.
                        Debug.Assert(lastHeadCommand != null);
                        renderData.firstHeadCommand = null; // will be replaced in InjectCommandInBetween
                        InjectCommandInBetween(renderTreeManager, cmd, prev, next);
                        renderData.lastHeadCommand = lastHeadCommand;
                    }
                }

                if (renderData.lastTailCommand == null || renderData.lastTailCommand.type != CommandType.EndDisable)
                {
                    var cmd = renderTreeManager.AllocCommand();
                    cmd.type = CommandType.EndDisable;
                    cmd.isTail = true;
                    cmd.owner = renderData;

                    if (renderData.lastTailCommand == null)
                    {
                        FindTailCommandInsertionPoint(renderData, out var cmdPrev, out var cmdNext);
                        InjectCommandInBetween(renderTreeManager, cmd, cmdPrev, cmdNext);
                    }
                    else
                    {
                        // Need intermediate variable to pass by reference as it is modified
                        var prev = renderData.lastTailCommand;
                        var next = renderData.lastTailCommand.next;
                        Debug.Assert(renderData.firstTailCommand != null);
                        InjectCommandInBetween(renderTreeManager, cmd, prev, next);
                    }
                }
            }
            else
            {
                if (renderData.firstHeadCommand != null && renderData.firstHeadCommand.type == CommandType.BeginDisable)
                    RemoveSingleCommand(renderTreeManager, renderData, renderData.firstHeadCommand);

                if (renderData.lastTailCommand != null && renderData.lastTailCommand.type == CommandType.EndDisable)
                    RemoveSingleCommand(renderTreeManager, renderData, renderData.lastTailCommand);
            }

        }

        static void RemoveSingleCommand(RenderTreeManager renderTreeManager, RenderData renderData, RenderChainCommand cmd)
        {
            Debug.Assert(cmd != null);
            Debug.Assert(cmd.owner == renderData);
            renderData.renderTree.OnRenderCommandsRemoved(cmd, cmd);
            var prev = cmd.prev;
            var next = cmd.next;
            if (prev != null) prev.next = next;
            if (next != null) next.prev = prev;

            // Clean up renderChain head commands pointers in the VisualElement's renderChainData
            if (renderData.firstHeadCommand == cmd)
            {
                // is this the last Head command of the object
                if (renderData.firstHeadCommand == renderData.lastHeadCommand)
                {
                    // Last command removed: extra checks
                    Debug.Assert(cmd.prev?.owner != renderData, "When removing the first head command, the command before this one in the queue should belong to an other parent");
                    Debug.Assert(cmd.next?.owner != renderData || cmd.next == renderData.firstTailCommand); // It could be valid that there is a closing command if they get removed after this call.
                    renderData.firstHeadCommand = null;
                    renderData.lastHeadCommand = null;
                }
                else
                {
                    Debug.Assert(cmd.next.owner == renderData);
                    Debug.Assert(renderData.lastHeadCommand != null);
                    renderData.firstHeadCommand = cmd.next;
                }
            }
            else if (renderData.lastHeadCommand == cmd)
            {
                Debug.Assert(cmd.prev.owner == renderData);
                Debug.Assert(renderData.firstHeadCommand != null);
                renderData.lastHeadCommand = cmd.prev;
            }


            // Clean up renderChain Tail commands
            if (renderData.firstTailCommand == cmd)
            {
                //is this the last tailCommand?
                if (renderData.firstTailCommand == renderData.lastTailCommand)
                {
                    // Last command removed: extra checks
                    Debug.Assert(cmd.prev?.owner != renderData || cmd.prev == renderData.lastHeadCommand);
                    Debug.Assert(cmd.next?.owner != renderData);
                    renderData.firstTailCommand = null;
                    renderData.lastTailCommand = null;
                }
                else
                {
                    Debug.Assert(cmd.next.owner == renderData);
                    Debug.Assert(renderData.lastTailCommand != null);
                    renderData.firstTailCommand = cmd.next;
                }
            }
            else if (renderData.lastTailCommand == cmd)
            {
                Debug.Assert(cmd.prev.owner == renderData);
                Debug.Assert(renderData.firstTailCommand != null);
                renderData.lastTailCommand = cmd.prev;
            }
            renderTreeManager.FreeCommand(cmd);
        }
    }
}
