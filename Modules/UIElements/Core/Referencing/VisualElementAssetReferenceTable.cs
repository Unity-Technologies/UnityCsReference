// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements;

/// <summary>
/// A reference table to map authoring id paths to VisualElements.
/// </summary>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
sealed class VisualElementAssetReferenceTable : IDisposable
{
    internal static readonly Pool.ObjectPool<ElementNode> s_ElementNodePool = new(
        createFunc: () => new ElementNode(),
        actionOnDestroy: node => node.Dispose());

    internal static readonly Pool.ObjectPool<DocumentNode> s_DocumentNodePool = new(
        createFunc: () => new DocumentNode(),
        actionOnDestroy: node => node.Dispose());

    internal static readonly Pool.ObjectPool<VisualElementAssetReferenceTable> s_TablePool = new(
        createFunc: () => new VisualElementAssetReferenceTable(),
        actionOnDestroy: node => node.Dispose());

    /// <summary>
    /// Holds a reference to a <see cref="VisualElement"/>.
    /// </summary>
    public class ElementNode : IDisposable
    {
        internal GCHandle m_VisualElementHandle;

        public VisualElement visualElement
        {
            get
            {
                if (m_VisualElementHandle.IsAllocated)
                    return m_VisualElementHandle.Target as VisualElement;
                return null;
            }
            set
            {
                ReleaseHandle();
                m_VisualElementHandle = GCHandle.Alloc(value, GCHandleType.Weak);
            }
        }

        internal ElementNode()
        {
        }

        ~ElementNode() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal virtual void ReleaseToPool()
        {
            ReleaseHandle();
            s_ElementNodePool.Release(this);
        }

        internal void ReleaseHandle()
        {
            if (m_VisualElementHandle.IsAllocated)
                m_VisualElementHandle.Free();
        }

        protected virtual void Dispose(bool disposing)
        {
            ReleaseHandle();
        }
    }

    /// <summary>
    /// Holds a reference to either the root element of a UIDocument or a template instance. This node can have children.
    /// </summary>
    public class DocumentNode : ElementNode
    {
        internal readonly Dictionary<int, ElementNode> m_Children = new();

        internal DocumentNode()
        {
        }

        internal ElementNode AddElement(int id, VisualElement visualElement)
        {
            var child = s_ElementNodePool.Get();
            child.visualElement = visualElement;
            m_Children.Add(id, child);
            return child;
        }

        internal DocumentNode AddDocument(int id, VisualElement visualElement)
        {
            var child = s_DocumentNodePool.Get();
            child.visualElement = visualElement;
            m_Children.Add(id, child);
            return child;
        }

        public bool TryGetChild(int id, out ElementNode elementNode) => m_Children.TryGetValue(id, out elementNode);

        internal override void ReleaseToPool()
        {
            foreach(var child in m_Children.Values)
                child.ReleaseToPool();
            m_Children.Clear();
            ReleaseHandle();
            s_DocumentNodePool.Release(this);
        }
    }

    internal readonly Dictionary<(ElementNode root, AuthoringIdPath path), GCHandle> m_CachedReferences = new();

    /// <summary>
    /// The root represents the root element of the UIDocument and may not have an authoring id.
    /// </summary>
    public DocumentNode root { get; private set; }

    private VisualElementAssetReferenceTable()
    {
    }

    ~VisualElementAssetReferenceTable()
    {
        Dispose(false);
    }

    /// <summary>
    /// Creates a new VisualElementAssetReferenceTable from the pool.
    /// </summary>
    internal static VisualElementAssetReferenceTable Create(VisualElement rootElement)
    {
        if (rootElement == null)
            throw new ArgumentNullException(nameof(rootElement));

        var table = s_TablePool.Get();
        table.root = s_DocumentNodePool.Get();
        table.root.visualElement = rootElement;

        return table;
    }

    internal void ReleaseToPool()
    {
        if (root != null)
        {
            root.ReleaseToPool();
            root = null;
        }

        FreeCachedReference();
        s_TablePool.Release(this);
    }

    /// <summary>
    /// Returns the VisualElement referenced by the given authoring id path.
    /// </summary>
    /// <typeparam name="T">The expected type.</typeparam>
    /// <param name="idPath">The path to search.</param>
    /// <param name="element">The found element.</param>
    /// <returns></returns>
    public bool TryGetReference<T>(AuthoringIdPath idPath, out T element) where T : VisualElement => TryGetReference(root, idPath, out element);

    /// <summary>
    /// Returns the VisualElement referenced by the given authoring id path.
    /// </summary>
    /// <typeparam name="T">The expected type.</typeparam>
    /// <param name="root"></param>
    /// <param name="idPath">The path to search.</param>
    /// <param name="element">The found element.</param>
    /// <returns></returns>
    public bool TryGetReference<T>(DocumentNode documentNode, AuthoringIdPath idPath, out T element) where T : VisualElement
    {
        if (idPath.isEmpty)
        {
            element = null;
            return false;
        }

        if (idPath.isRootReference)
        {
            if (documentNode.m_VisualElementHandle.IsAllocated && documentNode.m_VisualElementHandle.Target is T rootElement)
            {
                element = rootElement;
                return true;
            }
            element = null;
            return false;
        }

        if (idPath.path.Length == 1)
            return TryGetReferenceFromTree(documentNode, idPath, out element, out var _);

        // We cache longer paths for faster lookup
        if (m_CachedReferences.TryGetValue((documentNode, idPath), out var elementHandle) &&
            elementHandle.IsAllocated &&
            elementHandle.Target is T value)
        {
            element = value;
            return true;
        }

        if (TryGetReferenceFromTree<VisualElement>(documentNode, idPath, out var foundElement, out var foundNode))
        {
            // Cache the path for faster lookup in the future
            m_CachedReferences[(documentNode, idPath)] = GCHandle.Alloc(foundElement, GCHandleType.Weak);

            // Check the type
            if (foundElement is T typedElement)
            {
                element = typedElement;
                return true;
            }
        }

        element = null;
        return false;
    }

    bool TryGetReferenceFromTree<T>(DocumentNode documentNode, AuthoringIdPath idPath, out T element, out ElementNode foundNode) where T : VisualElement
    {
        element = null;
        foundNode = null;

        ElementNode currentNode = documentNode;
        foreach (var id in idPath.path)
        {
            if (currentNode is DocumentNode document)
                document.TryGetChild(id, out currentNode);
            else
                return false;

            if (currentNode == null)
                return false;
        }

        if (currentNode.m_VisualElementHandle.IsAllocated && currentNode.m_VisualElementHandle.Target is T result)
        {
            element = result;
            foundNode = currentNode;
            return true;
        }

        return false;
    }

    void FreeCachedReference()
    {
        foreach (var handle in m_CachedReferences.Values)
        {
            if (handle.IsAllocated)
                handle.Free();
        }
        m_CachedReferences.Clear();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        FreeCachedReference();

        if (disposing)
        {
            root?.Dispose();
            root = null;
        }
    }
}
