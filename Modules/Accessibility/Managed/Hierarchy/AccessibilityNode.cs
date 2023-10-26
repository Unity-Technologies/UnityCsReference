// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine.Accessibility
{
    /// <summary>
    /// An instance of a node in the <see cref="AccessibilityHierarchy"/>, representing an element in the UI that the screen reader
    /// can read, focus, and execute actions on.
    /// </summary>
    public class AccessibilityNode
    {
        private class ObservableList<T> : IList<T>, IReadOnlyList<T>, IList
        {
            readonly List<T> m_Items;

            public int Count => m_Items.Count;
            public bool IsSynchronized => (m_Items as IList)?.IsSynchronized ?? false;
            public object SyncRoot => (m_Items as IList)?.SyncRoot ?? false;
            public bool IsReadOnly => (m_Items as IList)?.IsReadOnly ?? false;
            object IList.this[int index]
            {
                get => m_Items[index];
                set => throw new NotImplementedException();
            }

            public ObservableList()
            {
                m_Items = new();
            }

            public ObservableList(IEnumerable<T> enumerable)
            {
                m_Items = new List<T>(enumerable);
            }

            public void CopyTo(Array array, int index) => (m_Items as IList)?.CopyTo(array, index);

            public void Add(T item)
            {
                m_Items.Add(item);
                listChanged?.Invoke();
            }

            public void Insert(int index, T item)
            {
                m_Items.Insert(index, item);
                listChanged?.Invoke();
            }

            public void Remove(T item)
            {
                m_Items.Remove(item);
                listChanged?.Invoke();
            }

            bool ICollection<T>.Remove(T item)
            {
                var removed = m_Items.Remove(item);

                if (removed)
                {
                    listChanged?.Invoke();
                }

                return removed;
            }

            public void Remove(object value)
            {
                throw new NotImplementedException();
            }

            public void RemoveAt(int index)
            {
                m_Items.RemoveAt(index);
                listChanged?.Invoke();
            }

            public bool IsFixedSize { get; }

            public int Add(object value)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                m_Items.Clear();
                listChanged?.Invoke();
            }

            public bool Contains(object value)
            {
                throw new NotImplementedException();
            }

            public int IndexOf(object value)
            {
                throw new NotImplementedException();
            }

            public void Insert(int index, object value)
            {
                throw new NotImplementedException();
            }

            public T this[int index]
            {
                get => m_Items[index];
                set => m_Items[index] = value;
            }

            public int IndexOf(T item) => m_Items.IndexOf(item);
            public bool Contains(T item) => m_Items.Contains(item);
            public void CopyTo(T[] array, int arrayIndex) => m_Items.CopyTo(array, arrayIndex);
            public IEnumerator<T> GetEnumerator() => m_Items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => m_Items.GetEnumerator();
            public event Action listChanged;
        }

        internal AccessibilityNode(int id, AccessibilityHierarchy hierarchy)
        {
            this.id = id;
            m_Hierarchy = hierarchy;
            m_Children = new ObservableList<AccessibilityNode>();
            m_Actions = new ObservableList<AccessibilityAction>();
        }

        internal void AllocateNative()
        {
            if (IsInActiveHierarchy())
            {
                var nodeData = new AccessibilityNodeData
                {
                    id = id,
                    label = label,
                    value = value,
                    hint = hint,
                    isActive = isActive,
                    role = role,
                    allowsDirectInteraction = allowsDirectInteraction,
                    state = state,
                    parentId = parent?.id ?? AccessibilityNodeManager.k_InvalidNodeId,
                    frame = frame,
                    language = language,
                    implementsSelected = selected != null,
                };

                // TODO: A11Y-364 Properly handle unsuccessful native node creation
                AccessibilityNodeManager.CreateNativeNodeWithData(nodeData);
            }

            ActionsChanged();
            m_Actions.listChanged += ActionsChanged;

            foreach (var child in m_Children)
            {
                child.AllocateNative();
            }

            ChildrenChanged();
            m_Children.listChanged += ChildrenChanged;
        }

        internal void FreeNative(bool freeChildren)
        {
            if (freeChildren)
            {
                foreach (var child in m_Children)
                {
                    child.FreeNative(true);
                }
            }

            m_Children.listChanged -= ChildrenChanged;
            m_Actions.listChanged -= ActionsChanged;

            if (IsInActiveHierarchy())
            {
                var parentId = parent?.id ?? AccessibilityNodeManager.k_InvalidNodeId;
                AccessibilityNodeManager.DestroyNativeNode(id, parentId);
            }
        }

        /// <summary>
        /// The ID of this node.
        /// </summary>
        public int id { get; }

        /// <summary>
        /// A string value that succinctly describes this node.
        /// The <see cref="label"/> is the first thing read by the screen reader when a node is focused.
        /// </summary>
        public string label
        {
            get => m_Label;
            set
            {
                if (string.Equals(m_Label, value))
                {
                    return;
                }

                m_Label = value;

                if (IsInActiveHierarchy())
                {
                    AccessibilityNodeManager.SetLabel(id, value);
                }
            }
        }

        /// <summary>
        /// The value of this node.
        /// </summary>
        public string value
        {
            get => m_Value;
            set
            {
                if (string.Equals(m_Value, value))
                {
                    return;
                }

                m_Value = value;

                if (IsInActiveHierarchy())
                {
                    AccessibilityNodeManager.SetValue(id, value);
                }
            }
        }

        /// <summary>
        /// Additional information about the accessibility node (e.g. the result
        /// of performing an action on the node).
        /// </summary>
        public string hint
        {
            get => m_Hint;
            set
            {
                if (string.Equals(m_Hint, value))
                {
                    return;
                }

                m_Hint = value;

                if (IsInActiveHierarchy())
                {
                    AccessibilityNodeManager.SetHint(id, value);
                }
            }
        }

        /// <summary>
        /// Whether this node is active in the hierarchy.
        /// </summary>
        /// <remarks>Non active nodes are ignored by the screen reader.</remarks>
        public bool isActive
        {
            get => m_IsActive;
            set
            {
                if (m_IsActive == value)
                {
                    return;
                }

                m_IsActive = value;

                if (IsInActiveHierarchy())
                {
                    AccessibilityNodeManager.SetIsActive(id, value);
                }
            }
        }

        /// <summary>
        /// The role for the node.
        /// </summary>
        public AccessibilityRole role
        {
            get => m_Role;
            set
            {
                if (m_Role == value)
                {
                    return;
                }

                m_Role = value;

                if (IsInActiveHierarchy())
                {
                    AccessibilityNodeManager.SetRole(id, value);
                }
            }
        }

        /// <summary>
        /// Whether this node allows direct touch interaction for users.
        /// </summary>
        /// <remarks>
        /// This is only supported on iOS.
        /// </remarks>
        public bool allowsDirectInteraction
        {
            get => m_AllowsDirectInteraction;

            set
            {
                if (value && !(Application.isEditor || Application.platform == RuntimePlatform.IPhonePlayer))
                {
                    throw new PlatformNotSupportedException($"allowsDirectInteraction is only supported on iOS.");
                }

                if (m_AllowsDirectInteraction == value)
                {
                    return;
                }

                m_AllowsDirectInteraction = value;

                if (IsInActiveHierarchy())
                {
                    AccessibilityNodeManager.SetAllowsDirectInteraction(id, value);
                }
            }
        }

        /// <summary>
        /// The state for the node.
        /// </summary>
        public AccessibilityState state
        {
            get => m_State;
            set
            {
                if (m_State == value)
                {
                    return;
                }

                m_State = value;

                if (IsInActiveHierarchy())
                {
                    AccessibilityNodeManager.SetState(id, value);
                }
            }
        }

        /// <summary>
        /// The parent of the node. If the node is at the root level, the <see cref="parent"/> value is @@null@@.
        /// </summary>
        public AccessibilityNode parent
        {
            get => m_Parent;
            internal set
            {
                if (m_Parent == value)
                {
                    return;
                }

                m_Parent = value;

                if (IsInActiveHierarchy())
                {
                    var parentId = value?.id ?? AccessibilityNodeManager.k_InvalidNodeId;
                    AccessibilityNodeManager.SetParent(id, parentId);
                }
            }
        }

        internal IList<AccessibilityNode> childList
        {
            get => m_Children;
            set
            {
                if (m_Children != null)
                {
                    m_Children.listChanged -= ChildrenChanged;
                }

                m_Children = new ObservableList<AccessibilityNode>(value);

                ChildrenChanged();
                m_Children.listChanged += ChildrenChanged;
            }
        }

        /// <summary>
        /// The children nodes of the node.
        /// </summary>
        public IReadOnlyList<AccessibilityNode> children => m_Children;

        internal IList<AccessibilityAction> actions
        {
            get => m_Actions;
            set
            {
                if (m_Actions != null)
                {
                    m_Actions.listChanged -= ActionsChanged;
                }

                m_Actions = new ObservableList<AccessibilityAction>(value);

                ActionsChanged();
                m_Actions.listChanged += ActionsChanged;
            }
        }

        /// <summary>
        /// The <see cref="Rect"/> representing the position of the node in the UI. Can be set directly but it is recommended
        /// that <see cref="CalculateFrame"/> is set instead so that the value can be recalculated when necessary.
        /// </summary>
        /// <remarks>If <see cref="AccessibilityHierarchy.RefreshNodeFrames"/> is called, the value of the
        /// <see cref="frame"/> is set to <see cref="Rect.zero"/> if <see cref="frameGetter"/> is not set.</remarks>
        public Rect frame
        {
            get
            {
                if (m_Frame == default)
                {
                    CalculateFrame();
                }

                return m_Frame;
            }
            set => SetFrame(value);
        }

        void SetFrame(Rect frame)
        {
            if (m_Frame == frame)
            {
                return;
            }

            m_Frame = frame;

            if (IsInActiveHierarchy())
            {
                AccessibilityNodeManager.SetFrame(id, frame);
            }
        }

        /// <summary>
        /// Optional delegate that can be set to calculate the <see cref="frame"/> for the node instead of setting a flat value.
        /// If the frame of the node may change over time, this delegate should be set instead of giving a one time value for
        /// the <see cref="frame"/>.
        /// </summary>
        /// <remarks>If <see cref="AccessibilityHierarchy.RefreshNodeFrames"/> is called, the value of the
        /// <see cref="frame"/> is set to <see cref="Rect.zero"/> if <see cref="frameGetter"/> is not set.</remarks>
        public Func<Rect> frameGetter { get; set; }

        internal void CalculateFrame()
        {
            SetFrame(frameGetter?.Invoke() ?? Rect.zero);
        }

        // TODO: A11Y-346 Change to string type
        internal SystemLanguage language
        {
            get => m_Language;
            set
            {
                if (m_Language == value)
                {
                    return;
                }

                m_Language = value;

                if (IsInActiveHierarchy())
                {
                    AccessibilityNodeManager.SetLanguage(id, value);
                }
            }
        }

        /// <summary>
        /// Whether the node is focused by the screen reader.
        /// </summary>
        public bool isFocused => IsInActiveHierarchy() && AccessibilityNodeManager.GetIsFocused(id);

        /// <summary>
        /// Calls the methods in its invocation list when this node is selected by the screen reader.
        /// </summary>
        public event Func<bool> selected;

        string m_Label;
        string m_Value;
        string m_Hint;
        bool m_IsActive = true;
        AccessibilityRole m_Role;
        bool m_AllowsDirectInteraction;
        AccessibilityState m_State;
        AccessibilityNode m_Parent;
        ObservableList<AccessibilityNode> m_Children;
        ObservableList<AccessibilityAction> m_Actions;
        Rect m_Frame;
        SystemLanguage m_Language;
        AccessibilityHierarchy m_Hierarchy;

        internal void GetNodeData(ref AccessibilityNodeData nodeData)
        {
            nodeData.id = id;
            nodeData.isActive = isActive;
            nodeData.label = label;
            nodeData.value = value;
            nodeData.hint = hint;
            nodeData.role = role;
            nodeData.allowsDirectInteraction = allowsDirectInteraction;
            nodeData.state = state;
            nodeData.frame = frame;
            nodeData.parentId = parent?.id ?? AccessibilityNodeManager.k_InvalidNodeId;

            var nodeChildIds = new int[m_Children.Count];
            for (var i = 0; i < m_Children.Count; ++i)
            {
                nodeChildIds[i] = m_Children[i].id;
            }

            nodeData.childIds = nodeChildIds;
            nodeData.language = language;
            nodeData.implementsSelected = selected != null;
        }

        internal void Destroy(bool destroyChildren)
        {
            // Free the native side of the node first, that way no changes on managed need to be synchronized.
            FreeNative(freeChildren: destroyChildren);

            parent?.childList.Remove(this);

            // Test boolean value once instead of once per loop iteration.
            if (destroyChildren)
            {
                for (var i = childList.Count - 1; i >= 0; i--)
                {
                    childList[i].Destroy(true);
                }
            }
            else // Re-parent all children to node's parent.
            {
                foreach (var child in childList)
                {
                    // Even if parent is null (i.e. node is a root) we need to assign it as the children's parent because
                    // that happens when this method is called by AccessibilityHierarchy.RemoveNode and that can happen
                    // with a root node with destroyChildren being false (therefore the children became roots themselves).
                    child.parent = parent;
                    parent?.childList.Add(child);
                }
            }
            childList.Clear();

            m_Hierarchy = null;
        }

        /// <summary>
        /// A hash used for comparisons.
        /// </summary>
        /// <returns>A unique hash code.</returns>
        public override int GetHashCode()
        {
            return id;
        }

        /// <summary>
        /// Provides a debugging string.
        /// </summary>
        /// <returns>A string containing the accessibility node ID and generational version.</returns>
        public override string ToString()
        {
            return $"AccessibilityNode(ID: {id}, Label: {label})";
        }

        void ChildrenChanged()
        {
            if (!IsInActiveHierarchy())
            {
                return;
            }

            var nodeChildIds = new int[m_Children.Count];
            for (var i = 0; i < m_Children.Count; ++i)
            {
                nodeChildIds[i] = m_Children[i].id;
            }

            AccessibilityNodeManager.SetChildren(id, nodeChildIds);
        }

        void ActionsChanged()
        {
            if (!IsInActiveHierarchy())
            {
                return;
            }

            var nodeActions = new AccessibilityAction[m_Actions.Count];
            for (var i = 0; i < m_Actions.Count; ++i)
            {
                nodeActions[i] = m_Actions[i];
            }

            AccessibilityNodeManager.SetActions(id, nodeActions);
        }

        bool IsInActiveHierarchy()
        {
            return m_Hierarchy != null && AssistiveSupport.activeHierarchy == m_Hierarchy;
        }

        /// <summary>
        /// Called when the node gains or loses screen reader focus.
        /// </summary>
        public event Action<AccessibilityNode, bool> focusChanged;

        internal void FocusChanged(bool isNodeFocused)
        {
            AccessibilityManager.QueueNotification(new AccessibilityManager.NotificationContext
            {
                notification = isNodeFocused ? AccessibilityNotification.ElementFocused : AccessibilityNotification.ElementUnfocused,
                currentNode = this,
            });
        }

        internal void NotifyFocusChanged(bool isNodeFocused)
        {
            focusChanged?.Invoke(this, isNodeFocused);
        }

        internal bool Selected()
        {
            return selected?.Invoke() ?? false;
        }
    }
}
