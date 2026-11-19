// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Helpers that determine which <see cref="ConditionComparison"/> operators apply to a given type.
    /// </summary>
    internal static class ConditionComparisonExtensions
    {
        static readonly ConditionComparison[] k_EqualityComparisons =
        {
            ConditionComparison.Equal,
            ConditionComparison.NotEqual,
        };

        static readonly ConditionComparison[] k_AllComparisons =
        {
            ConditionComparison.Equal,
            ConditionComparison.NotEqual,
            ConditionComparison.Less,
            ConditionComparison.LessOrEqual,
            ConditionComparison.Greater,
            ConditionComparison.GreaterOrEqual,
        };

        [NoAutoStaticsCleanup] // fixed lookup table; value is stable across reloads
        static readonly List<ConditionComparison> k_EqualityChoices = new(k_EqualityComparisons);

        [NoAutoStaticsCleanup] // fixed lookup table; value is stable across reloads
        static readonly List<ConditionComparison> k_AllChoices = new(k_AllComparisons);

        [NoAutoStaticsCleanup] // fixed lookup table; value is stable across reloads
        static readonly HashSet<Type> k_OrderedTypes = new(new[]
        {
            typeof(byte), typeof(sbyte),
            typeof(short), typeof(ushort),
            typeof(int), typeof(uint),
            typeof(long), typeof(ulong),
            typeof(float), typeof(double),
            typeof(char),
        });

        /// <summary>
        /// Whether a type supports ordering operators (less/greater), as opposed to equality only.
        /// </summary>
        /// <param name="typeHandle">The type to test.</param>
        /// <returns>True for numeric types; false otherwise.</returns>
        public static bool SupportsOrdering(TypeHandle typeHandle)
        {
            return SupportsOrdering(typeHandle.Resolve());
        }

        /// <summary>
        /// Whether a type supports ordering operators (less/greater), as opposed to equality only.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns>True for numeric types; false otherwise.</returns>
        public static bool SupportsOrdering(Type type)
        {
            return type != null && k_OrderedTypes.Contains(type);
        }

        /// <summary>
        /// Gets the comparison operators available for a given type.
        /// </summary>
        /// <param name="typeHandle">The type of the compared variable.</param>
        /// <returns>All six operators for ordered types; equality operators only otherwise.</returns>
        public static IReadOnlyList<ConditionComparison> GetAvailableComparisons(TypeHandle typeHandle)
        {
            return SupportsOrdering(typeHandle) ? k_AllComparisons : k_EqualityComparisons;
        }

        /// <summary>
        /// Gets the comparison operators available for a given type.
        /// </summary>
        /// <param name="type">The type of the compared value.</param>
        /// <returns>All six operators for ordered types; equality operators only otherwise.</returns>
        public static IReadOnlyList<ConditionComparison> GetAvailableComparisons(Type type)
        {
            return SupportsOrdering(type) ? k_AllComparisons : k_EqualityComparisons;
        }

        /// <summary>
        /// Creates the popup field used to edit a condition's comparison operator.
        /// </summary>
        /// <param name="valueType">The type of the compared value.</param>
        /// <param name="model">The condition model whose comparison is edited.</param>
        /// <param name="rootView">The view used to dispatch <see cref="SetConditionComparisonCommand"/>.</param>
        /// <typeparam name="TModel">The type of the condition model.</typeparam>
        /// <returns>The configured popup field.</returns>
        public static PopupField<ConditionComparison> CreateComparisonPopup<TModel>(
            Type valueType, TModel model, RootView rootView)
            where TModel : ConditionModel, IComparisonConditionModel
        {
            return CreateComparisonPopup(SupportsOrdering(valueType) ? k_AllChoices : k_EqualityChoices, model, rootView);
        }

        /// <summary>
        /// Creates the popup field used to edit a condition's comparison operator, offering an explicit list of
        /// operators.
        /// </summary>
        /// <param name="comparisons">The comparison operators offered by the popup.</param>
        /// <param name="model">The condition model whose comparison is edited.</param>
        /// <param name="rootView">The view used to dispatch <see cref="SetConditionComparisonCommand"/>.</param>
        /// <typeparam name="TModel">The type of the condition model.</typeparam>
        /// <returns>The configured popup field.</returns>
        public static PopupField<ConditionComparison> CreateComparisonPopup<TModel>(
            IReadOnlyList<ConditionComparison> comparisons, TModel model, RootView rootView)
            where TModel : ConditionModel, IComparisonConditionModel
        {
            var choices = comparisons as List<ConditionComparison> ?? new List<ConditionComparison>(comparisons);
            var current = choices.Contains(model.Comparison) ? model.Comparison : choices[0];
            var popup = new PopupField<ConditionComparison>(choices, current, ToGlyph, ToGlyph);
            popup.RegisterValueChangedCallback(evt =>
                rootView.Dispatch(new SetConditionComparisonCommand(model, evt.newValue)));
            return popup;
        }

        /// <summary>
        /// Gets the glyph used to display a <see cref="ConditionComparison"/>.
        /// </summary>
        /// <param name="comparison">The comparison operator.</param>
        /// <returns>The glyph representing the operator.</returns>
        public static string ToGlyph(this ConditionComparison comparison)
        {
            switch (comparison)
            {
                case ConditionComparison.Equal: return "=";
                case ConditionComparison.NotEqual: return "≠";
                case ConditionComparison.Less: return "<";
                case ConditionComparison.LessOrEqual: return "≤";
                case ConditionComparison.Greater: return ">";
                case ConditionComparison.GreaterOrEqual: return "≥";
                default: return comparison.ToString();
            }
        }
    }
}
