// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.UIElements
{
    public partial class AbstractProgressBar
    {
        /// <undoc/>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BindableElement.UxmlTraits { }
    }

    public partial class BaseField<TValueType>
    {
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="BaseField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BindableElement.UxmlTraits { }
    }

    /// <summary>
    /// Traits for the <see cref="BaseField"/>.
    /// </summary>
    [Obsolete("BaseFieldTraits<TValueType, TValueUxmlAttributeType> is deprecated and will be removed. Use UxmlElementAttribute instead.", VisualElement.UxmlTraitsObsoleteIsError)]
    public class BaseFieldTraits<TValueType, TValueUxmlAttributeType> : BaseField<TValueType>.UxmlTraits
        where TValueUxmlAttributeType : TypedUxmlAttributeDescription<TValueType>, new()
    {
    }

    public partial class BaseListView
    {
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="BaseListView"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the list view element properties that you can use in a UI document asset (UXML file).
        /// </remarks>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseVerticalCollectionView.UxmlTraits
        {
            /// <summary>
            /// Returns an empty enumerable, because list views usually do not have child elements.
            /// </summary>
            /// <returns>An empty enumerable.</returns>
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }
    }

    public partial class BaseSlider<TValueType>
    {
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseField<TValueType>.UxmlTraits { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="BaseSlider"/>.
        ///
        /// This class must be used instead of the non-generic inherited UxmlTraits equivalent.
        /// </summary>
        /// <remarks>
        /// This class defines the properties of a BaseSlider element that you can
        /// use in a UXML asset.
        /// </remarks>
        [Obsolete("UxmlTraits<TValueUxmlAttributeType> is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public class UxmlTraits<TValueUxmlAttributeType> : BaseFieldTraits<TValueType, TValueUxmlAttributeType>
            where TValueUxmlAttributeType : TypedUxmlAttributeDescription<TValueType>, new()
        { }
    }

    public partial class BaseTreeView
    {
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="TreeView"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the TreeView element properties that you can use in a UI document asset (UXML file).
        /// </remarks>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseVerticalCollectionView.UxmlTraits
        {
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }
    }

    public partial class BaseVerticalCollectionView
    {
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="BaseVerticalCollectionView"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the BaseVerticalCollectionView element properties that you can use in a UI document asset (UXML file).
        /// </remarks>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            /// <summary>
            /// Returns an empty enumerable, because list views usually do not have child elements.
            /// </summary>
            /// <returns>An empty enumerable.</returns>
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }
    }

    public partial class BindableElement
    {
        /// <summary>
        /// Instantiates a <see cref="BindableElement"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<BindableElement, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="BindableElement"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : VisualElement.UxmlTraits { }
    }

    public partial class BoundsField
    {
        /// <summary>
        /// Instantiates a <see cref="BoundsField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<BoundsField, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="BoundsField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseField<Bounds>.UxmlTraits { }
    }

    public partial class BoundsIntField
    {
        /// <summary>
        /// Instantiates a <see cref="BoundsIntField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<BoundsIntField, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="BoundsIntField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseField<BoundsInt>.UxmlTraits { }
    }

    public partial class Box
    {
        /// <summary>
        /// Instantiates a <see cref="Box"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<Box> { }
    }

    public partial class Button
    {
        /// <summary>
        /// Instantiates a <see cref="Button"/> using data from a UXML file.
        /// </summary>
        /// <remarks>
        /// This class is added to every <see cref="VisualElement"/> that is created from UXML.
        /// </remarks>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<Button, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Button"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the properties of a Button element that you can
        /// use in a UXML asset.
        /// </remarks>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : TextElement.UxmlTraits { }
    }

    partial class ButtonStripField
    {
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<ButtonStripField, UxmlTraits> { }
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseField<int>.UxmlTraits { }
    }

    public partial class Column
    {
        /// <summary>
        /// Instantiates a <see cref="Column"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlObjectFactory<T> is deprecated and will be removed. Use UxmlElementAttribute instead.", VisualElement.UxmlFactoryObsoleteIsError)]
        internal class UxmlObjectFactory<T> : UxmlObjectFactory<T, UxmlObjectTraits<T>> where T : Column, new() { }
        /// <summary>
        /// Instantiates a <see cref="Column"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlObjectFactory<T> is deprecated and will be removed. Use UxmlElementAttribute instead.", VisualElement.UxmlFactoryObsoleteIsError)]
        internal class UxmlObjectFactory : UxmlObjectFactory<Column> { }

        /// <summary>
        /// Defines <see cref="UxmlObjectTraits{T}"/> for the <see cref="Column"/>.
        /// </summary>
        [Obsolete("UxmlObjectTraits<T> is deprecated and will be removed. Use UxmlElementAttribute instead.", VisualElement.UxmlTraitsObsoleteIsError)]
        internal class UxmlObjectTraits<T> : UIElements.UxmlObjectTraits<T> where T : Column { }
    }

    public partial class Columns
    {

        /// <summary>
        /// Instantiates a <see cref="Columns"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlObjectFactory<T> is deprecated and will be removed. Use UxmlElementAttribute instead.", VisualElement.UxmlFactoryObsoleteIsError)]
        internal class UxmlObjectFactory<T> : UxmlObjectFactory<T, UxmlObjectTraits<T>> where T : Columns, new() { }

        /// <summary>
        /// Instantiates a <see cref="Columns"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlObjectFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", VisualElement.UxmlFactoryObsoleteIsError)]
        internal class UxmlObjectFactory : UxmlObjectFactory<Columns> { }

        /// <summary>
        /// Defines <see cref="UxmlObjectTraits{T}"/> for the <see cref="Columns"/>.
        /// </summary>
        [Obsolete("UxmlObjectTraits<T> is deprecated and will be removed. Use UxmlElementAttribute instead.", VisualElement.UxmlTraitsObsoleteIsError)]
        internal class UxmlObjectTraits<T> : UIElements.UxmlObjectTraits<T> where T : Columns { }
    }

    public partial class DoubleField
    {
        /// <summary>
        /// Instantiates a <see cref="DoubleField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<DoubleField, UxmlTraits> { }
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="DoubleField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : TextValueFieldTraits<double, UxmlDoubleAttributeDescription> { }
    }

    public partial class DropdownField
    {
        /// <summary>
        /// Instantiates a <see cref="DropdownField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<DropdownField, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="DropdownField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseField<string>.UxmlTraits { }
    }

    public partial class EnumField
    {
        /// <summary>
        /// Instantiates an <see cref="EnumField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<EnumField, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="EnumField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseField<Enum>.UxmlTraits { }
    }

    public partial class FloatField
    {
        /// <summary>
        /// Instantiates a <see cref="FloatField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<FloatField, UxmlTraits> { }
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="FloatField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : TextValueFieldTraits<float, UxmlFloatAttributeDescription> { }
    }

    public partial class Foldout
    {
        /// <summary>
        /// Instantiates a <see cref="Foldout"/> using the data from a UXML file.
        /// </summary>
        /// <remarks>
        /// This class is added to every <see cref="VisualElement"/> created from UXML.
        /// </remarks>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<Foldout, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Foldout"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the Foldout element properties that you can use in a UXML asset.
        /// </remarks>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BindableElement.UxmlTraits { }
    }

    public partial class GroupBox
    {
        /// <summary>
        /// Instantiates a <see cref="GroupBox"/> using data from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<GroupBox, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="GroupBox"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BindableElement.UxmlTraits { }
    }

    public partial class Hash128Field
    {
        /// <summary>
        /// Instantiates a <see cref="Hash128Field"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<Hash128Field, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Hash128Field"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : TextValueFieldTraits<Hash128, UxmlHash128AttributeDescription> { }
    }

    public partial class HelpBox
    {
        /// <summary>
        /// Instantiates a <see cref="HelpBox"/> with data from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<HelpBox, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="HelpBox"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : VisualElement.UxmlTraits { }
    }

    public partial class Image
    {
        /// <summary>
        /// Instantiates an <see cref="Image"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<Image, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Image"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            /// <summary>
            /// Returns an empty enumerable, as images generally do not have children.
            /// </summary>
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }
    }

    public partial class IMGUIContainer
    {
        /// <summary>
        /// Instantiates an <see cref="IMGUIContainer"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<IMGUIContainer, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="IMGUIContainer"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            /// <summary>
            /// Returns an empty enumerable, as IMGUIContainer cannot have VisualElement children.
            /// </summary>
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }
    }

    public partial class IntegerField
    {
        /// <summary>
        /// Instantiates an <see cref="IntegerField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<IntegerField, UxmlTraits> { }
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="IntegerField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : TextValueFieldTraits<int, UxmlIntAttributeDescription> { }
    }

    public partial class Label
    {
        /// <summary>
        /// Instantiates a <see cref="Label"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<Label, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Label"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : TextElement.UxmlTraits { }
    }

    public partial class ListView
    {
        /// <summary>
        /// Instantiates a <see cref="ListView"/> using data from a UXML file.
        /// </summary>
        /// <remarks>
        /// This class is added to every <see cref="VisualElement"/> created from UXML.
        /// </remarks>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<ListView, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ListView"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the ListView element properties that you can use in a UI document asset (UXML file).
        /// </remarks>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseListView.UxmlTraits { }
    }

    public partial class LongField
    {
        /// <summary>
        /// Instantiates a <see cref="LongField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<LongField, UxmlTraits> { }
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="LongField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : TextValueFieldTraits<long, UxmlLongAttributeDescription> { }
    }

    public partial class MinMaxSlider
    {
        /// <summary>
        /// Instantiates a <see cref="MinMaxSlider"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<MinMaxSlider, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="MinMaxSlider"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseField<Vector2>.UxmlTraits { }
    }

    public partial class MultiColumnListView
    {
        /// <summary>
        /// Instantiates a <see cref="MultiColumnListView"/> using data from a UXML file.
        /// </summary>
        /// <remarks>
        /// This class is added to every <see cref="VisualElement"/> created from UXML.
        /// </remarks>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<MultiColumnListView, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="MultiColumnListView"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the MultiColumnListView element properties that you can use in a UI document asset (UXML file).
        /// </remarks>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseListView.UxmlTraits { }
    }

    public partial class MultiColumnTreeView
    {
        /// <summary>
        /// Instantiates a <see cref="MultiColumnTreeView"/> using data from a UXML file.
        /// </summary>
        /// <remarks>
        /// This class is added to every <see cref="VisualElement"/> created from UXML.
        /// </remarks>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<MultiColumnTreeView, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="MultiColumnTreeView"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the MultiColumnTreeView element properties that you can use in a UI document asset (UXML file).
        /// </remarks>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseTreeView.UxmlTraits { }
    }

    public partial class PopupWindow
    {
        /// <summary>
        /// Instantiates a <see cref="PopupWindow"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<PopupWindow, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="PopupWindow"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : TextElement.UxmlTraits
        {
            /// <summary>
            /// Returns an empty enumerable, as popup windows generally do not have children.
            /// </summary>
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get
                {
                    yield return new UxmlChildElementDescription(typeof(VisualElement));
                }
            }
        }
    }

    public partial class ProgressBar
    {
        /// <undoc/>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<ProgressBar, UxmlTraits> { }
    }

    public partial class RadioButton
    {
        /// <summary>
        /// Instantiates a <see cref="RadioButton"/> using data from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<RadioButton, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="RadioButton"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseFieldTraits<bool, UxmlBoolAttributeDescription> { }
    }

    public partial class RadioButtonGroup
    {
        /// <summary>
        /// Instantiates a <see cref="RadioButtonGroup"/> using data from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<RadioButtonGroup, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="RadioButtonGroup"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseFieldTraits<int, UxmlIntAttributeDescription> { }
    }

    public partial class RectField
    {
        /// <summary>
        /// Instantiates a <see cref="RectField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<RectField, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="RectField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseCompositeField<Rect, FloatField, float>.UxmlTraits { }
    }

    public partial class RectIntField
    {
        /// <summary>
        /// Instantiates a <see cref="RectIntField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<RectIntField, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="RectIntField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseCompositeField<RectInt, IntegerField, int>.UxmlTraits { }
    }

    public partial class RepeatButton
    {
        /// <summary>
        /// Instantiates a <see cref="RepeatButton"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<RepeatButton, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="RepeatButton"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : TextElement.UxmlTraits { }
    }

    public partial class Scroller
    {
        /// <summary>
        /// Instantiates a <see cref="Scroller"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<Scroller, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Scroller"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            /// <summary>
            /// Returns an empty enumerable, as scrollers do not have children.
            /// </summary>
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }
    }

    public partial class ScrollView
    {
        /// <summary>
        /// Instantiates a <see cref="ScrollView"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<ScrollView, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ScrollView"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : VisualElement.UxmlTraits { }
    }

    public partial class Slider
    {
        /// <summary>
        /// Instantiates a <see cref="Slider"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<Slider, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Slider"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : UxmlTraits<UxmlFloatAttributeDescription> { }
    }

    public partial class SliderInt
    {
        /// <summary>
        /// Instantiates a <see cref="SliderInt"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<SliderInt, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="SliderInt"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : UxmlTraits<UxmlIntAttributeDescription> { }
    }

    public partial class SortColumnDescription
    {
        /// <summary>
        /// Instantiates a <see cref="SortColumnDescription"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlObjectFactory<T> is deprecated and will be removed. Use UxmlElementAttribute instead.", VisualElement.UxmlFactoryObsoleteIsError)]
        internal class UxmlObjectFactory<T> : UxmlObjectFactory<T, UxmlObjectTraits<T>> where T : SortColumnDescription, new() { }
        /// <summary>
        /// Instantiates a <see cref="SortColumnDescription"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlObjectFactory<T> is deprecated and will be removed. Use UxmlElementAttribute instead.", VisualElement.UxmlFactoryObsoleteIsError)]
        internal class UxmlObjectFactory : UxmlObjectFactory<SortColumnDescription> { }

        /// <summary>
        /// Defines <see cref="UxmlObjectTraits{T}"/> for the <see cref="SortColumnDescription"/>.
        /// </summary>
        [Obsolete("UxmlObjectTraits<T> is deprecated and will be removed. Use UxmlElementAttribute instead.", VisualElement.UxmlTraitsObsoleteIsError)]
        internal class UxmlObjectTraits<T> : UIElements.UxmlObjectTraits<T> where T : SortColumnDescription { }
    }

    public partial class SortColumnDescriptions
    {
        /// <summary>
        /// Instantiates a <see cref="SortColumnDescriptions"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlObjectFactory<T> is deprecated and will be removed. Use UxmlElementAttribute instead.", VisualElement.UxmlFactoryObsoleteIsError)]
        internal class UxmlObjectFactory<T> : UxmlObjectFactory<T, UxmlObjectTraits<T>> where T : SortColumnDescriptions, new() { }
        /// <summary>
        /// Instantiates a <see cref="SortColumnDescriptions"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlObjectFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", VisualElement.UxmlFactoryObsoleteIsError)]
        internal class UxmlObjectFactory : UxmlObjectFactory<SortColumnDescriptions> { }

        /// <summary>
        /// Defines <see cref="UxmlObjectTraits{T}"/> for the <see cref="SortColumnDescriptions"/>.
        /// </summary>
        [Obsolete("UxmlObjectTraits<T> is deprecated and will be removed. Use UxmlElementAttribute instead.", VisualElement.UxmlTraitsObsoleteIsError)]
        internal class UxmlObjectTraits<T> : UIElements.UxmlObjectTraits<T> where T : SortColumnDescriptions { }
    }

    public partial class Tab
    {
        /// <summary>
        /// Instantiates an <see cref="Tab"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<Tab, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Tab"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the properties of a Tab element that you can use in a UXML file.
        /// </remarks>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : VisualElement.UxmlTraits { }
    }

    public partial class TabView
    {
        /// <summary>
        /// Instantiates a <see cref="TabView"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<TabView, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="TabView"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the properties of a TabView element that you can use in a UXML asset.
        /// </remarks>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : VisualElement.UxmlTraits { }
    }

    public partial class TemplateContainer
    {
        /// <summary>
        /// Instantiates and clones a <see cref="TemplateContainer"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<TemplateContainer, UxmlTraits>
        {
            internal const string k_ElementName = "Instance";

            public override string uxmlName => k_ElementName;

            public override string uxmlQualifiedName => uxmlNamespace + "." + uxmlName;
        }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="TemplateContainer"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            internal const string k_TemplateAttributeName = "template";

            /// <summary>
            /// Returns an empty enumerable, as template instances do not have children.
            /// </summary>
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }
    }

    public partial class TextElement
    {
        /// <summary>
        /// Instantiates a <see cref="TextElement"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<TextElement, UxmlTraits> { }
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="TextElement"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            /// <summary>
            /// Enumerator to get the child elements of the <see cref="UxmlTraits"/> of <see cref="TextElement"/>.
            /// </summary>
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }
    }

    public partial class TextField
    {
        /// <summary>
        /// Instantiates a <see cref="TextField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<TextField, UxmlTraits> { }
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="TextField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : TextInputBaseField<string>.UxmlTraits { }
    }

    public partial class TextInputBaseField<TValueType>
    {
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for <see cref="TextInputFieldBase"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseFieldTraits<string, UxmlStringAttributeDescription> { }
    }

    // Derive from BaseFieldTraits in order to not inherit from TextInputBaseField UXML attributes.
    /// <summary>
    /// Specifies the <see cref="TextValueField{TValueType}"/>'s <see cref="UxmlTraits"/>.
    /// </summary>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    [Obsolete("TextValueFieldTraits<TValueType, TValueUxmlAttributeType> is deprecated and will be removed. Use UxmlElementAttribute instead.", VisualElement.UxmlTraitsObsoleteIsError)]
    public class TextValueFieldTraits<TValueType, TValueUxmlAttributeType> : BaseFieldTraits<TValueType, TValueUxmlAttributeType>
        where TValueUxmlAttributeType : TypedUxmlAttributeDescription<TValueType>, new()
    {
    }

    public partial class Toggle
    {
        /// <summary>
        /// Instantiates a <see cref="Toggle"/> using data from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<Toggle, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Toggle"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseFieldTraits<bool, UxmlBoolAttributeDescription> { }
    }

    public partial class ToggleButtonGroup
    {
        /// <summary>
        /// Instantiates a <see cref="ToggleButtonGroup"/>.
        /// </summary>
        /// <remarks>
        /// This class is added to every <see cref="VisualElement"/> that is created from UXML.
        /// </remarks>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<ToggleButtonGroup, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ToggleButtonGroup"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the properties of a ToggleButtonGroup element that you can use in a UXML asset.
        /// </remarks>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseField<ToggleButtonGroupState>.UxmlTraits { }
    }

    public partial class TreeView
    {
        /// <summary>
        /// Instantiates a <see cref="TreeView"/> using data from a UXML file.
        /// </summary>
        /// <remarks>
        /// This class is added to every <see cref="VisualElement"/> created from UXML.
        /// </remarks>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<TreeView, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="TreeView"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the TreeView element properties that you can use in a UI document asset (UXML file).
        /// </remarks>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseTreeView.UxmlTraits { }
    }

    public partial class TwoPaneSplitView
    {
        /// <summary>
        /// Instantiates a <see cref="TwoPaneSplitView"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<TwoPaneSplitView, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="TwoPaneSplitView"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }
    }

    public partial class UnsignedIntegerField
    {
        /// <summary>
        /// Instantiates an <see cref="UnsignedIntegerField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<UnsignedIntegerField, UxmlTraits> { }
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="UnsignedIntegerField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : TextValueFieldTraits<uint, UxmlUnsignedIntAttributeDescription> { }
    }

    public partial class UnsignedLongField
    {
        /// <summary>
        /// Instantiates a <see cref="UnsignedLongField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<UnsignedLongField, UxmlTraits> { }
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="UnsignedLongField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : TextValueFieldTraits<ulong, UxmlUnsignedLongAttributeDescription> { }
    }

    public partial class Vector2Field
    {
        /// <summary>
        /// Instantiates a <see cref="Vector2Field"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<Vector2Field, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Vector2Field"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseCompositeField<Vector2, FloatField, float>.UxmlTraits { }
    }

    public partial class Vector2IntField
    {
        /// <summary>
        /// Instantiates a <see cref="Vector2IntField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<Vector2IntField, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Vector2IntField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseCompositeField<Vector2Int, IntegerField, int>.UxmlTraits { }
    }

    public partial class Vector3Field
    {
        /// <summary>
        /// Instantiates a <see cref="Vector3Field"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<Vector3Field, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Vector3Field"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseCompositeField<Vector3, FloatField, float>.UxmlTraits { }
    }

    public partial class Vector3IntField
    {
        /// <summary>
        /// Instantiates a <see cref="Vector3IntField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<Vector3IntField, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Vector3IntField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseCompositeField<Vector3Int, IntegerField, int>.UxmlTraits { }
    }

    public partial class Vector4Field
    {
        /// <summary>
        /// Instantiates a <see cref="Vector4Field"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<Vector4Field, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Vector4Field"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseCompositeField<Vector4, FloatField, float>.UxmlTraits { }
    }

    public partial class VisualElement
    {
        internal const bool UxmlTraitsObsoleteIsError = false;
        internal const bool UxmlFactoryObsoleteIsError = false;

        /// <summary>
        /// Instantiates a <see cref="VisualElement"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public class UxmlFactory : UxmlFactory<VisualElement, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="VisualElement"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public class UxmlTraits : UIElements.UxmlTraits
        {
            /// <summary>
            /// Returns an enumerable containing <c>UxmlChildElementDescription(typeof(VisualElement))</c>, since VisualElements can contain other VisualElements.
            /// </summary>
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield return new UxmlChildElementDescription(typeof(VisualElement)); }
            }
        }
    }
}
