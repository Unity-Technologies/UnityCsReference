// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public sealed class SerializedPropertyChangeEvent : EventBase<SerializedPropertyChangeEvent>
    {
        public SerializedProperty changedProperty { get; set; }

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

        public static SerializedPropertyChangeEvent GetPooled(SerializedProperty value)
        {
            SerializedPropertyChangeEvent e = GetPooled();
            e.changedProperty = value;
            return e;
        }

        public SerializedPropertyChangeEvent()
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
