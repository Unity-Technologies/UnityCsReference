// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements.Experimental
{
    /// <summary>
    /// This event is sent when a pointer enters a link tag.
    /// </summary>
    [EventCategory(EventCategory.EnterLeave)]
    public class PointerOverLinkTagEvent : PointerEventBase<PointerOverLinkTagEvent>
    {
        static PointerOverLinkTagEvent()
        {
            SetCreateFunction(() => new PointerOverLinkTagEvent());
        }

        /// <summary>
        /// LinkTag corresponding linkID.
        /// </summary>
        public string linkID { get; private set; }

        /// <summary>
        /// LinkTag corresponding text.
        /// </summary>
        public string linkText { get; private set; }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown;
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <returns>An initialized event.</returns>
        public static PointerOverLinkTagEvent GetPooled(IPointerEvent evt, string linkID, string linkText)
        {
            PointerOverLinkTagEvent e = GetPooled(evt);
            e.linkID = linkID;
            e.linkText = linkText;
            return e;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public PointerOverLinkTagEvent()
        {
            LocalInit();
        }
    }

    /// <summary>
    /// This event is sent when a pointer changes state on a link tag.
    /// </summary>
    [EventCategory(EventCategory.PointerMove)]
    public class PointerMoveLinkTagEvent : PointerEventBase<PointerMoveLinkTagEvent>
    {
        static PointerMoveLinkTagEvent()
        {
            SetCreateFunction(() => new PointerMoveLinkTagEvent());
        }

        /// <summary>
        /// LinkTag corresponding linkID.
        /// </summary>
        public string linkID { get; private set; }

        /// <summary>
        /// LinkTag corresponding text.
        /// </summary>
        public string linkText { get; private set; }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown;
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <returns>An initialized event.</returns>
        public static PointerMoveLinkTagEvent GetPooled(IPointerEvent evt, string linkID, string linkText)
        {
            PointerMoveLinkTagEvent e = GetPooled(evt);
            e.linkID = linkID;
            e.linkText = linkText;
            return e;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public PointerMoveLinkTagEvent()
        {
            LocalInit();
        }
    }

    /// <summary>
    /// This event is sent when a pointer exits a link tag.
    /// </summary>
    [EventCategory(EventCategory.EnterLeave)]
    public class PointerOutLinkTagEvent : PointerEventBase<PointerOutLinkTagEvent>
    {
        static PointerOutLinkTagEvent()
        {
            SetCreateFunction(() => new PointerOutLinkTagEvent());
        }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown;
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <returns>An initialized event.</returns>
        public static PointerOutLinkTagEvent GetPooled(IPointerEvent evt, string linkID)
        {
            PointerOutLinkTagEvent e = GetPooled(evt);
            return e;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public PointerOutLinkTagEvent()
        {
            LocalInit();
        }
    }

    /// <summary>
    /// This event is sent when a pointer is pressed on a Link tag.
    /// </summary>
    public sealed class PointerDownLinkTagEvent : PointerEventBase<PointerDownLinkTagEvent>
    {
        static PointerDownLinkTagEvent()
        {
            SetCreateFunction(() => new PointerDownLinkTagEvent());
        }

        /// <summary>
        /// LinkTag corresponding linkID.
        /// </summary>
        public string linkID { get; private set; }

        /// <summary>
        /// LinkTag corresponding text.
        /// </summary>
        public string linkText { get; private set; }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown;
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <returns>An initialized event.</returns>
        public static PointerDownLinkTagEvent GetPooled(IPointerEvent evt, string linkID, string linkText)
        {
            PointerDownLinkTagEvent e = GetPooled(evt);
            e.linkID = linkID;
            e.linkText = linkText;
            return e;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public PointerDownLinkTagEvent()
        {
            LocalInit();
        }
    }


    /// <summary>
    /// This event is sent when a pointer's last pressed button is released on a link tag.
    /// </summary>
    public class PointerUpLinkTagEvent : PointerEventBase<PointerUpLinkTagEvent>
    {
        static PointerUpLinkTagEvent()
        {
            SetCreateFunction(() => new PointerUpLinkTagEvent());
        }

        /// <summary>
        /// LinkTag corresponding linkID.
        /// </summary>
        public string linkID { get; private set; }

        /// <summary>
        /// LinkTag corresponding text.
        /// </summary>
        public string linkText { get; private set; }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown;
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <returns>An initialized event.</returns>
        public static PointerUpLinkTagEvent GetPooled(IPointerEvent evt, string linkID, string linkText)
        {
            PointerUpLinkTagEvent e = GetPooled(evt);
            e.linkID = linkID;
            e.linkText = linkText;
            return e;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public PointerUpLinkTagEvent()
        {
            LocalInit();
        }
    }
}
