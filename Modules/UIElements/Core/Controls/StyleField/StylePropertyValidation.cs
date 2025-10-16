// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Properties;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.UIElements.StyleSheets.Syntax;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// This represents a collection of Syntax in StyleFields.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    [UxmlObject]
    internal abstract class StylePropertyValidation : INotifyBindablePropertyChanged
    {
        [ExcludeFromDocs, Serializable]
        public abstract class UxmlSerializedData : UIElements.UxmlSerializedData { };

        /// <summary>
        /// Called when a property has changed.
        /// </summary>
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        protected void NotifyPropertyChanged(in BindingId bindingId)
        {
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(bindingId));
        }
    }

    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    [UxmlObject]
    internal class Syntax : StylePropertyValidation
    {
        static readonly BindingId propertyBindingProperty = nameof(property);

        [ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StylePropertyValidation.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(property), "property"),
                }, false);
            }

            #pragma warning disable 649
            [SerializeField] string property;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags property_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new Syntax();

            public override void Deserialize(object obj)
            {
                var e = (Syntax)obj;
                if (ShouldWriteAttributeValue(property_UxmlAttributeFlags))
                    e.property = property;
            }
        }

        static readonly List<string> k_SyntaxTerms = new()
        {"length", "length-percentage", "color", "url", "resource", "angle", "number", "time",
            "single-transition-property", "easing-function" };

        string m_Property;

        /// <summary>
        /// The style property name or syntax that will be used to validate the style field value.
        /// </summary>
        [CreateProperty]
        public string property
        {
            get => m_Property;
            set
            {
                if (string.Compare(m_Property, value, StringComparison.Ordinal) == 0)
                    return;

                m_Property = value;
                NotifyPropertyChanged(propertyBindingProperty);
            }
        }

        public Syntax() { }

        public Syntax(string property)
        {
            this.property = property;
        }

        public static Expression GetSyntaxTree(Syntax syntax)
        {
            var syntaxParser = new StyleSyntaxParser();

            // Aggregate all syntax into a single expression
            var expression = GetExpressionString(syntax);
            var syntaxTree = syntaxParser.Parse(expression);
            return syntaxTree;
        }

        public static Expression GetSyntaxTree(List<Syntax> syntaxes)
        {
            var syntaxParser = new StyleSyntaxParser();

            // Aggregate all syntax into a single expression
            var expression = string.Join(" | ", syntaxes.UniqueSelect(GetExpressionString));
            var syntaxTree = syntaxParser.Parse(expression);
            return syntaxTree;
        }

        static string GetExpressionString(Syntax syntax)
        {
            if (string.IsNullOrEmpty(syntax.property))
                return null;

            // Add the <> to the syntax to make it a valid expression
            if (k_SyntaxTerms.Contains(syntax.property))
                return $"<{syntax.property}>";

            var syntaxFound = StylePropertyCache.TryGetSyntax(syntax.property, out _);
            return syntaxFound ? $"<'{syntax.property}'>" : syntax.property;
        }
    }
}
