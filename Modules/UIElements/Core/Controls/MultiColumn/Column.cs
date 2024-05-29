// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.Internal;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents a specific data of a column.
    /// </summary>
    internal enum ColumnDataType
    {
        /// <summary>
        /// Represents the name of a column.
        /// </summary>
        Name,
        /// <summary>
        /// Represents the title of a column.
        /// </summary>
        Title,
        /// <summary>
        /// Represents the icon of a column.
        /// </summary>
        Icon,
        /// <summary>
        /// Represents the visibility of a column.
        /// </summary>
        Visibility,
        /// <summary>
        /// Represents the desired with of a column.
        /// </summary>
        Width,
        /// <summary>
        /// Represents the maximum width of a column.
        /// </summary>
        MaxWidth,
        /// <summary>
        /// Represents the minimum width of a column.
        /// </summary>
        MinWidth,
        /// <summary>
        /// Represents the ability to stretch of a column.
        /// </summary>
        Stretchable,
        /// <summary>
        /// Represents the ability to sort of a column.
        /// </summary>
        Sortable,
        /// <summary>
        /// Represents the ability for user to interactively show or hide a column.
        /// </summary>
        Optional,
        /// <summary>
        /// Represents the ability for user to interactively resize a column.
        /// </summary>
        Resizable,
        /// <summary>
        /// Represents the visual representation of a column in a header.
        /// </summary>
        HeaderTemplate,
        /// <summary>
        /// Represents the template used to instantiate cells on a column.
        /// </summary>
        CellTemplate,
    }

    /// <summary>
    /// Represents a column in multi-column views such as multi-column list view or multi-column tree view.
    /// Provides the properties to define how user interacts with a column in a multi-column view, how its data and the data of each
    /// cell in this column are represented.
    /// </summary>
    [UxmlObject]
    public class Column : INotifyBindablePropertyChanged
    {
        static readonly BindingId nameProperty = nameof(name);
        static readonly BindingId titleProperty = nameof(title);
        static readonly BindingId iconProperty = nameof(icon);
        static readonly BindingId visibleProperty = nameof(visible);
        static readonly BindingId widthProperty = nameof(width);
        static readonly BindingId minWidthProperty = nameof(minWidth);
        static readonly BindingId maxWidthProperty = nameof(maxWidth);
        static readonly BindingId sortableProperty = nameof(sortable);
        static readonly BindingId stretchableProperty = nameof(stretchable);
        static readonly BindingId optionalProperty = nameof(optional);
        static readonly BindingId resizableProperty = nameof(resizable);
        static readonly BindingId headerTemplateProperty = nameof(headerTemplate);
        static readonly BindingId cellTemplateProperty = nameof(cellTemplate);

        internal const string k_HeaderTemplateAttributeName = "header-template";
        internal const string k_CellTemplateAttributeName = "cell-template";

        [ExcludeFromDocs, Serializable]
        public class UxmlSerializedData : UIElements.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField] string name;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags name_UxmlAttributeFlags;
            [SerializeField] string title;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags title_UxmlAttributeFlags;
            [SerializeField] bool visible;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags visible_UxmlAttributeFlags;
            [SerializeField] Length width;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags width_UxmlAttributeFlags;
            [SerializeField] Length minWidth;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags minWidth_UxmlAttributeFlags;
            [SerializeField] Length maxWidth;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags maxWidth_UxmlAttributeFlags;
            [SerializeField] bool stretchable;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags stretchable_UxmlAttributeFlags;
            [SerializeField] bool sortable;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags sortable_UxmlAttributeFlags;
            [SerializeField] bool optional;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags optional_UxmlAttributeFlags;
            [SerializeField] bool resizable;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags resizable_UxmlAttributeFlags;
            [SerializeField] VisualTreeAsset headerTemplate;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags headerTemplate_UxmlAttributeFlags;
            [SerializeField] VisualTreeAsset cellTemplate;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags cellTemplate_UxmlAttributeFlags;
            [SerializeField] string bindingPath;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags bindingPath_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new Column();

            public override void Deserialize(object obj)
            {
                var e = (Column)obj;
                if (ShouldWriteAttributeValue(name_UxmlAttributeFlags))
                    e.name = name;
                if (ShouldWriteAttributeValue(title_UxmlAttributeFlags))
                    e.title = title;
                if (ShouldWriteAttributeValue(visible_UxmlAttributeFlags))
                    e.visible = visible;
                if (ShouldWriteAttributeValue(width_UxmlAttributeFlags))
                    e.width = width;
                if (ShouldWriteAttributeValue(minWidth_UxmlAttributeFlags))
                    e.minWidth = minWidth;
                if (ShouldWriteAttributeValue(maxWidth_UxmlAttributeFlags))
                    e.maxWidth = maxWidth;
                if (ShouldWriteAttributeValue(sortable_UxmlAttributeFlags))
                    e.sortable = sortable;
                if (ShouldWriteAttributeValue(stretchable_UxmlAttributeFlags))
                    e.stretchable = stretchable;
                if (ShouldWriteAttributeValue(optional_UxmlAttributeFlags))
                    e.optional = optional;
                if (ShouldWriteAttributeValue(resizable_UxmlAttributeFlags))
                    e.resizable = resizable;
                if (ShouldWriteAttributeValue(bindingPath_UxmlAttributeFlags))
                    e.bindingPath = bindingPath;
                if (ShouldWriteAttributeValue(headerTemplate_UxmlAttributeFlags) && headerTemplate != null)
                {
                    e.headerTemplate = headerTemplate;
                    e.makeHeader = () => headerTemplate.Instantiate();
                }
                if (ShouldWriteAttributeValue(cellTemplate_UxmlAttributeFlags) && cellTemplate != null)
                {
                    e.cellTemplate = cellTemplate;
                    e.makeCell = () => cellTemplate.Instantiate();
                }
            }
        }

        /// <summary>
        /// Instantiates a <see cref="Column"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlObjectFactory<T> is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        internal class UxmlObjectFactory<T> : UxmlObjectFactory<T, UxmlObjectTraits<T>> where T : Column, new() {}
        /// <summary>
        /// Instantiates a <see cref="Column"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlObjectFactory<T> is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        internal class UxmlObjectFactory : UxmlObjectFactory<Column> {}

        /// <summary>
        /// Defines <see cref="UxmlObjectTraits{T}"/> for the <see cref="Column"/>.
        /// </summary>
        [Obsolete("UxmlObjectTraits<T> is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        internal class UxmlObjectTraits<T> : UnityEngine.UIElements.UxmlObjectTraits<T> where T : Column
        {
            UxmlStringAttributeDescription m_Name = new UxmlStringAttributeDescription { name = "name" };
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "title" };
            UxmlBoolAttributeDescription m_Visible = new UxmlBoolAttributeDescription { name = "visible", defaultValue = true };
            UxmlStringAttributeDescription m_Width = new UxmlStringAttributeDescription { name = "width" };
            UxmlStringAttributeDescription m_MinWidth = new UxmlStringAttributeDescription { name = "min-width" };
            UxmlStringAttributeDescription m_MaxWidth = new UxmlStringAttributeDescription { name = "max-width" };
            UxmlBoolAttributeDescription m_Stretch = new UxmlBoolAttributeDescription { name = "stretchable" };
            UxmlBoolAttributeDescription m_Sortable = new UxmlBoolAttributeDescription { name = "sortable", defaultValue = true };
            UxmlBoolAttributeDescription m_Optional = new UxmlBoolAttributeDescription { name = "optional", defaultValue = true };
            UxmlBoolAttributeDescription m_Resizable = new UxmlBoolAttributeDescription { name = "resizable", defaultValue = true };
            UxmlStringAttributeDescription m_HeaderTemplateId = new UxmlStringAttributeDescription { name = k_HeaderTemplateAttributeName };
            UxmlStringAttributeDescription m_CellTemplateId = new UxmlStringAttributeDescription { name = k_CellTemplateAttributeName };
            UxmlStringAttributeDescription m_BindingPath = new UxmlStringAttributeDescription { name = "binding-path" };

            static Length ParseLength(string str, Length defaultValue)
            {
                // Copied from UnityEditor.UIElements.Debugger.StyleLengthField.
                float value = defaultValue.value;
                LengthUnit unit = defaultValue.unit;

                // Find unit index
                int digitEndIndex = 0;
                int unitIndex = -1;
                for (int i = 0; i < str.Length; i++)
                {
                    var c = str[i];
                    if (char.IsLetter(c) || c == '%')
                    {
                        unitIndex = i;
                        break;
                    }

                    ++digitEndIndex;
                }

                var floatStr = str.Substring(0, digitEndIndex);
                var unitStr = string.Empty;
                if (unitIndex > 0)
                    unitStr = str.Substring(unitIndex, str.Length - unitIndex).ToLowerInvariant();

                float v;
                if (float.TryParse(floatStr, out v))
                    value = v;

                switch (unitStr)
                {
                    case "px":
                        unit = LengthUnit.Pixel;
                        break;
                    case "%":
                        unit = LengthUnit.Percent;
                        break;
                    default:
                        break;
                }

                return new Length(value, unit);
            }

            /// <summary>
            /// Initialize a uxml object instance with values from the UXML element attributes.
            /// </summary>
            /// <param name="obj">The object to initialize.</param>
            /// <param name="bag">A bag of name-value pairs, one for each attribute of the UXML element.</param>
            /// <param name="cc">Contains information about the uxml objects available in the tree for the initialization step.</param>
            /// <remarks>
            /// UxmlObject are simple data classes or structs.
            /// </remarks>
            public override void Init(ref T obj, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ref obj, bag, cc);

                obj.name = m_Name.GetValueFromBag(bag, cc);
                obj.title = m_Text.GetValueFromBag(bag, cc);
                obj.visible = m_Visible.GetValueFromBag(bag, cc);

                obj.width = ParseLength(m_Width.GetValueFromBag(bag, cc), new Length());
                obj.maxWidth = ParseLength(m_MaxWidth.GetValueFromBag(bag, cc), new Length(Length.k_MaxValue));
                obj.minWidth = ParseLength(m_MinWidth.GetValueFromBag(bag, cc), new Length(kDefaultMinWidth));
                obj.sortable = m_Sortable.GetValueFromBag(bag, cc);
                obj.stretchable = m_Stretch.GetValueFromBag(bag, cc);
                obj.optional = m_Optional.GetValueFromBag(bag, cc);
                obj.resizable = m_Resizable.GetValueFromBag(bag, cc);
                obj.bindingPath = m_BindingPath.GetValueFromBag(bag, cc);
                var headerTemplateId = m_HeaderTemplateId.GetValueFromBag(bag, cc);

                if (!string.IsNullOrEmpty(headerTemplateId))
                {
                    var asset = cc.visualTreeAsset?.ResolveTemplate(headerTemplateId);
                    obj.makeHeader = () =>
                    {
                        if (asset != null)
                            return asset.Instantiate();
                        return new Label(BaseVerticalCollectionView.k_InvalidTemplateError);
                    };
                }

                var cellTemplateId = m_CellTemplateId.GetValueFromBag(bag, cc);

                if (!string.IsNullOrEmpty(cellTemplateId))
                {
                    var asset = cc.visualTreeAsset?.ResolveTemplate(cellTemplateId);
                    obj.makeCell = () =>
                    {
                        if (asset != null)
                            return asset.Instantiate();
                        return new Label(BaseVerticalCollectionView.k_InvalidTemplateError);
                    };
                }
            }
        }

        internal const float kDefaultMinWidth = 35f;

        string m_Name;

        // Display
        string m_Title;
        Background m_Icon;
        bool m_Visible = true;

        // Dimensions and Layout
        Length m_Width = 0;
        Length m_MinWidth = kDefaultMinWidth;
        Length m_MaxWidth = Length.k_MaxValue;
        float m_DesiredWidth = float.NaN;
        bool m_Stretchable;

        // Sorting
        bool m_Sortable = true;

        // User interaction enablers
        bool m_Optional = true;
        bool m_Resizable = true;

        // Header content template
        VisualTreeAsset m_HeaderTemplate;
        VisualTreeAsset m_CellTemplate;
        Func<VisualElement> m_MakeHeader;
        Action<VisualElement> m_BindHeader;
        Action<VisualElement> m_UnbindHeader;
        Action<VisualElement> m_DestroyHeader;

        // Cell template
        Func<VisualElement> m_MakeCell;
        Action<VisualElement, int> m_BindCell;
        Action<VisualElement, int> m_UnbindCellItem;

        /// <summary>
        /// Called when a property has changed.
        /// </summary>
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        /// <summary>
        /// The name of the column.
        /// </summary>
        [CreateProperty]
        public string name
        {
            get => m_Name;
            set
            {
                if (m_Name == value)
                    return;
                m_Name = value;
                NotifyChange(ColumnDataType.Name);
                NotifyPropertyChanged(nameProperty);
            }
        }

        /// <summary>
        /// The title of the column.
        /// </summary>
        [CreateProperty]
        public string title
        {
            get => m_Title;
            set
            {
                if (m_Title == value)
                    return;
                m_Title = value;
                NotifyChange(ColumnDataType.Title);
                NotifyPropertyChanged(titleProperty);
            }
        }

        /// <summary>
        /// The icon of the column.
        /// </summary>
        [CreateProperty]
        public Background icon
        {
            get => m_Icon;
            set
            {
                if (m_Icon == value)
                    return;
                m_Icon = value;
                NotifyChange(ColumnDataType.Icon);
                NotifyPropertyChanged(iconProperty);
            }
        }

        /// <summary>
        /// The comparison to use when using <see cref="ColumnSortingMode.Default"/>. Compares two items by their index in the source.
        /// </summary>
        /// <example>
        /// The following example creates a [[MultiColumnListView]] that can be sorted with the default algorithm:
        /// <code source="../../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/MultiColumnListView_DefaultSorting.cs"/>
        /// </example>
        public Comparison<int> comparison { get; set; }

        /// <summary>
        /// The position of the column within its container relative to the other columns.
        /// </summary>
        /// <remarks>Returns -1 if not found in collection.</remarks>
        internal int index
        {
            get => collection?.IndexOf(this) ?? -1;
        }

        /// <summary>
        /// The ordered position of the column within its container relative to the other columns including hidden column.
        /// </summary>
        /// <remarks>Returns -1 if not found in collection.</remarks>
        internal int displayIndex
        {
            get => (collection?.displayList as List<Column>)?.IndexOf(this) ?? -1;
        }

        /// <summary>
        /// The display position of the column within its container relative to the other visible columns.
        /// </summary>
        /// <remarks>Returns -1 if not found in collection.</remarks>
        internal int visibleIndex
        {
            get => (collection?.visibleList as List<Column>)?.IndexOf(this) ?? -1;
        }

        /// <summary>
        /// Indicates whether the column is visible.
        /// </summary>
        /// <remarks>
        /// The default value is true.
        /// </remarks>
        [CreateProperty]
        public bool visible
        {
            get => m_Visible;
            set
            {
                if (m_Visible == value)
                    return;
                m_Visible = value;
                NotifyChange(ColumnDataType.Visibility);
                NotifyPropertyChanged(visibleProperty);
            }
        }

        /// <summary>
        /// The desired width of the column.
        /// </summary>
        /// <remarks>
        /// The default value is 0.
        /// </remarks>
        [CreateProperty]
        public Length width
        {
            get => m_Width;
            set
            {
                if (m_Width == value)
                    return;
                m_Width = value;
                desiredWidth = float.NaN;
                NotifyChange(ColumnDataType.Width);
                NotifyPropertyChanged(widthProperty);
            }
        }

        /// <summary>
        /// The minimum width of the column.
        /// </summary>
        /// <remarks>
        /// The default value is 35px.
        /// </remarks>
        [CreateProperty]
        public Length minWidth
        {
            get => m_MinWidth;
            set
            {
                if (m_MinWidth == value)
                    return;
                m_MinWidth = value;
                NotifyChange(ColumnDataType.MinWidth);
                NotifyPropertyChanged(minWidthProperty);
            }
        }

        /// <summary>
        /// The maximum width of the column.
        /// </summary>
        [CreateProperty]
        public Length maxWidth
        {
            get => m_MaxWidth;
            set
            {
                if (m_MaxWidth == value)
                    return;
                m_MaxWidth = value;
                NotifyChange(ColumnDataType.MaxWidth);
                NotifyPropertyChanged(maxWidthProperty);
            }
        }

        /// <summary>
        /// The desired width of the column computed by the layout.
        /// </summary>
        internal float desiredWidth
        {
            get => m_DesiredWidth;
            set
            {
                if (m_DesiredWidth == value)
                    return;
                m_DesiredWidth = value;
                resized?.Invoke(this);
            }
        }

        /// <summary>
        /// Indicates whether the column can be sorted.
        /// </summary>
        [CreateProperty]
        public bool sortable
        {
            get => m_Sortable;
            set
            {
                if (m_Sortable == value)
                    return;
                m_Sortable = value;
                NotifyChange(ColumnDataType.Sortable);
                NotifyPropertyChanged(sortableProperty);
            }
        }

        /// <summary>
        /// Indicates whether the column will be automatically resized to fill the available space within its container.
        /// </summary>
        [CreateProperty]
        public bool stretchable
        {
            get => m_Stretchable;
            set
            {
                if (m_Stretchable == value)
                    return;
                m_Stretchable = value;
                NotifyChange(ColumnDataType.Stretchable);
                NotifyPropertyChanged(stretchableProperty);
            }
        }

        /// <summary>
        /// Indicates whether the column is optional. Optional columns be shown or hidden interactively by the user.
        /// </summary>
        [CreateProperty]
        public bool optional
        {
            get => m_Optional;
            set
            {
                if (m_Optional == value)
                    return;
                m_Optional = value;
                NotifyChange(ColumnDataType.Optional);
                NotifyPropertyChanged(optionalProperty);
            }
        }

        /// <summary>
        /// Indicates whether the column can be resized interactively by the user.
        /// </summary>
        /// <remarks>
        /// The resize behaviour of all columns in a column collection can be specified by setting <see cref="Columns.resizable"/>.
        /// A column is effectively resizable if both <see cref="Column.resizable"/> and <see cref="Columns.resizable"/> are both true.
        /// </remarks>
        [CreateProperty]
        public bool resizable
        {
            get => m_Resizable;
            set
            {
                if (m_Resizable == value)
                    return;
                m_Resizable = value;
                NotifyChange(ColumnDataType.Resizable);
                NotifyPropertyChanged(resizableProperty);
            }
        }

        /// <summary>
        /// Path of the target property to be bound.
        /// </summary>
        public string bindingPath { get; set; }

        /// <summary>
        /// The VisualElement that is the template for the header of the column.
        /// </summary>
        [CreateProperty]
        public VisualTreeAsset headerTemplate
        {
            get => m_HeaderTemplate;
            set
            {
                if (m_HeaderTemplate == value)
                    return;
                m_HeaderTemplate = value;
                NotifyChange(ColumnDataType.HeaderTemplate);
                NotifyPropertyChanged(headerTemplateProperty);
            }
        }

        /// <summary>
        /// The VisualElement that is the template for each cell of the column.
        /// </summary>
        [CreateProperty]
        public VisualTreeAsset cellTemplate
        {
            get => m_CellTemplate;
            set
            {
                if (m_CellTemplate == value)
                    return;
                m_CellTemplate = value;
                NotifyChange(ColumnDataType.CellTemplate);
                NotifyPropertyChanged(cellTemplateProperty);
            }
        }

        /// <summary>
        /// Callback for constructing the visual representation of the column in the header.
        /// </summary>
        public Func<VisualElement> makeHeader
        {
            get => m_MakeHeader;
            set
            {
                if (m_MakeHeader == value)
                    return;
                m_MakeHeader = value;
                NotifyChange(ColumnDataType.HeaderTemplate);
            }
        }

        /// <summary>
        /// Callback for binding the header element to this column.
        /// </summary>
        public Action<VisualElement> bindHeader
        {
            get => m_BindHeader;
            set
            {
                if (m_BindHeader == value)
                    return;
                m_BindHeader = value;
                NotifyChange(ColumnDataType.HeaderTemplate);
            }
        }

        /// <summary>
        /// Callback for unbinding the header element to this column.
        /// </summary>
        public Action<VisualElement> unbindHeader
        {
            get => m_UnbindHeader;
            set
            {
                if (m_UnbindHeader == value)
                    return;
                m_UnbindHeader = value;
                NotifyChange(ColumnDataType.HeaderTemplate);
            }
        }

        /// <summary>
        /// Callback for destroying the visual representation of the column in the header.
        /// </summary>
        public Action<VisualElement> destroyHeader
        {
            get => m_DestroyHeader;
            set
            {
                if (m_DestroyHeader == value)
                    return;
                m_DestroyHeader = value;
                NotifyChange(ColumnDataType.HeaderTemplate);
            }
        }

        /// <summary>
        /// Callback for constructing the VisualElement that is the template for each cell of the column.
        /// </summary>
        public Func<VisualElement> makeCell
        {
            get => m_MakeCell;
            set
            {
                if (m_MakeCell == value)
                    return;
                m_MakeCell = value;
                NotifyChange(ColumnDataType.CellTemplate);
            }
        }

        /// <summary>
        /// Callback for binding the specified data item at the given row to the visual element.
        /// </summary>
        public Action<VisualElement, int> bindCell
        {
            get => m_BindCell;
            set
            {
                if (m_BindCell == value)
                    return;
                m_BindCell = value;
                NotifyChange(ColumnDataType.CellTemplate);
            }
        }

        /// <summary>
        /// Callback for unbinding the specified data item at the given row from the visual element.
        /// </summary>
        public Action<VisualElement, int> unbindCell
        {
            get => m_UnbindCellItem;
            set
            {
                if (m_UnbindCellItem == value)
                    return;
                m_UnbindCellItem = value;
                NotifyChange(ColumnDataType.CellTemplate);
            }
        }

        /// <summary>
        /// Callback for destroying the VisualElement that was built for this column.
        /// </summary>
        public Action<VisualElement> destroyCell { get; set; }

        /// <summary>
        /// The column collection that contains this column.
        /// </summary>
        public Columns collection { get; internal set; }

        /// <summary>
        /// Event sent whenever properties of the column change indicating the role of the property.
        /// </summary>
        internal event Action<Column, ColumnDataType> changed;

        /// <summary>
        /// Event sent whenever the actual width of the column changes.
        /// </summary>
        internal event Action<Column> resized;

        /// <summary>
        /// Notify that data of this column has changed.
        /// </summary>
        /// <param name="type"></param>
        void NotifyChange(ColumnDataType type)
        {
            changed?.Invoke(this, type);
        }

        void NotifyPropertyChanged(in BindingId property)
        {
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
        }

        internal float GetWidth(float layoutWidth)
        {
            return width.unit == LengthUnit.Pixel ? width.value : width.value * layoutWidth / 100f;
        }

        internal float GetMaxWidth(float layoutWidth)
        {
            return maxWidth.unit == LengthUnit.Pixel ? maxWidth.value : maxWidth.value * layoutWidth / 100f;
        }

        internal float GetMinWidth(float layoutWidth)
        {
            return minWidth.unit == LengthUnit.Pixel ? minWidth.value : minWidth.value * layoutWidth / 100f;
        }
    }
}
