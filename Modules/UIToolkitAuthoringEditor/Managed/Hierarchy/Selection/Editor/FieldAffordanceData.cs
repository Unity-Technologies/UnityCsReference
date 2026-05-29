// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Represents the different ways the value of a field has been resolved.
    /// </summary>
    internal enum FieldAffordanceSourceInfoType
    {
        /// <summary>
        /// Indicates that the value of the underlying property is default.
        /// </summary>
        Default,
        /// <summary>
        /// Indicates that the value of the underlying VisualElement property is set as inline in its source UXML file.
        /// </summary>
        Inline,
        /// <summary>
        /// Indicates that the value of the underlying Selector property is set in the selector's definition in its
        /// source StyleSheet file.
        /// </summary>
        LocalUSSSelector,
        /// <summary>
        /// Indicates that the value of the underlying VisualElement property is resolved from one of its matching
        /// USS selectors.
        /// </summary>
        MatchingUSSSelector,
        /// <summary>
        /// Indicates that the value of the underlying VisualElement property is inherited from one of its parents
        /// (ancestors).
        /// </summary>
        Inherited,
        /// <summary>
        /// Indicates that the value of the underlying VisualElement property is resolved from a Binding
        /// </summary>
        ResolvedBinding,
        /// <summary>
        /// Indicates that the value of the underlying VisualElement property is resolved from a Binding but the binding
        /// is unresolved
        /// </summary>
        UnresolvedBinding,
        /// <summary>
        /// Indicates that the value of the underlying VisualElement property is resolved from a Binding but the binding
        /// is unhandled
        /// </summary>
        UnhandledBinding,
        /// <summary>
        /// Indicates that the value of the underlying VisualElement property is resolved from a variable coming from a stylesheet
        /// </summary>
        USSVariable,
        /// <summary>
        /// Indicates that the value of the underlying VisualElement style property is currently driven by an animation
        /// </summary>
        AnimationAnimated,
        /// <summary>
        /// Indicates that the value of the underlying VisualElement style property is being driven by an animation and recording it
        /// </summary>
        AnimationRecording,
        /// <summary>
        /// Indicates that the value of the underlying VisualElement style property is being driven by an animation and
        /// has a pending candidate edit.
        /// </summary>
        AnimationCandidate
    }

    internal static class FieldAffordanceSourceInfoTypeExtensions
    {
        public static bool IsAnimationDriven(this FieldAffordanceSourceInfoType type)
        {
            return type is FieldAffordanceSourceInfoType.AnimationAnimated
                or FieldAffordanceSourceInfoType.AnimationRecording
                or FieldAffordanceSourceInfoType.AnimationCandidate;
        }

        // Field-level disable rule. Recording routes edits through AnimationRecordingStyleBridge;
        // Candidate routes through the candidate hook. Only Animated has no edit channel.
        public static bool ShouldDisableInlineEdit(this FieldAffordanceSourceInfoType type)
        {
            return type == FieldAffordanceSourceInfoType.AnimationAnimated;
        }
    }

    internal enum FieldAffordanceDataType
    {
        /// <summary>
        /// None
        /// </summary>
        None,
        /// <summary>
        /// UXML attribute.
        /// </summary>
        UXMLAttribute,
        /// <summary>
        /// USS property.
        /// </summary>
        USSProperty
    }

    internal class FieldAffordanceData : INotifyBindablePropertyChanged
    {
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        FieldAffordanceDataType m_Type;

        /// <summary>
        /// The type of the field affordance data (e.g., USS property, UXML attribute).
        /// </summary>
        public FieldAffordanceDataType type
        {
            get => m_Type;
            set
            {
                if (m_Type != value)
                {
                    m_Type = value;
                    Notify();
                }
            }
        }

        FieldAffordanceSourceInfoType m_SourceInfoType;

        /// <summary>
        /// The source information type describing how the field's value is resolved.
        /// </summary>
        public FieldAffordanceSourceInfoType sourceTypeInfo
        {
            get => m_SourceInfoType;
            set
            {
                if (m_SourceInfoType != value)
                {
                    m_SourceInfoType = value;
                    Notify();
                }
            }
        }

        object m_InlineValue;

        /// <summary>
        /// The inline value of the field, used to resolve the variable name.
        /// </summary>
        public object inlineValue
        {
            get => m_InlineValue;
            set
            {
                if (m_InlineValue != value)
                {
                    m_InlineValue = value;
                    Notify();
                }
            }
        }

        Binding m_Binding;
        /// <summary>
        /// The binding associated with the field, if any.
        /// </summary>
        public Binding binding
        {
            get => m_Binding;
            set
            {
                if (m_Binding != value)
                {
                    m_Binding = value;
                    Notify();
                }
            }
        }

        SelectorMatchRecord m_Selector;
        /// <summary>
        /// The selector match record for USS selectors or variables.
        /// </summary>
        public SelectorMatchRecord selector
        {
            get => m_Selector;
            set
            {
                if (!m_Selector.Equals(value))
                {
                    m_Selector = value;
                    Notify();
                }
            }
        }

        VisualElement m_TargetElement;

        /// <summary>
        /// The target visual element used for resolving the hierarchical data source.
        /// </summary>
        public VisualElement targetElement
        {
            get => m_TargetElement;
            set
            {
                if (m_TargetElement != value)
                {
                    m_TargetElement = value;
                    Notify();
                }
            }
        }

        StyleSheet m_VariableSheet;

        /// <summary>
        /// The stylesheet containing the variable.
        /// </summary>
        public StyleSheet variableSheet
        {
            get => m_VariableSheet;
            set
            {
                if (m_VariableSheet != value)
                {
                    m_VariableSheet = value;
                    Notify();
                }
            }
        }

        private void Notify([CallerMemberName] string name = null)
        {
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(name));
        }

        /// <summary>
        /// Resets the field affordance data to its default state.
        /// </summary>
        public void Reset()
        {
            type = FieldAffordanceDataType.None;
            sourceTypeInfo = FieldAffordanceSourceInfoType.Default;
            targetElement = null;
            binding = null;
            selector = default;
            inlineValue = null;
            variableSheet = null;
        }
    }
}
