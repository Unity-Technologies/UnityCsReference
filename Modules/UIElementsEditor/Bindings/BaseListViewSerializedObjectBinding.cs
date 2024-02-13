// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Bindings
{
    class SerializedObjectListReorderableDragAndDropController : ListViewReorderableDragAndDropController
    {
        private SerializedObjectList objectList => m_ListView.itemsSource as SerializedObjectList;

        public SerializedObjectListReorderableDragAndDropController(BaseListView baseListView)
            : base(baseListView) {}

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

    internal class SerializedObjectList : ISerializedObjectList
    {
        public SerializedProperty ArrayProperty { get; private set; }
        public SerializedProperty ArraySize { get; private set; }

        List<SerializedProperty> properties;

        public SerializedObjectList(SerializedProperty parentProperty)
        {
            ArrayProperty = parentProperty.Copy();
            RefreshProperties();
        }

        public void RefreshProperties()
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

        public int Count
        {
            get
            {
                if (ArrayProperty.serializedObject.isEditingMultipleObjects)
                {
                   if (IsOverMaxMultiEditLimit)
                        return 0;

                   return ArrayProperty.minArraySize;
                }
                return properties != null ? properties.Count : 0;
            }
        }

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
            RefreshProperties();
        }

        public int minArraySize => ArrayProperty.minArraySize;
        public int arraySize
        {
            get => ArrayProperty.arraySize;
            set => ArrayProperty.arraySize = value;
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

    abstract class BaseListViewSerializedObjectBinding : SerializedObjectBindingBase
    {
        protected SerializedObjectList m_DataList;
        protected EventCallback<DragUpdatedEvent> m_DragUpdatedCallback;
        protected EventCallback<DragPerformEvent> m_DragPerformCallback;
        protected EventCallback<SerializedObjectBindEvent> m_SerializedObjectBindEventCallback;
        protected Func<VisualElement> m_DefaultMakeItem;
        protected Action<VisualElement, int> m_DefaultBindItem;
        protected Action<VisualElement, int> m_DefaultUnbindItem;

        SerializedProperty m_ArraySize;
        int m_ListViewArraySize;

        BaseListView baseListView
        {
            get => boundElement as BaseListView;
            set => boundElement = value;
        }

        protected override string bindingId { get; } = BindingExtensions.s_SerializedBindingId;

        protected BaseListViewSerializedObjectBinding()
        {
            m_DefaultMakeItem = MakeItem;
            m_DefaultUnbindItem = UnbindListViewItem;
            m_DragUpdatedCallback = OnDragUpdated;
            m_DragPerformCallback = OnDragPerform;
            m_SerializedObjectBindEventCallback = SerializedObjectBindEventCallback;
        }

        void SerializedObjectBindEventCallback(SerializedObjectBindEvent evt)
        {
            // Prevents list view items to be bound to the parent's serialized object.
            // Binding is happening on a per item basis, either from Unity or from users, so we want to stop binding the hierarchy tree from here.
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
                baseListView.viewController.AddItems(1);
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

        public override void OnRelease()
        {
            if (isReleased)
                return;

            isReleased = true;

            ResetContext();
            m_DataList = null;
            m_ArraySize = null;
            m_ListViewArraySize = -1;

            ClearView();

            ResetCachedValues();
            PoolRelease();
        }

        public override BindingResult OnUpdate(in BindingContext context)
        {
            if (isReleased)
            {
                return new BindingResult(BindingStatus.Pending);
            }

            try
            {
                ResetUpdate();

                if (!IsSynced())
                {
                    return new BindingResult(BindingStatus.Pending);
                }

                var currentArraySize = m_ArraySize.intValue;
                var listViewShowsMixedValue = baseListView.arraySizeField is {showMixedValue: true};
                if (listViewShowsMixedValue || baseListView.arraySizeField == null)
                    return default;

                if (currentArraySize != m_ListViewArraySize)
                {
                    UpdateArraySize();
                }

                return default;
            }
            catch (NullReferenceException e) when (e.Message.Contains("SerializedObject of SerializedProperty has been Disposed."))
            {
                //this can happen when serializedObject has been disposed of
            }

            // We unbind here
            Unbind();
            return new BindingResult(BindingStatus.Failure, "Failed to update ListView binding.");
        }

        public override void OnPropertyValueChanged(SerializedProperty currentPropertyIterator)
        {
            if (isReleased)
            {
                return;
            }

            try
            {
                UpdateArraySize();
            }
            catch (NullReferenceException e) when (e.Message.Contains("SerializedObject of SerializedProperty has been Disposed."))
            {
                //this can happen when serializedObject has been disposed of
            }
        }

        protected override void ResetCachedValues()
        {
            m_ListViewArraySize = -1;
            UpdateFieldIsAttached();
        }

        private void UpdateArraySize()
        {
            m_DataList.RefreshProperties();
            m_ArraySize = m_DataList.ArraySize;
            m_ListViewArraySize = m_ArraySize.intValue;

            var isOverMaxMultiEditLimit = m_DataList.IsOverMaxMultiEditLimit;
            baseListView.footer?.SetEnabled(!isOverMaxMultiEditLimit);
            baseListView.SetOverMaxMultiEditLimit(isOverMaxMultiEditLimit, m_DataList.ArrayProperty.serializedObject.maxArraySizeForMultiEditing);

            baseListView.RefreshItems();

            if (baseListView.arraySizeField != null)
                baseListView.arraySizeField.showMixedValue = m_ArraySize.hasMultipleDifferentValues;
        }

        public static bool CreateBind(BaseListView baseListView,
            SerializedObjectBindingContext context,
            SerializedProperty prop)
        {
            var newBinding = baseListView switch
            {
                ListView => ListViewSerializedObjectBinding.GetFromPool(),
                MultiColumnListView => MultiColumnListViewSerializedObjectBinding.GetFromPool(),
                _ => null
            };

            if (newBinding != null)
            {
                newBinding.isReleased = false;
                baseListView.SetBinding(BindingExtensions.s_SerializedBindingId, newBinding);
                newBinding.SetBinding(baseListView, context, prop);
                return true;
            }

            return false;
        }

        private void SetBinding(BaseListView targetList, SerializedObjectBindingContext context,
            SerializedProperty prop)
        {
            m_DataList = new SerializedObjectList(prop);
            m_ArraySize = m_DataList.ArraySize;
            m_ListViewArraySize = m_DataList.ArraySize.intValue;

            SetView(targetList);
            SetContext(context, m_ArraySize);

            targetList.RefreshItems();
        }

        private void SetView(BaseListView view)
        {
            if (baseListView != null)
            {
                Debug.LogError("[UI Toolkit] Internal ListViewBindings error. Please report this with Help -> Report a bug...");
                return;
            }

            baseListView = view;
            baseListView.SetProperty(BaseVerticalCollectionView.internalBindingKey, this);
            var parentField = baseListView.GetProperty(PropertyField.listViewBoundFieldProperty);

            SetDefaultCallbacks();

            // We prevent hierarchy binding under the contentContainer.
            baseListView.scrollView.contentContainer.RegisterCallback(m_SerializedObjectBindEventCallback);

            // ListViews instantiated by users are driven by users. We only change the reordering options if the user
            // has used a PropertyField to display the list. (Cases UUM-33402 and UUM-27687)
            var isReorderable = baseListView.reorderable;
            if (parentField != null)
            {
                isReorderable = PropertyHandler.IsArrayReorderable(m_DataList.ArrayProperty);
                baseListView.reorderMode = isReorderable ? ListViewReorderMode.Animated : ListViewReorderMode.Simple;
            }

            SetEditorViewController();
            baseListView.SetDragAndDropController(new SerializedObjectListReorderableDragAndDropController(baseListView)
            {
                enableReordering = isReorderable,
            });

            baseListView.itemsSource = m_DataList;
            baseListView.SetupArraySizeField();

            var foldoutInput = baseListView.headerFoldout?.toggle?.visualInput;
            if (foldoutInput != null)
            {
                foldoutInput.RegisterCallback(m_DragUpdatedCallback);
                foldoutInput.RegisterCallback(m_DragPerformCallback);
            }
        }

        protected void BindListViewItem(VisualElement ve, int index)
        {
            var item = m_DataList[index];
            var itemProp = item as SerializedProperty;

            BindListViewItem(ve, itemProp.propertyPath);
        }

        protected void BindListViewItem(VisualElement ve, string propertyPath)
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

            field.bindingPath = propertyPath;
            bindingContext.ContinueBinding(ve, null);
        }

        private void UnbindListViewItem(VisualElement ve, int index)
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

        private void ClearView()
        {
            if (baseListView == null)
            {
                Debug.LogError("[UI Toolkit] Internal ListViewBindings error during release. Please report this with Help -> Report a bug...");
                return;
            }

            baseListView.SetProperty(BaseVerticalCollectionView.internalBindingKey, null);
            baseListView.itemsSource = null;
            baseListView.SetupArraySizeField();
            baseListView.Rebuild();

            ResetCallbacks();

            baseListView.scrollView.contentContainer.UnregisterCallback(m_SerializedObjectBindEventCallback);

            baseListView.SetViewController(null);

            var foldoutInput = baseListView.headerFoldout?.toggle?.visualInput;
            if (foldoutInput != null)
            {
                foldoutInput.UnregisterCallback(m_DragUpdatedCallback);
                foldoutInput.UnregisterCallback(m_DragPerformCallback);
            }

            baseListView = null;
        }

        protected abstract void SetEditorViewController();

        protected abstract VisualElement MakeItem();

        protected abstract void SetDefaultCallbacks();

        protected abstract bool HasDefaultBindItem();

        protected abstract void ResetCallbacks();

        protected abstract void PoolRelease();
    }
}
