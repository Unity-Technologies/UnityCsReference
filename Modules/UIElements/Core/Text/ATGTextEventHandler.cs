// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace UnityEngine.UIElements
{
    class ATGTextEventHandler
    {
        TextElement m_TextElement;

        public ATGTextEventHandler(TextElement textElement)
        {
            Debug.Assert(textElement.uitkTextHandle.useAdvancedText);
            m_TextElement = textElement;
        }

        EventCallback<PointerDownEvent> m_LinkTagOnPointerDown;
        EventCallback<PointerUpEvent> m_LinkTagOnPointerUp;
        EventCallback<PointerMoveEvent> m_LinkTagOnPointerMove;
        EventCallback<PointerOutEvent> m_LinkTagOnPointerOut;

        EventCallback<PointerUpEvent> m_HyperlinkOnPointerUp;
        EventCallback<PointerMoveEvent> m_HyperlinkOnPointerMove;
        EventCallback<PointerOverEvent> m_HyperlinkOnPointerOver;
        EventCallback<PointerOutEvent> m_HyperlinkOnPointerOut;

        bool HasAllocatedLinkCallbacks()
        {
            return m_LinkTagOnPointerDown != null;
        }

        void AllocateLinkCallbacks()
        {
            if (HasAllocatedLinkCallbacks())
                return;

            m_LinkTagOnPointerDown = LinkTagOnPointerDown;
            m_LinkTagOnPointerUp = LinkTagOnPointerUp;
            m_LinkTagOnPointerMove = LinkTagOnPointerMove;
            m_LinkTagOnPointerOut = LinkTagOnPointerOut;
        }

        bool HasAllocatedHyperlinkCallbacks()
        {
            return m_HyperlinkOnPointerUp != null;
        }

        void AllocateHyperlinkCallbacks()
        {
            if (HasAllocatedHyperlinkCallbacks())
                return;

            m_HyperlinkOnPointerUp = HyperlinkOnPointerUp;
            m_HyperlinkOnPointerMove = HyperlinkOnPointerMove;
            m_HyperlinkOnPointerOver = HyperlinkOnPointerOver;
            m_HyperlinkOnPointerOut = HyperlinkOnPointerOut;
        }

        void HyperlinkOnPointerUp(PointerUpEvent pue)
        {
            var pos = pue.localPosition - new Vector3(m_TextElement.contentRect.min.x, m_TextElement.contentRect.min.y);
            var(type, link) = m_TextElement.uitkTextHandle.ATGFindIntersectingLink(pos);
            if (link == null || type!= TextCore.RichTextTagParser.TagType.Hyperlink)
                return;

            if (Uri.IsWellFormedUriString(link, UriKind.Absolute))
                Application.OpenURL(link);
        }

        internal bool isOverridingCursor;

        void HyperlinkOnPointerOver(PointerOverEvent _)
        {
            isOverridingCursor = false;
        }

        void HyperlinkOnPointerMove(PointerMoveEvent pme)
        {
            var pos = pme.localPosition - new Vector3(m_TextElement.contentRect.min.x, m_TextElement.contentRect.min.y);
            var (type, link) = m_TextElement.uitkTextHandle.ATGFindIntersectingLink(pos);

            var cursorManager = (m_TextElement.panel as BaseVisualElementPanel)?.cursorManager;
            if (link != null && type == TextCore.RichTextTagParser.TagType.Hyperlink)
            {

                    if (!isOverridingCursor)
                    {
                        isOverridingCursor = true;

                        // defaultCursorId maps to the UnityEditor.MouseCursor enum where 4 is the link cursor.
                        cursorManager?.SetCursor(new Cursor { defaultCursorId = 4 });
                    }

                    return;
            }

            if (isOverridingCursor)
            {
                cursorManager?.SetCursor(m_TextElement.computedStyle.cursor);
                isOverridingCursor = false;
            }
        }

        void HyperlinkOnPointerOut(PointerOutEvent evt)
        {
            isOverridingCursor = false;
        }


        void LinkTagOnPointerDown(PointerDownEvent pde)
        {
            var pos = pde.localPosition - new Vector3(m_TextElement.contentRect.min.x, m_TextElement.contentRect.min.y);
            // Convert UITK pos to ATG pos
            var (type, link) = m_TextElement.uitkTextHandle.ATGFindIntersectingLink(pos);
            if (link == null || type != TextCore.RichTextTagParser.TagType.Link)
                return;

            using (var e = Experimental.PointerDownLinkTagEvent.GetPooled(pde, link, "test" /* TODO we have no way of gettting the hilighted text*/ ))
            {
                e.elementTarget = m_TextElement;
                m_TextElement.SendEvent(e);
            }
        }

        void LinkTagOnPointerUp(PointerUpEvent pue)
        {
            var pos = pue.localPosition - new Vector3(m_TextElement.contentRect.min.x, m_TextElement.contentRect.min.y);
            var (type, link) = m_TextElement.uitkTextHandle.ATGFindIntersectingLink(pos);
            if (link == null || type != TextCore.RichTextTagParser.TagType.Link)
                return;

            using (var e = Experimental.PointerUpLinkTagEvent.GetPooled(pue, link, "test" /* TODO we have no way of gettting the hilighted text*/ ))
            {
                e.elementTarget = m_TextElement;
                m_TextElement.SendEvent(e);
            }
        }

        // Used in automated test
        internal int currentLinkIDHash = -1;

        void LinkTagOnPointerMove(PointerMoveEvent pme)
        {
            var pos = pme.localPosition - new Vector3(m_TextElement.contentRect.min.x, m_TextElement.contentRect.min.y);
            // Convert UITK pos to ATG pos
            var (type, link) = m_TextElement.uitkTextHandle.ATGFindIntersectingLink(pos);

            if (link != null && type == TextCore.RichTextTagParser.TagType.Link)
            {
                // PointerOver
                if (currentLinkIDHash == -1)
                {
                    currentLinkIDHash = 0; // Placeholder for link.hashCode
                    using (var e = Experimental.PointerOverLinkTagEvent.GetPooled(pme, link, "test" /* TODO we have no way of gettting the hilighted text*/ ))
                    {
                        e.elementTarget = m_TextElement;
                        m_TextElement.SendEvent(e);
                    }

                    return;
                }

                // PointerMove
                if (currentLinkIDHash == 0) // Placeholder for link.hashCode
                {
                    using (var e = Experimental.PointerMoveLinkTagEvent.GetPooled(pme, link, "test" /* TODO we have no way of gettting the hilighted text*/ ))
                    {
                        e.elementTarget = m_TextElement;
                        m_TextElement.SendEvent(e);
                    }

                    return;
                }
            }

            // PointerOut
            if (currentLinkIDHash != -1)
            {
                currentLinkIDHash = -1;
                using (var e = Experimental.PointerOutLinkTagEvent.GetPooled(pme, string.Empty))
                {
                    e.elementTarget = m_TextElement;
                    m_TextElement.SendEvent(e);
                }
            }
        }

        void LinkTagOnPointerOut(PointerOutEvent poe)
        {
            if (currentLinkIDHash != -1)
            {
                using (var e = Experimental.PointerOutLinkTagEvent.GetPooled(poe, string.Empty))
                {
                    e.elementTarget = m_TextElement;
                    m_TextElement.SendEvent(e);
                }

                currentLinkIDHash = -1;
            }
        }

        internal void RegisterLinkTagCallbacks()
        {
            if (m_TextElement?.panel == null)
                return;

            AllocateLinkCallbacks();
            m_TextElement.RegisterCallback(m_LinkTagOnPointerDown, TrickleDown.TrickleDown);
            m_TextElement.RegisterCallback(m_LinkTagOnPointerUp, TrickleDown.TrickleDown);
            m_TextElement.RegisterCallback(m_LinkTagOnPointerMove, TrickleDown.TrickleDown);
            m_TextElement.RegisterCallback(m_LinkTagOnPointerOut, TrickleDown.TrickleDown);
        }

        internal void UnRegisterLinkTagCallbacks()
        {
            if (HasAllocatedLinkCallbacks())
            {
                m_TextElement.UnregisterCallback(m_LinkTagOnPointerDown, TrickleDown.TrickleDown);
                m_TextElement.UnregisterCallback(m_LinkTagOnPointerUp, TrickleDown.TrickleDown);
                m_TextElement.UnregisterCallback(m_LinkTagOnPointerMove, TrickleDown.TrickleDown);
                m_TextElement.UnregisterCallback(m_LinkTagOnPointerOut, TrickleDown.TrickleDown);
            }
        }

        internal void RegisterHyperlinkCallbacks()
        {
            if (m_TextElement?.panel == null)
                return;

            AllocateHyperlinkCallbacks();
            m_TextElement.RegisterCallback(m_HyperlinkOnPointerUp, TrickleDown.TrickleDown);

            // Switching the cursor to the Link cursor has been disable at runtime until OS cursor support is available at runtime.
            if (m_TextElement.panel.contextType == ContextType.Editor)
            {
                m_TextElement.RegisterCallback(m_HyperlinkOnPointerMove, TrickleDown.TrickleDown);
                m_TextElement.RegisterCallback(m_HyperlinkOnPointerOver, TrickleDown.TrickleDown);
                m_TextElement.RegisterCallback(m_HyperlinkOnPointerOut, TrickleDown.TrickleDown);
            }
        }

        internal void UnRegisterHyperlinkCallbacks()
        {
            if (m_TextElement?.panel == null)
                return;

            if (HasAllocatedHyperlinkCallbacks())
            {
                m_TextElement.UnregisterCallback(m_HyperlinkOnPointerUp, TrickleDown.TrickleDown);
                if (m_TextElement.panel.contextType == ContextType.Editor)
                {
                    m_TextElement.UnregisterCallback(m_HyperlinkOnPointerMove, TrickleDown.TrickleDown);
                    m_TextElement.UnregisterCallback(m_HyperlinkOnPointerOver, TrickleDown.TrickleDown);
                    m_TextElement.UnregisterCallback(m_HyperlinkOnPointerOut, TrickleDown.TrickleDown);
                }
            }
        }
    }
}
