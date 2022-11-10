// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements.UIR
{
    static class CommandManipulator
    {
        static bool IsParentOrAncestorOf(this VisualElement ve, VisualElement child)
        {
            // O(n) of tree depth, not very cool
            while (child.hierarchy.parent != null)
            {
                if (child.hierarchy.parent == ve)
                    return true;
                child = child.hierarchy.parent;
            }
            return false;
        }

        public static void ReplaceCommands(RenderChain renderChain, VisualElement ve, EntryProcessor processor)
        {
            if (processor.firstHeadCommand == null && processor.firstTailCommand == null && ve.renderChainData.firstHeadCommand != null)
            {
                ResetCommands(renderChain, ve);
                return;
            }

            // Update head commands
            // Retain our command insertion points if possible, to avoid paying the cost of finding them again
            {
                bool foundInsertionBounds = false;
                RenderChainCommand prev = null;
                RenderChainCommand next = null;
                if (ve.renderChainData.firstHeadCommand != null)
                {
                    prev = ve.renderChainData.firstHeadCommand.prev;
                    next = ve.renderChainData.lastHeadCommand.next;
                    RemoveChain(renderChain, ve.renderChainData.firstHeadCommand, ve.renderChainData.lastHeadCommand);
                    foundInsertionBounds = true;
                }

                // Do we have anything to insert?
                if (processor.firstHeadCommand != null)
                {
                    if (!foundInsertionBounds)
                        FindCommandInsertionPoint(ve, out prev, out next);

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

                    renderChain.OnRenderCommandAdded(processor.firstHeadCommand);
                }

                ve.renderChainData.firstHeadCommand = processor.firstHeadCommand;
                ve.renderChainData.lastHeadCommand = processor.lastHeadCommand;
            }

            // Update tail commands
            // Retain our command insertion points if possible, to avoid paying the cost of finding them again
            {
                bool foundInsertionBounds = false;
                RenderChainCommand prev = null;
                RenderChainCommand next = null;
                if (ve.renderChainData.firstTailCommand != null)
                {
                    prev = ve.renderChainData.firstTailCommand.prev;
                    next = ve.renderChainData.lastTailCommand.next;
                    RemoveChain(renderChain, ve.renderChainData.firstTailCommand, ve.renderChainData.lastTailCommand);
                    foundInsertionBounds = true;
                }

                // Do we have anything to insert?
                if (processor.firstTailCommand != null)
                {
                    if (!foundInsertionBounds)
                        FindTailCommandInsertionPoint(ve, out prev, out next);

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

                    renderChain.OnRenderCommandAdded(processor.firstTailCommand);
                }

                ve.renderChainData.firstTailCommand = processor.firstTailCommand;
                ve.renderChainData.lastTailCommand = processor.lastTailCommand;
            }
        }

        static void FindCommandInsertionPoint(VisualElement ve, out RenderChainCommand prev, out RenderChainCommand next)
        {
            VisualElement prevDrawingElem = ve.renderChainData.prev;

            // This can be potentially O(n) of VE count
            // It is ok to check against lastCommand to mean the presence of tailCommand too, as we
            // require that tail commands only exist if a head command exists too
            while (prevDrawingElem != null && prevDrawingElem.renderChainData.lastHeadCommand == null)
                prevDrawingElem = prevDrawingElem.renderChainData.prev;

            if (prevDrawingElem != null && prevDrawingElem.renderChainData.lastHeadCommand != null)
            {
                // A previous drawing element can be:
                // A) A previous sibling (O(1) check time)
                // B) A parent/ancestor (O(n) of tree depth check time - meh)
                // C) A child/grand-child of a previous sibling to an ancestor (lengthy check time, so it is left as the only choice remaining after the first two)
                if (prevDrawingElem.hierarchy.parent == ve.hierarchy.parent) // Case A
                    prev = prevDrawingElem.renderChainData.lastTailOrHeadCommand;
                else if (prevDrawingElem.IsParentOrAncestorOf(ve)) // Case B
                    prev = prevDrawingElem.renderChainData.lastHeadCommand;
                else
                {
                    // Case C, get the last command that isn't owned by us, this is to skip potential
                    // tail commands wrapped after the previous drawing element
                    var lastCommand = prevDrawingElem.renderChainData.lastTailOrHeadCommand;
                    for (;;)
                    {
                        prev = lastCommand;
                        lastCommand = lastCommand.next;
                        if (lastCommand == null || (lastCommand.owner == ve) || !lastCommand.isTail) // Once again, we assume tail commands cannot exist without opening commands on the element
                            break;
                        if (lastCommand.owner.IsParentOrAncestorOf(ve))
                            break;
                    }
                }

                next = prev.next;
            }
            else
            {
                VisualElement nextDrawingElem = ve.renderChainData.next;
                // This can be potentially O(n) of VE count, very bad.. must adjust
                while (nextDrawingElem != null && nextDrawingElem.renderChainData.firstHeadCommand == null)
                    nextDrawingElem = nextDrawingElem.renderChainData.next;
                next = nextDrawingElem?.renderChainData.firstHeadCommand;
                prev = null;
                Debug.Assert((next == null) || (next.prev == null));
            }
        }

        static void FindTailCommandInsertionPoint(VisualElement ve, out RenderChainCommand prev, out RenderChainCommand next)
        {
            // Tail commands for a visual element come after the tail commands of the shallowest child
            // If not found, then after the last command of the last deepest child
            // If not found, then after the last command of self

            VisualElement nextDrawingElem = ve.renderChainData.next;

            // Depth first search for the first VE that has a command (i.e. non empty element).
            // This can be potentially O(n) of VE count
            // It is ok to check against lastCommand to mean the presence of tailCommand too, as we
            // require that tail commands only exist if a startup command exists too
            while (nextDrawingElem != null && nextDrawingElem.renderChainData.firstHeadCommand == null)
                nextDrawingElem = nextDrawingElem.renderChainData.next;

            if (nextDrawingElem != null && nextDrawingElem.renderChainData.firstHeadCommand != null)
            {
                // A next drawing element can be:
                // A) A next sibling of ve (O(1) check time)
                // B) A child/grand-child of self (O(n) of tree depth check time - meh)
                // C) A next sibling of a parent/ancestor (lengthy check time, so it is left as the only choice remaining after the first two)
                if (nextDrawingElem.hierarchy.parent == ve.hierarchy.parent) // Case A
                {
                    next = nextDrawingElem.renderChainData.firstHeadCommand;
                    prev = next.prev;
                }
                else if (ve.IsParentOrAncestorOf(nextDrawingElem)) // Case B
                {
                    // Enclose the last deepest drawing child by our tail command
                    for (;;)
                    {
                        prev = nextDrawingElem.renderChainData.lastTailOrHeadCommand;
                        nextDrawingElem = prev.next?.owner;
                        if (nextDrawingElem == null || !ve.IsParentOrAncestorOf(nextDrawingElem))
                            break;
                    }
                    next = prev.next;
                }
                else
                {
                    // Case C, just wrap ourselves
                    prev = ve.renderChainData.lastHeadCommand;
                    next = prev.next;
                }
            }
            else
            {
                prev = ve.renderChainData.lastHeadCommand;
                next = prev.next; // prev should not be null since we don't support tail commands without opening commands too
            }
        }

        static void RemoveChain(RenderChain renderChain, RenderChainCommand first, RenderChainCommand last)
        {
            Debug.Assert(first != null);
            Debug.Assert(last != null);

            renderChain.OnRenderCommandsRemoved(first, last);

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
                renderChain.FreeCommand(current);
                prev = current;
                current = next;
            } while (prev != last);
        }

        public static void ResetCommands(RenderChain renderChain, VisualElement ve)
        {
            if (ve.renderChainData.firstHeadCommand != null)
                renderChain.OnRenderCommandsRemoved(ve.renderChainData.firstHeadCommand, ve.renderChainData.lastHeadCommand);

            var prev = ve.renderChainData.firstHeadCommand != null ? ve.renderChainData.firstHeadCommand.prev : null;
            var next = ve.renderChainData.lastHeadCommand != null ? ve.renderChainData.lastHeadCommand.next : null;
            Debug.Assert(prev == null || prev.owner != ve);
            Debug.Assert(next == null || next == ve.renderChainData.firstTailCommand || next.owner != ve);
            if (prev != null) prev.next = next;
            if (next != null) next.prev = prev;

            if (ve.renderChainData.firstHeadCommand != null)
            {
                var c = ve.renderChainData.firstHeadCommand;
                while (c != ve.renderChainData.lastHeadCommand)
                {
                    var nextC = c.next;
                    renderChain.FreeCommand(c);
                    c = nextC;
                }
                renderChain.FreeCommand(c); // Last head command
            }
            ve.renderChainData.firstHeadCommand = ve.renderChainData.lastHeadCommand = null;

            prev = ve.renderChainData.firstTailCommand != null ? ve.renderChainData.firstTailCommand.prev : null;
            next = ve.renderChainData.lastTailCommand != null ? ve.renderChainData.lastTailCommand.next : null;
            Debug.Assert(prev == null || prev.owner != ve);
            Debug.Assert(next == null || next.owner != ve);
            if (prev != null) prev.next = next;
            if (next != null) next.prev = prev;

            if (ve.renderChainData.firstTailCommand != null)
            {
                renderChain.OnRenderCommandsRemoved(ve.renderChainData.firstTailCommand, ve.renderChainData.lastTailCommand);

                var c = ve.renderChainData.firstTailCommand;
                while (c != ve.renderChainData.lastTailCommand)
                {
                    var nextC = c.next;
                    renderChain.FreeCommand(c);
                    c = nextC;
                }
                renderChain.FreeCommand(c); // Last tail command
            }
            ve.renderChainData.firstTailCommand = ve.renderChainData.lastTailCommand = null;
        }
    }
}
