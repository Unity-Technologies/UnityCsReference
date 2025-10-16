// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    public partial class ColorField
    {
        /// <summary>
        /// Instantiates a <see cref="ColorField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<ColorField, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ColorField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseFieldTraits<Color, UxmlColorAttributeDescription> { }
    }

    public partial class CurveField
    {
        /// <summary>
        /// Instantiates a <see cref="CurveField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<CurveField, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="CurveField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseField<AnimationCurve>.UxmlTraits { }
    }

    public partial class EnumFlagsField
    {
        /// <summary>
        /// Instantiates a <see cref="EnumFlagsField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<EnumFlagsField, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="EnumFlagsField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseMaskField<Enum>.UxmlTraits { }
    }

    public partial class GradientField
    {
        /// <summary>
        /// Instantiates a <see cref="GradientField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<GradientField, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="GradientField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseField<Gradient>.UxmlTraits { }
    }

    public partial class InspectorElement
    {
        /// <summary>
        /// Instantiates a <see cref="InspectorElement"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<InspectorElement, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="InspectorElement"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BindableElement.UxmlTraits { }
    }

    public partial class LayerField
    {
        /// <summary>
        /// Instantiates a <see cref="LayerField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<LayerField, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="LayerField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : PopupField<int>.UxmlTraits { }
    }

    public partial class LayerMaskField
    {
        /// <summary>
        /// Instantiates a <see cref="LayerMaskField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<LayerMaskField, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="LayerMaskField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BasePopupField<int, UxmlIntAttributeDescription>.UxmlTraits { }
    }

    public partial class MaskField
    {
        /// <summary>
        /// Instantiates a <see cref="MaskField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<MaskField, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="MaskField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BasePopupField<int, UxmlIntAttributeDescription>.UxmlTraits { }
    }

    internal partial class MinMaxGradientField
    {
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        protected new class UxmlFactory : UxmlFactory<MinMaxGradientField, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="MinMaxGradientField"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the properties of a MinMaxGradientField element that you can
        /// use in a UXML asset.
        /// </remarks>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        protected new class UxmlTraits : BaseField<ParticleSystem.MinMaxGradient>.UxmlTraits { }
    }

    public partial class ObjectField
    {
        /// <summary>
        /// Instantiates an <see cref="ObjectField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<ObjectField, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ObjectField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : BaseField<Object>.UxmlTraits { }
    }

    public partial class PropertyField
    {
        /// <summary>
        /// Instantiates a <see cref="PropertyField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<PropertyField, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="PropertyField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : VisualElement.UxmlTraits { }
    }

    public partial class SearchFieldBase<TextInputType, T>
    {
        /// <summary>
        /// Defines <see cref="SearchFieldBase.UxmlTraits"/> for the <see cref="SearchFieldBase"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the properties of a SearchFieldBase element that you can
        /// use in a UXML asset.
        /// </remarks>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : VisualElement.UxmlTraits { }
    }

    public partial class TagField
    {
        /// <summary>
        /// Instantiates a <see cref="TagField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<TagField, UxmlTraits> { }
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="TagField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : PopupField<string>.UxmlTraits { }
    }

    public partial class Toolbar
    {
        /// <summary>
        /// Instantiates a <see cref="Toolbar"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<Toolbar> { }
    }

    public partial class ToolbarBreadcrumbs
    {
        /// <summary>
        /// Instantiates a <see cref="ToolbarBreadcrumbs"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<ToolbarBreadcrumbs> { }
    }

    public partial class ToolbarButton
    {
        /// <summary>
        /// Instantiates a <see cref="ToolbarButton"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<ToolbarButton, UxmlTraits> { }
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ToolbarButton"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : Button.UxmlTraits { }
    }

    public partial class ToolbarMenu
    {
        /// <summary>
        /// Instantiates a <see cref="ToolbarMenu"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<ToolbarMenu, UxmlTraits> { }
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ToolbarMenu"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : TextElement.UxmlTraits { }
    }

    public partial class ToolbarPopupSearchField
    {
        /// <summary>
        /// Instantiates a <see cref="ToolbarPopupSearchField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<ToolbarPopupSearchField, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ToolbarPopupSearchField"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the properties of a ToolbarPopupSearchField element that you can
        /// use in a UXML asset.
        /// </remarks>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : ToolbarSearchField.UxmlTraits { }
    }

    public partial class ToolbarSearchField
    {
        /// <summary>
        /// Instantiates a <see cref="ToolbarSearchField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<ToolbarSearchField, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ToolbarSearchField"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the properties of a ToolbarSearchField element that you can
        /// use in a UXML asset.
        /// </remarks>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : SearchFieldBase<TextField, string>.UxmlTraits { }
    }

    public partial class ToolbarSpacer
    {
        /// <summary>
        /// Instantiates a <see cref="ToolbarSpacer"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<ToolbarSpacer> { }
    }

    public partial class ToolbarToggle
    {
        /// <summary>
        /// Instantiates a <see cref="ToolbarToggle"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlFactoryObsoleteIsError)]
        public new class UxmlFactory : UxmlFactory<ToolbarToggle, UxmlTraits> { }
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ToolbarToggle"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", UxmlTraitsObsoleteIsError)]
        public new class UxmlTraits : Toggle.UxmlTraits { }
    }
}
