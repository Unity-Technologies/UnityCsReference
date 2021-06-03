// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// An event sent when a value in a PropertyField changes.
    /// </summary>
    public sealed class SerializedPropertyChangeEvent : EventBase<SerializedPropertyChangeEvent>
    {
        /// <summary>
        /// The SerializedProperty whose value changed.
        /// </summary>
        public SerializedProperty changedProperty { get; set; }

        /// <summary>
        /// Sets the event to its initial state.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown;
            changedProperty = null;
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the values provided.
        /// Use this function instead of creating new events. Events obtained using this method need to be
        /// released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="value">The SerializedProperty that changed.</param>
        /// <returns>An initialized event.</returns>
        public static SerializedPropertyChangeEvent GetPooled(SerializedProperty value)
        {
            SerializedPropertyChangeEvent e = GetPooled();
            e.changedProperty = value;
            return e;
        }

        /// <summary>
        /// Constructor. Use GetPooled instead.
        /// </summary>
        public SerializedPropertyChangeEvent()
        {
            LocalInit();
        }
    };

    /// <summary>
    /// An event sent when any value in a SerializedObject changes
    /// </summary>
    public sealed class SerializedObjectChangeEvent : EventBase<SerializedObjectChangeEvent>
    {
        /// <summary>
        /// The SerializedObject whose value changed.
        /// </summary>
        public SerializedObject changedObject { get; set; }

        /// <summary>
        /// Sets the event to its initial state.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown;
            changedObject = null;
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the values provided. Use this function instead of
        /// creating new events. Events obtained using this method need to be released back to the pool.
        /// You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="value">The SerializedObject that changed.</param>
        /// <returns>An initialized event.</returns>
        public static SerializedObjectChangeEvent GetPooled(SerializedObject value)
        {
            SerializedObjectChangeEvent e = GetPooled();
            e.changedObject = value;
            return e;
        }

        /// <summary>
        /// Constructor. Use GetPooled instead.
        /// </summary>
        public SerializedObjectChangeEvent()
        {
            LocalInit();
        }
    };

    internal class SerializedObjectBindEvent : EventBase<SerializedObjectBindEvent>
    {
        private SerializedObject m_BindObject;
        public SerializedObject bindObject
        {
            get
            {
                return m_BindObject;
            }
        }

        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            this.propagation = EventPropagation.Cancellable; // Also makes it not propagatable.
            m_BindObject = null;
        }

        public static SerializedObjectBindEvent GetPooled(SerializedObject obj)
        {
            SerializedObjectBindEvent e = GetPooled();
            e.m_BindObject = obj;
            return e;
        }

        public SerializedObjectBindEvent()
        {
            LocalInit();
        }
    }

    internal class SerializedPropertyBindEvent : EventBase<SerializedPropertyBindEvent>
    {
        private SerializedProperty m_BindProperty;
        public SerializedProperty bindProperty
        {
            get
            {
                return m_BindProperty;
            }
        }

        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            this.propagation = EventPropagation.Cancellable; // Also makes it not propagatable.
            m_BindProperty = null;
        }

        public static SerializedPropertyBindEvent GetPooled(SerializedProperty obj)
        {
            SerializedPropertyBindEvent e = GetPooled();
            e.m_BindProperty = obj;
            return e;
        }

        public SerializedPropertyBindEvent()
        {
            LocalInit();
        }
    }
}
