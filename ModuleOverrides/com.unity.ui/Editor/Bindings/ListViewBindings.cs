// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace UnityEditor.UIElements.Bindings
{
    class ListViewSerializedObjectBinding : SerializedObjectBindingBase
    {
        ListView listView
        {
            get { return boundElement as ListView; }
            set { boundElement = value; }
        }

        SerializedObjectList m_DataList;

        SerializedProperty m_ArraySize;
        int m_ListViewArraySize;

        bool m_IsBinding;
        Func<VisualElement> m_DefaultMakeItem;
        Action<VisualElement, int> m_DefaultBindItem;
        Action<VisualElement, int> m_DefaultUnbindItem;
        EventCallback<DragUpdatedEvent> m_DragUpdatedCallback;
        EventCallback<DragPerformEvent> m_DragPerformCallback;
        EventCallback<SerializedObjectBindEvent> m_SerializedObjectBindEventCallback;
        EventCallback<SerializedPropertyBindEvent> m_SerializedPropertyBindEventCallback;

        public static void CreateBind(ListView listView,
            SerializedObjectBindingContext context,
            SerializedProperty prop)
        {
            var newBinding = new ListViewSerializedObjectBinding();
            newBinding.SetBinding(listView, context, prop);
        }

        public ListViewSerializedObjectBinding()
        {
            m_DefaultMakeItem = MakeListViewItem;
            m_DefaultBindItem = BindListViewItem;
            m_DefaultUnbindItem = UnbindListViewItem;
            m_DragUpdatedCallback = OnDragUpdated;
            m_DragPerformCallback = OnDragPerform;
            m_SerializedObjectBindEventCallback = SerializedObjectBindEventCallback;
            m_SerializedPropertyBindEventCallback = SerializedPropertyBindEventCallback;
        }

        protected void SetBinding(ListView listView, SerializedObjectBindingContext context,
            SerializedProperty prop)
        {
            bindingContext = context;
            boundProperty = prop;
            boundPropertyPath = prop.propertyPath;

            m_DataList = new SerializedObjectList(prop, listView.sourceIncludesArraySize);
            m_ArraySize = m_DataList.ArraySize;
            m_ListViewArraySize = m_DataList.ArraySize.intValue;
            m_LastSourceIncludesArraySize = listView.sourceIncludesArraySize;
            SetListView(listView);
        }

        private void SetListView(ListView lv)
        {
            if (listView != null)
            {
                Debug.LogError("[UI Toolkit] Internal ListViewBindings error. Please report this with Help -> Report a bug...");
                return;
            }

            listView = lv;
            listView.SetProperty(BaseVerticalCollectionView.internalBindingKey, this);
            var parentField = listView.GetProperty(PropertyField.listViewBoundFieldProperty);

            if (listView.makeItem == null)
            {
                listView.SetMakeItemWithoutNotify(m_DefaultMakeItem);
            }

            if (listView.bindItem == null)
            {
                listView.SetBindItemWithoutNotify(m_DefaultBindItem);
            }

            if (listView.unbindItem == null)
            {
                listView.unbindItem = m_DefaultUnbindItem;
            }

            // We prevent hierarchy binding under the contentContainer.
            listView.scrollView.contentContainer.RegisterCallback(m_SerializedObjectBindEventCallback);
            listView.scrollView.contentContainer.RegisterCallback(m_SerializedPropertyBindEventCallback);

            // ListViews instantiated by users are driven by users. We only change the reordering options if the user
            // has used a PropertyField to display the list. (Cases UUM-33402 and UUM-27687)
            var isReorderable = listView.reorderable;
            if (parentField != null)
            {
                isReorderable = PropertyHandler.IsArrayReorderable(m_DataList.ArrayProperty);
                listView.reorderMode = isReorderable ? ListViewReorderMode.Animated : ListViewReorderMode.Simple;
            }

            listView.SetViewController(new EditorListViewController());
            listView.SetDragAndDropController(new SerializedObjectListReorderableDragAndDropController(listView)
            {
                enableReordering = isReorderable,
            });

            listView.itemsSource = m_DataList;

            var foldoutInput = listView.headerFoldout?.toggle?.visualInput;
            if (foldoutInput != null)
            {
                foldoutInput.RegisterCallback(m_DragUpdatedCallback);
                foldoutInput.RegisterCallback(m_DragPerformCallback);
            }
        }

        private void ClearListView()
        {
            if (listView == null)
            {
                Debug.LogError("[UI Toolkit] Internal ListViewBindings error during release. Please report this with Help -> Report a bug...");
                return;
            }

            listView.SetProperty(BaseVerticalCollectionView.internalBindingKey, null);
            listView.itemsSource = null;
            listView.Rebuild();

            if (listView.bindItem == m_DefaultBindItem)
            {
                listView.SetBindItemWithoutNotify(null);
            }

            if (listView.makeItem == m_DefaultMakeItem)
            {
                listView.SetMakeItemWithoutNotify(null);
            }

            if (listView.unbindItem == m_DefaultUnbindItem)
            {
                listView.unbindItem = null;
            }

            listView.scrollView.contentContainer.UnregisterCallback(m_SerializedObjectBindEventCallback);
            listView.scrollView.contentContainer.UnregisterCallback(m_SerializedPropertyBindEventCallback);

            listView.SetViewController(null);

            var foldoutInput = listView.headerFoldout?.toggle?.visualInput;
            if (foldoutInput != null)
            {
                foldoutInput.UnregisterCallback(m_DragUpdatedCallback);
                foldoutInput.UnregisterCallback(m_DragPerformCallback);
            }

            listView = null;
        }

        VisualElement MakeListViewItem()
        {
            return new PropertyField();
        }

        void BindListViewItem(VisualElement ve, int index)
        {
            if (m_ListViewArraySize != -1 && m_ArraySize.intValue != m_ListViewArraySize)
            {
                // We need to wait for array size to be updated, which triggers a refresh anyway.
                return;
            }

            var field = ve as IBindable;
            if (field == null)
            {
                //we find the first Bindable
                field = ve.Query().Where(x => x is IBindable).First() as IBindable;
            }

            if (field == null)
            {
                //can't default bind to anything!
                throw new InvalidOperationException("Can't find BindableElement: please provide BindableVisualElements or provide your own Listview.bindItem callback");
            }

            var item = m_DataList[index];
            var itemProp = item as SerializedProperty;

            m_IsBinding = true;
            field.bindingPath = itemProp.propertyPath;
            bindingContext.ContinueBinding(ve, null);
            m_IsBinding = false;
        }

        void UnbindListViewItem(VisualElement ve, int index)
        {
            if (m_ListViewArraySize != -1 && m_ArraySize.intValue != m_ListViewArraySize)
            {
                // We need to wait for array size to be updated, which triggers a refresh anyway.
                return;
            }

            var field = ve as IBindable;
            if (field == null)
            {
                //we find the first Bindable
                field = ve.Query().Where(x => x is IBindable).First() as IBindable;
            }

            if (field == null)
            {
                //can't default unbind anything!
                throw new InvalidOperationException("Can't find BindableElement: please provide BindableVisualElements or provide your own Listview.unbindItem callback");
            }

            ve.Unbind();
            field.bindingPath = null;
        }

        void SerializedObjectBindEventCallback(SerializedObjectBindEvent evt)
        {
            if (m_IsBinding || listView.bindItem != m_DefaultBindItem)
                return;

            evt.StopPropagation();
        }

        void SerializedPropertyBindEventCallback(SerializedPropertyBindEvent evt)
        {
            if (m_IsBinding || listView.bindItem != m_DefaultBindItem)
                return;

            evt.StopPropagation();
        }

        void OnDragUpdated(DragUpdatedEvent evt)
        {
            ValidateObjectReferences(_ => DragAndDrop.visualMode = DragAndDropVisualMode.Copy);
        }

        void OnDragPerform(DragPerformEvent evt)
        {
            ValidateObjectReferences(obj =>
            {
                listView.viewController.AddItems(1);
                m_DataList.ArrayProperty.GetArrayElementAtIndex(m_DataList.ArraySize.intValue - 1).objectReferenceValue = obj;
                m_DataList.ApplyChanges();
            });
        }

        void ValidateObjectReferences(Action<UnityEngine.Object> onValidated)
        {
            var objReferences = DragAndDrop.objectReferences;
            foreach (var o in objReferences)
            {
                var validatedObject = EditorGUI.ValidateObjectFieldAssignment(new[] { o }, typeof(UnityEngine.Object), m_DataList.ArrayProperty, EditorGUI.ObjectFieldValidatorOptions.None);
                if (validatedObject != null)
                {
                    onValidated.Invoke(validatedObject);
                }
            }

            DragAndDrop.AcceptDrag();
        }

        void UpdateArraySize()
        {
            m_DataList.RefreshProperties(boundProperty, listView.sourceIncludesArraySize);
            m_ArraySize = m_DataList.ArraySize;
            m_ListViewArraySize = m_ArraySize.intValue;
            m_LastSourceIncludesArraySize = listView.sourceIncludesArraySize;
            listView.RefreshItems();
        }

        public override void Release()
        {
            isReleased = true;

            ClearListView();

            bindingContext = null;
            boundProperty = null;
            m_DataList = null;

            m_ArraySize = null;
            m_ListViewArraySize = -1;
        }

        private UInt64 m_LastUpdatedRevision = 0xFFFFFFFFFFFFFFFF;
        private bool m_LastSourceIncludesArraySize;

        protected override void ResetCachedValues()
        {
            m_ListViewArraySize = -1;
            UpdateFieldIsAttached();
        }

        public override void Update()
        {
            if (isReleased)
            {
                return;
            }

            try
            {
                ResetUpdate();
                isUpdating = true;

                if (m_LastUpdatedRevision == bindingContext.lastRevision && listView.sourceIncludesArraySize == m_LastSourceIncludesArraySize)
                    return;

                if (bindingContext.IsValid() && IsPropertyValid())
                {
                    m_LastUpdatedRevision = bindingContext.lastRevision;

                    int currentArraySize = m_ArraySize.intValue;
                    if (currentArraySize != m_ListViewArraySize || listView.sourceIncludesArraySize != m_LastSourceIncludesArraySize)
                    {
                        UpdateArraySize();
                    }

                    return;
                }
            }
            catch (ArgumentNullException)
            {
                //this can happen when serializedObject has been disposed of
            }
            finally
            {
                isUpdating = false;
            }

            // We unbind here
            Release();
        }
    }

    class SerializedObjectListReorderableDragAndDropController : ListViewReorderableDragAndDropController
    {
        private SerializedObjectList objectList => m_ListView.itemsSource as SerializedObjectList;

        public SerializedObjectListReorderableDragAndDropController(ListView listView)
            : base(listView) {}

        public override void OnDrop(IListDragAndDropArgs args)
        {
            switch (args.dragAndDropPosition)
            {
                case DragAndDropPosition.OutsideItems:
                case DragAndDropPosition.BetweenItems:
                    // we're ok'
                    break;
                default:
                    throw new ArgumentException($"{args.dragAndDropPosition} is not supported by {nameof(SerializedObjectListReorderableDragAndDropController)}.");
            }

            base.OnDrop(args);
        }
    }

    internal class SerializedObjectList : IList
    {
        public SerializedProperty ArrayProperty { get; private set; }
        public SerializedProperty ArraySize { get; private set; }

        List<SerializedProperty> properties;

        public SerializedObjectList(SerializedProperty parentProperty, bool includeArraySize)
        {
            RefreshProperties(parentProperty, includeArraySize);
        }

        public void RefreshProperties(SerializedProperty parentProperty, bool includeArraySize)
        {
            ArrayProperty = parentProperty.Copy();
            var property = ArrayProperty.Copy();
            var endProperty = property.GetEndProperty();

            property.NextVisible(true); // Expand the first child.

            properties = new List<SerializedProperty>();
            do
            {
                if (SerializedProperty.EqualContents(property, endProperty))
                    break;

                if (property.propertyType == SerializedPropertyType.ArraySize)
                {
                    ArraySize = property.Copy();
                    if (includeArraySize)
                    {
                        properties.Add(ArraySize);
                    }
                }
                else
                {
                    properties.Add(property.Copy());
                }
            }
            while (property.NextVisible(false));   // Never expand children.

            if (ArraySize == null)
            {
                throw new ArgumentException("Can't find array size property!");
            }
        }

        public object this[int index]
        {
            get { return properties[index]; }
            set { throw new NotImplementedException(); }
        }

        public bool IsReadOnly => true;

        public bool IsFixedSize => true;

        public int Count => properties?.Count ?? 0;

        bool ICollection.IsSynchronized
        {
            get { return (properties as ICollection).IsSynchronized; }
        }

        object ICollection.SyncRoot
        {
            get { return (properties as ICollection).SyncRoot; }
        }

        public int Add(object value)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(object value)
        {
            return IndexOf(value) >= 0;
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public IEnumerator GetEnumerator()
        {
            return properties.GetEnumerator();
        }

        public int IndexOf(object value)
        {
            var prop = value as SerializedProperty;

            if (value != null && prop != null)
            {
                return properties.IndexOf(prop);
            }

            return -1;
        }

        public void Move(int srcIndex, int destIndex)
        {
            if (srcIndex == destIndex)
                return;

            var sourceExpanded = ArrayProperty.GetArrayElementAtIndex(srcIndex).isExpanded;
            if (srcIndex < destIndex)
            {
                var currentProperty = ArrayProperty.GetArrayElementAtIndex(srcIndex);
                for (var i = srcIndex + 1; i <= destIndex; i++)
                {
                    var nextProperty = ArrayProperty.GetArrayElementAtIndex(i);
                    currentProperty.isExpanded = nextProperty.isExpanded;
                    currentProperty = nextProperty;
                }
            }
            else
            {
                var currentPropertyExpanded = ArrayProperty.GetArrayElementAtIndex(destIndex).isExpanded;
                for (var i = destIndex + 1; i <= srcIndex; i++)
                {
                    var nextProperty = ArrayProperty.GetArrayElementAtIndex(i);
                    var nextPropertyExpanded = nextProperty.isExpanded;
                    nextProperty.isExpanded = currentPropertyExpanded;
                    currentPropertyExpanded = nextPropertyExpanded;
                }
            }

            ArrayProperty.GetArrayElementAtIndex(destIndex).isExpanded = sourceExpanded;
            ArrayProperty.MoveArrayElement(srcIndex, destIndex);
        }

        public void ApplyChanges()
        {
            ArrayProperty.serializedObject.ApplyModifiedProperties();
        }

        public void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public void Remove(object value)
        {
            RemoveAt(IndexOf(value));
        }

        public void RemoveAt(int index) => RemoveAt(index, Count);

        public void RemoveAt(int index, int listCount)
        {
            if (index >= 0 && index < listCount)
            {
                var newCount = listCount - 1;
                ArrayProperty.DeleteArrayElementAtIndex(index);

                if (index < newCount - 1)
                {
                    var currentProperty = ArrayProperty.GetArrayElementAtIndex(index);
                    for (var i = index + 1; i < newCount; i++)
                    {
                        var nextProperty = ArrayProperty.GetArrayElementAtIndex(i);
                        if (nextProperty != null && currentProperty != null)
                        {
                            currentProperty.isExpanded = nextProperty.isExpanded;
                            currentProperty = nextProperty;
                        }
                    }
                }
            }
        }
    }
}
