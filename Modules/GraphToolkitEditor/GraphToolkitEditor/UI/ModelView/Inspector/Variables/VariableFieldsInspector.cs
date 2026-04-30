// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Pool;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Declare that the variable field must be shown in the variable quick settings in the blackboard.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    [UnityRestricted]
    internal class VariableQuickSettingsAttribute : Attribute
    {
    }

    /// <summary>
    /// Declare that the variable field must be shown in the advanced section in the variables inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    [UnityRestricted]
    internal class VariableAdvancedAttribute : Attribute
    {
    }

    /// <summary>
    /// Extensions methods for <see cref="VariableFieldsInspector.DisplayFlags"/>.
    /// </summary>
    [UnityRestricted]
    internal static class DisplayFlagsExtensions
    {
        /// <summary>
        /// Checks if <paramref name="value"/> has the <paramref name="flag"/> set.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="flag">The flag to check.</param>
        /// <returns>True if value has the flag set.</returns>
        public static bool HasFlagFast(this VariableFieldsInspector.DisplayFlags value, VariableFieldsInspector.DisplayFlags flag)
        {
            return (value & flag) != 0;
        }

        /// <summary>
        /// Checks if <paramref name="value"/> has any of the <paramref name="flags"/> set.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="flags">The flags to check.</param>
        /// <returns>True if value has at least one of the flags set.</returns>
        public static bool HasAnyFlagFast(this VariableFieldsInspector.DisplayFlags value, VariableFieldsInspector.DisplayFlags flags)
        {
            return (value & flags) != 0;
        }
    }

    /// <summary>
    /// Inspector for <see cref="VariableDeclarationModelBase"/>.
    /// </summary>
    [UnityRestricted]
    internal class VariableFieldsInspector : GraphElementFieldInspector
    {
        DisplayFlags m_DisplayFlags;
        ModifierFlags m_PreviousModifiers;

        /// <summary>
        /// The flags that controls which properties are displayed.
        /// </summary>
        [Flags]
        [UnityRestricted]
        internal enum DisplayFlags
        {
            /// <summary>
            /// For standard, serialized fields, only display those with the <see cref="VariableQuickSettingsAttribute"/>.
            /// </summary>
            QuickSettingsAttributeOnly = 1 << 0,

            /// <summary>
            /// Display the type property.
            /// </summary>
            TypeProperty = 1 << 1,

            /// <summary>
            /// Display the default value property.
            /// </summary>
            DefaultValueProperty = 1 << 2,

            /// <summary>
            /// Display the exposed property, only when <see cref="GraphModel.AllowExposedVariableCreation"/> is true.
            /// </summary>
            ExposedProperty = 1 << 3,

            /// <summary>
            /// Display the subgraph port property.
            /// </summary>
            SubgraphPortProperty = 1 << 4,

            /// <summary>
            /// Do not display the subgraph port toggle as the variable is always a subgraph port.
            /// </summary>
            AlwaysSubgraphPortProperty = 1 << 6,

            /// <summary>
            /// This inspector must show only advanced properties.
            /// </summary>
            AdvancedProperties = 1 << 5,

            /// <summary>
            /// Display the tooltip property.
            /// </summary>
            TooltipProperty = 1 << 7,

            /// <summary>
            /// Display the mode property (Single vs List).
            /// </summary>
            CollectionModeProperty = 1 << 8,

            /// <summary>
            /// The default, which is to display everything.
            /// </summary>
            Default = TypeProperty | DefaultValueProperty | ExposedProperty | SubgraphPortProperty | TooltipProperty | CollectionModeProperty,

            /// <summary>
            /// The value for standard quick settings properties.
            /// </summary>
            QuickSettings = QuickSettingsAttributeOnly | TypeProperty | CollectionModeProperty | DefaultValueProperty | ExposedProperty
        }

        /// <summary>
        /// The text used to describe the Exposed property.
        /// </summary>
        public virtual string ExposedPropertyLabelText => "Expose in global inspector";

        /// <summary>
        /// Creates a new instance of the <see cref="VariableFieldsInspector"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="models">The models displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="filter">A filter function to select which fields are displayed in the inspector. If null, defaults to <see cref="SerializedFieldsInspector.CanBeInspected"/>.</param>
        /// <param name="displayFlags"><see cref="DisplayFlags"/> that define which properties to display.</param>
        /// <returns>A new instance of <see cref="VariableFieldsInspector"/>.</returns>
        public static VariableFieldsInspector Create(string name, IReadOnlyList<VariableDeclarationModelBase> models,
            ChildView ownerElement, string parentClassName, Func<FieldInfo, bool> filter = null, DisplayFlags displayFlags = DisplayFlags.Default)
        {
            return models.Count > 0 ? new VariableFieldsInspector(name, models, ownerElement, parentClassName, filter, displayFlags) : null;
        }

        static Func<FieldInfo, bool> DisplayFlagsToFilter(DisplayFlags displayFlags, Func<FieldInfo, bool> filter)
        {
            return fi =>
            {
                if (filter != null && !filter(fi))
                    return false;
                if (displayFlags.HasFlag(DisplayFlags.QuickSettingsAttributeOnly) && fi.GetCustomAttribute<VariableQuickSettingsAttribute>() == null)
                    return false;
                var advancedAttribute = fi.GetCustomAttribute<VariableAdvancedAttribute>();
                if (!displayFlags.HasFlag(DisplayFlags.AdvancedProperties) && advancedAttribute != null)
                    return false;
                if (displayFlags.HasFlag(DisplayFlags.AdvancedProperties) && advancedAttribute == null)
                    return false;
                return true;
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableFieldsInspector"/> class.
        /// </summary>
        protected VariableFieldsInspector(string name, IReadOnlyList<VariableDeclarationModelBase> models, ChildView ownerElement, string parentClassName, Func<FieldInfo, bool> filter, DisplayFlags displayFlags)
            : base(name, models, ownerElement, parentClassName, DisplayFlagsToFilter(displayFlags, filter))
        {
            m_DisplayFlags = displayFlags;
        }

        TypeHandle m_CurrentDefaultValueFieldType;

        /// <inheritdoc />
        protected override IReadOnlyList<BaseModelPropertyField> GetFields()
        {
            var baseFieldList = base.GetFields();

            if (!m_DisplayFlags.HasAnyFlagFast(DisplayFlags.TypeProperty | DisplayFlags.DefaultValueProperty | DisplayFlags.ExposedProperty | DisplayFlags.SubgraphPortProperty))
            {
                return baseFieldList;
            }

            var fieldList = new List<BaseModelPropertyField>(baseFieldList);

            int insertIndex = fieldList.Count > 0 ? 1 : 0; // Name should be first

            var variableModels = new List<VariableDeclarationModelBase>(m_Models.Count);
            foreach (var v in m_Models)
            {
                variableModels.Add(v as VariableDeclarationModelBase);
            }

            if (m_DisplayFlags.HasFlagFast(DisplayFlags.TooltipProperty))
            {
                var tooltipEditor = new VariableTooltipPropertyField(OwnerRootView, variableModels);
                fieldList.Insert(insertIndex++, tooltipEditor);
            }

            if (m_DisplayFlags.HasFlagFast(DisplayFlags.TypeProperty))
            {
                var typeEditor = new VariableTypePropertyField(OwnerRootView, variableModels);
                fieldList.Insert(insertIndex++, typeEditor);
            }

            if (m_DisplayFlags.HasFlagFast(DisplayFlags.CollectionModeProperty))
            {
                var variableDeclaration = variableModels[0];
                var allSerializable = InternalTypeHelpers.IsTypeSerializable(variableDeclaration.DataType.Resolve());
                for (var i = 1; i < variableModels.Count; ++i)
                {
                    variableDeclaration = variableModels[i];
                    if (!InternalTypeHelpers.IsTypeSerializable(variableDeclaration.DataType.Resolve()))
                    {
                        allSerializable = false;
                        break;
                    }
                }

                if (allSerializable)
                {
                    var modeEditor = new VariableModePropertyField(OwnerRootView, variableModels);
                    fieldList.Insert(insertIndex++, modeEditor);
                }
            }

            if (m_DisplayFlags.HasFlagFast(DisplayFlags.DefaultValueProperty))
            {
                // Selected Variables must all have an Initialization model of the same TypeHandle to display their default value.
                var variableDeclaration = variableModels[0];
                if (!variableDeclaration.Modifiers.HasFlag(ModifierFlags.Write))
                {
                    using var dispose = ListPool<Constant>.Get(out var constants);

                    bool valid = variableDeclaration.InitializationModel != null;
                    TypeHandle firstHandle = default;
                    if (valid)
                    {
                        firstHandle = variableDeclaration.DataType;
                        constants.Add(variableDeclaration.InitializationModel);
                        for (var i = 1; i < variableModels.Count; i++)
                        {
                            variableDeclaration = variableModels[i];
                            if (variableDeclaration.InitializationModel == null || variableDeclaration.DataType != firstHandle)
                            {
                                valid = false;
                                break;
                            }

                            constants.Add(variableDeclaration.InitializationModel);
                        }
                    }

                    if (valid)
                    {
                        m_CurrentDefaultValueFieldType = firstHandle;
                        var field = InlineValueEditor.CreateEditorForConstants(OwnerRootView, variableModels, constants, "Default Value");
                        fieldList.Insert(insertIndex++, field);
                    }
                }
            }

            var graphModel = (OwnerRootView as GraphView)?.GraphModel ?? (variableModels.Count > 0 ? variableModels[0].GraphModel : null);
            var allowExposedVariables = graphModel?.AllowExposedVariableCreation ?? true;

            if (allowExposedVariables && m_DisplayFlags.HasFlagFast(DisplayFlags.ExposedProperty))
            {
                var exposedEditor = new VariableExposedPropertyField(OwnerRootView, variableModels, ExposedPropertyLabelText);
                fieldList.Insert(insertIndex++, exposedEditor);
            }

            if (m_DisplayFlags.HasFlagFast(DisplayFlags.SubgraphPortProperty))
            {
                var subgraphPortEditor = new VariableSubgraphPortPropertyField(OwnerRootView, variableModels, !m_DisplayFlags.HasFlag(DisplayFlags.AlwaysSubgraphPortProperty));
                fieldList.Insert(insertIndex, subgraphPortEditor);
            }

            // If any of the selected variables are not editable, disable all fields.
            var isEditable = true;
            foreach (var variableModel in variableModels)
            {
                if (!variableModel.HasCapability(Capabilities.Editable))
                {
                    isEditable = false;
                    break;
                }
            }
            foreach (var field in fieldList)
            {
                field.SetEnabled(isEditable);
            }

            return fieldList;
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            bool valid;
            TypeHandle firstHandle = default;
            ModifierFlags firstModifiers = ModifierFlags.None;

            using (m_Models.OfTypeToPooledList<VariableDeclarationModelBase, Model>(out var variableModels))
            {
                valid = variableModels[0].InitializationModel != null;
                if (valid)
                {
                    firstHandle = variableModels[0].DataType;
                    firstModifiers = variableModels[0].Modifiers;
                    for (var i = 0; i < variableModels.Count; i++)
                    {
                        if (variableModels[i].InitializationModel == null || variableModels[i].DataType != firstHandle || variableModels[i].Modifiers != firstModifiers || m_PreviousModifiers != variableModels[i].Modifiers)
                        {
                            valid = false;
                            break;
                        }
                    }
                }
            }

            if ((m_CurrentDefaultValueFieldType.IsValid != valid) || (valid && m_CurrentDefaultValueFieldType != firstHandle) || (m_PreviousModifiers != firstModifiers))
            {
                BuildFields();
                m_PreviousModifiers = firstModifiers;
            }

            base.UpdateUIFromModel(visitor);
        }
    }
}
