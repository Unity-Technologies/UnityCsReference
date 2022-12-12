// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Collections;

namespace UnityEditor.UIElements.Bindings
{
    class ListViewSerializedObjectBinding : SerializedObjectBindingBase, IInternalListViewBinding
    {
        public static ObjectPool<ListViewSerializedObjectBinding> s_Pool =
            new ObjectPool<ListViewSerializedObjectBinding>(() => new ListViewSerializedObjectBinding(), 32);

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
        Action<VisualElement> m_DefaultDestroyItem;
        EventCallback<SerializedObjectBindEvent> m_SerializedObjectBindEventCallback;

        public static void CreateBind(ListView listView,
            SerializedObjectBindingContext context,
            SerializedProperty prop)
        {
            var newBinding = s_Pool.Get();
            newBinding.isReleased = false;
            newBinding.SetBinding(listView, context, prop);
        }

        protected void SetBinding(ListView listView, SerializedObjectBindingContext context,
            SerializedProperty prop)
        {
            m_DataList = new SerializedObjectList(prop, listView.sourceIncludesArraySize);
            m_ArraySize = m_DataList.ArraySize;

            m_DefaultMakeItem = MakeListViewItem;
            m_DefaultBindItem = BindListViewItem;
            m_DefaultUnbindItem = UnbindListViewItem;
            m_DefaultDestroyItem = DestroyListViewItem;
            m_SerializedObjectBindEventCallback = SerializedObjectBindEventCallback;

            m_ListViewArraySize = m_DataList.ArraySize.intValue;
            m_LastSourceIncludesArraySize = listView.sourceIncludesArraySize;
            SetListView(listView);
            SetContext(context, m_ArraySize);

            listView.RefreshItems();
        }

        private void SetListView(ListView lv)
        {
            if (listView != null)
            {
                listView.itemsSource = null;
                listView.Rebuild();

                if (listView.bindItem == m_DefaultBindItem)
                    listView.SetBindItemWithoutNotify(null);
                if (listView.makeItem == m_DefaultMakeItem)
                    listView.SetMakeItemWithoutNotify(null);
                if (listView.unbindItem == m_DefaultUnbindItem)
                    listView.unbindItem = null;
                if (listView.destroyItem == m_DefaultDestroyItem)
                    listView.destroyItem = null;

                listView.SetViewController(null);
                listView.SetDragAndDropController(null);

                var foldoutInput = listView.headerFoldout?.toggle?.visualInput;
                if (foldoutInput != null)
                {
                    foldoutInput.UnregisterCallback<DragUpdatedEvent>(OnDragUpdated);
                    foldoutInput.UnregisterCallback<DragPerformEvent>(OnDragPerform);
                }
            }

            listView = lv;

            if (listView != null)
            {
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

                if (listView.destroyItem == null)
                {
                    listView.destroyItem = m_DefaultDestroyItem;
                }

                listView.SetViewController(new EditorListViewController());
                listView.SetDragAndDropController(new SerializedObjectListReorderableDragAndDropController(listView));
                listView.itemsSource = m_DataList;

                var foldoutInput = listView.headerFoldout?.toggle?.visualInput;
                if (foldoutInput != null)
                {
                    foldoutInput.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
                    foldoutInput.RegisterCallback<DragPerformEvent>(OnDragPerform);
                }
            }
        }

        VisualElement MakeListViewItem()
        {
            var prop = new PropertyField();
            prop.RegisterCallback(m_SerializedObjectBindEventCallback);
            return prop;
        }

        void DestroyListViewItem(VisualElement element)
        {
            element.UnregisterCallback(m_SerializedObjectBindEventCallback);
        }

