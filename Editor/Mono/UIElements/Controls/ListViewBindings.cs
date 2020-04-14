// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace UnityEditor.UIElements
{
    class ListViewSerializedObjectBinding : DefaultSerializedObjectBindingImplementation.SerializedObjectBindingBase
    {
        ListView listView { get { return boundElement as ListView; } set { boundElement = value; } }

        SerializedObjectList m_DataList;

        SerializedProperty m_ArraySize;
        int m_ListViewArraySize;

        public static void CreateBind(ListView listView,
            DefaultSerializedObjectBindingImplementation.SerializedObjectUpdateWrapper objWrapper,
            SerializedProperty prop)
        {
            var newBinding = new ListViewSerializedObjectBinding();
            newBinding.SetBinding(listView, objWrapper, prop);
        }

        protected void SetBinding(ListView listView,
            DefaultSerializedObjectBindingImplementation.SerializedObjectUpdateWrapper objWrapper,
            SerializedProperty prop)
        {
            boundObject = objWrapper;
            boundProperty = prop;
            boundPropertyPath = prop.propertyPath;

            m_DataList = new SerializedObjectList(prop, listView.showBoundCollectionSize);
            m_ArraySize = m_DataList.ArraySize;
            m_ListViewArraySize = m_DataList.ArraySize.intValue;
            this.listView = listView;

            if (listView.makeItem == null)
            {
                listView.makeItem = () => MakeListViewItem();
            }

            if (listView.bindItem == null)
            {
                listView.bindItem = (v, i) => BindListViewItem(v, i);
            }

            listView.itemsSource = m_DataList;

            listView.SetDragAndDropController(new SerializedObjectListReorderableDragAndDropController(listView));
        }

        VisualElement MakeListViewItem()
        {
            return new PropertyField();
        }

        void BindListViewItem(VisualElement ve, int index)
        {
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

            object item = listView.itemsSource[index];
            var itemProp = item as SerializedProperty;
            field.bindingPath = itemProp.propertyPath;
            BindingExtensions.bindingImpl.Bind(ve, boundObject, itemProp);
        }

        void UpdateArraySize()
        {
            m_DataList.RefreshProperties(boundProperty, listView.showBoundCollectionSize);
            m_ArraySize = m_DataList.ArraySize;
            m_ListViewArraySize = m_ArraySize.intValue;
            listView.Refresh();
        }

        public override void Release()
        {
            isReleased = true;
        }

        private UInt64 lastUpdatedRevision = 0xFFFFFFFFFFFFFFFF;

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

                if (boundObject.IsValid() && IsPropertyValid())
                {
                    if (lastUpdatedRevision == boundObject.LastRevision)
                    {
                        //nothing to do
                        return;
                    }

                    lastUpdatedRevision = boundObject.LastRevision;

                    int currentArraySize = m_ArraySize.intValue;

                    if (currentArraySize != m_ListViewArraySize)
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
        public SerializedObjectListReorderableDragAndDropController(ListView listView) : base(listView)
        {
        }

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

            var array = objectList;
            var selection = m_ListView.selectedIndices.OrderBy((i) => i).ToArray();

            var baseOffset = 0;
            if (m_ListView.showBoundCollectionSize)
            {
                //we must offset everything by 1
                baseOffset = -1;
            }

            var insertIndex = args.insertAtIndex + baseOffset;

            int insertIndexShift = 0;
            int srcIndexShift = 0;
            for (int i = selection.Length - 1; i >= 0; --i)
            {
                var index = selection[i] + baseOffset;

                if (index < 0)
                    continue;

                var newIndex = insertIndex - insertIndexShift;

                if (index > insertIndex)
                {
                    index += srcIndexShift;
                    srcIndexShift++;
                }
                else if (index < newIndex)
                {
                    insertIndexShift++;
                    newIndex--;
                }

                array.Move(index, newIndex);

                onItemMoved?.Invoke(new ItemMoveArgs<object>
                {
                    item = objectList[index],
                    newIndex = newIndex,
                    previousIndex = index
                });
            }

            array.ApplyChanges();

            var newSelection = new List<int>();

            for (int i = 0; i < selection.Length; ++i)
            {
                newSelection.Add(insertIndex - insertIndexShift + i - baseOffset);
            }

            m_ListView.SetSelectionWithoutNotify(newSelection);
        }
    }


    internal class SerializedObjectList : IList
    {
        public SerializedProperty ArrayProperty { get; private set; }
        public SerializedProperty ArraySize {get; private set;}

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
            while (property.NextVisible(false)); // Never expand children.

            if (ArraySize == null)
            {
                throw new ArgumentException("Can't find array size property!");
            }
        }

        public object this[int index]
        {
            get
            {
                return properties[index];
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool IsReadOnly => true;

        public bool IsFixedSize => true;

        public int Count => properties.Count;

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
            var index = IndexOf(value);
            if (index >= 0)
            {
                RemoveAt(index);
            }
        }

        public void RemoveAt(int index)
        {
            ArrayProperty.DeleteArrayElementAtIndex(index);
        }
    }
}
