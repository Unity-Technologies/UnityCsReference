// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A custom ListView for editing Lists/Arrays in the Graph Inspector.
    /// </summary>
    internal class ListPropertyField : ListView
    {
        public new static readonly string ussClassName = "ge-list-view";
        public static readonly string itemContainerUssClassName = ussClassName.WithUssElement("item-container");
        public static readonly string indexLabelUssClassName = ussClassName.WithUssElement("index-label");
        public static readonly string valueFieldUssClassName = ussClassName.WithUssElement("value-field");

        private class ListElementData
        {
            public int Index;
            public IList List;
            public Action<IList> DispatchChange;
            public Action<VisualElement, object> BindValue;
        }

        private readonly Type m_ElementType;
        private readonly bool m_IsArray;
        private readonly Action<IList> m_OnListChanged;

        public ListPropertyField(Type listType, Action<IList> onListChanged)
        {
            m_ElementType = listType.IsArray 
                ? listType.GetElementType() 
                : (listType.IsGenericType ? listType.GetGenericArguments()[0] : typeof(object));
            
            m_IsArray = listType.IsArray;
            m_OnListChanged = onListChanged;

            AddToClassList(ussClassName);
            
            // ListView Configuration
            virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            fixedItemHeight = 20;
            showFoldoutHeader = true;
            showBoundCollectionSize = true;
            showAddRemoveFooter = true;
            showBorder = true;
            reorderable = true;
            headerTitle = TypeHelpers.GetFriendlyName(listType);

            // Setup Callbacks
            makeItem = MakeItem;
            bindItem = BindItem;
            itemsAdded += OnItemsAdded;
            itemsRemoved += OnItemsRemoved;
            itemIndexChanged += OnItemIndexChanged;

            // Custom Add Logic (to handle null strings and defaults)
            onAdd = OnAdd;
        }

        /// <summary>
        /// Updates the list content, reusing the existing list instance if possible to avoid allocations.
        /// </summary>
        public void SetValue(IList sourceList)
        {
            var viewListType = typeof(List<>).MakeGenericType(m_ElementType);

            if (itemsSource != null && IsListContentEqual(itemsSource as IList, sourceList))
            {
                return;
            }

            if (itemsSource != null && itemsSource.GetType() == viewListType)
            {
                var viewList = (IList)itemsSource;
                viewList.Clear();
                
                if (sourceList != null)
                {
                    foreach (var item in sourceList)
                    {
                        viewList.Add(item);
                    }
                }
                
                // Only refresh if data actually changed
                RefreshItems();
            }
            else
            {
                if (sourceList != null)
                    itemsSource = (IList)Activator.CreateInstance(viewListType, sourceList);
                else
                    itemsSource = (IList)Activator.CreateInstance(viewListType);
                    
                Rebuild();
            }
        }

        private bool IsListContentEqual(IList listA, IList listB)
        {
            if (ReferenceEquals(listA, listB)) return true;
            if (listA == null || listB == null) return false;
            if (listA.Count != listB.Count) return false;

            for (int i = 0; i < listA.Count; i++)
            {
                if (!object.Equals(listA[i], listB[i]))
                {
                    return false;
                }
            }
            return true;
        }

        void OnAdd(VisualElement view)
        {
            var list = itemsSource as IList;
            if (list == null) return;

            object itemToAdd = m_ElementType == typeof(string) 
                ? "" 
                : (m_ElementType.IsValueType ? Activator.CreateInstance(m_ElementType) : null);
            
            list.Add(itemToAdd);
            
            RefreshItems();
            ScrollToItem(-1);
            DispatchChange(list);
        }

        void DispatchChange(IList updatedList)
        {
            object finalValue;

            if (m_IsArray)
            {
                var arr = Array.CreateInstance(m_ElementType, updatedList.Count);
                updatedList.CopyTo(arr, 0);
                finalValue = arr;
            }
            else
            {
                var listType = typeof(List<>).MakeGenericType(m_ElementType);
                finalValue = Activator.CreateInstance(listType, updatedList);
            }
            
            m_OnListChanged?.Invoke(finalValue as IList);
        }

        void OnItemsAdded(IEnumerable<int> indices) => schedule.Execute(() => DispatchChange(itemsSource as IList));
        void OnItemsRemoved(IEnumerable<int> indices) => schedule.Execute(() => DispatchChange(itemsSource as IList));
        void OnItemIndexChanged(int i, int j) => DispatchChange(itemsSource as IList);

        VisualElement MakeItem()
        {
            var container = new VisualElement();
            container.AddToClassList(itemContainerUssClassName);

            var label = new Label();
            label.AddToClassList(indexLabelUssClassName);

            var field = CreateFieldForElementType();
            
            container.Add(label);
            container.Add(field);
            return container;
        }

        void BindItem(VisualElement element, int index)
        {
            var list = itemsSource as IList;
            if (list == null || index < 0 || index >= list.Count) return;

            element.Q<Label>().text = index.ToString();

            var field = element.ElementAt(1);
            var data = field.userData as ListElementData;

            data.List = list;
            data.Index = index;

            data.DispatchChange = l => DispatchChange(l);

            data.BindValue(field, list[index]);
        }

        VisualElement CreateFieldForElementType()
        {
            // Primitives
            if (m_ElementType == typeof(int)) return CreateAndSetupField(() => new IntegerField { isDelayed = true });
            if (m_ElementType == typeof(float)) return CreateAndSetupField(() => new FloatField { isDelayed = true });
            if (m_ElementType == typeof(double)) return CreateAndSetupField(() => new DoubleField { isDelayed = true });
            if (m_ElementType == typeof(long)) return CreateAndSetupField(() => new LongField { isDelayed = true });
            if (m_ElementType == typeof(string)) return CreateAndSetupField(() => new TextField { isDelayed = true });
            if (m_ElementType == typeof(bool)) return CreateAndSetupField(() => new Toggle());

            // Special Primitives
            if (m_ElementType == typeof(char))
            {
                // Char renders as a TextField with length 1
                var field = new TextField { maxLength = 1, isDelayed = true };
                field.AddToClassList(valueFieldUssClassName);
                
                field.RegisterCallback<ChangeEvent<string>>(evt => 
                {
                    if (evt.target is VisualElement ve && ve.userData is ListElementData data)
                    {
                        char newVal = string.IsNullOrEmpty(evt.newValue) ? default : evt.newValue[0];
                        data.List[data.Index] = newVal;
                        data.DispatchChange?.Invoke(data.List);
                    }
                });
                
                field.userData = new ListElementData { 
                    BindValue = (elem, val) => ((TextField)elem).SetValueWithoutNotify(val.ToString()) 
                };
                return field;
            }

            // Unity Structs
            if (m_ElementType == typeof(Vector2)) return CreateAndSetupField(() => new Vector2Field());
            if (m_ElementType == typeof(Vector3)) return CreateAndSetupField(() => new Vector3Field());
            if (m_ElementType == typeof(Vector4)) return CreateAndSetupField(() => new Vector4Field());
            if (m_ElementType == typeof(Vector2Int)) return CreateAndSetupField(() => new Vector2IntField());
            if (m_ElementType == typeof(Vector3Int)) return CreateAndSetupField(() => new Vector3IntField());
            if (m_ElementType == typeof(Rect)) return CreateAndSetupField(() => new RectField());
            if (m_ElementType == typeof(RectInt)) return CreateAndSetupField(() => new RectIntField()); 
            if (m_ElementType == typeof(Bounds)) return CreateAndSetupField(() => new BoundsField());
            if (m_ElementType == typeof(BoundsInt)) return CreateAndSetupField(() => new BoundsIntField());
            if (m_ElementType == typeof(Color)) return CreateAndSetupField(() => new ColorField());
            if (m_ElementType == typeof(AnimationCurve)) return CreateAndSetupField(() => new CurveField());
            if (m_ElementType == typeof(Gradient)) return CreateAndSetupField(() => new GradientField());

            // LayerMask
            if (m_ElementType == typeof(LayerMask))
            {
                var field = new LayerMaskField();
                field.AddToClassList(valueFieldUssClassName);

                field.RegisterCallback<ChangeEvent<int>>(evt =>
                {
                    if (evt.target is VisualElement ve && ve.userData is ListElementData data)
                    {
                        // Convert int back to LayerMask struct for the list
                        LayerMask newVal = evt.newValue;
                        data.List[data.Index] = newVal;
                        data.DispatchChange?.Invoke(data.List);
                    }
                });

                field.userData = new ListElementData { 
                    // Convert LayerMask struct to int for the field
                    BindValue = (elem, val) => ((LayerMaskField)elem).SetValueWithoutNotify(((LayerMask)val).value) 
                };
                return field;
            }

            // Enums
            if (m_ElementType.IsEnum)
            {
                return CreateAndSetupField<Enum>(() => new EnumField((Enum)Activator.CreateInstance(m_ElementType)));
            }

            // Objects
            // GameObject specifically allows scene objects
            if (m_ElementType == typeof(GameObject))
            {
                return CreateAndSetupField<Object>(() => new ObjectField { objectType = m_ElementType, allowSceneObjects = true });
            }

            // General Objects do not
            if (typeof(Object).IsAssignableFrom(m_ElementType))
            {
                return CreateAndSetupField<Object>(() => new ObjectField { objectType = m_ElementType, allowSceneObjects = false });
            }

            // Fallback (Label)
            var label = new Label();
            label.AddToClassList(valueFieldUssClassName);
            
            Action<VisualElement, object> labelBinder = (elem, val) =>
            {
                ((Label)elem).text = val?.ToString() ?? "null";
                elem.SetEnabled(false);
            };
            label.userData = new ListElementData { BindValue = labelBinder };

            return label;
        }

        VisualElement CreateAndSetupField<T>(Func<BaseField<T>> constructor)
        {
            var field = constructor();
            field.AddToClassList(valueFieldUssClassName);

            // Event
            field.RegisterCallback<ChangeEvent<T>>(evt =>
            {
                if (evt.target is VisualElement ve && ve.userData is ListElementData data)
                {
                    data.List[data.Index] = evt.newValue;
                    data.DispatchChange?.Invoke(data.List);
                }
            });

            // Binder
            Action<VisualElement, object> binder = (elem, val) =>
            {
                ((BaseField<T>)elem).SetValueWithoutNotify((T)val);
            };

            field.userData = new ListElementData { BindValue = binder };
            return field;
        }
    }
}
