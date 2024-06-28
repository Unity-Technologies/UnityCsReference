// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace UnityEngine.UIElements
{
    class TextEventHandler
    {
        TextElement m_TextElement;
        TextInfo textInfo => m_TextElement.uitkTextHandle.textInfo;

        public TextEventHandler(TextElement textElement)
        {
            m_TextElement = textElement;
        }

        EventCallback<PointerDownEvent> m_LinkTagOnPointerDown;
        EventCallback<PointerUpEvent> m_LinkTagOnPointerUp;
        EventCallback<PointerMoveEvent> m_LinkTagOnPointerMove;
        EventCallback<PointerOutEvent> m_LinkTagOnPointerOut;

        EventCallback<PointerUpEvent> m_ATagOnPointerUp;
        EventCallback<PointerMoveEvent> m_ATagOnPointerMove;
        EventCallback<PointerOverEvent> m_ATagOnPointerOver;
        EventCallback<PointerOutEvent> m_ATagOnPointerOut;


        private bool HasAllocatedLinkCallbacks()
        {
            return m_LinkTagOnPointerDown != null;
        }

        private void AllocateLinkCallbacks()
        {
            if (HasAllocatedLinkCallbacks())
                return;

            m_LinkTagOnPointerDown = LinkTagOnPointerDown;
            m_LinkTagOnPointerUp = LinkTagOnPointerUp;
            m_LinkTagOnPointerMove = LinkTagOnPointerMove;
            m_LinkTagOnPointerOut = LinkTagOnPointerOut;
        }

        private bool HasAllocatedATagCallbacks()
        {
            return m_ATagOnPointerUp != null;
        }

        private void AllocateATagCallbacks()
        {
            if (HasAllocatedATagCallbacks())
                return;

            m_ATagOnPointerUp = ATagOnPointerUp;
            m_ATagOnPointerMove = ATagOnPointerMove;
            m_ATagOnPointerOver = ATagOnPointerOver;
            m_ATagOnPointerOut = ATagOnPointerOut;
        }

        void ATagOnPointerUp(PointerUpEvent pue)
        {
            var pos = pue.localPosition - new Vector3(m_TextElement.contentRect.min.x, m_TextElement.contentRect.min.y);
            var intersectingLink = m_TextElement.uitkTextHandle.FindIntersectingLink(pos);
            if (intersectingLink < 0)
                return;

            var link = textInfo.linkInfo[intersectingLink];
            if (link.hashCode != (int)MarkupTag.HREF)
                return;
            if (link.linkId == null || link.linkIdLength <= 0)
                return;

            var href = link.GetLinkId();
            if (Uri.IsWellFormedUriString(href, UriKind.Absolute))
                Application.OpenURL(href);
        }

        internal bool isOverridingCursor;

        void ATagOnPointerOver(PointerOverEvent _)
        {
            isOverridingCursor = false;
        }

        void ATagOnPointerMove(PointerMoveEvent pme)
        {
            var pos = pme.localPosition - new Vector3(m_TextElement.contentRect.min.x, m_TextElement.contentRect.min.y);
            var intersectingLink = m_TextElement.uitkTextHandle.FindIntersectingLink(pos);
            var cursorManager = (m_TextElement.panel as BaseVisualElementPanel)?.cursorManager;
            if (intersectingLink >= 0)
            {
                var link = textInfo.linkInfo[intersectingLink];
                if (link.hashCode == (int)MarkupTag.HREF)
                {
                    if (!isOverridingCursor)
                    {
                        isOverridingCursor = true;

                        // defaultCursorId maps to the UnityEditor.MouseCursor enum where 4 is the link cursor.
                        cursorManager?.SetCursor(new Cursor { defaultCursorId = 4 });
                    }

                    return;
                }
            }

            if (isOverridingCursor)
            {
                cursorManager?.SetCursor(m_TextElement.computedStyle.cursor);
                isOverridingCursor = false;
            }
        }

        void ATagOnPointerOut(PointerOutEvent evt)
        {
            isOverridingCursor = false;
        }

        void LinkTagOnPointerDown(PointerDownEvent pde)
        {
            var pos = pde.localPosition - new Vector3(m_TextElement.contentRect.min.x, m_TextElement.contentRect.min.y);
            var intersectingLink = m_TextElement.uitkTextHandle.FindIntersectingLink(pos);
            if (intersectingLink < 0)
                return;

            var link = textInfo.linkInfo[intersectingLink];

            if (link.hashCode == (int)MarkupTag.HREF)
                return;
            if (link.linkId == null || link.linkIdLength <= 0)
                return;

            using (var e = Experimental.PointerDownLinkTagEvent.GetPooled(pde, link.GetLinkId(), link.GetLinkText(textInfo)))
            {
                e.elementTarget = m_TextElement;
                m_TextElement.SendEvent(e);
            }
        }

        void LinkTagOnPointerUp(PointerUpEvent pue)
        {
            var pos = pue.localPosition - new Vector3(m_TextElement.contentRect.min.x, m_TextElement.contentRect.min.y);
            var intersectingLink = m_TextElement.uitkTextHandle.FindIntersectingLink(pos);
            if (intersectingLink < 0)
                return;

            var link = textInfo.linkInfo[intersectingLink];

            if (link.hashCode == (int)MarkupTag.HREF)
                return;
            if (link.linkId == null || link.linkIdLength <= 0)
                return;

            using (var e = Experimental.PointerUpLinkTagEvent.GetPooled(pue, link.GetLinkId(), link.GetLinkText(textInfo)))
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
            var intersectingLink = m_TextElement.uitkTextHandle.FindIntersectingLink(pos);
            if (intersectingLink >= 0)
            {
                var link = textInfo.linkInfo[intersectingLink];
                if (link.hashCode != (int)MarkupTag.HREF)
                {
                    // PointerOver
                    if (currentLinkIDHash == -1)
                    {
                        currentLinkIDHash = link.hashCode;
                        using (var e = Experimental.PointerOverLinkTagEvent.GetPooled(pme, link.GetLinkId(), link.GetLinkText(textInfo)))
                        {
                            e.elementTarget = m_TextElement;
                            m_TextElement.SendEvent(e);
                        }

                        return;
                    }

                    // PointerMove
                    if (currentLinkIDHash == link.hashCode)
                    {
                        using (var e = Experimental.PointerMoveLinkTagEvent.GetPooled(pme, link.GetLinkId(), link.GetLinkText(textInfo)))
                        {
                            e.elementTarget = m_TextElement;
                            m_TextElement.SendEvent(e);
                        }

                        return;
                    }
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

        internal void HandleLinkAndATagCallbacks()
        {
            if (m_TextElement?.panel == null)
                return;

            if (hasLinkTag)
            {
                AllocateLinkCallbacks();
                m_TextElement.RegisterCallback(m_LinkTagOnPointerDown, TrickleDown.TrickleDown);
                m_TextElement.RegisterCallback(m_LinkTagOnPointerUp, TrickleDown.TrickleDown);
                m_TextElement.RegisterCallback(m_LinkTagOnPointerMove, TrickleDown.TrickleDown);
                m_TextElement.RegisterCallback(m_LinkTagOnPointerOut, TrickleDown.TrickleDown);
            }
            else if (HasAllocatedLinkCallbacks())
            {
                m_TextElement.UnregisterCallback(m_LinkTagOnPointerDown, TrickleDown.TrickleDown);
                m_TextElement.UnregisterCallback(m_LinkTagOnPointerUp, TrickleDown.TrickleDown);
                m_TextElement.UnregisterCallback(m_LinkTagOnPointerMove, TrickleDown.TrickleDown);
                m_TextElement.UnregisterCallback(m_LinkTagOnPointerOut, TrickleDown.TrickleDown);
            }

            if (hasATag)
            {
                AllocateATagCallbacks();
                m_TextElement.RegisterCallback(m_ATagOnPointerUp, TrickleDown.TrickleDown);

                // Switching the cursor to the Link cursor has been disable at runtime until OS cursor support is available at runtime.
                if (m_TextElement.panel.contextType == ContextType.Editor)
                {
                    m_TextElement.RegisterCallback(m_ATagOnPointerMove, TrickleDown.TrickleDown);
                    m_TextElement.RegisterCallback(m_ATagOnPointerOver, TrickleDown.TrickleDown);
                    m_TextElement.RegisterCallback(m_ATagOnPointerOut, TrickleDown.TrickleDown);
                }
            }
            else if (HasAllocatedATagCallbacks())
            {
                m_TextElement.UnregisterCallback(m_ATagOnPointerUp, TrickleDown.TrickleDown);
                if (m_TextElement.panel.contextType == ContextType.Editor)
                {
                    m_TextElement.UnregisterCallback(m_ATagOnPointerMove, TrickleDown.TrickleDown);
                    m_TextElement.UnregisterCallback(m_ATagOnPointerOver, TrickleDown.TrickleDown);
                    m_TextElement.UnregisterCallback(m_ATagOnPointerOut, TrickleDown.TrickleDown);
                }
            }
        }

        // Used by our automated tests.
        internal bool hasLinkTag;

        internal void HandleLinkTag()
        {
            for (int i = 0; i < textInfo.linkCount; i++)
            {
                var linkInfo = textInfo.linkInfo[i];
                if (linkInfo.hashCode != (int)MarkupTag.HREF)
                {
                    hasLinkTag = true;
                    m_TextElement.uitkTextHandle.AddTextInfoToPermanentCache();
                    return;
                }
            }

            if (hasLinkTag)
            {
                hasLinkTag = false;
                m_TextElement.uitkTextHandle.RemoveTextInfoFromPermanentCache();
            }
        }

        // Used by our automated tests.
        internal bool hasATag;

        internal void HandleATag()
        {
            for (int i = 0; i < textInfo.linkCount; i++)
            {
                var linkInfo = textInfo.linkInfo[i];
                if (linkInfo.hashCode == (int)MarkupTag.HREF)
                {
                    hasATag = true;
                    m_TextElement.uitkTextHandle.AddTextInfoToPermanentCache();
                    return;
                }
            }

            if (hasATag)
            {
                hasATag = false;
                m_TextElement.uitkTextHandle.RemoveTextInfoFromPermanentCache();
            }
        }
    }
}
