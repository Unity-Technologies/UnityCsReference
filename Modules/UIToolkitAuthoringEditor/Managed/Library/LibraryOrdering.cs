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
        /// <summary>
        /// A named group of the curated controls, in display order.
        /// </summary>
        internal sealed class Category
        {
            public string Name { get; }

            /// <summary>
            /// Whether the controls of the category only work in editor UI.
            /// </summary>
            public bool IsEditorOnly { get; }

            public IReadOnlyList<Type> Types { get; }

            public Category(string name, bool isEditorOnly, params Type[] types)
            {
                Name = name;
                IsEditorOnly = isEditorOnly;
                Types = types;
            }
        }

        [NoAutoStaticsCleanup] // fixed engine control-type list, never mutated
        static readonly Category[] k_Categories =
        {
            new("Containers", false,
                typeof(VisualElement),
                typeof(ScrollView),
                typeof(ListView),
                typeof(TreeView),
                typeof(MultiColumnListView),
                typeof(MultiColumnTreeView),
                typeof(GroupBox)),

            new("Editor Containers", true,
                typeof(IMGUIContainer)),

            new("Controls", false,
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
                typeof(Mask64Field)),

            new("Numeric Fields", false,
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
                typeof(BoundsIntField)),

            new("Value Fields", true,
                typeof(ColorField),
                typeof(CurveField),
                typeof(GradientField)),

            new("Choice Fields", true,
                typeof(TagField),
                typeof(LayerField),
                typeof(LayerMaskField),
                typeof(EnumFlagsField)),

            new("Toolbar", true,
                typeof(Toolbar),
                typeof(ToolbarMenu),
                typeof(ToolbarButton),
                typeof(ToolbarSpacer),
                typeof(ToolbarToggle),
                typeof(ToolbarBreadcrumbs),
                typeof(ToolbarSearchField),
                typeof(ToolbarPopupSearchField)),

            new("Inspectors", true,
                typeof(ObjectField),
                typeof(PropertyField)),
        };

        [NoAutoStaticsCleanup] // built once from k_Categories engine types, never mutated
        static readonly Dictionary<Type, int> s_OrderByType = BuildOrderByType();

        [NoAutoStaticsCleanup] // built once from k_Categories engine types, never mutated
        static readonly Dictionary<Type, Category> s_CategoryByType = BuildCategoryByType();

        /// <summary>
        /// The curated categories, in display order.
        /// </summary>
        public static IReadOnlyList<Category> Categories => k_Categories;

        static Dictionary<Type, int> BuildOrderByType()
        {
            var orderByType = new Dictionary<Type, int>();
            foreach (var category in k_Categories)
            {
                foreach (var type in category.Types)
                    orderByType.Add(type, orderByType.Count);
            }

            return orderByType;
        }

        static Dictionary<Type, Category> BuildCategoryByType()
        {
            var categoryByType = new Dictionary<Type, Category>();
            foreach (var category in k_Categories)
            {
                foreach (var type in category.Types)
                    categoryByType.Add(type, category);
            }

            return categoryByType;
        }

        /// <summary>
        /// Returns the curated rank of a type, or <see cref="int.MaxValue"/> when the type isn't
        /// curated so it sorts after curated items.
        /// </summary>
        public static int GetOrder(Type type)
        {
            return type != null && s_OrderByType.TryGetValue(type, out var order) ? order : int.MaxValue;
        }

        /// <summary>
        /// Returns the curated category of a type, or null when the type isn't curated.
        /// </summary>
        public static Category GetCategory(Type type)
        {
            return type != null && s_CategoryByType.TryGetValue(type, out var category) ? category : null;
        }
    }
}
