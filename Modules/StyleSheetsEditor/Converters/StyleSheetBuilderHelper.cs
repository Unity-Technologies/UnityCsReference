// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.StyleSheets;

namespace UnityEditor.StyleSheets
{
    class StyleSheetBuilderHelper
    {
        public StyleSheetBuilder builder { get; private set; }
        public UssExportOptions options { get; private set; }
        public StyleSheet sheet { get; private set; }

        public StyleSheetBuilderHelper(UssExportOptions opts = null)
        {
            builder = new StyleSheetBuilder();
            options = opts ?? new UssExportOptions();
        }

        public StyleRule BeginRule(string comment = "")
        {
            var rule = builder.BeginRule(0);
            options.AddComment(rule, comment);
            return rule;
        }

        public void EndRule()
        {
            builder.EndRule();
        }

        public void AddProperty(string name, Color value, string comment = "")
        {
            var property = builder.BeginProperty(name);
            options.AddComment(property, comment);
            builder.AddValue(value);
            builder.EndProperty();
        }

        public void AddProperty(string name, bool value, string comment = "")
        {
            var property = builder.BeginProperty(name);
            options.AddComment(property, comment);
            builder.AddValue(value ? StyleValueKeyword.True : StyleValueKeyword.False);
            builder.EndProperty();
        }

        public void AddProperty(string name, string value, string comment = "")
        {
            var property = builder.BeginProperty(name);
            options.AddComment(property, comment);
            builder.AddValue(value, StyleValueType.Enum);
            builder.EndProperty();
        }

        public void AddPropertyString(string name, string value, string comment = "")
        {
            var property = builder.BeginProperty(name);
            options.AddComment(property, comment);
            builder.AddValue(value, StyleValueType.String);
            builder.EndProperty();
        }

        public void AddPropertyResource(string name, string value, string comment = "")
        {
            var property = builder.BeginProperty(name);
            options.AddComment(property, comment);
            builder.AddValue(value, StyleValueType.ResourcePath);
            builder.EndProperty();
        }

        public void AddProperty(string name, StyleValueKeyword value, string comment = "")
        {
            var property = builder.BeginProperty(name);
            options.AddComment(property, comment);
            builder.AddValue(value);
            builder.EndProperty();
        }

        public void AddProperty(string name, float value, string comment = "")
        {
            var property = builder.BeginProperty(name);
            options.AddComment(property, comment);
            builder.AddValue(value);
            builder.EndProperty();
        }

        public void AddProperty(string name, Vector2 offset, string comment = "")
        {
            var property = builder.BeginProperty(name);
            options.AddComment(property, comment);
            builder.AddValue(offset.x);
            builder.AddValue(offset.y);
            builder.EndProperty();
        }

        public void PopulateSheet()
        {
            sheet = ScriptableObject.CreateInstance<StyleSheet>();
            builder.BuildTo(sheet);
        }

        public string ToUsstring()
        {
            if (sheet == null)
            {
                PopulateSheet();
            }
            return StyleSheetToUss.ToUssString(sheet, options);
        }

        public static Vector2 ReadVector2(StyleSheet sheet, StyleProperty property)
        {
            return new Vector2(sheet.ReadFloat(property.values[0]), sheet.ReadFloat(property.values[1]));
        }

        #region StyleSheetManipulation
        public static void CopyProperty(StyleSheet sheet, UssComments comments, StyleProperty property, StyleSheetBuilderHelper helper)
        {
            var propertyCopy = helper.builder.BeginProperty(property.name);
            helper.options.comments.AddComment(propertyCopy, comments.Get(property));

            foreach (var value in property.values)
            {
                switch (value.valueType)
                {
                    case StyleValueType.Color:
                        helper.builder.AddValue(sheet.ReadColor(value));
                        break;
                    case StyleValueType.Enum:
                        helper.builder.AddValue(sheet.ReadEnum(value), StyleValueType.Enum);
                        break;
                    case StyleValueType.Float:
                        helper.builder.AddValue(sheet.ReadFloat(value));
                        break;
                    case StyleValueType.Keyword:
                        helper.builder.AddValue(sheet.ReadKeyword(value));
                        break;
                    case StyleValueType.ResourcePath:
                        helper.builder.AddValue(sheet.ReadResourcePath(value), StyleValueType.ResourcePath);
                        break;
                    case StyleValueType.String:
                        helper.builder.AddValue(sheet.ReadString(value), StyleValueType.String);
                        break;
                }
            }
            helper.builder.EndProperty();
        }

        public static void PopulateProperties(IEnumerable<StyleProperty> properties, Dictionary<string, PropertyPair> pairs, bool item1)
        {
            foreach (var property in properties)
            {
                PropertyPair pair;
                if (!pairs.TryGetValue(property.name, out pair))
                {
                    pair = new PropertyPair { name = property.name };
                    pairs.Add(property.name, pair);
                }
                if (item1)
                {
                    pair.p1 = property;
                }
                else
                {
                    pair.p2 = property;
                }
            }
        }

        public static void BuildSelector(StyleComplexSelector complexSelector, StyleSheetBuilderHelper helper)
        {
            using (helper.builder.BeginComplexSelector(complexSelector.specificity))
            {
                foreach (var selector in complexSelector.selectors)
                {
                    helper.builder.AddSimpleSelector(selector.parts, selector.previousRelationship);
                }
            }
        }

        public static void CopySelector(StyleSheet sheet, UssComments comments, StyleComplexSelector complexSelector, StyleSheetBuilderHelper helper)
        {
            helper.BeginRule(comments.Get(complexSelector.rule));
            BuildSelector(complexSelector, helper);

            foreach (var property in complexSelector.rule.properties)
            {
                CopyProperty(sheet, comments, property, helper);
            }

            helper.EndRule();
        }

        #endregion
    }
}