        void BindListViewItem(VisualElement ve, int index)
        {
            if (m_ListViewArraySize != -1 && m_ArraySize.intValue != m_ListViewArraySize)
            {
                // We need to wait for array size to be updated, which triggers a refresh anyway.
                return;
            }

            if (ve is not IBindable field)
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

            // No need to rebind to the same path. We should use a Rebuild if we need to force it.
            if (field.bindingPath == itemProp.propertyPath)
                return;

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

            if (ve is not IBindable field)
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
            if (!m_IsBinding)
            {
                evt.PreventDefault();
                evt.StopPropagation();
            }
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
            m_DataList.RefreshProperties(listView.sourceIncludesArraySize);
            m_ArraySize = m_DataList.ArraySize;
            m_ListViewArraySize = m_ArraySize.intValue;
            m_LastSourceIncludesArraySize = listView.sourceIncludesArraySize;

            var isOverMaxMultiEditLimit = m_DataList.IsOverMaxMultiEditLimit;
            listView.footer?.SetEnabled(!isOverMaxMultiEditLimit);
            listView.SetOverMaxMultiEditLimit(isOverMaxMultiEditLimit, m_DataList.ArrayProperty.serializedObject.maxArraySizeForMultiEditing);

            listView.RefreshItems();

            if (listView.arraySizeField != null)
                listView.arraySizeField.showMixedValue = m_ArraySize.hasMultipleDifferentValues;
        }

        public override void Release()
        {
            isReleased = true;

            SetListView(null);

            ResetContext();
            m_DataList = null;
            m_ArraySize = null;
            m_ListViewArraySize = -1;

            ResetCachedValues();
            s_Pool.Release(this);
        }

        private bool m_LastSourceIncludesArraySize;

        protected override void ResetCachedValues()
        {
            m_ListViewArraySize = -1;
            UpdateFieldIsAttached();
        }

        public override void OnPropertyValueChanged(SerializedProperty currentPropertyIterator)
        {
            if (isReleased)
            {
                return;
            }

            try
            {
                isUpdating = true;
                UpdateArraySize();
            }
            catch (ArgumentNullException)
            {
                //this can happen when serializedObject has been disposed of
            }
            finally
            {
                isUpdating = false;
            }
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

                if (!IsSynced())
                    return;

                isUpdating = true;

                var currentArraySize = m_ArraySize.intValue;
                var listViewShowsMixedValue = listView.arraySizeField is {showMixedValue: true};
                if (listViewShowsMixedValue ||
                    (listView.arraySizeField == null || int.Parse(listView.arraySizeField.value) == currentArraySize) &&
                    listView.sourceIncludesArraySize == m_LastSourceIncludesArraySize)
                    return;
                if (currentArraySize != m_ListViewArraySize ||
                    listView.sourceIncludesArraySize != m_LastSourceIncludesArraySize)
                {
                    UpdateArraySize();
                }

                return;
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
            ArrayProperty = parentProperty.Copy();
            RefreshProperties(includeArraySize);
        }

        public void RefreshProperties(bool includeArraySize)
        {
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

        public int Count => properties.Count;

        bool ICollection.IsSynchronized
        {
            get { return (properties as ICollection).IsSynchronized; }
        }

        object ICollection.SyncRoot
        {
            get { return (properties as ICollection).SyncRoot; }
        }

        internal bool IsOverMaxMultiEditLimit => ArrayProperty.minArraySize > ArrayProperty.serializedObject.maxArraySizeForMultiEditing && ArrayProperty.serializedObject.isEditingMultipleObjects;

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

            ArrayProperty.MoveArrayElement(srcIndex, destIndex);
            EditorGUIUtility.MoveArrayExpandedState(ArrayProperty, srcIndex, destIndex);
            RefreshProperties(properties.Count > 0 && properties[0] == ArraySize);
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

        public void RemoveAt(int index)
        {
            if (index >= 0 && index < Count)
            {
                var currentProperty = ArrayProperty.GetArrayElementAtIndex(index);
                for (var i = index + 1; i < ArraySize.intValue; i++)
                {
                    var nextProperty = ArrayProperty.GetArrayElementAtIndex(i);
                    if (nextProperty != null)
                    {
                        currentProperty.isExpanded = nextProperty.isExpanded;
                        currentProperty = nextProperty;
                    }
                }

                ArrayProperty.DeleteArrayElementAtIndex(index);
            }
        }
    }
}
