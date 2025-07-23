// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    /// <summary>
    /// Provides a set of methods to manipulate the document model.
    /// </summary>
    class BuilderDocumentModelEditor
    {
        // Helper class used to create uxml object and edit attributes
        class UxmlAttributesView : BuilderInspectorAttributes
        {
            public UxmlAttributesView(BuilderInspector inspector) : base(inspector)
            {
                // Do not generate any fields
                attributesContainer = null;

                // This view should not handle any batched changes, the main attributes view of the inspector will do it.
                this.inspector.batchedChangesController.deserializeElement -= DeserializeElement;
                this.inspector.batchedChangesController.notifyAllChangesProcessed -= NotifyAllChangesProcessed;
                this.inspector.batchedChangesController.onUndoRedoPerformedByController -= CallDeserializeOnElementActionWrapper;
            }
        }

        static readonly string k_BindingProperty = nameof(DataBinding.property);
        static readonly string k_BindingMode = nameof(DataBinding.bindingMode);
        static readonly string k_BindingDataSource = nameof(DataBinding.dataSource);
        static readonly string k_BindingDataSourceType = nameof(DataBinding.dataSourceTypeString);
        static readonly string k_BindingDataSourcePathString = nameof(DataBinding.dataSourcePathString);
        static readonly string k_BindingUiToSourceConvertersString = nameof(DataBinding.uiToSourceConvertersString);
        static readonly string k_BindingSourceToUIConvertersString = nameof(DataBinding.sourceToUiConvertersString);
        static readonly Dictionary<string, object> s_DataBindingValues = new()
        {
            { k_BindingDataSourcePathString, null },
            { k_BindingDataSource, null },
            { k_BindingDataSourceType, null },
            { k_BindingMode, null },
            { k_BindingUiToSourceConvertersString, null },
            { k_BindingSourceToUIConvertersString, null }
        };
        static readonly Dictionary<string, object> s_TempAllBindingValues = new();

        UxmlAttributesView m_AttributeView;
        UxmlElementAssetHandleBase m_DocumentRootElementHandle;
        UxmlElementAssetHandleBase m_ActiveRootElementHandle;
        VisualTreeAsset m_LastActiveVisualTreeAsset;

        /// <summary>
        /// The document that this editor is editing
        /// </summary>
        public BuilderDocument document { get; }

        /// <summary>
        /// The view representation of the document
        /// </summary>
        public VisualElement documentRootElement => builder.documentRootElement;

        /// <summary>
        /// The handle of the root element of the document
        /// </summary>
        UxmlElementAssetHandleBase documentRootElementHandle
        {
            get
            {
                // Ensure whether the visual tree is always up to date.
                if (m_DocumentRootElementHandle != null)
                {
                    m_DocumentRootElementHandle.visualTree = document.visualTreeAsset;
                }
                else
                {
                    m_DocumentRootElementHandle =
                        new UxmlElementAssetHandle<VisualElement>(documentRootElement, documentRootElement);
                }

                return m_DocumentRootElementHandle;
            }
        }

        /// <summary>
        /// The handle of the visual element that represents the active VisualTreeAsset
        /// </summary>
        public UxmlElementAssetHandleBase activeRootElementHandle
        {
            get
            {
                UpdateActiveRootElementHandle();
                return m_ActiveRootElementHandle;
            }
            set
            {
                m_ActiveRootElementHandle = value;
                m_AttributeView.SetAttributesOwner(document.visualTreeAsset, value.element);
            }
        }

        void UpdateActiveRootElementHandle()
        {
            // Verify whether the active VisualTreeAsset has changed. This happens when the user edits a sub document.
            if (m_LastActiveVisualTreeAsset == activeVisualTreeAsset)
                return;

            m_LastActiveVisualTreeAsset = activeVisualTreeAsset;

            if (m_LastActiveVisualTreeAsset == rootVisualTreeAsset)
                activeRootElementHandle = documentRootElementHandle;
            else
                activeRootElementHandle = new UxmlElementAssetHandle<VisualElement>(documentRootElement, builder.GetActiveRootElement());
        }

        /// <summary>
        /// The builder instance that is currently editing the document
        /// </summary>
        public Builder builder => document.primaryViewportWindow.viewport.paneWindow as Builder;

        /// <summary>
        /// The edited visual tree asset
        /// </summary>
        public VisualTreeAsset rootVisualTreeAsset => builder.documentRootElement.GetVisualTreeAsset();

        /// <summary>
        /// The edited visual tree asset
        /// </summary>
        public VisualTreeAsset activeVisualTreeAsset => document.visualTreeAsset;

        /// <summary>
        /// The selection of the document
        /// </summary>
        public BuilderSelection selection => builder.selection;

        /// <summary>
        /// Constructor for BuilderDocumentModelEditor
        /// </summary>
        /// <param name="document">The document to edit</param>
        public BuilderDocumentModelEditor(BuilderDocument document)
        {
            this.document = document;

            var builder = document.primaryViewportWindow.viewport.paneWindow as Builder;
            // Creates an attribute view only to be able to call AddUxmlObjectToSerializedData.
            // TODO: Remove usage of attribute view when UIT-2802 is addressed.
            m_AttributeView = new UxmlAttributesView(builder.inspector);
        }

        /// <summary>
        /// Creates a handle to the uxml element asset of specified element
        /// </summary>
        /// <param name="element">The element used to create a handle</param>
        /// <typeparam name="T">The type of the visual element</typeparam>
        /// <returns>The created handle</returns>
        public UxmlElementAssetHandle<T> CreateElementHandle<T>(T element) where T : VisualElement
        {
            return new UxmlElementAssetHandle<T>(documentRootElement, element);
        }

        /// <summary>
        /// Adds a new VisualElement with the specified name at the root of the document
        /// </summary>
        /// <param name="name">The optional name of the created element</param>
        /// <returns>The created VisualElement</returns>
        public UxmlElementAssetHandle<VisualElement> AddElement(string name = "")
        {
            return AddChildElement(activeRootElementHandle, name);
        }

        /// <summary>
        /// Inserts a new VisualElement at the specified index with the specified name at the root of the document
        /// </summary>
        /// <param name="index">The insertion index</param>
        /// <param name="name">The optional name of the element to add</param>
        /// <returns>The created VisualElement</returns>
        [CanBeNull]
        public UxmlElementAssetHandle<VisualElement> InsertElement(int index, string name = "")
        {
            return InsertChildElement(index, activeRootElementHandle, name);
        }

        /// <summary>
        /// Adds a new VisualElement of the specified T type with the specified name
        /// </summary>
        /// <param name="name">The optional name of the created element</param>
        /// <typeparam name="T">The type of the Visual Element</typeparam>
        /// <returns>The created VisualElement</returns>
        public UxmlElementAssetHandle<T> AddElement<T>(string name = "") where T : VisualElement, new()
        {
            return AddChildElement<T>(activeRootElementHandle, name);
        }

        /// <summary>
        /// Inserts a new VisualElement of the specified T type at the specified index with the specified name at the root of the document
        /// </summary>
        /// <param name="index">The insertion index</param>
        /// <param name="name">The optional name of the element to add</param>
        /// <typeparam name="T">The type of the element to add</typeparam>
        /// <returns>The created VisualElement</returns>
        public UxmlElementAssetHandle<T> InsertElement<T>(int index, string name = "")
            where T : VisualElement, new()
        {
            return InsertChildElement<T>(index, activeRootElementHandle, name);
        }

        /// <summary>
        /// Adds a child VisualElement to the specified parent element
        /// </summary>
        /// <param name="parentElement">The parent element</param>
        /// <param name="name">The optional name of the created Visual Element</param>
        /// <returns>The created VisualElement</returns>
        public UxmlElementAssetHandle<VisualElement> AddChildElement(UxmlElementAssetHandleBase parentElement,
            string name = "")
        {
            return AddChildElement<VisualElement>(parentElement, name);
        }

        /// <summary>
        /// Inserts a child VisualElement at the specified index to the specified parent element
        /// </summary>
        /// <param name="index">The insertion index</param>
        /// <param name="parentElement">The parent element</param>
        /// <param name="name">The optional name of the element to add</param>
        /// <returns>The added element</returns>
        public UxmlElementAssetHandle<VisualElement> InsertChildElement(int index,
            UxmlElementAssetHandleBase parentElement, string name = "")
        {
            return InsertChildElement<VisualElement>(index, parentElement, name);
        }

        /// <summary>
        /// Adds a child VisualElement of the specified T type to the specified parent element
        /// </summary>
        /// <param name="parentHandle">The parent element</param>
        /// <param name="name">The name of the element to add</param>
        /// <typeparam name="T">The type of the element to add</typeparam>
        /// <returns>The added element</returns>
        public UxmlElementAssetHandle<T> AddChildElement<T>(UxmlElementAssetHandleBase parentHandle,
            string name = "") where T : VisualElement, new()
        {
            return InsertChildElement<T>(-1, parentHandle, name);
        }

        /// <summary>
        /// Inserts a child VisualElement of the specified T type at the specified index to the specified parent element
        /// </summary>
        /// <param name="index">The insertion index</param>
        /// <param name="parentHandle">The parent element</param>
        /// <param name="name">The optional name of the element to add</param>
        /// <typeparam name="T">The type of the element to add</typeparam>
        /// <returns>The added element</returns>
        public UxmlElementAssetHandle<T> InsertChildElement<T>(int index,
            UxmlElementAssetHandleBase parentHandle, string name = "") where T : VisualElement, new()
        {
            var parentElement = parentHandle.element;
            var parentIndexPath = GetIndexPath(parentElement);
            var treeViewItem = BuilderLibraryContent.GetLibraryItemForType(typeof(T));

            VisualElement element = null;

            if (treeViewItem == null)
            {
                throw new ArgumentException($"No library item found for type {typeof(T)}");
            }

            if (BuilderLibraryUtility.InsertElementToDocument(document, selection, treeViewItem, parentElement,
                    index, element, name))
            {
                var updatedParent = GetElementAtIndexPath(parentIndexPath);
                var childIndex = index != -1 ? index : (updatedParent.childCount - 1);
                return new UxmlElementAssetHandle<T>(documentRootElement, updatedParent[childIndex] as T);
            }

            return null;
        }

        /// <summary>
        /// Returns a path of indices to the specified element from the root of the document
        /// </summary>
        /// <param name="element">The target element</param>
        /// <returns>The path of indices</returns>
        List<int> GetIndexPath(VisualElement element)
        {
            if (element == documentRootElement)
                return null;

            var path = new List<int>();

            var current = element;

            do
            {
                path.Add(current.parent.IndexOf(current));
                current = current.parent;
            } while (current != documentRootElement);

            return path;
        }

        /// <summary>
        /// Returns the VisualElement at the specified index path from the root of the document
        /// </summary>
        /// <param name="path">The index path</param>
        /// <returns>The element at the specified path</returns>
        VisualElement GetElementAtIndexPath(IList<int> path)
        {
            var rootElement = documentRootElement;

            if (path == null)
                return rootElement;

            var currentParent = rootElement;
            var index = 0;

            for (var i = path.Count - 1; i > 0; --i)
            {
                index = path[i];

                if (index < 0 || index >= currentParent.childCount)
                    return null;
                currentParent = currentParent[path[i]];
            }

            index = path[0];
            if (index < 0 || index >= currentParent.childCount)
                return null;
            return currentParent[index];
        }

        /// <summary>
        /// Adds a binding to the specified handle
        /// </summary>
        /// <param name="element">The handle of the visual element to bound</param>
        /// <param name="bindingPropertyPath">The path to the property to binding</param>
        /// <param name="dataSource">The data source of the binding</param>
        /// <param name="dataSourceType">The data source type of the binding</param>
        /// <param name="path">The binding path</param>
        /// <param name="bindingMode">The binding mode</param>
        /// <param name="convertersToSource">The identifier of the converter group used when trying to convert data from a UI property to the data source</param>
        /// <param name="convertersToUi">The identifier of the converter group used when trying to convert data from the data source to a UI property</param>
        /// <returns>The handle to the created binding uxml asset</returns>
        public UxmlObjectAssetHandle AddDataBinding(UxmlElementAssetHandleBase element, string bindingPropertyPath,
                UnityEngine.Object dataSource = null, Type dataSourceType = null, string path = null, BindingMode bindingMode = BindingMode.TwoWay,
                string convertersToSource = null, string convertersToUi = null)
        {
            s_DataBindingValues[k_BindingDataSource] = dataSource;
            s_DataBindingValues[k_BindingDataSourcePathString] = path;
            s_DataBindingValues[k_BindingDataSourceType] = dataSourceType?.AssemblyQualifiedName;
            s_DataBindingValues[k_BindingMode] = bindingMode;
            s_DataBindingValues[k_BindingUiToSourceConvertersString] = convertersToSource;
            s_DataBindingValues[k_BindingSourceToUIConvertersString] = convertersToUi;

            return AddBinding<DataBinding>(element, bindingPropertyPath, s_DataBindingValues);
        }

        /// <summary>
        /// Adds a binding to the specified handle
        /// </summary>
        /// <param name="handle">The handle of the visual element to binding</param>
        /// <param name="bindingPropertyPath">The path to the property to binding</param>
        /// <param name="values">The attribute values to initialize the created binding with</param>
        /// <typeparam name="T">The type of the binding to create</typeparam>
        /// <returns>The handle to the created binding uxml asset</returns>
        public UxmlObjectAssetHandle AddBinding<T>(UxmlElementAssetHandleBase handle,
            string bindingPropertyPath, Dictionary<string, object> values = null)
            where T : Binding
        {
            s_TempAllBindingValues.Add(k_BindingProperty, bindingPropertyPath);
            if (values != null)
            {
                foreach (var kvp in values)
                {
                    s_TempAllBindingValues.Add(kvp.Key, kvp.Value);
                }
            }

            UxmlObjectAssetHandle objectAdded;
            try
            {
                objectAdded = AddObject<T>(handle, "bindings", s_TempAllBindingValues);
            }
            finally
            {
                s_TempAllBindingValues.Clear();
            }

            return objectAdded;
        }

        /// <summary>
        /// Removes the binding instance that binds the specified property of the specified element.
        /// </summary>
        /// <param name="element">The handle of the element to unbind.</param>
        /// <param name="property">The property to unbind.</param>
        public void RemoveBinding(UxmlElementAssetHandleBase element, string property)
        {
            m_AttributeView.SetAttributesOwner(element.visualTree, element.element);
            m_AttributeView.serializedRootPath = element.serializedPath;

            VisualElement sourceField = null;

            if (SelectionUtility.IsSelected(element.element))
            {
                sourceField = builder.inspector.FindFieldAtPath(property);
            }

            BuilderBindingUtility.DeleteBinding(sourceField, property, builder, m_AttributeView);
        }

        /// <summary>
        /// Adds a uxml object to the specified parent element at the specified path
        /// </summary>
        /// <param name="parentHandle">The parent handle</param>
        /// <param name="propertyPath">The path to add the object</param>
        /// <param name="values">The attribute values to initialize the created object with</param>
        /// <typeparam name="T">The type of the object to create</typeparam>
        /// <returns>The handle to the created binding</returns>
        public UxmlObjectAssetHandle AddObject<T>(IUxmlAssetHandle parentHandle, string propertyPath,
            Dictionary<string, object> values = null)
        {
            var serializedObject = new SerializedObject(parentHandle.visualTree);
            var serializedProperty =
                serializedObject.FindProperty(parentHandle.serializedPath + $".{propertyPath}");
            var uxmlBindingTypeName = typeof(T).FullName;
            var description = UxmlSerializedDataRegistry.GetDescription(uxmlBindingTypeName);

            UxmlElementAssetHandleBase ownerElement = null;

            if (parentHandle is UxmlElementAssetHandleBase parentElement)
                ownerElement = parentElement;
            else
                ownerElement = (parentHandle as UxmlObjectAssetHandle).owner;

            m_AttributeView.SetAttributesOwner(parentHandle.visualTree, ownerElement.element);

            var res = m_AttributeView.AddUxmlObjectToSerializedData(serializedProperty, description.serializedDataType,
                values);

            var relativePath = res.propertyPath.Replace(ownerElement.serializedPath + ".", "");

            return new UxmlObjectAssetHandle(activeVisualTreeAsset, res.uxmlAsset as UxmlObjectAsset, res.serializedData as UxmlSerializedData,
                ownerElement, relativePath);
        }

        /// <summary>
        /// Adds a selector with the specified name to the active stylesheet
        /// </summary>
        /// <param name="selectorStr">The text of the selector</param>
        /// <returns>Returns true if the selector was successfully added</returns>
        public bool AddSelectorToActiveStyleSheet(string selectorStr)
        {
            return AddSelector(document.activeStyleSheet, selectorStr);
        }

        /// <summary>
        /// Adds a selector with the specified name to the specified stylesheet
        /// </summary>
        /// <param name="styleSheet">The stylesheet to edit</param>
        /// <param name="selectorStr">The text of the selector</param>
        /// <returns>Returns true if the selector was successfully added</returns>
        bool AddSelector(StyleSheet styleSheet, string selectorStr)
        {
            if (styleSheet == null)
                throw new ArgumentNullException(nameof(styleSheet));

            return builder.styleSheets.CreateNewSelector(styleSheet, selectorStr);
        }
    }
}
