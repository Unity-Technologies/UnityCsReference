// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.UIElements;

/// <summary>
/// A reference table to map authoring id paths to VisualElements.
/// </summary>
/// <example>
/// This example shows how to use the <see cref="VisualElementAssetReferenceTable"/> to resolve references to VisualElements after calling CloneTree./>.
/// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/VisualElementAssetReferenceTable_CloneTreeExample.cs"/>
/// </example>
public sealed class VisualElementAssetReferenceTable : IDisposable
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

        /// <summary>
        /// The referenced VisualElement.
        /// </summary>
        /// <remarks>
        /// This is stored as a weak reference, which might be null if the element has been destroyed.
        /// </remarks>
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

        /// <summary>
        /// Disposes the ElementNode and releases its handle.
        /// </summary>
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

        /// <summary>
        /// Disposes the ElementNode.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            ReleaseHandle();
        }
    }

    /// <summary>
    /// Holds a reference to either the root element of a PanelRenderer or a template instance. This node can have children.
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

        /// <summary>
        /// Tries to get a child <see cref="ElementNode"/> by its authoring ID.
        /// </summary>
        /// <param name="id">The authoring-id to find in the document.</param>
        /// <param name="elementNode">The found element node matching <paramref name="id"/>.</param>
        /// <returns><see langword="true"/> if an element with a matching authroing-id could be found; otherwise <see langword="false"/>.</returns>
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
    /// The root represents the root element of the PanelRenderer and may not have an authoring ID.
    /// </summary>
    /// <remarks>
    /// You can reference the root with an <see cref="AuthoringIdPath"/> that contains a single ID of 0.
    /// </remarks>
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

    /// <summary>
    /// Releases the <see cref="VisualElementAssetReferenceTable"/> and all its associated nodes back to the pool,
    /// making them available for reuse and reducing allocation overhead.
    /// </summary>
    public void ReleaseToPool()
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
    /// Retrieves the <see cref="VisualElement"/> associated with the specified authoring ID path.
    /// </summary>
    /// <typeparam name="T">The expected type of the element.</typeparam>
    /// <param name="idPath">The authoring ID path to search for.</param>
    /// <param name="element">
    /// The element found at the given path. It's <see langword="null"/> if the element is no longer
    /// part of the hierarchy and has been destroyed.
    /// </param>
    /// <returns><see langword="true"/> if the path was found; otherwise <see langword="false"/>.</returns>
    public bool TryGetReference<T>(AuthoringIdPath idPath, out T element) where T : VisualElement => TryGetReference(root, idPath, out element);

    /// <summary>
    /// Retrieves the <see cref="VisualElement"/> associated with the specified authoring Id path.
    /// </summary>
    /// <typeparam name="T">The expected type of the element.</typeparam>
    /// <param name="documentNode">The document node to search within. This allows searching for a path that is not based on the root.</param>
    /// <param name="idPath">The authoring ID path to search for.</param>
    /// <param name="element">
    /// The element found at the given path. It's <see langword="null"/> if the element is no longer
    /// part of the hierarchy and has been destroyed.
    /// </param>
    /// <returns><see langword="true"/> if the path was found; otherwise <see langword="false"/>.</returns>
    public bool TryGetReference<T>(DocumentNode documentNode, AuthoringIdPath idPath, out T element) where T : VisualElement
    {
        if (documentNode == null)
            throw new ArgumentNullException(nameof(documentNode));

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

    /// <summary>
    /// Disposes the VisualElementAssetReferenceTable.
    /// Releases all cached references and disposes all nodes.
    /// </summary>
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
