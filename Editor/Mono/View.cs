// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Scripting;
using UnityEditor;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using IntPtr = System.IntPtr;
using System;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal partial class View : ScriptableObject
    {
        internal virtual void Reflow()
        {
            foreach (View c in children)
                c.Reflow();
        }
        internal string DebugHierarchy(int level)
        {
            StringBuilder prefix = new StringBuilder();
            StringBuilder s = new StringBuilder();
            for (int i = 0; i < level; i++)
            {
                prefix.Append("  ");
            }
            s.Append($"{s}{prefix}{this} p:{position}");
            if (children.Length > 0)
            {
                s.Append(" {\n");
                foreach (View child in children)
                {
                    s.Append(child.DebugHierarchy(level + 2));
                }
                s.Append(prefix);
                s.Append(" }\n");
            }
            else
                s.Append("\n");
            return s.ToString();
        }
        // Can be used by concrete subclasses to store C++ objects
        [SerializeField]
        MonoReloadableIntPtr   m_ViewPtr;
        [SerializeField]
        View[] m_Children = new View[0];
        [System.NonSerialized]
        View m_Parent;
        [System.NonSerialized]
        ContainerWindow m_Window;

        // Workaround for nonserialized stuff above
        internal virtual void Initialize(ContainerWindow win)
        {
            SetWindow(win);
            foreach (View i in m_Children)
            {
                i.m_Parent = this;
                i.Initialize(win);
            }
        }

        [SerializeField] Rect m_Position = new Rect(0, 0, 100, 100);
        [SerializeField] internal Vector2 m_MinSize;
        [SerializeField] internal Vector2 m_MaxSize;

        public virtual Vector2 minSize { get { return m_MinSize; } }
        public virtual Vector2 maxSize { get { return m_MaxSize; } }

        internal void SetMinMaxSizes(Vector2 min, Vector2 max)
        {
            if (m_MinSize == min && m_MaxSize == max)
                return;
            m_MinSize = min;
            m_MaxSize = max;
            if (m_Parent)
                m_Parent.ChildrenMinMaxChanged();
            if (window && window.rootView == this)
                window.SetMinMaxSizes(min, max);
        }

        // Notification so other views can respond to this.
        protected virtual void ChildrenMinMaxChanged() {}

        // Get all children of this view, using bottom-first recursion
        public View[] allChildren
        {
            get
            {
                ArrayList arr = new ArrayList();
                foreach (View i in m_Children)
                {
                    arr.AddRange(i.allChildren);
                }
                arr.Add(this);
                return (View[])arr.ToArray(typeof(View));
            }
        }

        private void __internalAwake()
        {
            hideFlags = HideFlags.DontSave;
        }

        // position in the parent's space.
        public Rect position
        {
            get { return m_Position; }
            set { SetPosition(value); }
        }

        // Override to resize subviews
        protected virtual void SetPosition(Rect newPos)
        {
            if (!IsValidViewRect(newPos))
                throw new ArgumentException($"Invalid position: {newPos}");

            m_Position = newPos;
        }

        // position in the window
        public Rect windowPosition
        {
            get
            {
                if (m_Parent == null)
                    return position;

                Rect p = parent.windowPosition;
                return new Rect(p.x + position.x, p.y + position.y, position.width, position.height);
            }
        }

        // absolute screen position
        public Rect screenPosition
        {
            get
            {
                Rect r = windowPosition;
                if (window != null)
                {
                    Vector2 p = window.WindowToScreenPoint(Vector2.zero);
                    r.x += p.x; r.y += p.y;
                }
                return r;
            }
        }

        //  Which window we're inside. don't set this directly, but change use AddChild, RemoveChild instead.
        public ContainerWindow window { get { return m_Window; } }
        //  The parent view.
        public View parent { get { return m_Parent; } }

        // please don't modify this array directly. use AddChild or set the child's parent view
        public View[] children { get { return m_Children; } }
        public int IndexOfChild(View child)
        {
            int i = 0;
            foreach (View c in m_Children)
            {
                if (c == child)
                    return i;
                i++;
            }
            return -1;
        }

        protected virtual void OnDestroy()
        {
            foreach (View v in m_Children)
            {
                UnityEngine.Object.DestroyImmediate(v, true);
            }
        }

        // Add/remove child views
        public void AddChild(View child)
        {AddChild(child, m_Children.Length); }
        public virtual void AddChild(View child, int idx)
        {
            System.Array.Resize(ref m_Children, m_Children.Length + 1);
            if (idx != m_Children.Length - 1)
                System.Array.Copy(m_Children, idx, m_Children, idx + 1, m_Children.Length - idx - 1);

            m_Children[idx] = child;

            if (child.m_Parent)
                child.m_Parent.RemoveChild(child);
            child.m_Parent = this;
            child.SetWindowRecurse(window);
            ChildrenMinMaxChanged();
        }

        public virtual void RemoveChild(View child)
        {
            int idx = System.Array.IndexOf(m_Children, child);
            if (idx == -1)
                Debug.LogError("Unable to remove child - it's not IN the view");
            else
                RemoveChild(idx);
        }

        public virtual void RemoveChild(int idx)
        {
            View child = m_Children[idx];
            child.m_Parent = null;
            child.SetWindowRecurse(null);
            System.Array.Copy(m_Children, idx + 1, m_Children, idx, m_Children.Length - idx - 1);
            System.Array.Resize(ref m_Children, m_Children.Length - 1);
            ChildrenMinMaxChanged();
        }

        internal virtual void SetWindow(ContainerWindow win)
        {
            m_Window = win;
        }

        internal void SetWindowRecurse(ContainerWindow win)
        {
            SetWindow(win);
            foreach (View i in m_Children)
            {
                i.SetWindowRecurse(win);
            }
        }

        virtual protected bool OnFocus()
        {
            return true;
        }

        internal static bool IsValidViewPosition(Vector2 p) => IsValidViewVector(p);
        internal static bool IsValidViewSize(Vector2 s) => IsValidViewVector(s);
        internal static bool IsValidViewRect(Rect r) => IsValidViewVector(r.position) && IsValidViewVector(r.size);

        static bool IsValidViewVector(Vector2 v)
        {
            return !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsInfinity(v.x) && !float.IsInfinity(v.y);
        }
    }
} //namespace
