// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    /// <summary>
    /// This interface represents a handle to a Uxml element or Uxml object in a Builder document.
    /// </summary>
    interface IUxmlAssetHandle
    {
        VisualTreeAsset visualTree { get; }
        string serializedPath { get; }
    }

    /// <summary>
    /// Represents a handle to a Uxml element or Uxml object in a Builder document.
    /// </summary>
    abstract class UxmlAssetHandle<T> : IUxmlAssetHandle where T : UxmlAsset
    {
        public VisualTreeAsset visualTree { get; set; }
        public T uxmlAsset { get; set; }
        public UxmlSerializedData serializedData { get; set; }
        public string serializedPath => GetSerializedPathFromRoot();

        /// <summary>
        /// Constructs a handle from a uxml asset and its visual tree
        /// </summary>
        /// <param name="visualTree">The </param>
        /// <param name="uxmlObjectAsset"></param>
        /// <param name="serializedData"></param>
        public UxmlAssetHandle(VisualTreeAsset visualTree,
            T uxmlAsset, UxmlSerializedData serializedData)
        {
            this.visualTree = visualTree;
            this.uxmlAsset = uxmlAsset;
            this.serializedData = serializedData;
        }

        /// <summary>
        /// Gets the serialized path of the uxml asset from the VisualTreeAsset
        /// </summary>
        /// <returns>The serialized path</returns>
        protected abstract string GetSerializedPathFromRoot();
    }

    /// <summary>
    /// Represents a handle to an uxml object asset
    /// </summary>
    class UxmlObjectAssetHandle : UxmlAssetHandle<UxmlObjectAsset>
    {
        public string relativeSerializedPath { get; }

        /// <summary>
        /// Returns the owner handle of the uxml object asset
        /// </summary>
        public UxmlElementAssetHandleBase owner { get; }

        public UxmlObjectAssetHandle(VisualTreeAsset visualTree, UxmlObjectAsset uxmlObjectAsset,
            UxmlSerializedData serializedData, UxmlElementAssetHandleBase owner, string relativePropertyPath)
            : base(visualTree, uxmlObjectAsset, serializedData)
        {
            this.owner = owner;
            this.relativeSerializedPath = relativePropertyPath;
        }

        protected override string GetSerializedPathFromRoot() => $"{owner.serializedPath}.{relativeSerializedPath}";
    }

    /// <summary>
    /// Represents the base class of handles to uxml element assets
    /// </summary>
    abstract class UxmlElementAssetHandleBase : UxmlAssetHandle<VisualElementAsset>
    {
        private VisualElement m_RootDocumentElement;
        private VisualElement m_Element;

        /// <summary>
        /// Returns the visual element associated to the uxml element asset referenced by this handle
        /// </summary>
        public VisualElement element
        {
            get
            {
                ResolveReference();
                return m_Element;
            }
            private set
            {
                if (m_Element == value)
                    return;

                if (m_Element != null)
                {
                    m_Element.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
                }

                m_Element = value;

                if (m_Element != null)
                {
                    m_Element.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
                }
            }
        }

        public UxmlElementAssetHandleBase(VisualElement rootDocument, VisualElement element)
            : base(rootDocument.GetVisualTreeAsset(), element.GetVisualElementAsset(), element.GetVisualElementAsset()?.serializedData)
        {
            m_RootDocumentElement = rootDocument;
            this.element = element;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            element = null;
        }

        void ResolveUxmlAsset()
        {
            // Resolve the uxmlAsset in case it has been recreated in the VisualTreeAsset.
            // This can happen after performed Undo or Redo for instance.
            var elementAssets = visualTree.DepthFirstTraversalOfType<VisualElementAsset>();

            foreach (var asset in elementAssets)
            {
                if (asset.id == uxmlAsset.id)
                {
                    // Need to be resolved
                    if (uxmlAsset != asset)
                    {
                        uxmlAsset = asset;
                        serializedData = uxmlAsset.serializedData;
                    }
                    return;
                }
            }
        }

        void ResolveReference()
        {
            bool shouldTryToResolve = uxmlAsset != null && m_Element == null;

            // Try to resolve as long as the associated VisualElement is null and there is valid uxml asset
            if (!shouldTryToResolve)
                return;

            ResolveUxmlAsset();

            element = m_RootDocumentElement.Query().Where((e) =>
            {
                var elementAsset = e.GetVisualElementAsset();

                if (elementAsset == null)
                    return false;

                if (elementAsset == uxmlAsset)
                    return true;
                return false;
            }).First();
        }

        protected override string GetSerializedPathFromRoot()
        {
            ResolveUxmlAsset();
            return uxmlAsset.GetSerializedPath();
        }
    }

    /// <summary>
    /// Represents a handle to the uxml element asset of a VisualElement of type T. It keeps track of the referenced
    /// VisualElement and updates its reference when the element is recreated from its uxml element asset.
    /// </summary>
    /// <typeparam name="T">The type of the VisualElement related to this handle</typeparam>
    class UxmlElementAssetHandle<T> : UxmlElementAssetHandleBase where T : VisualElement
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="rootDocument"></param>
        /// <param name="element"></param>
        public UxmlElementAssetHandle(VisualElement rootDocument, T element)
            : base(rootDocument, element)
        {
        }

        /// <summary>
        /// Returns the VisualElement of type T associated to the uxml element asset referenced by this handle
        /// </summary>
        public T Get() => element as T;

        // implicit conversion to T
        public static implicit operator T(UxmlElementAssetHandle<T> handle)
        {
            return handle.Get();
        }
    }
}
