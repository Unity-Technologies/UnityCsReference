// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    internal static class LibraryOrdering
    {
        [NoAutoStaticsCleanup] // fixed engine control-type list, never mutated
        static readonly Type[] k_CuratedOrder =
        {
            // Containers
            typeof(VisualElement),
            typeof(ScrollView),
            typeof(ListView),
            typeof(TreeView),
            typeof(MultiColumnListView),
            typeof(MultiColumnTreeView),
            typeof(GroupBox),

            // Editor Containers
            typeof(IMGUIContainer),

            // Controls
            typeof(Label),
            typeof(Image),
            typeof(Button),
            typeof(Toggle),
            typeof(ToggleButtonGroup),
            typeof(Scroller),
            typeof(TextField),
            typeof(Foldout),
            typeof(Slider),
            typeof(SliderInt),
            typeof(MinMaxSlider),
            typeof(ProgressBar),
            typeof(DropdownField),
            typeof(EnumField),
            typeof(RadioButton),
            typeof(RadioButtonGroup),
            typeof(Tab),
            typeof(TabView),
            typeof(HelpBox),
            typeof(MaskField),
            typeof(Mask64Field),

            // Numeric Fields
            typeof(IntegerField),
            typeof(FloatField),
            typeof(LongField),
            typeof(DoubleField),
            typeof(Hash128Field),
            typeof(Vector2Field),
            typeof(Vector3Field),
            typeof(Vector4Field),
            typeof(RectField),
            typeof(BoundsField),
            typeof(UnsignedIntegerField),
            typeof(UnsignedLongField),
            typeof(Vector2IntField),
            typeof(Vector3IntField),
            typeof(RectIntField),
            typeof(BoundsIntField),

            // Value Fields
            typeof(ColorField),
            typeof(CurveField),
            typeof(GradientField),

            // Choice Fields
            typeof(TagField),
            typeof(LayerField),
            typeof(LayerMaskField),
            typeof(EnumFlagsField),

            // Toolbar
            typeof(Toolbar),
            typeof(ToolbarMenu),
            typeof(ToolbarButton),
            typeof(ToolbarSpacer),
            typeof(ToolbarToggle),
            typeof(ToolbarBreadcrumbs),
            typeof(ToolbarSearchField),
            typeof(ToolbarPopupSearchField),

            // Inspectors
            typeof(ObjectField),
            typeof(PropertyField),
        };

        [NoAutoStaticsCleanup] // built once from k_CuratedOrder engine types, never mutated
        static readonly Dictionary<Type, int> s_OrderByType = BuildOrderByType();

        static Dictionary<Type, int> BuildOrderByType()
        {
            var orderByType = new Dictionary<Type, int>(k_CuratedOrder.Length);
            for (var i = 0; i < k_CuratedOrder.Length; i++)
                orderByType.Add(k_CuratedOrder[i], i);
            return orderByType;
        }

        /// <summary>
        /// Returns the curated rank of a type, or <see cref="int.MaxValue"/> when the type isn't
        /// curated so it sorts after curated items.
        /// </summary>
        public static int GetOrder(Type type)
        {
            return type != null && s_OrderByType.TryGetValue(type, out var order) ? order : int.MaxValue;
        }
    }
}
